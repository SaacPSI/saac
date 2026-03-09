using System;
using System.Collections.Generic;

namespace CASPERAnalysis
{
    /// <summary>
    /// Represents the classification result from the logigramme analysis
    /// </summary>
    public enum ClassificationType
    {
        None,
        AnticipationGamma,      // R1: Anticipation Gamma
        GammaLearning,           // R2: Gamma - apprentissage
        Gamma,                   // R3, R6: Gamma
        Alpha,                   // R4: Alpha
        Beta                     // R5: Beta
    }

    /// <summary>
    /// Classification result with timestamp and metadata
    /// </summary>
    public class ClassificationResult
    {
        public DateTime Timestamp { get; set; }
        public ClassificationType Classification { get; set; }
        public string Reason { get; set; } = "";
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public override string ToString()
        {
            return $"{Timestamp:HH:mm:ss.fff} - {Classification}: {Reason}";
        }
    }
}

