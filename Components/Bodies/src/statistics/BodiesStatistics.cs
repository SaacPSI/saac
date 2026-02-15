// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies.Statistics
{
    using System.IO;
    using MathNet.Numerics.Statistics;
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;

    /// <summary>
    /// Component that collects and calculates statistics on body bone lengths.
    /// </summary>
    public class BodiesStatistics : IConsumer<List<SimplifiedBody>>, IDisposable
    {
        private readonly BodiesStatisticsConfiguration configuration;
        private readonly Dictionary<uint, StatisticBody> data = new Dictionary<uint, StatisticBody>();
        private readonly string name;
        private string statsCount = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="BodiesStatistics"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">Optional configuration for statistics collection.</param>
        /// <param name="name">The name of the component.</param>
        /// <param name="defaultDeliveryPolicy">Optional default delivery policy.</param>
        public BodiesStatistics(Pipeline pipeline, BodiesStatisticsConfiguration? configuration, string name = nameof(BodiesStatistics), DeliveryPolicy? defaultDeliveryPolicy = null)
        {
            this.name = name;
            this.configuration = configuration ?? new BodiesStatisticsConfiguration();
            this.In = pipeline.CreateReceiver<List<SimplifiedBody>>(this, this.Process, $"{name}-In");
        }

        /// <summary>
        /// Gets the receiver for input simplified bodies.
        /// </summary>
        public Receiver<List<SimplifiedBody>> In { get; }

        /// <summary>
        /// Disposes the component and writes collected statistics to file.
        /// </summary>
        public void Dispose()
        {
            this.statsCount = "body_id;bone_id;count;mean;std_dev;var\n";
            foreach (var body in this.data)
            {
                foreach (var bone in body.Value.BonesValues)
                {
                    var std = bone.Value.MeanStandardDeviation();
                    var variance = bone.Value.MeanVariance();
                    string statis = body.Key.ToString() + ";" + bone.Key.Item1.ToString() + "-" + bone.Key.Item2.ToString() + ";" + bone.Value.Count.ToString() + ";" + std.Item1.ToString() + ";" + std.Item2.ToString() + ";" + variance.Item2.ToString();
                    this.statsCount += statis + "\n";
                }

                this.statsCount += "\n";
            }

            File.WriteAllText(this.configuration.StoringPath, this.statsCount);
        }

        /// <summary>
        /// Processes a list of simplified bodies and collects bone length statistics.
        /// </summary>
        /// <param name="bodies">The list of simplified bodies.</param>
        /// <param name="envelope">The message envelope.</param>
        private void Process(List<SimplifiedBody> bodies, Envelope envelope)
        {
            foreach (SimplifiedBody body in bodies)
            {
                if (!this.data.ContainsKey(body.Id))
                {
                    this.data.Add(body.Id, new StatisticBody());
                }

                foreach (var bone in AzureKinectBody.Bones)
                {
                    if (body.Joints[bone.ParentJoint].Item1 >= this.configuration.ConfidenceLevel && body.Joints[bone.ChildJoint].Item1 >= this.configuration.ConfidenceLevel)
                    {
                        this.data[body.Id].BonesValues[bone].Add(MathNet.Numerics.Distance.Euclidean(body.Joints[bone.ParentJoint].Item2.ToVector(), body.Joints[bone.ChildJoint].Item2.ToVector()));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Represents statistical data collected for a single body.
    /// </summary>
    public class StatisticBody
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatisticBody"/> class.
        /// </summary>
        public StatisticBody()
        {
            foreach (var bone in AzureKinectBody.Bones)
            {
                this.BonesValues.Add(bone, new List<double>());
            }
        }

        /// <summary>
        /// Gets the dictionary of bone length measurements.
        /// </summary>
        public Dictionary<(JointId, JointId), List<double>> BonesValues { get; } = new Dictionary<(JointId, JointId), List<double>>();
    }
}
