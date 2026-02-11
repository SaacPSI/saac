// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies
{
    using Microsoft.Azure.Kinect.BodyTracking;

    /// <summary>
    /// Configuration for the calibration by bodies component.
    /// </summary>
    public class CalibrationByBodiesConfiguration
    {
        /// <summary>
        /// Delegate for displaying calibration status messages.
        /// </summary>
        /// <param name="status">The status message.</param>
        public delegate void DelegateStatus(string status);

        /// <summary>
        /// Gets or sets the number of joints used in RANSAC for calibration.
        /// </summary>
        public uint NumberOfJointForCalibration { get; set; } = 200;

        /// <summary>
        /// Gets or sets the number of joints used for validating the calibration.
        /// </summary>
        public uint NumberOfJointForTesting { get; set; } = 100;

        /// <summary>
        /// Gets or sets the minimum confidence level required for joints used in calibration.
        /// </summary>
        public JointConfidenceLevel ConfidenceLevelForCalibration { get; set; } = JointConfidenceLevel.High;

        /// <summary>
        /// Gets or sets a value indicating whether to test the transformation matrix with RMSE before sending.
        /// </summary>
        public bool TestMatrixBeforeSending { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum allowed Root Mean Square Error (RMSE) in meters.
        /// </summary>
        public double AllowedMaxRMSE { get; set; } = 0.5;

        /// <summary>
        /// Gets or sets a value indicating whether synchronized calibration is enabled.
        /// </summary>
        public bool SynchedCalibration { get; set; } = true;

        /// <summary>
        /// Gets or sets the delegate for displaying status messages.
        /// </summary>
        public DelegateStatus? SetStatus { get; set; } = null;

        /// <summary>
        /// Gets or sets the file path for storing calibration data.
        /// </summary>
        public string StoringPath { get; set; } = "./Calib.csv";
    }
}
