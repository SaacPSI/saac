using MathNet.Spatial.Euclidean;
using Microsoft.Azure.Kinect.BodyTracking;
namespace SAAC.Bodies
{
    public class BodiesSelectionConfiguration
    {
        /// <summary>
        /// Gets or sets in which space the bodies are sent.
        /// </summary>
        public CoordinateSystem? Camera2ToCamera1Transformation { get; set; } = null;

        /// <summary>
        /// Gets or sets in which space the bodies are sent.
        /// </summary>
        public JointId JointUsedForCorrespondence { get; set; } = JointId.Pelvis;

        /// <summary>
        /// Gets or sets maximum acceptable distance for correpondance in millimeter
        /// </summary>
        public double MaxDistance { get; set; } = 0.8;

        /// <summary>
        /// Gets or sets minimum distance removing pair form possibility
        /// </summary>
        public double NotPairableDistanceThreshold { get; set; } = 8;

    }
}
