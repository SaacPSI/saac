// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PLUME
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Data;
    using SAAC.PipelineServices;

    /// <summary>
    /// Represents a dataset pipeline for loading and processing PLUME files.
    /// </summary>
    public class PlumeDatasetPipeline : DatasetPipeline, ISourceComponent
    {
        /// <summary>
        /// The PLUME file parser for processing PLUME data.
        /// </summary>
        protected PlumeFileParser? parser;

        /// <summary>
        /// The session for storing pipeline data.
        /// </summary>
        protected Session? session;

        /// <summary>
        /// Dictionary of emitters for posting messages to the pipeline.
        /// </summary>
        protected Dictionary<string, object> emitters;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlumeDatasetPipeline"/> class.
        /// </summary>
        /// <param name="datasetPipelineConfiguration">The dataset pipeline configuration.</param>
        /// <param name="name">The name of the pipeline.</param>
        /// <param name="assembliesToLoad">Optional list of assemblies to load for parsing.</param>
        /// <param name="log">Optional logging delegate.</param>
        public PlumeDatasetPipeline(DatasetPipelineConfiguration datasetPipelineConfiguration, string name, List<string>? assembliesToLoad = null, LogStatus? log = null)
            : base(datasetPipelineConfiguration, name, log)
        {
            this.parser = new PlumeFileParser(assembliesToLoad, log);
            this.emitters = new Dictionary<string, object>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlumeDatasetPipeline"/> class with an existing parser.
        /// </summary>
        /// <param name="parser">The PLUME file parser to use.</param>
        /// <param name="datasetPipelineConfiguration">The dataset pipeline configuration.</param>
        /// <param name="name">The name of the pipeline.</param>
        /// <param name="log">Optional logging delegate.</param>
        public PlumeDatasetPipeline(PlumeFileParser parser, DatasetPipelineConfiguration datasetPipelineConfiguration, string name, LogStatus? log = null)
            : base(datasetPipelineConfiguration, name, log)
        {
            this.parser = parser;
            this.emitters = new Dictionary<string, object>();
        }

        /// <summary>
        /// Loads and processes a PLUME file.
        /// </summary>
        /// <param name="file">The path to the PLUME file to load.</param>
        /// <param name="dataToStore">Dictionary specifying which data types to store.</param>
        public void LoadPlumeFile(string file, Dictionary<string, Type> dataToStore)
        {
            if (!System.IO.File.Exists(file))
            {
                this.Log($"File {file} does not exist.");
                return;
            }

            if (this.parser is null)
            {
                this.Log($"Parser is null.");
                return;
            }

            this.parser.ParseFile(file);
            this.SetDelegatesAndStores(dataToStore);
            this.Pipeline.ProposeReplayTime(new TimeInterval(this.parser.StartTime, DateTime.MaxValue));
            this.RunPipelineAndSubpipelines();
            this.parser.UnpackAll();
            this.Stop(1000);
            this.Log($"File {file} loaded.");
        }

        /// <summary>
        /// Starts the source component.
        /// </summary>
        /// <param name="notifyCompletionTime">Delegate to notify the completion time.</param>
        void ISourceComponent.Start(Action<DateTime> notifyCompletionTime)
        {
            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <summary>
        /// Stops the source component.
        /// </summary>
        /// <param name="finalOriginatingTime">The final originating time.</param>
        /// <param name="notifyCompleted">Delegate to notify completion.</param>
        void ISourceComponent.Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            notifyCompleted();
        }

        /// <summary>
        /// Runs the pipeline asynchronously.
        /// </summary>
        protected override void PipelineRunAsync()
        {
            if (this.parser is null)
            {
                this.Log($"Parser is null.");
                return;
            }

            this.Pipeline.RunAsync(this.parser.StartTime, false);
        }

        /// <summary>
        /// Sets up delegates and stores for the specified data types.
        /// </summary>
        /// <param name="dataToStore">Dictionary specifying which data types to store.</param>
        protected virtual void SetDelegatesAndStores(Dictionary<string, Type> dataToStore)
        {
            if (this.parser == null)
            {
                this.Log("Parser is null, cannot set delegates and stores.");
                return;
            }

            this.session = this.CreateOrGetSessionFromMode(this.parser.Name) ?? this.CreateOrGetSession("PlumeDatasetPipeline");

            foreach (var data in dataToStore)
            {
                if (this.emitters.ContainsKey(data.Key))
                {
                    continue;
                }

                var emitter = typeof(Pipeline).GetMethod("CreateEmitter").MakeGenericMethod(data.Value).Invoke(this.Pipeline, [this, data.Key, null]);
                typeof(PlumeDatasetPipeline).GetMethod("CreateStore").MakeGenericMethod(data.Value).Invoke(this, [this.Pipeline, this.session, data.Key, this.parser.Name, emitter]);
                this.emitters.Add(data.Key, emitter);
            }

            this.parser.OnTransformUpdate += this.TransformMessage;
        }

        /// <summary>
        /// Handles transform update messages from the PLUME parser.
        /// </summary>
        /// <param name="name">The name of the transform.</param>
        /// <param name="msg">The coordinate system message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        private void TransformMessage(string name, CoordinateSystem msg, ulong? timestamp)
        {
            if (this.emitters.ContainsKey(name) && this.parser != null)
            {
                var time = this.parser.StartTime.AddTicks(timestamp.HasValue ? (long)timestamp.Value : 0);

                // var time = DateTime.Now.AddTicks(timestamp.HasValue ? (long)timestamp.Value : 0);
                ((Emitter<CoordinateSystem>)this.emitters[name]).Post(msg, time);
                this.Log($"->Posted message to {name} at {time}");
            }
        }
    }
}
