// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.

// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.

// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.



using System;

using System.Collections.Generic;

using System.Linq;

using Microsoft.Psi;

using Microsoft.Psi.Data;


namespace CasperDatasetEventsViewer

{

    internal static class Program

    {

        private const string DefaultDatasetFilePath = @"C:\Users\alexi\Documents\ExpCASPER\Casper5.pds";

        private const string DefaultSessionName = "PreTest.009";

        private const string DefaultStoreName = "*";

        private const string DefaultStreamName = "*";

        private const string EventsStoreName = "Events";

        // Change this value to the sliding window you're working with.
        // CLI arg [slidingWindowMs] (5th argument) still overrides it.
        private static double DefaultSlidingWindowMs = 1500;

        private const int MaxCoOccurrencesPrinted = 50;



        private static int Main(string[] args)

        {

            string datasetFilePath = args.Length > 0 ? args[0] : DefaultDatasetFilePath;

            string sessionName = args.Length > 1 ? args[1] : DefaultSessionName;

            string storeName = args.Length > 2 ? args[2] : DefaultStoreName;

            string streamName = args.Length > 3 ? args[3] : DefaultStreamName;



            if (!TryParseSlidingWindowMs(args, out double slidingWindowMs, out string? parseError))

            {

                Console.Error.WriteLine(parseError);

                return 5;

            }



            TimeSpan slidingWindow = TimeSpan.FromMilliseconds(slidingWindowMs);



            Console.WriteLine($"Dataset: {datasetFilePath}");

            Console.WriteLine($"Session: {sessionName}");

            Console.WriteLine($"Stream filter: store='{storeName}', name='{streamName}' ('*' = all)");

            Console.WriteLine($"Sliding window: {slidingWindowMs} ms ({slidingWindow.TotalSeconds:0.###} s)");

            Console.WriteLine();



            if (!System.IO.File.Exists(datasetFilePath))

            {

                Console.Error.WriteLine("Dataset file not found.");

                PrintUsage();

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



                PrintStreamSummaries(session, storeName, streamName);



                Console.WriteLine();

                AnalyzeAddModuleBatteryFireCoOccurrence(session, EventsStoreName, slidingWindow);



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



        private static void PrintUsage()

        {

            Console.Error.WriteLine(

                "Usage: CasperDatasetEventsViewer <dataset.pds> [sessionName] [storeName|*] [streamName|*] [slidingWindowMs]");

            Console.Error.WriteLine(

                $"  slidingWindowMs: max span for events in the same window (default {DefaultSlidingWindowMs} ms).");

        }



        private static bool TryParseSlidingWindowMs(string[] args, out double slidingWindowMs, out string? error)

        {

            slidingWindowMs = DefaultSlidingWindowMs;

            error = null;



            if (args.Length <= 4)

            {

                return true;

            }



            if (!double.TryParse(args[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out slidingWindowMs)

                || slidingWindowMs < 0)

            {

                error = $"Invalid sliding window '{args[4]}'. Expected a non-negative number of milliseconds.";

                return false;

            }



            return true;

        }



        private static void PrintStreamSummaries(Session session, string storeNameFilter, string streamNameFilter)

        {

            bool matchAllStores = storeNameFilter == "*" || string.IsNullOrWhiteSpace(storeNameFilter);

            bool matchAllStreams = streamNameFilter == "*" || string.IsNullOrWhiteSpace(streamNameFilter);



            var streams = session.Partitions

                .SelectMany(p => p.AvailableStreams)

                .Where(sm => matchAllStores || sm.StoreName.Equals(storeNameFilter, StringComparison.OrdinalIgnoreCase))

                .Where(sm => matchAllStreams || sm.Name.Equals(streamNameFilter, StringComparison.OrdinalIgnoreCase));



            var grouped = streams

                .GroupBy(sm => new { sm.StoreName, sm.Name, sm.TypeName })

                .Select(g => new

                {

                    g.Key.StoreName,

                    g.Key.Name,

                    g.Key.TypeName,

                    MessageCount = g.Sum(s => (long)s.MessageCount),

                    Partitions = g.Count(),

                })

                .OrderBy(x => x.StoreName)

                .ThenBy(x => x.Name)

                .ThenBy(x => x.TypeName)

                .ToList();



            if (grouped.Count == 0)

            {

                Console.Error.WriteLine("No streams matched the provided filters.");

                Console.Error.WriteLine();

                Console.Error.WriteLine("Available stores:");

                foreach (var st in session.Partitions.SelectMany(p => p.AvailableStreams).Select(sm => sm.StoreName).Distinct().OrderBy(s => s))

                {

                    Console.Error.WriteLine($"- {st}");

                }



                return;

            }



            int storeWidth = Math.Max(5, grouped.Max(x => x.StoreName?.Length ?? 0));

            int nameWidth = Math.Max(6, grouped.Max(x => x.Name?.Length ?? 0));

            int countWidth = Math.Max(8, grouped.Max(x => x.MessageCount.ToString().Length));



            Console.WriteLine($"Streams found: {grouped.Count}");

            Console.WriteLine();

            Console.WriteLine(

                $"{PadRight("Store", storeWidth)}  " +

                $"{PadRight("Stream", nameWidth)}  " +

                $"{PadLeft("Messages", countWidth)}  " +

                $"Type");

            Console.WriteLine(

                $"{new string('-', storeWidth)}  " +

                $"{new string('-', nameWidth)}  " +

                $"{new string('-', countWidth)}  " +

                $"{new string('-', 4)}");



            foreach (var s in grouped)

            {

                string typeSuffix = s.Partitions > 1 ? $" (x{s.Partitions} partitions)" : string.Empty;

                Console.WriteLine(

                    $"{PadRight(s.StoreName, storeWidth)}  " +

                    $"{PadRight(s.Name, nameWidth)}  " +

                    $"{PadLeft(s.MessageCount.ToString(), countWidth)}  " +

                    $"{s.TypeName}{typeSuffix}");

            }

        }



        private static void AnalyzeAddModuleBatteryFireCoOccurrence(Session session, string storeName, TimeSpan window)

        {

            Console.WriteLine("=== Sliding window: AddModule + BatteryFire ===");

            Console.WriteLine($"Store: {storeName}");

            Console.WriteLine($"Window: {window.TotalMilliseconds} ms (events co-occur when |t1 - t2| <= window)");

            Console.WriteLine();



            Pipeline? pipeline = null;

            try

            {

                pipeline = Pipeline.Create("CasperDatasetEventsViewer", enableDiagnostics: false);

                SessionImporter importer = SessionImporter.Open(pipeline, session);



                IProducer<object>? addModule = OpenStream(importer, storeName, "AddModule");

                IProducer<object>? batteryFire = OpenStream(importer, storeName, "BatteryFire");



                if (addModule is null)

                {

                    Console.Error.WriteLine($"Stream 'AddModule' not found in store '{storeName}'.");

                    return;

                }



                if (batteryFire is null)

                {

                    Console.Error.WriteLine($"Stream 'BatteryFire' not found in store '{storeName}'.");

                    return;

                }



                var results = new AddModuleBatteryFireAnalyzer.Results();

                AddModuleBatteryFireAnalyzer.Configure(pipeline, addModule, batteryFire, window, results);



                Console.WriteLine("Replaying AddModule and BatteryFire (Psi pipeline)...");

                pipeline.Run(ReplayDescriptor.ReplayAll);



                AddModuleBatteryFireAnalyzer.FinalizeNearestGap(window, results);

                AddModuleBatteryFireAnalyzer.PrintResults(results, window, MaxCoOccurrencesPrinted);

            }

            finally

            {

                pipeline?.Dispose();

            }

        }



        private static string PadRight(string? value, int width) => (value ?? string.Empty).PadRight(width);

        private static string PadLeft(string? value, int width) => (value ?? string.Empty).PadLeft(width);



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

                        return dynamicStream.Select(d => (object)d);

                    }

                }

                catch

                {

                    // try next partition

                }

            }



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



        private static bool PartitionMatchesStore(string partitionKey, IEnumerable<IStreamMetadata> streams, string storeName)

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
