using nuitrack;

namespace SAAC.Bodies
{
    public class SimpleBodiesPositionExtractionConfiguration
    {
        /// <summary>
        /// Gets or sets the joint used as global position for the algorithm for Nuitrack.
        /// </summary>
        public JointType NuitrackJointAsPosition { get; set; } = JointType.Torso;

        /// <summary>
        /// Gets or sets the joint used as global position for the algorithm for Azure or Simplifed skeletons.
        /// </summary>
        public Microsoft.Azure.Kinect.BodyTracking.JointId GeneralJointAsPosition { get; set; } = Microsoft.Azure.Kinect.BodyTracking.JointId.Pelvis;
    }
}
