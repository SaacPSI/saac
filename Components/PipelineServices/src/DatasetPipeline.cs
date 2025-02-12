using Microsoft.Psi;
using Microsoft.Psi.Data;
using System.IO;

namespace SAAC.PipelineServices
{
    public class DatasetPipeline : ConnectorsAndStoresCreator
    {
        public Dataset? Dataset { get; private set; }
        public Pipeline Pipeline { get; private set; }

        public LogStatus Log;

        public enum SessionNamingMode { Unique, Increment, Overwrite };
        public enum StoreMode { Independant, Process, Dictionnary };
        public enum DiagnosticsMode { Off, Store, Export };

        protected bool isPipelineRunning;

        public virtual DatasetPipelineConfiguration Configuration { get; set; }

        protected List<Pipeline> subpipelines;

        protected bool OwningPipeline;

        public DatasetPipeline(Pipeline parent, DatasetPipelineConfiguration? configuration = null, string name = nameof(DatasetPipeline), LogStatus? log = null, Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null)
            : base("", connectors, name)
        {
            OwningPipeline = false;
            Pipeline = parent;

            Initialize(configuration, log);
        }

        public DatasetPipeline(DatasetPipelineConfiguration? configuration = null, string name = nameof(DatasetPipeline), LogStatus? log = null, Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null)
            : base("", connectors, name)
        {
            OwningPipeline = true;
            Pipeline = Pipeline.Create(name, enableDiagnostics: configuration?.Diagnostics != DiagnosticsMode.Off);
            Initialize(configuration, log);
        }

        public bool RunPipeline()
        {
            if (!OwningPipeline)
            {
                Log($"{name} does not own Pipeline, it cannot be started from here.");
                return isPipelineRunning;
            }
            
            if (isPipelineRunning)
                return true;
            try
            {
                RunAsync();
                isPipelineRunning = true;
                Log("Pipeline running.");
            }
            catch (Exception ex)
            {
                Log($"{ex.Message}\n{ex.InnerException}");
            }
            return isPipelineRunning;
        }

        protected virtual void RunAsync()
        {
            Pipeline.RunAsync();
        }

        public virtual void Stop()
        {
            if (!OwningPipeline)
            {
                Log($"{name} does not own Pipeline, it cannot be stroped from here.");
                return;
            }

            if (isPipelineRunning)
            {
                Pipeline.Dispose();
                foreach(Pipeline sub in subpipelines)
                    sub.Dispose();
                subpipelines.Clear();
            }
            Dataset?.Save();
            isPipelineRunning = false;
            Log("Pipeline Stopped.");
        }

        public Session? GetSession(string sessionName)
        {
            if (Dataset != null)
            {
                if (sessionName.EndsWith("."))
                {
                    Session? sessionTmp = null;
                    foreach (var session in Dataset.Sessions)
                    {
                        if (session != null && session.Name.Contains(sessionName))
                        {
                            if (sessionTmp != null)
                            {
                                if (session.Name.Replace(sessionName, "").CompareTo(sessionTmp.Name.Replace(sessionName, "")) < 0)
                                    continue;
                            }
                            sessionTmp = session;
                        }
                    }
                    return sessionTmp;
                }
                else
                {
                    foreach (var session in Dataset.Sessions)
                    {
                        if (session != null && session.Name == sessionName)
                            return session;
                    }
                }
            }
            return null;
        }

        public Session? CreateOrGetSession(string sessionName)
        {
            if (Dataset == null)
                return null;
            foreach (var session in Dataset.Sessions)
                if (session != null && session.Name == sessionName)
                    return session;
            return Dataset.AddEmptySession(sessionName);
        }

        public Session? CreateIterativeSession(string sessionName)
        {
            if (Dataset == null)
                return null;
            int iterator = 0;
            foreach (var session in Dataset.Sessions)
                if (session != null && session.Name.Contains(sessionName))
                    iterator++;
            return Dataset.AddEmptySession($"{sessionName}.{iterator:D3}");
        }

        public Session? CreateOrGetSessionFromMode(string sessionName)
        {
            switch (Configuration.SessionMode)
            {
                case SessionNamingMode.Unique:
                    return CreateOrGetSession(Configuration.SessionName);
                case SessionNamingMode.Overwrite:
                    return CreateOrGetSession(Configuration.SessionName + sessionName);
                case SessionNamingMode.Increment:
                default:
                    return CreateIterativeSession(Configuration.SessionName + sessionName);
            }
        }

        public virtual (string, string) GetStoreName(string streamName, string processName, Session? session)
        {
            switch (Configuration.StoreMode)
            {
                case StoreMode.Process:
                    return (streamName, processName);
                case StoreMode.Dictionnary:
                    if (Configuration.StreamToStore.ContainsKey(streamName) && session != null)
                    {
                        string storeName = Configuration.StreamToStore[streamName];
                        if (storeName.Contains("%s"))
                            storeName = storeName.Replace("%s", session.Name);
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

        public Pipeline CreateSubpipeline(string name = "SaaCSubpipeline")
        {
            subpipelines.Add(Microsoft.Psi.Pipeline.CreateSynchedPipeline(Pipeline, name));
            return subpipelines.Last();
        }

        private void Initialize(DatasetPipelineConfiguration? configuration = null, LogStatus? log = null)
        {
            this.Log = log ?? ((log) => { Console.WriteLine(log); });
            Configuration = configuration ?? new DatasetPipelineConfiguration();
            subpipelines = new List<Pipeline>();
            if (this.Configuration.DatasetName.Length > 4)
            {
                if (File.Exists(this.Configuration.DatasetPath + this.Configuration.DatasetName))
                    Dataset = Dataset.Load(this.Configuration.DatasetPath + this.Configuration.DatasetName, true);
                else
                {
                    Dataset = new Dataset(this.Configuration.DatasetName, this.Configuration.DatasetPath + this.Configuration.DatasetName, true);
                    Dataset.Save(); // throw exception here if the path is not correct
                }
                StorePath = this.Configuration.DatasetPath;
            }
            else
                Dataset = null;
            StorePath = this.Configuration.DatasetPath;
        }
    }
}
