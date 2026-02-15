// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.PipelineServices
{
    using System.IO;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Represents a dataset pipeline that manages Microsoft Psi pipelines, datasets, sessions, and stores.
    /// </summary>
    public class DatasetPipeline : ConnectorsAndStoresCreator
    {
        /// <summary>
        /// Gets the dataset associated with this pipeline.
        /// </summary>
        public Dataset? Dataset { get; private set; }

        /// <summary>
        /// Gets the Microsoft Psi pipeline.
        /// </summary>
        public Pipeline Pipeline { get; private set; }

        /// <summary>
        /// Gets or sets the logging delegate for status messages.
        /// </summary>
        public LogStatus Log { get; set; }

        /// <summary>
        /// Defines the naming mode for sessions.
        /// </summary>
        public enum SessionNamingMode
        {
            /// <summary>
            /// Create a unique session name.
            /// </summary>
            Unique,

            /// <summary>
            /// Increment the session name with a counter.
            /// </summary>
            Increment,

            /// <summary>
            /// Overwrite existing session with the same name.
            /// </summary>
            Overwrite
        }

        /// <summary>
        /// Defines the storage mode for pipeline data.
        /// </summary>
        public enum StoreMode
        {
            /// <summary>
            /// Each store is independent.
            /// </summary>
            Independant,

            /// <summary>
            /// Stores are organized by process.
            /// </summary>
            Process,

            /// <summary>
            /// Stores are organized by dictionary mapping.
            /// </summary>
            Dictionnary
        }

        /// <summary>
        /// Defines the diagnostics mode for the pipeline.
        /// </summary>
        public enum DiagnosticsMode
        {
            /// <summary>
            /// Diagnostics are disabled.
            /// </summary>
            Off,

            /// <summary>
            /// Diagnostics are stored.
            /// </summary>
            Store,

            /// <summary>
            /// Diagnostics are exported.
            /// </summary>
            Export
        }

        /// <summary>
        /// Gets or sets a value indicating whether the pipeline is running.
        /// </summary>
        protected bool IsPipelineRunning { get; set; } = false;

        /// <summary>
        /// Gets or sets the configuration for the dataset pipeline.
        /// </summary>
        public virtual DatasetPipelineConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets the dictionary of subpipelines.
        /// </summary>
        protected Dictionary<string, Subpipeline> Subpipelines { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance owns the pipeline.
        /// </summary>
        protected bool OwningPipeline { get; private set; }

        /// <summary>
        /// Gets or sets the current session.
        /// </summary>
        public Session? CurrentSession { get; private set; } = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatasetPipeline"/> class with a parent pipeline.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">Optional configuration.</param>
        /// <param name="name">The pipeline name.</param>
        /// <param name="log">Optional logging delegate.</param>
        /// <param name="connectors">Optional connectors dictionary.</param>
        public DatasetPipeline(Pipeline parent, DatasetPipelineConfiguration? configuration = null, string name = nameof(DatasetPipeline), LogStatus? log = null, Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null)
            : base(string.Empty, connectors, name)
        {
            this.OwningPipeline = false;
            this.Pipeline = parent;
            this.Pipeline.PipelineRun += this.TriggerRun;
            this.Initialize(configuration, log);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatasetPipeline"/> class.
        /// </summary>
        /// <param name="configuration">Optional configuration.</param>
        /// <param name="name">The pipeline name.</param>
        /// <param name="log">Optional logging delegate.</param>
        /// <param name="connectors">Optional connectors dictionary.</param>
        public DatasetPipeline(DatasetPipelineConfiguration? configuration = null, string name = nameof(DatasetPipeline), LogStatus? log = null, Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null)
            : base(string.Empty, connectors, name)
        {
            this.OwningPipeline = true;
            this.Initialize(configuration, log);
            this.Pipeline = Pipeline.Create(name, enableDiagnostics: this.Configuration?.Diagnostics != DiagnosticsMode.Off);
        }

        /// <summary>
        /// Runs the pipeline and all subpipelines.
        /// </summary>
        /// <returns>True if the pipeline is running; otherwise false.</returns>
        public bool RunPipelineAndSubpipelines()
        {
            if (!this.OwningPipeline)
            {
                this.Log($"{this.name} does not own Pipeline, it cannot be started from here.");
                return this.IsPipelineRunning;
            }

            if (this.IsPipelineRunning)
            {
                return true;
            }

            try
            {
                this.IsPipelineRunning = true;
                this.PipelineRunAsync();
                this.Log("Pipeline running.");

                // foreach (Subpipeline sub in subpipelines)
                // {
                //    if (sub.StartTime == DateTime.MinValue)
                //    {
                //        sub.Start((d) => { });
                //        Log($"SubPipeline {sub.Name} started.");
                //    }
                // }
            }
            catch (Exception ex)
            {
                this.Log($"{ex.Message}\n{ex.InnerException}");
            }

            return this.IsPipelineRunning;
        }

        /// <summary>
        /// Stops the pipeline and saves the dataset.
        /// </summary>
        /// <param name="maxWaitingTime">Maximum waiting time in milliseconds.</param>
        public virtual void Stop(int maxWaitingTime = 100)
        {
            if (!this.OwningPipeline)
            {
                this.Log($"{this.name} does not own Pipeline, it cannot be stopped from here.");
            }
            else if (this.IsPipelineRunning)
            {
                foreach (var subpipeline in this.Subpipelines)
                {
                    subpipeline.Value.Stop(this.Pipeline.GetCurrentTime(), () => { });
                }

                this.Pipeline.WaitAll(maxWaitingTime);
                this.Log("Pipeline Stopped.");
            }

            this.Dataset?.Save();
            this.IsPipelineRunning = false;
        }

        /// <summary>
        /// Resets the pipeline to its initial state.
        /// </summary>
        /// <param name="pipeline">Optional pipeline to use instead of creating a new one.</param>
        public virtual void Reset(Pipeline? pipeline = null)
        {
            this.Dispose();
            this.Subpipelines.Clear();
            this.Connectors.Clear();
            this.OwningPipeline = pipeline == null;
            this.IsPipelineRunning = false;
            this.Pipeline = pipeline ?? Pipeline.Create(this.name, enableDiagnostics: this.Configuration?.Diagnostics != DiagnosticsMode.Off);
        }

        /// <inheritdoc/>
        public new void Dispose()
        {
            base.Dispose();
            this.Subpipelines.Clear();
            if (!this.OwningPipeline)
            {
                this.Log($"{this.name} does not own Pipeline, it cannot be dispose from here.");
                return;
            }

            this.Pipeline?.Dispose();
        }

        /// <summary>
        /// Gets a session by name from the dataset.
        /// </summary>
        /// <param name="sessionName">The session name to retrieve.</param>
        /// <returns>The session if found; otherwise null.</returns>
        public Session? GetSession(string sessionName)
        {
            if (this.Dataset != null)
            {
                if (sessionName.EndsWith("."))
                {
                    Session? sessionTmp = null;
                    foreach (var session in this.Dataset.Sessions)
                    {
                        if (session != null && session.Name.Contains(sessionName))
                        {
                            if (sessionTmp != null)
                            {
                                if (session.Name.Replace(sessionName, string.Empty).CompareTo(sessionTmp.Name.Replace(sessionName, string.Empty)) < 0)
                                {
                                    continue;
                                }
                            }

                            this.CurrentSession = sessionTmp = session;
                        }
                    }

                    return sessionTmp;
                }
                else
                {
                    foreach (var session in this.Dataset.Sessions)
                    {
                        if (session != null && session.Name == sessionName)
                        {
                            return this.CurrentSession = session;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a new session or gets an existing one with the specified name.
        /// </summary>
        /// <param name="sessionName">The session name.</param>
        /// <returns>The session if created or found; otherwise null.</returns>
        public Session? CreateOrGetSession(string sessionName)
        {
            if (this.Dataset == null)
            {
                return null;
            }

            foreach (var session in this.Dataset.Sessions)
            {
                if (session != null && session.Name == sessionName)
                {
                    return session;
                }
            }

            return this.CurrentSession = this.Dataset.AddEmptySession(sessionName);
        }

        /// <summary>
        /// Creates a new iterative session with an incremented name.
        /// </summary>
        /// <param name="sessionName">The base session name.</param>
        /// <returns>The created session; otherwise null.</returns>
        public Session? CreateIterativeSession(string sessionName)
        {
            if (this.Dataset is null)
            {
                return null;
            }

            int iterator = 0;
            if (this.CurrentSession?.Name.Contains(sessionName) is true)
            {
                return this.CurrentSession;
            }

            foreach (var session in this.Dataset.Sessions)
            {
                if (session != null && session.Name.Contains(sessionName))
                {
                    iterator++;
                }
            }

            return this.CurrentSession = this.Dataset.AddEmptySession($"{sessionName}.{iterator:D3}");
        }

        /// <summary>
        /// Creates or gets a session based on the configured session naming mode.
        /// </summary>
        /// <param name="sessionName">Optional session name suffix.</param>
        /// <returns>The session if created or found; otherwise null.</returns>
        public Session? CreateOrGetSessionFromMode(string sessionName = "")
        {
            switch (this.Configuration.SessionMode)
            {
                case SessionNamingMode.Unique:
                    return this.CurrentSession is null ? this.CreateIterativeSession(this.Configuration.SessionName) : this.CurrentSession;
                case SessionNamingMode.Overwrite:
                    return this.CreateOrGetSession(this.Configuration.SessionName + sessionName);
                case SessionNamingMode.Increment:
                default:
                    return this.CreateIterativeSession(this.Configuration.SessionName + sessionName);
            }
        }

        /// <summary>
        /// Gets the store name for a stream based on the configured store mode.
        /// </summary>
        /// <param name="streamName">The stream name.</param>
        /// <param name="processName">The process name.</param>
        /// <param name="session">The session.</param>
        /// <returns>A tuple containing the stream name and store name.</returns>
        public virtual (string, string) GetStoreName(string streamName, string processName, Session? session)
        {
            switch (this.Configuration.StoreMode)
            {
                case StoreMode.Process:
                    return (streamName, processName);
                case StoreMode.Dictionnary:
                    if (this.Configuration.StreamToStore.ContainsKey(streamName) && session != null)
                    {
                        string storeName = this.Configuration.StreamToStore[streamName];
                        if (storeName.Contains("%s"))
                        {
                            storeName = storeName.Replace("%s", session.Name);
                        }

                        if (storeName.Contains("%p"))
                        {
                            storeName = storeName.Replace("%p", processName);
                            return (streamName, storeName);
                        }

                        return ($"{processName}-{streamName}", storeName);
                    }

                    goto default;
                default:
                case StoreMode.Independant:
                    return (streamName, $"{processName}-{streamName}");
            }
        }

        /// <summary>
        /// Gets an existing subpipeline or creates a new one with the specified name.
        /// </summary>
        /// <param name="name">The subpipeline name.</param>
        /// <returns>The subpipeline.</returns>
        public Subpipeline GetOrCreateSubpipeline(string name = "SaaCSubPipeline")
        {
            if (!this.Subpipelines.ContainsKey(name))
            {
                this.Subpipelines.Add(name, Subpipeline.Create(this.Pipeline, name));
            }

            return this.Subpipelines[name];
        }

        /// <summary>
        /// Triggered when the pipeline starts running.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The pipeline run event arguments.</param>
        protected virtual void TriggerRun(object sender, PipelineRunEventArgs e)
        {
            this.IsPipelineRunning = true;

            // foreach (Subpipeline sub in subpipelines)
            // {
            //    if (sub.StartTime == DateTime.MinValue)
            //    {
            //        sub.Start((d) => { });
            //        Log($"SubPipeline {sub.Name} started.");
            //    }
            // }
        }

        /// <summary>
        /// Runs the pipeline asynchronously.
        /// </summary>
        protected virtual void PipelineRunAsync()
        {
            this.Pipeline.RunAsync();
        }

        /// <summary>
        /// Initializes the dataset pipeline with configuration and logging.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="log">The logging delegate.</param>
        private void Initialize(DatasetPipelineConfiguration? configuration = null, LogStatus? log = null)
        {
            this.Log = log ?? ((logMessage) => { Console.WriteLine(logMessage); });
            this.Configuration = configuration ?? new DatasetPipelineConfiguration();
            this.Subpipelines = new Dictionary<string, Subpipeline>();
            if (this.Configuration.DatasetName.Length > 4)
            {
                string fullPath = Path.Combine(this.Configuration.DatasetPath, this.Configuration.DatasetName);
                if (File.Exists(fullPath))
                {
                    this.Dataset = Dataset.Load(fullPath, true);
                }
                else
                {
                    this.Dataset = new Dataset(Path.GetFileNameWithoutExtension(this.Configuration.DatasetName), fullPath, true);
                    this.Dataset.Save(); // throw exception here if the path is not correct
                }

                this.StorePath = this.Configuration.DatasetPath;
            }
            else
            {
                this.Dataset = null;
            }

            this.StorePath = this.Configuration.DatasetPath;
        }
    }
}
