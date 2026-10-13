// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Psi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.Groups;

namespace Groups.Tests
{
    /// <summary>
    /// The state of a group at one instant (GroupInstant): any number of participants, what
    /// is not known left out, and the state read back from a store as it was written.
    /// </summary>
    [TestClass]
    public class GroupInstantClassTests
    {
        [TestMethod]
        public void State_FindsItsPersonsAndItsPairs_AndSaysWhatItIsBuiltFrom()
        {
            GroupInstant state = Busy();
            Assert.AreEqual(1.5, state.FindPerson("2").X);
            Assert.IsNull(state.FindPerson("9"));
            Assert.AreSame(state.FindPair("1", "2"), state.FindPair("2", "1"), "A pair, in either order.");
            Assert.IsNull(state.FindPair("1", "3"), "A pair with nothing in common is not in the state.");
            Assert.IsTrue(state.Has(GroupInstant.Voice));
            Assert.IsFalse(state.Has(GroupInstant.Task), "A signal that is not named is not known.");

            // A state that was not filled answers without error.
            var empty = new GroupInstant { Persons = null, Pairs = null, Events = null, Signals = null };
            Assert.IsNull(empty.FindPerson("1"));
            Assert.IsNull(empty.FindPair("1", "2"));
            Assert.IsFalse(empty.Has(GroupInstant.Places));
        }

        [TestMethod]
        public void State_IsReadBackFromAStoreAsItWasWritten()
        {
            var written = new List<GroupInstant> { new GroupInstant(), Busy(), Busy() };
            written[2].Persons.Add(new PersonInstant { Id = "12" });
            written[2].Events.Clear();

            string folder = Path.Combine(Path.GetTempPath(), "SaacGroupInstantTests_" + Guid.NewGuid().ToString("N"));
            var read = new List<GroupInstant>();
            try
            {
                DateTime start = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
                using (Pipeline pipeline = Pipeline.Create("Write"))
                {
                    var store = PsiStore.Create(pipeline, "Instant", folder);
                    Generators.Sequence(pipeline, written.Select((state, index) => (state, start.AddSeconds(index))).ToList()).Write("GroupInstant", store);
                    pipeline.Run();
                }

                using (Pipeline pipeline = Pipeline.Create("Read"))
                {
                    var store = PsiStore.Open(pipeline, "Instant", folder);
                    store.OpenStream<GroupInstant>("GroupInstant").Do(state => read.Add(state.DeepClone()));
                    pipeline.Run(ReplayDescriptor.ReplayAll);
                }
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }

            Assert.AreEqual(written.Count, read.Count);
            for (int i = 0; i < written.Count; i++)
            {
                Assert.AreEqual(Describe(written[i]), Describe(read[i]), $"State {i}.");
            }

            StringAssert.Contains(Describe(read[1]), "1 at (0, 0) facing (1, 0) speaking looks at 2 | Cube holds Piece_3");
            StringAssert.Contains(Describe(read[1]), "JointVisualAttention 1>2 Cube");
            Assert.AreEqual(0, read[0].Persons.Count + read[0].Pairs.Count + read[0].Events.Count + read[0].Signals.Count, "An empty state comes back empty.");
        }

        // Two participants face to face who look at each other and at the cube; the first one speaks and holds a piece.
        private static GroupInstant Busy()
        {
            DateTime now = new DateTime(2026, 1, 15, 10, 0, 5, DateTimeKind.Utc);
            var state = new GroupInstant { Signals = new List<string> { GroupInstant.Formations, GroupInstant.Gaze, GroupInstant.Places, GroupInstant.Voice } };
            state.Persons.Add(new PersonInstant { Id = "1", HasPlace = true, HeadingX = 1, Speaking = true, LooksAtPersons = { "2" }, LooksAtObjects = { "Cube" }, Holds = { "Piece_3" } });
            state.Persons.Add(new PersonInstant { Id = "2", HasPlace = true, X = 1.5, HeadingX = -1, LooksAtPersons = { "1" }, LooksAtObjects = { "Cube" } });
            state.Persons.Add(new PersonInstant { Id = "3" });
            state.Pairs.Add(new PairInstant { First = "1", Second = "2", Formation = "FaceToFace", MutualGaze = true, SharedObjects = { "Cube" } });
            state.Events.Add(new InstantEvent { Category = "JointVisualAttention", Person = "1", Other = "2", Label = "Cube", Time = now.AddSeconds(-2), DetectedAt = now.AddSeconds(-1) });
            return state;
        }

        // Everything a state holds, as text.
        private static string Describe(GroupInstant state)
        {
            var lines = new List<string> { "signals " + string.Join(",", state.Signals) };
            lines.AddRange(state.Persons.Select(person =>
                $"{person.Id} {(person.HasPlace ? $"at ({person.X}, {person.Y}) facing ({person.HeadingX}, {person.HeadingY})" : "nowhere")}{(person.Speaking ? " speaking" : string.Empty)}"
                + $" looks at {string.Join(",", person.LooksAtPersons)} | {string.Join(",", person.LooksAtObjects)} holds {string.Join(",", person.Holds)}"));
            lines.AddRange(state.Pairs.Select(pair => $"{pair.First}-{pair.Second} {pair.Formation} mutual {pair.MutualGaze} shared {string.Join(",", pair.SharedObjects)}"));
            lines.AddRange(state.Events.Select(detection => $"{detection.Category} {detection.Person}>{detection.Other} {detection.Label} at {detection.Time:O} detected {detection.DetectedAt:O}"));
            return string.Join("\n", lines);
        }
    }
}
