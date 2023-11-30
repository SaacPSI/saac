using MathNet.Spatial.Euclidean;
using MathNet.Numerics.LinearAlgebra;
using System.IO;

namespace Helpers
{
    /// <summary>
    /// Helpers class for bodies components, containing static methods used across components.
    /// </summary>
    public class Helpers
    {
        static public bool CheckConfidenceLevel(in Tuple<Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel, Vector3D>[] array, Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel minimumConfidenceLevel)
        {
            if (array.Select(j => j.Item1).Any(c => c <= minimumConfidenceLevel))
                return false;
            return true;
        }

        static public Vector3D NuitrackToMathNet(nuitrack.Vector3 vect)
        {
            return new Vector3D(vect.X, vect.Y, vect.Z);
        }

        static public Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel FloatToConfidence(float confidence)
        {
            if (confidence == 0f)
                return Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.None;
            if (confidence < 0.33f)
                return Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.Low;
            if (confidence < 0.66f)
                return Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.Medium;
            return Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.High;
        }

        static public Vector3D CalculateTransform(Vector3D origin, Matrix<double> transformationMatrix)
        {
            Vector<double> v4Origin = Vector<double>.Build.Dense(4);
            v4Origin[0] = origin.X;
            v4Origin[1] = origin.Y;
            v4Origin[2] = origin.Z;
            v4Origin[3] = 1.0f;
            var result = v4Origin * transformationMatrix;
            return new Vector3D(result[0], result[1], result[2]);
        }

        static public void PushToList(Vector3D origin, Matrix<double> transformationMatrix, ref Tuple<List<double>, List<double>> list)
        {
            PushToList(origin, CalculateTransform(origin, transformationMatrix), ref list);
        }

        static public void PushToList(Vector3D origin, Vector3D transformed, ref Tuple<List<double>, List<double>> list)
        {
            var ov = origin.ToVector();
            var tv = transformed.ToVector();
            for (int iterator = 0; iterator < 3; iterator++)
            {
                list.Item1.Add(ov[iterator]);
                list.Item2.Add(tv[iterator]);
            }
        }

        static public double CalculateRMSE(ref Tuple<List<double>, List<double>> list)
        {
            //From https://github.com/mathnet/mathnet-numerics/issues/673
            var offsetAndSlope = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(list.Item1.ToArray(), list.Item2.ToArray());
            var offset = offsetAndSlope.Item1;
            var slope = offsetAndSlope.Item2;

            var yBest = list.Item1.Select(p => offset + p * slope).ToArray(); // Best fitted y values

            var RSS = MathNet.Numerics.Distance.SSD(list.Item2.ToArray(), yBest);
            var degreeOfFreedom = list.Item1.Count - 2;
            return Math.Sqrt(RSS / degreeOfFreedom);
        }

        static public void StoreCalibrationMatrix(string filepath, Matrix<double> matrix)
        {
            if (filepath.Length > 4)
                File.WriteAllText(filepath, matrix.ToMatrixString());
        }

        static public bool ReadCalibrationFromFile(string filepath, out Matrix<double> matrix)
        {
            matrix = Matrix<double>.Build.DenseIdentity(4, 4);
            try
            {
                var matrixStr = File.ReadLines(filepath);
                int count = 0;
                double[,] valuesD = new double[4, 4];
                foreach (string line in matrixStr)
                {
                    foreach (string value in line.Split(' '))
                    {
                        if (value.Length == 0)
                            continue;
                        valuesD[count / 4, count % 4] = Double.Parse(value);
                        count++;
                    }
                }
                matrix = Matrix<double>.Build.DenseOfArray(valuesD);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message);
                return false;
            }
            return true;
        }

        public static bool IsValidDouble(double val)
        {
            if (Double.IsNaN(val))
                return false;
            if (Double.IsInfinity(val))
                return false;
            return true;
        }

        public static bool IsValidPoint2D(Point2D point) => IsValidDouble(point.X) && IsValidDouble(point.Y);
        public static bool IsValidVector2D(Vector2D vector) => IsValidDouble(vector.X) && IsValidDouble(vector.Y);
        public static bool IsValidPoint3D(Point3D point) => IsValidDouble(point.X) && IsValidDouble(point.Y) && IsValidDouble(point.Z);
        public static bool IsValidVector3D(Vector3D vector) => IsValidDouble(vector.X) && IsValidDouble(vector.Y) && IsValidDouble(vector.Z);
    }
}
