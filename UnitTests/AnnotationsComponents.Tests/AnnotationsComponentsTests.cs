using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Psi;
using Microsoft.Psi.Data.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SAAC.AnnotationsComponents;

namespace AnnotationsComponents.Tests
{
    [TestClass]
    public class AnnotationProcessorTests
    {
        private string testSchemaPath;

        public static string GetSolutionPath()
        {
            string currentDirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            while (currentDirPath != null)
            {
                var solutionFile = Directory.GetFiles(currentDirPath)
                    .FirstOrDefault(f => Path.GetExtension(f).Equals(".sln", StringComparison.OrdinalIgnoreCase));
                if (solutionFile != null)
                    return Path.GetDirectoryName(solutionFile);
                currentDirPath = Path.GetDirectoryName(currentDirPath);
            }
            throw new FileNotFoundException("Aucun fichier .sln trouvé.");
        }

        [TestInitialize]
        public void Setup()
        {
            testSchemaPath = $@"{GetSolutionPath()}\Components\AnnotationsComponents\AnnotationFiles\annotation.schema.json";
        }

        [TestCleanup]
        public void Cleanup()
        {
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

                var generator = Generators.Sequence(pipeline, new[] { "Transcript=TestValue" }, TimeSpan.FromMilliseconds(100));
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

                var messages = new[] { "Transcript=First", "Transcript=Second", "Transcript=Third" };
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

                var generator = Generators.Sequence(pipeline, new[] { "Transcript=" }, TimeSpan.FromMilliseconds(100));
                generator.PipeTo(processor.In);

                pipeline.Run();

                Assert.AreEqual(1, results.Count);
            }
        }

    }
}
