using Microsoft.Psi;
using System.Threading;
using System;


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
            shutdown = true;
            TimeSpan waitTime = TimeSpan.FromSeconds(1);
            if (this.captureThread != null && this.captureThread.Join(waitTime) != true)
            {
#pragma warning disable SYSLIB0006 // Le type ou le membre est obsolète
                captureThread.Abort();
#pragma warning restore SYSLIB0006 // Le type ou le membre est obsolète
            }
            notifyCompleted();
        }

        private void CaptureThreadProc()
        {
            while (!this.shutdown)
            {
                Console.WriteLine("Ready to send text!");
                var message = Console.ReadLine();
                if (message != null)
                {
                    try
                    {
                        Out.Post(message, DateTime.UtcNow);
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
