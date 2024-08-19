using Microsoft.Psi;
using System.Windows.Media.Animation;
using System.Xml.Linq;

namespace SAAC.AttentionMeasures
{
    public class ObjectRankDisplay
    {
        // Receiver for the EyeMovements
        public Receiver<Dictionary<(int, string), double>> In { get; private set; }
        // Emitter for the output stream
        public Emitter<double> ScoreOut { get; private set; }
        // Emitter for the output stream
        public Emitter<string> NameOut { get; private set; }

        private string objectName;
        private int objectNumber;
        private string name;


        //Constructor
        public ObjectRankDisplay(Pipeline pipeline, string objectName, string name = nameof(ObjectRankDisplay))
        {
            this.objectName = objectName;
            this.name = name;
            InitialiseReceiverEmitter(pipeline);
        }

        public ObjectRankDisplay(Pipeline pipeline, int objectNumber, string name = nameof(ObjectRankDisplay))
        {
            this.objectNumber = objectNumber;
            this.name = name;
            InitialiseReceiverEmitter(pipeline);
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void InitialiseReceiverEmitter(Pipeline pipeline)
        {
            In = pipeline.CreateReceiver<Dictionary<(int, string), double>>(this, Receive, $"{name}-In");
            ScoreOut = pipeline.CreateEmitter<double>(this, $"{name}-RankingOut");
            NameOut = pipeline.CreateEmitter<string>(this, $"{name}-NameOut");
        }

        protected void Receive(Dictionary<(int, string), double> input, Envelope envelope)
        {
            double score = 0;
            string name = "";

            if (input.Count > objectNumber + 2)
            {
                score = input.ElementAt(objectNumber + 1).Value;
                name = input.ElementAt(objectNumber + 1).Key.Item2;
            }

            ScoreOut.Post(score, envelope.OriginatingTime);
            NameOut.Post(name, envelope.OriginatingTime);
        }
    }
}
