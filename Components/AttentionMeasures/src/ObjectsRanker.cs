// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using Microsoft.Psi;

namespace SAAC.AttentionMeasures
{
    /// <summary>
    /// Psi component that ranks objects based on gaze attention using a scoring system.
    /// </summary>
    public class ObjectsRanker
    {
        /// <summary>
        /// Gets the receiver for eye tracking data.
        /// </summary>
        public Receiver<Dictionary<IEyeTracking.ETData, IEyeTracking>> In { get; private set; }

        /// <summary>
        /// Gets the receiver for the timer.
        /// </summary>
        public Receiver<TimeSpan> TimerIn { get; private set; }

        /// <summary>
        /// Gets the emitter for the ranked objects output.
        /// </summary>
        public Emitter<Dictionary<(int, string), double>> Out { get; private set; }

        /// <summary>
        /// Gets the dictionary storing the ranking of objects.
        /// </summary>
        public Dictionary<(int, string), double> ObjectsRanking { get; private set; }

        private (int, string) lastGazedObject;
        private string name;

        /// <summary>
        /// Gets the configuration for this ranker.
        /// </summary>
        public ObjectsRankerConfiguration Configuration { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectsRanker"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="configuration">Optional configuration for the ranker.</param>
        /// <param name="name">The name of the component.</param>
        public ObjectsRanker(Pipeline pipeline, ObjectsRankerConfiguration? configuration = null, string name = nameof(ObjectsRanker))
        {
            this.name = name;
            this.Configuration = configuration ?? new ObjectsRankerConfiguration();
            this.ObjectsRanking = new Dictionary<(int, string), double>();
            this.In = pipeline.CreateReceiver<Dictionary<IEyeTracking.ETData, IEyeTracking>>(this, this.Receive, $"{name}-In");
            this.TimerIn = pipeline.CreateReceiver<TimeSpan>(this, this.ReceiveTimer, $"{name}-TimerIn");
            this.Out = pipeline.CreateEmitter<Dictionary<(int, string), double>>(this, $"{name}-Out");
            this.ObjectsRanking.Add((0, "NothingGazed"), 0);
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Receives and processes eye tracking data.
        /// </summary>
        /// <param name="input">The eye tracking data.</param>
        /// <param name="envelope">The message envelope.</param>
        protected void Receive(Dictionary<IEyeTracking.ETData, IEyeTracking> input, Envelope envelope)
        {
            int gazedObjectID = 0;
            string gazedObjectName = "NothingGazed";

            // If an object is currently gazed, update the values of the ID and name
            if (((EyeTrackingBool)input[IEyeTracking.ETData.IsGazingAtSomething]).Content)
            {
                gazedObjectID = ((EyeTrackingInt)input[IEyeTracking.ETData.GazedObjectID]).Content.DeepClone();
                gazedObjectName = ((EyeTrackingString)input[IEyeTracking.ETData.GazedObjectName]).Content.DeepClone();

                // If the current gazed object has not been gazed yet, add it to the ranking
                if (!this.ObjectsRanking.ContainsKey((gazedObjectID, gazedObjectName)))
                {
                    this.ObjectsRanking.Add((gazedObjectID, gazedObjectName), 0);
                }
            }

            this.UpdateRanking((gazedObjectID, gazedObjectName));
        }

        /// <summary>
        /// Receives timer messages and posts the current ranking.
        /// </summary>
        /// <param name="input">The timer input.</param>
        /// <param name="envelope">The message envelope.</param>
        protected virtual void ReceiveTimer(TimeSpan input, Envelope envelope)
        {
            this.Out.Post(this.ObjectsRanking, envelope.OriginatingTime);
        }

        /// <summary>
        /// Updates the ranking scores for all objects based on current gaze.
        /// </summary>
        /// <param name="currentGazedIDName">The currently gazed object ID and name.</param>
        private void UpdateRanking((int, string) currentGazedIDName)
        {
            List<(int, string)> keys = new List<(int, string)>(this.ObjectsRanking.Keys);
            foreach ((int, string) key in keys)
            {
                if (key == currentGazedIDName)
                {
                    this.ObjectsRanking[currentGazedIDName] = this.IncreaseGazedFunction(this.ObjectsRanking[currentGazedIDName]);
                }
                else
                {
                    this.ObjectsRanking[key] = this.DecreaseGazedFunction(this.ObjectsRanking[key]);
                }
            }

            // Uncomment if you want to sort the ranking
            // objectsRanking.OrderBy(x => x.Value);
        }

        private double IncreaseGazedFunction(double x)
        {
            return x + this.Configuration.B;
        }

        private double DecreaseGazedFunction(double x)
        {
            return Math.Max(x + this.Configuration.D, 0);
        }
    }
}
