// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using nuitrack;

    /// <summary>
    /// Simple component that extract the position of a joint from bodies given.
    /// See SimpleBodiesPositionExtractionConfiguration for parameters details.
    /// </summary>
    public class SimpleBodiesPositionExtraction : IProducer<Dictionary<uint, Vector3D>>
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<uint, Vector3D>> Out { get; private set; }

        /// <summary>
        /// Gets the nuitrack connector of lists of currently tracked bodies.
        /// </summary>
        public Receiver<List<Skeleton>> InBodiesNuitrack;

        /// <summary>
        /// Gets the azure connector of lists of currently tracked bodies.
        /// </summary>
        public Receiver<List<AzureKinectBody>> InBodiesAzure;

        /// <summary>
        /// Gets the azure connector of lists of currently tracked bodies.
        /// </summary>
        public Receiver<List<SimplifiedBody>> InBodiesSimplified;

        private SimpleBodiesPositionExtractionConfiguration configuration;
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleBodiesPositionExtraction"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">Optional configuration for position extraction.</param>
        /// <param name="name">Optional name for the component.</param>
        public SimpleBodiesPositionExtraction(Pipeline parent, SimpleBodiesPositionExtractionConfiguration? configuration = null, string name = nameof(SimpleBodiesPositionExtraction))
        {
            this.configuration = configuration ?? new SimpleBodiesPositionExtractionConfiguration();
            this.InBodiesNuitrack = parent.CreateReceiver<List<Skeleton>>(parent, this.Process, $"{name}-InBodiesNuitrack");
            this.InBodiesAzure = parent.CreateReceiver<List<AzureKinectBody>>(parent, this.Process, $"{name}-InBodiesAzure");
            this.InBodiesSimplified = parent.CreateReceiver<List<SimplifiedBody>>(parent, this.Process, $"{name}-InBodiesSimplified");
            this.Out = parent.CreateEmitter<Dictionary<uint, Vector3D>>(this, $"{name}-Out");
            this.name = name;
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process(List<Skeleton> bodies, Envelope envelope)
        {
            Dictionary<uint, Vector3D> skeletons = new Dictionary<uint, Vector3D>();

            foreach (var skeleton in bodies)
            {
                skeletons.Add((uint)skeleton.ID, Helpers.Helpers.NuitrackToMathNet(skeleton.GetJoint(this.configuration.NuitrackJointAsPosition).Real));
            }

            this.Out.Post(skeletons, envelope.OriginatingTime);
        }

        private void Process(List<AzureKinectBody> bodies, Envelope envelope)
        {
            Dictionary<uint, Vector3D> skeletons = new Dictionary<uint, Vector3D>();

            foreach (var skeleton in bodies)
            {
                skeletons.Add(skeleton.TrackingId, skeleton.Joints[this.configuration.GeneralJointAsPosition].Pose.Origin.ToVector3D());
            }

            this.Out.Post(skeletons, envelope.OriginatingTime);
        }

        private void Process(List<SimplifiedBody> bodies, Envelope envelope)
        {
            Dictionary<uint, Vector3D> skeletons = new Dictionary<uint, Vector3D>();

            foreach (var skeleton in bodies)
            {
                if (!skeletons.ContainsKey(skeleton.Id))
                {
                    skeletons.Add(skeleton.Id, skeleton.Joints[this.configuration.GeneralJointAsPosition].Item2);
                }
            }

            this.Out.Post(skeletons, envelope.OriginatingTime);
        }
    }
}
