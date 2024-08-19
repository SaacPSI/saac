using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace SAAC.AttentionMeasures
{ 
    //Psi component classifying the raw EyeTracking data into eye movements (fixations or saccades)
    public class TestInput : ConsumerProducer<Dictionary<ETData, IEyeTracking>, bool>
    {
        //Last received message and timestamp
        private Dictionary<ETData, IEyeTracking> lastInput;

        //Constructor
        public TestInput(Pipeline pipeline) : base(pipeline) { lastInput = new EyeTrackingTemplate().content; }

        //Executes upon receiving a message
        protected override void Receive(Dictionary<ETData, IEyeTracking> input, Envelope envelope)
        {

            Out.Post(input == lastInput, envelope.OriginatingTime);

            //Updating last input
            this.lastInput = input.DeepClone();
        }
    }
}
