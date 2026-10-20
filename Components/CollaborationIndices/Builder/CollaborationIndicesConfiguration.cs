// <copyright file="CollaborationIndicesConfiguration.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SAAC.CollaborationIndices
{
    /// <summary>
    /// Declaration of one indicator in a <see cref="CollaborationIndicesConfiguration"/>.
    /// </summary>
    public class IndicatorConfiguration
    {
        /// <summary>
        /// Gets or sets the kind of indicator: a name of <see cref="Indicators"/> ("Movement",
        /// "Synchrony", ...), a name registered in <see cref="IndicatorRegistry"/>, or the
        /// assembly qualified name of a class implementing <see cref="ICollaborationIndicator"/>.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the options of the indicator, by property name. Null or empty keeps
        /// the defaults. "Name" renames the indicator, "WindowDuration" gives it its own window.
        /// </summary>
        public Dictionary<string, object>? Options { get; set; } = null;
    }

    /// <summary>
    /// Serializable description of a set of collaboration indices: what the builder does in
    /// code, as data. Everything in it can be written in a JSON file.
    ///
    /// <code>
    /// var configuration = CollaborationIndicesConfiguration.Load("indices.json");
    /// var indices = CollaborationIndicesBuilder.FromConfiguration(pipeline, configuration).Build();
    /// </code>
    /// </summary>
    public class CollaborationIndicesConfiguration
    {
        /// <summary>Gets or sets the name of the instance, prefix of every component and stream name.</summary>
        public string Name { get; set; } = CollaborationIndicesBuilder.DefaultName;

        /// <summary>Gets or sets the participants of the session. Any number, contiguous or not.</summary>
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>Gets or sets the sliding window of the indicators that do not set their own.</summary>
        public TimeSpan WindowDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Gets or sets the publication period of the indices.</summary>
        public TimeSpan ComputationInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets a value indicating whether the instance creates its own clock. When
        /// false, connect a tick stream to ClockIn, which is what a replay needs.
        /// </summary>
        public bool UseInternalClock { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the indices only run inside a phase, i.e.
        /// after a message on Gate.PhaseStartIn.
        /// </summary>
        public bool RequirePhase { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether the gate transitions are traced on the console.</summary>
        public bool LogGateTransitions { get; set; } = false;

        /// <summary>
        /// Gets or sets the calibration of the normalized indices. Null uses the calibration
        /// measured for the window (<see cref="IndexCalibration.ForWindow"/>).
        /// </summary>
        public IndexCalibration? Calibration { get; set; } = null;

        /// <summary>Gets or sets the indicators to compute.</summary>
        public List<IndicatorConfiguration> Indicators { get; set; } = new List<IndicatorConfiguration>();

        /// <summary>Gets or sets a value indicating whether the collaboration score is computed.</summary>
        public bool ComputeCollaborationScores { get; set; } = false;

        /// <summary>Gets or sets the dimensions of the collaboration score. Null uses the default ones.</summary>
        public List<ScoreDimension>? Dimensions { get; set; } = null;

        /// <summary>Gets or sets a value indicating whether the interaction graph is published.</summary>
        public bool GenerateGraph { get; set; } = false;

        /// <summary>Gets or sets the path of the CSV export. Null or empty disables it.</summary>
        public string? ExportPath { get; set; } = null;

        /// <summary>Gets or sets the columns of the CSV export. Null exports every declared output.</summary>
        public List<string>? ExportColumns { get; set; } = null;

        /// <summary>Gets the JSON settings shared by the configuration and the indicator options.</summary>
        internal static JsonSerializerSettings JsonSettings => new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,

            // Replace, not merge: a list read from the file must not be appended to the default one.
            ObjectCreationHandling = ObjectCreationHandling.Replace,

            // A misspelled property would otherwise be silently ignored.
            MissingMemberHandling = MissingMemberHandling.Error,
            Converters = { new StringEnumConverter() },
        };

        /// <summary>Reads a configuration from JSON.</summary>
        /// <param name="json">The JSON text.</param>
        /// <returns>The configuration.</returns>
        public static CollaborationIndicesConfiguration FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("The configuration text is empty.", nameof(json));
            }

            try
            {
                return JsonConvert.DeserializeObject<CollaborationIndicesConfiguration>(json, JsonSettings)
                    ?? throw new CollaborationIndicesConfigurationException(new[] { "The configuration text contains no configuration." });
            }
            catch (JsonException exception)
            {
                throw new CollaborationIndicesConfigurationException(new[] { "The configuration cannot be read: " + exception.Message });
            }
        }

        /// <summary>Reads a configuration from a JSON file.</summary>
        /// <param name="path">Path of the file.</param>
        /// <returns>The configuration.</returns>
        public static CollaborationIndicesConfiguration Load(string path) => FromJson(File.ReadAllText(path));

        /// <summary>Writes the configuration as JSON.</summary>
        /// <returns>The JSON text.</returns>
        public string ToJson() => JsonConvert.SerializeObject(this, JsonSettings);

        /// <summary>Writes the configuration into a JSON file.</summary>
        /// <param name="path">Path of the file.</param>
        public void Save(string path) => File.WriteAllText(path, this.ToJson());
    }
}
