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
        /// Logging delegate for status messages.
        /// </summary>
        public LogStatus Log;

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

        protected bool isPipelineRunning = false;

        public virtual DatasetPipelineConfiguration Configuration { get; set; }

        protected Dictionary<string, Subpipeline> subpipelines;

        protected bool owningPipeline;

        public Session? CurrentSession { get; private set; } = null;

        public DatasetPipeline(Pipeline parent, DatasetPipelineConfiguration? configuration = null, string name = nameof(DatasetPipeline), LogStatus? log = null, Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null)
            : base(string.Empty, connectors, name)
        {
            this.owningPipeline = false;
            this.Pipeline = parent;
            this.Pipeline.PipelineRun += this.TriggerRun;
            this.Initialize(configuration, log);
        }

        public DatasetPipeline(DatasetPipelineConfiguration? configuration = null, string name = nameof(DatasetPipeline), LogStatus? log = null, Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null)
            : base(string.Empty, connectors, name)
        {
            this.owningPipeline = true;
            this.Initialize(configuration, log);
            this.Pipeline = Pipeline.Create(name, enableDiagnostics: this.Configuration?.Diagnostics != DiagnosticsMode.Off);
        }

        public bool RunPipelineAndSubpipelines()
        {
            if (!this.owningPipeline)
            {
                this.Log($"{this.name} does not own Pipeline, it cannot be started from here.");
                return this.isPipelineRunning;
            }

            if (this.isPipelineRunning)
            {
                return true;
            }

            try
            {
                this.isPipelineRunning = true;
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

            return this.isPipelineRunning;
        }

        public virtual void Stop(int maxWaitingTime = 100)
        {
            if (!this.owningPipeline)
            {
                this.Log($"{this.name} does not own Pipeline, it cannot be stopped from here.");
            }
            else if (this.isPipelineRunning)
            {
                foreach (var subpipeline in this.subpipelines)
                {
                    subpipeline.Value.Stop(this.Pipeline.GetCurrentTime(), () => { });
                }

                this.Pipeline.WaitAll(maxWaitingTime);
                this.Log("Pipeline Stopped.");
            }

            this.Dataset?.Save();
            this.isPipelineRunning = false;
        }

        public virtual void Reset(Pipeline? pipeline = null)
        {
            this.Dispose();
            this.subpipelines.Clear();
            this.Connectors.Clear();
            this.owningPipeline = pipeline == null;
            this.isPipelineRunning = false;
            this.Pipeline = pipeline ?? Pipeline.Create(this.name, enableDiagnostics: this.Configuration?.Diagnostics != DiagnosticsMode.Off);
        }

        public new void Dispose()
        {
            base.Dispose();
            this.subpipelines.Clear();
            if (!this.owningPipeline)
            {
                this.Log($"{this.name} does not own Pipeline, it cannot be dispose from here.");
                return;
            }

            this.Pipeline?.Dispose();
        }

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

        public Subpipeline GetOrCreateSubpipeline(string name = "SaaCSubPipeline")
        {
            if (!this.subpipelines.ContainsKey(name))
            {
                this.subpipelines.Add(name, Subpipeline.Create(this.Pipeline, name));
            }

            return this.subpipelines[name];
        }

        protected virtual void TriggerRun(object sender, PipelineRunEventArgs e)
        {
            this.isPipelineRunning = true;

            // foreach (Subpipeline sub in subpipelines)
            // {
            //    if (sub.StartTime == DateTime.MinValue)
            //    {
            //        sub.Start((d) => { });
            //        Log($"SubPipeline {sub.Name} started.");
            //    }
            // }
        }

        protected virtual void PipelineRunAsync()
        {
            this.Pipeline.RunAsync();
        }

        private void Initialize(DatasetPipelineConfiguration? configuration = null, LogStatus? log = null)
        {
            this.Log = log ?? ((log) => { Console.WriteLine(log); });
            this.Configuration = configuration ?? new DatasetPipelineConfiguration();
            this.subpipelines = new Dictionary<string, Subpipeline>();
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
