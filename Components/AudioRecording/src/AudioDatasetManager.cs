// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.AudioRecording
{
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Component that manages audio sources from PSI datasets.
    /// </summary>
    public class AudioDatasetManager : IAudioSourcesManager
    {
        /// <summary>
        /// Gets the dictionary mapping stream names to audio buffer producers from the dataset.
        /// </summary>
        public Dictionary<string, IProducer<AudioBuffer>> DatasetAudioStreamsDictionnary { get; private set; }

        /// <summary>
        /// Gets or sets the list of PSI importers.
        /// </summary>
        protected List<PsiImporter> psiImporters;

        private readonly Pipeline pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioDatasetManager"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to which this component belongs.</param>
        public AudioDatasetManager(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            this.DatasetAudioStreamsDictionnary = new Dictionary<string, IProducer<AudioBuffer>>();
            this.psiImporters = new List<PsiImporter>();
        }

        /// <summary>
        /// Stops all PSI importers associated with this manager.
        /// </summary>
        public void Stop()
        {
            foreach (var psiImporter in this.psiImporters)
            {
                psiImporter.Stop(this.pipeline.GetCurrentTime(), () => { });
            }
        }

        /// <inheritdoc/>
        public Dictionary<string, IProducer<AudioBuffer>> GetDictonaryIdAudioStream()
        {
            return this.DatasetAudioStreamsDictionnary;
        }

        /// <summary>
        /// Opens all audio streams from a dataset at the specified path.
        /// </summary>
        /// <param name="audioSourceDatasetPath">The path to the dataset.</param>
        /// <param name="audioSourceSessionName">Optional session name to filter streams.</param>
        public void OpenAllAudioStreamsFromDataset(string audioSourceDatasetPath, string? audioSourceSessionName = null)
        {
            this.OpenAllAudioStreamsFromDataset(Dataset.Load(audioSourceDatasetPath), audioSourceSessionName);
        }

        /// <summary>
        /// Opens all audio streams from the specified dataset.
        /// </summary>
        /// <param name="dataset">The dataset to open streams from.</param>
        /// <param name="audioSourceSessionName">Optional session name to filter streams.</param>
        public void OpenAllAudioStreamsFromDataset(Dataset dataset, string? audioSourceSessionName = null)
        {
            foreach (Session session in dataset.Sessions)
            {
                if (audioSourceSessionName != null && session.Name != audioSourceSessionName)
                {
                    continue;
                }

                foreach (var partition in session.Partitions)
                {
                    foreach (var streamMetadata in partition.AvailableStreams)
                    {
                        if (typeof(AudioBuffer) != Type.GetType(streamMetadata.TypeName))
                        {
                            continue;
                        }

                        this.DatasetAudioStreamsDictionnary.Add(streamMetadata.Name, PsiStore.Open(this.pipeline, streamMetadata.StoreName, streamMetadata.StorePath).OpenStream<AudioBuffer>(streamMetadata.Name));
                    }
                }
            }
        }

        /// <summary>
        /// Opens specified audio streams from a dataset at the specified path.
        /// </summary>
        /// <param name="audioSourceDatasetPath">The path to the dataset.</param>
        /// <param name="streams">The list of stream names to open.</param>
        /// <param name="audioSourceSessionName">Optional session name to filter streams.</param>
        public void OpenAudioStreamsFromDataset(string audioSourceDatasetPath, List<string> streams, string? audioSourceSessionName = null)
        {
            this.OpenAudioStreamsFromDataset(Dataset.Load(audioSourceDatasetPath), streams, audioSourceSessionName);
        }

        /// <summary>
        /// Opens specified audio streams from the specified dataset.
        /// </summary>
        /// <param name="dataset">The dataset to open streams from.</param>
        /// <param name="streams">The list of stream names to open.</param>
        /// <param name="audioSourceSessionName">Optional session name to filter streams.</param>
        public void OpenAudioStreamsFromDataset(Dataset dataset, List<string> streams, string? audioSourceSessionName = null)
        {
            foreach (Session session in dataset.Sessions)
            {
                if (audioSourceSessionName != null && session.Name != audioSourceSessionName)
                {
                    continue;
                }

                foreach (var partition in session.Partitions)
                {
                    foreach (var streamMetadata in partition.AvailableStreams)
                    {
                        if (typeof(AudioBuffer) != Type.GetType(streamMetadata.TypeName) || !streams.Contains(streamMetadata.Name) || this.DatasetAudioStreamsDictionnary.ContainsKey(streamMetadata.Name))
                        {
                            continue;
                        }

                        this.DatasetAudioStreamsDictionnary.Add(streamMetadata.Name, PsiStore.Open(this.pipeline, streamMetadata.StoreName, streamMetadata.StorePath).OpenStream<AudioBuffer>(streamMetadata.Name));
                    }
                }
            }
        }
    }
}
