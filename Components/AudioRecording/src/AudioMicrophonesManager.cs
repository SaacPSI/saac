using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Data;
using SAAC.PipelineServices;

namespace SAAC.AudioRecording
{
    public class AudioMicrophonesManager : IAudioSourcesManager,  IDisposable
    {
        public static List<(string, int)> AvailableMicrophones { get; private set; } = AudioCapture.GetAvailableDevicesWithChannels().ToList();

        public bool _isMicrophoneInit = false;
        public PsiExporter? AudioStore { get; private set; }
        public Dictionary<string, User> Users { get; private set; }
        public bool LocalStore { get; private set; }
        public Dictionary<User, IProducer<AudioBuffer>> UserAudioStreamDictionnary { get; private set; }
        protected Dictionary<string, AudioCapture> mics;
        protected Dictionary<string, AudioSplitter> splitter;
        protected Pipeline pipeline;

        public AudioMicrophonesManager(Pipeline p, bool storeLocally = false)
        {
            this.pipeline = p;
            LocalStore = storeLocally;
            AudioStore = null;
            mics = new Dictionary<string, AudioCapture>();
            splitter = new Dictionary<string, AudioSplitter>();
            Users = new Dictionary<string, User>();
            UserAudioStreamDictionnary = new Dictionary<User, IProducer<AudioBuffer>>();
        }

        public void Stop()
        {
            foreach (var mic in mics)
                mic.Value.Stop(pipeline.GetCurrentTime(), () => { });
        }

        public static List<(string, int)> RefreshAvailableMicrophones()
        {
           return (AvailableMicrophones = AudioCapture.GetAvailableDevicesWithChannels().ToList());
        }

        public Dictionary<string, IProducer<AudioBuffer>> GetDictonaryIdAudioStream()
        {
            return UserAudioStreamDictionnary.ToDictionary(data => data.Key.Id, data => data.Value);
        }

        public bool AddUser(string id, string microphone, int channel)
        {
            return AddUser(new User(id, microphone, channel));
        }

        public bool AddUser(User user)
        {
            if (Users.ContainsKey(user.Id))
                return false;
            Users.Add(user.Id, user);
            return true;
        }

        public bool AddUsers(List<User> users)
        {
            foreach (User user in users)
            {
                if (!AddUser(user))
                    return false;
            }
            return true;
        }

        public bool SetupAudio(RendezVousPipeline server, string sessionName)
        {
            InternalSetupAudio();
            bool result = true;
            foreach (var user in Users)
                result &= RegisterAudioSourceWithUser(server, sessionName, user.Value);
            return result;
        }

        public bool SetupAudioWithoutRDV(Session session, string path)
        {
            AudioStore = PsiStore.Create(pipeline, "Audio", $"{path}/{session.Name}/");
            session.AddPartitionFromPsiStoreAsync("Audio", $"{path}/{session.Name}/");
            return SetupAudioWithoutRDV();
        }

        public bool SetupAudioWithoutRDV()
        {
            InternalSetupAudio();
            bool result = true;
            foreach (var user in Users)
                result &= RegisterAudioSourceWithUser(user.Value);
            return result;
        }

        public void Dispose()
        {
            mics.Clear();
            splitter.Clear();
            AudioStore?.Dispose();
        }

        protected void InternalSetupAudio()
        {
            // Initialize the AudioCapture / Splitters if needed.
            foreach (var user in Users)
            {
                if (!mics.ContainsKey(user.Value.Microphone))
                    CreateAudioSources(user.Value.Microphone, (AvailableMicrophones.Where((nameChannel) => nameChannel.Item1 == user.Value.Microphone)).First().Item2);
            }
        }

        protected void CreateAudioSources(string microphoneName, int nbOfActiveChannel)
        {
            if (nbOfActiveChannel < 1)
                throw new Exception("Cannot instantiate microphone with 0 channel");

            AudioCapture mic = new AudioCapture(pipeline, new AudioCaptureConfiguration()
            {
                DeviceName = microphoneName,
                Format = WaveFormat.Create16BitPcm(16000, nbOfActiveChannel),
            });

            mics.Add(microphoneName, mic);
            if (nbOfActiveChannel > 1)
            {
                AudioSplitter splitAudio = new AudioSplitter(pipeline, microphoneName, nbOfActiveChannel);
                mic.PipeTo(splitAudio);
                splitter.Add(microphoneName, splitAudio);
            }
        }

        protected bool RegisterAudioSourceWithUser(RendezVousPipeline server, string sessionName, User user)
        {
            if (splitter.ContainsKey(user.Microphone))
            {
                if (splitter[user.Microphone].Audios.Count() < user.Channel)
                    throw new Exception("Incorrect channel number.");
                server.CreateConnectorAndStore("Audio", $"Audio_User_{user.Id}", server.CreateOrGetSessionFromMode(sessionName), pipeline, typeof(AudioBuffer), splitter[user.Microphone].Audios[user.Channel-1], LocalStore);
                UserAudioStreamDictionnary.Add(user, splitter[user.Microphone].Audios[user.Channel-1]);
            }
            else if (mics.ContainsKey(user.Microphone))
            { 
                server.CreateConnectorAndStore("Audio", $"Audio_User_{user.Id}", server.CreateOrGetSessionFromMode(sessionName), pipeline, typeof(AudioBuffer), mics[user.Microphone].Out, LocalStore);
                UserAudioStreamDictionnary.Add(user, mics[user.Microphone].Out);
            }
            else
                return false;
            return true;
        }

        protected bool RegisterAudioSourceWithUser(User user)
        {
            if (splitter.ContainsKey(user.Microphone))
            {
                if (splitter[user.Microphone].Audios.Count() < user.Channel)
                    throw new Exception("Incorrect channel number.");
                if (LocalStore)
                    splitter[user.Microphone].Audios[user.Channel].Write($"Audio_user{user.Id}", AudioStore);
                UserAudioStreamDictionnary.Add(user, splitter[user.Microphone].Audios[user.Channel-1]);
            }
            else if (mics.ContainsKey(user.Microphone))
            {
                if (LocalStore)
                    mics[user.Microphone].Out.Write($"Audio_user{user.Id}", AudioStore);
                UserAudioStreamDictionnary.Add(user, mics[user.Microphone].Out);
            }
            else
                return false;
            return true;
        }
    }
}
