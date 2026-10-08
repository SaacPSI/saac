// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Psi;
using SAAC.CollaborationIndices;

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// Runs the synthetic session through a set of indices and records what comes out, under
    /// the stream names used by the reference recordings of the Golden folder.
    /// </summary>
    internal static class IndicesHarness
    {
        /// <summary>One second, the period of the clock of the indices in every scenario.</summary>
        public static readonly TimeSpan Second = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Builds a pipeline, runs it as fast as possible and returns once it has completed.
        /// </summary>
        /// <param name="threads">Number of threads: 1 for an exactly reproducible run, 0 for the default of \psi.</param>
        /// <param name="build">Fills the pipeline.</param>
        public static void Run(int threads, Action<Pipeline> build)
        {
            using (Pipeline pipeline = Pipeline.Create("CollaborationIndicesTests", null, threads))
            {
                build(pipeline);

                // ReplayAll: the messages carry their own timestamps and are not paced by the clock.
                pipeline.Run(ReplayDescriptor.ReplayAll);
            }
        }

        /// <summary>Connects the streams of the session to the indicators that are present.</summary>
        /// <param name="player">The session.</param>
        /// <param name="indices">The indices.</param>
        /// <param name="connectClock">False when the instance already has a clock.</param>
        public static void Connect(SessionPlayer player, SlidingAverageComputation indices, bool connectClock = true)
        {
            PhysicalActivityIndicator movement = indices.Find<PhysicalActivityIndicator>();
            PhysicalSynchronyIndicator synchrony = indices.Find<PhysicalSynchronyIndicator>();
            VerbalParticipationIndicator verbal = indices.Find<VerbalParticipationIndicator>();
            AttentionLevelIndicator attention = indices.Find<AttentionLevelIndicator>();

            foreach (uint id in SyntheticData.Participants)
            {
                foreach (string part in SyntheticData.BodyParts)
                {
                    if (movement != null && movement.Component.Configuration.BodyParts.Contains(part))
                    {
                        player.Positions(id, part).PipeTo(movement.Component.GetPositionInput(id, part));
                    }

                    if (synchrony != null && synchrony.Component.Configuration.BodyParts.Contains(part))
                    {
                        player.Positions(id, part).PipeTo(synchrony.Component.GetPositionInput(id, part));
                    }
                }

                if (verbal != null)
                {
                    player.Speech(id).PipeTo(verbal.Component.GetIntervalInput(id));
                }
            }

            if (connectClock)
            {
                player.Ticks.PipeTo(indices.ClockIn);
            }

            if (attention != null)
            {
                player.AttentionTicks.PipeTo(attention.ClockIn);
            }

            player.PhaseStarts.PipeTo(indices.Gate.PhaseStartIn);
            player.PhaseEnds.PipeTo(indices.Gate.PhaseEndIn);
        }

        /// <summary>Records the outputs of the indicators that are present.</summary>
        /// <param name="recorder">The recording.</param>
        /// <param name="indices">The indices.</param>
        /// <param name="prefix">Prefix of the stream names, to tell several instances apart.</param>
        public static void Record(StreamRecorder recorder, SlidingAverageComputation indices, string prefix = "")
        {
            recorder.Record($"{prefix}Gate.Tick", indices.Gate.Out);
            recorder.Record($"{prefix}Gate.Enabled", indices.Gate.EnabledOut);
            recorder.RecordWith($"{prefix}Gate.PhaseId", indices.Gate.PhaseIdOut, id => id.ToString());

            PhysicalActivityLevelComponent movement = indices.Find<PhysicalActivityIndicator>()?.Component;
            if (movement != null)
            {
                foreach (uint id in indices.ParticipantIds)
                {
                    recorder.Record($"{prefix}Movement.P{id}", movement.GetActivityLevelEmitter(id));
                }

                recorder.Record($"{prefix}Movement.Individual", movement.Out);
                recorder.Record($"{prefix}Movement.Group", movement.GroupActivityLevelOut);
                foreach (TimeSpan window in movement.Configuration.AdditionalWindows)
                {
                    recorder.Record($"{prefix}Movement.Window{(int)window.TotalSeconds}s", movement.GetWindowedActivityLevelsEmitter(window));
                }
            }

            PhysicalSynchronyComponent synchrony = indices.Find<PhysicalSynchronyIndicator>()?.Component;
            if (synchrony != null)
            {
                recorder.Record($"{prefix}Synchrony.Pair", synchrony.Out);
                recorder.Record($"{prefix}Synchrony.Raw", synchrony.PairCorrelationsOut);
                recorder.Record($"{prefix}Synchrony.Group", synchrony.GroupSynchronyOut);
                recorder.Record($"{prefix}Synchrony.Subset", synchrony.SubsetSynchronyOut);
                recorder.Record($"{prefix}Synchrony.P0-1", synchrony.GetPairSynchronyEmitter(0, 1));
            }

            AttentionLevelComponent attention = indices.Find<AttentionLevelIndicator>()?.Component;
            if (attention != null)
            {
                recorder.Record($"{prefix}Attention.Individual", attention.Out);
            }

            VerbalParticipationComponent verbal = indices.Find<VerbalParticipationIndicator>()?.Component;
            if (verbal != null)
            {
                recorder.Record($"{prefix}Verbal.Individual", verbal.Out);
                recorder.Record($"{prefix}Verbal.SpeakingTimes", verbal.SpeakingTimesOut);
                recorder.Record($"{prefix}Verbal.Pair", verbal.PairOut);
                recorder.Record($"{prefix}Verbal.Group", verbal.GroupOut);
                recorder.Record($"{prefix}Verbal.EqualityUsable", verbal.EqualityUsableOut);
            }

            EqualityIndexComponent speechEquality = indices.Find<EqualityIndicator>(IndexNames.SpeechEquality)?.Component;
            if (speechEquality != null)
            {
                recorder.Record($"{prefix}SpeechEquality.Group", speechEquality.Out);
                recorder.Record($"{prefix}SpeechEquality.Pair", speechEquality.PairOut);
                recorder.Record($"{prefix}SpeechEquality.Subset", speechEquality.SubsetOut);
            }

            DominanceIdentityComponent talkingMost = indices.Find<DominanceIndicator>(IndexNames.TalkingMost)?.Component;
            if (talkingMost != null)
            {
                recorder.Record($"{prefix}TalkingMost.Group", talkingMost.Out);
                recorder.Record($"{prefix}TalkingMost.Pair", talkingMost.PairOut);
                recorder.Record($"{prefix}TalkingMost.HasLeader", talkingMost.HasLeaderOut);
            }

            if (indices.CollaborationScore != null)
            {
                recorder.Record($"{prefix}Score.Global", indices.CollaborationScore.Out);
                recorder.Record($"{prefix}Score.Dimensions", indices.CollaborationScore.DimensionsOut);
                recorder.Record($"{prefix}Score.Indices", indices.CollaborationScore.NormalizedIndicesOut);
            }
        }

        /// <summary>Adds the rows of a CSV export to a recording, one sample per row.</summary>
        /// <param name="recorder">The recording.</param>
        /// <param name="csv">The export, once the pipeline has completed.</param>
        /// <param name="prefix">Prefix of the stream name.</param>
        public static void RecordCsv(StreamRecorder recorder, StringWriter csv, string prefix = "")
        {
            int index = 0;
            foreach (string line in Rows(csv))
            {
                recorder.Add($"{prefix}Csv", new DateTime(index++, DateTimeKind.Utc), line);
            }
        }

        /// <summary>Rows of a CSV export, header included.</summary>
        /// <param name="csv">The export.</param>
        /// <returns>The rows.</returns>
        public static List<string> Rows(StringWriter csv)
            => csv.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

        /// <summary>Path of a reference recording.</summary>
        /// <param name="scenario">Name of the scenario.</param>
        /// <returns>The path of the file, next to the test assembly.</returns>
        public static string GoldenPath(string scenario)
            => Path.Combine(Path.GetDirectoryName(typeof(IndicesHarness).Assembly.Location), "Golden", scenario + ".txt");

        /// <summary>Streams of a recording that come from the indicators themselves.</summary>
        /// <param name="recorder">The recording.</param>
        /// <returns>Every stream but the score and the export, which merge several indicators.</returns>
        public static IEnumerable<string> IndicatorStreams(StreamRecorder recorder)
            => recorder.Keys.Where(key => !key.Contains("Score.") && !key.EndsWith("Csv")).ToList();

        /// <summary>
        /// Value of a keyed stream at a tick, or the last one published before it: an index
        /// that skips a tick keeps its last value in the score and in the export.
        /// </summary>
        /// <param name="recorder">The recording.</param>
        /// <param name="key">Name of the stream.</param>
        /// <param name="tick">The tick.</param>
        /// <returns>The payload, or null when nothing was published yet.</returns>
        public static string LatestAt(StreamRecorder recorder, string key, DateTime tick)
        {
            string latest = null;
            foreach (StreamRecorder.Sample sample in recorder.Get(key))
            {
                if (sample.Time > tick)
                {
                    break;
                }

                latest = sample.Payload;
            }

            return latest;
        }
    }
}
