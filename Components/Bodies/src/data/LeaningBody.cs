// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies
{
    using Microsoft.Azure.Kinect.BodyTracking;

    /// <summary>
    /// Represents a body in the learning phase, collecting bone length measurements.
    /// </summary>
    public class LearningBody
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LearningBody"/> class.
        /// </summary>
        /// <param name="id">The body identifier.</param>
        /// <param name="time">The creation time.</param>
        /// <param name="bones">The list of bone pairs to learn.</param>
        public LearningBody(uint id, DateTime time, List<(JointId ChildJoint, JointId ParentJoint)> bones)
        {
            this.Id = id;
            this.CreationTime = time;
            this.LearningBones = new Dictionary<(JointId ChildJoint, JointId ParentJoint), List<double>>();
            foreach (var bone in bones)
            {
                this.LearningBones.Add(bone, new List<double>());
            }
        }

        /// <summary>
        /// Gets or sets the dictionary of bone measurements being collected.
        /// </summary>
        public Dictionary<(JointId ChildJoint, JointId ParentJoint), List<double>> LearningBones { get; set; }

        /// <summary>
        /// Gets the body identifier.
        /// </summary>
        public uint Id { get; private set; }

        /// <summary>
        /// Gets the time when learning started.
        /// </summary>
        public DateTime CreationTime { get; private set; }

        /// <summary>
        /// Checks if the body is still in the learning phase.
        /// </summary>
        /// <param name="time">The current time.</param>
        /// <param name="duration">The maximum learning duration.</param>
        /// <param name="MinimumBonesForIdentification">The minimum number of bones required.</param>
        /// <returns>True if still learning; otherwise false.</returns>
        public bool StillLearning(DateTime time, TimeSpan duration, uint MinimumBonesForIdentification)
        {
            uint count = 0;
            foreach (var bone in LearningBones)
                if (bone.Value.Count >= MinimumBonesForIdentification)
                    count++;

            return ((time - CreationTime) < duration) || MinimumBonesForIdentification >= count;
        }

        /// <summary>
        /// Generates a learned body from the collected measurements.
        /// </summary>
        /// <param name="maxStdDev">The maximum acceptable standard deviation for bone lengths.</param>
        /// <returns>A new learned body with averaged bone lengths.</returns>
        public LearnedBody GeneratorLearnedBody(double maxStdDev)
        {
            Dictionary<(JointId ChildJoint, JointId ParentJoint), double> learnedBones = new Dictionary<(JointId ChildJoint, JointId ParentJoint), double>();
            foreach (var iterator in LearningBones)
            {
                var statistics = MathNet.Numerics.Statistics.Statistics.MeanStandardDeviation(iterator.Value);
                if (statistics.Item2 < maxStdDev)
                    learnedBones[iterator.Key] = statistics.Item1;
                else
                    learnedBones[iterator.Key] = -1;
            }
            return new LearnedBody(Id, learnedBones);
        }
    }
}
