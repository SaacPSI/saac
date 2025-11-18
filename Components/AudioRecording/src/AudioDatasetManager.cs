using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Data;

namespace SAAC.AudioRecording
{
    public class AudioDatasetManager : IAudioSourcesManager
    {
        public Dictionary<string, IProducer<AudioBuffer>> DatasetAudioStreamsDictionnary { get; private set; }

        protected List<PsiImporter> psiImporters;
        private Pipeline pipeline;

        public AudioDatasetManager(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            DatasetAudioStreamsDictionnary = new Dictionary<string, IProducer<AudioBuffer>>();
            psiImporters = new List<PsiImporter>();
        }
        public void Stop()
        {
            foreach (var psiImporter in psiImporters)
                psiImporter.Stop(pipeline.GetCurrentTime(), () => { });
        }

        public Dictionary<string, IProducer<AudioBuffer>> GetDictonaryIdAudioStream()
        {
            return DatasetAudioStreamsDictionnary;
        }

        public void OpenAllAudioStreamsFromDataset(string AudioSourceDatasetPath, string? audioSourceSessionName = null)
        {
            OpenAllAudioStreamsFromDataset(Dataset.Load(AudioSourceDatasetPath), audioSourceSessionName);
        }

        public void OpenAllAudioStreamsFromDataset(Dataset dataset, string? audioSourceSessionName = null)
        {
            foreach (Session session in dataset.Sessions)
            {
                if (audioSourceSessionName != null && session.Name != audioSourceSessionName)
                    continue;
                foreach (var partition in session.Partitions)
                {
                    foreach (var streamMetadata in partition.AvailableStreams)
                    {
                        if (typeof(AudioBuffer) != Type.GetType(streamMetadata.TypeName))
                            continue;
                        DatasetAudioStreamsDictionnary.Add(streamMetadata.Name, PsiStore.Open(pipeline, streamMetadata.StoreName, streamMetadata.StorePath).OpenStream<AudioBuffer>(streamMetadata.Name));
                    }
                }
            }
        }

        public void OpenAudioStreamsFromDataset(string AudioSourceDatasetPath, List<string> streams, string? audioSourceSessionName = null)
        {
            OpenAudioStreamsFromDataset(Dataset.Load(AudioSourceDatasetPath), streams, audioSourceSessionName);
        }

        public void OpenAudioStreamsFromDataset(Dataset dataset, List<string> streams, string? audioSourceSessionName = null)
        {
            foreach (Session session in dataset.Sessions)
            {
                if (audioSourceSessionName != null && session.Name != audioSourceSessionName)
                    continue;
                foreach (var partition in session.Partitions)
                {
                    foreach (var streamMetadata in partition.AvailableStreams)
                    {
                        if (typeof(AudioBuffer) != Type.GetType(streamMetadata.TypeName) || !streams.Contains(streamMetadata.Name) || DatasetAudioStreamsDictionnary.ContainsKey(streamMetadata.Name))
                            continue;
                        DatasetAudioStreamsDictionnary.Add(streamMetadata.Name, PsiStore.Open(pipeline, streamMetadata.StoreName, streamMetadata.StorePath).OpenStream<AudioBuffer>(streamMetadata.Name));
                    }
                }
            }
        }
    }
}
