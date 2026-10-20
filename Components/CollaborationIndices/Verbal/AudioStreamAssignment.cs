// <copyright file="AudioStreamAssignment.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SAAC.CollaborationIndices.Verbal
{
    /// <summary>
    /// Which audio stream is the one of which participant, for a session whose speech is
    /// computed from the audio (<see cref="SpeechFromAudio"/>).
    ///
    /// The names of the streams are not interpreted: a session may name them by number, by
    /// colour or by device. The audio streams of the session are numbered from 1, in the
    /// natural order of their names ("Audio_2" before "Audio_10"), and that number is the id
    /// of the stream. With one stream per participant and no instruction, stream 1 is
    /// participant 1, stream 2 is participant 2... An instruction gives the stream of each
    /// participant, by id or by name, in the order of the participants.
    ///
    /// <code>
    /// var assignment = AudioStreamAssignment.Resolve(new[] { "Audio_User_yellow", "Audio_User_red" }, null, 2);
    /// // assignment.Available: Audio_User_red (id 1), Audio_User_yellow (id 2)
    /// // assignment.Assigned:  Audio_User_red, Audio_User_yellow
    ///
    /// assignment = AudioStreamAssignment.Resolve(new[] { "Audio_User_yellow", "Audio_User_red" }, new[] { "2", "1" }, 2);
    /// // assignment.Assigned:  Audio_User_yellow, Audio_User_red
    /// </code>
    /// </summary>
    public class AudioStreamAssignment
    {
        private AudioStreamAssignment(List<string> available, List<string> assigned, bool isExplicit, string problem)
        {
            this.Available = available;
            this.Assigned = assigned;
            this.IsExplicit = isExplicit;
            this.Problem = problem;
        }

        /// <summary>Gets the audio streams of the session, each one once, in the order of their ids: the id of a stream is its rank from 1.</summary>
        public IReadOnlyList<string> Available { get; }

        /// <summary>Gets the stream of each participant, in the order of the participants; empty when it could not be resolved.</summary>
        public IReadOnlyList<string> Assigned { get; }

        /// <summary>Gets a value indicating whether every participant has a stream.</summary>
        public bool IsResolved => this.Assigned.Count > 0;

        /// <summary>Gets a value indicating whether the assignment follows an instruction, and not the order of the ids.</summary>
        public bool IsExplicit { get; }

        /// <summary>Gets why the assignment could not be resolved; empty when it was.</summary>
        public string Problem { get; }

        /// <summary>Gives each participant an audio stream.</summary>
        /// <param name="availableStreams">Names of the audio streams of the session, in any order; a name met several times counts once.</param>
        /// <param name="requested">The stream of each participant, by id or by name, in the order of the participants; null or empty for the order of the ids.</param>
        /// <param name="participants">Number of participants.</param>
        /// <returns>The assignment, resolved or with its problem.</returns>
        public static AudioStreamAssignment Resolve(IEnumerable<string>? availableStreams, IEnumerable<string>? requested, int participants)
        {
            List<string> available = (availableStreams ?? Enumerable.Empty<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(NaturalKey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(name => name, StringComparer.Ordinal)
                .ToList();
            List<string> instruction = (requested ?? Enumerable.Empty<string>())
                .Select(entry => (entry ?? string.Empty).Trim())
                .Where(entry => entry.Length > 0)
                .ToList();
            bool isExplicit = instruction.Count > 0;

            AudioStreamAssignment Failed(string problem) => new AudioStreamAssignment(available, new List<string>(), isExplicit, problem);

            if (participants <= 0)
            {
                return Failed("there is no participant");
            }

            if (available.Count == 0)
            {
                return Failed("the session has no audio stream");
            }

            if (!isExplicit)
            {
                return available.Count == participants
                    ? new AudioStreamAssignment(available, available.ToList(), false, string.Empty)
                    : Failed($"the session has {available.Count} audio streams for {participants} participants: the stream of each participant must be given, by id or by name");
            }

            if (instruction.Count != participants)
            {
                return Failed($"{instruction.Count} audio streams are given ({string.Join(", ", instruction)}) for {participants} participants: one per participant is needed");
            }

            var assigned = new List<string>();
            foreach (string entry in instruction)
            {
                // A name first: a stream may be named by a number.
                string? stream = available.FirstOrDefault(name => name == entry);
                if (stream == null && int.TryParse(entry, out int id) && id >= 1 && id <= available.Count)
                {
                    stream = available[id - 1];
                }

                if (stream == null)
                {
                    return Failed($"\"{entry}\" is neither the id (1 to {available.Count}) nor the name of an audio stream of the session");
                }

                if (assigned.Contains(stream))
                {
                    return Failed($"the audio stream {stream} is given to two participants");
                }

                assigned.Add(stream);
            }

            return new AudioStreamAssignment(available, assigned, true, string.Empty);
        }

        /// <summary>The id of a stream of <see cref="Available"/>.</summary>
        /// <param name="stream">Name of the stream.</param>
        /// <returns>The id, from 1; 0 for a stream that is not available.</returns>
        public int IdOf(string stream)
        {
            for (int i = 0; i < this.Available.Count; i++)
            {
                if (this.Available[i] == stream)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        /// <summary>The stream of each participant, for a log: "participant 1 = Audio_User_red (audio 1), participant 2 = ...".</summary>
        /// <returns>The description; empty when the assignment is not resolved.</returns>
        public string Describe()
            => string.Join(", ", this.Assigned.Select((stream, index) => $"participant {index + 1} = {stream} (audio {this.IdOf(stream)})"));

        // "Audio_10" after "Audio_2": the numbers of a name are compared as numbers.
        private static string NaturalKey(string name) => Regex.Replace(name, @"\d+", number => number.Value.PadLeft(12, '0'));
    }
}
