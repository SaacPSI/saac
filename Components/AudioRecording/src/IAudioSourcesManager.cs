using Microsoft.Psi;
using Microsoft.Psi.Audio;

namespace SAAC.AudioRecording
{
    public interface IAudioSourcesManager
    {
        public Dictionary<string, IProducer<AudioBuffer>> GetDictonaryIdAudioStream();
        public void Stop();
    }
}
