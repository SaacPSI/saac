using Microsoft.Psi;
using Microsoft.Psi.Audio;

namespace SAAC.AudioRecording
{
    public class AudioSplitter : IConsumer<AudioBuffer>
    {
        public Receiver<AudioBuffer> In { get; private set; }
        public int NbrChannels { get; private set; }
        public Emitter<AudioBuffer>[] Audios { get; private set; }
        public string Name { get; private set; }

        public AudioSplitter(Pipeline pipeline, string microphoneName, int nbrChannels)
        {
            this.Name = $"{microphoneName}_AudioSplitter";
            this.NbrChannels = nbrChannels;
            Audios = new Emitter<AudioBuffer>[this.NbrChannels];
            for (int i = 0; i < this.NbrChannels; i++)
                Audios[i] = pipeline.CreateEmitter<AudioBuffer>(this, $"{Name}-Out{i}");

            In = pipeline.CreateReceiver<AudioBuffer>(this, Receive, $"{Name}-In");
        }

        public override string ToString() => Name;

        protected void Receive(AudioBuffer audioBuffer2, Envelope e)
        {
            var audioBuffer = audioBuffer2.DeepClone();
            var nbrblock = (int)audioBuffer.Format.BlockAlign / audioBuffer.Format.Channels;

            var nbrBits = audioBuffer.Length;
            var bts = new byte[NbrChannels][];
            for (int k = 0; k < NbrChannels; k++)
            {
                bts[k] = new byte[nbrBits / NbrChannels];
            }
            var init = 0;
            for (int i = 0; i < nbrBits; i = i + NbrChannels * nbrblock)
            {
                for (int j = 0; j < nbrblock; j++)
                {
                    for (int k = 0; k < NbrChannels; k++)
                    {
                        bts[k][init] = audioBuffer.Data[i + k * nbrblock + j];
                    }
                    init++;
                }
            }
            for (int i = 0; i < NbrChannels; i++)
            {
                var audio = new AudioBuffer(bts[i], WaveFormat.CreatePcm((int)audioBuffer.Format.SamplesPerSec, audioBuffer.Format.BitsPerSample, 1));
                Audios[i].Post(audio, e.OriginatingTime);
            }
        }
    }
}
