
namespace SAAC.Groups
{
    public class IntegratedGroupsDetectorConfiguration
    {
        /// <summary>
        /// Gets or sets the weight for body on group.
        /// </summary>
        public double IncreaseWeightFactor { get; set; } = 3.0;

        /// <summary>
        /// Gets or sets the value of decreasing weight when a body is not found un a group.
        /// </summary>
        public double DecreaseWeightFactor { get; set; } = 2.0;

        /// <summary>
        /// Gets or sets percentage of match between two groups for removing the smallest one.
        /// </summary>
        public double IntersectionPercentage { get; set; } = 0.8;
    }

}
