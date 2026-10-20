// <copyright file="IndicatorBuildContext.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// What an indicator sees while it is validated and built: the settings shared by the
    /// instance, the other indicators, and the methods to declare its clock and its outputs.
    /// One context is created per indicator, so everything declared through it is attributed
    /// to that indicator.
    /// </summary>
    public sealed class IndicatorBuildContext
    {
        private readonly IndicesBuildState state;
        private readonly ICollaborationIndicator indicator;
        private readonly IndicatorPulse pulse;

        internal IndicatorBuildContext(IndicesBuildState state, ICollaborationIndicator indicator, TimeSpan windowDuration)
        {
            this.state = state;
            this.indicator = indicator;
            this.WindowDuration = windowDuration;
            this.pulse = state.PulseOf(indicator.Name);
        }

        /// <summary>Gets the pipeline hosting the components.</summary>
        public Pipeline Pipeline => this.state.Pipeline;

        /// <summary>Gets the name of the instance, prefix of every component name.</summary>
        public string InstanceName => this.state.Name;

        /// <summary>Gets the participants of the session.</summary>
        public IReadOnlyList<uint> ParticipantIds => this.state.ParticipantIds;

        /// <summary>Gets the sliding window of this indicator: its own when it sets one, the window of the builder otherwise.</summary>
        public TimeSpan WindowDuration { get; }

        /// <summary>Gets the period of the clock of the indices.</summary>
        public TimeSpan ComputationInterval => this.state.ComputationInterval;

        /// <summary>Gets the calibration of the instance.</summary>
        public IndexCalibration Calibration => this.state.Calibration;

        /// <summary>Gets where the components persist their streams, or null when nothing is stored.</summary>
        public IndexStore? Store => this.state.Store;

        /// <summary>Participants as a new list, for a component configuration.</summary>
        /// <returns>A copy of the participant list.</returns>
        public List<uint> CopyParticipantIds() => new List<uint>(this.state.ParticipantIds);

        /// <summary>Name of a component of this instance.</summary>
        /// <param name="suffix">Suffix identifying the component.</param>
        /// <returns>The instance name followed by the suffix.</returns>
        public string ComponentName(string suffix) => $"{this.state.Name}-{suffix}";

        /// <summary>Normalizer of an index, from the calibration of the instance.</summary>
        /// <param name="indexName">Name of the index.</param>
        /// <returns>A saturating normalizer when the calibration knows the index, the identity otherwise.</returns>
        public IIndexNormalizer NormalizerFor(string indexName) => this.state.Calibration.NormalizerFor(indexName);

        /// <summary>Whether an indicator is declared in the instance.</summary>
        /// <param name="name">Name of the indicator.</param>
        /// <returns>True when it is declared.</returns>
        public bool Contains(string name) => this.state.Indicators.Any(i => i.Name == name);

        /// <summary>Another indicator of the instance, by name.</summary>
        /// <param name="name">Name of the indicator.</param>
        /// <returns>The indicator, or null when it is not declared.</returns>
        public ICollaborationIndicator? Find(string name) => this.state.Indicators.FirstOrDefault(i => i.Name == name);

        /// <summary>Another indicator of the instance, by type.</summary>
        /// <typeparam name="TIndicator">Type of the indicator.</typeparam>
        /// <param name="name">Name of the indicator, needed when several of that type are declared.</param>
        /// <returns>The indicator.</returns>
        public TIndicator Get<TIndicator>(string? name = null)
            where TIndicator : class, ICollaborationIndicator
            => this.state.Get<TIndicator>(name);

        /// <summary>
        /// Declares that the indicator is paced by the clock of the indices: the gated tick is
        /// connected to <paramref name="tickIn"/>.
        /// </summary>
        /// <param name="tickIn">Tick input of the component.</param>
        /// <param name="tickProcessed">
        /// Completion stream of the component (TickProcessedOut of the base classes). With it,
        /// the outputs declared afterwards are gathered tick by tick; without it, the score,
        /// the graph and the export read their latest value.
        /// </param>
        public void DriveByClock(Receiver<bool> tickIn, IProducer<bool>? tickProcessed = null)
        {
            if (tickIn == null)
            {
                throw new ArgumentNullException(nameof(tickIn));
            }

            this.EnsureBuilding();
            this.state.Gate!.Out.PipeTo(tickIn);
            this.pulse.Processed = tickProcessed;
            this.pulse.Source = null;
        }

        /// <summary>
        /// Declares that the indicator publishes whenever another one does, because it is
        /// computed from its output (an equality index from a participation).
        /// </summary>
        /// <param name="indicatorName">Name of the indicator it follows.</param>
        public void FollowPulseOf(string indicatorName)
        {
            this.EnsureBuilding();
            if (!this.Contains(indicatorName))
            {
                throw new ArgumentException($"Indicator '{this.indicator.Name}' follows '{indicatorName}', which is not declared.", nameof(indicatorName));
            }

            this.pulse.Processed = null;
            this.pulse.Source = this.state.PulseOf(indicatorName);
        }

        /// <summary>Declares a group level output.</summary>
        /// <param name="name">Name of the index.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="usage">What it is used for.</param>
        public void PublishGroup(string name, IProducer<double> stream, IndexUsage usage = IndexUsage.Export)
        {
            this.EnsureBuilding();
            this.state.Outputs.AddGroup(new IndexOutput<double>(name, this.indicator.Name, stream, usage, this.pulse));
        }

        /// <summary>Declares an output holding one value per participant.</summary>
        /// <param name="name">Name of the index.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="usage">What it is used for.</param>
        public void PublishIndividual(string name, IProducer<Dictionary<uint, double>> stream, IndexUsage usage = IndexUsage.All)
        {
            this.EnsureBuilding();
            this.state.Outputs.AddIndividual(new IndexOutput<Dictionary<uint, double>>(name, this.indicator.Name, stream, usage, this.pulse));
        }

        /// <summary>Declares an output holding one value per pair.</summary>
        /// <param name="name">Name of the index.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="usage">What it is used for.</param>
        public void PublishPair(string name, IProducer<Dictionary<ParticipantPair, double>> stream, IndexUsage usage = IndexUsage.All)
        {
            this.EnsureBuilding();
            this.state.Outputs.AddPair(new IndexOutput<Dictionary<ParticipantPair, double>>(name, this.indicator.Name, stream, usage, this.pulse));
        }

        /// <summary>Declares an output holding one value per ordered pair.</summary>
        /// <param name="name">Name of the index.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="usage">What it is used for.</param>
        public void PublishDirectedPair(string name, IProducer<Dictionary<DirectedParticipantPair, double>> stream, IndexUsage usage = IndexUsage.All)
        {
            this.EnsureBuilding();
            this.state.Outputs.AddDirectedPair(new IndexOutput<Dictionary<DirectedParticipantPair, double>>(name, this.indicator.Name, stream, usage, this.pulse));
        }

        /// <summary>
        /// Declares a normalized index feeding the collaboration score. Nothing is declared
        /// when the options of the indicator exclude it from the score.
        /// </summary>
        /// <param name="name">Name of the index, as referenced by the score dimensions.</param>
        /// <param name="stream">The normalized stream.</param>
        public void PublishScoreInput(string name, IProducer<double> stream)
        {
            this.EnsureBuilding();
            if (this.indicator.Options.IncludeInScore)
            {
                this.state.Outputs.AddScoreInput(new IndexOutput<double>(name, this.indicator.Name, stream, IndexUsage.None, this.pulse));
            }
        }

        /// <summary>
        /// Declares a flag telling whether a score input is usable. Several flags may be
        /// declared for the same index, which is usable when all of them are true.
        /// </summary>
        /// <param name="name">Name of the index.</param>
        /// <param name="stream">The flag.</param>
        public void PublishValidity(string name, IProducer<bool> stream)
        {
            this.EnsureBuilding();
            if (this.indicator.Options.IncludeInScore)
            {
                this.state.Outputs.AddValidity(new IndexOutput<bool>(name, this.indicator.Name, stream, IndexUsage.None, this.pulse));
            }
        }

        private void EnsureBuilding()
        {
            if (this.state.Gate == null)
            {
                throw new InvalidOperationException("Clocks and outputs can only be declared from ICollaborationIndicator.Build, not while validating.");
            }
        }
    }

    /// <summary>
    /// State shared by the contexts of one instance while it is validated and built.
    /// </summary>
    internal sealed class IndicesBuildState
    {
        private readonly Dictionary<string, IndicatorPulse> pulses = new Dictionary<string, IndicatorPulse>();

        public IndicesBuildState(Pipeline pipeline, string name, IReadOnlyList<uint> participantIds, TimeSpan computationInterval, IndexCalibration calibration, IndexStore? store, IReadOnlyList<ICollaborationIndicator> indicators)
        {
            this.Pipeline = pipeline;
            this.Name = name;
            this.ParticipantIds = participantIds;
            this.ComputationInterval = computationInterval;
            this.Calibration = calibration;
            this.Store = store;
            this.Indicators = indicators;
        }

        public Pipeline Pipeline { get; }

        public string Name { get; }

        public IReadOnlyList<uint> ParticipantIds { get; }

        public TimeSpan ComputationInterval { get; }

        public IndexCalibration Calibration { get; }

        public IndexStore? Store { get; }

        public IReadOnlyList<ICollaborationIndicator> Indicators { get; }

        public IndicatorOutputs Outputs { get; } = new IndicatorOutputs();

        /// <summary>Gets or sets the phase gate. Null while validating: nothing can be wired yet.</summary>
        public PhaseGateComponent? Gate { get; set; }

        public IndicatorPulse PulseOf(string indicatorName)
        {
            if (!this.pulses.TryGetValue(indicatorName, out IndicatorPulse? pulse))
            {
                pulse = new IndicatorPulse();
                this.pulses[indicatorName] = pulse;
            }

            return pulse;
        }

        public TIndicator Get<TIndicator>(string? name)
            where TIndicator : class, ICollaborationIndicator
        {
            TIndicator? found = this.Find<TIndicator>(name);
            if (found == null)
            {
                string wanted = name == null ? typeof(TIndicator).Name : $"'{name}' ({typeof(TIndicator).Name})";
                string declared = this.Indicators.Count == 0 ? "none" : string.Join(", ", this.Indicators.Select(i => i.Name));
                throw new InvalidOperationException($"Indicator {wanted} is not declared. Declared indicators: {declared}.");
            }

            return found;
        }

        public TIndicator? Find<TIndicator>(string? name)
            where TIndicator : class, ICollaborationIndicator
        {
            if (name != null)
            {
                return this.Indicators.FirstOrDefault(i => i.Name == name) as TIndicator;
            }

            List<TIndicator> matches = this.Indicators.OfType<TIndicator>().ToList();
            if (matches.Count > 1)
            {
                throw new InvalidOperationException(
                    $"{matches.Count} indicators of type {typeof(TIndicator).Name} are declared ({string.Join(", ", matches.Select(i => i.Name))}): give the name of the one you want.");
            }

            return matches.Count == 1 ? matches[0] : null;
        }
    }
}
