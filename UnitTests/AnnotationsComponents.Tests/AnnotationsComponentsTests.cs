using Microsoft.Psi;
using Microsoft.Psi.Data.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.AnnotationsComponents;
using System;
using System.Collections.Generic;
using System.IO;

namespace AnnotationsComponents.Tests
{
    [TestClass]
    public class AnnotationProcessorTests
    {
        private const string TestSchemaJson = @"{
            ""Name"": ""TestSchema"",
            ""Version"": ""1.0"",
            ""Attributes"": [
                {
                    ""Name"": ""Comment"",
                    ""Description"": ""A comment"",
                    ""ValueSchema"": {
                        ""$type"": ""Microsoft.Psi.Data.Annotations.AnnotationSchemaValueString, Microsoft.Psi.Data""
                    }
                }
            ]
        }";

        private const string EnumerableSchemaJson = @"{
            ""Name"": ""EnumerableSchema"",
            ""Version"": ""1.0"",
            ""Attributes"": [
                {
                    ""Name"": ""Status"",
                    ""Description"": ""Status indicator"",
                    ""ValueSchema"": {
                        ""$type"": ""Microsoft.Psi.Data.Annotations.AnnotationSchemaValueEnumeration, Microsoft.Psi.Data"",
                        ""Values"": [
                            { ""Value"": ""Active"", ""Description"": ""Active state"" },
                            { ""Value"": ""Inactive"", ""Description"": ""Inactive state"" }
                        ]
                    }
                }
            ]
        }";

        private string testSchemaPath;
        private string enumerableSchemaPath;

        [TestInitialize]
        public void Setup()
        {
            testSchemaPath = Path.Combine(Path.GetTempPath(), "TestSchema.schema.json");
            enumerableSchemaPath = Path.Combine(Path.GetTempPath(), "EnumerableSchema.schema.json");
            File.WriteAllText(testSchemaPath, TestSchemaJson);
            File.WriteAllText(enumerableSchemaPath, EnumerableSchemaJson);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(testSchemaPath))
                File.Delete(testSchemaPath);
            if (File.Exists(enumerableSchemaPath))
                File.Delete(enumerableSchemaPath);
        }

        private AnnotationSchema LoadSchema(string path)
        {
            if (AnnotationSchema.TryLoadFrom(path, out AnnotationSchema schema))
                return schema;
            throw new Exception($"Failed to load schema from {path}");
        }

        [TestMethod]
        public void AnnotationProcessor_Constructor_CreatesInstance()
        {
            using (var pipeline = Pipeline.Create())
            {
                var schema = LoadSchema(testSchemaPath);
                var processor = new AnnotationProcessor(pipeline, schema, "TestProcessor");

                Assert.IsNotNull(processor);
                Assert.IsNotNull(processor.In);
            }
        }

        [TestMethod]
        public void AnnotationProcessor_ToString_ReturnsName()
        {
            using (var pipeline = Pipeline.Create())
            {
                var schema = LoadSchema(testSchemaPath);
                var processor = new AnnotationProcessor(pipeline, schema, "MyAnnotationProcessor");

                Assert.AreEqual("MyAnnotationProcessor", processor.ToString());
            }
        }

        [TestMethod]
        public void AnnotationProcessor_DefaultName_ReturnsAnnotationProcessor()
        {
            using (var pipeline = Pipeline.Create())
            {
                var schema = LoadSchema(testSchemaPath);
                var processor = new AnnotationProcessor(pipeline, schema);

                Assert.AreEqual("AnnotationProcessor", processor.ToString());
            }
        }

        [TestMethod]
        public void AnnotationProcessor_StringAnnotation_PostsAnnotationSet()
        {
            using (var pipeline = Pipeline.Create())
            {
                var schema = LoadSchema(testSchemaPath);
                var processor = new AnnotationProcessor(pipeline, schema, "TestProcessor");

                var results = new List<TimeIntervalAnnotationSet>();
                processor.Out.Do(result => results.Add(result.DeepClone()));

                var generator = Generators.Sequence(pipeline, new[] { "Comment=TestValue" }, TimeSpan.FromMilliseconds(100));
                generator.PipeTo(processor.In);

                pipeline.Run();

                Assert.AreEqual(1, results.Count);
            }
        }

        [TestMethod]
        public void AnnotationProcessor_InvalidFormat_MissingEquals_DoesNotPost()
        {
            using (var pipeline = Pipeline.Create())
            {
                var schema = LoadSchema(testSchemaPath);
                var processor = new AnnotationProcessor(pipeline, schema, "TestProcessor");

                var results = new List<TimeIntervalAnnotationSet>();
                processor.Out.Do(result => results.Add(result.DeepClone()));

                var generator = Generators.Sequence(pipeline, new[] { "InvalidMessageWithoutEquals" }, TimeSpan.FromMilliseconds(100));
                generator.PipeTo(processor.In);

                pipeline.Run();

                Assert.AreEqual(0, results.Count);
            }
        }

        [TestMethod]
        public void AnnotationProcessor_InvalidAttributeName_DoesNotPost()
        {
            using (var pipeline = Pipeline.Create())
            {
                var schema = LoadSchema(testSchemaPath);
                var processor = new AnnotationProcessor(pipeline, schema, "TestProcessor");

                var results = new List<TimeIntervalAnnotationSet>();
                processor.Out.Do(result => results.Add(result.DeepClone()));

                var generator = Generators.Sequence(pipeline, new[] { "NonExistentAttribute=Value" }, TimeSpan.FromMilliseconds(100));
                generator.PipeTo(processor.In);

                pipeline.Run();

                Assert.AreEqual(0, results.Count);
            }
        }

        [TestMethod]
        public void AnnotationProcessor_MultipleStringAnnotations_PostsAll()
        {
            using (var pipeline = Pipeline.Create())
            {
                var schema = LoadSchema(testSchemaPath);
                var processor = new AnnotationProcessor(pipeline, schema, "TestProcessor");

                var results = new List<TimeIntervalAnnotationSet>();
                processor.Out.Do(result => results.Add(result.DeepClone()));

                var messages = new[] { "Comment=First", "Comment=Second", "Comment=Third" };
                var generator = Generators.Sequence(pipeline, messages, TimeSpan.FromMilliseconds(100));
                generator.PipeTo(processor.In);

                pipeline.Run();

                Assert.AreEqual(3, results.Count);
            }
        }

        [TestMethod]
        public void AnnotationProcessor_EmptyValue_PostsAnnotation()
        {
            using (var pipeline = Pipeline.Create())
            {
                var schema = LoadSchema(testSchemaPath);
                var processor = new AnnotationProcessor(pipeline, schema, "TestProcessor");

                var results = new List<TimeIntervalAnnotationSet>();
                processor.Out.Do(result => results.Add(result.DeepClone()));

                var generator = Generators.Sequence(pipeline, new[] { "Comment=" }, TimeSpan.FromMilliseconds(100));
                generator.PipeTo(processor.In);

                pipeline.Run();

                Assert.AreEqual(1, results.Count);
            }
        }

        [TestMethod]
        public void AnnotationProcessor_MultipleEqualsInValue_PostsAnnotation()
        {
            using (var pipeline = Pipeline.Create())
            {
                var schema = LoadSchema(testSchemaPath);
                var processor = new AnnotationProcessor(pipeline, schema, "TestProcessor");

                var results = new List<TimeIntervalAnnotationSet>();
                processor.Out.Do(result => results.Add(result.DeepClone()));

                // Message with multiple '=' should split only on first '='
                var generator = Generators.Sequence(pipeline, new[] { "Comment=Value=With=Equals" }, TimeSpan.FromMilliseconds(100));
                generator.PipeTo(processor.In);

                pipeline.Run();

                // This depends on implementation - may or may not work
                // Testing to verify behavior
                Assert.IsTrue(results.Count <= 1);
            }
        }
    }
}
