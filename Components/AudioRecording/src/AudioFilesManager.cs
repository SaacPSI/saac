using Microsoft.Psi;
using Microsoft.Psi.Audio;
using System.IO;

namespace SAAC.AudioRecording
{
    public class AudioFilesManager : IAudioSourcesManager
    {
        public Dictionary<string, IProducer<AudioBuffer>> FilesAudioStreamDictionnary { get; private set; }
    
        protected List<WaveFileImporter> waveFileImporters;

        private Pipeline p;
        public AudioFilesManager(Pipeline pipeline )
        {
            this.p = pipeline;
            FilesAudioStreamDictionnary = new Dictionary<string, IProducer<AudioBuffer>>();
            waveFileImporters = new List<WaveFileImporter>();
        }
        public void Stop()
        {
            foreach (var waveFileImporter in waveFileImporters)
                waveFileImporter.Stop(p.GetCurrentTime(), () => { });
        }

        public Dictionary<string, IProducer<AudioBuffer>> GetDictonaryIdAudioStream()
        {
            return FilesAudioStreamDictionnary;
        }

        public void SetupAudioFromFiles(List<string> filesList)
        {
            foreach (string file in filesList)
                SetupAudioFromFile(file);
        }

        public void SetupAudioFromFile(string file)
        {
            WaveFileImporter audioStream = new WaveFileImporter(p, Path.GetFileName(file), Path.GetDirectoryName(file), UnixSecondsToDateTime(1724332525, false));
            waveFileImporters.Add(audioStream);
            IProducer<AudioBuffer> audio = audioStream.OpenStream<AudioBuffer>("Audio");
            int channelCount = audioStream.GetWaveFileChannelCount();
            if (channelCount > 1)
            {
                AudioSplitter splitter = new AudioSplitter(p, Path.GetFileName(file), channelCount);
                audio.PipeTo(splitter);
                foreach(Emitter<AudioBuffer> audioChannel in splitter.Audios)
                {
                    AudioResampler resampler = new AudioResampler(p, new AudioResamplerConfiguration()
                    {
                        OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm()
                    });
                    audioChannel.PipeTo(resampler);
                    FilesAudioStreamDictionnary.Add(Path.GetFileNameWithoutExtension(file) + $"_{Array.IndexOf(splitter.Audios, audioChannel)+1}", resampler.Out);
                }
            }
            else
            {
                AudioResampler resampler = new AudioResampler(p, new AudioResamplerConfiguration()
                {
                    OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm()
                });
                audio.PipeTo(resampler);
                FilesAudioStreamDictionnary.Add(Path.GetFileNameWithoutExtension(file), resampler.Out);
            }
        }

        public static DateTime UnixSecondsToDateTime(long timestamp, bool local = false)
        {
            var offset = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            return local ? offset.LocalDateTime : offset.UtcDateTime;
        }
    }
}
