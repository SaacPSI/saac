// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.CollaborationIndices.Verbal;

namespace CollaborationIndices.Tests
{
    /// <summary>The audio stream of each participant, whatever the streams are named.</summary>
    [TestClass]
    public class AudioStreamAssignmentTests
    {
        [TestMethod]
        public void WithoutInstruction_TheStreamsAreTakenInTheOrderOfTheirIds()
        {
            // Named by colour, listed in any order, one of them met in two stores.
            AudioStreamAssignment assignment = AudioStreamAssignment.Resolve(new[] { "Audio_User_yellow", "Audio_User_red", "Audio_User_yellow" }, null, 2);

            Assert.IsTrue(assignment.IsResolved);
            Assert.IsFalse(assignment.IsExplicit);
            CollectionAssert.AreEqual(new[] { "Audio_User_red", "Audio_User_yellow" }, assignment.Available.ToArray());
            CollectionAssert.AreEqual(new[] { "Audio_User_red", "Audio_User_yellow" }, assignment.Assigned.ToArray());
            Assert.AreEqual(2, assignment.IdOf("Audio_User_yellow"));
            Assert.AreEqual("participant 1 = Audio_User_red (audio 1), participant 2 = Audio_User_yellow (audio 2)", assignment.Describe());
        }

        [TestMethod]
        public void Ids_FollowTheNumbersOfTheNames()
        {
            // "Audio_10" comes after "Audio_2", which the alphabetical order would not give.
            string[] streams = Enumerable.Range(1, 11).Select(i => $"Audio_{i}").Reverse().ToArray();
            AudioStreamAssignment assignment = AudioStreamAssignment.Resolve(streams, new string[0], 11);

            CollectionAssert.AreEqual(Enumerable.Range(1, 11).Select(i => $"Audio_{i}").ToArray(), assignment.Assigned.ToArray());
            Assert.AreEqual(10, assignment.IdOf("Audio_10"));
        }

        [TestMethod]
        public void AnInstruction_GivesTheStreamOfEachParticipant_ByIdOrByName()
        {
            string[] streams = { "Audio_User_red", "Audio_User_yellow", "Room" };

            AudioStreamAssignment byId = AudioStreamAssignment.Resolve(streams, new[] { " 2 ", "1" }, 2);
            Assert.IsTrue(byId.IsResolved && byId.IsExplicit);
            CollectionAssert.AreEqual(new[] { "Audio_User_yellow", "Audio_User_red" }, byId.Assigned.ToArray());
            Assert.AreEqual("participant 1 = Audio_User_yellow (audio 2), participant 2 = Audio_User_red (audio 1)", byId.Describe());

            AudioStreamAssignment byName = AudioStreamAssignment.Resolve(streams, new[] { "Audio_User_yellow", "", "Audio_User_red" }, 2);
            CollectionAssert.AreEqual(byId.Assigned.ToArray(), byName.Assigned.ToArray());

            AudioStreamAssignment mixed = AudioStreamAssignment.Resolve(streams, new[] { "Room", "1" }, 2);
            CollectionAssert.AreEqual(new[] { "Room", "Audio_User_red" }, mixed.Assigned.ToArray());
        }

        [TestMethod]
        public void AStreamNamedByANumber_IsFoundByItsNameFirst()
        {
            // Ids: "1" is 1, "2" is 2, "Extra" is 3. The entry "2" is the stream named 2, not a rank to look up.
            AudioStreamAssignment assignment = AudioStreamAssignment.Resolve(new[] { "2", "Extra", "1" }, new[] { "2", "3" }, 2);

            CollectionAssert.AreEqual(new[] { "1", "2", "Extra" }, assignment.Available.ToArray());
            CollectionAssert.AreEqual(new[] { "2", "Extra" }, assignment.Assigned.ToArray());
        }

        [TestMethod]
        public void NamesThatOnlyDifferByTheirCase_AreDifferentStreams()
        {
            AudioStreamAssignment assignment = AudioStreamAssignment.Resolve(new[] { "Audio_User_yellow", "Audio_User_Yellow" }, null, 2);

            Assert.IsTrue(assignment.IsResolved);
            Assert.AreEqual(2, assignment.Available.Count);
        }

        [TestMethod]
        public void WhatCannotBeResolved_IsNotGuessed()
        {
            string[] streams = { "Audio_User_red", "Audio_User_yellow", "Room" };

            // More streams than participants, and nothing says which ones are theirs.
            AudioStreamAssignment tooMany = AudioStreamAssignment.Resolve(streams, null, 2);
            Assert.IsFalse(tooMany.IsResolved);
            Assert.AreEqual(0, tooMany.Assigned.Count);
            Assert.AreEqual(3, tooMany.Available.Count, "The streams are still listed, with their ids.");
            StringAssert.Contains(tooMany.Problem, "3 audio streams for 2 participants");

            StringAssert.Contains(AudioStreamAssignment.Resolve(streams, null, 4).Problem, "3 audio streams for 4 participants");
            StringAssert.Contains(AudioStreamAssignment.Resolve(streams, new[] { "1" }, 2).Problem, "one per participant");
            StringAssert.Contains(AudioStreamAssignment.Resolve(streams, new[] { "1", "4" }, 2).Problem, "\"4\" is neither the id (1 to 3) nor the name");
            StringAssert.Contains(AudioStreamAssignment.Resolve(streams, new[] { "1", "Nope" }, 2).Problem, "\"Nope\"");
            StringAssert.Contains(AudioStreamAssignment.Resolve(streams, new[] { "1", "Audio_User_red" }, 2).Problem, "given to two participants");
            StringAssert.Contains(AudioStreamAssignment.Resolve(new string[0], null, 2).Problem, "no audio stream");
            StringAssert.Contains(AudioStreamAssignment.Resolve(null, null, 2).Problem, "no audio stream");
            StringAssert.Contains(AudioStreamAssignment.Resolve(streams, null, 0).Problem, "no participant");
        }
    }
}
