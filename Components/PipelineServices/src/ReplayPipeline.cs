// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices
{
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Represents a replay pipeline that can replay data from a dataset.
    /// </summary>
    public class ReplayPipeline : DatasetPipeline, ISourceComponent
    {
        /// <summary>
        /// Defines the type of replay operation.
        /// </summary>
        public enum ReplayType
        {
            /// <summary>
            /// Replay at full speed without timing constraints.
            /// </summary>
            FullSpeed,

            /// <summary>
            /// Replay in real-time according to original timestamps.
            /// </summary>
            RealTime,

            /// <summary>
            /// Replay specific intervals at full speed.
            /// </summary>
            IntervalFullSpeed,

            /// <summary>
            /// Replay specific intervals in real-time.
            /// </summary>
            IntervalRealTime
        }

        /// <summary>
        /// Gets the replay pipeline configuration.
        /// </summary>
        public ReplayPipelineConfiguration Configuration { get; private set; }

        private DatasetLoader loader;
        private SortedSet<string> readOnlyStores;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayPipeline"/> class.
        /// </summary>
        /// <param name="configuration">The replay pipeline configuration.</param>
        /// <param name="name">The name of the pipeline.</param>
        /// <param name="log">Optional logging delegate.</param>
        public ReplayPipeline(ReplayPipelineConfiguration configuration, string name = nameof(ReplayPipeline), LogStatus? log = null)
            : base(configuration, name, log)
        {
            this.Initialize(configuration);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayPipeline"/> class with a parent pipeline.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">The replay pipeline configuration.</param>
        /// <param name="name">The name of the pipeline.</param>
        /// <param name="log">Optional logging delegate.</param>
        public ReplayPipeline(Pipeline parent, ReplayPipelineConfiguration configuration, string name = nameof(ReplayPipeline), LogStatus? log = null)
            : base(parent, configuration, name, log)
        {
            this.Initialize(configuration);
        }

        /// <summary>
        /// Starts the replay pipeline.
        /// </summary>
        /// <param name="notifyCompletionTime">Delegate to notify completion time.</param>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.RunPipelineAndSubpipelines();
            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <summary>
        /// Stops the replay pipeline.
        /// </summary>
        /// <param name="finalOriginatingTime">The final originating time.</param>
        /// <param name="notifyCompleted">Delegate to notify completion.</param>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.Stop();
            notifyCompleted();
        }

        /// <summary>
        /// Disposes the replay pipeline and releases resources.
        /// </summary>
        public new void Dispose()
        {
            this.readOnlyStores.Clear();
            base.Dispose();
        }

        /// <summary>
        /// Resets the replay pipeline to its initial state.
        /// </summary>
        /// <param name="pipeline">Optional pipeline to reset to.</param>
        public override void Reset(Pipeline? pipeline = null)
        {
            base.Reset(pipeline);
            this.readOnlyStores = new SortedSet<string>();
            this.loader = new DatasetLoader(this.Pipeline, this.Connectors, $"{this.name}-Loader");
        }

        /// <inheritdoc/>
        public override void AddNewProcessEvent(EventHandler<(string, Dictionary<string, Dictionary<string, ConnectorInfo>>)> handler)
        {
            base.AddNewProcessEvent(handler);
            this.loader.AddNewProcessEvent(handler);
        }

        /// <inheritdoc/>
        public override void AddRemoveProcessEvent(EventHandler<string> handler)
        {
            base.AddRemoveProcessEvent(handler);
            this.loader.AddRemoveProcessEvent(handler);
        }

        /// <summary>
        /// Loads the dataset and its connectors.
        /// </summary>
        /// <param name="manager">Optional connectors manager.</param>
        /// <param name="sessionName">Optional session name to load.</param>
        /// <returns>True if the dataset was loaded successfully; otherwise false.</returns>
        public bool LoadDatasetAndConnectors(ConnectorsManager? manager = null, string? sessionName = null)
        {
            if (this.Dataset != null && this.loader.Load(this.Dataset, sessionName))
            {
                this.Connectors = this.loader.Connectors;
                this.Log($"Dataset {this.Dataset?.Name} loaded!");
                foreach (var session in this.Connectors)
                {
                    this.Log($"\tSession {session.Key}:");
                    foreach (var connectorPair in session.Value)
                    {
                        this.Log($"\t\tStream {connectorPair.Key}");
                        this.readOnlyStores.Add(connectorPair.Value.StoreName);
                    }
                }

                if (manager != null)
                {
                    manager.Connectors = this.Connectors;
                }

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        protected override void PipelineRunAsync()
        {
            switch (this.Configuration.ReplayType)
            {
                case ReplayType.FullSpeed:
                    this.Pipeline.RunAsync(ReplayDescriptor.ReplayAll, this.Configuration.ProgressReport);
                    break;
                case ReplayType.RealTime:
                    this.Pipeline.RunAsync(ReplayDescriptor.ReplayAllRealTime, this.Configuration.ProgressReport);
                    break;
                case ReplayType.IntervalFullSpeed:
                    this.Pipeline.RunAsync(new ReplayDescriptor(this.Configuration.ReplayInterval, false), this.Configuration.ProgressReport);
                    break;
                case ReplayType.IntervalRealTime:
                    this.Pipeline.RunAsync(new ReplayDescriptor(this.Configuration.ReplayInterval, true), this.Configuration.ProgressReport);
                    break;
            }
        }

        /// <inheritdoc/>
        public override void CreateStore<T>(Pipeline pipeline, Session session, string streamName, string storeName, IProducer<T> source)
        {
            if (this.readOnlyStores.Contains(storeName))
            {
                throw new InvalidOperationException("Trying to write a Store that is readonly");
            }

            base.CreateStore(pipeline, session, streamName, storeName, source);
        }

        /// <inheritdoc/>
        public override (string, string) GetStoreName(string streamName, string processName, Session? session)
        {
            var names = base.GetStoreName(streamName, processName, session);
            if (this.readOnlyStores.Contains(names.Item2))
            {
                this.Log($"ReplayPipeline - GetStoreName : {names.Item2} already exist as Store Importer, switching name to {names.Item2}_{this.name}.");
                names.Item2 = $"{names.Item2}_{this.name}";
            }

            return names;
        }

        private void Initialize(ReplayPipelineConfiguration configuration)
        {
            this.Configuration = configuration ?? new ReplayPipelineConfiguration();
            this.loader = new DatasetLoader(this.Pipeline, this.Connectors, $"{this.name}-Loader");
            this.readOnlyStores = new SortedSet<string>();
            if (this.Dataset == null)
            {
                throw new ArgumentNullException(nameof(this.Dataset));
            }
            else if (this.Configuration.DatasetBackup)
            {
                var filename = this.Dataset.Filename;
                this.Dataset.SaveAs(this.Dataset.Filename.Insert(this.Dataset.Filename.Length - 4, "_backup"));
                this.Dataset.Filename = filename;
            }
        }
    }
}
