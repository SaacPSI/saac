// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Psi component classifying the raw EyeTracking data into eye movements (fixations or saccades).
    /// </summary>
    public class EyeMovementClassifier : ConsumerProducer<Dictionary<IEyeTracking.ETData, IEyeTracking>, EyeMovement>
    {
        /// <summary>
        /// Last received message and timestamp.
        /// </summary>
        private Dictionary<IEyeTracking.ETData, IEyeTracking> lastInput;

        private DateTime lastTimeStamp;
        private string name;

        /// <summary>
        /// Last EyeMovement recorded.
        /// </summary>
        private EyeMovement lastEyeMovement;

        /// <summary>
        /// List to store the gaze direction of messages in the last movement.
        /// </summary>
        private List<System.Numerics.Vector3> directionList = new List<System.Numerics.Vector3>();

        /// <summary>
        /// List to store the gazed object identification keys (UnityID, name) of messages in the last movement.
        /// </summary>
        private List<(int, string)> gazedObjectKeyList = new List<(int, string)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="EyeMovementClassifier"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="name">The name of the component.</param>
        public EyeMovementClassifier(Pipeline pipeline, string name = nameof(EyeMovementClassifier))
            : base(pipeline)
        {
            this.lastInput = new EyeTrackingTemplate().Content;
            this.name = name;
        }

        /// <summary>
        /// Gets or sets the threshold determining the classification of fixations and saccades, in degrees/sec.
        /// </summary>
        public double VelocityThreshold { get; set; } = 100;

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Executes upon receiving a message.
        /// </summary>
        /// <param name="input">The input data.</param>
        /// <param name="envelope">The message envelope.</param>
        protected override void Receive(Dictionary<IEyeTracking.ETData, IEyeTracking> input, Envelope envelope)
        {
            // Not treating data when it is the exact same
            if (!input.SequenceEqual(this.lastInput))
            {
                // Recovering gaze vectors from inputs
                var previousGaze = System.Numerics.Vector3.Normalize(((EyeTrackingVector3)this.lastInput[IEyeTracking.ETData.AverageGaze]).Content);
                var currentGaze = System.Numerics.Vector3.Normalize(((EyeTrackingVector3)input[IEyeTracking.ETData.AverageGaze]).Content).DeepClone();

                // Recovering the gazed object id key
                var gazedObjectKey = (((EyeTrackingInt)input[IEyeTracking.ETData.GazedObjectID]).Content.DeepClone(), ((EyeTrackingString)input[IEyeTracking.ETData.GazedObjectName]).Content.DeepClone());

                // Classifying the current message
                bool isCurrentMessageFix = this.ClassifyCurrentMessage(envelope.OriginatingTime, previousGaze, currentGaze);

                // If this is the first message received, create a new one
                if (this.lastEyeMovement is null)
                {
                    this.lastEyeMovement = new EyeMovement(envelope.OriginatingTime.DeepClone(), isCurrentMessageFix);
                    if (!isCurrentMessageFix)
                    {
                        this.lastEyeMovement.SaccStartDirection = previousGaze;
                    }
                }

                // If the current message is part of the same movement as the last message, add its values to the lists
                else if (isCurrentMessageFix == this.lastEyeMovement.IsFixation)
                {
                    this.directionList.Add(currentGaze);
                    this.gazedObjectKeyList.Add(gazedObjectKey);
                    this.lastEyeMovement.MessagesCount++;
                }

                // If this message is a transition between to movements
                else
                {
                    // Adding the ending timeStamp to the last movement
                    this.lastEyeMovement.LastTimeStamp = envelope.OriginatingTime.DeepClone();

                    // If the last movement was a fixation, add the average direction and the mostGazedObjectKey
                    if (this.lastEyeMovement.IsFixation)
                    {
                        this.lastEyeMovement.FixDirection = this.CalculateAverageDirection();
                        this.lastEyeMovement.FixedObjectKey = this.GetMostGazedObjectKey();
                    }

                    // If it is a saccade, add the end position
                    else
                    {
                        this.lastEyeMovement.SaccEndDirection = this.CalculateAverageDirection();
                    }

                    // Posting the the last movement
                    this.Out.Post(this.lastEyeMovement, envelope.OriginatingTime);

                    // Creating a new movement containing the current message
                    this.lastEyeMovement = new EyeMovement(envelope.OriginatingTime.DeepClone(), isCurrentMessageFix);
                    if (!isCurrentMessageFix)
                    {
                        this.lastEyeMovement.SaccStartDirection = previousGaze;
                    }

                    // Reseting lists and adding the current message inputs
                    this.directionList.Clear();
                    this.directionList.Add(currentGaze);
                    this.gazedObjectKeyList.Clear();
                    this.gazedObjectKeyList.Add(gazedObjectKey);
                }
            }

            // Updating last input
            this.lastInput = input.DeepClone();
            this.lastTimeStamp = envelope.OriginatingTime.DeepClone();
        }

        // Returns true if the current message is classified as a fixation, false otherwise
        private bool ClassifyCurrentMessage(DateTime currentTimeStamp, System.Numerics.Vector3 previousGaze, System.Numerics.Vector3 currentGaze)
        {
            double angle = 0;

            // Calculating the angular velocity
            double dot = (double)System.Numerics.Vector3.Dot(previousGaze, currentGaze);

            // Forcing the calculation in the right interval because of rounding errors
            if (dot >= -1 && dot <= 1)
            {
                angle = Math.Acos(dot) * 180 / Math.PI;
            }

            // double velocity = angle / (currentTimeStamp - lastTimeStamp).TotalSeconds;
            double velocity = angle * 60;

            // Returning the result (true if fixation, false if saccade)
            return velocity < this.VelocityThreshold;
        }

        // Averages the directions of the vectors of all the last EyeMovement messages
        private System.Numerics.Vector3 CalculateAverageDirection()
        {
            System.Numerics.Vector3 average = System.Numerics.Vector3.Zero;
            foreach (var position in this.directionList)
            {
                average += position;
            }

            return average / this.directionList.Count;
        }

        // Returns the most gazed object of all the last EyeMovement messages
        private (int, string) GetMostGazedObjectKey()
        {
            int maxCount = 0;
            (int, string) mostGazedObjectKey = (0, string.Empty);

            // Searching for the most frequent element in the gazedObjectKeyList
            for (int i = 0; i < this.gazedObjectKeyList.Count(); i++)
            {
                int count = 0;
                for (int j = 0; j < this.gazedObjectKeyList.Count(); j++)
                {
                    if (this.gazedObjectKeyList[i] == this.gazedObjectKeyList[j])
                    {
                        count++;
                    }
                }

                if (count > maxCount)
                {
                    maxCount = count;
                    mostGazedObjectKey = this.gazedObjectKeyList[i];
                }
            }

            return mostGazedObjectKey;
        }
    }
}
