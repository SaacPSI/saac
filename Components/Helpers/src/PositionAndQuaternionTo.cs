// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Helpers
{
    using Microsoft.Psi.Spatial.Euclidean;

    /// <summary>
    /// Static utility class for converting position and quaternion to Matrix4x4 or CoordinateSystem.
    /// </summary>
    public static class PositionAndQuaternionTo
    {
        /// <summary>
        /// Converts a position and quaternion to a 4x4 transformation matrix.
        /// </summary>
        /// <param name="position">The 3D position.</param>
        /// <param name="q">The rotation quaternion.</param>
        /// <returns>A 4x4 transformation matrix.</returns>
        public static System.Numerics.Matrix4x4 Matrix4x4(MathNet.Spatial.Euclidean.Point3D position, MathNet.Spatial.Euclidean.Quaternion q)
        {
            // Extract quaternion components
            double w = q.Real;
            double x = q.ImagX;
            double y = q.ImagY;
            double z = q.ImagZ;

            // Compute rotation matrix elements
            double xx = x * x;
            double yy = y * y;
            double zz = z * z;
            double xy = x * y;
            double xz = x * z;
            double yz = y * z;
            double wx = w * x;
            double wy = w * y;
            double wz = w * z;

            System.Numerics.Matrix4x4 matrix = default(System.Numerics.Matrix4x4);

            matrix.M11 = (float)(1 - 2 * (yy + zz));
            matrix.M12 = (float)(2 * (xy - wz));
            matrix.M13 = (float)(2 * (xz + wy));
            matrix.M14 = (float)(position.X);

            matrix.M21 = (float)(2 * (xy + wz));
            matrix.M22 = (float)(1 - 2 * (xx + zz));
            matrix.M23 = (float)(2 * (yz - wx));
            matrix.M24 = (float)(position.Y);

            matrix.M31 = (float)(2 * (xz - wy));
            matrix.M32 = (float)(2 * (yz + wx));
            matrix.M33 = (float)(1 - 2 * (xx + yy));
            matrix.M34 = (float)(position.Z);

            matrix.M41 = 0;
            matrix.M42 = 0;
            matrix.M43 = 0;
            matrix.M44 = 1;

            return matrix;
        }

        /// <summary>
        /// Converts a position and quaternion to a coordinate system.
        /// </summary>
        /// <param name="position">The 3D position.</param>
        /// <param name="q">The rotation quaternion.</param>
        /// <returns>A coordinate system.</returns>
        public static MathNet.Spatial.Euclidean.CoordinateSystem CoordinateSystem(MathNet.Spatial.Euclidean.Point3D position, MathNet.Spatial.Euclidean.Quaternion q)
        {
            return Matrix4x4(position, q).ToCoordinateSystem();
        }
    }
}
