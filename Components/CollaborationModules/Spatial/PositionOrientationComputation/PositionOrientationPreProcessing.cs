using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.Spatial.Euclidean;
using SAAC.PipelineServices;

namespace SAAC.Components.CollaborationModules
{
    public class PositionOrientationConfiguration
    {
    }

    public class PositionOrientationPreProcessing
    {
        private readonly PositionOrientationConfiguration posRotConfiguration;

        public PositionOrientationPreProcessing(Pipeline pipeline, DatasetPipeline server, PositionOrientationConfiguration? configuration = null)
        {
            // Receivers declaration
            this.HeadPositionOrientationIn = pipeline.CreateReceiver<Tuple<Vector3, Vector3>>(this, ProcessHeadPositionOrientationIn, nameof(this.HeadPositionOrientationIn));
            this.LeftHandPositionOrientationIn = pipeline.CreateReceiver<Tuple<Vector3, Vector3>>(this, ProcessLeftHandPositionOrientationIn, nameof(this.LeftHandPositionOrientationIn));
            this.RightHandPositionOrientationIn = pipeline.CreateReceiver<Tuple<Vector3, Vector3>>(this, ProcessRightHandPositionOrientationIn, nameof(this.RightHandPositionOrientationIn));

            // Emitters declaration
            this.HeadPositionOut = pipeline.CreateEmitter<Tuple<int, Vector3>>(this, nameof(this.HeadPositionOut));
            this.LeftHandPositionOut = pipeline.CreateEmitter<Tuple<int, Vector3>>(this, nameof(this.LeftHandPositionOut));
            this.RightHandPositionOut = pipeline.CreateEmitter<Tuple<int, Vector3>>(this, nameof(this.RightHandPositionOut));
            this.HeadOrientationOut = pipeline.CreateEmitter<Tuple<int, Vector3>>(this, nameof(this.HeadOrientationOut));
            this.HeadPositionOrientationOut = pipeline.CreateEmitter<Tuple<Vector3, Vector3>>(this, nameof(this.HeadPositionOrientationOut));
            this.HeadPositionOrientationQuatOut = pipeline.CreateEmitter<Tuple<Vector3, System.Numerics.Quaternion>>(this, nameof(this.HeadPositionOrientationQuatOut));
            this.LeftHandPositionOrientationOut = pipeline.CreateEmitter<Tuple<Vector3, Vector3>>(this, nameof(this.LeftHandPositionOrientationOut));
            this.RightHandPositionOrientationOut = pipeline.CreateEmitter<Tuple<Vector3, Vector3>>(this, nameof(this.RightHandPositionOrientationOut));
        }

        private void ProcessHeadPositionOrientationIn(Tuple<Vector3, Vector3> tuple, Envelope envelope)
        {
            throw new NotImplementedException();
        }

        private void ProcessLeftHandPositionOrientationIn(Tuple<Vector3, Vector3> tuple, Envelope envelope)
        {
            throw new NotImplementedException();
        }

        private void ProcessRightHandPositionOrientationIn(Tuple<Vector3, Vector3> tuple, Envelope envelope)
        {
            throw new NotImplementedException();
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
