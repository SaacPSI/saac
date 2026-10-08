// <copyright file="CollaborationSessionConfiguration.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

namespace SAAC.CollaborationIndices
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using Microsoft.Psi;
    using Newtonsoft.Json;

    /// <summary>The frame the positions of a session are expressed in.</summary>
    public enum DataFrame
    {
        /// <summary>The frame of a Unity scene: left-handed, Y up. What the Unity server streams.</summary>
        Unity,

        /// <summary>
        /// The standardised frame: right-handed, Z up, which is the Unity frame with Y and Z
        /// swapped. What PositionOrientationPreProcessing publishes.
        /// </summary>
        Standard,
    }

    /// <summary>
    /// The settings of the detectors that an application may want to change. Everything
    /// else keeps the default of each detector.
    /// </summary>
    public class DetectorSettings
    {
        /// <summary>
        /// Gets or sets the frame of the positions. It tells which axis is vertical, for what
        /// is measured on the horizontal plane, and in which frame the direction of a head is
        /// expressed. Declared as it is, a session gives the same indices in either frame.
        /// </summary>
        public DataFrame Frame { get; set; } = DataFrame.Unity;

        /// <summary>
        /// Gets or sets the local axis of the recorded head transform that points where the
        /// participant looks: "+Z" for a Unity transform, "+Y" for the head bone of the
        /// avatars of the puzzle task. Used for the formations and the joint attention test.
        /// </summary>
        public string HeadForwardAxis { get; set; } = "+Z";

        /// <summary>Gets or sets how long a turn taking waits to be confirmed (TurnTakingDetector).</summary>
        public TimeSpan TurnTakingConfirmationDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>Gets or sets the shortest look that counts (GazeEpisodeDetector).</summary>
        public TimeSpan MinimumGazeDuration { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>Gets or sets a value indicating whether a joint look requires the heads to point to the same place (JointVisualAttentionDetector).</summary>
        public bool RequireConvergingHeads { get; set; } = false;

        /// <summary>Gets or sets how long a formation must be observed to start, and missing to end (FFormationDetector).</summary>
        public TimeSpan FormationTransitionDuration { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>Gets the vertical axis of the frame of the positions.</summary>
        /// <returns>Y for the Unity frame, Z for the standard one.</returns>
        public Vector3 UpAxis() => this.Frame == DataFrame.Standard ? Vector3.UnitZ : Vector3.UnitY;

        /// <summary>Gets the forward axis as a vector.</summary>
        /// <returns>The unit vector of <see cref="HeadForwardAxis"/>.</returns>
        public Vector3 HeadForwardVector()
        {
            string axis = (this.HeadForwardAxis ?? string.Empty).Trim().ToUpperInvariant();
            float sign = axis.StartsWith("-") ? -1f : 1f;
            switch (axis.TrimStart('+', '-'))
            {
                case "X": return new Vector3(sign, 0, 0);
                case "Y": return new Vector3(0, sign, 0);
                case "Z": return new Vector3(0, 0, sign);
                default: throw new CollaborationIndicesConfigurationException(new[] { $"HeadForwardAxis '{this.HeadForwardAxis}' is not one of +X, -X, +Y, -Y, +Z, -Z." });
            }
        }

        /// <summary>
        /// Position and forward direction of a head, from its position and Unity Euler angles
        /// in degrees. The angles are those of the Unity transform in both frames: the
        /// standardised streams keep them as they are and only swap the position.
        ///
        /// With <see cref="HeadForwardAxis"/> "+Y" and the standard frame, this is the forward
        /// of PositionOrientationPreProcessing.Convert: its local +Z axis of the reframed
        /// rotation is the local +Y axis of the Unity transform, with Y and Z swapped.
        /// </summary>
        /// <param name="positionAndEuler">Position, and Euler angles (X pitch, Y yaw, Z roll).</param>
        /// <returns>Position and forward direction, both in the frame of the position.</returns>
        public Tuple<Vector3, Vector3> ToHeadPose(Tuple<Vector3, Vector3> positionAndEuler)
        {
            const float DegreesToRadians = (float)Math.PI / 180f;
            Vector3 euler = positionAndEuler.Item2;
            Quaternion rotation = Quaternion.CreateFromYawPitchRoll(euler.Y * DegreesToRadians, euler.X * DegreesToRadians, euler.Z * DegreesToRadians);
            Vector3 forward = Vector3.Transform(this.HeadForwardVector(), rotation);

            // The direction comes out in the Unity frame; the standard one has Y and Z swapped.
            if (this.Frame == DataFrame.Standard)
            {
                forward = new Vector3(forward.X, forward.Z, forward.Y);
            }

            return Tuple.Create(positionAndEuler.Item1, forward);
        }
    }

    /// <summary>
    /// What an application computes for one session: for whom, on which windows, which
    /// indicators, with or without scores and profiles. It is the template shared by the
    /// console applications, which read it from a JSON file, and by the server, which fills
    /// it from its interface.
    ///
    /// <code>
    /// var configuration = CollaborationSessionConfiguration.Load("session.json");
    /// SlidingAverageComputationSet indices = configuration.CreateBuilder(pipeline)
    ///     .WithDataClock(headPositions[0])
    ///     .BuildSet(configuration.Windows.ToArray());
    /// </code>
    ///
    /// What is specific to an application stays in the application: where its streams are and
    /// how they are connected. An indicator that is not declared is not computed, so the
    /// application connects an input only when <c>indices.Contains(name)</c>.
    /// </summary>
    public class CollaborationSessionConfiguration
    {
        // ---- Where ----

        /// <summary>Gets or sets the folder of the recorded session, for the applications that replay one.</summary>
        public string DatasetPath { get; set; } = string.Empty;

        /// <summary>Gets or sets the folder the result files are written in.</summary>
        public string OutputFolder { get; set; } = string.Empty;

        /// <summary>Gets or sets a value indicating whether a recorded session is replayed at its original pace rather than as fast as possible.</summary>
        public bool RealTime { get; set; } = false;

        // ---- Who and when ----

        /// <summary>Gets or sets the prefix of the components and streams.</summary>
        public string Name { get; set; } = "Indices";

        /// <summary>Gets or sets the participants of the session.</summary>
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>Gets or sets the sliding windows; the indices are computed once per window.</summary>
        public List<TimeSpan> Windows { get; set; } = new List<TimeSpan> { TimeSpan.FromSeconds(20) };

        /// <summary>Gets or sets the period at which the indices are published.</summary>
        public TimeSpan ComputationInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>Gets or sets a value indicating whether the indices only run inside a phase (a puzzle, an exercise).</summary>
        public bool RequirePhase { get; set; } = false;

        // ---- What ----

        /// <summary>Gets or sets the indicators, by catalogue name, each with its options.</summary>
        public List<IndicatorConfiguration> Indicators { get; set; } = new List<IndicatorConfiguration>();

        /// <summary>Gets or sets a value indicating whether the dimension scores and the collaboration score are computed.</summary>
        public bool ComputeCollaborationScores { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether the collaboration profiles of the pairs and of the group are computed.</summary>
        public bool ComputeProfiles { get; set; } = false;

        /// <summary>Gets or sets the settings of the detectors.</summary>
        public DetectorSettings Detectors { get; set; } = new DetectorSettings();

        /// <summary>
        /// The indicators the collaboration profiles read. A profile computed without one of
        /// them reads 0 for it.
        /// </summary>
        public static IReadOnlyList<string> ProfileIndicators { get; } = new[]
        {
            IndexNames.VerbalParticipation, IndexNames.SpeechEquality, IndexNames.TalkingMost, IndexNames.TurnTaking,
            IndexNames.JointVisualAttention, IndexNames.GazeOnPeers, IndexNames.TaskParticipation, IndexNames.TaskEquality,
            IndexNames.TaskingMost, IndexNames.Formation, IndexNames.Synchrony,
        };

        /// <summary>A configuration with every indicator of the catalogue that has a detector, to start from.</summary>
        /// <param name="participantCount">Number of participants, numbered from 0.</param>
        /// <returns>The template.</returns>
        public static CollaborationSessionConfiguration Template(int participantCount = 3)
        {
            var configuration = new CollaborationSessionConfiguration
            {
                DatasetPath = @"C:\path\to\session",
                OutputFolder = @"C:\path\to\results",
                ParticipantIds = Enumerable.Range(0, participantCount).Select(i => (uint)i).ToList(),
                Windows = new List<TimeSpan> { TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(45) },
                ComputeCollaborationScores = true,
                ComputeProfiles = true,
            };

            foreach (string name in new[]
            {
                IndexNames.Movement, IndexNames.Synchrony, IndexNames.VerbalParticipation, IndexNames.SpeechEquality, IndexNames.TalkingMost,
                IndexNames.TurnTaking, IndexNames.Silence, IndexNames.CrossTalk, IndexNames.JointVisualAttention, IndexNames.GazeOnPeers,
                IndexNames.MutualGaze, IndexNames.TaskParticipation, IndexNames.TaskEquality, IndexNames.TaskingMost, IndexNames.Formation,
                IndexNames.Proximity,
            })
            {
                configuration.Add(name);
            }

            // The areas are those of the scene: here the ones of the puzzle task.
            configuration.Add(IndexNames.TimeInArea, new Dictionary<string, object>
            {
                { "Areas", new List<string> { "CentraleTableZone", "Generator1", "Generator2", "IterationTable", "Button" } },
                { "PlanningAreas", new List<string> { "IterationTable" } },
            });

            return configuration;
        }

        /// <summary>Reads a configuration from JSON.</summary>
        /// <param name="json">The JSON text.</param>
        /// <returns>The configuration.</returns>
        public static CollaborationSessionConfiguration FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("The configuration text is empty.", nameof(json));
            }

            try
            {
                return JsonConvert.DeserializeObject<CollaborationSessionConfiguration>(json, CollaborationIndicesConfiguration.JsonSettings)
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
        public static CollaborationSessionConfiguration Load(string path) => FromJson(File.ReadAllText(path));

        /// <summary>Writes the configuration as JSON.</summary>
        /// <returns>The JSON text.</returns>
        public string ToJson() => JsonConvert.SerializeObject(this, CollaborationIndicesConfiguration.JsonSettings);

        /// <summary>Writes the configuration into a JSON file.</summary>
        /// <param name="path">Path of the file.</param>
        public void Save(string path) => File.WriteAllText(path, this.ToJson());

        /// <summary>Whether an indicator is declared.</summary>
        /// <param name="name">Catalogue name of the indicator.</param>
        /// <returns>True when it is.</returns>
        public bool Has(string name) => this.Indicators.Any(indicator => indicator.Type == name);

        /// <summary>
        /// Declares an indicator, once, together with the indicator it is computed from when
        /// there is one (speech equality needs verbal participation, and so on).
        /// </summary>
        /// <param name="name">Catalogue name of the indicator.</param>
        /// <param name="options">Options of the indicator, by property name.</param>
        /// <returns>This configuration.</returns>
        public CollaborationSessionConfiguration Add(string name, Dictionary<string, object>? options = null)
        {
            if (Sources.TryGetValue(name, out string? source) && !this.Has(source))
            {
                this.Add(source);
            }

            if (!this.Has(name))
            {
                this.Indicators.Add(new IndicatorConfiguration { Type = name, Options = options });
            }

            return this;
        }

        /// <summary>The problems of the configuration that do not need a pipeline to be found.</summary>
        /// <returns>One line per problem; empty when there is none.</returns>
        public IEnumerable<string> Validate()
        {
            if (this.ParticipantIds == null || this.ParticipantIds.Count == 0)
            {
                yield return "ParticipantIds: at least one participant is required.";
            }

            if (this.Windows == null || this.Windows.Count == 0)
            {
                yield return "Windows: at least one window is required.";
            }
            else
            {
                if (this.Windows.Any(window => window <= TimeSpan.Zero))
                {
                    yield return "Windows: every window must be strictly positive.";
                }

                if (this.Windows.Distinct().Count() != this.Windows.Count)
                {
                    yield return "Windows: the same window is declared twice.";
                }
            }

            if (this.Indicators == null || this.Indicators.Count == 0)
            {
                yield return "Indicators: at least one indicator is required.";
            }

            if (this.ComputeProfiles && this.ParticipantIds != null && this.ParticipantIds.Count < 2)
            {
                yield return "ComputeProfiles: the profiles are those of pairs, two participants are required.";
            }
        }

        /// <summary>The indicators the profiles read that are not declared.</summary>
        /// <returns>Their names.</returns>
        public List<string> MissingProfileIndicators() => ProfileIndicators.Where(name => !this.Has(name)).ToList();

        /// <summary>
        /// A builder with the participants, the period, the phases, the indicators and the
        /// scores of this configuration. The application adds what depends on its streams: the
        /// clock, the exports, the stores; then calls <c>BuildSet</c> with <see cref="Windows"/>.
        /// </summary>
        /// <param name="pipeline">The pipeline the components are created in.</param>
        /// <returns>The builder.</returns>
        public CollaborationIndicesBuilder CreateBuilder(Pipeline pipeline)
        {
            string[] problems = this.Validate().ToArray();
            if (problems.Length > 0)
            {
                throw new CollaborationIndicesConfigurationException(problems);
            }

            CollaborationIndicesBuilder builder = new CollaborationIndicesBuilder(pipeline)
                .WithName(this.Name)
                .WithParticipants(this.ParticipantIds)
                .WithComputationInterval(this.ComputationInterval)
                .WithPhaseGate(this.RequirePhase);

            foreach (IndicatorConfiguration indicator in this.Indicators)
            {
                builder.AddIndicator(indicator.Type, indicator.Options);
            }

            if (this.ComputeCollaborationScores)
            {
                builder.WithCollaborationScore();
            }

            return builder;
        }

        /// <summary>The profile component of one window, connected to the indices of that window.</summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="window">The window.</param>
        /// <param name="indices">The indices of the window.</param>
        /// <param name="profiles">Writers and session identifier; the participants and the gaze reference are filled here.</param>
        /// <returns>The component.</returns>
        public CollaborationProfilesComponent CreateProfiles(Pipeline pipeline, TimeSpan window, SlidingAverageComputation indices, CollaborationProfilesConfiguration? profiles = null)
        {
            profiles = profiles ?? new CollaborationProfilesConfiguration();
            profiles.ParticipantIds = this.ParticipantIds.ToList();
            profiles.Rules.GazeOnPeersReference = CollaborationProfileRulesConfiguration.GazeOnPeersReferenceFor(window);

            var component = new CollaborationProfilesComponent(pipeline, profiles, $"{this.Name}Profiles{SlidingAverageComputationSet.StoreSuffix(window)}");
            indices.SnapshotOut.PipeTo(component.SnapshotIn);
            indices.Gate.PhaseIdOut.PipeTo(component.PhaseIdIn);
            if (indices.Contains(IndexNames.JointVisualAttention))
            {
                indices.Get(SAAC.CollaborationIndices.Indicators.JointVisualAttention).Component.LeadVisualAttentionByPairOut.PipeTo(component.LeadVisualAttentionByPairIn);
            }

            return component;
        }

        // The indicators computed from another one.
        private static readonly Dictionary<string, string> Sources = new Dictionary<string, string>
        {
            { IndexNames.SpeechEquality, IndexNames.VerbalParticipation },
            { IndexNames.TalkingMost, IndexNames.VerbalParticipation },
            { IndexNames.TaskEquality, IndexNames.TaskParticipation },
            { IndexNames.TaskingMost, IndexNames.TaskParticipation },
        };
    }
}
