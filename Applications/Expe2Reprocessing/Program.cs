// <copyright file="Program.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

namespace Expe2Reprocessing
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi;
    using SAAC.CollaborationIndices;

    /// <summary>
    /// Computes the collaboration indices and profiles of one recorded session of the puzzle task.
    ///
    ///   Expe2Reprocessing datasetFolder outputFolder [--realtime]   the default configuration
    ///   Expe2Reprocessing --config session.json                     a configuration file
    ///   Expe2Reprocessing --template session.json                   writes the default configuration, to edit
    ///
    /// The configuration says for whom, on which windows, which indicators, with or without
    /// scores and profiles (CollaborationSessionConfiguration; expe2.json next to the program
    /// is the default one). The dataset is only read. The results are written in the output
    /// folder, one CSV file per window, with the configuration that produced them.
    ///
    /// By default the session is replayed as fast as possible, on one thread so that two runs
    /// give the same files; RealTime replays it at its original pace.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                CollaborationSessionConfiguration configuration = ReadArguments(args);
                if (configuration == null)
                {
                    return 0;
                }

                return Run(configuration);
            }
            catch (CollaborationIndicesConfigurationException exception)
            {
                // Every problem of a configuration is reported at once.
                Console.Error.WriteLine(exception.Message);
                return 2;
            }
            catch (ArgumentException exception)
            {
                Console.Error.WriteLine(exception.Message);
                Console.Error.WriteLine("Usage: Expe2Reprocessing datasetFolder outputFolder [--realtime] | --config file.json | --template file.json");
                return 1;
            }
        }

        /// <summary>
        /// The configuration of the puzzle task: three participants, the three windows, every
        /// indicator, the indices restricted to the puzzles, scores and profiles.
        /// </summary>
        private static CollaborationSessionConfiguration DefaultConfiguration()
        {
            CollaborationSessionConfiguration configuration = CollaborationSessionConfiguration.Template(participantCount: 3);
            configuration.RequirePhase = true;
            configuration.Detectors.HeadForwardAxis = SessionStreams.HeadForwardAxis;

            // A gazed object is identified by its kind ("SM_angle(Clone)"), not by its instance:
            // two participants looking at two pieces of the same kind would count as a joint
            // look. Requiring the heads to point to the same place is what tells them apart.
            configuration.Detectors.RequireConvergingHeads = true;
            return configuration;
        }

        private static CollaborationSessionConfiguration ReadArguments(string[] args)
        {
            string[] values = args.Where(argument => !argument.StartsWith("--")).ToArray();

            if (args.Contains("--template"))
            {
                string path = values.Length > 0 ? values[0] : throw new ArgumentException("--template needs the path of the file to write.");
                DefaultConfiguration().Save(path);
                Console.WriteLine($"Configuration template written to {Path.GetFullPath(path)}");
                return null;
            }

            if (args.Contains("--config"))
            {
                string path = values.Length > 0 ? values[0] : throw new ArgumentException("--config needs the path of the configuration file.");
                return CollaborationSessionConfiguration.Load(path);
            }

            if (values.Length < 2)
            {
                throw new ArgumentException("The dataset folder and the output folder are required.");
            }

            CollaborationSessionConfiguration configuration = DefaultConfiguration();
            configuration.DatasetPath = values[0];
            configuration.OutputFolder = values[1];
            configuration.RealTime = args.Contains("--realtime");
            return configuration;
        }

        private static int Run(CollaborationSessionConfiguration configuration)
        {
            configuration.DatasetPath = Path.GetFullPath(configuration.DatasetPath);
            configuration.OutputFolder = Path.GetFullPath(configuration.OutputFolder);

            if (!Directory.Exists(configuration.DatasetPath))
            {
                throw new ArgumentException($"Dataset not found: {configuration.DatasetPath}");
            }

            if (IsInside(configuration.OutputFolder, configuration.DatasetPath))
            {
                throw new ArgumentException("The output folder must not be inside the dataset: the dataset is only read.");
            }

            Directory.CreateDirectory(configuration.OutputFolder);

            // What produced the files, kept next to them; it can be given back with --config.
            configuration.Save(Path.Combine(configuration.OutputFolder, "configuration.json"));

            using (Pipeline pipeline = Pipeline.Create("Expe2Reprocessing", threadCount: configuration.RealTime ? 0 : 1))
            {
                SessionStreams streams = SessionStreams.Open(pipeline, configuration.DatasetPath, configuration.ParticipantIds);
                var collaboration = new CollaborationPipeline(pipeline, streams, configuration);
                collaboration.Build();

                int handovers = 0;
                collaboration.Handovers.Do(_ => handovers++);

                try
                {
                    pipeline.Run(configuration.RealTime ? ReplayDescriptor.ReplayAllRealTime : ReplayDescriptor.ReplayAll);
                }
                finally
                {
                    collaboration.Close();
                }

                Console.WriteLine($"Object handovers detected: {handovers}");
                foreach (string file in collaboration.OutputFiles)
                {
                    Console.WriteLine($"{file}: {File.ReadLines(file).Count() - 1} rows");
                }
            }

            return 0;
        }

        private static bool IsInside(string folder, string parent)
        {
            string normalizedParent = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string normalizedFolder = folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return normalizedFolder.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
        }
    }
}
