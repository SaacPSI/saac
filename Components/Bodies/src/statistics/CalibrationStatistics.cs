// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies.Statistics
{
    using System.IO;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.Statistics;
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

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

    /// <summary>
    /// Subpipeline component that calculates calibration statistics (RMSE) between two camera views.
    /// </summary>
    public class CalibrationStatistics : Subpipeline
    {
        private readonly CalibrationStatisticsConfiguration configuration;
        private readonly Connector<Matrix<double>> inCalibrationMatrixConnector;
        private readonly Connector<bool> inSynchEventConnector;
        private readonly Connector<List<SimplifiedBody>> inCamera1BodiesConnector;
        private readonly Connector<List<SimplifiedBody>> inCamera2BodiesConnector;
        private readonly List<Tuple<double, double>> rmseList;
        private int addedCount = 0;
        private Tuple<List<double>, List<double>> testingArray;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalibrationStatistics"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">Optional configuration for calibration statistics.</param>
        /// <param name="name">Optional name for the subpipeline.</param>
        /// <param name="defaultDeliveryPolicy">Optional default delivery policy.</param>
        public CalibrationStatistics(Pipeline parent, CalibrationStatisticsConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
          : base(parent, name, defaultDeliveryPolicy)
        {
            this.configuration = configuration ?? new CalibrationStatisticsConfiguration();
            this.inCamera1BodiesConnector = this.CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(this.inCamera1BodiesConnector));
            this.inCamera2BodiesConnector = this.CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(this.inCamera2BodiesConnector));
            this.inCalibrationMatrixConnector = this.CreateInputConnectorFrom<Matrix<double>>(parent, nameof(this.InCalibrationMatrix));
            this.inSynchEventConnector = this.CreateInputConnectorFrom<bool>(parent, nameof(this.inSynchEventConnector));
            this.OutCalibrationRMSE = parent.CreateEmitter<double>(this, nameof(this.OutCalibrationRMSE));

            if (this.configuration.SynchedAcquisition)
            {
                this.inSynchEventConnector.Pair(this.inCamera1BodiesConnector).Pair(this.inCamera2BodiesConnector).Do(this.Process);
            }
            else
            {
                this.inCamera1BodiesConnector.Pair(this.inCamera2BodiesConnector).Do(this.Process);
            }

            if (this.configuration.TransformationMatrix == null)
            {
                this.inCalibrationMatrixConnector.Do(this.Process);
            }

            this.testingArray = new Tuple<List<double>, List<double>>(new List<double>(), new List<double>());
            this.rmseList = new List<Tuple<double, double>>();
            this.SetStatus("Collecting data...");
        }

        /// <summary>
        /// Gets the emitter for calibration RMSE values.
        /// </summary>
        public Emitter<double> OutCalibrationRMSE { get; private set; }

        /// <summary>
        /// Gets the receiver for calibration matrix input.
        /// </summary>
        public Receiver<Matrix<double>> InCalibrationMatrix => this.inCalibrationMatrixConnector.In;

        /// <summary>
        /// Gets the receiver for synchronization event input.
        /// </summary>
        public Receiver<bool> InSynchEvent => this.inSynchEventConnector.In;

        /// <summary>
        /// Gets the receiver for camera 1 bodies input.
        /// </summary>
        public Receiver<List<SimplifiedBody>> InCamera1Bodies => this.inCamera1BodiesConnector.In;

        /// <summary>
        /// Gets the receiver for camera 2 bodies input.
        /// </summary>
        public Receiver<List<SimplifiedBody>> InCamera2Bodies => this.inCamera2BodiesConnector.In;
        /// <summary>
        /// Processes synchronized bodies and calculates RMSE.
        /// </summary>
        /// <param name="bodies">Tuple containing sync signal and bodies from both cameras.</param>
        /// <param name="envelope">The message envelope.</param>
        private void Process((bool, List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            this.Process((bodies.Item2, bodies.Item3), envelope);
        }

        /// <summary>
        /// Processes calibration matrix update.
        /// </summary>
        /// <param name="calib">The calibration transformation matrix.</param>
        /// <param name="envelope">The message envelope.</param>
        private void Process(Matrix<double> calib, Envelope envelope)
        {
            this.configuration.TransformationMatrix = calib;
        }

        /// <summary>
        /// Processes bodies from two cameras and calculates RMSE for calibration validation.
        /// </summary>
        /// <param name="bodies">Tuple containing bodies from camera 1 and camera 2.</param>
        /// <param name="envelope">The message envelope.</param>
        private void Process((List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            if (bodies.Item1.Count != 1 || bodies.Item2.Count != 1 || this.configuration.TransformationMatrix == null)
            {
                return;
            }

            var camera1 = bodies.Item1[0];
            var camera2 = bodies.Item2[0];
            for (JointId iterator = JointId.Pelvis; iterator < JointId.Count; iterator++)
            {
                if (camera1.Joints[iterator].Item1 >= this.configuration.ConfidenceLevelForCalibration && camera2.Joints[iterator].Item1 >= this.configuration.ConfidenceLevelForCalibration)
                {
                    if (this.addedCount >= this.configuration.TestingCount)
                    {
                        break;
                    }

                    Helpers.Helpers.PushToList(camera2.Joints[iterator].Item2, this.configuration.TransformationMatrix, ref this.testingArray);
                    if (this.configuration.CalculationType == CalibrationStatisticsConfiguration.TestingType.ByNumberOfJoints)
                    {
                        this.addedCount++;
                    }
                }
            }

            if (this.configuration.CalculationType == CalibrationStatisticsConfiguration.TestingType.ByNumberOfFrames)
            {
                this.addedCount++;
            }

            this.SetStatus("Checking: " + this.addedCount.ToString() + "/" + this.configuration.TestingCount.ToString());

            if (this.addedCount >= this.configuration.TestingCount)
            {
                double RMSE = Helpers.Helpers.CalculateRMSE(ref this.testingArray);
                this.SetStatus("RMSE: " + RMSE.ToString());
                this.OutCalibrationRMSE.Post(RMSE, DateTime.Now);
                this.rmseList.Add(new Tuple<double, double>(RMSE, this.testingArray.Item2.Count / 3));
                this.CleanIteratorsAndCounters();
            }
        }

        /// <summary>
        /// Disposes the subpipeline and writes RMSE statistics to file.
        /// </summary>
        public override void Dispose()
        {
            if (this.configuration.StoringPath.Count() > 4)
            {
                string statsCount = "rmse;joints_count;\n";
                foreach (var rmseTuple in this.rmseList)
                {
                    statsCount += rmseTuple.Item1.ToString() + ";" + rmseTuple.Item2.ToString() + "\n";
                }

                var rmse = this.rmseList.Select(p => p.Item1);
                var std = rmse.MeanStandardDeviation();
                var variance = rmse.MeanVariance();

                statsCount += "\n\n Std;" + std.Item2.ToString() + "\n Var;" + variance.Item1.ToString();

                File.WriteAllText(this.configuration.StoringPath, statsCount);
            }

            base.Dispose();
        }

        /// <summary>
        /// Cleans internal counters and testing data for next iteration.
        /// </summary>
        private void CleanIteratorsAndCounters()
        {
            this.addedCount = 0;
            this.testingArray.Item1.Clear();
            this.testingArray.Item2.Clear();
        }

        /// <summary>
        /// Sets status message using the configured delegate.
        /// </summary>
        /// <param name="message">The status message to display.</param>
        private void SetStatus(string message)
        {
            if (this.configuration.SetStatus != null)
            {
                this.configuration.SetStatus(message);
            }
        }
    }
}
