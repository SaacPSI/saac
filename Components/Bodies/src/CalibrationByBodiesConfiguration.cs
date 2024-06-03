using Microsoft.Azure.Kinect.BodyTracking;

namespace SAAC.Bodies
{
    public class CalibrationByBodiesConfiguration
    {
        /// <summary>
        /// Gets or sets the number of joints used in ransac for calibration.
        /// </summary>
        public uint NumberOfJointForCalibration { get; set; } = 200;

        /// <summary>
        /// Gets or sets the number of joints used for validating the calibration.
        /// </summary>
        public uint NumberOfJointForTesting { get; set; } = 100;

        /// <summary>
        /// Gets or sets the confidence level used for calibration.
        /// </summary>
        public JointConfidenceLevel ConfidenceLevelForCalibration { get; set; } = JointConfidenceLevel.High;

        /// <summary>
        /// Test the transformation matrix with some frames & RMSE.
        /// </summary>
        public bool TestMatrixBeforeSending { get; set; } = true;

        /// <summary>
        /// .
        /// </summary>
        public double AllowedMaxRMSE { get; set; } = 0.5;

        /// <summary>
        /// Connect Synch event receiver
        /// </summary>
        public bool SynchedCalibration { get; set; } = true;

        /// <summary>
        /// Delegate to display status.
        /// </summary>
        public delegate void DelegateStatus(string status);
        public DelegateStatus? SetStatus = null;

        /// <summary>
        /// Pouet Status.
        /// </summary>
        public string StoringPath { get; set; } = "./Calib.csv";
    }
}
