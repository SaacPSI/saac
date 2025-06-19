using System;
using Microsoft.Extensions.Logging;
using Microsoft.Psi;
using SpeechProcess;
using Whisper.net.Ggml;

namespace SAAC.Whisper
{
    public sealed class WhisperSpeechRecognizerConfiguration 
    {
        public FusionSpeechProcessing speechProcessing;
        public int userID { get; set; } = 0;
        public string ModelDirectory { get; set; } = "";

        public string LibrairyPath { get; set; } = "./whisper.dll";

        public GgmlType ModelType { get; set; } = GgmlType.Medium;

        public QuantizationType QuantizationType { get; set; } = QuantizationType.Q5_1;

        public bool ForceDownload { get; set; } = false;

        public double DownloadTimeoutInSeconds { get; set; } = 15;

        public bool LazyInitialization { get; set; } = false;

        public Language Language { get; set; } = Language.French;

        public string Prompt { get; set; } = "";

        public SegmentationRestriction SegmentationRestriction { get; set; } = SegmentationRestriction.OnePerUtterence;

        public TimestampMode InputTimestampMode { get; set; } = TimestampMode.AtEnd;//\psi convention

        public TimestampMode OutputTimestampMode { get; set; } = TimestampMode.AtEnd;

        public bool OutputPartialResults { get; set; } = false;

        public double PartialEvalueationInvervalInSeconds { get; set; } = 0.5;

        public bool OutputAudio { get; set; } = false;

        public WhisperSpeechRecognizerConfiguration() 
        {
            
        }
    }
}
