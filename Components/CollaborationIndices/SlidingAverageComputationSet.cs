using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Several windows of the same indices, computed side by side.
    ///
    /// The study runs the indices on 20 s, 30 s and 45 s windows at the same time. The legacy
    /// code did that by declaring three SlidingAverageSpeech instances, three configurations,
    /// three writers, and then dispatching everything with `switch (sa.speechConfiguration.threshold)`
    /// at every connection point. This class holds the instances in a dictionary keyed by window,
    /// so the dispatch becomes an indexer and adding a fourth window costs one line.
    ///
    /// Three things this class enforces, each of which is a silent bug when done by hand:
    ///  1. a distinct component name per instance, otherwise the emitter names collide and the
    ///     stores of the second instance overwrite those of the first;
    ///  2. an independent configuration object per instance, because Enabled is written at
    ///     runtime by the phase gate;
    ///  3. a calibration matching the window, because a P95 measured on 20 s does not apply
    ///     to a 45 s window.
    /// </summary>
    public class SlidingAverageComputationSet
    {
        private readonly Dictionary<TimeSpan, SlidingAverageComputation> instances = new Dictionary<TimeSpan, SlidingAverageComputation>();

        /// <summary>
        /// Creates one instance per window.
        /// </summary>
        /// <param name="pipeline">Pipeline or subpipeline hosting the components.</param>
        /// <param name="template">
        /// Configuration shared by every instance. It is cloned, and its WindowDuration and
        /// Calibration are overridden per window; the template value of those two is ignored.
        /// </param>
        /// <param name="windows">Window durations and the writer of each one.</param>
        /// <param name="namePrefix">Prefix of the component names.</param>
        public SlidingAverageComputationSet(
            Pipeline pipeline,
            SlidingAverageConfiguration template,
            IEnumerable<KeyValuePair<TimeSpan, TextWriter>> windows,
            string namePrefix = "SlidingAverage")
        {
            foreach (var window in windows)
            {
                if (this.instances.ContainsKey(window.Key))
                {
                    throw new ArgumentException($"The window {window.Key} is declared twice.", nameof(windows));
                }

                SlidingAverageConfiguration configuration = template.Clone();
                configuration.WindowDuration = window.Key;
                configuration.Calibration = IndexCalibration.ForWindow(window.Key);
                configuration.IndicesWriter = window.Value;

                // The clocks are shared by the whole set: two generators of the same period
                // would drift apart and produce rows that cannot be aligned between windows.
                configuration.UseInternalClock = false;

                string name = $"{namePrefix}_{(int)window.Key.TotalSeconds}s";
                this.instances[window.Key] = new SlidingAverageComputation(pipeline, configuration, name);
            }
        }

        /// <summary>Instance of one window.</summary>
        public SlidingAverageComputation this[TimeSpan window]
        {
            get
            {
                if (!this.instances.TryGetValue(window, out var instance))
                {
                    throw new ArgumentException($"No instance declared for the window {window}.", nameof(window));
                }

                return instance;
            }
        }

        /// <summary>Instance of one window, in seconds, for readability at the call site.</summary>
        public SlidingAverageComputation OfSeconds(double seconds) => this[TimeSpan.FromSeconds(seconds)];

        public IEnumerable<TimeSpan> Windows => this.instances.Keys;

        public IEnumerable<SlidingAverageComputation> All => this.instances.Values;

        /// <summary>
        /// Connects the shared clocks. The \psi Timers produce a TimeSpan, the components expect
        /// a tick, hence the conversion.
        /// </summary>
        public void ConnectClocks(IProducer<TimeSpan> indexClock, IProducer<TimeSpan> attentionClock = null)
        {
            IProducer<bool> tick = indexClock.Select(_ => true);
            IProducer<bool> attentionTick = attentionClock?.Select(_ => true);

            foreach (SlidingAverageComputation instance in this.All)
            {
                tick.PipeTo(instance.ClockIn);
                attentionTick?.PipeTo(instance.AttentionClockIn);
            }
        }

        /// <summary>Connects the phase boundaries to every instance.</summary>
        public void ConnectPhases(IProducer<bool> phaseStart, IProducer<bool> phaseEnd)
        {
            foreach (SlidingAverageComputation instance in this.All)
            {
                phaseStart?.PipeTo(instance.Gate.PhaseStartIn);
                phaseEnd?.PipeTo(instance.Gate.PhaseEndIn);
            }
        }

        /// <summary>
        /// Applies the same wiring to every instance. Use it for the raw source streams, which
        /// are shared: a \psi stream can feed any number of receivers.
        /// </summary>
        public void ForEach(Action<SlidingAverageComputation> action)
        {
            foreach (SlidingAverageComputation instance in this.All)
            {
                action(instance);
            }
        }

        /// <summary>
        /// Same, with the window available, for the connections that differ per window
        /// (a downstream consumer declared per window, a store suffix).
        /// </summary>
        public void ForEach(Action<TimeSpan, SlidingAverageComputation> action)
        {
            foreach (var entry in this.instances)
            {
                action(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Pairs each instance with the matching element of a per window list, replacing the
        /// `switch (threshold) { case 20000: ... }` blocks of the legacy pipeline.
        /// </summary>
        public void PairWith<T>(IReadOnlyDictionary<TimeSpan, T> consumers, Action<SlidingAverageComputation, T> action)
        {
            foreach (var entry in this.instances)
            {
                if (consumers.TryGetValue(entry.Key, out T consumer))
                {
                    action(entry.Value, consumer);
                }
            }
        }

        /// <summary>
        /// Suffix identifying the window in a store or a file name: "_20", "_30", "_45".
        /// Same convention as the legacy thresholdType.
        /// </summary>
        public static string StoreSuffix(TimeSpan window) => $"_{(int)window.TotalSeconds}";
    }
}
