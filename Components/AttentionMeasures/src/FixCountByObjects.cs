using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    //Psi component counting the amount of fixations in a sliding window
    public class FixCountByObjects : TimedSlidingWindowComponent<EyeMovement, Dictionary<(int, string), int>>
    {
        //Dictionnary to store fixations counts along with the object keys
        private Dictionary<(int, string), int> fixCountByObjects = new Dictionary<(int, string), int>();

        //Constructors
        public FixCountByObjects(Pipeline pipeline, string name = nameof(FixCountByObjects)) : base(pipeline, name) { }
        public FixCountByObjects(Pipeline pipeline, TimeSpan w, string name = nameof(FixCountByObjects)) : base(pipeline, w, name) { }

        //Executes upon receiving an EyeMovement
        protected override void Receive(EyeMovement input, Envelope envelope)
        {
            base.Receive(input, envelope);
        }

        protected override void ReceiveTimer(TimeSpan input, Envelope envelope)
        {
            base.ReceiveTimer(input, envelope);
            UpdateReFixationRanks();
            this.Out.Post(fixCountByObjects, envelope.OriginatingTime);
        }

        //Updating the refixations dictionary
        private void UpdateReFixationRanks()
        {
            fixCountByObjects.Clear();
            foreach (var input in inputQueue)
            {
                if (input.Item1.isFixation)
                {
                    (int, string) fixedObjectKey = input.Item1.fixedObjectKey.DeepClone();
                    if (fixCountByObjects.ContainsKey(fixedObjectKey))
                    {
                        fixCountByObjects[fixedObjectKey]++;
                    }
                    else
                    {
                        fixCountByObjects.Add(fixedObjectKey, 1);
                    }
                }
            }
        }
    }
}