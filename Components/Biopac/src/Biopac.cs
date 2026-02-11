// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Biopac
{
    using BiopacInterop;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Biopac communicator component class.
    /// </summary>
    public class Biopac : ISourceComponent, IProducer<int>
    {
        private BiopacCommunicatorWrapper communicator;

        private Thread? captureThread = null;
        private bool shutdown = false;
        private bool isSynchOnly;
        private readonly Pipeline pipelineLocal;
        private string name;

        /// <summary>
        /// Gets emitter that encapsulates the string output stream.
        /// </summary>
        public Emitter<string> OutString { get; }

        /// <summary>
        /// Gets emitter that encapsulates the output stream.
        /// </summary>
        public Emitter<int> Out { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Biopac"/> class.
        /// <param name="syncOnly">Allow to select the communication mode with AcqKnowledge.
        /// If true  the componant will only start and stop the acquisition, at false it will collect data throught TCP</param>
        /// </summary>
        /// <param name="pipeline">The parent pipeline.</param>
        /// <param name="syncOnly">Boolean to select the communication mode with AcqKnowledge. If true, the component will only start and stop the acquisition; if false, it will collect data through TCP.</param>
        /// <param name="name">The name of the component.</param>
        public Biopac(Pipeline pipeline, bool syncOnly = false, string name = nameof(Biopac))
        {
            this.OutString = pipeline.CreateEmitter<string>(this, $"{name}-OutString");
            this.Out = pipeline.CreateEmitter<int>(this, $"{name}-Out");

            this.pipelineLocal = pipeline;
            this.name = name;

            this.communicator = new BiopacCommunicatorWrapper(syncOnly);

            // Application exit callback
            pipeline.ComponentCompleted += this.OnExitMethod;
            this.isSynchOnly = syncOnly;
            this.name = name;
        }

        private void Reset()
        {
            if (this.communicator.getAcquisitionInProgress() == 1)
            {
                if (this.communicator.toggleAcquisition() == 0)
                {
                    Console.WriteLine("XML-RPC SERVER: toggleAcquisition() SUCCEEDED" + "\n" + "....." + "acquisition_progress = off");
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Generates and time-stamps a data from Biopac.
        /// </summary>
        private void CaptureThreadProc()
        {
            while (!this.shutdown)
            {
                DateTime originatingTime = this.pipelineLocal.GetCurrentTime();
                int data = this.communicator.GetData();

                // string s = "Biopac";

                // No more data
                if (data == -1)
                {
                    continue;
                }

                this.Out.Post(data, originatingTime);
                this.OutString.Post(data.ToString(), originatingTime);
            }
        }

        /// <summary>
        /// Application exit method.
        /// </summary>
        private void OnExitMethod(object sender, EventArgs e)
        {
            this.Reset();
        }

        /// <summary>
        /// Start implementation.
        /// </summary>
        /// <param name="notifyCompletionTime">Delegate to notify completion time.</param>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.communicator.StartSyncedCommunication();

            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);
            if (!this.isSynchOnly)
            {
                this.captureThread = new Thread(new ThreadStart(this.CaptureThreadProc));
                this.captureThread.Start();
            }
        }

        /// <summary>
        /// Stop implementation.
        /// </summary>
        /// <param name="finalOriginatingTime">The final originating time.</param>
        /// <param name="notifyCompleted">Delegate to notify completion.</param>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.Reset();
            this.shutdown = true;
            TimeSpan waitTime = TimeSpan.FromSeconds(1);
            if (this.captureThread != null && this.captureThread.Join(waitTime) != true)
            {
                this.captureThread.Abort();
            }

            notifyCompleted();
        }
    }
}
