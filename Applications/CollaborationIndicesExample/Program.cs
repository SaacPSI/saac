// <copyright file="Program.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

namespace CollaborationIndicesExample
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi;
    using SAAC.CollaborationIndices;

    /// <summary>
    /// How to instantiate the collaboration indices.
    ///
    ///   CollaborationIndicesExample builder  [outputFolder] [--realtime]
    ///   CollaborationIndicesExample config   [outputFolder] [--realtime]
    ///   CollaborationIndicesExample windows  [outputFolder] [--realtime]
    ///
    /// The three modes compute the same indices on the same synthetic session:
    ///  - builder: the indicators are declared in code;
    ///  - config:  the indicators are declared in indices.json, next to the executable;
    ///  - windows: the same declaration, computed on several windows side by side.
    ///
    /// The steps are always the same:
    ///  1. declare the participants, the window and the indicators (builder or configuration);
    ///  2. Build(), which checks the declaration and creates the components;
    ///  3. connect the source streams to the inputs of the indicators;
    ///  4. run the pipeline.
    ///
    /// In a real pipeline, step 3 connects the streams of the sensors or of a store, as
    /// RealTimeProcessingUseCase does; here they come from <see cref="SyntheticSession"/>.
    /// </summary>
    internal static class Program
    {
        private static readonly uint[] Participants = { 0, 1, 2 };

        private static readonly TimeSpan SessionDuration = TimeSpan.FromSeconds(90);

        private static int threadCount;

        private static int Main(string[] args)
        {
            string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "builder";
            bool realTime = args.Any(argument => argument == "--realtime");
            string outputFolder = args.Skip(1).FirstOrDefault(argument => !argument.StartsWith("--"))
                ?? Path.Combine(Path.GetTempPath(), "CollaborationIndicesExample");

            Directory.CreateDirectory(outputFolder);

            // Run as fast as possible by default: the timestamps of the messages drive the
            // indices, not the clock of the machine. --realtime replays at the original pace.
            // Pacing a recorded session needs the clock of the pipeline to start where the data starts.
            ReplayDescriptor replay = realTime ? new ReplayDescriptor(SyntheticSession.Start, true) : ReplayDescriptor.ReplayAll;

            // A replay that is not paced by the clock delivers the messages as fast as it can.
            // With several threads, a tick can then be handled before the last positions stamped
            // before it have been delivered, and two runs of the same data differ in the last
            // digits. One thread makes an offline replay exactly reproducible.
            threadCount = realTime ? 0 : 1;

            try
            {
                switch (mode)
                {
                    case "builder":
                        RunWithBuilder(outputFolder, replay);
                        return 0;

                    case "config":
                        RunWithConfiguration(outputFolder, replay, Path.Combine(AppContext.BaseDirectory, "indices.json"));
                        return 0;

                    case "windows":
                        RunWithSeveralWindows(outputFolder, replay);
                        return 0;

                    default:
                        Console.Error.WriteLine("Usage: CollaborationIndicesExample [builder|config|windows] [outputFolder] [--realtime]");
                        return 1;
                }
            }
            catch (CollaborationIndicesConfigurationException exception)
            {
                // Build reports every problem of a declaration at once.
                Console.Error.WriteLine(exception.Message);
                return 2;
            }
        }

        /// <summary>
        /// Mode 1: the indicators are declared in code.
        /// </summary>
        private static void RunWithBuilder(string outputFolder, ReplayDescriptor replay)
        {
            string csvPath = Path.Combine(outputFolder, "indices.csv");

            using (Pipeline pipeline = Pipeline.Create("CollaborationIndicesExample", threadCount: threadCount))
            {
                var session = new SyntheticSession(pipeline, Participants, SessionDuration);

                CollaborationIndicesBuilder builder = new CollaborationIndicesBuilder(pipeline)
                    .WithName("Indices")
                    .WithParticipants(Participants)
                    .WithWindow(TimeSpan.FromSeconds(20))
                    .WithComputationInterval(TimeSpan.FromSeconds(1))

                    // One tick per second of data, taken from a dense stream: the clock is on
                    // the timeline of the data, live as well as in replay.
                    .WithDataClock(session.Frames)

                    // The indicators: one line each. Remove a line, the index is not computed
                    // any more; nothing else has to change.
                    .AddIndicator(Indicators.Movement, options => options.AdditionalWindows = new List<TimeSpan> { TimeSpan.FromSeconds(5) })
                    .AddIndicator(Indicators.Synchrony, options => options.SubsetSize = 3)
                    .AddIndicator(Indicators.VerbalParticipation)
                    .AddIndicator(Indicators.SpeechEquality)
                    .AddIndicator(Indicators.TalkingMost)

                    // What is done with them.
                    .WithCollaborationScore()
                    .WithCsvExport(csvPath);

                // The declaration as data, kept next to the results: it says what was computed
                // and can be loaded back with the "config" mode.
                builder.ToConfiguration().Save(Path.Combine(outputFolder, "indices-configuration.json"));

                SlidingAverageComputation indices = builder.Build();

                ConnectSources(session, indices);
                PrintSnapshots(indices, "20 s");

                Console.WriteLine($"Indicators: {string.Join(", ", indices.Indicators.Select(indicator => indicator.Name))}");
                PrintHeader();
                pipeline.Run(replay);
            }

            Report(csvPath);
        }

        /// <summary>
        /// Mode 2: the indicators are declared in a JSON file.
        /// </summary>
        private static void RunWithConfiguration(string outputFolder, ReplayDescriptor replay, string configurationPath)
        {
            CollaborationIndicesConfiguration configuration = CollaborationIndicesConfiguration.Load(configurationPath);
            configuration.ExportPath ??= Path.Combine(outputFolder, "indices-from-configuration.csv");

            using (Pipeline pipeline = Pipeline.Create("CollaborationIndicesExample", threadCount: threadCount))
            {
                var session = new SyntheticSession(pipeline, configuration.ParticipantIds, SessionDuration);

                // A stream is not data: the clock is given in code, on the builder that the
                // configuration produced. Anything else could be added the same way.
                SlidingAverageComputation indices = CollaborationIndicesBuilder.FromConfiguration(pipeline, configuration)
                    .WithDataClock(session.Frames)
                    .Build();

                ConnectSources(session, indices);
                PrintSnapshots(indices, $"{configuration.WindowDuration.TotalSeconds:0} s");

                Console.WriteLine($"Configuration: {configurationPath}");
                Console.WriteLine($"Indicators: {string.Join(", ", indices.Indicators.Select(indicator => indicator.Name))}");
                PrintHeader();
                pipeline.Run(replay);
            }

            Report(configuration.ExportPath);
        }

        /// <summary>
        /// Mode 3: the same declaration, computed on several windows side by side.
        /// </summary>
        private static void RunWithSeveralWindows(string outputFolder, ReplayDescriptor replay)
        {
            var writers = new Dictionary<TimeSpan, TextWriter>();
            foreach (int seconds in new[] { 10, 20, 30 })
            {
                writers[TimeSpan.FromSeconds(seconds)] = new StreamWriter(Path.Combine(outputFolder, $"indices{SlidingAverageComputationSet.StoreSuffix(TimeSpan.FromSeconds(seconds))}.csv"));
            }

            try
            {
                using (Pipeline pipeline = Pipeline.Create("CollaborationIndicesExample", threadCount: threadCount))
                {
                    var session = new SyntheticSession(pipeline, Participants, SessionDuration);

                    // One instance per window, named Indices_10s, Indices_20s and Indices_30s,
                    // each one with the calibration measured for its window, all on one clock.
                    SlidingAverageComputationSet set = new CollaborationIndicesBuilder(pipeline)
                        .WithName("Indices")
                        .WithParticipants(Participants)
                        .WithDataClock(session.Frames)
                        .AddIndicator(Indicators.Movement)
                        .AddIndicator(Indicators.Synchrony)
                        .AddIndicator(Indicators.VerbalParticipation)
                        .AddIndicator(Indicators.SpeechEquality)
                        .AddIndicator(Indicators.TalkingMost)
                        .WithCollaborationScore()
                        .BuildSet(writers);

                    // The source streams are shared: a stream can feed any number of instances.
                    set.ForEach(indices => ConnectSources(session, indices));
                    set.ForEach((window, indices) => PrintSnapshots(indices, $"{window.TotalSeconds:0} s", 15));

                    PrintHeader();
                    pipeline.Run(replay);
                }
            }
            finally
            {
                // The writers were opened here, so they are closed here.
                foreach (TextWriter writer in writers.Values)
                {
                    writer.Dispose();
                }
            }

            foreach (TimeSpan window in writers.Keys)
            {
                Report(Path.Combine(outputFolder, $"indices{SlidingAverageComputationSet.StoreSuffix(window)}.csv"));
            }
        }

        /// <summary>
        /// Step 3: connects the source streams to the inputs of the indicators that are part
        /// of the instance. An indicator is reached through its catalogue entry, and its
        /// component exposes the inputs.
        /// </summary>
        private static void ConnectSources(SyntheticSession session, SlidingAverageComputation indices)
        {
            foreach (uint participantId in indices.ParticipantIds)
            {
                if (indices.Contains(IndexNames.Movement))
                {
                    // All the body parts of a participant at once. A stream of single positions
                    // would go to GetPositionInput(participantId, BodyPartNames.Head) instead.
                    session.Body(participantId).PipeTo(indices.Get(Indicators.Movement).Component.GetBodyInput(participantId));
                }

                if (indices.Contains(IndexNames.Synchrony))
                {
                    session.Body(participantId).PipeTo(indices.Get(Indicators.Synchrony).Component.GetBodyInput(participantId));
                }

                if (indices.Contains(IndexNames.VerbalParticipation))
                {
                    session.Speech(participantId).PipeTo(indices.Get(Indicators.VerbalParticipation).Component.GetIntervalInput(participantId));
                }
            }
        }

        /// <summary>
        /// Prints some of the snapshots. A snapshot holds every index of one tick, all
        /// describing the same instant: the simplest output to consume downstream.
        /// </summary>
        private static void PrintSnapshots(SlidingAverageComputation indices, string label, int every = 5)
        {
            int count = 0;
            indices.SnapshotOut.Do((snapshot, envelope) =>
            {
                if (count++ % every != 0)
                {
                    return;
                }

                double seconds = (envelope.OriginatingTime - SyntheticSession.Start).TotalSeconds;
                Console.WriteLine(
                    $"{label,6} {seconds,7:0.0}   {Cell(snapshot.Group, IndexNames.Movement)} {Cell(snapshot.Group, IndexNames.Synchrony)} " +
                    $"{Cell(snapshot.Group, IndexNames.VerbalParticipation)} {Cell(snapshot.Group, IndexNames.SpeechEquality)} " +
                    $"{Cell(snapshot.Group, IndexNames.TalkingMost)} {Cell(snapshot.Group, IndexNames.CollaborationScore)}");
            });
        }

        private static void PrintHeader()
        {
            Console.WriteLine();
            Console.WriteLine("window    time   movement synchrony    verbal  inequal.   talking     score");
            Console.WriteLine("          (s)      (m/s)    (0-1)     (0-1)    (Gini)      most          ");
        }

        private static string Cell(Dictionary<string, double> values, string name)
            => values.TryGetValue(name, out double value) ? value.ToString("0.000", CultureInfo.InvariantCulture).PadLeft(9) : "        -";

        private static void Report(string csvPath)
        {
            string[] lines = File.ReadAllLines(csvPath);
            Console.WriteLine();
            Console.WriteLine($"{lines.Length - 1} rows written to {csvPath}");
            Console.WriteLine($"Columns: {lines[0]}");
        }
    }
}
