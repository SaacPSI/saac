using Newtonsoft.Json;
using System.Collections.Immutable;
using System.Numerics;

///From OpenSense : https://github.com/intelligent-human-perception-laboratory/OpenSense
// Nuget for HashCode => Microsoft.Bcl.HashCode
namespace Helpers
{
    public class ActionUnit : IEquatable<ActionUnit>
    {
        public readonly double Intensity;

        public readonly double Presence;

        [JsonConstructor]
        public ActionUnit(double intensity, double presence)
        {
            Intensity = intensity;
            Presence = presence;
        }

        #region IEquatable
        public bool Equals(ActionUnit other) =>
            Intensity.Equals(other.Intensity)
            && Presence.Equals(other.Presence)
            ;

        public override bool Equals(object obj) => obj is ActionUnit other ? Equals(other) : false;

        public override int GetHashCode() => HashCode.Combine(
            Intensity,
            Presence
        );

        public static bool operator ==(ActionUnit a, ActionUnit b) => a.Equals(b);

        public static bool operator !=(ActionUnit a, ActionUnit b) => !(a == b);
        #endregion
    }

    public class GazeVector : IEquatable<GazeVector>
    {
        public readonly Vector3 Left;

        public readonly Vector3 Right;

        [JsonConstructor]
        public GazeVector(Vector3 left, Vector3 right)
        {
            Left = left;
            Right = right;
        }

        #region IEquatable
        public bool Equals(GazeVector other) =>
            Left.Equals(other.Left)
            && Right.Equals(other.Right)
            ;

        public override bool Equals(object obj) => obj is GazeVector other ? Equals(other) : false;

        public override int GetHashCode() => HashCode.Combine(
            Left,
            Right
        );

        public static bool operator ==(GazeVector a, GazeVector b) => a.Equals(b);

        public static bool operator !=(GazeVector a, GazeVector b) => !(a == b);
        #endregion
    }

    public class Eye : IEquatable<Eye>
    {
        /// <summary>
        /// Normalized left pupil vector to camera
        /// </summary>

        public readonly GazeVector GazeVector;

        /// <summary>
        /// Absolute gaze angle to camera in radian、
        /// mean of eyes
        /// </summary>

        public readonly Vector2 Angle;
        public readonly IReadOnlyList<Vector2> Landmarks;
        public readonly IReadOnlyList<Vector3> Landmarks3D;
        public readonly IReadOnlyList<Vector2> VisiableLandmarks;
        public readonly IReadOnlyList<ValueTuple<Vector2, Vector2>> IndicatorLines;


        public Eye(
            GazeVector gazeVector,
            Vector2 angle,
            IEnumerable<Vector2> landmarks,
            IEnumerable<Vector2> visiableLandmarks,
            IEnumerable<Vector3> landmarks3D,
            IEnumerable<ValueTuple<Vector2, Vector2>> indicatorLines
            )
        {
            GazeVector = gazeVector;
            Angle = angle;
            Landmarks = landmarks.ToImmutableArray();
            Landmarks3D = landmarks3D.ToImmutableArray();
            VisiableLandmarks = visiableLandmarks.ToImmutableArray();
            IndicatorLines = indicatorLines.ToImmutableArray();
        }


        public GazeVector PupilPosition
        {
            get
            {
                var leftLandmarks = Landmarks3D.Skip(0).Take(8).ToList();
                var leftSum = leftLandmarks.Aggregate((a, b) => a + b);
                var left = leftSum / leftLandmarks.Count;
                var rightLandmarks = Landmarks3D.Skip(28).Take(8).ToList();
                var rightSum = rightLandmarks.Aggregate((a, b) => a + b);
                var right = rightSum / rightLandmarks.Count;
                return new GazeVector(left, right);
            }
        }


        public GazeVector InnerEyeCornerPosition => new GazeVector(Landmarks3D[14], Landmarks3D[36]);

        #region IEquatable
        public bool Equals(Eye other) =>
            Landmarks.SequenceEqual(other.Landmarks)
            && VisiableLandmarks.SequenceEqual(other.VisiableLandmarks)
            && Landmarks3D.SequenceEqual(other.Landmarks3D)
            && GazeVector.Equals(other.GazeVector)
            && Angle.Equals(other.Angle);

        public override bool Equals(object obj) => obj is Eye other ? Equals(other) : false;

        public override int GetHashCode() => HashCode.Combine(
            Landmarks,
            VisiableLandmarks,
            Landmarks3D,
            GazeVector,
            Angle
        );

        public static bool operator ==(Eye a, Eye b) => a.Equals(b);

        public static bool operator !=(Eye a, Eye b) => !(a == b);
        #endregion
    }

    public class Face : IEquatable<Face>
    {
        public readonly IReadOnlyDictionary<string, ActionUnit> ActionUnits;

        public Face(IDictionary<string, ActionUnit> actionUnits)
        {
            ActionUnits = actionUnits.ToImmutableSortedDictionary();
        }

        #region IEquatable
        public bool Equals(Face other) =>
            ActionUnits.SequenceEqual(other.ActionUnits);

        public override bool Equals(object obj) => obj is Face other ? Equals(other) : false;

        public override int GetHashCode() => HashCode.Combine(
            ActionUnits
        );

        public static bool operator ==(Face a, Face b) => a.Equals(b);

        public static bool operator !=(Face a, Face b) => !(a == b);
        #endregion
    }

    public class Pose /*: IEnumerable<double>, IEquatable<Pose>*/
    {//Interfaces removed since no support for JsonObjectAttribute after Json.Net is removed

        /// <summary>
        /// Absolute head postion to camera in millimeter
        /// </summary>
        public readonly Vector3 Position;

        /// <summary>
        /// Absolute head rotation to camera in radian
        /// </summary>
        public readonly Vector3 Angle;

        public readonly IReadOnlyList<Vector2> Landmarks;

        public readonly IReadOnlyList<Vector2> VisiableLandmarks;


        public readonly IReadOnlyList<Vector3> Landmarks3D;

        public readonly IReadOnlyList<ValueTuple<Vector2, Vector2>> IndicatorLines;

        [JsonConstructor]
        public Pose(
            Vector3 position,
            Vector3 angle,
            ImmutableArray<Vector2> landmarks,
            ImmutableArray<Vector2> visiableLandmarks,
            ImmutableArray<Vector3> landmarks3D,
            ImmutableArray<ValueTuple<Vector2, Vector2>> indicatorLines
            )
        {
            IndicatorLines = indicatorLines;
            Landmarks = landmarks;
            VisiableLandmarks = visiableLandmarks;
            Landmarks3D = landmarks3D;
            Position = position;
            Angle = angle;
        }

        public Pose(
            IList<float> data,
            IEnumerable<Vector2> landmarks,
            IEnumerable<Vector2> visiableLandmarks,
            IEnumerable<Vector3> landmarks3D,
            IEnumerable<ValueTuple<Vector2, Vector2>> indicatorLines
            ) : this(
                new Vector3(data[0], data[1], data[2]),
                new Vector3(data[3], data[4], data[5]),
                landmarks.ToImmutableArray(),
                visiableLandmarks.ToImmutableArray(),
                landmarks3D.ToImmutableArray(),
                indicatorLines.ToImmutableArray()
                )
        { }


        public Vector3 NoseTip3D => Landmarks3D[30];

        #region To accommodate old code

        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return Position.X;
                    case 1:
                        return Position.Y;
                    case 2:
                        return Position.Z;
                    case 3:
                        return Angle.X;
                    case 4:
                        return Angle.Y;
                    case 5:
                        return Angle.Z;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }

        public int Count => 6;

        public IEnumerator<double> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }


        #endregion

        #region IEquatable
        public bool Equals(Pose other) =>
            Landmarks.SequenceEqual(other.Landmarks)
            && VisiableLandmarks.SequenceEqual(other.VisiableLandmarks)
            && Landmarks3D.SequenceEqual(other.Landmarks3D)
            && Position.Equals(other.Position)
            && Angle.Equals(other.Angle);

        public override bool Equals(object obj) => obj is Pose other ? Equals(other) : false;

        public override int GetHashCode() => HashCode.Combine(
            Landmarks,
            VisiableLandmarks,
            Landmarks3D,
            Position,
            Angle
        );

        public static bool operator ==(Pose a, Pose b) => a.Equals(b);

        public static bool operator !=(Pose a, Pose b) => !(a == b);
        #endregion
    }

    public class PoseAndEyeAndFace : IEquatable<PoseAndEyeAndFace>
    {

        public readonly Pose Pose;

        public readonly Eye Eye;

        public readonly Face Face;

        public PoseAndEyeAndFace(Pose headPose, Eye gaze, Face face)
        {
            Pose = headPose;
            Eye = gaze;
            Face = face;
        }

        #region IEquatable
        public bool Equals(PoseAndEyeAndFace other) =>
            Pose.Equals(other.Pose)
            && Eye.Equals(other.Eye)
            && Face.Equals(other.Face)
            ;

        public override bool Equals(object obj) => obj is PoseAndEyeAndFace other ? Equals(other) : false;

        public override int GetHashCode() => HashCode.Combine(
            Pose,
            Eye,
            Face
        );

        public static bool operator ==(PoseAndEyeAndFace a, PoseAndEyeAndFace b) => a.Equals(b);

        public static bool operator !=(PoseAndEyeAndFace a, PoseAndEyeAndFace b) => !(a == b);
        #endregion
    }
}
