using System;
using System.Text.RegularExpressions;
using Microsoft.Psi;
using Microsoft.Psi.Components;

namespace CASPERAnalysis.StreamProcessors
{
    /// <summary>
    /// Classifies speech into comprehension states
    /// </summary>
    public enum SpeechComprehensionState
    {
        Unknown,
        Comprehended,
        Incomprehended,
        Annoyance
    }

    /// <summary>
    /// Classifies speech transcription into comprehension states
    /// </summary>
    public class SpeechComprehensionClassifier : ConsumerProducer<string, SpeechComprehensionState>
    {
        // Keywords that indicate incomprehension or annoyance
        private static readonly string[] IncomprehensionKeywords = {
            "quoi", "comment", "je ne comprends pas", "je comprends pas",
            "pardon", "répète", "encore", "je ne sais pas"
        };

        private static readonly string[] AnnoyanceKeywords = {
            "merde", "putain", "zut", "mince", "bordel",
            "c'est nul", "ça marche pas", "ça ne fonctionne pas"
        };

        public SpeechComprehensionClassifier(Pipeline pipeline)
            : base(pipeline)
        {
        }

        protected override void Receive(string transcription, Envelope envelope)
        {
            if (string.IsNullOrWhiteSpace(transcription))
            {
                Out.Post(SpeechComprehensionState.Unknown, envelope.OriginatingTime);
                return;
            }

            var lowerTranscription = transcription.ToLowerInvariant();

            // Check for annoyance first (more specific)
            foreach (var keyword in AnnoyanceKeywords)
            {
                if (lowerTranscription.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    Out.Post(SpeechComprehensionState.Annoyance, envelope.OriginatingTime);
                    return;
                }
            }

            // Check for incomprehension
            foreach (var keyword in IncomprehensionKeywords)
            {
                if (lowerTranscription.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    Out.Post(SpeechComprehensionState.Incomprehended, envelope.OriginatingTime);
                    return;
                }
            }

            // Default: assume comprehension if there's meaningful speech
            // (This is a simplification - you may want more sophisticated logic)
            if (transcription.Length > 3)
            {
                Out.Post(SpeechComprehensionState.Comprehended, envelope.OriginatingTime);
            }
            else
            {
                Out.Post(SpeechComprehensionState.Unknown, envelope.OriginatingTime);
            }
        }
    }
}

