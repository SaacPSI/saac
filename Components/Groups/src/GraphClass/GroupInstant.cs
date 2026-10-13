// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Groups
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// What a group is doing at one instant: where each participant is and which way it
    /// faces, who is speaking, who is looking at whom or at what, which pairs stand in a
    /// formation, what each one holds, and what was detected in the last few seconds.
    ///
    /// It is the counterpart of <see cref="IndividualPairCharacteristics"/>, which describes
    /// the last sliding window: here nothing is a mean or a count, everything is the state
    /// of the instant, for any number of participants. What is not known is left out: a
    /// group without eye tracking has no look, and <see cref="Signals"/> says what the state
    /// was built from.
    /// </summary>
    public class GroupInstant
    {
        /// <summary>Name of the signal of the places and headings of the participants.</summary>
        public const string Places = "Places";

        /// <summary>Name of the signal of the voice activity of the participants.</summary>
        public const string Voice = "Voice";

        /// <summary>Name of the signal of the looks of the participants.</summary>
        public const string Gaze = "Gaze";

        /// <summary>Name of the signal of the formations of the pairs.</summary>
        public const string Formations = "Formations";

        /// <summary>Name of the signal of the actions on the objects of the task.</summary>
        public const string Task = "Task";

        /// <summary>Gets or sets the participants.</summary>
        public List<PersonInstant> Persons { get; set; } = new List<PersonInstant>();

        /// <summary>Gets or sets the pairs of participants that have something in common at this instant.</summary>
        public List<PairInstant> Pairs { get; set; } = new List<PairInstant>();

        /// <summary>Gets or sets what was detected in the last few seconds, the latest last.</summary>
        public List<InstantEvent> Events { get; set; } = new List<InstantEvent>();

        /// <summary>
        /// Gets or sets the signals the state is built from (<see cref="Places"/>,
        /// <see cref="Voice"/>, <see cref="Gaze"/>, <see cref="Formations"/>, <see cref="Task"/>):
        /// what is not named is not known, which is not the same as nobody speaking or looking.
        /// </summary>
        public List<string> Signals { get; set; } = new List<string>();

        /// <summary>The state of a participant.</summary>
        /// <param name="id">Identifier of the participant.</param>
        /// <returns>The state, or null when the group has none for it.</returns>
        public PersonInstant? FindPerson(string id) => this.Persons?.FirstOrDefault(person => person != null && person.Id == id);

        /// <summary>The state of a pair.</summary>
        /// <param name="a">One participant.</param>
        /// <param name="b">The other one.</param>
        /// <returns>The state, or null when the pair has nothing in common at this instant.</returns>
        public PairInstant? FindPair(string a, string b)
            => this.Pairs?.FirstOrDefault(pair => pair != null && ((pair.First == a && pair.Second == b) || (pair.First == b && pair.Second == a)));

        /// <summary>Says whether the state is built from a signal.</summary>
        /// <param name="signal">Name of the signal.</param>
        /// <returns>True when it is.</returns>
        public bool Has(string signal) => this.Signals != null && this.Signals.Contains(signal);
    }

    /// <summary>
    /// A participant at one instant.
    /// </summary>
    public class PersonInstant
    {
        /// <summary>Gets or sets the identifier of the participant: "1" for the first one.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Gets or sets a value indicating whether the place of the participant is known.</summary>
        public bool HasPlace { get; set; }

        /// <summary>Gets or sets the place in the room, seen from above, in metres: drawn from left to right.</summary>
        public double X { get; set; }

        /// <summary>Gets or sets the place in the room, seen from above, in metres: drawn from the bottom to the top.</summary>
        public double Y { get; set; }

        /// <summary>
        /// Gets or sets the direction the participant faces, on the floor: first coordinate of
        /// a vector of length 1, or 0 with <see cref="HeadingY"/> when it is not known.
        /// </summary>
        public double HeadingX { get; set; }

        /// <summary>Gets or sets the second coordinate of the direction the participant faces.</summary>
        public double HeadingY { get; set; }

        /// <summary>Gets or sets a value indicating whether the participant is speaking, from its voice activity.</summary>
        public bool Speaking { get; set; }

        /// <summary>Gets or sets the participants this one is looking at.</summary>
        public List<string> LooksAtPersons { get; set; } = new List<string>();

        /// <summary>Gets or sets the objects this participant is looking at.</summary>
        public List<string> LooksAtObjects { get; set; } = new List<string>();

        /// <summary>Gets or sets the objects this participant holds.</summary>
        public List<string> Holds { get; set; } = new List<string>();
    }

    /// <summary>
    /// A pair of participants at one instant.
    /// </summary>
    public class PairInstant
    {
        /// <summary>Gets or sets the participant of the pair with the lower number.</summary>
        public string First { get; set; } = string.Empty;

        /// <summary>Gets or sets the other participant of the pair.</summary>
        public string Second { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the formation the two participants stand in (face to face, L shape,
        /// side by side, as the formation detector names it); empty when they are in none.
        /// </summary>
        public string Formation { get; set; } = string.Empty;

        /// <summary>Gets or sets a value indicating whether the two participants are looking at each other.</summary>
        public bool MutualGaze { get; set; }

        /// <summary>Gets or sets the objects both participants are looking at.</summary>
        public List<string> SharedObjects { get; set; } = new List<string>();
    }

    /// <summary>
    /// Something that was detected: a joint visual attention, a mutual gaze, a turn taking,
    /// an action on an object, a handover.
    /// </summary>
    public class InstantEvent
    {
        /// <summary>Gets or sets when it happened.</summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets when it was detected: a detector names an event when it is complete,
        /// which may be some time after it happened (a look when it ends, a turn taking once
        /// it is confirmed).
        /// </summary>
        public DateTime DetectedAt { get; set; }

        /// <summary>Gets or sets what it is, as the detector names it: "JointVisualAttention", "Grab"...</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Gets or sets the participant who did it.</summary>
        public string Person { get; set; } = string.Empty;

        /// <summary>Gets or sets the other participant, for what involves two; empty otherwise.</summary>
        public string Other { get; set; } = string.Empty;

        /// <summary>Gets or sets what it is about: an object, a kind of formation.</summary>
        public string Label { get; set; } = string.Empty;
    }
}
