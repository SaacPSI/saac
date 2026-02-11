// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

namespace SAAC.Whisper
{
    using global::Whisper.net.Ggml;

    /// <summary>
    /// Configuration for the Whisper speech recognizer.
    /// </summary>
    public sealed class WhisperSpeechRecognizerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WhisperSpeechRecognizerConfiguration"/> class.
        /// </summary>
        public WhisperSpeechRecognizerConfiguration()
        {
        }

        /// <summary>
        /// Whisper model download state.
        /// </summary>
        public enum EWhisperModelDownloadState
        {
            /// <summary>
            /// Download is in progress.
            /// </summary>
            InProgress,

            /// <summary>
            /// Download completed successfully.
            /// </summary>
            Completed,

            /// <summary>
            /// Download failed.
            /// </summary>
            Failed,
        }

        /// <summary>
        /// Gets or sets the specific model path.
        /// </summary>
        public string? SpecificModelPath { get; set; } = null;

        /// <summary>
        /// Gets or sets the model directory.
        /// </summary>
        public string ModelDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model type.
        /// </summary>
        public GgmlType ModelType { get; set; } = GgmlType.Medium;

        /// <summary>
        /// Gets or sets the quantization type.
        /// </summary>
        public QuantizationType QuantizationType { get; set; } = QuantizationType.Q5_1;

        /// <summary>
        /// Gets or sets a value indicating whether to force download.
        /// </summary>
        public bool ForceDownload { get; set; } = false;

        /// <summary>
        /// Gets or sets the download timeout in seconds.
        /// </summary>
        public double DownloadTimeoutInSeconds { get; set; } = 15;

        /// <summary>
        /// Gets or sets a value indicating whether to use lazy initialization.
        /// </summary>
        public bool LazyInitialization { get; set; } = false;

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        public Language Language { get; set; } = Language.French;

        /// <summary>
        /// Gets or sets the prompt.
        /// </summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the segmentation restriction.
        /// </summary>
        public SegmentationRestriction SegmentationRestriction { get; set; } = SegmentationRestriction.OnePerUtterence;

        /// <summary>
        /// Gets or sets the input timestamp mode. Default is AtEnd per psi convention.
        /// </summary>
        public TimestampMode InputTimestampMode { get; set; } = TimestampMode.AtEnd;

        /// <summary>
        /// Gets or sets the output timestamp mode.
        /// </summary>
        public TimestampMode OutputTimestampMode { get; set; } = TimestampMode.AtEnd;

        /// <summary>
        /// Gets or sets a value indicating whether to output partial results.
        /// </summary>
        public bool OutputPartialResults { get; set; } = false;

        /// <summary>
        /// Gets or sets the partial evaluation interval in seconds.
        /// </summary>
        public double PartialEvalueationInvervalInSeconds { get; set; } = 0.5;

        /// <summary>
        /// Gets or sets a value indicating whether to output audio.
        /// </summary>
        public bool OutputAudio { get; set; } = false;

        /// <summary>
        /// Gets or sets the model download progress handler.
        /// </summary>
        public EventHandler<(EWhisperModelDownloadState, string)>? OnModelDownloadProgressHandler { get; set; } = null;
    }
}
