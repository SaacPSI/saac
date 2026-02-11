// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.WhisperRemoteServices
{
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Speech;
    using SAAC.PipelineServices;
    using SAAC.RemoteConnectors;
    using SAAC.Whisper;

    /// <summary>
    /// Component for handling Whisper audio processing with remote capabilities.
    /// </summary>
    public class WhiperRemoteComponent : WhisperAudioProcessing
    {
        private readonly RendezVousPipeline rendezVousPipeline;
        private readonly WhisperRemoteStreamsConfiguration whisperRemoteStreamsConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="WhiperRemoteComponent"/> class.
        /// </summary>
        /// <param name="rdvPipeline">The rendezvous pipeline.</param>
        /// <param name="vadConfiguration">The VAD configuration.</param>
        /// <param name="whisperConfiguration">The Whisper configuration.</param>
        /// <param name="whisperRemoteConfiguration">The Whisper remote configuration.</param>
        /// <param name="subpipelineName">The subpipeline name.</param>
        /// <param name="onSpeechRecognitionFinalResultAction">The speech recognition final result action.</param>
        /// <param name="log">The log action.</param>
        public WhiperRemoteComponent(RendezVousPipeline rdvPipeline, SystemVoiceActivityDetectorConfiguration vadConfiguration, WhisperSpeechRecognizerConfiguration whisperConfiguration, WhisperRemoteStreamsConfiguration whisperRemoteConfiguration, string? subpipelineName = null, OnSpeechRecognitionFinalResult? onSpeechRecognitionFinalResultAction = null, LogStatus? log = null)
            : base(subpipelineName is null ? rdvPipeline.Pipeline : rdvPipeline.GetOrCreateSubpipeline(subpipelineName), vadConfiguration, whisperConfiguration, onSpeechRecognitionFinalResultAction, log)
        {
            this.rendezVousPipeline = rdvPipeline;
            this.whisperRemoteStreamsConfiguration = whisperRemoteConfiguration;
        }

        /// <summary>
        /// Sets up Whisper audio processing for the given users.
        /// </summary>
        /// <param name="usersAudioSourceDictionary">The users audio source dictionary.</param>
        /// <param name="sessionName">The session name.</param>
        /// <param name="localStorageMode">The local storage mode.</param>
        public void SetupWhisperAudioProcessing(Dictionary<string, IProducer<AudioBuffer>> usersAudioSourceDictionary, string sessionName, LocalStorageMode localStorageMode)
        {
            foreach (var userData in usersAudioSourceDictionary)
            {
                (IProducer<AudioBuffer> audio, IProducer<bool> vad, IProducer<IStreamingSpeechRecognitionResult> stt) = this.SetupUserWhisper(userData.Value, userData.Key);
                this.rendezVousPipeline.Log($"VAD and STT Initialized for user {userData.Key}");
                this.rendezVousPipeline.CreateConnectorAndStore($"Audio", $"Audio_User_{userData.Key}", this.rendezVousPipeline.CreateOrGetSessionFromMode(sessionName), this.pipeline, typeof(AudioBuffer), audio, ((int)localStorageMode) % 2 == 1);
                this.rendezVousPipeline.CreateConnectorAndStore($"VAD", $"User_{userData.Key}", this.rendezVousPipeline.CreateOrGetSessionFromMode(sessionName), this.pipeline, typeof(bool), vad, localStorageMode > LocalStorageMode.AudioOnly);
                this.rendezVousPipeline.CreateConnectorAndStore($"STT", $"User_{userData.Key}", this.rendezVousPipeline.CreateOrGetSessionFromMode(sessionName), this.pipeline, typeof(IStreamingSpeechRecognitionResult), stt, localStorageMode > LocalStorageMode.AudioOnly);
            }

            this.rendezVousPipeline.AddProcess(WhisperRemoteStreams.GenerateProcess(this.pipeline, this.whisperRemoteStreamsConfiguration, this.UsersAudioProducersDictionary));
            this.rendezVousPipeline.Log($"Whisper remote streams Process generated for {this.UsersAudioProducersDictionary.Count} users.");
        }
    }
}
