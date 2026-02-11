// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.AudioRecording
{
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;

    /// <summary>
    /// Component that splits a multi-channel audio buffer into separate single-channel buffers.
    /// </summary>
    public class AudioSplitter : IConsumer<AudioBuffer>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioSplitter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="microphoneName">The microphone name.</param>
        /// <param name="nbrChannels">The number of channels.</param>
        public AudioSplitter(Pipeline pipeline, string microphoneName, int nbrChannels)
        {
            this.Name = $"{microphoneName}_AudioSplitter";
            this.NbrChannels = nbrChannels;
            this.Audios = new Emitter<AudioBuffer>[this.NbrChannels];
            for (int i = 0; i < this.NbrChannels; i++)
            {
                this.Audios[i] = pipeline.CreateEmitter<AudioBuffer>(this, $"{this.Name}-Out{i}");
            }

            this.In = pipeline.CreateReceiver<AudioBuffer>(this, this.Receive, $"{this.Name}-In");
        }

        /// <summary>
        /// Gets the receiver for audio input.
        /// </summary>
        public Receiver<AudioBuffer> In { get; private set; }

        /// <summary>
        /// Gets the number of channels.
        /// </summary>
        public int NbrChannels { get; private set; }

        /// <summary>
        /// Gets the array of audio output emitters.
        /// </summary>
        public Emitter<AudioBuffer>[] Audios { get; private set; }

        /// <summary>
        /// Gets the name of this component.
        /// </summary>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public override string ToString() => this.Name;

        /// <summary>
        /// Processes incoming audio buffer and splits it into separate channels.
        /// </summary>
        /// <param name="audioBuffer2">The incoming audio buffer.</param>
        /// <param name="e">The envelope.</param>
        protected void Receive(AudioBuffer audioBuffer2, Envelope e)
        {
            var audioBuffer = audioBuffer2.DeepClone();
            var nbrblock = (int)audioBuffer.Format.BlockAlign / audioBuffer.Format.Channels;

            var nbrBits = audioBuffer.Length;
            var bts = new byte[this.NbrChannels][];
            for (int k = 0; k < this.NbrChannels; k++)
            {
                bts[k] = new byte[nbrBits / this.NbrChannels];
            }

            var init = 0;
            for (int i = 0; i < nbrBits; i = i + (this.NbrChannels * nbrblock))
            {
                for (int j = 0; j < nbrblock; j++)
                {
                    for (int k = 0; k < this.NbrChannels; k++)
                    {
                        bts[k][init] = audioBuffer.Data[i + (k * nbrblock) + j];
                    }

                    init++;
                }
            }

            for (int i = 0; i < this.NbrChannels; i++)
            {
                var audio = new AudioBuffer(bts[i], WaveFormat.CreatePcm((int)audioBuffer.Format.SamplesPerSec, audioBuffer.Format.BitsPerSample, 1));
                this.Audios[i].Post(audio, e.OriginatingTime);
            }
        }
    }
}
