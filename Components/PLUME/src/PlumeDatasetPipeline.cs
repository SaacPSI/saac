using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Data;
using SAAC.PipelineServices;

namespace SAAC.PLUME
{
    public class PlumeDatasetPipeline : DatasetPipeline, ISourceComponent
    {
        protected PlumeFileParser Parser;
        protected Session Session;

        protected Dictionary<string, object> Emitters = new Dictionary<string, object>();

        public PlumeDatasetPipeline(DatasetPipelineConfiguration datasetPipelineConfiguration, string name, List<string>? assembliesToLoad = null, LogStatus? log = null)
            : base(datasetPipelineConfiguration, name, log)
        {
            this.Parser = new PlumeFileParser(assembliesToLoad, log);
        }
        public PlumeDatasetPipeline(PlumeFileParser parser, DatasetPipelineConfiguration datasetPipelineConfiguration, string name, LogStatus? log = null)
            : base(datasetPipelineConfiguration, name, log)
        {
            this.Parser = parser;
        }

        protected virtual void SetDelegatesAndStores(Dictionary<string, Type> dataToStore)
        {
            if (this.Parser == null)
            {
                Log("Parser is null, cannot set delegates and stores.");
                return;
            }
        
            Session = this.CreateOrGetSessionFromMode(this.Parser.Name) ?? this.CreateOrGetSession("PlumeDatasetPipeline");

            foreach(var data in dataToStore)
            {
                if (Emitters.ContainsKey(data.Key))
                    continue;
                var emitter = typeof(Pipeline).GetMethod("CreateEmitter").MakeGenericMethod(data.Value).Invoke(this.Pipeline, [this, data.Key, null]);
                typeof(PlumeDatasetPipeline).GetMethod("CreateStore").MakeGenericMethod(data.Value).Invoke(this, [this.Pipeline, Session, data.Key, Parser.Name, emitter]);
                Emitters.Add(data.Key, emitter);
            }
            this.Parser.OnTransformUpdate += TransformMessage;
        }

        private void TransformMessage(string name, CoordinateSystem msg, ulong? timestamp)
        {
            if(Emitters.ContainsKey(name))
            {
                var time = Parser.StartTime.AddTicks(timestamp.HasValue ? (long)timestamp.Value : 0);
                //var time = DateTime.Now.AddTicks(timestamp.HasValue ? (long)timestamp.Value : 0);
                ((Emitter<CoordinateSystem>)Emitters[name]).Post(msg, time);
                this.Log($"->Posted message to {name} at {time}");
            }
        }

        public void LoadPlumeFile(string file, Dictionary<string, Type> dataToStore)
        {
            if (!System.IO.File.Exists(file))
            {
                Log($"File {file} does not exist.");
                return;
            }
            this.Parser.ParseFile(file);
            this.SetDelegatesAndStores(dataToStore);
            this.Pipeline.ProposeReplayTime(new TimeInterval(Parser.StartTime, DateTime.MaxValue));
            this.RunPipelineAndSubpipelines();
            this.Parser.UnpackAll();
            this.Stop(1000);
            Log($"File {file} loaded.");
        }

        void ISourceComponent.Start(Action<DateTime> notifyCompletionTime)
        {
            notifyCompletionTime(DateTime.MaxValue);
        }

        void ISourceComponent.Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            notifyCompleted();
        }
        protected override void PipelineRunAsync()
        {
            Pipeline.RunAsync(Parser.StartTime, false);
        }
    }
}
