using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    public class ObjectsRanker
    {
        // Receiver for the EyeMovements
        public Receiver<Dictionary<ETData, IEyeTracking>> In { get; private set; }
        // Receiver for the timer
        public Receiver<TimeSpan> TimerIn { get; private set; }
        // Emitter for the output stream
        public Emitter<Dictionary<(int, string), double>> Out { get; private set; }
        // Dictionary storing the ranking of objects (which are represented by an (ID,name) key)
        public Dictionary<(int, string), double> ObjectsRanking { get; private set; }
        private (int, string) lastGazedObject;
        private string name;
        public ObjectsRankerConfiguration Configuration { get; private set; }

        //Constructor
        public ObjectsRanker(Pipeline pipeline, ObjectsRankerConfiguration configuration = null, string name = nameof(ObjectsRanker))
        {
            this.name = name;
            Configuration = configuration ?? new ObjectsRankerConfiguration();
            ObjectsRanking = new Dictionary<(int, string), double>();
            In = pipeline.CreateReceiver<Dictionary<ETData, IEyeTracking>>(this, Receive, $"{name}-In");
            TimerIn = pipeline.CreateReceiver<TimeSpan>(this, ReceiveTimer, $"{name}-TimerIn");
            Out = pipeline.CreateEmitter<Dictionary<(int, string), double>>(this, $"{name}-Out");
            ObjectsRanking.Add((0, "NothingGazed"), 0);
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        protected void Receive(Dictionary<ETData, IEyeTracking> input, Envelope envelope)
        {
            int gazedObjectID = 0;
            string gazedObjectName = "NothingGazed";

            //If an object is currently gazed, update the values of the ID and name
            if (((EyeTrackingBool)input[ETData.IsGazingAtSomething]).content)
            {
                gazedObjectID = ((EyeTrackingInt)input[ETData.GazedObjectID]).content.DeepClone();
                gazedObjectName = ((EyeTrackingString)input[ETData.GazedObjectName]).content.DeepClone();

                //If the current gazed object has not been gazed yet, add it to the ranking
                if (!ObjectsRanking.ContainsKey((gazedObjectID, gazedObjectName)))
                {
                    ObjectsRanking.Add((gazedObjectID, gazedObjectName), 0);
                }

            }
            UpdateRanking((gazedObjectID, gazedObjectName));
        }

        protected virtual void ReceiveTimer(TimeSpan input, Envelope envelope)
        {
            Out.Post(ObjectsRanking, envelope.OriginatingTime);
        }


        private void UpdateRanking((int, string) currentGazedIDName)
        {
            List<(int, string)> keys = new List<(int, string)>(ObjectsRanking.Keys);
            foreach ((int, string) key in keys)
            {
                if (key == currentGazedIDName)
                {
                    ObjectsRanking[currentGazedIDName] = IncreaseGazedFunction(ObjectsRanking[currentGazedIDName]);
                }
                else
                {
                    ObjectsRanking[key] = DecreaseGazedFunction(ObjectsRanking[key]);
                }
            }

            //Uncomment if you want to sort the ranking
            //objectsRanking.OrderBy(x => x.Value);
        }

        private double IncreaseGazedFunction(double x)
        {

            return x + Configuration.B;
        }

        private double DecreaseGazedFunction(double x)
        {
            return Math.Max(x + Configuration.D, 0);
        }
    }
}