// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    using System.Collections.Generic;

    /// <summary>
    /// Names of the metrics of the sociogram (<see cref="IndividualPairCharacteristics"/>).
    /// They are the names the collaboration indices give to their outputs, so that a set of
    /// indices is copied name for name whatever the indicators declared. A metric that is not
    /// computed is absent from the dictionaries, which is not the same as a value of zero.
    /// </summary>
    public static class GraphMetricNames
    {
        /// <summary>Per participant: share of the window spent speaking, in [0, 1].</summary>
        public const string VerbalParticipation = "VerbalParticipation";

        /// <summary>Per participant: physical activity level.</summary>
        public const string Movement = "Movement";

        /// <summary>Per participant: participation in the task.</summary>
        public const string TaskParticipation = "TaskParticipation";

        /// <summary>
        /// Per participant: its place in the room at the instant of the graph, seen from above,
        /// in metres: first horizontal coordinate, drawn from left to right.
        /// </summary>
        public const string PositionX = "PositionX";

        /// <summary>
        /// Per participant: its place in the room at the instant of the graph, seen from above,
        /// in metres: second horizontal coordinate, drawn from the bottom to the top.
        /// </summary>
        public const string PositionY = "PositionY";

        /// <summary>Per participant: mean of <see cref="PositionX"/> over the sliding window of the other metrics.</summary>
        public const string MeanPositionX = "MeanPositionX";

        /// <summary>Per participant: mean of <see cref="PositionY"/> over the sliding window of the other metrics.</summary>
        public const string MeanPositionY = "MeanPositionY";

        /// <summary>Per pair: distance between the two participants at the instant of the graph, in metres.</summary>
        public const string Distance = "Distance";

        /// <summary>
        /// Per pair: mean distance between the two participants over the sliding window of the
        /// other metrics, in metres. The sociogram places its nodes from it, so that the whole
        /// picture describes the same span of time.
        /// </summary>
        public const string MeanDistance = "MeanDistance";

        /// <summary>Per pair: proximity score of the collaboration indices, 1 when the participants are together.</summary>
        public const string Proximity = "Proximity";

        /// <summary>Per pair: synchrony of the movements, in [0, 1].</summary>
        public const string Synchrony = "Synchrony";

        /// <summary>Per pair: joint visual attention.</summary>
        public const string JointVisualAttention = "JointVisualAttention";

        /// <summary>Per pair: mean duration of the joint visual attention episodes, normalised.</summary>
        public const string JointVisualAttentionIntensity = "JointVisualAttentionIntensity";

        /// <summary>Per pair: equality of the speaking times, -1 when nobody spoke.</summary>
        public const string SpeechEquality = "SpeechEquality";

        /// <summary>Per pair: equality of the participations in the task.</summary>
        public const string TaskEquality = "TaskEquality";

        /// <summary>Per ordered pair: looks of the first participant at the second.</summary>
        public const string GazeOnPeers = "GazeOnPeers";

        /// <summary>Group: number of the participant who speaks most, 0 when nobody does.</summary>
        public const string TalkingMost = "TalkingMost";

        /// <summary>Group: number of the participant who acts most on the task, 0 when nobody does.</summary>
        public const string TaskingMost = "TaskingMost";

        /// <summary>Group: number of the participant the others look at most, 0 when nobody is.</summary>
        public const string WatchedMost = "WatchedMost";

        /// <summary>Group: number of the participant who leads the visual attention, 0 when nobody does.</summary>
        public const string LeadVisualAttention = "LeadVisualAttention";

        /// <summary>Group: dominance score.</summary>
        public const string Dominance = "Dominance";

        /// <summary>Group: joint attention score.</summary>
        public const string JointAttention = "JointAttention";

        /// <summary>Group: communication score.</summary>
        public const string CommunicationProcessManagement = "CommunicationProcessManagement";

        /// <summary>Group: spatial score.</summary>
        public const string SpatialBehaviour = "SpatialBehaviour";

        /// <summary>Group: engagement score.</summary>
        public const string Engagement = "Engagement";

        /// <summary>Group: mean of the dimension scores, as the pipeline before the collaboration indices computed it.</summary>
        public const string SecondDimension = "Dimension2";

        /// <summary>Group: collaboration score.</summary>
        public const string CollaborationScore = "CollaborationScore";

        /// <summary>
        /// Gets the group metrics that name a participant, in the order the sociogram lists them.
        /// </summary>
        public static IReadOnlyList<string> Leaders { get; } = new[] { TaskingMost, TalkingMost, WatchedMost, LeadVisualAttention };

        /// <summary>
        /// Gets the scores of the default collaboration model, in the order the sociogram lists them.
        /// </summary>
        public static IReadOnlyList<string> DefaultScores { get; } = new[]
        {
            Dominance, JointAttention, CommunicationProcessManagement, SpatialBehaviour, Engagement, CollaborationScore, SecondDimension,
        };
    }
}
