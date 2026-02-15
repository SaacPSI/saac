// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies
{
    using Microsoft.Azure.Kinect.BodyTracking;

    /// <summary>
    /// Represents a learned body with bone length characteristics for identification.
    /// </summary>
    public class LearnedBody
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LearnedBody"/> class.
        /// </summary>
        /// <param name="id">The body identifier.</param>
        /// <param name="bones">The dictionary of learned bone lengths.</param>
        public LearnedBody(uint id, Dictionary<(JointId ChildJoint, JointId ParentJoint), double> bones)
        {
            this.Id = id;
            this.LearnedBones = bones;
        }

        /// <summary>
        /// Gets the dictionary of learned bone lengths keyed by joint pairs.
        /// </summary>
        public Dictionary<(JointId ChildJoint, JointId ParentJoint), double> LearnedBones { get; private set; }

        /// <summary>
        /// Gets the body identifier.
        /// </summary>
        public uint Id { get; private set; }

        /// <summary>
        /// Gets or sets the last time this body was seen.
        /// </summary>
        public DateTime LastSeen { get; set; }

        /// <summary>
        /// Gets or sets the last known position of this body.
        /// </summary>
        public MathNet.Spatial.Euclidean.Vector3D LastPosition { get; set; }

        /// <summary>
        /// Checks if this body is the same as another learned body based on bone length comparison.
        /// </summary>
        /// <param name="b">The other learned body to compare.</param>
        /// <param name="maxDeviation">The maximum allowed standard deviation.</param>
        /// <returns>True if bodies are similar; otherwise false.</returns>
        public bool IsSameAs(LearnedBody b, double maxDeviation)
        {
            return this.ProcessDifference(b) < maxDeviation;
        }

        /// <summary>
        /// Checks if a simplified body seems to match this learned body.
        /// </summary>
        /// <param name="b">The simplified body to compare.</param>
        /// <param name="maxDeviation">The maximum allowed standard deviation.</param>
        /// <param name="jointConfidenceLevel">The minimum required joint confidence level.</param>
        /// <returns>True if the body matches; otherwise false.</returns>
        public bool SeemsTheSame(SimplifiedBody b, double maxDeviation, JointConfidenceLevel jointConfidenceLevel)
        {
            List<double> dists = new List<double>();
            foreach (var bones in this.LearnedBones)
            {
                if (bones.Value > 0.0 && b.Joints[bones.Key.ParentJoint].Item1 >= jointConfidenceLevel && b.Joints[bones.Key.ChildJoint].Item1 >= jointConfidenceLevel)
                {
                    dists.Add(Math.Abs(MathNet.Numerics.Distance.Euclidean(b.Joints[bones.Key.ParentJoint].Item2.ToVector(), b.Joints[bones.Key.ChildJoint].Item2.ToVector()) - bones.Value));
                }
            }

            return MathNet.Numerics.Statistics.Statistics.MeanStandardDeviation(dists).Item2 < maxDeviation;
        }

        /// <summary>
        /// Calculates the difference between this learned body and another.
        /// </summary>
        /// <param name="b">The other learned body.</param>
        /// <returns>The standard deviation of bone length differences.</returns>
        public double ProcessDifference(LearnedBody b)
        {
            List<double> diff = new List<double>();
            foreach (var iterator in this.LearnedBones)
            {
                if (iterator.Value > 0.0 && b.LearnedBones[iterator.Key] > 0.0)
                {
                    diff.Add(Math.Abs(iterator.Value - b.LearnedBones[iterator.Key]));
                }
            }

            var statistics = MathNet.Numerics.Statistics.Statistics.MeanStandardDeviation(diff);
            return statistics.Item2;
        }

        /// <summary>
        /// Finds the closest matching learned body from a list.
        /// </summary>
        /// <param name="listOfBodies">The list of learned bodies to search.</param>
        /// <param name="maxDeviation">The maximum allowed deviation.</param>
        /// <returns>The ID of the closest body, or 0 if no match found.</returns>
        public uint FindClosest(List<LearnedBody> listOfBodies, double maxDeviation)
        {
            List<KeyValuePair<double, LearnedBody>> pairs = new List<KeyValuePair<double, LearnedBody>>();
            foreach (var pair in listOfBodies)
            {
                pairs.Add(new KeyValuePair<double, LearnedBody>(this.ProcessDifference(pair), pair));
            }

            pairs.Sort(new TupleDoubleLearnedBodyComparer());
            if (pairs.Count == 0 || double.IsNaN(pairs.First().Key) || maxDeviation < pairs.First().Key)
            {
                return 0;
            }

            return pairs.First().Value.Id;
        }

        /// <summary>
        /// Comparer for sorting learned bodies by difference value.
        /// </summary>
        internal class TupleDoubleLearnedBodyComparer : Comparer<KeyValuePair<double, LearnedBody>>
        {
            /// <summary>
            /// Compares two key-value pairs by their double key.
            /// </summary>
            /// <param name="a">The first pair.</param>
            /// <param name="b">The second pair.</param>
            /// <returns>A value indicating the relative order.</returns>
            public override int Compare(KeyValuePair<double, LearnedBody> a, KeyValuePair<double, LearnedBody> b)
            {
                if (a.Key == b.Key)
                {
                    return 0;
                }

                return a.Key > b.Key ? 1 : -1;
            }
        }
    }
}
