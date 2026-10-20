using System;
using System.IO;
using System.Numerics;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using SAAC.PipelineServices;

namespace SAAC.Components.CollaborationModules
{
    public class PositionOrientationConfiguration
    {
        public int sessionNum = 0;
        public string csvAdress = string.Empty;
        public int userID;
        public string condition = string.Empty;
        public bool isOrb = false;
    }

    public class PositionOrientationPreProcessing
    {
        public StreamWriter positionrotationWriter;
        private readonly PositionOrientationConfiguration positionOrientationConfiguration;
        // private string headWristHeadupEU = "utc_timestamp_ms,participant_id,position_x,position_y,position_z,rotation_x,rotation_y,rotation_z,left_hand_position_x,left_hand_position_y,left_hand_position_z,right_hand_position_x,right_hand_position_y,right_hand_position_z,left_hand_rotation_x,left_hand_rotation_y,left_hand_rotation_z,right_hand_rotation_x,right_hand_rotation_y,right_hand_rotation_z".Replace(',', ';');
        private string headWristHeadupEU = "utc_timestamp_ms,participant_id,type,position_x,position_y,position_z,rotation_x,rotation_y,rotation_z,rotation_w,forward_x,forward_y,forward_z,left_hand_position_x,left_hand_position_y,left_hand_position_z,right_hand_position_x,right_hand_position_y,right_hand_position_z,left_hand_rotation_x,left_hand_rotation_y,left_hand_rotation_z,right_hand_rotation_x,right_hand_rotation_y,right_hand_rotation_z".Replace(',', ';');
        private DateTime lastHeadPositionOrientationOriginatingTime;
        private DateTime lastLeftHandPositionOrientationOriginatingTime = DateTime.MinValue;
        private DateTime lastRightHandPositionOrientationOriginatingTime = DateTime.MinValue;
        private bool isHeadup = false;
        private Tuple<Vector3, Vector3> head = new Tuple<Vector3, Vector3>(Vector3.Zero, Vector3.Zero);
        private Tuple<Vector3, Vector3> left = new Tuple<Vector3, Vector3>(Vector3.Zero, Vector3.Zero);
        private Tuple<Vector3, Vector3> right = new Tuple<Vector3, Vector3>(Vector3.Zero, Vector3.Zero);
        private Session sessionName;
        private const float Deg2Rad = (float)Math.PI / 180f;
        private string type = string.Empty;

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

            if (!configuration.isOrb)
            {
                server.CreateConnectorAndStore($"{this.positionOrientationConfiguration.userID + 1}_Head", "LiveVisualization", this.sessionName, pipeline, this.HeadPositionOrientationOut.Type, this.HeadPositionOrientationOut, true);
                server.CreateConnectorAndStore($"{this.positionOrientationConfiguration.userID + 1}_Left", "LiveVisualization", this.sessionName, pipeline, this.LeftHandPositionOrientationOut.Type, this.LeftHandPositionOrientationOut, true);
                server.CreateConnectorAndStore($"{this.positionOrientationConfiguration.userID + 1}_Right", "LiveVisualization", this.sessionName, pipeline, this.RightHandPositionOrientationOut.Type, this.RightHandPositionOrientationOut, true);

                this.type = "individual";
                this.positionrotationWriter = new StreamWriter($@"{configuration.csvAdress}\{configuration.sessionNum}_{configuration.condition}-{configuration.userID + 1}_position_rotation.csv");
            }
            else
            {
                server.CreateConnectorAndStore($"{this.positionOrientationConfiguration.userID + 1}_Orb_Head", "LiveVisualization", this.sessionName, pipeline, this.HeadPositionOrientationOut.Type, this.HeadPositionOrientationOut, true);
                server.CreateConnectorAndStore($"{this.positionOrientationConfiguration.userID + 1}_Orb_Left", "LiveVisualization", this.sessionName, pipeline, this.LeftHandPositionOrientationOut.Type, this.LeftHandPositionOrientationOut, true);
                server.CreateConnectorAndStore($"{this.positionOrientationConfiguration.userID + 1}_Orb_Right", "LiveVisualization", this.sessionName, pipeline, this.RightHandPositionOrientationOut.Type, this.RightHandPositionOrientationOut, true);

                this.type = "orb";
                this.positionrotationWriter = new StreamWriter($@"{configuration.csvAdress}\{configuration.sessionNum}_{configuration.condition}-{configuration.userID + 1}_Orbs_position_rotation.csv");
            }

            if (!this.isHeadup)
            {
                this.positionrotationWriter.WriteLine(this.headWristHeadupEU);
                this.isHeadup = true;
            }
        }

        private void ProcessHeadPositionOrientationIn(Tuple<Vector3, Vector3> tuple, Envelope envelope)
        {
            Pose pose = this.Convert(tuple.Item1, tuple.Item2);

            Tuple<int, Vector3> pos = new Tuple<int, Vector3>(this.positionOrientationConfiguration.userID, pose.Position);
            Tuple<int, Vector3> rot = new Tuple<int, Vector3>(this.positionOrientationConfiguration.userID, tuple.Item2);
            Tuple<Vector3, Vector3> posrot = new Tuple<Vector3, Vector3>(pose.Position, tuple.Item2);

            string positionrotationFormat = string.Empty;
            if (this.left != null && this.right != null)
            {
                // positionrotationFormat = $"{envelope.OriginatingTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString().Replace(',', '.')};{this.positionOrientationConfiguration.userID + 1};{positionAriaFormat.X};{positionAriaFormat.Y};{positionAriaFormat.Z};{rotationAriaFormat.X};{rotationAriaFormat.Y};{rotationAriaFormat.Z};{this.left.Item1.X};{this.left.Item1.Y};{this.left.Item1.Z};{this.right.Item1.X};{this.right.Item1.Y};{this.right.Item1.Z};{this.left.Item2.X};{this.left.Item2.Y};{this.left.Item2.Z};{this.right.Item2.X};{this.right.Item2.Y};{this.right.Item2.Z}".Replace(',', '.');
                // positionrotationFormat = $"{envelope.OriginatingTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString().Replace(',', '.')};{this.positionOrientationConfiguration.userID + 1};{pose.Position.X};{pose.Position.Y};{pose.Position.Z};{pose.Rotation.X};{pose.Rotation.Y};{pose.Rotation.Z};{pose.Rotation.W};{pose.Forward.X};{pose.Forward.Y};{pose.Forward.Z};{this.left.Item1.X};{this.left.Item1.Y};{this.left.Item1.Z};{this.right.Item1.X};{this.right.Item1.Y};{this.right.Item1.Z};{this.left.Item2.X};{this.left.Item2.Y};{this.left.Item2.Z};{this.right.Item2.X};{this.right.Item2.Y};{this.right.Item2.Z}".Replace(',', '.');
                positionrotationFormat = $"{envelope.OriginatingTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString().Replace(',', '.')};{this.positionOrientationConfiguration.userID + 1};{this.type};{pose.Position.X};{pose.Position.Y};{pose.Position.Z};{pose.Rotation.X};{pose.Rotation.Y};{pose.Rotation.Z};{pose.Rotation.W};{pose.Forward.X};{pose.Forward.Y};{pose.Forward.Z};{this.left.Item1.X};{this.left.Item1.Y};{this.left.Item1.Z};{this.right.Item1.X};{this.right.Item1.Y};{this.right.Item1.Z};{this.left.Item2.X};{this.left.Item2.Y};{this.left.Item2.Z};{this.right.Item2.X};{this.right.Item2.Y};{this.right.Item2.Z}".Replace(',', '.');
            }

            this.positionrotationWriter.WriteLine(positionrotationFormat);
            this.HeadPositionOrientationOut.Post(posrot, envelope.OriginatingTime);
        }

        private void ProcessLeftHandPositionOrientationIn(Tuple<Vector3, Vector3> tuple, Envelope envelope)
        {
            Vector3 positionAriaFormat = new Vector3(tuple.Item1.X, tuple.Item1.Z, tuple.Item1.Y);
            Vector3 rotationAriaFormat = tuple.Item2;
            this.left = new Tuple<Vector3, Vector3>(positionAriaFormat, rotationAriaFormat);
            Tuple<Vector3, Vector3> leftposrot = new Tuple<Vector3, Vector3>(positionAriaFormat, rotationAriaFormat);

            this.LeftHandPositionOrientationOut.Post(leftposrot, envelope.OriginatingTime);
        }

        private void ProcessRightHandPositionOrientationIn(Tuple<Vector3, Vector3> tuple, Envelope envelope)
        {
            Vector3 positionAriaFormat = new Vector3(tuple.Item1.X, tuple.Item1.Z, tuple.Item1.Y);
            Vector3 rotationAriaFormat = tuple.Item2;
            this.right = new Tuple<Vector3, Vector3>(positionAriaFormat, rotationAriaFormat);
            Tuple<Vector3, Vector3> rightposrot = new Tuple<Vector3, Vector3>(positionAriaFormat, rotationAriaFormat);

            this.RightHandPositionOrientationOut.Post(rightposrot, envelope.OriginatingTime);
        }

        public Pose Convert(Vector3 unityPosition, Vector3 unityEulerDegrees)
        {
            Vector3 position = this.SwapYZ(unityPosition);

            Quaternion qUnity = this.EulerToQuaternion(unityEulerDegrees);
            Quaternion rotation = this.ToExternalFrame(qUnity);

            Vector3 forward = this.ForwardFromQuaternion(rotation);

            return new Pose(position, rotation, forward);
        }

        /// <summary>Swap Y and Z components. Valid for points and direction vectors.</summary>
        public Vector3 SwapYZ(Vector3 v) => new Vector3(v.X, v.Z, v.Y);

        /// <summary>
        /// Raw Unity Euler (degrees, X=pitch, Y=yaw, Z=roll) -> quaternion in Unity's frame.
        /// CreateFromYawPitchRoll composes qYaw * qPitch * qRoll, matching Unity's Z,X,Y order.
        /// </summary>
        public Quaternion EulerToQuaternion(Vector3 eulerDegrees)
        {
            return Quaternion.CreateFromYawPitchRoll(
                eulerDegrees.Y * Deg2Rad,   // yaw   (Unity Y)
                eulerDegrees.X * Deg2Rad,   // pitch (Unity X)
                eulerDegrees.Z * Deg2Rad);  // roll  (Unity Z)
        }

        /// <summary>
        /// Head forward in the external (right-handed, Z-up) frame.
        /// The head's forward is the local +Z axis of the (already reframed) orientation.
        /// Verified in post-processing: horizontal (mean pitch ~ -3 deg) with the right
        /// hand on the correct side in 99.9% of frames for both participants.
        /// </summary>
        public Vector3 ForwardFromQuaternion(Quaternion rotation)
            => Vector3.Normalize(Vector3.Transform(Vector3.UnitZ, rotation));

        /// <summary>
        /// Reframe a Unity-frame quaternion into the external (Y<->Z-swapped) frame.
        /// The swap is a reflection, hence the sign flip on the vector part.
        /// </summary>
        public Quaternion ToExternalFrame(Quaternion q) => new Quaternion(-q.X, -q.Z, -q.Y, q.W);

        /// <summary>Result of a conversion, all in the external (right-handed, Z-up) frame.</summary>
        public readonly struct Pose
        {
            public readonly Vector3 Position;   // point in the external frame
            public readonly Quaternion Rotation;   // orientation in the external frame
            public readonly Vector3 Forward;    // unit forward direction (external frame)

            public Pose(Vector3 position, Quaternion rotation, Vector3 forward)
            {
                Position = position;
                Rotation = rotation;
                Forward = forward;
            }
        }

        /// <summary>
        /// Converts a Unity Euler rotation (degrees) to a normalised forward direction Vector3.
        /// Unity convention: X = pitch (up/down), Y = yaw (left/right), Z = roll.
        /// </summary>
        public static Vector3 EulerToForward(Vector3 unityEulerDeg)
        {
            double pitch = unityEulerDeg.X * Math.PI / 180.0;
            double yaw = unityEulerDeg.Y * Math.PI / 180.0;

            return new Vector3(
                (float)(Math.Cos(pitch) * Math.Sin(yaw)),   // X
                (float)(-Math.Sin(pitch)),                    // Y  (pitch vers le haut = Y positif)
                (float)(Math.Cos(pitch) * Math.Cos(yaw)));  // Z
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
