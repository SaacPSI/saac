// <copyright file="IndicatorOutputs.cs" company="SAAC">
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
    /// What a declared output is used for, beyond being available by name.
    /// </summary>
    [Flags]
    public enum IndexUsage
    {
        /// <summary>Only available by name.</summary>
        None = 0,

        /// <summary>Becomes a metric of the interaction graph.</summary>
        Graph = 1,

        /// <summary>Becomes one or several columns of the CSV export.</summary>
        Export = 2,

        /// <summary>Graph and export.</summary>
        All = Graph | Export,
    }

    /// <summary>
    /// One named output declared by an indicator.
    /// </summary>
    /// <typeparam name="T">Type of the messages.</typeparam>
    public sealed class IndexOutput<T>
    {
        internal IndexOutput(string name, string indicatorName, IProducer<T> stream, IndexUsage usage, IndicatorPulse pulse)
        {
            this.Name = name;
            this.IndicatorName = indicatorName;
            this.Stream = stream;
            this.Usage = usage;
            this.Pulse = pulse;
        }

        /// <summary>Gets the name of the index.</summary>
        public string Name { get; }

        /// <summary>Gets the name of the indicator that declared it.</summary>
        public string IndicatorName { get; }

        /// <summary>Gets the stream.</summary>
        public IProducer<T> Stream { get; }

        /// <summary>Gets what the output is used for.</summary>
        public IndexUsage Usage { get; }

        internal IndicatorPulse Pulse { get; }
    }

    /// <summary>
    /// The outputs declared by the indicators of one instance, by level and by name.
    /// This is what the score, the graph and the export are wired from.
    /// </summary>
    public sealed class IndicatorOutputs
    {
        private readonly List<IndexOutput<double>> groups = new List<IndexOutput<double>>();
        private readonly List<IndexOutput<Dictionary<uint, double>>> individuals = new List<IndexOutput<Dictionary<uint, double>>>();
        private readonly List<IndexOutput<Dictionary<ParticipantPair, double>>> pairs = new List<IndexOutput<Dictionary<ParticipantPair, double>>>();
        private readonly List<IndexOutput<Dictionary<DirectedParticipantPair, double>>> directedPairs = new List<IndexOutput<Dictionary<DirectedParticipantPair, double>>>();
        private readonly List<IndexOutput<double>> scoreInputs = new List<IndexOutput<double>>();
        private readonly List<IndexOutput<bool>> validities = new List<IndexOutput<bool>>();

        internal IndicatorOutputs()
        {
        }

        /// <summary>Gets the group level outputs, in declaration order.</summary>
        public IReadOnlyList<IndexOutput<double>> Groups => this.groups;

        /// <summary>Gets the per participant outputs, in declaration order.</summary>
        public IReadOnlyList<IndexOutput<Dictionary<uint, double>>> Individuals => this.individuals;

        /// <summary>Gets the per pair outputs, in declaration order.</summary>
        public IReadOnlyList<IndexOutput<Dictionary<ParticipantPair, double>>> Pairs => this.pairs;

        /// <summary>Gets the per ordered pair outputs, in declaration order.</summary>
        public IReadOnlyList<IndexOutput<Dictionary<DirectedParticipantPair, double>>> DirectedPairs => this.directedPairs;

        /// <summary>Gets the normalized indices feeding the collaboration score.</summary>
        public IReadOnlyList<IndexOutput<double>> ScoreInputs => this.scoreInputs;

        /// <summary>Gets the usability flags of the score inputs.</summary>
        public IReadOnlyList<IndexOutput<bool>> Validities => this.validities;

        /// <summary>Group level stream of an index.</summary>
        /// <param name="name">Name of the index.</param>
        /// <returns>The stream.</returns>
        public IProducer<double> Group(string name) => Find(this.groups, name, "group").Stream;

        /// <summary>Per participant stream of an index.</summary>
        /// <param name="name">Name of the index.</param>
        /// <returns>The stream.</returns>
        public IProducer<Dictionary<uint, double>> Individual(string name) => Find(this.individuals, name, "individual").Stream;

        /// <summary>Per pair stream of an index.</summary>
        /// <param name="name">Name of the index.</param>
        /// <returns>The stream.</returns>
        public IProducer<Dictionary<ParticipantPair, double>> Pair(string name) => Find(this.pairs, name, "pair").Stream;

        /// <summary>Per ordered pair stream of an index.</summary>
        /// <param name="name">Name of the index.</param>
        /// <returns>The stream.</returns>
        public IProducer<Dictionary<DirectedParticipantPair, double>> DirectedPair(string name) => Find(this.directedPairs, name, "directed pair").Stream;

        /// <summary>Normalized stream of an index, as it enters the collaboration score.</summary>
        /// <param name="name">Name of the index.</param>
        /// <returns>The stream.</returns>
        public IProducer<double> ScoreInput(string name) => Find(this.scoreInputs, name, "score input").Stream;

        internal void AddGroup(IndexOutput<double> output) => Add(this.groups, output, "group");

        internal void AddIndividual(IndexOutput<Dictionary<uint, double>> output) => Add(this.individuals, output, "individual");

        internal void AddPair(IndexOutput<Dictionary<ParticipantPair, double>> output) => Add(this.pairs, output, "pair");

        internal void AddDirectedPair(IndexOutput<Dictionary<DirectedParticipantPair, double>> output) => Add(this.directedPairs, output, "directed pair");

        internal void AddScoreInput(IndexOutput<double> output) => Add(this.scoreInputs, output, "score input");

        // Several flags may qualify the same index, so no uniqueness check here.
        internal void AddValidity(IndexOutput<bool> output) => this.validities.Add(output);

        private static void Add<T>(List<IndexOutput<T>> outputs, IndexOutput<T> output, string level)
        {
            IndexOutput<T>? existing = outputs.FirstOrDefault(o => o.Name == output.Name);
            if (existing != null)
            {
                throw new CollaborationIndicesConfigurationException(new[]
                {
                    $"The {level} output '{output.Name}' is declared by both '{existing.IndicatorName}' and '{output.IndicatorName}'. " +
                    "Give one of the two indicators another name (Options.Name).",
                });
            }

            outputs.Add(output);
        }

        private static IndexOutput<T> Find<T>(List<IndexOutput<T>> outputs, string name, string level)
        {
            IndexOutput<T>? output = outputs.FirstOrDefault(o => o.Name == name);
            if (output == null)
            {
                string available = outputs.Count == 0 ? "none" : string.Join(", ", outputs.Select(o => o.Name));
                throw new ArgumentException($"No {level} output named '{name}'. Available: {available}.", nameof(name));
            }

            return output;
        }
    }

    /// <summary>
    /// Completion stream of an indicator, shared by its outputs and by the indicators derived
    /// from it. It is what lets the outputs of one tick be gathered reliably.
    /// </summary>
    internal sealed class IndicatorPulse
    {
        /// <summary>Gets or sets the stream telling, for each tick, whether the indicator published.</summary>
        public IProducer<bool>? Processed { get; set; }

        /// <summary>Gets or sets the pulse this one follows, for an indicator computed from another one.</summary>
        public IndicatorPulse? Source { get; set; }

        /// <summary>Gets the pulse at the origin of the chain.</summary>
        public IndicatorPulse Root => this.Source == null ? this : this.Source.Root;
    }
}
