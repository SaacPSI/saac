﻿using Microsoft.Psi;
using Microsoft.Psi.Data;
using SAAC;
using SAAC.RendezVousPipelineServices;
using System.IO;

namespace RendezVousPipelineServices
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

        public DatasetPipeline(DatasetPipelineConfiguration? configuration = null, string name = nameof(RendezVousPipeline), LogStatus? log = null)
            : base("", null, name)
        {
            this.Log = log ?? ((log) => { Console.WriteLine(log); });
            Pipeline = Pipeline.Create(enableDiagnostics: configuration?.Diagnostics != DiagnosticsMode.Off);
            Configuration = configuration ?? new DatasetPipelineConfiguration();
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

        public bool RunPipeline()
        {
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
            if (isPipelineRunning)
                Pipeline.Dispose();
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

        public (string, string) GetStoreName(string streamName, string processName, Session? session)
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
            return Microsoft.Psi.Pipeline.CreateSynchedPipeline(Pipeline, name);
        }

    }
}