// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.AudioRecording
{
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using SAAC.PipelineServices;

    /// <summary>
    /// Component that manages audio sources from physical microphone devices.
    /// </summary>
    public class AudioMicrophonesManager : IAudioSourcesManager, IDisposable
    {
        /// <summary>
        /// Gets the list of available microphones with their channel counts.
        /// </summary>
        public static List<(string, int)> AvailableMicrophones { get; private set; } = AudioCapture.GetAvailableDevicesWithChannels().ToList();

        /// <summary>
        /// Gets or sets a value indicating whether microphones have been initialized.
        /// </summary>
        public bool IsMicrophoneInit = false;

        /// <summary>
        /// Gets the PSI exporter for storing audio data.
        /// </summary>
        public PsiExporter? AudioStore { get; private set; }

        /// <summary>
        /// Gets the dictionary of users.
        /// </summary>
        public Dictionary<string, User> Users { get; private set; }

        /// <summary>
        /// Gets a value indicating whether audio is stored locally.
        /// </summary>
        public bool LocalStore { get; private set; }

        /// <summary>
        /// Gets the dictionary mapping users to their audio streams.
        /// </summary>
        public Dictionary<User, IProducer<AudioBuffer>> UserAudioStreamDictionnary { get; private set; }

        /// <summary>
        /// Gets or sets the dictionary of audio capture devices.
        /// </summary>
        protected Dictionary<string, AudioCapture> mics;

        /// <summary>
        /// Gets or sets the dictionary of audio splitters.
        /// </summary>
        protected Dictionary<string, AudioSplitter> splitter;

        /// <summary>
        /// Gets or sets the pipeline.
        /// </summary>
        protected Pipeline pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioMicrophonesManager"/> class.
        /// </summary>
        /// <param name="p">The pipeline to which this component belongs.</param>
        /// <param name="storeLocally">Whether to store audio locally.</param>
        public AudioMicrophonesManager(Pipeline p, bool storeLocally = false)
        {
            this.pipeline = p;
            this.LocalStore = storeLocally;
            this.AudioStore = null;
            this.mics = new Dictionary<string, AudioCapture>();
            this.splitter = new Dictionary<string, AudioSplitter>();
            this.Users = new Dictionary<string, User>();
            this.UserAudioStreamDictionnary = new Dictionary<User, IProducer<AudioBuffer>>();
        }

        /// <summary>
        /// Refreshes the list of available microphones.
        /// </summary>
        /// <returns>The updated list of available microphones with their channel counts.</returns>
        public static List<(string, int)> RefreshAvailableMicrophones()
        {
            return (AvailableMicrophones = AudioCapture.GetAvailableDevicesWithChannels().ToList());
        }

        /// <summary>
        /// Stops all microphone captures.
        /// </summary>
        public void Stop()
        {
            foreach (var mic in this.mics)
            {
                mic.Value.Stop(this.pipeline.GetCurrentTime(), () => { });
            }
        }

        /// <inheritdoc/>
        public Dictionary<string, IProducer<AudioBuffer>> GetDictonaryIdAudioStream()
        {
            return this.UserAudioStreamDictionnary.ToDictionary(data => data.Key.Id, data => data.Value);
        }

        /// <summary>
        /// Adds a user with the specified parameters.
        /// </summary>
        /// <param name="id">The user ID.</param>
        /// <param name="microphone">The microphone device name.</param>
        /// <param name="channel">The audio channel number.</param>
        /// <returns>True if the user was added successfully; otherwise false.</returns>
        public bool AddUser(string id, string microphone, int channel)
        {
            return this.AddUser(new User(id, microphone, channel));
        }

        /// <summary>
        /// Adds a user to the manager.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <returns>True if the user was added successfully; otherwise false.</returns>
        public bool AddUser(User user)
        {
            if (this.Users.ContainsKey(user.Id))
            {
                return false;
            }

            this.Users.Add(user.Id, user);
            return true;
        }

        /// <summary>
        /// Adds multiple users to the manager.
        /// </summary>
        /// <param name="users">The list of users to add.</param>
        /// <returns>True if all users were added successfully; otherwise false.</returns>
        public bool AddUsers(List<User> users)
        {
            foreach (User user in users)
            {
                if (!this.AddUser(user))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Sets up audio for all users with RendezVous pipeline support.
        /// </summary>
        /// <param name="server">The RendezVous pipeline server.</param>
        /// <param name="sessionName">The session name.</param>
        /// <returns>True if setup was successful; otherwise false.</returns>
        public bool SetupAudio(RendezVousPipeline server, string sessionName)
        {
            this.InternalSetupAudio();
            bool result = true;
            foreach (var user in this.Users)
            {
                result &= this.RegisterAudioSourceWithUser(server, sessionName, user.Value);
            }

            return result;
        }

        /// <summary>
        /// Sets up audio without RendezVous pipeline, storing to a session.
        /// </summary>
        /// <param name="session">The session to store audio data.</param>
        /// <param name="path">The storage path.</param>
        /// <returns>True if setup was successful; otherwise false.</returns>
        public bool SetupAudioWithoutRDV(Session session, string path)
        {
            this.AudioStore = PsiStore.Create(this.pipeline, "Audio", $"{path}/{session.Name}/");
            session.AddPartitionFromPsiStoreAsync("Audio", $"{path}/{session.Name}/");
            return this.SetupAudioWithoutRDV();
        }

        /// <summary>
        /// Sets up audio without RendezVous pipeline.
        /// </summary>
        /// <returns>True if setup was successful; otherwise false.</returns>
        public bool SetupAudioWithoutRDV()
        {
            this.InternalSetupAudio();
            bool result = true;
            foreach (var user in this.Users)
            {
                result &= this.RegisterAudioSourceWithUser(user.Value);
            }

            return result;
        }

        /// <summary>
        /// Disposes of managed resources.
        /// </summary>
        public void Dispose()
        {
            this.mics.Clear();
            this.splitter.Clear();
            this.AudioStore?.Dispose();
        }

        /// <summary>
        /// Initializes audio capture devices and splitters for all users.
        /// </summary>
        protected void InternalSetupAudio()
        {
            // Initialize the AudioCapture / Splitters if needed.
            foreach (var user in this.Users)
            {
                if (!this.mics.ContainsKey(user.Value.Microphone))
                {
                    this.CreateAudioSources(user.Value.Microphone, (AvailableMicrophones.Where((nameChannel) => nameChannel.Item1 == user.Value.Microphone)).First().Item2);
                }
            }
        }

        /// <summary>
        /// Creates audio sources for a specific microphone.
        /// </summary>
        /// <param name="microphoneName">The name of the microphone device.</param>
        /// <param name="nbOfActiveChannel">The number of active channels.</param>
        protected void CreateAudioSources(string microphoneName, int nbOfActiveChannel)
        {
            if (nbOfActiveChannel < 1)
            {
                throw new Exception("Cannot instantiate microphone with 0 channel");
            }

            AudioCapture mic = new AudioCapture(this.pipeline, new AudioCaptureConfiguration()
            {
                DeviceName = microphoneName,
                Format = WaveFormat.Create16BitPcm(16000, nbOfActiveChannel),
            });

            this.mics.Add(microphoneName, mic);
            if (nbOfActiveChannel > 1)
            {
                AudioSplitter splitAudio = new AudioSplitter(this.pipeline, microphoneName, nbOfActiveChannel);
                mic.PipeTo(splitAudio);
                this.splitter.Add(microphoneName, splitAudio);
            }
        }

        /// <summary>
        /// Registers an audio source for a user with RendezVous pipeline support.
        /// </summary>
        /// <param name="server">The RendezVous pipeline server.</param>
        /// <param name="sessionName">The session name.</param>
        /// <param name="user">The user to register.</param>
        /// <returns>True if registration was successful; otherwise false.</returns>
        protected bool RegisterAudioSourceWithUser(RendezVousPipeline server, string sessionName, User user)
        {
            if (this.splitter.ContainsKey(user.Microphone))
            {
                if (this.splitter[user.Microphone].Audios.Count() < user.Channel)
                {
                    throw new Exception("Incorrect channel number.");
                }

                server.CreateConnectorAndStore("Audio", $"Audio_User_{user.Id}", server.CreateOrGetSessionFromMode(sessionName), this.pipeline, typeof(AudioBuffer), this.splitter[user.Microphone].Audios[user.Channel - 1], this.LocalStore);
                this.UserAudioStreamDictionnary.Add(user, this.splitter[user.Microphone].Audios[user.Channel - 1]);
            }
            else if (this.mics.ContainsKey(user.Microphone))
            {
                server.CreateConnectorAndStore("Audio", $"Audio_User_{user.Id}", server.CreateOrGetSessionFromMode(sessionName), this.pipeline, typeof(AudioBuffer), this.mics[user.Microphone].Out, this.LocalStore);
                this.UserAudioStreamDictionnary.Add(user, this.mics[user.Microphone].Out);
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Registers an audio source for a user without RendezVous pipeline.
        /// </summary>
        /// <param name="user">The user to register.</param>
        /// <returns>True if registration was successful; otherwise false.</returns>
        protected bool RegisterAudioSourceWithUser(User user)
        {
            if (this.splitter.ContainsKey(user.Microphone))
            {
                if (this.splitter[user.Microphone].Audios.Count() < user.Channel)
                {
                    throw new Exception("Incorrect channel number.");
                }

                if (this.LocalStore)
                {
                    this.splitter[user.Microphone].Audios[user.Channel].Write($"Audio_user{user.Id}", this.AudioStore);
                }

                this.UserAudioStreamDictionnary.Add(user, this.splitter[user.Microphone].Audios[user.Channel - 1]);
            }
            else if (this.mics.ContainsKey(user.Microphone))
            {
                if (this.LocalStore)
                {
                    this.mics[user.Microphone].Out.Write($"Audio_user{user.Id}", this.AudioStore);
                }

                this.UserAudioStreamDictionnary.Add(user, this.mics[user.Microphone].Out);
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
