namespace SAAC.Groups
{
    public class InstantGroupsDetectorConfiguration
    {
        /// <summary>
        /// Gets or sets the distance threshold between skeletons for constitute a group.
        /// </summary>
        public double DistanceThreshold { get; set; } = 0.8;
    }
}
