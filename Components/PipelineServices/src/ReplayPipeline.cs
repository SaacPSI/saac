using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Data;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Media.Animation;
//using SaaCPsiStudio.src;

namespace SAAC.PipelineServices
{
    public class progressChecker : System.IProgress<double>
    {
        public RendezVousPipeline rdvP;
        public ReplayPipeline replayP;
        public double lastValue = -1;
        public double lastLastValue = -1;
        public bool isPipelineEnded = false;
        public DateTime sameValueTime = DateTime.MinValue;
        public double sameValue = 0;
        public List<TextWriter> streamsWriters = new List<TextWriter>();
        public progressChecker(ReplayPipeline replayPipeline, List<TextWriter> writers, RendezVousPipeline? pipeline = null)
        {
            rdvP = pipeline;
            replayP = replayPipeline;
            streamsWriters = writers;
            this.AppStatusIn = replayPipeline.Pipeline.CreateReceiver<DateTime>(this, ReceiverMessage, nameof(this.AppStatusIn));
            //this.AppStatusOut = replayPipeline.Pipeline.CreateEmitter<string>(this, nameof(this.AppStatusOut));
        }

        private void ReceiverMessage(DateTime arg1, Envelope envelope)
        {
            AppStatusOut.Post("message", replayP.Pipeline.GetCurrentTime());
        }

        public void Report(double value)
        {
            if (isPipelineEnded) return;

            if (replayP.Pipeline.GetCurrentTime() >= sameValueTime.AddMilliseconds(10000) && value == lastValue)
            {
                if(value == sameValue)
                {
                    //replayP.TriggerNewProcessEvent("EndSession");

                    foreach (var writer in streamsWriters)
                    {
                        CloseAndDisposeWriter(writer);
                    }
                    Console.WriteLine("Writer are closed and Session is ended");

                    if(rdvP != null) rdvP.Dataset.Save();
                    replayP.Dataset.Save();

                    try
                    {
                        if (rdvP != null) rdvP?.Stop();
                        //replayP?.Stop();
                        if (rdvP != null) rdvP.Dispose();
                        replayP.Dispose();
                    }
                    catch {  }
                    System.Environment.Exit(0);
                    isPipelineEnded = true;
                    Console.WriteLine("Pipeline Ended");
                }

                sameValueTime = replayP.Pipeline.GetCurrentTime();
                sameValue = value;
            }

            /*if (!isPipelineEnded && value >= 0.781*//*value == lastValue && value == lastLastValue*//*) //0.781
            {
                
            }*/
            lastValue = value;

            //Console.WriteLine($"Progress: {value}");
        }
        public void CloseAndDisposeWriter(TextWriter writer)
        {
            StreamWriter streamWriter = (StreamWriter)writer;
            if (writer == null) return;
            if (streamWriter.BaseStream.CanWrite)
            {
                try { writer.Flush(); }
                catch (ObjectDisposedException) { Console.WriteLine($"TextWriter {writer.ToString()} Flush exception"); }
                try { writer.Close(); }
                catch (ObjectDisposedException) { Console.WriteLine($"TextWriter {writer.ToString()} Close exception"); }
                try { writer.Dispose(); }
                catch (ObjectDisposedException) { Console.WriteLine($"TextWriter {writer.ToString()} Dispose exception"); }
            }
        }

        public void GetEmitter(Emitter<string> emitter)
        {
            AppStatusOut = emitter;
        }
        public Receiver<DateTime> AppStatusIn { get; set; }
        public Emitter<string> AppStatusOut { get; set; }
    }
    

    public class ReplayPipeline : DatasetPipeline
    {
        public enum ReplayType { FullSpeed, RealTime, IntervalFullSpeed, IntervalRealTime };

        public ReplayPipelineConfiguration Configuration { get; private set; }

        private DatasetLoader loader;
        private SortedSet<string> ReadOnlyStores;

        public ReplayPipeline(ReplayPipelineConfiguration configuration, string name = nameof(ReplayPipeline), LogStatus? log = null)
            : base(configuration, name, log)
        {
            Configuration = configuration ?? new ReplayPipelineConfiguration();
            loader = new DatasetLoader(Pipeline, Connectors, $"{name}-Loader");
            ReadOnlyStores = new SortedSet<string>();
            if (Dataset == null)
                throw new ArgumentNullException(nameof(Dataset));
            else if(Configuration.DatasetBackup)
            {
                var filename = Dataset.Filename;
                Dataset.SaveAs(Dataset.Filename.Insert(Dataset.Filename.Length - 4, "_backup"));
                Dataset.Filename = filename;
            }
        }

        public override void AddNewProcessEvent(EventHandler<(string, Dictionary<string, Dictionary<string, ConnectorInfo>>)> handler)
        {
            base.AddNewProcessEvent(handler);
            loader.AddNewProcessEvent(handler);
        }

        public override void AddRemoveProcessEvent(EventHandler<string> handler)
        {
            base.AddRemoveProcessEvent(handler);
            loader.AddRemoveProcessEvent(handler);
        }

        public bool LoadDatasetAndConnectors(string? sessionName = null)
        {
            if (loader.Load(Dataset, sessionName))
            {
                this.Connectors = loader.Connectors;
                foreach (var session in Connectors)
                    foreach (var connectorPair in session.Value)
                        ReadOnlyStores.Add(connectorPair.Value.StoreName);
                return true;
            }
            return false;
        }

        protected override void RunAsync(System.IProgress<double> progress)
        {
            switch (Configuration.ReplayType)
            {
                case ReplayType.FullSpeed:
                    Pipeline.RunAsync(ReplayDescriptor.ReplayAll, progress);
                    break;
                case ReplayType.RealTime:
                    Pipeline.RunAsync(ReplayDescriptor.ReplayAllRealTime, progress);
                    break;
                case ReplayType.IntervalFullSpeed:
                    Pipeline.RunAsync(new ReplayDescriptor(Configuration.ReplayInterval, false), progress);
                    break;
                case ReplayType.IntervalRealTime:
                    Pipeline.RunAsync(new ReplayDescriptor(Configuration.ReplayInterval, true), progress);
                    break;
            }
        }
        /*protected override void RunAsync()
        {
            switch(Configuration.ReplayType)
            {
                case ReplayType.FullSpeed:
                    Pipeline.RunAsync(ReplayDescriptor.ReplayAll);
                    break;
                case ReplayType.RealTime:
                    Pipeline.Run(ReplayDescriptor.ReplayAllRealTime);
                    break;
                case ReplayType.IntervalFullSpeed:
                    Pipeline.RunAsync(new ReplayDescriptor(Configuration.ReplayInterval, false)); 
                    break;
                case ReplayType.IntervalRealTime:
                    Pipeline.RunAsync(new ReplayDescriptor(Configuration.ReplayInterval, true));
                    break;
            }
        }*/

        public override void CreateStore<T>(Pipeline pipeline, Session session, string streamName, string storeName, IProducer<T> source)
        {
            if (ReadOnlyStores.Contains(storeName))
                throw new InvalidOperationException("Trying to write a Store that is readonly");
            base.CreateStore(pipeline, session, streamName, storeName, source);
        }

        public override (string, string) GetStoreName(string streamName, string processName, Session? session)
        {
            var names = base.GetStoreName(streamName, processName, session);
            if (ReadOnlyStores.Contains(names.Item2))
            {
                Log($"ReplayPipeline - GetStoreName : {names.Item2} already exist as Store Importer, switching name to {names.Item2}_{name}.");
                names.Item2 = $"{names.Item2}_{name}";
            }
            return names;
        }
        public void Dispose()
        {
            ReadOnlyStores.Clear();
            base.Dispose();
        }
    }
}
