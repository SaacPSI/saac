// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Helpers
{
    using System.Numerics;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that converts a tuple of two Vector3 (position and euler angles) into a Ray3D.
    /// </summary>
    public class TupleOfVectorToRay : IConsumerProducer<Tuple<Vector3, Vector3>, MathNet.Spatial.Euclidean.Ray3D>
    {
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="TupleOfVectorToRay"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public TupleOfVectorToRay(Pipeline parent, string name = nameof(TupleOfVectorToRay))
        {
            this.name = name;
            this.In = parent.CreateReceiver<Tuple<Vector3, Vector3>>(this, this.Process, $"{name}-In");
            this.Out = parent.CreateEmitter<Ray3D>(this, $"{name}-Out");
        }

        /// <summary>
        /// Converts Euler angles to a forward unit vector.
        /// </summary>
        /// <param name="eulerAngles">The euler angles in degrees (pitch, yaw, roll).</param>
        /// <returns>A normalized forward vector.</returns>
        public static MathNet.Spatial.Euclidean.UnitVector3D EulerAnglesToForwardVector(System.Numerics.Vector3 eulerAngles)
        {
            var pitch = eulerAngles.X * System.Math.PI / 180.0;
            var yaw = eulerAngles.Y * System.Math.PI / 180.0;

            var x = System.Math.Sin(yaw) * System.Math.Cos(pitch);
            var y = -System.Math.Sin(pitch);
            var z = System.Math.Cos(yaw) * System.Math.Cos(pitch);

            return new MathNet.Spatial.Euclidean.Vector3D(x, y, z).Normalize();
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Gets the receiver for input tuples of position and euler angles.
        /// </summary>
        public Receiver<Tuple<Vector3, Vector3>> In { get; private set; }

        /// <summary>
        /// Gets the emitter for output Ray3D.
        /// </summary>
        public Emitter<Ray3D> Out { get; private set; }

        /// <summary>
        /// Processes a tuple of position and euler angles, converting it to a Ray3D.
        /// </summary>
        /// <param name="data">Tuple containing position (Item1) and euler angles (Item2).</param>
        /// <param name="envelope">The message envelope.</param>
        public void Process(Tuple<Vector3, Vector3> data, Envelope envelope)
        {
            this.Out.Post(new Ray3D(new Point3D(data.Item1.X, data.Item1.Y, data.Item1.Z), EulerAnglesToForwardVector(data.Item2)), envelope.OriginatingTime);
        }
    }
}
