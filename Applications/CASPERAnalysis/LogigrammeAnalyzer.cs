using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Interop;
using CASPERAnalysis.StreamProcessors;

namespace CASPERAnalysis
{
    /// <summary>
    /// Implements the decision tree logic from Logigramme1.mmd
    /// </summary>
    public class LogigrammeAnalyzer : IProducer<ClassificationResult>
    {
        private readonly Pipeline pipeline;
        private readonly Emitter<ClassificationResult> output;

        // Input streams
        private IProducer<bool> moduleGenerationSuccess;
        private IProducer<bool> handDoorProximity;
        private IProducer<bool> gazeOnIndicator;
        private IProducer<bool> gazeOnDoor;
        private IProducer<SpeechComprehensionState> speechComprehension;
        private IProducer<bool> doorClosed;
        private IProducer<int> validationButtonCount;
        private IProducer<bool> differentButtonPressed;
        private IProducer<bool> visualFeedbackEnabled;

        public Emitter<ClassificationResult> Out => output;

        public LogigrammeAnalyzer(
            Pipeline pipeline,
            IProducer<bool> moduleGenerationSuccess,
            IProducer<bool> handDoorProximity,
            IProducer<bool> gazeOnIndicator,
            IProducer<bool> gazeOnDoor,
            IProducer<SpeechComprehensionState> speechComprehension,
            IProducer<bool> doorClosed,
            IProducer<int> validationButtonCount,
            IProducer<bool> differentButtonPressed,
            IProducer<bool> visualFeedbackEnabled)
        {
            this.pipeline = pipeline;
            this.output = pipeline.CreateEmitter<ClassificationResult>(this, nameof(Out));

            this.moduleGenerationSuccess = moduleGenerationSuccess;
            this.handDoorProximity = handDoorProximity;
            this.gazeOnIndicator = gazeOnIndicator;
            this.gazeOnDoor = gazeOnDoor;
            this.speechComprehension = speechComprehension;
            this.doorClosed = doorClosed;
            this.validationButtonCount = validationButtonCount;
            this.differentButtonPressed = differentButtonPressed;
            this.visualFeedbackEnabled = visualFeedbackEnabled;

            // Subscribe to module generation success to start the decision tree
            moduleGenerationSuccess.Do(ProcessModuleGenerationResult);
        }

        private void ProcessModuleGenerationResult(bool success, Envelope envelope)
        {
            if (success)
            {
                // Start: Oui -> F2
                CheckHandDoorProximity(envelope.OriginatingTime);
            }
            else
            {
                // Start: Non -> F3
                ProcessPerturbationDetected(envelope.OriginatingTime);
            }
        }

        private void CheckHandDoorProximity(DateTime timestamp)
        {
            // F2: Tentative de fermeture porte ?
            // Use a window looking back 500ms from the current message
            handDoorProximity
                .Window(RelativeTimeInterval.Past(TimeSpan.FromMilliseconds(500)))
                .Do(window =>
                {
                    bool hasProximity = window.Any();
                    if (hasProximity)
                    {
                        // R1: Anticipation Gamma
                        EmitClassification(ClassificationType.AnticipationGamma, "Hand touching/moving toward door", timestamp);
                    }
                    // If not, loop back to Start (handled by continuous monitoring)
                });
        }

        private void ProcessPerturbationDetected(DateTime timestamp)
        {
            // F3: Perturbation détectée - porte mal fermée si feedback visuel activé
            visualFeedbackEnabled
                .Window(RelativeTimeInterval.Past(TimeSpan.FromMilliseconds(100)))
                .Do(window =>
                {
                    bool feedbackEnabled = window.Any();
                    if (feedbackEnabled)
                    {
                        // F4: Regard sur indicateur visuel porte fermée ? (50-150ms)
                        CheckGazeOnIndicator(timestamp);
                    }
                });
        }

        private void CheckGazeOnIndicator(DateTime timestamp)
        {
            // F4: Regard sur indicateur visuel porte fermée ? (50-150ms)
            // Use a window from 50ms in the past to 150ms in the future
            gazeOnIndicator
                .Window(new RelativeTimeInterval(TimeSpan.FromMilliseconds(-50), TimeSpan.FromMilliseconds(150)))
                .Do(window =>
                {
                    bool gazedOnIndicator = window.Any();
                    if (gazedOnIndicator)
                    {
                        // F5: Regard sur porte ? (50-150ms ET/OU Parole compréhension)
                        CheckGazeOnDoorOrSpeech(timestamp);
                    }
                    else
                    {
                        // F7: Regard sur la porte ? ET/OU parole compréhension
                        CheckGazeOnDoorOrSpeechAlternative(timestamp);
                    }
                });
        }

        private void CheckGazeOnDoorOrSpeech(DateTime timestamp)
        {
            // F5: Regard sur porte ? (50-150ms ET/OU Parole compréhension)
            // Check both gaze and speech within window from 50ms past to 150ms future
            var relativeInterval = new RelativeTimeInterval(TimeSpan.FromMilliseconds(-50), TimeSpan.FromMilliseconds(150));
            var gazeWindow = gazeOnDoor.Window(relativeInterval);
            var speechWindow = speechComprehension.Window(relativeInterval);

            // Join the streams to check both conditions
            gazeWindow.Join(speechWindow, RelativeTimeInterval.Zero)
                .Do(fused =>
                {
                    bool gazedOnDoor = fused.Item1.Any();
                    bool speechComprehended = fused.Item2.Any(s => s == SpeechComprehensionState.Comprehended);

                    if (gazedOnDoor || speechComprehended)
                    {
                        // F6: Fermeture de la porte ?
                        CheckDoorClosure(timestamp, ClassificationType.GammaLearning, "Gaze on door or speech comprehension");
                    }
                });
        }

        private void CheckGazeOnDoorOrSpeechAlternative(DateTime timestamp)
        {
            // F7: Regard sur la porte ? ET/OU parole compréhension
            var relativeInterval = new RelativeTimeInterval(TimeSpan.FromMilliseconds(-50), TimeSpan.FromMilliseconds(150));
            var gazeWindow = gazeOnDoor.Window(relativeInterval);
            var speechWindow = speechComprehension.Window(relativeInterval);

            gazeWindow.Join(speechWindow, RelativeTimeInterval.Zero)
                .Do(fused =>
                {
                    bool gazedOnDoor = fused.Item1.Any();
                    bool speechComprehended = fused.Item2.Any(s => s == SpeechComprehensionState.Comprehended);

                    if (gazedOnDoor || speechComprehended)
                    {
                        // F8: Fermeture de la porte ?
                        CheckDoorClosure(timestamp, ClassificationType.Gamma, "Gaze on door or speech comprehension (alternative path)");
                    }
                    else
                    {
                        // F9: Action sur bouton validé x3 ET parole incompréhension/agacement ?
                        CheckButtonPressAndSpeech(timestamp);
                    }
                });
        }

        private void CheckDoorClosure(DateTime timestamp, ClassificationType successType, string reason)
        {
            doorClosed
                .Window(RelativeTimeInterval.Future(TimeSpan.FromMilliseconds(2000)))
                .Do(window =>
                {
                    bool doorWasClosed = window.Any();
                    if (doorWasClosed)
                    {
                        EmitClassification(successType, reason + " - Door closed", timestamp);
                    }
                    else
                    {
                        // F9: Action sur bouton validé x3 ET parole incompréhension/agacement ?
                        CheckButtonPressAndSpeech(timestamp);
                    }
                });
        }

        private void CheckButtonPressAndSpeech(DateTime timestamp)
        {
            // F9: Action sur bouton validé x3 ET parole incompréhension/agacement ?
            var relativeInterval = RelativeTimeInterval.Future(TimeSpan.FromMilliseconds(5000));
            var buttonWindow = validationButtonCount.Window(relativeInterval);
            var speechWindow = speechComprehension.Window(relativeInterval);

            buttonWindow.Join(speechWindow, RelativeTimeInterval.Zero)
                .Do(fused =>
                {
                    bool buttonPressed3x = fused.Item1.Any(count => count >= 3);
                    bool speechIncomprehension = fused.Item2.Any(s => 
                        s == SpeechComprehensionState.Incomprehended || 
                        s == SpeechComprehensionState.Annoyance);

                    if (buttonPressed3x && speechIncomprehension)
                    {
                        // R4: Alpha
                        EmitClassification(ClassificationType.Alpha, "Button pressed 3x with speech incomprehension/annoyance", timestamp);
                    }
                    else
                    {
                        // F10: Appuie sur différent bouton du générateur ET parole incompréhension / agacement ?
                        CheckDifferentButtonAndSpeech(timestamp);
                    }
                });
        }

        private void CheckDifferentButtonAndSpeech(DateTime timestamp)
        {
            // F10: Appuie sur différent bouton du générateur ET parole incompréhension / agacement ?
            var relativeInterval = RelativeTimeInterval.Future(TimeSpan.FromMilliseconds(5000));
            var buttonWindow = differentButtonPressed.Window(relativeInterval);
            var speechWindow = speechComprehension.Window(relativeInterval);

            buttonWindow.Join(speechWindow, RelativeTimeInterval.Zero)
                .Do(fused =>
                {
                    bool differentButtonPressed = fused.Item1.Any();
                    bool speechIncomprehension = fused.Item2.Any(s => 
                        s == SpeechComprehensionState.Incomprehended || 
                        s == SpeechComprehensionState.Annoyance);

                    if (differentButtonPressed && speechIncomprehension)
                    {
                        // R5: Beta
                        EmitClassification(ClassificationType.Beta, "Different button pressed with speech incomprehension/annoyance", timestamp);
                    }
                    else
                    {
                        // F11: Fermeture de la porte ?
                        CheckDoorClosureFinal(timestamp);
                    }
                });
        }

        private void CheckDoorClosureFinal(DateTime timestamp)
        {
            // F11: Fermeture de la porte ?
            doorClosed
                .Window(RelativeTimeInterval.Future(TimeSpan.FromMilliseconds(5000)))
                .Do(window =>
                {
                    bool doorWasClosed = window.Any();
                    if (doorWasClosed)
                    {
                        // R6: Gamma
                        EmitClassification(ClassificationType.Gamma, "Door closed (final check)", timestamp);
                    }
                    else
                    {
                        // Loop back to F9
                        CheckButtonPressAndSpeech(timestamp);
                    }
                });
        }

        private void EmitClassification(ClassificationType type, string reason, DateTime timestamp)
        {
            var result = new ClassificationResult
            {
                Timestamp = timestamp,
                Classification = type,
                Reason = reason
            };

            output.Post(result, timestamp);
        }
    }
}

