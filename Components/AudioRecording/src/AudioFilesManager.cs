// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.AudioRecording
{
    using System.IO;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;

    /// <summary>
    /// Component that manages audio sources from WAV files.
    /// </summary>
    public class AudioFilesManager : IAudioSourcesManager
    {
        /// <summary>
        /// Gets the dictionary mapping file names to audio buffer producers.
        /// </summary>
        public Dictionary<string, IProducer<AudioBuffer>> FilesAudioStreamDictionnary { get; private set; }

        /// <summary>
        /// Gets or sets the list of wave file importers.
        /// </summary>
        protected List<WaveFileImporter> waveFileImporters;

        private readonly Pipeline p;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioFilesManager"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to which this component belongs.</param>
        public AudioFilesManager(Pipeline pipeline)
        {
            this.p = pipeline;
            this.FilesAudioStreamDictionnary = new Dictionary<string, IProducer<AudioBuffer>>();
            this.waveFileImporters = new List<WaveFileImporter>();
        }

        /// <summary>
        /// Stops all wave file importers associated with this manager.
        /// </summary>
        public void Stop()
        {
            foreach (var waveFileImporter in this.waveFileImporters)
            {
                waveFileImporter.Stop(this.p.GetCurrentTime(), () => { });
            }
        }

        /// <inheritdoc/>
        public Dictionary<string, IProducer<AudioBuffer>> GetDictonaryIdAudioStream()
        {
            return this.FilesAudioStreamDictionnary;
        }

        /// <summary>
        /// Sets up audio sources from a list of WAV files.
        /// </summary>
        /// <param name="filesList">The list of file paths.</param>
        public void SetupAudioFromFiles(List<string> filesList)
        {
            foreach (string file in filesList)
            {
                this.SetupAudioFromFile(file);
            }
        }

        /// <summary>
        /// Sets up audio source from a single WAV file.
        /// </summary>
        /// <param name="file">The file path.</param>
        public void SetupAudioFromFile(string file)
        {
            WaveFileImporter audioStream = new WaveFileImporter(this.p, Path.GetFileName(file), Path.GetDirectoryName(file), UnixSecondsToDateTime(1724332525, false));
            this.waveFileImporters.Add(audioStream);
            IProducer<AudioBuffer> audio = audioStream.OpenStream<AudioBuffer>("Audio");
            int channelCount = audioStream.GetWaveFileChannelCount();
            if (channelCount > 1)
            {
                AudioSplitter splitter = new AudioSplitter(this.p, Path.GetFileName(file), channelCount);
                audio.PipeTo(splitter);
                foreach (Emitter<AudioBuffer> audioChannel in splitter.Audios)
                {
                    AudioResampler resampler = new AudioResampler(this.p, new AudioResamplerConfiguration()
                    {
                        OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm()
                    });
                    audioChannel.PipeTo(resampler);
                    this.FilesAudioStreamDictionnary.Add(Path.GetFileNameWithoutExtension(file) + $"_{Array.IndexOf(splitter.Audios, audioChannel) + 1}", resampler.Out);
                }
            }
            else
            {
                AudioResampler resampler = new AudioResampler(this.p, new AudioResamplerConfiguration()
                {
                    OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm()
                });
                audio.PipeTo(resampler);
                this.FilesAudioStreamDictionnary.Add(Path.GetFileNameWithoutExtension(file), resampler.Out);
            }
        }

        /// <summary>
        /// Converts Unix timestamp (in seconds) to a DateTime object.
        /// </summary>
        /// <param name="timestamp">The Unix timestamp in seconds.</param>
        /// <param name="local">Whether to return local time (true) or UTC (false).</param>
        /// <returns>The corresponding DateTime object.</returns>
        public static DateTime UnixSecondsToDateTime(long timestamp, bool local = false)
        {
            var offset = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            return local ? offset.LocalDateTime : offset.UtcDateTime;
        }
    }
}
