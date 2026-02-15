// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies.Statistics
{
    using MathNet.Numerics.LinearAlgebra;
    using Microsoft.Azure.Kinect.BodyTracking;

    /// <summary>
    /// Configuration for the calibration statistics component.
    /// </summary>
    public class CalibrationStatisticsConfiguration
    {
        /// <summary>
        /// Enumeration of testing types for statistics calculation.
        /// </summary>
        public enum TestingType
        {
            /// <summary>Calculate by number of frames.</summary>
            ByNumberOfFrames,

            /// <summary>Calculate by number of joints.</summary>
            ByNumberOfJoints
        }

        /// <summary>
        /// Delegate for displaying calibration status messages.
        /// </summary>
        /// <param name="status">The status message.</param>
        public delegate void DelegateStatus(string status);

        /// <summary>
        /// Gets or sets the transformation matrix from camera 2 to camera 1.
        /// </summary>
        public Matrix<double>? TransformationMatrix { get; set; } = null;

        /// <summary>
        /// Gets or sets the type of trigger for calculating statistics.
        /// </summary>
        public TestingType CalculationType { get; set; } = TestingType.ByNumberOfFrames;

        /// <summary>
        /// Gets or sets the number of frames or joints used for calculating statistics.
        /// </summary>
        public uint TestingCount { get; set; } = 1;

        /// <summary>
        /// Gets or sets the minimum confidence level required for joints used in calibration.
        /// </summary>
        public JointConfidenceLevel ConfidenceLevelForCalibration { get; set; } = JointConfidenceLevel.Medium;

        /// <summary>
        /// Gets or sets a value indicating whether synchronized acquisition is enabled.
        /// </summary>
        public bool SynchedAcquisition { get; set; } = true;

        /// <summary>
        /// Gets or sets the delegate for displaying status messages.
        /// </summary>
        public DelegateStatus? SetStatus { get; set; } = null;

        /// <summary>
        /// Gets or sets the file path for storing calibration statistics.
        /// </summary>
        public string StoringPath { get; set; } = "./CalibStat.csv";
    }
}
