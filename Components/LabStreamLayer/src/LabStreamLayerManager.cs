using Microsoft.Psi;
using static LSL.liblsl;
using System;
using System.Management;

namespace SAAC.LabStreamLayer
{
    public class LabStreamLayerManager : IDisposable
    {
        public bool IsRunning { get; private set; }
        public Dictionary<string, ILabStreamLayerComponent> LabStreamComponents { get; private set; }
        public EventHandler<string>? NewStream;
        public EventHandler<string>? RemovedStream;

        protected ContinuousResolver resolver;
        protected Pipeline pipeline;
        protected LogStatus log;

        private Thread? thread;
        private int maxBufferLength;
        private int updateSleepTime;
        private double lslStratTime;

        public LabStreamLayerManager(Pipeline pipeline, LogStatus? log = null, int updateSleepTime = 500, int maxBufferLength = 512)
        {
            this.pipeline = pipeline;
            this.maxBufferLength = maxBufferLength;
            this.updateSleepTime = updateSleepTime;
            this.log = log ?? Console.WriteLine;
            resolver = new ContinuousResolver();
            LabStreamComponents = new Dictionary<string, ILabStreamLayerComponent>();
            thread = null;
        }

        public void Start()
        {
            IsRunning = true;
            thread = new Thread(new ThreadStart(this.Update));
            thread.Start();
            log($"Starting LabStreamLayerManager protocol {protocol_version()} library {library_version()}");
        }

        public void Stop()
        {
            log("Stoping LabStreamLayerManager");
            Dispose();
        }

        public void Dispose()
        {
            IsRunning = false;
            thread?.Abort();
            resolver.Dispose();
        }

        protected void Update()
        {
            while (IsRunning)
            {
                IEnumerable<StreamInfo> results = resolver.results();
                List<string> existing = LabStreamComponents.Join(results, k => k.Key, streamInfo => $"{streamInfo.name()}-{streamInfo.type()}", (name, info) => name.Key).ToList();

                var ComponentsToRemove = LabStreamComponents.Where((d, i) => !existing.Contains(d.Key));
                foreach (var info in ComponentsToRemove)
                    RemoveComponent(info.Value.GetStreamInfo());

                IEnumerable<StreamInfo> toAdd = results.Where((streamInfo) => !existing.Contains($"{streamInfo.name()}-{streamInfo.type()}"));
                foreach (StreamInfo info in toAdd)
                    CreateComponent(info);

                Thread.Sleep(updateSleepTime);
            }
        }

        protected void CreateComponent(StreamInfo info)
        {
            StreamInlet inlet = new StreamInlet(info, maxBufferLength, postproc_flags: processing_options_t.proc_clocksync);
            dynamic? labStreamLayerComponent = null;
            switch (info.channel_format())
            {
                case channel_format_t.cf_undefined:
                    return;
                case channel_format_t.cf_string:
                    labStreamLayerComponent = new LabStreamLayerComponent<string>(ref pipeline, info, inlet, maxBufferLength);
                    break;
                case channel_format_t.cf_double64:
                    labStreamLayerComponent = new LabStreamLayerComponent<double>(ref pipeline, info, inlet, maxBufferLength);
                    break;
                case channel_format_t.cf_float32:
                    labStreamLayerComponent = new LabStreamLayerComponent<float>(ref pipeline, info, inlet, maxBufferLength);
                    break;
                case channel_format_t.cf_int64:
                    labStreamLayerComponent = new LabStreamLayerComponent<long>(ref pipeline, info, inlet, maxBufferLength);
                    break;
                case channel_format_t.cf_int32:
                    labStreamLayerComponent = new LabStreamLayerComponent<int>(ref pipeline, info, inlet, maxBufferLength);
                    break;
                case channel_format_t.cf_int16:
                    labStreamLayerComponent = new LabStreamLayerComponent<short>(ref pipeline, info, inlet, maxBufferLength);
                    break;
                case channel_format_t.cf_int8:
                    labStreamLayerComponent = new LabStreamLayerComponent<char>(ref pipeline, info, inlet, maxBufferLength);
                    break;
            }
            string key = $"{info.name()}-{info.type()}";
            LabStreamComponents.Add(key, labStreamLayerComponent);
            log($"LabStreamLayerManager component {key} created.");
            //log($"Component info :\n{info.as_xml()}");
            NewStream?.Invoke(this, key);
        }

        protected void RemoveComponent(StreamInfo info)
        {
            string key = $"{info.name()}-{info.type()}";
            if (!LabStreamComponents.ContainsKey(key))
                return;
            LabStreamComponents[key].Dispose();
            LabStreamComponents.Remove(key);
            log($"LabStreamLayerManager component {key} removed.");
            RemovedStream?.Invoke(this, key);
        }
    }
}
