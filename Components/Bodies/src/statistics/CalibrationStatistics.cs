using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Azure.Kinect.BodyTracking;
using System.IO;

namespace Bodies.Statistics
{
    public class CalibrationStatisticsConfiguration
    {
        /// <summary>
        /// Gets or sets in which space the bodies are sent.
        /// </summary>
        public Matrix<double>? TransformationMatrix { get; set; } = null;

        /// <summary>
        /// Gets or sets the type of type of trigger for calculating the statistics.
        /// </summary>
        public enum TestingType { ByNumberOfFrames, ByNumberOfJoints };
        public TestingType CalculationType = TestingType.ByNumberOfFrames;

        /// <summary>
        /// Gets or sets the number of frame or joints used for calculating the statistics.
        /// </summary>
        public uint TestingCount { get; set; } = 1;

        /// <summary>
        /// Gets or sets the confidence level used for calibration.
        /// </summary>
        public JointConfidenceLevel ConfidenceLevelForCalibration { get; set; } = JointConfidenceLevel.Medium;

        /// <summary>
        /// Connect Synch event receiver
        /// </summary>
        public bool SynchedAcquisition { get; set; } = true;

        /// <summary>
        /// Delegate Status.
        /// </summary>
        public delegate void DelegateStatus(string status);
        public DelegateStatus? SetStatus = null;

        /// <summary>
        /// Delegate to display status.
        /// </summary>
        public string StoringPath { get; set; } = "./CalibStat.csv";
    }

    public class CalibrationStatistics : Subpipeline
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<double> OutCalibrationRMSE { get; private set; }

        /// <summary>
        /// Gets the connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<Matrix<double>> InCalibrationMatrixConnector;

        /// <summary>
        /// Receiver that encapsulates the input list of skeletons
        /// </summary>
        public Receiver<Matrix<double>> InCalibrationMatrix => InCalibrationMatrixConnector.In;

        /// <summary>
        /// Synch signals for capturing skeletons.
        /// </summary>
        private Connector<bool> InSynchEventConnector;

        // Receiver that encapsulates the synch signal.
        public Receiver<bool> InSynchEvent => InSynchEventConnector.In;

        /// <summary>
        /// Gets the connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<SimplifiedBody>> InCamera1BodiesConnector;

        // Receiver that encapsulates the input list of skeletons
        public Receiver<List<SimplifiedBody>> InCamera1Bodies => InCamera1BodiesConnector.In;

        /// <summary>
        /// Gets the connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<SimplifiedBody>> InCamera2BodiesConnector;

        // Receiver that encapsulates the input list of skeletons
        public Receiver<List<SimplifiedBody>> InCamera2Bodies => InCamera2BodiesConnector.In;

        private CalibrationStatisticsConfiguration Configuration { get; }

        //Stats stuff
        private int AddedCount = 0;
        private Tuple<List<double>, List<double>> TestingArray;
        private List<Tuple<double, double>> RMSEList;

        public CalibrationStatistics(Pipeline parent, CalibrationStatisticsConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
          : base(parent, name, defaultDeliveryPolicy)
        {
            Configuration = configuration ?? new CalibrationStatisticsConfiguration();
            InCamera1BodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InCamera1BodiesConnector));
            InCamera2BodiesConnector = CreateInputConnectorFrom<List<SimplifiedBody>>(parent, nameof(InCamera2BodiesConnector));
            InCalibrationMatrixConnector = CreateInputConnectorFrom<Matrix<double>>(parent, nameof(InCalibrationMatrix));
            InSynchEventConnector = CreateInputConnectorFrom<bool>(parent, nameof(InSynchEventConnector));
            OutCalibrationRMSE = parent.CreateEmitter<double>(this, nameof(OutCalibrationRMSE));

            if (Configuration.SynchedAcquisition)
                InSynchEventConnector.Pair(InCamera1BodiesConnector).Pair(InCamera2BodiesConnector).Do(Process);
            else
                InCamera1BodiesConnector.Pair(InCamera2BodiesConnector).Do(Process);

            if (Configuration.TransformationMatrix == null)
                InCalibrationMatrixConnector.Do(Process);

            TestingArray = new Tuple<List<double>, List<double>>(new List<double>(), new List<double>());
            RMSEList = new List<Tuple<double, double>>();
            SetStatus("Collecting data...");
        }
        private void Process((bool, List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            Process((bodies.Item2, bodies.Item3), envelope);
        }

        private void Process(Matrix<double> calib, Envelope envelope)
        {
            Configuration.TransformationMatrix = calib;
        }

        private void Process((List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            if (bodies.Item1.Count != 1 || bodies.Item2.Count != 1 || Configuration.TransformationMatrix == null)
                return;
            var camera1 = bodies.Item1[0];
            var camera2 = bodies.Item2[0];
            for (JointId iterator = JointId.Pelvis; iterator < JointId.Count; iterator++)
            {
                if (camera1.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration && camera2.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration)
                {
                    if (AddedCount >= Configuration.TestingCount)
                        break;
                    Helpers.Helpers.PushToList(camera2.Joints[iterator].Item2, Configuration.TransformationMatrix, ref TestingArray);
                    if (Configuration.CalculationType == CalibrationStatisticsConfiguration.TestingType.ByNumberOfJoints)
                        AddedCount++;
                }
            }
            if (Configuration.CalculationType == CalibrationStatisticsConfiguration.TestingType.ByNumberOfFrames)
                AddedCount++;
            SetStatus("Checking: " + AddedCount.ToString() + "/" + Configuration.TestingCount.ToString());

            if (AddedCount >= Configuration.TestingCount)
            {
                double RMSE = Helpers.Helpers.CalculateRMSE(ref TestingArray);
                SetStatus("RMSE: " + RMSE.ToString());
                OutCalibrationRMSE.Post(RMSE, DateTime.Now);
                RMSEList.Add(new Tuple<double, double>(RMSE, TestingArray.Item2.Count / 3));
                CleanIteratorsAndCounters();
            }
        }

        public override void Dispose()
        {
            if (Configuration.StoringPath.Count() > 4)
            {
                string statsCount = "rmse;joints_count;\n";
                foreach (var rmseTuple in RMSEList)
                {
                    statsCount += rmseTuple.Item1.ToString() + ";" + rmseTuple.Item2.ToString() + "\n";
                }
                var rmse = RMSEList.Select(p => p.Item1);
                var std = rmse.MeanStandardDeviation();
                var variance = rmse.MeanVariance();

                statsCount += "\n\n Std;" + std.Item2.ToString() + "\n Var;" + variance.Item1.ToString();

                File.WriteAllText(Configuration.StoringPath, statsCount);
            }
            base.Dispose();
        }

        private void CleanIteratorsAndCounters()
        {
            AddedCount = 0;
            TestingArray.Item1.Clear();
            TestingArray.Item2.Clear();
        }

        private void SetStatus(string message)
        {
            if (Configuration.SetStatus != null)
                Configuration.SetStatus(message);
        }
    }
}
