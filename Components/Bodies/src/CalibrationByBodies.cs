using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Azure.Kinect.BodyTracking;

namespace SAAC.Bodies
{
    public class CalibrationByBodies : IProducer<CoordinateSystem>
    {
        /// <summary>
        /// Gets the emitter of groups detected.
        /// </summary>
        public Emitter<CoordinateSystem> Out{ get; private set; }

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
        private Tuple<Emgu.CV.Structure.MCvPoint3D32f[], Emgu.CV.Structure.MCvPoint3D32f[]> calibrationJoints;
        private DateTime? calibrationTime = null;
        private int jointAddedCount = 0;
        private enum ECalibrationState { Idle, Running, Testing };
        private ECalibrationState calibrationState = ECalibrationState.Running;
        private Matrix<double> transformationMatrix = Matrix<double>.Build.Dense(1,1);
        private Tuple<List<double>, List<double>> testingArray;
        private string name;

        public CalibrationByBodies(Pipeline parent, CalibrationByBodiesConfiguration? configuration = null, string name = nameof(CalibrationByBodies), DeliveryPolicy? defaultDeliveryPolicy = null)
        {
            this.name = name;
            Configuration = configuration ?? new CalibrationByBodiesConfiguration();
            InCamera1BodiesConnector = parent.CreateConnector<List<SimplifiedBody>>($"{name}-InCamera1BodiesConnector");
            InCamera2BodiesConnector = parent.CreateConnector<List<SimplifiedBody>>($"{name}-InCamera2BodiesConnector");
            Out = parent.CreateEmitter<CoordinateSystem>(this, $"{name}-Out");
            InSynchEventConnector = parent.CreateConnector<bool>($"{name}-InSynchEventConnector");

            if (Configuration.SynchedCalibration)
                InSynchEventConnector.Pair(InCamera1BodiesConnector).Pair(InCamera2BodiesConnector).Do(Process);
            else
                InCamera1BodiesConnector.Pair(InCamera2BodiesConnector).Do(Process);

            Emgu.CV.Structure.MCvPoint3D32f[] camera1 = new Emgu.CV.Structure.MCvPoint3D32f[(int)Configuration.NumberOfJointForCalibration];
            Emgu.CV.Structure.MCvPoint3D32f[] camera2 = new Emgu.CV.Structure.MCvPoint3D32f[(int)Configuration.NumberOfJointForCalibration];
            calibrationJoints = new Tuple<Emgu.CV.Structure.MCvPoint3D32f[], Emgu.CV.Structure.MCvPoint3D32f[]>(camera1, camera2);
            testingArray = new Tuple<List<double>, List<double>>(new List<double>(), new List<double>());
            SetStatus("Collecting data...");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process((bool, List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            Process((bodies.Item2, bodies.Item3), envelope);
        }

        private void Process((List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            switch(calibrationState)
            {
                case ECalibrationState.Running:
                    if (bodies.Item1.Count == bodies.Item2.Count && bodies.Item1.Count == 1)
                        calibrationState = DoCalibration(bodies.Item1[0], bodies.Item2[0], envelope.OriginatingTime);
                    break;
                case ECalibrationState.Testing:
                    if (bodies.Item1.Count == bodies.Item2.Count && bodies.Item1.Count == 1)
                        calibrationState = DoTesting(bodies.Item1[0], bodies.Item2[0], envelope.OriginatingTime);
                    break;
            }
        }

        private ECalibrationState DoCalibration(SimplifiedBody camera1, SimplifiedBody camera2, DateTime time)
        {
            //Wait 5 seconds
            if (calibrationTime != null)
            {
                TimeSpan interval = (TimeSpan)(time - calibrationTime);
                if (interval.TotalMilliseconds < 5)
                    return ECalibrationState.Running;
            }
            calibrationTime = time;
            for (JointId iterator = JointId.Pelvis; iterator < JointId.Count; iterator++)
            {
                if (camera1.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration &&
                    camera2.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration)
                {
                    if (jointAddedCount >= Configuration.NumberOfJointForCalibration)
                        break;
                    calibrationJoints.Item1[jointAddedCount] = VectorToCVPoint(camera1.Joints[iterator].Item2);
                    calibrationJoints.Item2[jointAddedCount] = VectorToCVPoint(camera2.Joints[iterator].Item2);
                    jointAddedCount++;
                }
            }
            SetStatus("Calibration running:  " + jointAddedCount.ToString() + "/" + Configuration.NumberOfJointForCalibration.ToString());
            if (jointAddedCount >= Configuration.NumberOfJointForCalibration)
            {
                Emgu.CV.UMat outputArray = new Emgu.CV.UMat();
                Emgu.CV.UMat inliers = new Emgu.CV.UMat();
                Emgu.CV.Util.VectorOfPoint3D32F v1 = new Emgu.CV.Util.VectorOfPoint3D32F();
                Emgu.CV.Util.VectorOfPoint3D32F v2 = new Emgu.CV.Util.VectorOfPoint3D32F();
                v1.Push(calibrationJoints.Item1);
                v2.Push(calibrationJoints.Item2);
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
                transformationMatrix = Matrix<double>.Build.DenseOfArray(dArray);
                CleanIteratorsAndCounters();
                if(Configuration.TestMatrixBeforeSending)
                {
                    SetStatus("Calibration done! Checking...");
                    return ECalibrationState.Testing;
                }
                else
                {
                    Out.Post(new CoordinateSystem(transformationMatrix), time);
                    SetStatus("Calibration Done");
                    Helpers.Helpers.StoreCalibrationMatrix(Configuration.StoringPath, transformationMatrix);
                    return ECalibrationState.Idle;
                }
            }
            return ECalibrationState.Running;
        }

        private ECalibrationState DoTesting(SimplifiedBody camera1, SimplifiedBody camera2, DateTime time)
        {
            if (calibrationTime != null)
            {
                TimeSpan interval = (TimeSpan)(time - calibrationTime);
                if (interval.TotalMilliseconds < 5)
                    return ECalibrationState.Testing;
            }
            calibrationTime = time;
            for (JointId iterator = JointId.Pelvis; iterator < JointId.Count; iterator++)
            {
                if (camera1.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration && camera2.Joints[iterator].Item1 >= Configuration.ConfidenceLevelForCalibration)
                {
                    if (jointAddedCount >= Configuration.NumberOfJointForTesting)
                        break;
                    Helpers.Helpers.PushToList(camera2.Joints[iterator].Item2, transformationMatrix, ref testingArray);
                    jointAddedCount++;
                }
            }
            SetStatus("Checking: " + jointAddedCount.ToString() + "/" + Configuration.NumberOfJointForTesting.ToString());

            if (jointAddedCount >= Configuration.NumberOfJointForTesting)
            {
                double RMSE = Helpers.Helpers.CalculateRMSE(ref testingArray);
                CleanIteratorsAndCounters();
                if (RMSE < Configuration.AllowedMaxRMSE)
                {
                    SetStatus("Calibration done! RMSE: " + RMSE.ToString());
                    Out.Post(new CoordinateSystem(transformationMatrix), time);
                    Helpers.Helpers.StoreCalibrationMatrix(Configuration.StoringPath, transformationMatrix);
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
            calibrationTime = null;
            jointAddedCount = 0;
            testingArray.Item1.Clear();
            testingArray.Item2.Clear();
        }

        private void SetStatus(string message)
        {
            if (Configuration.SetStatus != null)
                Configuration.SetStatus(message);
        }
    }
}
