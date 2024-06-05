
namespace SAAC.Groups
{
    public class SimplifiedFlockGroupsDetectorConfiguration
    {
        /// <summary>
        /// Gets or set the maximum size of the bodies queues for calculation.
        /// </summary>
        public uint QueueMaxCount { get; set; } = 60;

        /// <summary>
        /// Gets or sets the distance weight for the model to constitute a group.
        /// </summary>
        public double DistanceWeigth { get; set; } = 1;

        /// <summary>
        ///  Gets or sets the distance velocity for the model to constitute a group.
        /// </summary>
        public double VelocityWeigth { get; set; } = 0;

        /// <summary>
        ///  Gets or sets the distance direction for the model to constitute a group.
        /// </summary>
        public double DirectionWeigth { get; set; } = 0;

        /// <summary>
        ///  Gets or sets the model value to constitute a group.
        /// </summary>
       public double ModelThreshold { get; set; } = 0.8;
    }
}
