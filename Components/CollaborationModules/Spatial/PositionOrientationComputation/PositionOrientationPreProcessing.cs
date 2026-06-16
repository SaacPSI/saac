using System;
using System.Collections.Generic;
using System.IdentityModel.Claims;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.Spatial.Euclidean;
using SAAC.PipelineServices;

namespace SAAC.Components.CollaborationModules
{
    public class PositionOrientationConfiguration
    {
        public int sessionNum = 0;
        public string csvAdress = string.Empty;
        public int userID;
    }

    public class PositionOrientationPreProcessing
    {
        private readonly PositionOrientationConfiguration positionOrientationConfiguration;
        public StreamWriter positionrotationWriter;
        private string headWristHeadupEU = "utc_timestamp_ms,participant_id,position_x,position_y,position_z,rotation_x,rotation_y,rotation_z,left_hand_position_x,left_hand_position_y,left_hand_position_z,right_hand_position_x,right_hand_position_y,right_hand_position_z,left_hand_rotation_x,left_hand_rotation_y,left_hand_rotation_z,right_hand_rotation_x,right_hand_rotation_y,right_hand_rotation_z".Replace(',', ';');
        private DateTime lastHeadPositionOrientationOriginatingTime;
        private DateTime lastLeftHandPositionOrientationOriginatingTime = DateTime.MinValue;
        private DateTime lastRightHandPositionOrientationOriginatingTime = DateTime.MinValue;
        private bool isHeadup = false;
        private Tuple<Vector3, Vector3> head = new Tuple<Vector3, Vector3>(Vector3.Zero, Vector3.Zero);
        private Tuple<Vector3, Vector3> left = new Tuple<Vector3, Vector3>(Vector3.Zero, Vector3.Zero);
        private Tuple<Vector3, Vector3> right = new Tuple<Vector3, Vector3>(Vector3.Zero, Vector3.Zero);
        private Session sessionName;

        public PositionOrientationPreProcessing(Pipeline pipeline, DatasetPipeline server, PositionOrientationConfiguration? configuration = null)
        {
            this.positionOrientationConfiguration = configuration ?? new PositionOrientationConfiguration();

            // Receivers declaration
            this.HeadPositionOrientationIn = pipeline.CreateReceiver<Tuple<Vector3, Vector3>>(this, this.ProcessHeadPositionOrientationIn, nameof(this.HeadPositionOrientationIn));
            this.LeftHandPositionOrientationIn = pipeline.CreateReceiver<Tuple<Vector3, Vector3>>(this, this.ProcessLeftHandPositionOrientationIn, nameof(this.LeftHandPositionOrientationIn));
            this.RightHandPositionOrientationIn = pipeline.CreateReceiver<Tuple<Vector3, Vector3>>(this, this.ProcessRightHandPositionOrientationIn, nameof(this.RightHandPositionOrientationIn));

            // Emitters declaration
            this.HeadPositionOut = pipeline.CreateEmitter<Tuple<int, Vector3>>(this, nameof(this.HeadPositionOut));
            this.LeftHandPositionOut = pipeline.CreateEmitter<Tuple<int, Vector3>>(this, nameof(this.LeftHandPositionOut));
            this.RightHandPositionOut = pipeline.CreateEmitter<Tuple<int, Vector3>>(this, nameof(this.RightHandPositionOut));
            this.HeadOrientationOut = pipeline.CreateEmitter<Tuple<int, Vector3>>(this, nameof(this.HeadOrientationOut));
            this.HeadPositionOrientationOut = pipeline.CreateEmitter<Tuple<Vector3, Vector3>>(this, nameof(this.HeadPositionOrientationOut));
            this.HeadPositionOrientationQuatOut = pipeline.CreateEmitter<Tuple<Vector3, System.Numerics.Quaternion>>(this, nameof(this.HeadPositionOrientationQuatOut));
            this.LeftHandPositionOrientationOut = pipeline.CreateEmitter<Tuple<Vector3, Vector3>>(this, nameof(this.LeftHandPositionOrientationOut));
            this.RightHandPositionOrientationOut = pipeline.CreateEmitter<Tuple<Vector3, Vector3>>(this, nameof(this.RightHandPositionOrientationOut));

            this.sessionName = server.GetSession("RawDataPipelineProcess.000");

            server.CreateConnectorAndStore($"{this.positionOrientationConfiguration.userID + 1}_Head", "LiveVisualization", this.sessionName, pipeline, this.HeadPositionOrientationOut.Type, this.HeadPositionOrientationOut, true);
            server.CreateConnectorAndStore($"{this.positionOrientationConfiguration.userID + 1}_Left", "LiveVisualization", this.sessionName, pipeline, this.LeftHandPositionOrientationOut.Type, this.LeftHandPositionOrientationOut, true);
            server.CreateConnectorAndStore($"{this.positionOrientationConfiguration.userID + 1}_Right", "LiveVisualization", this.sessionName, pipeline, this.RightHandPositionOrientationOut.Type, this.RightHandPositionOrientationOut, true);

            this.positionrotationWriter = new StreamWriter($@"{configuration.csvAdress}\{configuration.sessionNum}-{configuration.userID + 1}_position_rotation.csv");

            if (!this.isHeadup)
            {
                this.positionrotationWriter.WriteLine(this.headWristHeadupEU);
                this.isHeadup = true;
            }
        }

        private void ProcessHeadPositionOrientationIn(Tuple<Vector3, Vector3> tuple, Envelope envelope)
        {
            Vector3 positionAriaFormat = new Vector3(tuple.Item1.X, tuple.Item1.Z, tuple.Item1.Y);
            Vector3 rotationAriaFormat = new Vector3(tuple.Item2.X, tuple.Item2.Z, tuple.Item2.Y);

            Tuple<int, Vector3> pos = new Tuple<int, Vector3>(this.positionOrientationConfiguration.userID, positionAriaFormat);
            Tuple<int, Vector3> rot = new Tuple<int, Vector3>(this.positionOrientationConfiguration.userID, rotationAriaFormat);

            if (this.lastHeadPositionOrientationOriginatingTime < envelope.OriginatingTime.AddMilliseconds(-50))
            {
                this.HeadPositionOut.Post(pos, envelope.OriginatingTime);
                this.lastHeadPositionOrientationOriginatingTime = envelope.OriginatingTime;
            }

            string positionrotationFormat = string.Empty;
            if (this.left != null && this.right != null)
            {
                positionrotationFormat = $"{envelope.OriginatingTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString().Replace(',', '.')};{this.positionOrientationConfiguration.userID + 1};{positionAriaFormat.X};{positionAriaFormat.Y};{positionAriaFormat.Z};{rotationAriaFormat.X};{rotationAriaFormat.Y};{rotationAriaFormat.Z};{this.left.Item1.X};{this.left.Item1.Y};{this.left.Item1.Z};{this.right.Item1.X};{this.right.Item1.Y};{this.right.Item1.Z};{this.left.Item2.X};{this.left.Item2.Y};{this.left.Item2.Z};{this.right.Item2.X};{this.right.Item2.Y};{this.right.Item2.Z}".Replace(',', '.');
            }

            this.positionrotationWriter.WriteLine(positionrotationFormat);
        }

        private void ProcessLeftHandPositionOrientationIn(Tuple<Vector3, Vector3> tuple, Envelope envelope)
        {
            Vector3 positionAriaFormat = new Vector3(tuple.Item1.X, tuple.Item1.Z, tuple.Item1.Y);
            Vector3 rotationAriaFormat = new Vector3(tuple.Item2.X, tuple.Item2.Z, tuple.Item2.Y);
            this.left = new Tuple<Vector3, Vector3>(positionAriaFormat, rotationAriaFormat);
            if (this.lastLeftHandPositionOrientationOriginatingTime < envelope.OriginatingTime.AddMilliseconds(-50))
            {
                this.LeftHandPositionOut.Post(new Tuple<int, Vector3>(this.positionOrientationConfiguration.userID, positionAriaFormat), envelope.OriginatingTime);
                this.lastLeftHandPositionOrientationOriginatingTime = envelope.OriginatingTime;
            }
        }

        private void ProcessRightHandPositionOrientationIn(Tuple<Vector3, Vector3> tuple, Envelope envelope)
        {
            Vector3 positionAriaFormat = new Vector3(tuple.Item1.X, tuple.Item1.Z, tuple.Item1.Y);
            Vector3 rotationAriaFormat = new Vector3(tuple.Item2.X, tuple.Item2.Z, tuple.Item2.Y);
            this.right = new Tuple<Vector3, Vector3>(positionAriaFormat, rotationAriaFormat);
            if (this.lastRightHandPositionOrientationOriginatingTime < envelope.OriginatingTime.AddMilliseconds(-50))
            {
                this.RightHandPositionOut.Post(new Tuple<int, Vector3>(this.positionOrientationConfiguration.userID, positionAriaFormat), envelope.OriginatingTime);
                this.lastRightHandPositionOrientationOriginatingTime = envelope.OriginatingTime;
            }
        }

        #region Receivers

        public Receiver<Tuple<Vector3, Vector3>> HeadPositionOrientationIn { get; set; }

        public Receiver<Tuple<Vector3, Vector3>> LeftHandPositionOrientationIn { get; set; }

        public Receiver<Tuple<Vector3, Vector3>> RightHandPositionOrientationIn { get; set; }
        #endregion

        #region Emitters

        public Emitter<Tuple<int, Vector3>> HeadPositionOut { get; set; }

        public Emitter<Tuple<int, Vector3>> HeadOrientationOut { get; set; }

        public Emitter<Tuple<int, Vector3>> LeftHandPositionOut { get; set; }

        public Emitter<Tuple<int, Vector3>> RightHandPositionOut { get; set; }

        public Emitter<Tuple<Vector3, Vector3>> HeadPositionOrientationOut { get; set; }

        public Emitter<Tuple<Vector3, System.Numerics.Quaternion>> HeadPositionOrientationQuatOut { get; set; }

        public Emitter<Tuple<Vector3, Vector3>> LeftHandPositionOrientationOut { get; set; }

        public Emitter<Tuple<Vector3, Vector3>> RightHandPositionOrientationOut { get; set; }
        #endregion

    }

}
