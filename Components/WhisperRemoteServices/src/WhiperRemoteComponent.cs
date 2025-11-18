using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Speech;
using SAAC.PipelineServices;
using SAAC.Whisper;
using SAAC.RemoteConnectors;

namespace SAAC.WhisperRemoteServices
{
    public class WhiperRemoteComponent : WhisperAudioProcessing
    {
        protected RendezVousPipeline rendezVousPipeline;
        protected WhisperRemoteStreamsConfiguration whisperRemoteStreamsConfiguration;

        public WhiperRemoteComponent(RendezVousPipeline rdvPipeline, SystemVoiceActivityDetectorConfiguration vadConfiguration, WhisperSpeechRecognizerConfiguration whisperConfiguration, WhisperRemoteStreamsConfiguration whisperRemoteConfiguration, string? subpipelineName = null, OnSpeechRecognitionFinalResult? onSpeechRecognitionFinalResultAction = null, LogStatus? log= null)
            : base(subpipelineName is null ? rdvPipeline.Pipeline : rdvPipeline.GetOrCreateSubpipeline(subpipelineName), vadConfiguration, whisperConfiguration, onSpeechRecognitionFinalResultAction, log)
        { 
            rendezVousPipeline = rdvPipeline;
            whisperRemoteStreamsConfiguration = whisperRemoteConfiguration;
        }

        public void SetupWhisperAudioProcessing(Dictionary<string, IProducer<AudioBuffer>> usersAudioSourceDictionary, string sessionName, LocalStorageMode localStorageMode)
        {
            foreach (var userData in usersAudioSourceDictionary)
            {
                (IProducer<AudioBuffer> audio, IProducer<bool> vad, IProducer<IStreamingSpeechRecognitionResult> stt) = SetupUserWhisper(userData.Value, userData.Key);
                rendezVousPipeline.Log($"VAD and STT Initialized for user { userData.Key}");
                rendezVousPipeline.CreateConnectorAndStore($"Audio", $"Audio_User_{userData.Key}", rendezVousPipeline.CreateOrGetSessionFromMode(sessionName), pipeline, typeof(AudioBuffer), audio, ((int)localStorageMode) % 2 == 1 );
                rendezVousPipeline.CreateConnectorAndStore($"VAD", $"User_{userData.Key}", rendezVousPipeline.CreateOrGetSessionFromMode(sessionName), pipeline, typeof(bool), vad, localStorageMode > LocalStorageMode.AudioOnly);
                rendezVousPipeline.CreateConnectorAndStore($"STT", $"User_{userData.Key}", rendezVousPipeline.CreateOrGetSessionFromMode(sessionName), pipeline, typeof(IStreamingSpeechRecognitionResult), stt, localStorageMode > LocalStorageMode.AudioOnly);
            }

            rendezVousPipeline.AddProcess(WhisperRemoteStreams.GenerateProcess(pipeline, whisperRemoteStreamsConfiguration, UsersAudioProducersDictionary));
            rendezVousPipeline.Log($"Whisper remote streams Process generated for {UsersAudioProducersDictionary.Count} users.");
        }
    }
}
