using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class PhaseGateConfiguration
    {
        /// <summary>
        /// After the beginning of a phase, the indices are meaningless until the window is
        /// filled with data of that phase. The gate stays closed for this duration.
        /// Set it to the window duration of the indicators.
        /// </summary>
        public TimeSpan WarmUpDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>If true, the gate closes as soon as a phase ends and reopens at the next start.</summary>
        public bool CloseBetweenPhases { get; set; } = true;

        /// <summary>Period of the tick published while the gate is open.</summary>
        public TimeSpan TickInterval { get; set; } = TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// Session and phase management. It replaces the sequence of early returns that opened the
    /// legacy ReceiveTimer: task not started, gap between two puzzles, window not yet filled
    /// since the phase start, phase ended.
    ///
    /// Inputs:
    ///  - PhaseStartIn / PhaseEndIn: boundaries of a phase (puzzle, exercise, session);
    ///  - ClockIn: the raw clock, typically Generators.Repeat(pipeline, true, TickInterval).
    ///
    /// Outputs:
    ///  - Out: the gated tick, to be connected to the TickIn of every indicator component;
    ///  - EnabledOut: the state of the gate, to be connected to their EnableIn;
    ///  - PhaseIdOut: index of the current phase.
    /// </summary>
    public class PhaseGateComponent : IProducer<bool>
    {
        private readonly PhaseGateConfiguration configuration;
        private readonly string name;

        private DateTime phaseStart = DateTime.MaxValue;
        private bool phaseRunning;
        private int phaseId = -1;
        private bool lastEnabled;

        public PhaseGateComponent(Pipeline pipeline, PhaseGateConfiguration configuration, string name = nameof(PhaseGateComponent))
        {
            this.configuration = configuration ?? new PhaseGateConfiguration();
            this.name = name;

            this.PhaseStartIn = pipeline.CreateReceiver<bool>(this, this.ReceivePhaseStart, $"{name}-PhaseStart");
            this.PhaseEndIn = pipeline.CreateReceiver<bool>(this, this.ReceivePhaseEnd, $"{name}-PhaseEnd");
            this.ClockIn = pipeline.CreateReceiver<bool>(this, this.ReceiveClock, $"{name}-Clock");

            this.Out = pipeline.CreateEmitter<bool>(this, $"{name}-Tick");
            this.EnabledOut = pipeline.CreateEmitter<bool>(this, $"{name}-Enabled");
            this.PhaseIdOut = pipeline.CreateEmitter<int>(this, $"{name}-PhaseId");
        }

        public Receiver<bool> PhaseStartIn { get; }

        public Receiver<bool> PhaseEndIn { get; }

        public Receiver<bool> ClockIn { get; }

        public Emitter<bool> Out { get; }

        public Emitter<bool> EnabledOut { get; }

        public Emitter<int> PhaseIdOut { get; }

        private void ReceivePhaseStart(bool value, Envelope envelope)
        {
            this.phaseStart = envelope.OriginatingTime;
            this.phaseRunning = true;
            this.phaseId++;
            this.PhaseIdOut.Post(this.phaseId, envelope.OriginatingTime);
        }

        private void ReceivePhaseEnd(bool value, Envelope envelope)
        {
            if (this.configuration.CloseBetweenPhases)
            {
                this.phaseRunning = false;
            }
        }

        private void ReceiveClock(bool value, Envelope envelope)
        {
            bool enabled = this.phaseRunning
                && this.phaseStart != DateTime.MaxValue
                && (envelope.OriginatingTime - this.phaseStart) >= this.configuration.WarmUpDuration;

            if (enabled != this.lastEnabled)
            {
                this.EnabledOut.Post(enabled, envelope.OriginatingTime);
                this.lastEnabled = enabled;
            }

            if (enabled)
            {
                this.Out.Post(true, envelope.OriginatingTime);
            }
        }
    }

    public class IndexExportConfiguration
    {
        /// <summary>Destination of the rows. The component does not own it and does not close it.</summary>
        public TextWriter Writer { get; set; }

        /// <summary>Ordered list of the columns, i.e. of the index names.</summary>
        public List<string> Columns { get; set; } = new List<string>();

        public string Separator { get; set; } = ";";

        /// <summary>If true, a header line is written on the first row.</summary>
        public bool WriteHeader { get; set; } = true;

        /// <summary>Value written when an index has not been received yet.</summary>
        public string MissingValue { get; set; } = "NA";

        /// <summary>Number format. The invariant culture avoids the decimal comma.</summary>
        public string Format { get; set; } = "0.######";
    }

    /// <summary>
    /// Writes one row per computation with every index of the session, in a stable column order.
    ///
    /// The legacy version built a single interpolated string of about sixty fields, which had to
    /// be edited by hand every time an index was added and produced silent column shifts. Here a
    /// column is declared by name and fed by its own receiver.
    ///
    /// Usage:
    ///   verbalEquality.Out.PipeTo(export.GetColumnInput("SpeechEquality"));
    ///   export.TickIn is connected to the same gated clock as the indicators.
    /// </summary>
    public class IndexExportComponent
    {
        private readonly IndexExportConfiguration configuration;
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly Dictionary<string, string> values = new Dictionary<string, string>();

        private bool headerWritten;
        private DateTime lastWrite = DateTime.MinValue;

        public IndexExportComponent(Pipeline pipeline, IndexExportConfiguration configuration, string name = nameof(IndexExportComponent))
        {
            this.pipeline = pipeline;
            this.configuration = configuration;
            this.name = name;
            this.TickIn = pipeline.CreateReceiver<bool>(this, (_, envelope) => this.WriteRow(envelope.OriginatingTime), $"{name}-Tick");
        }

        public Receiver<bool> TickIn { get; }

        /// <summary>Receiver feeding one numeric column.</summary>
        public Receiver<double> GetColumnInput(string columnName)
        {
            string key = columnName;
            return this.pipeline.CreateReceiver<double>(
                this,
                (value, _) => this.values[key] = value.ToString(this.configuration.Format, CultureInfo.InvariantCulture),
                $"{this.name}-Column-{key}");
        }

        /// <summary>Receiver feeding several columns at once, one per participant.</summary>
        public Receiver<Dictionary<uint, double>> GetParticipantColumnsInput(string columnPrefix)
        {
            string prefix = columnPrefix;
            return this.pipeline.CreateReceiver<Dictionary<uint, double>>(
                this,
                (dictionary, _) =>
                {
                    foreach (var entry in dictionary)
                    {
                        this.values[$"{prefix}_{entry.Key}"] = entry.Value.ToString(this.configuration.Format, CultureInfo.InvariantCulture);
                    }
                },
                $"{this.name}-Columns-{prefix}");
        }

        /// <summary>Receiver feeding several columns at once, one per pair.</summary>
        public Receiver<Dictionary<ParticipantPair, double>> GetPairColumnsInput(string columnPrefix)
        {
            string prefix = columnPrefix;
            return this.pipeline.CreateReceiver<Dictionary<ParticipantPair, double>>(
                this,
                (dictionary, _) =>
                {
                    foreach (var entry in dictionary)
                    {
                        this.values[$"{prefix}_{entry.Key}"] = entry.Value.ToString(this.configuration.Format, CultureInfo.InvariantCulture);
                    }
                },
                $"{this.name}-PairColumns-{prefix}");
        }

        /// <summary>
        /// Declares the columns generated by a per participant or per pair input, so that the
        /// header and the row order stay stable even before the first message arrives.
        /// </summary>
        public void DeclareColumns(IEnumerable<string> columnNames)
        {
            foreach (string columnName in columnNames)
            {
                if (!this.configuration.Columns.Contains(columnName))
                {
                    this.configuration.Columns.Add(columnName);
                }
            }
        }

        private void WriteRow(DateTime originatingTime)
        {
            if (this.configuration.Writer == null || originatingTime <= this.lastWrite)
            {
                return;
            }

            this.lastWrite = originatingTime;

            if (this.configuration.WriteHeader && !this.headerWritten)
            {
                this.configuration.Writer.WriteLine(string.Join(this.configuration.Separator, new[] { "Timestamp" }.Concat(this.configuration.Columns)));
                this.headerWritten = true;
            }

            var cells = new List<string>
            {
                originatingTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString(this.configuration.Format, CultureInfo.InvariantCulture),
            };

            foreach (string column in this.configuration.Columns)
            {
                cells.Add(this.values.TryGetValue(column, out string value) ? value : this.configuration.MissingValue);
            }

            this.configuration.Writer.WriteLine(string.Join(this.configuration.Separator, cells));
        }
    }
}
