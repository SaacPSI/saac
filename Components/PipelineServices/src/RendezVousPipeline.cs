// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

// USING https://github.com/SaacPSI/psi/ branch 'Pipeline' or 'PsiStudio' version of Psi.Runtime package
namespace SAAC.PipelineServices
{
    using System.Runtime.InteropServices;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.Interop.Transport;
    using Microsoft.Psi.Remoting;

    /// <summary>
    /// Pipeline that supports rendezvous-based distributed communication.
    /// </summary>
    public class RendezVousPipeline : DatasetPipeline
    {
        /// <summary>
        /// Clock synchronization process name.
        /// </summary>
        public const string ClockSynchProcessName = "ClockSynch";

        /// <summary>
        /// Diagnostics process name.
        /// </summary>
        public const string DiagnosticsProcessName = "Diagnostics";

        /// <summary>
        /// Command process name.
        /// </summary>
        public const string CommandProcessName = "Command";

        private List<TcpSource<(Command, string)>> commandTcpSources = new List<TcpSource<(Command, string)>>();
        private Helpers.PipeToMessage<(Command, string)> p2m;

        /// <summary>
        /// Initializes a new instance of the <see cref="RendezVousPipeline"/> class.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="name">The name.</param>
        /// <param name="rendezVousServerAddress">The rendezvous server address.</param>
        /// <param name="log">The log action.</param>
        /// <param name="connectors">The connectors.</param>
        public RendezVousPipeline(Pipeline parent, RendezVousPipelineConfiguration? configuration, string name = nameof(RendezVousPipeline), string? rendezVousServerAddress = null, LogStatus? log = null, Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null)
            : base(parent, configuration, name, log, connectors)
        {
            this.Initialize(configuration, name, rendezVousServerAddress, log);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RendezVousPipeline"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="name">The name.</param>
        /// <param name="rendezVousServerAddress">The rendezvous server address.</param>
        /// <param name="log">The log action.</param>
        /// <param name="connectors">The connectors.</param>
        public RendezVousPipeline(RendezVousPipelineConfiguration? configuration, string name = nameof(RendezVousPipeline), string? rendezVousServerAddress = null, LogStatus? log = null, Dictionary<string, Dictionary<string, ConnectorInfo>>? connectors = null)
            : base(configuration, name, log, connectors)
        {
            this.Initialize(configuration, name, rendezVousServerAddress, log);
        }

        /// <summary>
        /// Command types.
        /// </summary>
        public enum Command
        {
            /// <summary>
            /// Initialize command.
            /// </summary>
            Initialize,

            /// <summary>
            /// Run command.
            /// </summary>
            Run,

            /// <summary>
            /// Stop command.
            /// </summary>
            Stop,

            /// <summary>
            /// Reset command.
            /// </summary>
            Reset,

            /// <summary>
            /// Close command.
            /// </summary>
            Close,

            /// <summary>
            /// Status command.
            /// </summary>
            Status,
        }

        /// <summary>
        /// Delegate for command receive events.
        /// </summary>
        /// <param name="process">The process name.</param>
        /// <param name="command">The command and argument tuple.</param>
        public delegate void OnCommandReceive(string process, (Command, string) command);

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public RendezVousPipelineConfiguration Configuration { get; private set; }

        /// <summary>
        /// Gets the command emitter.
        /// </summary>
        protected Emitter<(Command, string)> CommandEmitter { get; private set; }

        /// <summary>
        /// The process names.
        /// </summary>
        protected List<string> processNames;

        /// <summary>
        /// Whether the pipeline is started.
        /// </summary>
        protected bool isStarted;

        /// <summary>
        /// The rendezvous relay.
        /// </summary>
        protected RendezvousRelay rendezvousRelay;

        /// <summary>
        /// The rendezvous object.
        /// </summary>
        protected dynamic rendezVous;

        /// <summary>
        /// The command pipeline.
        /// </summary>
        protected Pipeline commandPipeline;

        /// <summary>
        /// Processes to add when active.
        /// </summary>
        protected List<Rendezvous.Process> rendezvousProcessesToAddWhenActive;

        /// <summary>
        /// Starts the rendezvous pipeline with an optional time interval.
        /// </summary>
        /// <param name="interval">The optional time interval for the pipeline.</param>
        public void Start(TimeInterval? interval = null)
        {
            if (this.isStarted)
            {
                return;
            }

            this.isStarted = true;
            if (!this.StartRendezVousRelay(interval ?? TimeInterval.Infinite))
            {
                return;
            }

            if (this.Configuration.AutomaticPipelineRun && this.Configuration.ClockPort != 0)
            {
                this.RunPipelineAndSubpipelines();
            }
        }

        /// <summary>
        /// Starts the pipeline as a source component.
        /// </summary>
        /// <param name="notifyCompletionTime">Delegate to notify completion time.</param>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.Start();
            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <summary>
        /// Stops the rendezvous pipeline.
        /// </summary>
        /// <param name="maxWaitingTime">Maximum time to wait for pipeline stop in milliseconds.</param>
        public override void Stop(int maxWaitingTime = 1000)
        {
            this.Dataset?.Save();
            if (!this.isStarted)
            {
                return;
            }

            this.isStarted = false;
            this.rendezVous.Stop();
            base.Stop(maxWaitingTime);
        }

        /// <summary>
        /// Stops the pipeline as a source component.
        /// </summary>
        /// <param name="finalOriginatingTime">The final originating time.</param>
        /// <param name="notifyCompleted">Delegate to notify completion.</param>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.Stop();
            notifyCompleted();
        }

        /// <summary>
        /// Disposes the rendezvous pipeline and releases all resources.
        /// </summary>
        public new void Dispose()
        {
            this.Dataset?.Save();
            var copy = this.processNames.DeepClone();
            foreach (var prcName in copy)
            {
                this.RemoveProcess(prcName);
            }

            if (this.commandPipeline is not null)
            {
                this.CommandEmitter = null;
                foreach (var commandSource in this.commandTcpSources)
                {
                    commandSource.Dispose();
                }

                this.commandPipeline = null;
            }

            base.Dispose();
            this.rendezVous.Dispose();
            this.Stores = null;
        }

        /// <summary>
        /// Resets the rendezvous pipeline to its initial state.
        /// </summary>
        /// <param name="pipeline">Optional pipeline to reset to.</param>
        public override void Reset(Pipeline? pipeline = null)
        {
            base.Reset(pipeline);
            this.processNames?.Clear();
            foreach (Rendezvous.Process prc in this.rendezvousRelay.Rendezvous.Processes)
            {
                if (!prc.Name.Contains(CommandProcessName))
                {
                    this.rendezvousRelay.Rendezvous.TryRemoveProcess(prc);
                }
            }
        }

        /// <summary>
        /// Sends a command to a target process.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="target">The target process name.</param>
        /// <param name="arguments">Command arguments.</param>
        /// <returns>True if the command was sent successfully; otherwise false.</returns>
        public bool SendCommand(Command command, string target, string arguments)
        {
            if (this.CommandEmitter != null)
            {
                this.CommandEmitter.Post((command, $"{target};{arguments}"), this.commandPipeline.GetCurrentTime());
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a clock synchronization process to the rendezvous.
        /// </summary>
        /// <param name="interval">The time interval for clock synchronization.</param>
        /// <returns>True if the process was added successfully; otherwise false.</returns>
        public bool AddSynchClockProcess(TimeInterval interval)
        {
            if (!this.processNames.Contains(ClockSynchProcessName))
            {
                if (this.owningPipeline)
                {
                    var remoteClock = new RemoteClockExporter(this.Configuration.ClockPort);
                    this.AddProcess(new Rendezvous.Process(
                        ClockSynchProcessName,
                        [remoteClock.ToRendezvousEndpoint(this.Configuration.RendezVousHost)]));
                }
                else
                {
                    var remoteClock = new RemotePipelineClockExporter(this.Pipeline, this.Configuration.ClockPort, interval);
                    this.AddProcess(new Rendezvous.Process(
                        ClockSynchProcessName,
                        [remoteClock.ToRendezvousEndpoint(this.Configuration.RendezVousHost)]));
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a connecting process with an event handler.
        /// </summary>
        /// <param name="name">The process name.</param>
        /// <param name="eventProcess">The event handler for process events.</param>
        /// <returns>True if the process was added successfully; otherwise false.</returns>
        public bool AddConnectingProcess(string name, EventHandler<Rendezvous.Process> eventProcess)
        {
            if (this.rendezvousRelay == null)
            {
                return false;
            }

            this.rendezvousRelay.Rendezvous.ProcessAdded += eventProcess;
            this.processNames.Add(name);
            return true;
        }

        /// <summary>
        /// Adds a process to the rendezvous.
        /// </summary>
        /// <param name="newProcess">The process to add.</param>
        /// <returns>True if the process was added successfully; otherwise false.</returns>
        public bool AddProcess(Rendezvous.Process newProcess)
        {
            if (this.rendezvousRelay == null)
            {
                return false;
            }

            this.processNames.Add(newProcess.Name);
            if (this.rendezVous.IsActive)
            {
                if (!this.rendezvousRelay.Rendezvous.TryAddProcess(newProcess))
                {
                    this.processNames.Remove(newProcess.Name);
                    this.Log($"Failed to AddProcess {newProcess.Name}");
                    return false;
                }
            }
            else
            {
                this.rendezvousProcessesToAddWhenActive.Add(newProcess);
            }

            return true;
        }

        /// <summary>
        /// Removes a process from the rendezvous.
        /// </summary>
        /// <param name="processName">The name of the process to remove.</param>
        /// <returns>True if the process was removed successfully; otherwise false.</returns>
        public bool RemoveProcess(string processName)
        {
            if (this.rendezvousRelay == null)
            {
                return false;
            }

            if (this.rendezvousRelay.Rendezvous.TryRemoveProcess(processName))
            {
                this.processNames.Remove(processName);
                this.Connectors.Remove(processName);
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Generates a TCP endpoint for a producer stream.
        /// </summary>
        /// <typeparam name="T">The type of data in the stream.</typeparam>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="port">The TCP port.</param>
        /// <param name="producer">The producer stream.</param>
        /// <param name="streamName">The stream name.</param>
        /// <param name="process">The process to add the endpoint to.</param>
        public void GenerateTCPEnpoint<T>(Pipeline parent, int port, IProducer<T> producer, string streamName, ref Rendezvous.Process process)
        {
            Type type = typeof(T);
            if (!this.Configuration.TypesSerializers.ContainsKey(type))
            {
                throw new Exception($"Missing serializer of type {type} in configuration.");
            }

            TcpWriter<T> writer = new TcpWriter<T>(parent, port, this.Configuration.TypesSerializers[type].GetFormat());
            producer.PipeTo(writer);
            process.AddEndpoint(writer.ToRendezvousEndpoint(this.Configuration.RendezVousHost, streamName));
        }

        /// <summary>
        /// Generates a TCP process from connectors in the specified store.
        /// </summary>
        /// <param name="storeName">The store name.</param>
        /// <param name="startingPort">The starting TCP port.</param>
        /// <returns>The next available port, or 0 if failed.</returns>
        public int GenerateTCPProcessFromConnectors(string storeName, int startingPort)
        {
            if (this.Connectors.ContainsKey(storeName))
            {
                return this.GenerateTCPProcessFromConnectors(storeName, this.Connectors[storeName], startingPort);
            }

            return 0;
        }

        /// <summary>
        /// Generates a TCP process from a dictionary of connectors.
        /// </summary>
        /// <param name="processName">The process name.</param>
        /// <param name="connectors">The dictionary of connectors.</param>
        /// <param name="startingPort">The starting TCP port.</param>
        /// <returns>The next available port, or 0 if failed.</returns>
        public int GenerateTCPProcessFromConnectors(string processName, Dictionary<string, ConnectorInfo> connectors, int startingPort)
        {
            Rendezvous.Process process = new Rendezvous.Process(processName);
            Subpipeline parent = this.GetOrCreateSubpipeline(processName);
            foreach (var connector in connectors)
            {
                var producer = typeof(ConnectorInfo).GetMethod("CreateBridge").MakeGenericMethod(connector.Value.DataType).Invoke(connector.Value,[parent]);
                typeof(RendezVousPipeline).GetMethod("GenerateTCPEnpoint").MakeGenericMethod(connector.Value.DataType).Invoke(this,[parent, startingPort++, producer, connector.Key, process]);
            }

            if (this.isPipelineRunning)
            {
                parent.Start((d) => { this.Log($"SubPipeline {process.Name} started @{d}."); });
            }

            return this.AddProcess(process) ? startingPort : 0;
        }

        /// <summary>
        /// Generates a remote endpoint for a set of connectors.
        /// </summary>
        /// <param name="parent">The parent pipeline.</param>
        /// <param name="port">The TCP port.</param>
        /// <param name="connectors">The dictionary of connectors.</param>
        /// <param name="process">The process to add the endpoint to.</param>
        /// <param name="transportKind">The transport kind for the remote endpoint.</param>
        public void GenerateRemoteEnpoint(Pipeline parent, int port, Dictionary<string, ConnectorInfo> connectors, ref Rendezvous.Process process, TransportKind transportKind = TransportKind.Tcp)
        {
            RemoteExporter writer = new RemoteExporter(parent, port, transportKind);
            foreach (var connector in connectors)
            {
                var producer = typeof(ConnectorInfo).GetMethod("CreateBridge").MakeGenericMethod(connector.Value.DataType).Invoke(connector.Value,[parent]);

                // Marshal.SizeOf(connector.Value.DataType) > 4096 if true allow only one stream in exporter ?
                // Removing the amiguity of the Write method with the one from Exporter class which is the one we want to call, as the RemoteExporter class also have a Write method but with a different signature.
                typeof(Exporter).GetMethods().FirstOrDefault(m => m.Name == "Write" && m.IsGenericMethodDefinition).MakeGenericMethod(connector.Value.DataType).Invoke(writer.Exporter, [producer, connector.Key, Marshal.SizeOf(connector.Value.DataType) > 4096, null]);
            }

            process.AddEndpoint(writer.ToRendezvousEndpoint(this.Configuration.RendezVousHost));
        }

        /// <summary>
        /// Generates a remote process from connectors in the specified store.
        /// </summary>
        /// <param name="storeName">The store name.</param>
        /// <param name="startingPort">The starting TCP port.</param>
        /// <returns>True if the process was generated successfully; otherwise false.</returns>
        public bool GenerateRemoteProcessFromConnectors(string storeName, int startingPort)
        {
            if (this.Connectors.ContainsKey(storeName))
            {
                return this.GenerateRemoteProcessFromConnectors(storeName, this.Connectors[storeName], startingPort);
            }

            return false;
        }

        /// <summary>
        /// Generates a remote process from a dictionary of connectors.
        /// </summary>
        /// <param name="processName">The process name.</param>
        /// <param name="connectors">The dictionary of connectors.</param>
        /// <param name="startingPort">The starting TCP port.</param>
        /// <returns>True if the process was generated successfully; otherwise false.</returns>
        public bool GenerateRemoteProcessFromConnectors(string processName, Dictionary<string, ConnectorInfo> connectors, int startingPort)
        {
            Rendezvous.Process process = new Rendezvous.Process(processName);
            Pipeline parent = this.GetOrCreateSubpipeline(processName);
            this.GenerateRemoteEnpoint(parent, startingPort, connectors, ref process);
            parent.RunAsync();
            return this.AddProcess(process);
        }

        /// <summary>
        /// Starts the rendezvous relay and initializes all processes.
        /// </summary>
        /// <param name="interval">The time interval for the relay.</param>
        /// <param name="createSynchClockProcess">Whether to create a clock synchronization process.</param>
        /// <returns>True if the relay started successfully; otherwise false.</returns>
        protected bool StartRendezVousRelay(TimeInterval? interval = null, bool createSynchClockProcess = true)
        {
            if (this.rendezvousRelay == null)
            {
                return false;
            }

            this.rendezvousRelay.Rendezvous.ProcessAdded += this.RendezvousProcessAdded;
            this.rendezvousRelay.Rendezvous.ProcessRemoved += this.RendezvousProcessRemoved;
            this.rendezvousRelay.Error += (s, e) => { this.Log(e.Message);
                this.Log(e.HResult.ToString()); };
            this.rendezVous.Start();
            foreach (Rendezvous.Process prc in this.rendezvousProcessesToAddWhenActive)
            {
                this.AddProcess(prc);
            }

            this.rendezvousProcessesToAddWhenActive.Clear();
            if (this.Dataset != null)
            {
                switch (this.Configuration.Diagnostics)
                {
                    case DiagnosticsMode.Store:
                        this.CreateStore(this.Pipeline, this.CreateOrGetSessionFromMode(this.Configuration.SessionName + "_Diagnostics"), this.name, DiagnosticsProcessName, this.Pipeline.Diagnostics);
                        break;
                    case DiagnosticsMode.Export:
                        var remoteDiagnostics = new RemoteExporter(this.Pipeline, this.Configuration.DiagnosticPort, TransportKind.Tcp);
                        remoteDiagnostics.Exporter.Write(this.Pipeline.Diagnostics, DiagnosticsProcessName);
                        this.AddProcess(new Rendezvous.Process($"{this.name}-{DiagnosticsProcessName}",[remoteDiagnostics.ToRendezvousEndpoint(this.Configuration.RendezVousHost)]));
                        break;
                }
            }

            if (this.Configuration.ClockPort != 0)
            {
                this.AddSynchClockProcess(interval);
            }

            this.commandPipeline = Pipeline.Create(CommandProcessName, DeliveryPolicy.SynchronousOrThrottle, enableDiagnostics: false);
            if (this.Configuration.CommandPort != 0)
            {
                this.CommandEmitter = this.commandPipeline.CreateEmitter<(Command, string)>(this, $"{this.name}-CommandEmitter");
                TcpWriterMulti<(Command, string)> writer = new TcpWriterMulti<(Command, string)>(this.commandPipeline, this.Configuration.CommandPort, PsiFormats.PsiFormatCommand.GetFormat(), CommandProcessName);
                this.CommandEmitter.PipeTo(writer);
                this.AddProcess(new Rendezvous.Process($"{this.name}-{CommandProcessName}",[writer.ToRendezvousEndpoint(this.Configuration.RendezVousHost, CommandProcessName)]));
            }

            this.commandPipeline.RunAsync(ReplayDescriptor.ReplayAllRealTime);
            this.Log("RendezVous started!");
            return true;
        }

        /// <summary>
        /// Handles the event when a process is added to the rendezvous.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="process">The process that was added.</param>
        protected void RendezvousProcessAdded(object? sender, Rendezvous.Process process)
        {
            this.Log($"Process {process.Name}");
            if (this.processNames.Contains(process.Name))
            {
                return;
            }

            if (process.Name.Contains(CommandProcessName))
            {
                if (!process.Name.Contains(this.name))
                {
                    this.ProcessAddedCommand(process);
                }
                else
                {
                    return;
                }
            }
            else if (process.Name.Contains(DiagnosticsProcessName))
            {
                if (!process.Name.Contains(this.name))
                {
                    this.ProcessAddedDiagnotics(process);
                }
                else
                {
                    return;
                }
            }
            else
            {
                switch (process.Name)
                {
                    case ClockSynchProcessName:
                        if (this.Configuration.ClockPort == 0)
                        {
                            this.ProcessAddedClock(process);
                        }

                        break;
                    default:
                        if (this.Configuration.RecordIncomingProcess)
                        {
                            this.ProcessAddedData(process);
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Processes a clock synchronization process that was added to the rendezvous.
        /// </summary>
        /// <param name="process">The clock process that was added.</param>
        protected void ProcessAddedClock(Rendezvous.Process process)
        {
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.RemotePipelineClockExporterEndpoint remotePipelineClockEndpoint)
                {
                    remotePipelineClockEndpoint.ToRemotePipelineClockImporter(this.Pipeline);
                    if (this.Configuration.AutomaticPipelineRun)
                    {
                        this.RunPipelineAndSubpipelines();
                    }

                    return;
                }
                else if (endpoint is Rendezvous.RemoteClockExporterEndpoint remoteClockEndpoint)
                {
                    remoteClockEndpoint.ToRemoteClockImporter(this.Pipeline);
                    if (this.Configuration.AutomaticPipelineRun)
                    {
                        this.RunPipelineAndSubpipelines();
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Processes a command process that was added to the rendezvous.
        /// </summary>
        /// <param name="process">The command process that was added.</param>
        protected void ProcessAddedCommand(Rendezvous.Process process)
        {
            if (this.Configuration.CommandDelegate == null)
            {
                return;
            }

            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.TcpSourceEndpoint)
                {
                    Rendezvous.TcpSourceEndpoint? source = endpoint as Rendezvous.TcpSourceEndpoint;
                    if (source == null)
                    {
                        continue;
                    }

                    foreach (var stream in endpoint.Streams)
                    {
                        if (stream.StreamName == CommandProcessName)
                        {
                            Subpipeline subCommandPipeline = Subpipeline.Create(this.commandPipeline, process.Name);
                            var tcpSource = Microsoft.Psi.Interop.Rendezvous.Operators.ToTcpSource<(Command, string)>(source, subCommandPipeline, PsiFormats.PsiFormatCommand.GetFormat(), null, true, stream.StreamName);
                            this.p2m = new Helpers.PipeToMessage<(Command, string)>(subCommandPipeline, this.Configuration.CommandDelegate, process.Name, $"p2m-{process.Name}");
                            Microsoft.Psi.Operators.PipeTo(tcpSource.Out, this.p2m.In);
                            subCommandPipeline.Start((d) => { });
                            this.commandTcpSources.Add(tcpSource);

                            // TriggerNewProcessEvent(process.Name);
                            this.Log($"Subpipeline {process.Name} started.");
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Processes a diagnostics process that was added to the rendezvous.
        /// </summary>
        /// <param name="process">The diagnostics process that was added.</param>
        protected void ProcessAddedDiagnotics(Rendezvous.Process process)
        {
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.RemoteExporterEndpoint)
                {
                    Rendezvous.RemoteExporterEndpoint? source = endpoint as Rendezvous.RemoteExporterEndpoint;
                    if (source == null)
                    {
                        continue;
                    }

                    foreach (var stream in source.Streams)
                    {
                        if (stream.GetType() == typeof(PipelineDiagnostics))
                        {
                            Subpipeline processSubPipeline = this.GetOrCreateSubpipeline(process.Name);
                            this.Connection(stream.StreamName, DiagnosticsProcessName, this.CreateOrGetSession(this.Configuration.SessionName + "_Diagnostics"), source, processSubPipeline, true);
                            if (this.isPipelineRunning)
                            {
                                processSubPipeline.Start((d) => { });
                                this.Log($"SubPipeline {process.Name} started.");
                            }

                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Processes a data process that was added to the rendezvous.
        /// </summary>
        /// <param name="process">The data process that was added.</param>
        protected void ProcessAddedData(Rendezvous.Process process)
        {
            if (process.Endpoints.Count() == 0)
            {
                return;
            }

            int elementAdded = 0;
            Subpipeline processSubPipeline = this.GetOrCreateSubpipeline(process.Name);
            Session? session = this.CreateOrGetSessionFromMode(process.Name);
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.TcpSourceEndpoint)
                {
                    Rendezvous.TcpSourceEndpoint? source = endpoint as Rendezvous.TcpSourceEndpoint;
                    if (source == null)
                    {
                        continue;
                    }

                    foreach (var stream in endpoint.Streams)
                    {
                        this.Log($"\tStream {stream.StreamName}");
                        if (this.Configuration.TopicsTypes.ContainsKey(stream.StreamName))
                        {
                            Type type = this.Configuration.TopicsTypes[stream.StreamName];
                            if (!this.Configuration.TypesSerializers.ContainsKey(type))
                            {
                                throw new Exception($"Missing serializer of type {type} in configuration.");
                            }

                            this.Connection(stream.StreamName, process.Name, session, source, processSubPipeline, !this.Configuration.NotStoredTopics.Contains(stream.StreamName), this.Configuration.TypesSerializers[type].GetFormat(), this.Configuration.Transformers.ContainsKey(stream.StreamName) ? this.Configuration.Transformers[stream.StreamName] : null);
                            elementAdded++;
                        }
                    }
                }
                else if (endpoint is Rendezvous.RemoteExporterEndpoint)
                {
                    Rendezvous.RemoteExporterEndpoint? source = endpoint as Rendezvous.RemoteExporterEndpoint;
                    if (source == null)
                    {
                        continue;
                    }

                    foreach (var stream in source.Streams)
                    {
                        elementAdded += this.Connection(stream.StreamName, process.Name, session, source, processSubPipeline, !this.Configuration.NotStoredTopics.Contains(stream.StreamName)) ? 1 : 0;
                    }
                }
            }

            this.Log($"Process {process.Name} sources added : {elementAdded}");
            if (elementAdded == 0 && session != null)
            {
                processSubPipeline.Dispose();
                if (session.Partitions.Count() == 0)
                {
                    this.Dataset?.RemoveSession(session);
                }

                return;
            }
            else if (this.isPipelineRunning)
            {
                processSubPipeline.Start((d) => { });
                this.Log($"SubPipeline {process.Name} started.");
            }

            // TriggerNewProcessEvent(process.Name);
            // Dataset?.Save();
        }

        /// <summary>
        /// Handles the event when a process is removed from the rendezvous.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The process that was removed.</param>
        private void RendezvousProcessRemoved(object sender, Rendezvous.Process e)
        {
            if (this.subpipelines.ContainsKey(e.Name))
            {
                this.subpipelines[e.Name].Dispose();
                this.Connectors.Remove(e.Name);

                // TriggerNewProcessEvent(e.Name);
            }

            this.RemovedEntry?.Invoke(this, e.Name);
        }

        /// <summary>
        /// Creates a connection for a TCP source endpoint with optional data transformation.
        /// </summary>
        /// <typeparam name="T">The type of data in the stream.</typeparam>
        /// <param name="streamName">The stream name.</param>
        /// <param name="processName">The process name.</param>
        /// <param name="session">The session to use.</param>
        /// <param name="source">The TCP source endpoint.</param>
        /// <param name="p">The pipeline.</param>
        /// <param name="storeSteam">Whether to store the stream.</param>
        /// <param name="deserializer">The deserializer for the data.</param>
        /// <param name="transformerType">Optional transformer type to apply to the data.</param>
        protected void Connection<T>(string streamName, string processName, Session? session, Rendezvous.TcpSourceEndpoint source, Pipeline p, bool storeSteam, Format<T> deserializer, Type? transformerType)
        {
            var storeName = this.GetStoreName(streamName, processName, session);
            var tcpSource = source.ToTcpSource<T>(p, deserializer, null, true, $"{processName}-{streamName}");
            if (this.Configuration.Debug)
            {
                tcpSource.Do((d, e) => { this.Log($"Receive {processName}-{streamName} data @{e.OriginatingTime} : {d}"); });
            }

            if (transformerType != null)
            {
                dynamic transformer = Activator.CreateInstance(transformerType,[p, $"{processName}-{streamName}_transformer"]);
                Microsoft.Psi.Operators.PipeTo(tcpSource.Out, transformer.In);
                if (transformerType.GetInterfaces().Intersect([typeof(IComplexTransformer)]).Count() > 0)
                {
                    transformer.CreateConnections(streamName, storeName, session, p, storeSteam, this);
                }
                else
                {
                    this.CreateConnectorAndStore(storeName.Item1, storeName.Item2, session, p, transformer.Out.Type, transformer, storeSteam);
                }
            }
            else
            {
                this.CreateConnectorAndStore(storeName.Item1, storeName.Item2, session, p, typeof(T), tcpSource, storeSteam);
            }
        }

        /// <summary>
        /// Creates a connection for a remote exporter endpoint.
        /// </summary>
        /// <param name="streamName">The stream name.</param>
        /// <param name="processName">The process name.</param>
        /// <param name="session">The session to use.</param>
        /// <param name="source">The remote exporter endpoint.</param>
        /// <param name="p">The pipeline.</param>
        /// <param name="storeSteam">Whether to store the stream.</param>
        /// <returns>True if the connection was created successfully; otherwise false.</returns>
        protected bool Connection(string streamName, string processName, Session? session, Rendezvous.RemoteExporterEndpoint source, Pipeline p, bool storeSteam)
        {
            var importer = source.ToRemoteImporter(p);
            if (!importer.Connected.WaitOne())
            {
                this.Log($"Failed to connect to {streamName}");
                return false;
            }

            foreach (var streamInfo in importer.Importer.AvailableStreams)
            {
                // This is on hold as it constraint more the use of the rendezVousPipeline system.
                // if (!Configuration.TopicsTypes.ContainsKey(streamInfo.Name) || streamName != streamInfo.Name)
                //    continue;
                this.Log($"\tStream {streamName}");
                var storeName = this.GetStoreName(streamName, processName, session);
                Type type = Type.GetType(streamInfo.TypeName);
                typeof(ConnectorsAndStoresCreator).GetMethod("CreateConnectorAndStore").MakeGenericMethod(type).Invoke(this,[storeName.Item1, storeName.Item2, session, p, type, typeof(Importer).GetMethod("OpenStream").MakeGenericMethod(type).Invoke(importer.Importer,[streamInfo.Name, null, null]), storeSteam]);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Initializes the rendezvous pipeline with the specified configuration.
        /// </summary>
        /// <param name="configuration">The pipeline configuration.</param>
        /// <param name="name">The pipeline name.</param>
        /// <param name="rendezVousServerAddress">Optional server address; if null, creates a server, otherwise connects as client.</param>
        /// <param name="log">Optional logging delegate.</param>
        private void Initialize(RendezVousPipelineConfiguration? configuration, string name = nameof(RendezVousPipeline), string? rendezVousServerAddress = null, LogStatus? log = null)
        {
            this.Configuration = configuration ?? new RendezVousPipelineConfiguration();
            this.rendezvousProcessesToAddWhenActive = new List<Rendezvous.Process>();
            this.processNames = new List<string>();
            if (rendezVousServerAddress == null)
            {
                this.rendezvousRelay = this.rendezVous = new RendezvousServer(this.Configuration.RendezVousPort);
            }
            else
            {
                this.rendezvousRelay = this.rendezVous = new RendezvousClient(rendezVousServerAddress, this.Configuration.RendezVousPort);
            }

            this.isStarted = this.isPipelineRunning = false;
        }
    }
}
