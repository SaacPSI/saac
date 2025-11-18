using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Data;
using Microsoft.Psi.Speech;
using System.Speech.Recognition;
using System.Threading.Tasks;

namespace SAAC.Whisper
{
    public class WhisperAudioProcessing
    {
        public delegate void OnSpeechRecognitionFinalResult(DateTime time, string id, string stt);
        public enum LocalStorageMode { None, AudioOnly, VAD_STT, All };

        public SystemVoiceActivityDetectorConfiguration VadConfiguration { get; private set; }
        public WhisperSpeechRecognizerConfiguration WhisperConfiguration { get; private set; }

        public Dictionary<string, (IProducer<AudioBuffer>, IProducer<bool>, IProducer<IStreamingSpeechRecognitionResult>)> UsersAudioProducersDictionary { get; private set; }

        protected List<PsiExporter> exporters;
        protected Pipeline pipeline;
        protected OnSpeechRecognitionFinalResult? onSpeechRecognitionFinalResultAction = null;
        protected LogStatus? log; 

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
        public void Stop()
        {
            foreach(PsiExporter exporter in exporters)
                exporter.Stop(pipeline.GetCurrentTime(), () => { });
        }

        public Dictionary<string, (IProducer<AudioBuffer>, IProducer<bool>, IProducer<IStreamingSpeechRecognitionResult>)> SetupUsersWhisper(Dictionary<string, IProducer<AudioBuffer>> users, ref Session session, string path, LocalStorageMode localStorageMode, Pipeline? subpipeline = null)
        {            
            Pipeline currentP = subpipeline is null ? pipeline : subpipeline;
            foreach (var user in users)
            {
                var result = SetupWhisperAudioProcessing(currentP, user.Value, user.Key, VadConfiguration, WhisperConfiguration, log,  onSpeechRecognitionFinalResultAction);
                switch (localStorageMode)
                {
                    case LocalStorageMode.AudioOnly:
                        {
                            PsiExporter audioStore = PsiStore.Create(currentP, $"Audio_User_{user.Key}", $"{path}/{session.Name}/");
                            audioStore.Write(result.Item1, "Audio");
                            session.AddPartitionFromPsiStoreAsync($"Audio_User_{user.Key}", $"{path}/{session.Name}/");
                            exporters.Add(audioStore);
                        }
                        break;
                    case LocalStorageMode.VAD_STT:
                        {
                            PsiExporter vadsttStore = PsiStore.Create(currentP, $"User_{user.Key}", $"{path}/{session.Name}/");
                            vadsttStore.Write(result.Item2, "VAD");
                            vadsttStore.Write(result.Item3, "STT");
                            session.AddPartitionFromPsiStoreAsync($"User_{user.Key}", $"{path}/{session.Name}/");
                            exporters.Add(vadsttStore);
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
                            exporters.Add(audioStore);
                            exporters.Add(vadsttStore);
                        }
                        break;
                }
                UsersAudioProducersDictionary.Add(user.Key, result);
            }
            return UsersAudioProducersDictionary;
        }
 
        public Dictionary<string, (IProducer<AudioBuffer>, IProducer<bool>, IProducer<IStreamingSpeechRecognitionResult>)> SetupUsersWhisper(Dictionary<string, IProducer<AudioBuffer>> users, Pipeline? subpipeline = null)
        {
            Pipeline currentP = subpipeline is null ? pipeline : subpipeline;
            foreach (var user in users)
                UsersAudioProducersDictionary.Add(user.Key, SetupWhisperAudioProcessing(currentP, user.Value, user.Key, VadConfiguration, WhisperConfiguration, log, onSpeechRecognitionFinalResultAction));
            return UsersAudioProducersDictionary;
        }

        public (IProducer<AudioBuffer>, IProducer<bool>, IProducer<IStreamingSpeechRecognitionResult>) SetupUserWhisper(IProducer<AudioBuffer> audio, string userId, Pipeline? subpipeline = null)
        {
            var result = SetupWhisperAudioProcessing(subpipeline is null ? pipeline : subpipeline, audio, userId, VadConfiguration, WhisperConfiguration, log, onSpeechRecognitionFinalResultAction);
            UsersAudioProducersDictionary.Add(userId, result);
            return result;
        }

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
                EndSilenceTimeoutMs = 150
            };
            return SetupVoiceActivityDetection(pipeline, audio, userId, configuration);
        }

        public static IProducer<bool> SetupVoiceActivityDetection(Pipeline pipeline, IProducer<AudioBuffer> audio, string userId, SystemVoiceActivityDetectorConfiguration configuration)
        {
            SystemVoiceActivityDetector vad = new SystemVoiceActivityDetector(pipeline, configuration, $"VAD{userId}");
            audio.PipeTo(vad.In);
            return vad.Out;
        }

        public static Action<IStreamingSpeechRecognitionResult, Envelope> OnSpeechRecognitionFinalResultAction(string userId, LogStatus? log = null, OnSpeechRecognitionFinalResult? onSpeechRecognitionFinalResult = null)
        {
            if (onSpeechRecognitionFinalResult is null)
            {
                if (log is null)
                    return (m, e) =>
                    {
                        e.CreationTime = e.OriginatingTime;
                    };
                else
                    return (m, e) =>
                    {
                        e.CreationTime = e.OriginatingTime;
                        log($"{e.OriginatingTime} - {userId}: {m.Text}");
                    };
            }
            else
            {
                if(log is null)
                    return (m, e) =>
                    {
                        e.CreationTime = e.OriginatingTime;
                        onSpeechRecognitionFinalResult(e.OriginatingTime, userId, m.Text);
                    };
                else
                    return (m, e) =>
                    {
                        e.CreationTime = e.OriginatingTime;
                        log($"{e.OriginatingTime} - {userId}: {m.Text}");
                        onSpeechRecognitionFinalResult(e.OriginatingTime, userId, m.Text);
                    };
            } 
        }

        public static IProducer<IStreamingSpeechRecognitionResult> SetupWhisper(Pipeline pipeline, IProducer<(AudioBuffer, bool)> annotatedAudioWhisper, string userId, WhisperSpeechRecognizerConfiguration configuration, LogStatus? log = null, OnSpeechRecognitionFinalResult? onSpeechRecognitionFinalResultAction = null)
        {
            WhisperSpeechRecognizer whisper = new WhisperSpeechRecognizer(pipeline, configuration, $"Whisper{userId}");
            annotatedAudioWhisper.PipeTo(whisper);
            return whisper.FinalOut.Where(result => result.IsFinal).Do(OnSpeechRecognitionFinalResultAction(userId, log, onSpeechRecognitionFinalResultAction));
        }
        

        public static (IProducer<AudioBuffer>, IProducer<bool>, IProducer<IStreamingSpeechRecognitionResult>) SetupWhisperAudioProcessing(Pipeline pipeline, IProducer<AudioBuffer> audio, string userId, SystemVoiceActivityDetectorConfiguration vadConfiguration, WhisperSpeechRecognizerConfiguration whisperConfiguration, LogStatus? log = null, OnSpeechRecognitionFinalResult? onSpeechRecognitionFinalResultAction = null)
        {
            IProducer<bool> vad = SetupVoiceActivityDetection(pipeline, audio, userId, vadConfiguration);
            return (audio, vad, SetupWhisper(pipeline, audio.Join(vad), userId, whisperConfiguration, log, onSpeechRecognitionFinalResultAction));
        }
    }
}
