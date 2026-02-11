// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Whisper
{
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Checks and validates envelopes for Whisper speech recognition pipeline.
    /// </summary>
    public class WhisperEnvelopesChecker
    {
        private DateTime lastVadOut = DateTime.MinValue;
        private DateTime lastAudioOut = DateTime.MinValue;
        private DateTime lastSttOut = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="WhisperEnvelopesChecker"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="name">The component name.</param>
        public WhisperEnvelopesChecker(Pipeline pipeline, string name = nameof(WhisperEnvelopesChecker))
        {
            this.Name = name;
            this.VadIn = pipeline.CreateReceiver<bool>(this, this.Process, $"{this.Name}-{nameof(this.VadIn)}");
            this.AudioIn = pipeline.CreateReceiver<AudioBuffer>(this, this.Process, $"{this.Name}-{nameof(this.AudioIn)}");
            this.SttIn = pipeline.CreateReceiver<IStreamingSpeechRecognitionResult>(this, this.Process, $"{this.Name}-{nameof(this.SttIn)}");
            this.VadOut = pipeline.CreateEmitter<bool>(this, $"{this.Name}-{nameof(this.VadOut)}");
            this.AudioOut = pipeline.CreateEmitter<AudioBuffer>(this, $"{this.Name}-{nameof(this.AudioOut)}");
            this.SttOut = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, $"{this.Name}-{nameof(this.SttOut)}");
        }

        /// <summary>
        /// Gets the VAD input receiver.
        /// </summary>
        public Receiver<bool> VadIn { get; private set; }

        /// <summary>
        /// Gets the audio input receiver.
        /// </summary>
        public Receiver<AudioBuffer> AudioIn { get; private set; }

        /// <summary>
        /// Gets the STT input receiver.
        /// </summary>
        public Receiver<IStreamingSpeechRecognitionResult> SttIn { get; private set; }

        /// <summary>
        /// Gets the VAD output emitter.
        /// </summary>
        public Emitter<bool> VadOut { get; private set; }

        /// <summary>
        /// Gets the audio output emitter.
        /// </summary>
        public Emitter<AudioBuffer> AudioOut { get; private set; }

        /// <summary>
        /// Gets the STT output emitter.
        /// </summary>
        public Emitter<IStreamingSpeechRecognitionResult> SttOut { get; private set; }

        /// <summary>
        /// Gets the component name.
        /// </summary>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public override string ToString() => this.Name;

        private void Process(bool value, Envelope envelope)
        {
            if (envelope.OriginatingTime > this.lastVadOut)
            {
                this.VadOut.Post(value, envelope.OriginatingTime);
                this.lastVadOut = envelope.OriginatingTime;
            }
        }

        private void Process(AudioBuffer buffer, Envelope envelope)
        {
            if (envelope.OriginatingTime > this.lastAudioOut)
            {
                this.AudioOut.Post(buffer, envelope.OriginatingTime);
                this.lastAudioOut = envelope.OriginatingTime;
            }
        }

        private void Process(IStreamingSpeechRecognitionResult finalResult, Envelope envelope)
        {
            if (envelope.OriginatingTime > this.lastSttOut)
            {
                this.SttOut.Post(finalResult, envelope.OriginatingTime);
                this.lastSttOut = envelope.OriginatingTime;
            }
        }
    }
}
