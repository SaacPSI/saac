namespace SAAC.Whisper
{
    public abstract class WhisperTranscriptionManager
    {
        public List<(DateTime, string, string)> Transcriptions { get; private set; }

        public WhisperTranscriptionManager()
        {
            Transcriptions = new List<(DateTime, string, string)>();
        }

        public WhisperAudioProcessing.OnSpeechRecognitionFinalResult GetDelegate()
        {
            return AddTranscription;
        }

        public void AddTranscription(DateTime time, string userId, string text)
        {
            Transcriptions.Add((time, userId, text));
        }

        public List<(DateTime, string, string)> SortTranscriptions()
        {
            return Transcriptions = Transcriptions.OrderBy(entry => entry.Item1).ToList();
        }

        public abstract void WriteTranscription(string file, bool cleanList = true);
    }
}
