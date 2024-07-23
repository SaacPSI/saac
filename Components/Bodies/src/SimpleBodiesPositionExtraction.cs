using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.AzureKinect;
using MathNet.Spatial.Euclidean;
using nuitrack;

namespace SAAC.Bodies
{
    /// <summary>
    /// Simple component that extract the position of a joint from bodies given.
    /// See SimpleBodiesPositionExtractionConfiguration for parameters details.
    /// </summary>
    public class SimpleBodiesPositionExtraction : IProducer<Dictionary<uint, Vector3D>>
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Dictionary<uint, Vector3D>> Out{ get; private set; }

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

        public SimpleBodiesPositionExtraction(Pipeline parent, SimpleBodiesPositionExtractionConfiguration? configuration = null, string name = nameof(SimpleBodiesPositionExtraction))
        {
            this.configuration = configuration ?? new SimpleBodiesPositionExtractionConfiguration();
            InBodiesNuitrack = parent.CreateReceiver<List<Skeleton>>(parent, Process, $"{name}-InBodiesNuitrack");
            InBodiesAzure = parent.CreateReceiver<List<AzureKinectBody>>(parent, Process, $"{name}-InBodiesAzure");
            InBodiesSimplified = parent.CreateReceiver<List<SimplifiedBody>>(parent, Process, $"{name}-InBodiesSimplified");
            Out = parent.CreateEmitter<Dictionary<uint, Vector3D>>(this, $"{name}-Out");
            this.name = name;
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process(List<Skeleton> bodies, Envelope envelope)
        {
            Dictionary<uint, Vector3D> skeletons = new Dictionary<uint, Vector3D>();

            foreach (var skeleton in bodies)
                skeletons.Add((uint)skeleton.ID, Helpers.Helpers.NuitrackToMathNet(skeleton.GetJoint(configuration.NuitrackJointAsPosition).Real));
            Out.Post(skeletons, envelope.OriginatingTime);
        }

        private void Process(List<AzureKinectBody> bodies, Envelope envelope)
        {
            Dictionary<uint, Vector3D> skeletons = new Dictionary<uint, Vector3D>();

            foreach (var skeleton in bodies)
                skeletons.Add(skeleton.TrackingId, skeleton.Joints[configuration.GeneralJointAsPosition].Pose.Origin.ToVector3D());
            Out.Post(skeletons, envelope.OriginatingTime);
        }

        private void Process(List<SimplifiedBody> bodies, Envelope envelope)
        {
            Dictionary<uint, Vector3D> skeletons = new Dictionary<uint, Vector3D>();

            foreach (var skeleton in bodies)
                if(!skeletons.ContainsKey(skeleton.Id))
                    skeletons.Add(skeleton.Id, skeleton.Joints[configuration.GeneralJointAsPosition].Item2);
            Out.Post(skeletons, envelope.OriginatingTime);
        }
    }
}
