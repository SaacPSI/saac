// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Psi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.CollaborationIndices;

namespace CollaborationIndices.Tests
{
    /// <summary>
    /// What Build refuses, and what it says when it does.
    /// </summary>
    [TestClass]
    public class ValidationTests
    {
        [TestMethod]
        public void ValidDeclaration_HasNoError()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                CollaborationIndicesBuilder builder = Valid(pipeline);

                Assert.AreEqual(0, builder.Validate().Count);
                Assert.IsNotNull(builder.Build());
            }
        }

        [TestMethod]
        public void NoIndicator_IsRejected()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                var builder = new CollaborationIndicesBuilder(pipeline).WithParticipants(0, 1);

                AssertRejected(builder, "No indicator declared");
            }
        }

        [TestMethod]
        public void WindowMustBePositive()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                AssertRejected(Valid(pipeline).WithWindow(TimeSpan.Zero), "sliding window must be strictly positive");
                AssertRejected(Valid(pipeline).WithWindow(TimeSpan.FromSeconds(-30)), "sliding window must be strictly positive");
                AssertRejected(
                    Valid(pipeline).AddIndicator(Indicators.Synchrony, options => options.WindowDuration = TimeSpan.Zero),
                    "Indicator 'Synchrony': its own window must be strictly positive");
            }
        }

        [TestMethod]
        public void ComputationIntervalMustBePositive()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                AssertRejected(Valid(pipeline).WithComputationInterval(TimeSpan.Zero), "computation interval must be strictly positive");
            }
        }

        [TestMethod]
        public void ParticipantsAreRequiredAndUnique()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                AssertRejected(Valid(pipeline).WithParticipants(), "No participant declared");
                AssertRejected(Valid(pipeline).WithParticipants(0, 1, 1), "Participant 1 is declared 2 times");
            }
        }

        [TestMethod]
        public void SameIndicatorTwice_IsRejected_UnlessRenamed()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                AssertRejected(Valid(pipeline).AddIndicator(Indicators.Movement), "Indicator 'Movement' is declared 2 times");

                // Declared by name or by catalogue entry, it is the same indicator.
                AssertRejected(Valid(pipeline).AddIndicator("Movement"), "Indicator 'Movement' is declared 2 times");

                SlidingAverageComputation indices = Valid(pipeline)
                    .AddIndicator(Indicators.Movement, options =>
                    {
                        options.Name = "HeadMovement";
                        options.BodyParts = new List<string> { BodyPartNames.Head };
                    })
                    .Build();

                Assert.AreEqual(2, indices.Indicators.OfType<PhysicalActivityIndicator>().Count());
            }
        }

        [TestMethod]
        public void MissingDependency_IsRejected()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                AssertRejected(
                    Valid(pipeline).AddIndicator(Indicators.SpeechEquality),
                    "Indicator 'SpeechEquality' needs indicator 'VerbalParticipation', which is not declared");

                AssertRejected(
                    Valid(pipeline).AddIndicator(Indicators.Equality),
                    "Indicator 'Equality' needs the name of the indicator it compares");
            }
        }

        [TestMethod]
        public void DependencyOfTheWrongKind_IsRejected()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                CollaborationIndicesBuilder builder = Valid(pipeline)
                    .AddIndicator(Indicators.Synchrony)
                    .AddIndicator(Indicators.Equality, options => options.Source = IndexNames.Synchrony);

                AssertRejected(builder, "cannot use 'Synchrony' as its source");
            }
        }

        [TestMethod]
        public void UnknownIndicatorType_IsRejected_WithTheKnownOnes()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                CollaborationIndicesConfigurationException exception = AssertRejected(Valid(pipeline).AddIndicator("Synchronie"), "Unknown indicator type 'Synchronie'");

                StringAssert.Contains(exception.Message, "Synchrony", "The message lists the types that do exist.");
            }
        }

        [TestMethod]
        public void UnknownOption_IsRejected_WithTheValidOnes()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                var options = new Dictionary<string, object> { { "SubsetSise", 3 } };

                CollaborationIndicesConfigurationException exception = AssertRejected(Valid(pipeline).AddIndicator("Synchrony", options), "Indicator 'Synchrony': invalid options");

                StringAssert.Contains(exception.Message, "SubsetSise");
                StringAssert.Contains(exception.Message, "SubsetSize", "The message lists the options that do exist.");
            }
        }

        [TestMethod]
        public void OptionOfTheWrongType_IsRejected()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                var options = new Dictionary<string, object> { { "SubsetSize", "three" } };

                AssertRejected(Valid(pipeline).AddIndicator("Synchrony", options), "Indicator 'Synchrony': invalid options");
            }
        }

        [TestMethod]
        public void IndicatorConstraints_AreChecked()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                AssertRejected(
                    new CollaborationIndicesBuilder(pipeline).WithParticipants(0).AddIndicator(Indicators.Synchrony),
                    "Indicator 'Synchrony' needs at least 2 participants, 1 declared");

                AssertRejected(
                    Valid(pipeline).AddIndicator(Indicators.Synchrony, options => options.SubsetSize = 2),
                    "SubsetSize must be 0 (no sub-group) or at least 3");

                AssertRejected(
                    new CollaborationIndicesBuilder(pipeline).WithParticipants(0, 1).AddIndicator(Indicators.Movement, options => options.BodyParts = new List<string>()),
                    "Indicator 'Movement' needs at least one body part");
            }
        }

        [TestMethod]
        public void ExportNeedsADestination()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                AssertRejected(Valid(pipeline).WithCsvExport((TextWriter)null), "CSV export needs a writer or a file path");
                AssertRejected(Valid(pipeline).WithCsvExport(string.Empty), "CSV export needs a writer or a file path");
            }
        }

        [TestMethod]
        public void ScoreNeedsNamedDistinctDimensions()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                AssertRejected(Valid(pipeline).WithCollaborationScore(new ScoreDimension[0]), "score needs at least one dimension");

                var twice = new[]
                {
                    new ScoreDimension { Name = "Engagement", IndexNames = new List<string> { IndexNames.Movement } },
                    new ScoreDimension { Name = "Engagement", IndexNames = new List<string> { IndexNames.Synchrony } },
                };
                AssertRejected(Valid(pipeline).WithCollaborationScore(twice), "score dimension 'Engagement' is declared 2 times");
            }
        }

        [TestMethod]
        public void OnlyOneClockCanBeDeclared()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                IProducer<bool> ticks = Generators.Return(pipeline, true);

                AssertRejected(Valid(pipeline).WithInternalClock().WithClock(ticks), "Several clocks are declared");
            }
        }

        [TestMethod]
        public void EveryProblemIsReportedAtOnce()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                CollaborationIndicesBuilder builder = new CollaborationIndicesBuilder(pipeline)
                    .WithParticipants(0, 0)
                    .WithWindow(TimeSpan.Zero)
                    .AddIndicator(Indicators.SpeechEquality)
                    .AddIndicator("NoSuchIndicator");

                CollaborationIndicesConfigurationException exception = Assert.ThrowsException<CollaborationIndicesConfigurationException>(() => builder.Build());

                Assert.AreEqual(4, exception.Errors.Count, exception.Message);
                StringAssert.Contains(exception.Message, "4 problems");
                StringAssert.Contains(exception.Message, "sliding window");
                StringAssert.Contains(exception.Message, "Participant 0 is declared 2 times");
                StringAssert.Contains(exception.Message, "Unknown indicator type 'NoSuchIndicator'");
                StringAssert.Contains(exception.Message, "needs indicator 'VerbalParticipation'");

                CollectionAssert.AreEqual(exception.Errors.ToList(), builder.Validate().ToList(), "Validate reports the same problems without throwing.");
            }
        }

        [TestMethod]
        public void BuildSet_RejectsDuplicateAndCollidingWindows()
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                CollaborationIndicesConfigurationException none = Assert.ThrowsException<CollaborationIndicesConfigurationException>(() => Valid(pipeline).BuildSet());
                StringAssert.Contains(none.Message, "No window declared");

                CollaborationIndicesConfigurationException twice = Assert.ThrowsException<CollaborationIndicesConfigurationException>(
                    () => Valid(pipeline).BuildSet(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20)));
                StringAssert.Contains(twice.Message, "declared 2 times");

                CollaborationIndicesConfigurationException collision = Assert.ThrowsException<CollaborationIndicesConfigurationException>(
                    () => Valid(pipeline).BuildSet(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2.5)));
                StringAssert.Contains(collision.Message, "would both be named");
            }
        }

        [TestMethod]
        public void InvalidJson_IsReportedAsAConfigurationProblem()
        {
            CollaborationIndicesConfigurationException misspelled = Assert.ThrowsException<CollaborationIndicesConfigurationException>(
                () => CollaborationIndicesConfiguration.FromJson(@"{ ""WindowDuratoin"": ""00:00:30"" }"));
            StringAssert.Contains(misspelled.Message, "WindowDuratoin");

            Assert.ThrowsException<CollaborationIndicesConfigurationException>(() => CollaborationIndicesConfiguration.FromJson("{ not json"));
            Assert.ThrowsException<ArgumentException>(() => CollaborationIndicesConfiguration.FromJson(string.Empty));
        }

        [TestMethod]
        public void NullArguments_AreRejectedImmediately()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new CollaborationIndicesBuilder(null));

            using (Pipeline pipeline = Pipeline.Create())
            {
                Assert.ThrowsException<ArgumentNullException>(() => CollaborationIndicesBuilder.FromConfiguration(pipeline, null));
                Assert.ThrowsException<ArgumentNullException>(() => new CollaborationIndicesBuilder(pipeline).WithClock(null));
            }
        }

        private static CollaborationIndicesBuilder Valid(Pipeline pipeline)
            => new CollaborationIndicesBuilder(pipeline)
                .WithParticipants(0, 1, 2)
                .WithWindow(TimeSpan.FromSeconds(30))
                .AddIndicator(Indicators.Movement);

        private static CollaborationIndicesConfigurationException AssertRejected(CollaborationIndicesBuilder builder, string expectedMessage)
        {
            CollaborationIndicesConfigurationException exception = Assert.ThrowsException<CollaborationIndicesConfigurationException>(() => builder.Build());

            StringAssert.Contains(exception.Message, expectedMessage);
            Assert.IsTrue(exception.Errors.Count >= 1);
            return exception;
        }
    }
}
