// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Psi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.CollaborationIndices;

namespace CollaborationIndices.Tests
{
    /// <summary>The configuration of a session, shared by the console applications and the server.</summary>
    [TestClass]
    public class SessionConfigurationTests
    {
        [TestMethod]
        public void Template_RoundTripsThroughJson()
        {
            CollaborationSessionConfiguration template = CollaborationSessionConfiguration.Template(participantCount: 3);
            CollaborationSessionConfiguration read = CollaborationSessionConfiguration.FromJson(template.ToJson());

            Assert.AreEqual(template.ToJson(), read.ToJson());
            CollectionAssert.AreEqual(new uint[] { 0, 1, 2 }, read.ParticipantIds);
            CollectionAssert.AreEqual(new[] { 20.0, 30.0, 45.0 }, read.Windows.Select(window => window.TotalSeconds).ToArray());
            Assert.IsTrue(read.ComputeProfiles && read.ComputeCollaborationScores);
            Assert.AreEqual(0, read.MissingProfileIndicators().Count, "The template declares everything the profiles read.");
            Assert.AreEqual(0, read.Validate().Count());
        }

        [TestMethod]
        public void Add_BringsTheSourceIndicator_Once()
        {
            var configuration = new CollaborationSessionConfiguration();
            configuration.Add(IndexNames.SpeechEquality).Add(IndexNames.TaskingMost).Add(IndexNames.SpeechEquality).Add(IndexNames.VerbalParticipation);

            CollectionAssert.AreEqual(
                new[] { IndexNames.VerbalParticipation, IndexNames.SpeechEquality, IndexNames.TaskParticipation, IndexNames.TaskingMost },
                configuration.Indicators.Select(indicator => indicator.Type).ToArray(),
                "Each indicator once, after the one it is computed from.");
        }

        [TestMethod]
        public void Validate_ListsEveryProblem()
        {
            var empty = new CollaborationSessionConfiguration { Windows = new List<TimeSpan>() };
            string[] problems = empty.Validate().ToArray();
            Assert.AreEqual(3, problems.Length, string.Join(" | ", problems));
            Assert.IsTrue(problems.Any(problem => problem.StartsWith("ParticipantIds")));
            Assert.IsTrue(problems.Any(problem => problem.StartsWith("Windows")));
            Assert.IsTrue(problems.Any(problem => problem.StartsWith("Indicators")));

            var configuration = new CollaborationSessionConfiguration
            {
                ParticipantIds = new List<uint> { 0 },
                Windows = new List<TimeSpan> { TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20), TimeSpan.Zero },
                ComputeProfiles = true,
            };
            configuration.Add(IndexNames.Movement);
            problems = configuration.Validate().ToArray();
            Assert.AreEqual(3, problems.Length, string.Join(" | ", problems));
            Assert.IsTrue(problems.Any(problem => problem.Contains("strictly positive")));
            Assert.IsTrue(problems.Any(problem => problem.Contains("declared twice")));
            Assert.IsTrue(problems.Any(problem => problem.StartsWith("ComputeProfiles")));

            using (Pipeline pipeline = Pipeline.Create())
            {
                var exception = Assert.ThrowsException<CollaborationIndicesConfigurationException>(() => configuration.CreateBuilder(pipeline));
                Assert.AreEqual(3, exception.Errors.Count, "The builder is not created from an invalid configuration.");
            }
        }

        [TestMethod]
        public void MisspelledProperty_IsAnError()
        {
            string json = CollaborationSessionConfiguration.Template().ToJson().Replace("\"ComputeProfiles\"", "\"ComputeProfile\"");
            Assert.ThrowsException<CollaborationIndicesConfigurationException>(() => CollaborationSessionConfiguration.FromJson(json));
        }

        [TestMethod]
        public void HeadPose_InTheStandardFrame_IsThatOfThePreProcessing()
        {
            const float DegreesToRadians = (float)Math.PI / 180f;
            var unity = new DetectorSettings { HeadForwardAxis = "+Y", Frame = DataFrame.Unity };
            var standard = new DetectorSettings { HeadForwardAxis = "+Y", Frame = DataFrame.Standard };
            Assert.AreEqual(Vector3.UnitY, unity.UpAxis());
            Assert.AreEqual(Vector3.UnitZ, standard.UpAxis());

            foreach (Vector3 euler in new[] { new Vector3(0, 0, 0), new Vector3(20, 75, 0), new Vector3(-35, 190, 12), new Vector3(80, -40, -170), new Vector3(5, 359, 90) })
            {
                var position = new Vector3(1.5f, 2.5f, -3.5f);

                // PositionOrientationPreProcessing.Convert: the Unity quaternion reframed for
                // the frame with Y and Z swapped, then its local +Z axis.
                Quaternion q = Quaternion.CreateFromYawPitchRoll(euler.Y * DegreesToRadians, euler.X * DegreesToRadians, euler.Z * DegreesToRadians);
                Vector3 expected = Vector3.Normalize(Vector3.Transform(Vector3.UnitZ, new Quaternion(-q.X, -q.Z, -q.Y, q.W)));

                Tuple<Vector3, Vector3> inStandard = standard.ToHeadPose(Tuple.Create(position, euler));
                Assert.AreEqual(position, inStandard.Item1, "The position is already in the frame: it is not touched.");
                Assert.IsTrue(Vector3.Distance(expected, inStandard.Item2) < 1e-5f, $"Euler {euler}: {inStandard.Item2} instead of {expected}");

                // The same direction in the Unity frame: Y and Z swapped back.
                Tuple<Vector3, Vector3> inUnity = unity.ToHeadPose(Tuple.Create(position, euler));
                Assert.IsTrue(Vector3.Distance(new Vector3(expected.X, expected.Z, expected.Y), inUnity.Item2) < 1e-5f, $"Euler {euler}, Unity frame");
            }

            Assert.AreEqual(DataFrame.Standard, CollaborationSessionConfiguration.FromJson(new CollaborationSessionConfiguration { Detectors = standard }.ToJson()).Detectors.Frame);
        }

        [TestMethod]
        public void Formations_AreTheSameInBothFrames()
        {
            // Two participants facing each other across a table, one standing and looking down,
            // the other seated and looking up. Unity frame: Y is the height.
            var a = Tuple.Create(new Vector3(0f, 1.6f, 0f), new Vector3(25f, 15f, 0f));
            var b = Tuple.Create(new Vector3(0.3f, 1.1f, 1.1f), new Vector3(-10f, 195f, 0f));
            Func<Tuple<Vector3, Vector3>, Tuple<Vector3, Vector3>> swapped = pose => Tuple.Create(new Vector3(pose.Item1.X, pose.Item1.Z, pose.Item1.Y), pose.Item2);

            var unity = new DetectorSettings { HeadForwardAxis = "+Z", Frame = DataFrame.Unity };
            var standard = new DetectorSettings { HeadForwardAxis = "+Z", Frame = DataFrame.Standard };

            using (Pipeline pipeline = Pipeline.Create("Frames"))
            {
                Func<DetectorSettings, Tuple<Vector3, Vector3>, Tuple<Vector3, Vector3>, string> classify = (settings, first, second) =>
                {
                    var detector = new FFormationDetector(pipeline, new FFormationDetectorConfiguration { ParticipantIds = new List<uint> { 0, 1 }, UpAxis = settings.UpAxis() }, $"Detector{Guid.NewGuid():N}");
                    Tuple<Vector3, Vector3> poseA = settings.ToHeadPose(first);
                    Tuple<Vector3, Vector3> poseB = settings.ToHeadPose(second);
                    return detector.Classify(poseA.Item1, poseA.Item2, poseB.Item1, poseB.Item2);
                };

                Assert.AreEqual(FormationTypes.FaceToFace, classify(unity, a, b), "Data of the Unity frame, declared as such.");
                Assert.AreEqual(FormationTypes.FaceToFace, classify(standard, swapped(a), swapped(b)), "The same scene in the standard frame, declared as such.");
                Assert.AreNotEqual(FormationTypes.FaceToFace, classify(unity, swapped(a), swapped(b)), "Declared in the wrong frame, the height is taken for a horizontal axis.");
            }
        }

        [TestMethod]
        public void CreateBuilder_DeclaresWhatTheConfigurationLists()
        {
            var configuration = new CollaborationSessionConfiguration
            {
                ParticipantIds = new List<uint> { 0, 1 },
                Windows = new List<TimeSpan> { TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30) },
            };
            configuration.Add(IndexNames.TurnTaking, new Dictionary<string, object> { { "Categories", new List<string> { IndexCategories.TurnTakingWithoutOverlap } } });
            configuration.Add(IndexNames.SpeechEquality);

            // From the object, and from the file an application would read.
            foreach (CollaborationSessionConfiguration source in new[] { configuration, CollaborationSessionConfiguration.FromJson(configuration.ToJson()) })
            {
                using (Pipeline pipeline = Pipeline.Create())
                {
                    SlidingAverageComputationSet indices = source.CreateBuilder(pipeline).BuildSet(source.Windows.ToArray());

                    var windows = new List<TimeSpan>();
                    indices.ForEach((window, instance) =>
                    {
                        windows.Add(window);
                        Assert.IsTrue(instance.Contains(IndexNames.TurnTaking) && instance.Contains(IndexNames.VerbalParticipation) && instance.Contains(IndexNames.SpeechEquality));
                        Assert.IsFalse(instance.Contains(IndexNames.Movement), "An indicator that is not declared is not built.");
                        CollectionAssert.AreEqual(
                            new[] { IndexCategories.TurnTakingWithoutOverlap },
                            instance.Get(Indicators.TurnTaking).Options.Categories,
                            "The options of the configuration reach the indicator.");
                    });

                    CollectionAssert.AreEqual(source.Windows, windows);
                }
            }
        }
    }
}
