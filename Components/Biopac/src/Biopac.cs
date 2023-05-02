using System;
using System.Windows.Media.Animation;
using BiopacInterop;
using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace Biopac {
    /// <summary>
    /// StringProducer class.
    /// </summary>
    public class Biopac : ISourceComponent, IProducer<int> {

        private BiopacCommunicatorWrapper communicator;

        private Thread? captureThread = null;
        private bool shutdown = false;
        private bool isSynchOnly;
        private readonly Pipeline pipelineLocal;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringProducer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        public Biopac(Pipeline pipeline, bool syncOnly = false)
        {
            OutString = pipeline.CreateEmitter<string>(this, nameof(OutString));
            Out = pipeline.CreateEmitter<int>(this, nameof(Out));

            pipelineLocal = pipeline;

            communicator = new BiopacCommunicatorWrapper(syncOnly);

            // Application exit callback
            pipeline.ComponentCompleted += OnExitMethod;
            isSynchOnly = syncOnly;
        }

        /// <summary>
        /// Gets. Emitter that encapsulates the string output stream.
        /// </summary>
        public Emitter<string> OutString { get; }

        /// <summary>
        /// Gets. Emitter that encapsulates the output stream.
        /// </summary>
        public Emitter<int> Out { get; }

        private void Reset() {
            if (communicator.getAcquisitionInProgress() == 1) {
                if (communicator.toggleAcquisition() == 0) {
                    Console.WriteLine("XML-RPC SERVER: toggleAcquisition() SUCCEEDED" + "\n" + "....." + "acquisition_progress = off");
                }
            }
        }

        /// <summary>
        /// Generates and time-stamps a data from Biopac.
        /// </summary>
        private void CaptureThreadProc()
        {
            while (!this.shutdown)
            {
                DateTime originatingTime = pipelineLocal.GetCurrentTime();
                int data = communicator.GetData();
                //string s = "Biopac";

                // No more data
                if (data == -1)
                   continue;

                Out.Post(data, originatingTime);
                OutString.Post(data.ToString(), originatingTime);
            }
        }

        /// <summary>
        /// Application exit method.
        /// </summary>
        private void OnExitMethod(object sender, EventArgs e) {
            Reset();
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            communicator.StartSyncedCommunication();
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);
            if (!isSynchOnly)
            {
                this.captureThread = new Thread(new ThreadStart(this.CaptureThreadProc));
                this.captureThread.Start();
            }
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            Reset();
            shutdown = true;
            TimeSpan waitTime = TimeSpan.FromSeconds(1);
            if (this.captureThread != null && this.captureThread.Join(waitTime) != true)
            {
                captureThread.Abort();
            }
            notifyCompleted();
        }
    }
}
