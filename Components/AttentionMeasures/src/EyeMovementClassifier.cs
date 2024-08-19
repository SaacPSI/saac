using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace SAAC.AttentionMeasures
{
    //Psi component classifying the raw EyeTracking data into eye movements (fixations or saccades)
    public class EyeMovementClassifier : ConsumerProducer<Dictionary<ETData, IEyeTracking>, EyeMovement>
    {
        //Last received message and timestamp
        private Dictionary<ETData, IEyeTracking> lastInput;
        private DateTime lastTimeStamp;
        private string name;
        //Last EyeMovement recorded
        private EyeMovement lastEyeMovement;
        //List to store the gaze direction of messages in the last movement
        private List<System.Numerics.Vector3> directionList = new List<System.Numerics.Vector3>();
        //List to store the gazed object identification keys (UnityID, name) of messages in the last movement
        private List<(int, string)> gazedObjectKeyList = new List<(int, string)>();
        //Threshold determining the classification of fixations and saccades, in degrees/sec
        public double VelocityThreshold { get; set; } = 100;

        //Constructor
        public EyeMovementClassifier(Pipeline pipeline, string name = nameof(EyeMovementClassifier)) : base(pipeline) 
        { 
            lastInput = new EyeTrackingTemplate().content;
            this.name = name;
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        //Executes upon receiving a message
        protected override void Receive(Dictionary<ETData, IEyeTracking> input, Envelope envelope)
        {
            //Not treating data when it is the exact same
            if (!input.SequenceEqual(lastInput))
            {

                //Recovering gaze vectors from inputs
                var previousGaze = System.Numerics.Vector3.Normalize(((EyeTrackingVector3)lastInput[ETData.AverageGaze]).content);
                var currentGaze = System.Numerics.Vector3.Normalize(((EyeTrackingVector3)input[ETData.AverageGaze]).content).DeepClone();
                //Recovering the gazed object id key
                var gazedObjectKey = (((EyeTrackingInt)input[ETData.GazedObjectID]).content.DeepClone(), ((EyeTrackingString)input[ETData.GazedObjectName]).content.DeepClone());

                //Classifying the current message
                bool isCurrentMessageFix = ClassifyCurrentMessage(envelope.OriginatingTime, previousGaze, currentGaze);

                //If this is the first message received, create a new one
                if (lastEyeMovement is null)
                {
                    lastEyeMovement = new EyeMovement(envelope.OriginatingTime.DeepClone(), isCurrentMessageFix);
                    if (!isCurrentMessageFix) { lastEyeMovement.saccStartDirection = previousGaze; }
                }
                //If the current message is part of the same movement as the last message, add its values to the lists
                else if (isCurrentMessageFix == lastEyeMovement.isFixation)
                {
                    directionList.Add(currentGaze);
                    gazedObjectKeyList.Add(gazedObjectKey);
                    lastEyeMovement.messagesCount++;
                }
                //If this message is a transition between to movements
                else
                {
                    //Adding the ending timeStamp to the last movement
                    lastEyeMovement.lastTimeStamp = envelope.OriginatingTime.DeepClone();

                    //If the last movement was a fixation, add the average direction and the mostGazedObjectKey
                    if (lastEyeMovement.isFixation)
                    {
                        lastEyeMovement.fixDirection = CalculateAverageDirection();
                        lastEyeMovement.fixedObjectKey = GetMostGazedObjectKey();
                    }
                    //If it is a saccade, add the end position
                    else
                    {
                        lastEyeMovement.saccEndDirection = CalculateAverageDirection();
                    }

                    //Posting the the last movement
                    this.Out.Post(lastEyeMovement, envelope.OriginatingTime);

                    //Creating a new movement containing the current message
                    lastEyeMovement = new EyeMovement(envelope.OriginatingTime.DeepClone(), isCurrentMessageFix);
                    if (!isCurrentMessageFix)
                    {
                        lastEyeMovement.saccStartDirection = previousGaze;
                    }
                    //Reseting lists and adding the current message inputs
                    directionList.Clear();
                    directionList.Add(currentGaze);
                    gazedObjectKeyList.Clear();
                    gazedObjectKeyList.Add(gazedObjectKey);
                }
            }
            //Updating last input
            this.lastInput = input.DeepClone();
            this.lastTimeStamp = envelope.OriginatingTime.DeepClone();
        }

        //Returns true if the current message is classified as a fixation, false otherwise
        private bool ClassifyCurrentMessage(DateTime currentTimeStamp, System.Numerics.Vector3 previousGaze, System.Numerics.Vector3 currentGaze)
        {
            double angle = 0;
            //Calculating the angular velocity
            double dot = (double)System.Numerics.Vector3.Dot(previousGaze, currentGaze);

            //Forcing the calculation in the right interval because of rounding errors
            if (dot >= -1 && dot <= 1)
            {
                angle = Math.Acos(dot) * 180 / Math.PI;
            }
            //double velocity = angle / (currentTimeStamp - lastTimeStamp).TotalSeconds;
            double velocity = angle * 60;

            //Returning the result (true if fixation, false if saccade)
            return velocity < VelocityThreshold;
        }


        //Averages the directions of the vectors of all the last EyeMovement messages
        private System.Numerics.Vector3 CalculateAverageDirection()
        {
            System.Numerics.Vector3 average = System.Numerics.Vector3.Zero;
            foreach (var position in directionList)
            {
                average += position;
            }
            return average / directionList.Count;
        }


        //Returns the most gazed object of all the last EyeMovement messages
        private (int, string) GetMostGazedObjectKey()
        {
            int maxCount = 0;
            (int, string) mostGazedObjectKey = (0, "");

            //Searching for the most frequent element in the gazedObjectKeyList
            for (int i = 0; i < gazedObjectKeyList.Count(); i++)
            {
                int count = 0;
                for (int j = 0; j < gazedObjectKeyList.Count(); j++)
                {
                    if (gazedObjectKeyList[i] == gazedObjectKeyList[j]) { count++; }
                }
                if (count > maxCount)
                {
                    maxCount = count;
                    mostGazedObjectKey = gazedObjectKeyList[i];
                }
            }

            return mostGazedObjectKey;
        }
    }
}