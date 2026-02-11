// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Whisper
{
    /// <summary>
    /// Abstract base class for managing Whisper transcriptions.
    /// </summary>
    public abstract class WhisperTranscriptionManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhisperTranscriptionManager"/> class.
        /// </summary>
        public WhisperTranscriptionManager()
        {
            this.Transcriptions = new List<(DateTime, string, string)>();
        }

        /// <summary>
        /// Gets the list of transcriptions.
        /// </summary>
        public List<(DateTime, string, string)> Transcriptions { get; private set; }

        /// <summary>
        /// Gets the delegate for handling speech recognition final results.
        /// </summary>
        /// <returns>The delegate.</returns>
        public WhisperAudioProcessing.OnSpeechRecognitionFinalResult GetDelegate()
        {
            return this.AddTranscription;
        }

        /// <summary>
        /// Adds a transcription to the list.
        /// </summary>
        /// <param name="time">The time of the transcription.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="text">The transcription text.</param>
        public void AddTranscription(DateTime time, string userId, string text)
        {
            this.Transcriptions.Add((time, userId, text));
        }

        /// <summary>
        /// Sorts the transcriptions by time.
        /// </summary>
        /// <returns>The sorted list of transcriptions.</returns>
        public List<(DateTime, string, string)> SortTranscriptions()
        {
            return this.Transcriptions = this.Transcriptions.OrderBy(entry => entry.Item1).ToList();
        }

        /// <summary>
        /// Writes the transcriptions to a file.
        /// </summary>
        /// <param name="file">The file path.</param>
        /// <param name="cleanList">Whether to clean the list after writing.</param>
        public abstract void WriteTranscription(string file, bool cleanList = true);
    }
}
