// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Whisper
{
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Provides audio processing capabilities for Whisper speech recognition.
    /// </summary>
    public class WhisperAudioProcessing
    {
        /// <summary>
        /// Delegate for handling final speech recognition results.
        /// </summary>
        /// <param name="time">The time of the result.</param>
        /// <param name="id">The user ID.</param>
        /// <param name="stt">The speech-to-text result.</param>
        public delegate void OnSpeechRecognitionFinalResult(DateTime time, string id, string stt);

        /// <summary>
        /// Local storage mode options.
        /// </summary>
        public enum LocalStorageMode
        {
            /// <summary>
            /// No local storage.
            /// </summary>
            None,

            /// <summary>
            /// Store audio only.
            /// </summary>
            AudioOnly,

            /// <summary>
            /// Store VAD and STT results.
            /// </summary>
            VAD_STT,

            /// <summary>
            /// Store all data.
            /// </summary>
            All,
        }

        /// <summary>
        /// The exporters list.
        /// </summary>
        protected List<PsiExporter> exporters;

        /// <summary>
        /// The pipeline.
        /// </summary>
        protected Pipeline pipeline;

        /// <summary>
        /// The speech recognition final result action.
        /// </summary>
        protected OnSpeechRecognitionFinalResult? onSpeechRecognitionFinalResultAction = null;

        /// <summary>
        /// The log action.
        /// </summary>
        protected LogStatus? log;

        /// <summary>
        /// Initializes a new instance of the <see cref="WhisperAudioProcessing"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="vadConfiguration">The VAD configuration.</param>
        /// <param name="whisperConfiguration">The Whisper configuration.</param>
        /// <param name="onSpeechRecognitionFinalResultAction">The speech recognition final result action.</param>
        /// <param name="log">The log action.</param>
        public WhisperAudioProcessing(Pipeline pipeline, SystemVoiceActivityDetectorConfiguration vadConfiguration, WhisperSpeechRecognizerConfiguration whisperConfiguration, OnSpeechRecognitionFinalResult? onSpeechRecognitionFinalResultAction = null, LogStatus? log = null)
        {
            this.log = log;
            this.pipeline = pipeline;
            this.exporters = new List<PsiExporter>();
            this.VadConfiguration = vadConfiguration;
            this.WhisperConfiguration = whisperConfiguration;
            this.UsersAudioProducersDictionary = new Dictionary<string, (IProducer<AudioBuffer>, IProducer<bool>, IProducer<IStreamingSpeechRecognitionResult>)>();
            this.onSpeechRecognitionFinalResultAction = onSpeechRecognitionFinalResultAction;
        }

        /// <summary>
        /// Gets the VAD configuration.
        /// </summary>
        public SystemVoiceActivityDetectorConfiguration VadConfiguration { get; private set; }

        /// <summary>
        /// Gets the Whisper configuration.
        /// </summary>
        public WhisperSpeechRecognizerConfiguration WhisperConfiguration { get; private set; }

        /// <summary>
        /// Gets the dictionary of users audio producers.
        /// </summary>
        public Dictionary<string, (IProducer<AudioBuffer>, IProducer<bool>, IProducer<IStreamingSpeechRecognitionResult>)> UsersAudioProducersDictionary { get; private set; }

        /// <summary>
        /// Sets up voice activity detection.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="audio">The audio producer.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="language">The language.</param>
        /// <returns>The VAD producer.</returns>
        public static IProducer<bool> SetupVoiceActivityDetection(Pipeline pipeline, IProducer<AudioBuffer> audio, string userId, string language = "fr-fr")
        {
            SystemVoiceActivityDetectorConfiguration configuration = new SystemVoiceActivityDetectorConfiguration()
            {
                Language = language,
                Grammars = null,
                BufferLengthInMs = 500,
                VoiceActivityStartOffsetMs = -250,
                VoiceActivityEndOffsetMs = -250,
                InputFormat = WaveFormat.Create16kHz1Channel16BitPcm(),
                InitialSilenceTimeoutMs = 250,
                BabbleTimeoutMs = 1000,
                EndSilenceTimeoutAmbiguousMs = 250,
                EndSilenceTimeoutMs = 150,
            };
            return SetupVoiceActivityDetection(pipeline, audio, userId, configuration);
        }

        /// <summary>
        /// Sets up voice activity detection with configuration.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="audio">The audio producer.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="configuration">The VAD configuration.</param>
        /// <returns>The VAD producer.</returns>
        public static IProducer<bool> SetupVoiceActivityDetection(Pipeline pipeline, IProducer<AudioBuffer> audio, string userId, SystemVoiceActivityDetectorConfiguration configuration)
        {
            SystemVoiceActivityDetector vad = new SystemVoiceActivityDetector(pipeline, configuration, $"VAD{userId}");
            audio.PipeTo(vad.In);
            return vad.Out;
        }

        /// <summary>
        /// Creates an action for handling speech recognition final results.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="log">The log action.</param>
        /// <param name="onSpeechRecognitionFinalResult">The callback action.</param>
        /// <returns>The action.</returns>
        public static Action<IStreamingSpeechRecognitionResult, Envelope> OnSpeechRecognitionFinalResultAction(string userId, LogStatus? log = null, OnSpeechRecognitionFinalResult? onSpeechRecognitionFinalResult = null)
        {
            if (onSpeechRecognitionFinalResult is null)
            {
                if (log is null)
                {
                    return (m, e) =>
                    {
                        e.CreationTime = e.OriginatingTime;
                    };
                }
                else
                {
                    return (m, e) =>
                    {
                        e.CreationTime = e.OriginatingTime;
                        log($"{e.OriginatingTime} - {userId}: {m.Text}");
                    };
                }
            }
            else
            {
                if (log is null)
                {
                    return (m, e) =>
                    {
                        e.CreationTime = e.OriginatingTime;
                        onSpeechRecognitionFinalResult(e.OriginatingTime, userId, m.Text);
                    };
                }
                else
                {
                    return (m, e) =>
                    {
                        e.CreationTime = e.OriginatingTime;
                        log($"{e.OriginatingTime} - {userId}: {m.Text}");
                        onSpeechRecognitionFinalResult(e.OriginatingTime, userId, m.Text);
                    };
                }
            }
        }

        /// <summary>
        /// Sets up Whisper speech recognition.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="annotatedAudioWhisper">The annotated audio producer.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="configuration">The Whisper configuration.</param>
        /// <param name="log">The log action.</param>
        /// <param name="onSpeechRecognitionFinalResultAction">The callback action.</param>
        /// <returns>The speech recognition result producer.</returns>
        public static IProducer<IStreamingSpeechRecognitionResult> SetupWhisper(Pipeline pipeline, IProducer<(AudioBuffer, bool)> annotatedAudioWhisper, string userId, WhisperSpeechRecognizerConfiguration configuration, LogStatus? log = null, OnSpeechRecognitionFinalResult? onSpeechRecognitionFinalResultAction = null)
        {
            WhisperSpeechRecognizer whisper = new WhisperSpeechRecognizer(pipeline, configuration, $"Whisper{userId}");
            annotatedAudioWhisper.PipeTo(whisper);
            return whisper.FinalOut.Where(result => result.IsFinal).Do(OnSpeechRecognitionFinalResultAction(userId, log, onSpeechRecognitionFinalResultAction));
        }

        /// <summary>
        /// Sets up Whisper audio processing.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="audio">The audio producer.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="vadConfiguration">The VAD configuration.</param>
        /// <param name="whisperConfiguration">The Whisper configuration.</param>
        /// <param name="log">The log action.</param>
        /// <param name="onSpeechRecognitionFinalResultAction">The callback action.</param>
        /// <returns>A tuple of audio, VAD, and STT producers.</returns>
        public static (IProducer<AudioBuffer>, IProducer<bool>, IProducer<IStreamingSpeechRecognitionResult>) SetupWhisperAudioProcessing(Pipeline pipeline, IProducer<AudioBuffer> audio, string userId, SystemVoiceActivityDetectorConfiguration vadConfiguration, WhisperSpeechRecognizerConfiguration whisperConfiguration, LogStatus? log = null, OnSpeechRecognitionFinalResult? onSpeechRecognitionFinalResultAction = null)
        {
            IProducer<bool> vad = SetupVoiceActivityDetection(pipeline, audio, userId, vadConfiguration);
            return (audio, vad, SetupWhisper(pipeline, audio.Join(vad), userId, whisperConfiguration, log, onSpeechRecognitionFinalResultAction));
        }

        /// <summary>
        /// Stops the audio processing.
        /// </summary>
        public void Stop()
        {
            foreach (PsiExporter exporter in this.exporters)
            {
                exporter.Stop(this.pipeline.GetCurrentTime(), () => { });
            }
        }

        /// <summary>
        /// Sets up Whisper for multiple users with session.
        /// </summary>
        /// <param name="users">The users dictionary.</param>
        /// <param name="session">The session.</param>
        /// <param name="path">The storage path.</param>
        /// <param name="localStorageMode">The local storage mode.</param>
        /// <param name="subpipeline">The optional subpipeline.</param>
        /// <returns>The users audio producers dictionary.</returns>
        public Dictionary<string, (IProducer<AudioBuffer>, IProducer<bool>, IProducer<IStreamingSpeechRecognitionResult>)> SetupUsersWhisper(Dictionary<string, IProducer<AudioBuffer>> users, ref Session session, string path, LocalStorageMode localStorageMode, Pipeline? subpipeline = null)
        {
            Pipeline currentP = subpipeline is null ? this.pipeline : subpipeline;
            foreach (var user in users)
            {
                var result = SetupWhisperAudioProcessing(currentP, user.Value, user.Key, this.VadConfiguration, this.WhisperConfiguration, this.log, this.onSpeechRecognitionFinalResultAction);
                switch (localStorageMode)
                {
                    case LocalStorageMode.AudioOnly:
                        {
                            PsiExporter audioStore = PsiStore.Create(currentP, $"Audio_User_{user.Key}", $"{path}/{session.Name}/");
                            audioStore.Write(result.Item1, "Audio");
                            session.AddPartitionFromPsiStoreAsync($"Audio_User_{user.Key}", $"{path}/{session.Name}/");
                            this.exporters.Add(audioStore);
                        }

                        break;
                    case LocalStorageMode.VAD_STT:
                        {
                            PsiExporter vadsttStore = PsiStore.Create(currentP, $"User_{user.Key}", $"{path}/{session.Name}/");
                            vadsttStore.Write(result.Item2, "VAD");
                            vadsttStore.Write(result.Item3, "STT");
                            session.AddPartitionFromPsiStoreAsync($"User_{user.Key}", $"{path}/{session.Name}/");
                            this.exporters.Add(vadsttStore);
                        }

                        break;
                    case LocalStorageMode.All:
                        {
                            PsiExporter audioStore = PsiStore.Create(currentP, $"Audio_User_{user.Key}", $"{path}/{session.Name}/");
                            audioStore.Write(result.Item1, "Audio");
                            session.AddPartitionFromPsiStoreAsync($"Audio_User_{user.Key}", $"{path}/{session.Name}/");
                            PsiExporter vadsttStore = PsiStore.Create(currentP, $"User_{user.Key}", $"{path}/{session.Name}/");
                            vadsttStore.Write(result.Item2, "VAD");
                            vadsttStore.Write(result.Item3, "STT");
                            session.AddPartitionFromPsiStoreAsync($"User_{user.Key}", $"{path}/{session.Name}/");
                            this.exporters.Add(audioStore);
                            this.exporters.Add(vadsttStore);
                        }

                        break;
                }

                this.UsersAudioProducersDictionary.Add(user.Key, result);
            }

            return this.UsersAudioProducersDictionary;
        }

        /// <summary>
        /// Sets up Whisper for multiple users.
        /// </summary>
        /// <param name="users">The users dictionary.</param>
        /// <param name="subpipeline">The optional subpipeline.</param>
        /// <returns>The users audio producers dictionary.</returns>
        public Dictionary<string, (IProducer<AudioBuffer>, IProducer<bool>, IProducer<IStreamingSpeechRecognitionResult>)> SetupUsersWhisper(Dictionary<string, IProducer<AudioBuffer>> users, Pipeline? subpipeline = null)
        {
            Pipeline currentP = subpipeline is null ? this.pipeline : subpipeline;
            foreach (var user in users)
            {
                this.UsersAudioProducersDictionary.Add(user.Key, SetupWhisperAudioProcessing(currentP, user.Value, user.Key, this.VadConfiguration, this.WhisperConfiguration, this.log, this.onSpeechRecognitionFinalResultAction));
            }

            return this.UsersAudioProducersDictionary;
        }

        /// <summary>
        /// Sets up Whisper for a single user.
        /// </summary>
        /// <param name="audio">The audio producer.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="subpipeline">The optional subpipeline.</param>
        /// <returns>A tuple of audio, VAD, and STT producers.</returns>
        public (IProducer<AudioBuffer>, IProducer<bool>, IProducer<IStreamingSpeechRecognitionResult>) SetupUserWhisper(IProducer<AudioBuffer> audio, string userId, Pipeline? subpipeline = null)
        {
            var result = SetupWhisperAudioProcessing(subpipeline is null ? this.pipeline : subpipeline, audio, userId, this.VadConfiguration, this.WhisperConfiguration, this.log, this.onSpeechRecognitionFinalResultAction);
            this.UsersAudioProducersDictionary.Add(userId, result);
            return result;
        }
    }
}
