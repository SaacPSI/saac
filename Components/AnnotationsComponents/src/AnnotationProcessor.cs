// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System;
using System.Collections.Generic;
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.AnnotationsComponents
{
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Data.Annotations;

    /// <summary>
    /// Component that processes annotation messages received from WebSocket connections
    /// and converts them into TimeIntervalAnnotationSet objects.
    /// </summary>
    public class AnnotationProcessor : Generator<TimeIntervalAnnotationSet>, IConsumer<string>
    {
        private static readonly List<(TimeIntervalAnnotationSet, DateTime)> Definition = new ();
        private readonly AnnotationSchema annotationSchema;
        private readonly string name;
        private Dictionary<string, TimeIntervalAnnotation> currentValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationProcessor"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to attach the component to.</param>
        /// <param name="schema">The annotation schema that defines the structure of annotations.</param>
        /// <param name="name">The name of the annotation processor.</param>
        public AnnotationProcessor(Pipeline pipeline, AnnotationSchema schema, string name = nameof(AnnotationProcessor))
            : base(pipeline, Definition.GetEnumerator(), name: name)
        {
            this.In = pipeline.CreateReceiver<string>(this, this.ProcessAnnotation, $"{name}-In");
            this.annotationSchema = schema;
            this.name = name;
            this.currentValues = new Dictionary<string, TimeIntervalAnnotation>();
        }

        /// <summary>
        /// Gets the receiver for annotation messages.
        /// </summary>
        public Receiver<string> In { get; }

        /// <inheritdoc/>>
        public override string ToString() => this.name;

        private void ProcessAnnotation(string message, Envelope envelope)
        {
            string[] firstSplit = message.Split('=');
            if (firstSplit.Length != 2)
            {
                Trace.Write($"Invalid annotation format: {message}");
                return;
            }

            if (this.annotationSchema.ContainsAttribute(firstSplit[0]) == false)
            {
                Trace.Write($"Invalid attribute name: {firstSplit[0]}");
                return;
            }

            AnnotationAttributeSchema attributeSchema = this.annotationSchema.GetAttributeSchema(firstSplit[0]);
            if (attributeSchema != null)
            {
                IEnumerableAnnotationValueSchema? enumerable = attributeSchema.ValueSchema as IEnumerableAnnotationValueSchema;
                if (enumerable == null)
                {
                    // String annotation: create an instantaneous annotation
                    TimeIntervalAnnotation newAnnotation = this.annotationSchema.CreateDefaultTimeIntervalAnnotation(new TimeInterval(envelope.OriginatingTime, envelope.OriginatingTime.AddSeconds(1)), this.name);
                    this.MergeAttributeValues(attributeSchema.CreateAttribute(firstSplit[1]), newAnnotation.AttributeValues);
                    this.Out.Post(new TimeIntervalAnnotationSet(newAnnotation), envelope.OriginatingTime);
                }
                else
                {
                    // Enumerable annotation: handle start/end markers
                    string[] secondSplit = firstSplit[1].Split('?');
                    var annotation = enumerable.GetPossibleAnnotationValues().Where((annot) => { return annot.ValueAsString == secondSplit[0]; }).ToList();
                    if (secondSplit.Length != 2 && annotation != null && annotation.Count > 0)
                    {
                        Trace.Write($"Invalid annotation format for enumerable {firstSplit[0]}: {message}");
                        return;
                    }

                    if (secondSplit[1] == "start" && this.currentValues.ContainsKey(attributeSchema.Name) == false)
                    {
                        // Start of enumerable annotation: create and store annotation with indefinite end time
                        TimeIntervalAnnotation newAnnotation = this.annotationSchema.CreateDefaultTimeIntervalAnnotation(new TimeInterval(envelope.OriginatingTime, DateTime.MaxValue), this.name);
                        this.MergeAttributeValues(attributeSchema.CreateAttribute(secondSplit[0]), newAnnotation.AttributeValues);
                        this.currentValues[attributeSchema.Name] = newAnnotation;
                    }
                    else if (secondSplit[1] == "end")
                    {
                        // End of enumerable annotation: finalize the interval and post the annotation
                        if (this.currentValues.ContainsKey(attributeSchema.Name))
                        {
                            TimeIntervalAnnotation newAnnotation = this.currentValues[attributeSchema.Name];
                            newAnnotation.Interval = new TimeInterval(newAnnotation.Interval.Left, envelope.OriginatingTime);
                            this.Out.Post(new TimeIntervalAnnotationSet(newAnnotation), envelope.OriginatingTime);
                            this.currentValues.Remove(attributeSchema.Name);
                        }
                        else
                        {
                            Trace.Write($"No start found for enumerable annotation {attributeSchema.Name}");
                        }
                    }
                }
            }
        }

        private void MergeAttributeValues(in Dictionary<string, IAnnotationValue> source, Dictionary<string, IAnnotationValue> destination)
        {
            foreach (var kvp in source)
            {
                destination[kvp.Key] = kvp.Value;
            }
        }
    }
}
