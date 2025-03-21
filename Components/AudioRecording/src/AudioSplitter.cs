using Microsoft.Psi;
using Microsoft.Psi.Audio;

namespace SAAC.AudioRecording
{
    public class AudioSplitter : IConsumer<AudioBuffer>
    {
        public Receiver<AudioBuffer> In { get; }
        public int nbrChannels;
        public Emitter<AudioBuffer>[] Audios { get; private set; }

        public AudioSplitter(Pipeline pipeline, int nbrChannels)
        {
            this.nbrChannels = nbrChannels;
            Audios = new Emitter<AudioBuffer>[this.nbrChannels];
            for (int i = 0; i < this.nbrChannels; i++)
            {
                Audios[i] = pipeline.CreateEmitter<AudioBuffer>(this, "Audio_" + i);
            }
            In = pipeline.CreateReceiver<AudioBuffer>(this, Receive, "In");
        }

        protected void Receive(AudioBuffer audioBuffer2, Envelope e)
        {
            var audioBuffer = audioBuffer2.DeepClone();
            var nbrblock = (int)audioBuffer.Format.BlockAlign / audioBuffer.Format.Channels;

            var nbrBits = audioBuffer.Length;
            var bts = new byte[nbrChannels][];
            for (int k = 0; k < nbrChannels; k++)
            {
                bts[k] = new byte[nbrBits / nbrChannels];
            }
            var init = 0;
            for (int i = 0; i < nbrBits; i = i + nbrChannels * nbrblock)
            {
                for (int j = 0; j < nbrblock; j++)
                {
                    for (int k = 0; k < nbrChannels; k++)
                    {
                        bts[k][init] = audioBuffer.Data[i + k * nbrblock + j];
                    }
                    init++;
                }
            }
            for (int i = 0; i < nbrChannels; i++)
            {
                var audio = new AudioBuffer(bts[i], WaveFormat.CreatePcm((int)audioBuffer.Format.SamplesPerSec, audioBuffer.Format.BitsPerSample, 1));
                Audios[i].Post(audio, e.OriginatingTime);
            }
        }
    }
}
