using System;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Interop.Transport;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.PsiStudio;

namespace SAAC.PsiStudioReplayExtension
{
    /// <summary>
    /// Class that manage connection de high level functionnalties with PsiStudio Network replay feature (<see cref="NetworkStreamsManager"/> in PsiStudio).
    /// </summary>
    public class PsiStudioNetworkConnector : IConsumerProducer<PsiStudioNetworkInfo, PsiStudioNetworkInfo>,  IDisposable
    {
        private TcpWriter<PsiStudioNetworkInfo>? writer;
        private TcpSource<PsiStudioNetworkInfo>? source;
        private string name;
        private Pipeline? pipeline;
        private bool isSynchedPipeline;
        private DateTime pipelineStartTime;

        /// <summary>
        /// Gets the current interval of time replayed.
        /// </summary>
        public TimeInterval PlayInterval { get; private set; }

        /// <summary>
        /// Gets the last time of a pause.
        /// </summary>
        public DateTime PauseTime { get; private set; }

        /// <summary>
        /// Gets the Receiver of the component.
        /// </summary>
        public Receiver<PsiStudioNetworkInfo>? In => writer?.In;

        /// <summary>
        /// Gets the Emitter of the component.
        /// </summary>
        public Emitter<PsiStudioNetworkInfo>? Out => source?.Out;

        /// <summary>
        /// Gets the EventHandler for handling incoming message from outside \psi components.
        /// </summary>
        public EventHandler<PsiStudioNetworkInfo>? OnReceiveMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="PsiStudioNetworkConnector"/> class.
        /// </summary>
        /// <param name="name">Name of the component.</param>
        public PsiStudioNetworkConnector(bool synchedPipeline = false, string name = nameof(PsiStudioNetworkConnector))
        {
            this.isSynchedPipeline = synchedPipeline;
            this.name = name;
            this.writer = null;
            this.source = null;
            this.pipeline = null;
            this.OnReceiveMessage = null;
            this.PlayInterval = TimeInterval.Empty;
            this.PauseTime = DateTime.MinValue;
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.writer?.Dispose();
            this.source?.Dispose();
        }

        /// <summary>
        /// Create the TcpSource with the information given.
        /// </summary>
        /// <param name="pipeline">The pipeline where the TcpWriter will be on.</param>
        /// <param name="address">Ip used by the writer.</param>
        /// <param name="port">Port used by the writer.</param>
        /// <param name="processName">The name of the rendezVous process.</param>
        public Rendezvous.Process CreateProcessWriter(Pipeline pipeline, string address, int port, string processName)
        { 
            this.pipeline = pipeline;           
            this.writer = new TcpWriter<PsiStudioNetworkInfo>(pipeline, port, PsiFormatPsiStudioNetworkInfo.GetFormat(), $"{name}-Out");
#if UNITY_6000_0_OR_NEWER
            return new Rendezvous.Process(processName, new System.Collections.Generic.List<Microsoft.Psi.Interop.Rendezvous.Rendezvous.Endpoint>(){writer.ToRendezvousEndpoint(address, name)});
#else
            return new Rendezvous.Process(processName, [writer.ToRendezvousEndpoint(address, name)]);
#endif
        }

        /// <summary>
        /// Create the TcpSource with the information given.
        /// </summary>
        /// <param name="pipeline">The pipeline where the TcpSource will be on.</param>
        /// <param name="address">Ip of the remote writer.</param>
        /// <param name="port">Port of the remote writer.</param>
        public void ConnectToSource(Pipeline pipeline, string address, int port)
        {
            this.source = new TcpSource<PsiStudioNetworkInfo>(pipeline, address, port, PsiFormatPsiStudioNetworkInfo.GetFormat(), null, true, $"{name}-In");
            this.source.Out.Do((data, enveloppe) => { OnMessage(data); });
        }

        /// <summary>
        /// Send a play message and store the time interval given.
        /// </summary>
        /// <param name="timeInterval">The interval of replay, if null then it will replay the full session.</param>
        public void SendPlay(TimeInterval? timeInterval = null)
        {
            if (this.pipeline is null)
                return;
            if (timeInterval != null)
                this.PlayInterval = timeInterval;

            this.pipelineStartTime = this.pipeline.GetCurrentTime();
            this.Out?.Post(new PsiStudioNetworkInfo(PsiStudioNetworkInfo.PsiStudioNetworkEvent.Playing, this.PlayInterval), this.pipelineStartTime);
        }

        /// <summary>
        /// Send a stop message and store the current time as time paused.
        /// </summary>
        public void SendPause()
        {
            if (this.pipeline is null)
                return;
            SendStop();
            this.PauseTime = this.isSynchedPipeline ? this.pipeline.GetCurrentTime() : this.PlayInterval.Left.AddSeconds((this.pipeline.GetCurrentTime() - this.pipelineStartTime).TotalSeconds);
        }

        /// <summary>
        /// Send a play message, with a new interval with as left bound the paused time.
        /// </summary>
        /// <param name="name">Name of the component.</param>
        public void SendResume()
        {
            if (this.PauseTime == DateTime.MinValue)
                return;
            SendPlay(new TimeInterval(this.PauseTime, this.PlayInterval.Right));
        }

        /// <summary>
        /// Send a stop message to PsiStudio.
        /// </summary>
        public void SendStop()
        {
            if (this.pipeline is null)
                return;
            this.Out?.Post(new PsiStudioNetworkInfo(PsiStudioNetworkInfo.PsiStudioNetworkEvent.Stopping, this.PlayInterval), this.pipeline.GetCurrentTime());
        }
        /// <summary>
        /// Send a new read speed message to PsiStudio.
        /// </summary>
        /// <param name="speed">The new speed to send.</param>
        public void SendPlaySpeed(double speed)
        {
            if (this.pipeline is null)
                return;
            this.Out?.Post(new PsiStudioNetworkInfo(PsiStudioNetworkInfo.PsiStudioNetworkEvent.PlaySpeed, this.PlayInterval, speed), this.pipeline.GetCurrentTime());
        }

        private void OnMessage(PsiStudioNetworkInfo message)
        {
            if (message.Event == PsiStudioNetworkInfo.PsiStudioNetworkEvent.Playing)
                PlayInterval = message.Interval; 
            OnReceiveMessage?.Invoke(this, message);
        }
    }
}
