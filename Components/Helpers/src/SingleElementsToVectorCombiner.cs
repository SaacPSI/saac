// <copyright file="SingleElementsToVectorCombiner.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

namespace Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Provides predefined <see cref="Func{TResult}"/> combiners for use with
    /// <see cref="SingleElementsToVector{Tin, Tout}"/> targeting common numeric vector types.
    /// </summary>
    /// <remarks>
    /// All combiners assume the input list elements are ordered by receiver index (X=0, Y=1, Z=2, W=3).
    /// </remarks>
    public static class SingleElementsToVectorCombiner
    {
        /// <summary>
        /// Combines 2 <see cref="float"/> values into a <see cref="Vector2"/> (X, Y).
        /// </summary>
        public static readonly Func<List<float>, Vector2> ToVector2 =
            list => new Vector2(list[0], list[1]);

        /// <summary>
        /// Combines 3 <see cref="float"/> values into a <see cref="Vector3"/> (X, Y, Z).
        /// </summary>
        public static readonly Func<List<float>, Vector3> ToVector3 =
            list => new Vector3(list[0], list[1], list[2]);

        /// <summary>
        /// Combines 4 <see cref="float"/> values into a <see cref="Vector4"/> (X, Y, Z, W).
        /// </summary>
        public static readonly Func<List<float>, Vector4> ToVector4 =
            list => new Vector4(list[0], list[1], list[2], list[3]);

        /// <summary>
        /// Combines 3 <see cref="double"/> values into a <see cref="Point3D"/> (X, Y, Z).
        /// </summary>
        public static readonly Func<List<double>, Point3D> ToPoint3D =
            list => new Point3D(list[0], list[1], list[2]);

        /// <summary>
        /// Combines 3 <see cref="double"/> values into a <see cref="Vector3D"/> (X, Y, Z).
        /// </summary>
        public static readonly Func<List<double>, Vector3D> ToVector3D =
            list => new Vector3D(list[0], list[1], list[2]);
    }
}
