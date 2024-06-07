using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Spatial.Euclidean;
using System.Numerics;

namespace SAAC.Helpers
{
    public class MatrixToCoordinateSystem : IConsumerProducer<Matrix4x4, MathNet.Spatial.Euclidean.CoordinateSystem>
    {
        public Receiver<Matrix4x4> In { get; private set; }
        public Emitter<MathNet.Spatial.Euclidean.CoordinateSystem> Out { get; private set; }

        private string name;
        public MatrixToCoordinateSystem(Pipeline parent, string name = nameof(MatrixToCoordinateSystem))
        {
            this.name = name;
            In = parent.CreateReceiver<Matrix4x4>(this, Process, $"{name}-In");
            Out = parent.CreateEmitter<MathNet.Spatial.Euclidean.CoordinateSystem>(this, $"{name}-Out");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        public void Process(Matrix4x4 data, Envelope envelope)
        {
            Out.Post(data.ToCoordinateSystem(), envelope.OriginatingTime);
        }
    }
}