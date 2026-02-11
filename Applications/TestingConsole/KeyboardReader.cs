// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using Microsoft.Psi;

namespace KeyboardReader
{
    public class KeyboardReader : Microsoft.Psi.Components.ISourceComponent, IProducer<string>
    {
        public Emitter<string> Out { get; private set; }

        private Thread? captureThread = null;
        private bool shutdown = false;

        public KeyboardReader(Pipeline pipeline)
        {
            this.Out = pipeline.CreateEmitter<string>(this, nameof(this.Out));
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);
            this.captureThread = new Thread(new ThreadStart(this.CaptureThreadProc));
            this.captureThread.Start();
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.shutdown = true;
            TimeSpan waitTime = TimeSpan.FromSeconds(1);
            if (this.captureThread != null && this.captureThread.Join(waitTime) != true)
            {
#pragma warning disable SYSLIB0006 // Le type ou le membre est obsolète
                this.captureThread.Abort();
#pragma warning restore SYSLIB0006 // Le type ou le membre est obsolète
            }

            notifyCompleted();
        }

        private void CaptureThreadProc()
        {
            Console.WriteLine(">");
            while (!this.shutdown)
            {
                var message = Console.ReadLine();
                if (message != null)
                {
                    try
                    {
                        this.Out.Post(message, this.Out.Pipeline.GetCurrentTime());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }
    }
}
