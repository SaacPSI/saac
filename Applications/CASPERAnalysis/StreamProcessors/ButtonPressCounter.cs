using System;
using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace CASPERAnalysis.StreamProcessors
{
    /// <summary>
    /// Counts consecutive button presses and resets on different actions
    /// </summary>
    public class ButtonPressCounter : ConsumerProducer<bool, int>
    {
        private int consecutivePresses = 0;
        private DateTime? lastPressTime;
        private readonly TimeSpan resetWindow; // Time window to reset counter

        public ButtonPressCounter(Pipeline pipeline, TimeSpan resetWindow)
            : base(pipeline)
        {
            this.resetWindow = resetWindow;
        }

        protected override void Receive(bool isPressed, Envelope envelope)
        {
            if (isPressed)
            {
                // Check if we should reset (too much time passed)
                if (lastPressTime.HasValue && 
                    (envelope.OriginatingTime - lastPressTime.Value) > resetWindow)
                {
                    consecutivePresses = 1;
                }
                else
                {
                    consecutivePresses++;
                }
                
                lastPressTime = envelope.OriginatingTime;
            }
            else
            {
                // Reset on release (optional - depends on your button stream behavior)
                // consecutivePresses = 0;
            }

            Out.Post(consecutivePresses, envelope.OriginatingTime);
        }

        public void Reset()
        {
            consecutivePresses = 0;
            lastPressTime = null;
        }
    }
}

