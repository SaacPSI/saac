// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Helpers
{
    using System.Numerics;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Spatial.Euclidean;

    /// <summary>
    /// Component that converts a Matrix4x4 to a MathNet CoordinateSystem.
    /// </summary>
    public class MatrixToCoordinateSystem : IConsumerProducer<Matrix4x4, MathNet.Spatial.Euclidean.CoordinateSystem>
    {
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="MatrixToCoordinateSystem"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public MatrixToCoordinateSystem(Pipeline parent, string name = nameof(MatrixToCoordinateSystem))
        {
            this.name = name;
            this.In = parent.CreateReceiver<Matrix4x4>(this, this.Process, $"{name}-In");
            this.Out = parent.CreateEmitter<MathNet.Spatial.Euclidean.CoordinateSystem>(this, $"{name}-Out");
        }

        /// <summary>
        /// Gets the receiver for input Matrix4x4.
        /// </summary>
        public Receiver<Matrix4x4> In { get; private set; }

        /// <summary>
        /// Gets the emitter for output CoordinateSystem.
        /// </summary>
        public Emitter<MathNet.Spatial.Euclidean.CoordinateSystem> Out { get; private set; }

        /// <summary>
        /// Processes a Matrix4x4, converting it to a CoordinateSystem.
        /// </summary>
        /// <param name="data">The input Matrix4x4.</param>
        /// <param name="envelope">The message envelope.</param>
        public void Process(Matrix4x4 data, Envelope envelope)
        {
            this.Out.Post(data.ToCoordinateSystem(), envelope.OriginatingTime);
        }
    }
}
