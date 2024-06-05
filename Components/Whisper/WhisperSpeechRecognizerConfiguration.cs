using System;
using Microsoft.Extensions.Logging;
using Microsoft.Psi;
using Whisper.net.Ggml;

namespace SAAC.Whisper
{
    public sealed class WhisperSpeechRecognizerConfiguration 
    { 
        public string modelDirectory { get; set; } = "";

        public GgmlType modelType { get; set; } = GgmlType.Medium;

        public QuantizationType quantizationType { get; set; } = QuantizationType.Q5_1;

        public bool forceDownload { get; set; } = false;

        public double downloadTimeoutInSeconds { get; set; } = 15;

        public bool lazyInitialization { get; set; } = false;

        public Language language { get; set; } = Language.French;

        public string prompt { get; set; } = "";

        public SegmentationRestriction segmentationRestriction { get; set; } = SegmentationRestriction.OnePerUtterence;

        public TimestampMode inputTimestampMode { get; set; } = TimestampMode.AtEnd;//\psi convention

        public TimestampMode outputTimestampMode { get; set; } = TimestampMode.AtEnd;

        public bool outputPartialResults { get; set; } = false;

        public double partialEvalueationInvervalInSeconds { get; set; } = 0.5;

        public bool outputAudio { get; set; } = false;

        public WhisperSpeechRecognizerConfiguration() 
        {
            
        }
    }
}
