using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using System.Numerics;

namespace SAAC.Helpers
{
    public class TupleOfVectorToRay : IConsumerProducer<Tuple<Vector3, Vector3>, MathNet.Spatial.Euclidean.Ray3D>
    {
        public Receiver<Tuple<Vector3, Vector3>> In { get; private set; }
        public Emitter<Ray3D> Out { get; private set; }

        private string name;
        public TupleOfVectorToRay(Pipeline parent, string name = nameof(TupleOfVectorToRay))
        {
            this.name = name;
            In = parent.CreateReceiver<Tuple<Vector3, Vector3>>(this, Process, $"{name}-In");
            Out = parent.CreateEmitter<Ray3D>(this, $"{name}-Out");
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        public void Process(Tuple<Vector3, Vector3> data, Envelope envelope)
        {
            Out.Post(new Ray3D(new Point3D(data.Item1.X, data.Item1.Y, data.Item1.Z), UnitVector3D.Create(data.Item2.X, data.Item2.Y, data.Item2.Z)), envelope.OriginatingTime);
        }
    }
}
