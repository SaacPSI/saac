// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Bodies.Helpers
{
    using System.IO;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Helper class for bodies components, containing static utility methods.
    /// </summary>
    public class Helpers
    {
        /// <summary>
        /// Checks if all joints in an array meet the minimum confidence level.
        /// </summary>
        /// <param name="array">The array of joint data with confidence levels.</param>
        /// <param name="minimumConfidenceLevel">The minimum required confidence level.</param>
        /// <returns>True if all joints meet the minimum; otherwise false.</returns>
        public static bool CheckConfidenceLevel(in Tuple<Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel, Vector3D>[] array, Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel minimumConfidenceLevel)
        {
            if (array.Select(j => j.Item1).Any(c => c <= minimumConfidenceLevel))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Converts a Nuitrack Vector3 to a MathNet Vector3D.
        /// </summary>
        /// <param name="vect">The Nuitrack vector.</param>
        /// <returns>The MathNet vector.</returns>
        public static Vector3D NuitrackToMathNet(nuitrack.Vector3 vect)
        {
            return new Vector3D(vect.X, vect.Y, vect.Z);
        }

        /// <summary>
        /// Converts a System.Numerics Vector3 to a MathNet Vector3D.
        /// </summary>
        /// <param name="vect">The System.Numerics vector.</param>
        /// <returns>The MathNet vector.</returns>
        public static Vector3D NumericToMathNet(System.Numerics.Vector3 vect)
        {
            return new Vector3D(vect.X, vect.Y, vect.Z);
        }

        /// <summary>
        /// Converts a float confidence value to a JointConfidenceLevel enum.
        /// </summary>
        /// <param name="confidence">The confidence value (0.0 to 1.0).</param>
        /// <returns>The corresponding confidence level.</returns>
        public static Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel FloatToConfidence(float confidence)
        {
            if (confidence == 0f)
                return Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.None;
            if (confidence < 0.33f)
                return Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.Low;
            if (confidence < 0.66f)
                return Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.Medium;
            return Microsoft.Azure.Kinect.BodyTracking.JointConfidenceLevel.High;
        }

        /// <summary>
        /// Applies a transformation matrix to a 3D vector.
        /// </summary>
        /// <param name="origin">The origin vector to transform.</param>
        /// <param name="transformationMatrix">The 4x4 transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector3D CalculateTransform(Vector3D origin, Matrix<double> transformationMatrix)
        {
            Vector<double> v4Origin = Vector<double>.Build.Dense(4);
            v4Origin[0] = origin.X;
            v4Origin[1] = origin.Y;
            v4Origin[2] = origin.Z;
            v4Origin[3] = 1.0f;
            var result = v4Origin * transformationMatrix;
            return new Vector3D(result[0], result[1], result[2]);
        }

        /// <summary>
        /// Adds vector components to a list after applying a transformation.
        /// </summary>
        /// <param name="origin">The origin vector.</param>
        /// <param name="transformationMatrix">The transformation matrix to apply.</param>
        /// <param name="list">The tuple of lists to add the values to.</param>
        public static void PushToList(Vector3D origin, Matrix<double> transformationMatrix, ref Tuple<List<double>, List<double>> list)
        {
            PushToList(origin, CalculateTransform(origin, transformationMatrix), ref list);
        }

        /// <summary>
        /// Adds vector components from origin and transformed vectors to a list.
        /// </summary>
        /// <param name="origin">The origin vector.</param>
        /// <param name="transformed">The transformed vector.</param>
        /// <param name="list">The tuple of lists to add the values to.</param>
        public static void PushToList(Vector3D origin, Vector3D transformed, ref Tuple<List<double>, List<double>> list)
        {
            var ov = origin.ToVector();
            var tv = transformed.ToVector();
            for (int iterator = 0; iterator < 3; iterator++)
            {
                list.Item1.Add(ov[iterator]);
                list.Item2.Add(tv[iterator]);
            }
        }

        /// <summary>
        /// Calculates the Root Mean Square Error (RMSE) between two lists of values.
        /// </summary>
        /// <param name="list">The tuple containing the two lists to compare.</param>
        /// <returns>The calculated RMSE value.</returns>
        public static double CalculateRMSE(ref Tuple<List<double>, List<double>> list)
        {
            var offsetAndSlope = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(list.Item1.ToArray(), list.Item2.ToArray());
            var offset = offsetAndSlope.Item1;
            var slope = offsetAndSlope.Item2;

            var yBest = list.Item1.Select(p => offset + (p * slope)).ToArray();

            var RSS = MathNet.Numerics.Distance.SSD(list.Item2.ToArray(), yBest);
            var degreeOfFreedom = list.Item1.Count - 2;
            return Math.Sqrt(RSS / degreeOfFreedom);
        }

        /// <summary>
        /// Stores a calibration matrix to a file.
        /// </summary>
        /// <param name="filepath">The file path where to store the matrix.</param>
        /// <param name="matrix">The matrix to store.</param>
        public static void StoreCalibrationMatrix(string filepath, Matrix<double> matrix)
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
