// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Linq;
using Microsoft.Psi;
using Microsoft.Psi.Data;

namespace CasperDatasetEventsViewer
{
    internal static class Program
    {
        private const string DefaultDatasetFilePath = @"E:\Exp CASPER\Casper5.pds";
        private const string DefaultSessionName = "PreTest.013";
        private const string DefaultStoreName = "Server";
        private const string DefaultStreamName = "BatteryFinish";

        private static int Main(string[] args)
        {
            string datasetFilePath = args.Length > 0 ? args[0] : DefaultDatasetFilePath;
            string sessionName = args.Length > 1 ? args[1] : DefaultSessionName;
            string storeName = args.Length > 2 ? args[2] : DefaultStoreName;
            string streamName = args.Length > 3 ? args[3] : DefaultStreamName;

            Console.WriteLine($"Dataset: {datasetFilePath}");
            Console.WriteLine($"Session: {sessionName}");
            Console.WriteLine($"Looking for stream: {storeName}/{streamName}");
            Console.WriteLine();

            if (!System.IO.File.Exists(datasetFilePath))
            {
                Console.Error.WriteLine("Dataset file not found.");
                Console.Error.WriteLine("Usage: CasperDatasetEventsViewer <dataset.pds> [sessionName] [storeName] [streamName]");
                return 4;
            }

            try
            {
                // Read-only: autoSave=false avoids locking the .pds for write (common cause of semaphore timeouts).
                Dataset dataset = Dataset.Load(datasetFilePath, autoSave: false);
                Session? session = dataset.Sessions.FirstOrDefault(s => s?.Name == sessionName);
                if (session is null)
                {
                    Console.Error.WriteLine($"Session '{sessionName}' not found. Available sessions:");
                    foreach (var s in dataset.Sessions.Where(s => s != null).Select(s => s!.Name).OrderBy(n => n))
                    {
                        Console.Error.WriteLine($"- {s}");
                    }

                    return 2;
                }

                IStreamMetadata? streamMeta = FindStreamMetadata(session, storeName, streamName);
                if (streamMeta is null)
                {
                    PrintStreamNotFound(session, storeName, streamName);
                    return 3;
                }

                Console.WriteLine("Match found:");
                Console.WriteLine($"- Store:     {streamMeta.StoreName}");
                Console.WriteLine($"- Stream:    {streamMeta.Name}");
                Console.WriteLine($"- TypeName:  {streamMeta.TypeName}");
                Console.WriteLine($"- Messages:  {streamMeta.MessageCount}");
                Console.WriteLine($"- Path:      {streamMeta.StorePath}");
                Console.WriteLine();

                ReplayAndPrint(session, storeName, streamName);
                return 0;
            }
            catch (System.IO.IOException ex) when (IsSemaphoreTimeout(ex))
            {
                Console.Error.WriteLine("IO error: semaphore timeout while accessing the dataset/store.");
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine("Typical causes:");
                Console.Error.WriteLine("- Another app holds the dataset open (Psi Studio, ServerApplication, a previous crashed run).");
                Console.Error.WriteLine("- The dataset is on a slow/network drive (E:). Try copying locally.");
                Console.Error.WriteLine("- Close other Psi tools and retry.");
                return 10;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static IStreamMetadata? FindStreamMetadata(Session session, string storeName, string streamName)
        {
            var exact = session.Partitions
                .SelectMany(p => p.AvailableStreams)
                .FirstOrDefault(sm => sm.Name == streamName && sm.StoreName == storeName);

            if (exact != null)
            {
                return exact;
            }

            // Prefer "Server" partition when stream exists in multiple stores.
            return session.Partitions
                .SelectMany(p => p.AvailableStreams)
                .Where(sm => sm.Name == streamName)
                .OrderBy(sm => sm.StoreName.Equals(storeName, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(sm => sm.StoreName.Equals("Server", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .FirstOrDefault();
        }

        private static void PrintStreamNotFound(Session session, string storeName, string streamName)
        {
            Console.Error.WriteLine($"No stream '{streamName}' in store '{storeName}'.");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Available stores:");
            foreach (var st in session.Partitions.SelectMany(p => p.AvailableStreams).Select(sm => sm.StoreName).Distinct().OrderBy(s => s))
            {
                Console.Error.WriteLine($"- {st}");
            }

            Console.Error.WriteLine();
            Console.Error.WriteLine($"Streams named '{streamName}' (any store):");
            foreach (var sm in session.Partitions.SelectMany(p => p.AvailableStreams).Where(sm => sm.Name == streamName))
            {
                Console.Error.WriteLine($"- {sm.StoreName} ({sm.TypeName})");
            }
        }

        private static void ReplayAndPrint(Session session, string storeName, string streamName)
        {
            Pipeline? pipeline = null;
            try
            {
                pipeline = Pipeline.Create("CasperDatasetEventsViewer", enableDiagnostics: false);
                SessionImporter importer = SessionImporter.Open(pipeline, session);

                IProducer<object>? stream = OpenStream(importer, storeName, streamName);
                if (stream is null)
                {
                    throw new InvalidOperationException($"Could not open stream '{streamName}' (store '{storeName}').");
                }

                long count = 0;
                stream.Do((message, envelope) =>
                {
                    count++;
                    Console.WriteLine($"{envelope.OriginatingTime:O}\t{message}");
                });

                Console.WriteLine("Replaying stream (read-only)...");
                pipeline.Run(ReplayDescriptor.ReplayAll);
                Console.WriteLine($"Replay done. {count} message(s) printed.");
            }
            finally
            {
                pipeline?.Dispose();
            }
        }

        private static IProducer<object>? OpenStream(SessionImporter importer, string storeName, string streamName)
        {
            foreach (var partition in importer.PartitionImporters)
            {
                if (!PartitionMatchesStore(partition.Key, partition.Value.AvailableStreams, storeName))
                {
                    continue;
                }

                if (!partition.Value.AvailableStreams.Any(s => s.Name == streamName))
                {
                    continue;
                }

                try
                {
                    var dynamicStream = partition.Value.OpenDynamicStream(streamName);
                    if (dynamicStream != null)
                    {
                        Console.WriteLine($"Opened via dynamic stream on partition '{partition.Key}'.");
                        return dynamicStream.Select(d => (object)d);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Partition '{partition.Key}': {ex.Message}");
                }
            }

            // Fallback: search any partition (wrong store name on CLI).
            foreach (var partition in importer.PartitionImporters)
            {
                if (!partition.Value.AvailableStreams.Any(s => s.Name == streamName))
                {
                    continue;
                }

                try
                {
                    var dynamicStream = partition.Value.OpenDynamicStream(streamName);
                    if (dynamicStream != null)
                    {
                        Console.WriteLine($"Opened via dynamic stream on partition '{partition.Key}' (store filter '{storeName}' did not match).");
                        return dynamicStream.Select(d => (object)d);
                    }
                }
                catch
                {
                    // try next partition
                }
            }

            try
            {
                return importer.OpenStream<object>(streamName);
            }
            catch
            {
                return null;
            }
        }

        private static bool PartitionMatchesStore(string partitionKey, System.Collections.Generic.IEnumerable<IStreamMetadata> streams, string storeName)
        {
            if (partitionKey.Equals(storeName, StringComparison.OrdinalIgnoreCase)
                || partitionKey.Contains(storeName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return streams.Any(s => s.StoreName.Equals(storeName, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSemaphoreTimeout(Exception ex)
        {
            return ex.Message.Contains("semaphore", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("sémaphore", StringComparison.OrdinalIgnoreCase);
        }
    }
}
