// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies
{
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that performs calibration between two cameras using body joint positions.
    /// </summary>
    public class CalibrationByBodies : IProducer<CoordinateSystem>
    {
        /// <summary>
        /// Gets the emitter of the calibrated coordinate system.
        /// </summary>
        public Emitter<CoordinateSystem> Out { get; private set; }

        /// <summary>
        /// Gets the connector for synchronization signals for capturing skeletons.
        /// </summary>
        private Connector<bool> InSynchEventConnector;

        /// <summary>
        /// Gets the receiver that encapsulates the synch signal.
        /// </summary>
        public Receiver<bool> InSynchEvent => InSynchEventConnector.In;

        /// <summary>
        /// Gets the connector of lists of currently tracked bodies from first camera.
        /// </summary>
        private Connector<List<SimplifiedBody>> InCamera1BodiesConnector;

        /// <summary>
        /// Gets the receiver that encapsulates the input list of skeletons from first camera.
        /// </summary>
        public Receiver<List<SimplifiedBody>> InCamera1Bodies => InCamera1BodiesConnector.In;

        /// <summary>
        /// Gets the connector of lists of currently tracked bodies from second camera.
        /// </summary>
        private Connector<List<SimplifiedBody>> InCamera2BodiesConnector;

        /// <summary>
        /// Gets the receiver that encapsulates the input list of skeletons from second camera.
        /// </summary>
        public Receiver<List<SimplifiedBody>> InCamera2Bodies => InCamera2BodiesConnector.In;

        private CalibrationByBodiesConfiguration Configuration { get; }

        private Tuple<Emgu.CV.Structure.MCvPoint3D32f[], Emgu.CV.Structure.MCvPoint3D32f[]> calibrationJoints;
        private DateTime? calibrationTime = null;
        private int jointAddedCount = 0;

        /// <summary>
        /// Enumeration of calibration states.
        /// </summary>
        private enum ECalibrationState
        {
            /// <summary>Idle state.</summary>
            Idle,

            /// <summary>Running calibration.</summary>
            Running,

            /// <summary>Testing calibration.</summary>
            Testing,
        }

        private ECalibrationState calibrationState = ECalibrationState.Running;
        private Matrix<double> transformationMatrix = Matrix<double>.Build.Dense(1, 1);
        private Tuple<List<double>, List<double>> testingArray;
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalibrationByBodies"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">Optional configuration for calibration.</param>
        /// <param name="name">Optional name for the component.</param>
        /// <param name="defaultDeliveryPolicy">Optional default delivery policy.</param>
        public CalibrationByBodies(Pipeline parent, CalibrationByBodiesConfiguration? configuration = null, string name = nameof(CalibrationByBodies), DeliveryPolicy? defaultDeliveryPolicy = null)
        {
            this.name = name;
            this.Configuration = configuration ?? new CalibrationByBodiesConfiguration();
            this.InCamera1BodiesConnector = parent.CreateConnector<List<SimplifiedBody>>($"{name}-InCamera1BodiesConnector");
            this.InCamera2BodiesConnector = parent.CreateConnector<List<SimplifiedBody>>($"{name}-InCamera2BodiesConnector");
            this.Out = parent.CreateEmitter<CoordinateSystem>(this, $"{name}-Out");
            this.InSynchEventConnector = parent.CreateConnector<bool>($"{name}-InSynchEventConnector");

            if (this.Configuration.SynchedCalibration)
            {
                this.InSynchEventConnector.Pair(this.InCamera1BodiesConnector).Pair(this.InCamera2BodiesConnector).Do(this.Process);
            }
            else
            {
                this.InCamera1BodiesConnector.Pair(this.InCamera2BodiesConnector).Do(this.Process);
            }

            Emgu.CV.Structure.MCvPoint3D32f[] camera1 = new Emgu.CV.Structure.MCvPoint3D32f[(int)this.Configuration.NumberOfJointForCalibration];
            Emgu.CV.Structure.MCvPoint3D32f[] camera2 = new Emgu.CV.Structure.MCvPoint3D32f[(int)this.Configuration.NumberOfJointForCalibration];
            this.calibrationJoints = new Tuple<Emgu.CV.Structure.MCvPoint3D32f[], Emgu.CV.Structure.MCvPoint3D32f[]>(camera1, camera2);
            this.testingArray = new Tuple<List<double>, List<double>>(new List<double>(), new List<double>());
            this.SetStatus("Collecting data...");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Process((bool, List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            this.Process((bodies.Item2, bodies.Item3), envelope);
        }

        private void Process((List<SimplifiedBody>, List<SimplifiedBody>) bodies, Envelope envelope)
        {
            switch (this.calibrationState)
            {
                case ECalibrationState.Running:
                    if (bodies.Item1.Count == bodies.Item2.Count && bodies.Item1.Count == 1)
                    {
                        this.calibrationState = this.DoCalibration(bodies.Item1[0], bodies.Item2[0], envelope.OriginatingTime);
                    }

                    break;
                case ECalibrationState.Testing:
                    if (bodies.Item1.Count == bodies.Item2.Count && bodies.Item1.Count == 1)
                    {
                        this.calibrationState = this.DoTesting(bodies.Item1[0], bodies.Item2[0], envelope.OriginatingTime);
                    }

                    break;
            }
        }

        private ECalibrationState DoCalibration(SimplifiedBody camera1, SimplifiedBody camera2, DateTime time)
        {
            if (this.calibrationTime != null)
            {
                TimeSpan interval = (TimeSpan)(time - this.calibrationTime);
                if (interval.TotalMilliseconds < 5)
                {
                    return ECalibrationState.Running;
                }
            }

            this.calibrationTime = time;
            for (JointId iterator = JointId.Pelvis; iterator < JointId.Count; iterator++)
            {
                if (camera1.Joints[iterator].Item1 >= this.Configuration.ConfidenceLevelForCalibration &&
                    camera2.Joints[iterator].Item1 >= this.Configuration.ConfidenceLevelForCalibration)
                {
                    if (this.jointAddedCount >= this.Configuration.NumberOfJointForCalibration)
                    {
                        break;
                    }

                    this.calibrationJoints.Item1[this.jointAddedCount] = this.VectorToCVPoint(camera1.Joints[iterator].Item2);
                    this.calibrationJoints.Item2[this.jointAddedCount] = this.VectorToCVPoint(camera2.Joints[iterator].Item2);
                    this.jointAddedCount++;
                }
            }

            this.SetStatus("Calibration running:  " + this.jointAddedCount.ToString() + "/" + this.Configuration.NumberOfJointForCalibration.ToString());
            if (this.jointAddedCount >= this.Configuration.NumberOfJointForCalibration)
            {
                Emgu.CV.UMat outputArray = new Emgu.CV.UMat();
                Emgu.CV.UMat inliers = new Emgu.CV.UMat();
                Emgu.CV.Util.VectorOfPoint3D32F v1 = new Emgu.CV.Util.VectorOfPoint3D32F();
                Emgu.CV.Util.VectorOfPoint3D32F v2 = new Emgu.CV.Util.VectorOfPoint3D32F();
                v1.Push(this.calibrationJoints.Item1);
                v2.Push(this.calibrationJoints.Item2);
                int retval = Emgu.CV.CvInvoke.EstimateAffine3D(v2, v1, outputArray, inliers);

                double[] tempArray = new double[12];
                var mat = outputArray.GetOutputArray().GetMat();
                mat.CopyTo(tempArray);
                mat.Dispose();
                double[,] dArray = new double[4, 4];
                for (int index = 0; index < 12; index++)
                {
                    dArray[index % 4, index / 4] = tempArray[index];
                }

                v1.Clear();
                v2.Clear();
                inliers.Dispose();
                outputArray.GetOutputArray().Dispose();
                outputArray.Dispose();
                dArray[3, 3] = 1;
                this.transformationMatrix = Matrix<double>.Build.DenseOfArray(dArray);
                this.CleanIteratorsAndCounters();
                if (this.Configuration.TestMatrixBeforeSending)
                {
                    this.SetStatus("Calibration done! Checking...");
                    return ECalibrationState.Testing;
                }
                else
                {
                    this.Out.Post(new CoordinateSystem(this.transformationMatrix), time);
                    this.SetStatus("Calibration Done");
                    Helpers.Helpers.StoreCalibrationMatrix(this.Configuration.StoringPath, this.transformationMatrix);
                    return ECalibrationState.Idle;
                }
            }

            return ECalibrationState.Running;
        }

        private ECalibrationState DoTesting(SimplifiedBody camera1, SimplifiedBody camera2, DateTime time)
        {
            if (this.calibrationTime != null)
            {
                TimeSpan interval = (TimeSpan)(time - this.calibrationTime);
                if (interval.TotalMilliseconds < 5)
                {
                    return ECalibrationState.Testing;
                }
            }

            this.calibrationTime = time;
            for (JointId iterator = JointId.Pelvis; iterator < JointId.Count; iterator++)
            {
                if (camera1.Joints[iterator].Item1 >= this.Configuration.ConfidenceLevelForCalibration && camera2.Joints[iterator].Item1 >= this.Configuration.ConfidenceLevelForCalibration)
                {
                    if (this.jointAddedCount >= this.Configuration.NumberOfJointForTesting)
                    {
                        break;
                    }

                    Helpers.Helpers.PushToList(camera2.Joints[iterator].Item2, this.transformationMatrix, ref this.testingArray);
                    this.jointAddedCount++;
                }
            }

            this.SetStatus("Checking: " + this.jointAddedCount.ToString() + "/" + this.Configuration.NumberOfJointForTesting.ToString());

            if (this.jointAddedCount >= this.Configuration.NumberOfJointForTesting)
            {
                double rmse = Helpers.Helpers.CalculateRMSE(ref this.testingArray);
                this.CleanIteratorsAndCounters();
                if (rmse < this.Configuration.AllowedMaxRMSE)
                {
                    this.SetStatus("Calibration done! RMSE: " + rmse.ToString());
                    this.Out.Post(new CoordinateSystem(this.transformationMatrix), time);
                    Helpers.Helpers.StoreCalibrationMatrix(this.Configuration.StoringPath, this.transformationMatrix);
                    return ECalibrationState.Idle;
                }
                else
                {
                    this.SetStatus("Test fail RMSE: " + rmse.ToString() + "/" + this.Configuration.AllowedMaxRMSE.ToString() + ", back to calibration");
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
            this.calibrationTime = null;
            this.jointAddedCount = 0;
            this.testingArray.Item1.Clear();
            this.testingArray.Item2.Clear();
        }

        private void SetStatus(string message)
        {
            if (this.Configuration.SetStatus != null)
            {
                this.Configuration.SetStatus(message);
            }
        }
    }
}
