// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.LabStreamLayer
{
    using Microsoft.Psi;
    using static LSL.liblsl;

    /// <summary>
    /// Manages Lab Streaming Layer (LSL) components, automatically discovering and creating components for available LSL streams.
    /// </summary>
    public class LabStreamLayerManager : IDisposable
    {
        private readonly ContinuousResolver resolver;
        private readonly LogStatus log;
        private readonly int maxBufferLength;
        private readonly int updateSleepTime;
        private Pipeline pipeline;
        private Thread? thread;
        private double lslStratTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="LabStreamLayerManager"/> class.
        /// </summary>
        /// <param name="pipeline">The PSI pipeline.</param>
        /// <param name="log">Optional logging delegate.</param>
        /// <param name="updateSleepTime">Time in milliseconds to sleep between stream discovery updates.</param>
        /// <param name="maxBufferLength">Maximum buffer length for LSL streams.</param>
        public LabStreamLayerManager(Pipeline pipeline, LogStatus? log = null, int updateSleepTime = 500, int maxBufferLength = 512)
        {
            this.pipeline = pipeline;
            this.maxBufferLength = maxBufferLength;
            this.updateSleepTime = updateSleepTime;
            this.log = log ?? Console.WriteLine;
            this.resolver = new ContinuousResolver();
            this.LabStreamComponents = new Dictionary<string, ILabStreamLayerComponent>();
            this.thread = null;
        }

        /// <summary>
        /// Event raised when a new LSL stream is discovered and a component is created.
        /// </summary>
        public EventHandler<string>? NewStream;

        /// <summary>
        /// Event raised when an LSL stream is removed and the component is disposed.
        /// </summary>
        public EventHandler<string>? RemovedStream;

        /// <summary>
        /// Gets a value indicating whether the manager is running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets the dictionary of active LSL components, keyed by stream name and type.
        /// </summary>
        public Dictionary<string, ILabStreamLayerComponent> LabStreamComponents { get; private set; }

        /// <summary>
        /// Starts the LSL stream discovery and management thread.
        /// </summary>
        public void Start()
        {
            this.IsRunning = true;
            this.thread = new Thread(new ThreadStart(this.Update));
            this.thread.Start();
            this.log($"Starting LabStreamLayerManager protocol {protocol_version()} library {library_version()}");
        }

        /// <summary>
        /// Stops the LSL stream discovery and management.
        /// </summary>
        public void Stop()
        {
            this.log("Stoping LabStreamLayerManager");
            this.Dispose();
        }

        /// <summary>
        /// Disposes the manager and all managed LSL components.
        /// </summary>
        public void Dispose()
        {
            this.IsRunning = false;
            this.thread?.Abort();
            this.resolver.Dispose();
        }

        /// <summary>
        /// Main update loop that discovers new LSL streams and removes disconnected ones.
        /// </summary>
        protected void Update()
        {
            while (this.IsRunning)
            {
                IEnumerable<StreamInfo> results = this.resolver.results();
                List<string> existing = this.LabStreamComponents.Join(results, k => k.Key, streamInfo => $"{streamInfo.name()}-{streamInfo.type()}", (name, info) => name.Key).ToList();

                var componentsToRemove = this.LabStreamComponents.Where((d, i) => !existing.Contains(d.Key));
                foreach (var info in componentsToRemove)
                {
                    this.RemoveComponent(info.Value.GetStreamInfo());
                }

                IEnumerable<StreamInfo> toAdd = results.Where((streamInfo) => !existing.Contains($"{streamInfo.name()}-{streamInfo.type()}"));
                foreach (StreamInfo info in toAdd)
                {
                    this.CreateComponent(info);
                }

                Thread.Sleep(this.updateSleepTime);
            }
        }

        /// <summary>
        /// Creates a new LSL component for the specified stream.
        /// </summary>
        /// <param name="info">The stream information.</param>
        protected void CreateComponent(StreamInfo info)
        {
            StreamInlet inlet = new StreamInlet(info, this.maxBufferLength, postproc_flags: processing_options_t.proc_clocksync);
            dynamic? labStreamLayerComponent = null;
            switch (info.channel_format())
            {
                case channel_format_t.cf_undefined:
                    return;
                case channel_format_t.cf_string:
                    labStreamLayerComponent = new LabStreamLayerComponent<string>(ref this.pipeline, info, inlet, this.maxBufferLength);
                    break;
                case channel_format_t.cf_double64:
                    labStreamLayerComponent = new LabStreamLayerComponent<double>(ref this.pipeline, info, inlet, this.maxBufferLength);
                    break;
                case channel_format_t.cf_float32:
                    labStreamLayerComponent = new LabStreamLayerComponent<float>(ref this.pipeline, info, inlet, this.maxBufferLength);
                    break;
                case channel_format_t.cf_int64:
                    labStreamLayerComponent = new LabStreamLayerComponent<long>(ref this.pipeline, info, inlet, this.maxBufferLength);
                    break;
                case channel_format_t.cf_int32:
                    labStreamLayerComponent = new LabStreamLayerComponent<int>(ref this.pipeline, info, inlet, this.maxBufferLength);
                    break;
                case channel_format_t.cf_int16:
                    labStreamLayerComponent = new LabStreamLayerComponent<short>(ref this.pipeline, info, inlet, this.maxBufferLength);
                    break;
                case channel_format_t.cf_int8:
                    labStreamLayerComponent = new LabStreamLayerComponent<char>(ref this.pipeline, info, inlet, this.maxBufferLength);
                    break;
            }

            string key = $"{info.name()}-{info.type()}";
            this.LabStreamComponents.Add(key, labStreamLayerComponent);
            this.log($"LabStreamLayerManager component {key} created.");

            // log($"Component info :\n{info.as_xml()}");
            this.NewStream?.Invoke(this, key);
        }

        /// <summary>
        /// Removes an LSL component for the specified stream.
        /// </summary>
        /// <param name="info">The stream information.</param>
        protected void RemoveComponent(StreamInfo info)
        {
            string key = $"{info.name()}-{info.type()}";
            if (!this.LabStreamComponents.ContainsKey(key))
            {
                return;
            }

            this.LabStreamComponents[key].Dispose();
            this.LabStreamComponents.Remove(key);
            this.log($"LabStreamLayerManager component {key} removed.");
            this.RemovedStream?.Invoke(this, key);
        }
    }
}
