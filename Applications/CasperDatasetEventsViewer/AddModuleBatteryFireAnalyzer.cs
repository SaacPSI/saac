// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;
using Microsoft.Psi.Serialization;

namespace CasperDatasetEventsViewer
{
    /// <summary>
    /// Psi pipeline for AddModule / BatteryFire temporal analysis (join, window, merge).
    /// </summary>
    internal sealed class AddModuleBatteryFireAnalyzer
    {
        public sealed class Results
        {
            public List<EventPair> Pairs { get; } = new List<EventPair>();
            public List<CoOccurrenceWindow> SlidingWindows { get; } = new List<CoOccurrenceWindow>();
            public List<DateTime> AddModuleTimes { get; } = new List<DateTime>();
            public List<DateTime> BatteryFireTimes { get; } = new List<DateTime>();
            public EventPair? NearestPairOutsideWindow { get; set; }
        }

        public static void Configure(
            Pipeline pipeline,
            IProducer<object> addModule,
            IProducer<object> batteryFire,
            TimeSpan window,
            Results results)
        {
            RegisterSerializers();

            var addModuleTimes = addModule.Select((_, envelope) => envelope.OriginatingTime, nameof(addModuleTimes));
            var batteryFireTimes = batteryFire.Select((_, envelope) => envelope.OriginatingTime, nameof(batteryFireTimes));

            addModuleTimes.Do(t => results.AddModuleTimes.Add(t));
            batteryFireTimes.Do(t => results.BatteryFireTimes.Add(t));

            // Co-occurrence: |t_add - t_fire| <= window (symmetric join tolerance).
            var matchInterval = new RelativeTimeInterval(-window, window);
            addModuleTimes
                .Join(batteryFireTimes, matchInterval, (tA, tB) => new EventPair(tA, tB, AbsDelta(tA, tB)))
                .Do(pair => results.Pairs.Add(pair));

            // Sliding windows (span <= window) that contain at least one of each event type.
            var labeledAdd = addModuleTimes.Select(t => new LabeledTime(t, isAddModule: true));
            var labeledFire = batteryFireTimes.Select(t => new LabeledTime(t, isAddModule: false));
            var merged = labeledAdd.Merge(labeledFire);

            merged
                .Window(RelativeTimeInterval.Past(window))
                .Where(HasBothEventTypes)
                .Do(window => results.SlidingWindows.Add(ToCoOccurrenceWindow(window)));
        }

        public static void FinalizeNearestGap(TimeSpan window, Results results)
        {
            if (results.Pairs.Count > 0 || results.AddModuleTimes.Count == 0 || results.BatteryFireTimes.Count == 0)
            {
                return;
            }

            results.AddModuleTimes.Sort();
            results.BatteryFireTimes.Sort();

            TimeSpan nearest = TimeSpan.MaxValue;
            DateTime nearestA = default;
            DateTime nearestB = default;

            int j = 0;
            foreach (var tA in results.AddModuleTimes)
            {
                while (j < results.BatteryFireTimes.Count && results.BatteryFireTimes[j] < tA)
                {
                    j++;
                }

                if (j < results.BatteryFireTimes.Count)
                {
                    var delta = AbsDelta(tA, results.BatteryFireTimes[j]);
                    if (delta < nearest)
                    {
                        nearest = delta;
                        nearestA = tA;
                        nearestB = results.BatteryFireTimes[j];
                    }
                }

                if (j > 0)
                {
                    var delta = AbsDelta(tA, results.BatteryFireTimes[j - 1]);
                    if (delta < nearest)
                    {
                        nearest = delta;
                        nearestA = tA;
                        nearestB = results.BatteryFireTimes[j - 1];
                    }
                }
            }

            if (nearest < TimeSpan.MaxValue)
            {
                results.NearestPairOutsideWindow = new EventPair(nearestA, nearestB, nearest);
            }
        }

        public static void PrintResults(Results results, TimeSpan window, int maxPrinted)
        {
            Console.WriteLine($"AddModule messages:   {results.AddModuleTimes.Count}");
            Console.WriteLine($"BatteryFire messages: {results.BatteryFireTimes.Count}");
            Console.WriteLine();

            if (results.AddModuleTimes.Count == 0 || results.BatteryFireTimes.Count == 0)
            {
                Console.WriteLine("Cannot evaluate co-occurrence: at least one stream has no messages.");
                return;
            }

            var distinctAddModule = results.Pairs.Select(p => p.TimeA).Distinct().Count();
            var distinctBatteryFire = results.Pairs.Select(p => p.TimeB).Distinct().Count();

            Console.WriteLine($"Co-occurring pairs (AddModule, BatteryFire): {results.Pairs.Count}");
            Console.WriteLine($"  AddModule events with a match:   {distinctAddModule} / {results.AddModuleTimes.Count}");
            Console.WriteLine($"  BatteryFire events with a match: {distinctBatteryFire} / {results.BatteryFireTimes.Count}");
            Console.WriteLine();

            if (results.Pairs.Count == 0)
            {
                Console.WriteLine("No AddModule event falls in the same sliding window as a BatteryFire event.");
                if (results.NearestPairOutsideWindow is EventPair nearest)
                {
                    Console.WriteLine();
                    Console.WriteLine(
                        $"Closest pair (outside window): {nearest.TimeA:O} / {nearest.TimeB:O}  (|delta| = {nearest.Delta.TotalMilliseconds:0.###} ms)");
                }

                return;
            }

            Console.WriteLine("Matches (AddModule time, BatteryFire time, |delta|):");
            PrintLimited(
                results.Pairs,
                maxPrinted,
                pair => $"  {pair.TimeA:O}  |  {pair.TimeB:O}  |  {pair.Delta.TotalMilliseconds,8:0.###} ms");

            Console.WriteLine();
            Console.WriteLine($"Sliding windows containing both event types: {results.SlidingWindows.Count}");
            PrintLimited(
                results.SlidingWindows,
                maxPrinted,
                w => $"  [{w.WindowStart:O} .. {w.WindowEnd:O}]  span {w.Span.TotalMilliseconds:0.###} ms  " +
                     $"(AddModule x{w.AddModuleCount}, BatteryFire x{w.BatteryFireCount})");
        }

        private static void RegisterSerializers()
        {
            KnownSerializers.Default.Register<WeakReference>(CloningFlags.CloneIntPtrFields);
        }

        private static bool HasBothEventTypes(IEnumerable<LabeledTime> window)
        {
            bool hasAdd = false;
            bool hasFire = false;
            foreach (var item in window)
            {
                if (item.IsAddModule)
                {
                    hasAdd = true;
                }
                else
                {
                    hasFire = true;
                }

                if (hasAdd && hasFire)
                {
                    return true;
                }
            }

            return false;
        }

        private static CoOccurrenceWindow ToCoOccurrenceWindow(IEnumerable<LabeledTime> window)
        {
            DateTime start = DateTime.MaxValue;
            DateTime end = DateTime.MinValue;
            int addCount = 0;
            int fireCount = 0;

            foreach (var item in window)
            {
                if (item.Time < start)
                {
                    start = item.Time;
                }

                if (item.Time > end)
                {
                    end = item.Time;
                }

                if (item.IsAddModule)
                {
                    addCount++;
                }
                else
                {
                    fireCount++;
                }
            }

            return new CoOccurrenceWindow(start, end, end - start, addCount, fireCount);
        }

        private static void PrintLimited<T>(IReadOnlyList<T> items, int maxPrinted, Func<T, string> format)
        {
            int printed = 0;
            foreach (var item in items)
            {
                if (printed >= maxPrinted)
                {
                    Console.WriteLine($"... ({items.Count - printed} more not shown)");
                    break;
                }

                Console.WriteLine(format(item));
                printed++;
            }
        }

        private static TimeSpan AbsDelta(DateTime a, DateTime b) => a >= b ? a - b : b - a;
    }

    internal readonly struct LabeledTime
    {
        public LabeledTime(DateTime time, bool isAddModule)
        {
            Time = time;
            IsAddModule = isAddModule;
        }

        public DateTime Time { get; }
        public bool IsAddModule { get; }
    }

    internal readonly struct EventPair
    {
        public EventPair(DateTime timeA, DateTime timeB, TimeSpan delta)
        {
            TimeA = timeA;
            TimeB = timeB;
            Delta = delta;
        }

        public DateTime TimeA { get; }
        public DateTime TimeB { get; }
        public TimeSpan Delta { get; }
    }

    internal readonly struct CoOccurrenceWindow
    {
        public CoOccurrenceWindow(DateTime windowStart, DateTime windowEnd, TimeSpan span, int addModuleCount, int batteryFireCount)
        {
            WindowStart = windowStart;
            WindowEnd = windowEnd;
            Span = span;
            AddModuleCount = addModuleCount;
            BatteryFireCount = batteryFireCount;
        }

        public DateTime WindowStart { get; }
        public DateTime WindowEnd { get; }
        public TimeSpan Span { get; }
        public int AddModuleCount { get; }
        public int BatteryFireCount { get; }
    }
}
