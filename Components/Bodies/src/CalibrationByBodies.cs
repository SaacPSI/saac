using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Azure.Kinect.BodyTracking;

namespace Bodies
{
    public class CalibrationByBodies : IProducer<Matrix<double>>
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<Matrix<double>> Out{ get; private set; }

        /// <summary>
        /// Synch signals for capturing skeletons.
        /// </summary>
        private Connector<bool> InSynchEventConnector;

        // Receiver that encapsulates the synch signal.
        public Receiver<bool> InSynchEvent => InSynchEventConnector.In;

        /// <summary>
        /// Gets the nuitrack connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<SimplifiedBody>> InCamera1BodiesConnector;

        // Receiver that encapsulates the input list of Nuitrack skeletons
        public Receiver<List<SimplifiedBody>> InCamera1Bodies => InCamera1BodiesConnector.In;

        /// <summary>
        /// Gets the nuitrack connector of lists of currently tracked bodies.
        /// </summary>
        private Connector<List<SimplifiedBody>> InCamera2BodiesConnector;

        // Receiver that encapsulates the input list of Nuitrack skeletons
        public Receiver<List<SimplifiedBody>> InCamera2Bodies => InCamera2BodiesConnector.In;

        private CalibrationByBodiesConfiguration Configuration { get; }

        //Calibration stuff
        private Tuple<Emgu.CV.Structure.MCvPoint3D32f[], Emgu.CV.Structure.MCvPoint3D32f[]> CalibrationJoints;
        private DateTime? CalibrationTime = null;
        private int JointAddedCount = 0;
        private enum ECalibrationState { Idle, Running, Testing };
        private ECalibrationState CalibrationState = ECalibrationState.Running;
        Matrix<double> TransformationMatrix = Matrix<double>.Build.Dense(1,1);
        private Tuple<List<double>, List<double>> TestingArray;

        public CalibrationByBodies(Pipeline parent, CalibrationByBodiesConfiguration? configuration = null, string? name = null, DeliveryPolicy? defaultDeliveryPolicy = null)
        {
            Configuration = configuration ?? new CalibrationByBodiesConfiguration();
            InCamera1BodiesConnector = parent.CreateConnector<List<SimplifiedBody>>(nameof(InCamera1BodiesConnector));
            InCamera2BodiesConnector = parent.CreateConnector<List<SimplifiedBody>>(nameof(InCamera2BodiesConnector));
            Out = parent.CreateEmitter<Matrix<double>>(this, nameof(Out));
            InSynchEventConnector = parent.CreateConnector<bool>(nameof(InSynchEventConnector));

            if (Configuration.SynchedCalibration)
                InSynchEventConnector.Pair(InCamera1BodiesConnector).Pair(InCamera2BodiesConnector).Do(Process);
            else
                InCamera1BodiesConnector.Pair(InCamera2BodiesConnector).Do(Process);

            Emgu.CV.Structure.MCvPoint3D32f[] camera1 = new Emgu.CV.Structure.MCvPoint3D32f[(int)Configuration.NumberOfJointForCalibration];
            Emgu.CV.Structure.MCvPoint3D32f[] camera2 = new Emgu.CV.Structure.MCvPoint3D32f[(int)Configuration.NumberOfJointForCalibration];
            CalibrationJoints = new Tuple<Emgu.CV.Structure.MCvPoint3D32f[], Emgu.CV.Structure.MCvPoint3D32f[]>(camera1, camera2);
            TestingArray = new Tuple<List<double>, List<double>>(new List<double>(), new List<double>());
            SetStatus("Collecting data...");
        }

        private void Process((bool, List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            Process((bodies.Item2, bodies.Item3), envelope);
        }

        private void Process((List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            switch(CalibrationState)
            {
                case ECalibrationState.Running:
                    if (bodies.Item1.Count == bodies.Item2.Count && bodies.Item1.Count == 1)
                        CalibrationState = DoCalibration(bodies.Item1[0], bodies.Item2[0], envelope.OriginatingTime);
                    break;
                case ECalibrationState.Testing:
                    if (bodies.Item1.Count == bodies.Item2.Count && bodies.Item1.Count == 1)
                        CalibrationState = DoTesting(bodies.Item1[0], bodies.Item2[0], envelope.OriginatingTime);
                    break;
            }
        }

        private ECalibrationState DoCalibration(SimplifiedBody camera1, SimplifiedBody camera2, DateTime time)
        {
            //Wait 5 seconds
            if (CalibrationTime != null)
            {
                TimeSpan interval = (TimeSpan)(time - CalibrationTime);
                if (interval.TotalMilliseconds < 5)
                    return ECalibrationState.Running;
            }
            CalibrationTime = time;
            for (JointId iterator = JointId.Pelvis; iterator < JointId.Count; iterator++)
            {
                if (camera1.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration &&
                    camera2.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration)
                {
                    if (JointAddedCount >= Configuration.NumberOfJointForCalibration)
                        break;
                    CalibrationJoints.Item1[JointAddedCount] = VectorToCVPoint(camera1.Joints[iterator].Item2);
                    CalibrationJoints.Item2[JointAddedCount] = VectorToCVPoint(camera2.Joints[iterator].Item2);
                    JointAddedCount++;
                }
            }
            SetStatus("Calibration running:  " + JointAddedCount.ToString() + "/" + Configuration.NumberOfJointForCalibration.ToString());
            if (JointAddedCount >= Configuration.NumberOfJointForCalibration)
            {
                Emgu.CV.UMat outputArray = new Emgu.CV.UMat();
                Emgu.CV.UMat inliers = new Emgu.CV.UMat();
                Emgu.CV.Util.VectorOfPoint3D32F v1 = new Emgu.CV.Util.VectorOfPoint3D32F();
                Emgu.CV.Util.VectorOfPoint3D32F v2 = new Emgu.CV.Util.VectorOfPoint3D32F();
                v1.Push(CalibrationJoints.Item1);
                v2.Push(CalibrationJoints.Item2);
                int retval = Emgu.CV.CvInvoke.EstimateAffine3D(v2, v1, outputArray, inliers);

                double[] tempArray = new double[12];
                var mat = outputArray.GetOutputArray().GetMat();
                mat.CopyTo(tempArray); mat.Dispose();
                double[,] dArray = new double[4, 4];
                for(int index = 0; index < 12; index++)
                    dArray[index%4, index/4] = tempArray[index];

                v1.Clear();
                v2.Clear();
                inliers.Dispose();
                outputArray.GetOutputArray().Dispose();
                outputArray.Dispose();
                dArray[3, 3] = 1;
                TransformationMatrix = Matrix<double>.Build.DenseOfArray(dArray);
                CleanIteratorsAndCounters();
                if(Configuration.TestMatrixBeforeSending)
                {
                    SetStatus("Calibration done! Checking...");
                    return ECalibrationState.Testing;
                }
                else
                {
                    Out.Post(TransformationMatrix, time);
                    SetStatus("Calibration Done");
                    Helpers.Helpers.StoreCalibrationMatrix(Configuration.StoringPath, TransformationMatrix);
                    return ECalibrationState.Idle;
                }
            }
            return ECalibrationState.Running;
        }

        private ECalibrationState DoTesting(SimplifiedBody camera1, SimplifiedBody camera2, DateTime time)
        {
            if (CalibrationTime != null)
            {
                TimeSpan interval = (TimeSpan)(time - CalibrationTime);
                if (interval.TotalMilliseconds < 5)
                    return ECalibrationState.Testing;
            }
            CalibrationTime = time;
            for (JointId iterator = JointId.Pelvis; iterator < JointId.Count; iterator++)
            {
                if (camera1.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration && camera2.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration)
                {
                    if (JointAddedCount >= Configuration.NumberOfJointForTesting)
                        break;
                    Helpers.Helpers.PushToList(camera2.Joints[iterator].Item2, TransformationMatrix, ref TestingArray);
                    JointAddedCount++;
                }
            }
            SetStatus("Checking: " + JointAddedCount.ToString() + "/" + Configuration.NumberOfJointForTesting.ToString());

            if (JointAddedCount >= Configuration.NumberOfJointForTesting)
            {
                double RMSE = Helpers.Helpers.CalculateRMSE(ref TestingArray);
                CleanIteratorsAndCounters();
                if (RMSE < Configuration.AllowedMaxRMSE)
                {
                    SetStatus("Calibration done! RMSE: " + RMSE.ToString());
                    Out.Post(TransformationMatrix, time);
                    Helpers.Helpers.StoreCalibrationMatrix(Configuration.StoringPath, TransformationMatrix);
                    return ECalibrationState.Idle;
                }
                else
                {
                    SetStatus("Test fail RMSE: " + RMSE.ToString() + "/" + Configuration.AllowedMaxRMSE.ToString() + ", back to calibration");
                    return ECalibrationState.Running;
                }
            }
            return ECalibrationState.Testing;
        }

        private Emgu.CV.Structure.MCvPoint3D32f VectorToCVPoint(Vector3D point)
        {
            Emgu.CV.Structure.MCvPoint3D32f retValue = new Emgu.CV.Structure.MCvPoint3D32f((float)point.X, (float)point.Y, (float)point.Z);
            return retValue;
        }

        private void CleanIteratorsAndCounters()
        {
            CalibrationTime = null;
            JointAddedCount = 0;
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
