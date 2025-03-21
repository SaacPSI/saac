using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Speech;
using Microsoft.Psi;

namespace WhisperRemoteApp
{
    public class WhisperAudioProcessing
    {
        private DateTime lastVadOut = DateTime.MinValue;
        private DateTime lastAudioOut = DateTime.MinValue;
        private DateTime lastSttOut = DateTime.MinValue;
        public WhisperAudioProcessing(Pipeline pipeline)
        {
            this.vadIn = pipeline.CreateReceiver<bool>(this, Process, nameof(this.vadIn));
            this.audioIn = pipeline.CreateReceiver<AudioBuffer>(this, Process, nameof(this.audioIn));
            this.sttIn = pipeline.CreateReceiver<IStreamingSpeechRecognitionResult>(this, Process, nameof(this.sttIn));
            this.vadOut = pipeline.CreateEmitter<bool>(this, nameof(this.vadOut));
            this.audioOut = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.audioOut));
            this.sttOut = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, nameof(this.sttOut));
        }

        private void Process(bool value, Envelope envelope)
        {
            if (envelope.OriginatingTime > lastVadOut)
            {
                vadOut.Post(value, envelope.OriginatingTime);
                lastVadOut = envelope.OriginatingTime;
            }
        }

        private void Process(AudioBuffer buffer, Envelope envelope)
        {
            if (envelope.OriginatingTime > lastAudioOut)
            {
                audioOut.Post(buffer, envelope.OriginatingTime);
                lastAudioOut = envelope.OriginatingTime;
            }
        }
        private void Process(IStreamingSpeechRecognitionResult finalResult, Envelope envelope)
        {
            if (envelope.OriginatingTime > lastSttOut)
            {
                sttOut.Post(finalResult, envelope.OriginatingTime);
                lastSttOut = envelope.OriginatingTime;
            }
        }

        public Receiver<bool> vadIn { get; private set; }
        public Receiver<AudioBuffer> audioIn { get; private set; }
        public Receiver<IStreamingSpeechRecognitionResult> sttIn { get; private set; }
        public Emitter<bool> vadOut { get; private set; }
        public Emitter<AudioBuffer> audioOut { get; private set; }
        public Emitter<IStreamingSpeechRecognitionResult> sttOut { get; private set; }
    }
}
