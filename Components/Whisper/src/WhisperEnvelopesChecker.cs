using Microsoft.Psi.Audio;
using Microsoft.Psi.Speech;
using Microsoft.Psi;

namespace SAAC.Whisper
{
    public class WhisperEnvelopesChecker
    {
        public Receiver<bool> vadIn { get; private set; }
        public Receiver<AudioBuffer> audioIn { get; private set; }
        public Receiver<IStreamingSpeechRecognitionResult> sttIn { get; private set; }
        public Emitter<bool> vadOut { get; private set; }
        public Emitter<AudioBuffer> audioOut { get; private set; }
        public Emitter<IStreamingSpeechRecognitionResult> sttOut { get; private set; }
        public string Name { get; private set; }

        private DateTime lastVadOut = DateTime.MinValue;
        private DateTime lastAudioOut = DateTime.MinValue;
        private DateTime lastSttOut = DateTime.MinValue;

        public WhisperEnvelopesChecker(Pipeline pipeline, string name = nameof(WhisperEnvelopesChecker))
        {
            Name = name;
            this.vadIn = pipeline.CreateReceiver<bool>(this, Process, $"{Name}-{nameof(this.vadIn)}");
            this.audioIn = pipeline.CreateReceiver<AudioBuffer>(this, Process, $"{Name}-{nameof(this.audioIn)}");
            this.sttIn = pipeline.CreateReceiver<IStreamingSpeechRecognitionResult>(this, Process, $"{Name}-{nameof(this.sttIn)}");
            this.vadOut = pipeline.CreateEmitter<bool>(this, $"{Name}-{nameof(this.vadOut)}");
            this.audioOut = pipeline.CreateEmitter<AudioBuffer>(this, $"{Name}-{nameof(this.audioOut)}");
            this.sttOut = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, $"{Name}-{nameof(this.sttOut)}");
        }

        public override string ToString() => Name;

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
    }
}
