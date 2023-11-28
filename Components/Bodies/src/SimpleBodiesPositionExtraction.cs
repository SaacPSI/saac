using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.AzureKinect;
using MathNet.Spatial.Euclidean;
using nuitrack;

namespace Bodies
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

        private SimpleBodiesPositionExtractionConfiguration Configuration { get; }

        public SimpleBodiesPositionExtraction(Pipeline parent, SimpleBodiesPositionExtractionConfiguration? configuration = null)
        {
            Configuration = configuration ?? new SimpleBodiesPositionExtractionConfiguration();
            InBodiesNuitrack = parent.CreateReceiver<List<Skeleton>>(parent, Process, nameof(InBodiesNuitrack));
            InBodiesAzure = parent.CreateReceiver<List<AzureKinectBody>>(parent, Process, nameof(InBodiesAzure));
            InBodiesSimplified = parent.CreateReceiver<List<SimplifiedBody>>(parent, Process, nameof(InBodiesSimplified));
            Out = parent.CreateEmitter<Dictionary<uint, Vector3D>>(this, nameof(Out));
        }

        private void Process(List<Skeleton> bodies, Envelope envelope)
        {
            Dictionary<uint, Vector3D> skeletons = new Dictionary<uint, Vector3D>();

            foreach (var skeleton in bodies)
                skeletons.Add((uint)skeleton.ID, Helpers.Helpers.NuitrackToMathNet(skeleton.GetJoint(Configuration.NuitrackJointAsPosition).Real));
            Out.Post(skeletons, envelope.OriginatingTime);
        }

        private void Process(List<AzureKinectBody> bodies, Envelope envelope)
        {
            Dictionary<uint, Vector3D> skeletons = new Dictionary<uint, Vector3D>();

            foreach (var skeleton in bodies)
                skeletons.Add(skeleton.TrackingId, skeleton.Joints[Configuration.GeneralJointAsPosition].Pose.Origin.ToVector3D());
            Out.Post(skeletons, envelope.OriginatingTime);
        }

        private void Process(List<SimplifiedBody> bodies, Envelope envelope)
        {
            Dictionary<uint, Vector3D> skeletons = new Dictionary<uint, Vector3D>();

            foreach (var skeleton in bodies)
                if(!skeletons.ContainsKey(skeleton.Id))
                    skeletons.Add(skeleton.Id, skeleton.Joints[Configuration.GeneralJointAsPosition].Item2);
            Out.Post(skeletons, envelope.OriginatingTime);
        }
    }
}
