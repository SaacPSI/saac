// <copyright file="VerbalizationDetectorComponent.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;
using Microsoft.Psi.Data.Annotations;
using Microsoft.Psi.Speech;
using SAAC.PsiFormats;

namespace SAAC.CollaborationIndices
{
    public static class ConsoleColors
    {
        public const string Reset = "\u001b[0m";
        public const string Red = "\u001b[31m";
        public const string Green = "\u001b[32m";
        public const string Yellow = "\u001b[33m";
        public const string Blue = "\u001b[34m";
        public const string Magenta = "\u001b[35m";
        public const string Cyan = "\u001b[36m";

        // 256-colour and truecolor if you need more participants
        public static string Fg(int r, int g, int b) => $"\u001b[38;2;{r};{g};{b}m";
    }

    public class VerbalizationDetectorConfiguration
    {
        public List<uint> ParticipantIds { get; set; } = new List<uint>();

        /// <summary>Transcriptions containing one of these markers are ignored (noise tags such as "(laughs)", "[music]").</summary>
        public List<string> ExcludedMarkers { get; set; } = new List<string> { "(", "[" };

        /// <summary>Transcriptions shorter than this are ignored. Zero disables the filter.</summary>
        public TimeSpan MinimumDuration { get; set; } = TimeSpan.Zero;

        /// <summary>Track read from a TimeIntervalAnnotationSet when annotations are used instead of STT.</summary>
        public string AnnotationTrack { get; set; } = "Track:0";

        /// <summary>Also publish the legacy SpeakingTimeIDData type, for the components not yet migrated.</summary>
        public bool PublishLegacyType { get; set; } = true;

        public bool LogToConsole { get; set; } = true;
    }

    /// <summary>
    /// Turns speech recognition results into speech intervals, for any number of participants.
    ///
    /// Replaces AddVerbalization, which was instantiated once per participant with its
    /// userID in the configuration. Here one component serves the whole group:
    /// GetSttInput(participantId) gives the receiver of each participant.
    ///
    /// Three behaviours differ from the original, each of them fixing a defect:
    ///  1. the task window is driven by two separate receivers (TaskStartIn / TaskEndIn).
    ///     The original had a single TaskEventIn whose handler set startTask on the first
    ///     message and endTask on every following one, so a second event silently stopped
    ///     every verbalization from being published;
    ///  2. a null Duration on the recognition result is skipped instead of throwing
    ///     (result.Duration.Value was dereferenced unconditionally);
    ///  3. the marker filter is configurable rather than hard coded to "(" and "[".
    /// </summary>
    public class VerbalizationDetectorComponent
    {
        private readonly VerbalizationDetectorConfiguration configuration;
        private readonly ReceiverMap<uint, IStreamingSpeechRecognitionResult> sttReceivers;
        private readonly ReceiverMap<uint, TimeIntervalAnnotationSet> annotationReceivers;
        private readonly EmitterMap<uint, InteractionInterval> intervalEmitters;
        private readonly EmitterMap<uint, SpeakingTimeIDData> legacyEmitters;

        private readonly MonotonicPoster poster = new MonotonicPoster();

        private DateTime taskStart = DateTime.MinValue;
        private DateTime taskEnd = DateTime.MaxValue;

        public VerbalizationDetectorComponent(Pipeline pipeline, VerbalizationDetectorConfiguration configuration, string name = nameof(VerbalizationDetectorComponent))
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.Name = name;

            this.sttReceivers = new ReceiverMap<uint, IStreamingSpeechRecognitionResult>(
                pipeline, this, configuration.ParticipantIds, this.ReceiveStt, $"{name}-Stt");

            this.annotationReceivers = new ReceiverMap<uint, TimeIntervalAnnotationSet>(
                pipeline, this, configuration.ParticipantIds, this.ReceiveAnnotation, $"{name}-Annotation");

            this.intervalEmitters = new EmitterMap<uint, InteractionInterval>(
                pipeline, this, configuration.ParticipantIds, $"{name}-Speaking");

            this.legacyEmitters = new EmitterMap<uint, SpeakingTimeIDData>(
                pipeline, this, configuration.ParticipantIds, $"{name}-SpeakingLegacy");

            this.Out = pipeline.CreateEmitter<InteractionInterval>(this, $"{name}-Speaking");

            this.TaskStartIn = pipeline.CreateReceiver<DateTime>(this, (time, _) => this.taskStart = time, $"{name}-TaskStart");
            this.TaskEndIn = pipeline.CreateReceiver<DateTime>(this, (time, _) => this.taskEnd = time, $"{name}-TaskEnd");
        }

        public string Name { get; }

        /// <summary>Every speech interval, whatever the participant.</summary>
        public Emitter<InteractionInterval> Out { get; }

        public Receiver<DateTime> TaskStartIn { get; }

        public Receiver<DateTime> TaskEndIn { get; }

        /// <summary>Recognition results of one participant.</summary>
        public Receiver<IStreamingSpeechRecognitionResult> GetSttInput(uint participantId) => this.sttReceivers[participantId];

        /// <summary>Manual annotations of one participant, as an alternative to STT.</summary>
        public Receiver<TimeIntervalAnnotationSet> GetAnnotationInput(uint participantId) => this.annotationReceivers[participantId];

        /// <summary>Speech intervals of one participant.</summary>
        public Emitter<InteractionInterval> GetSpeakingEmitter(uint participantId) => this.intervalEmitters[participantId];

        /// <summary>Legacy typed stream of one participant, for the components not yet migrated.</summary>
        public Emitter<SpeakingTimeIDData> GetLegacySpeakingEmitter(uint participantId) => this.legacyEmitters[participantId];

        private void ReceiveStt(uint participantId, IStreamingSpeechRecognitionResult result, Envelope envelope)
        {
            if (result == null || !result.Duration.HasValue || string.IsNullOrWhiteSpace(result.Text))
            {
                return;
            }

            if (this.configuration.ExcludedMarkers != null && this.configuration.ExcludedMarkers.Any(marker => result.Text.Contains(marker)))
            {
                return;
            }

            this.Publish(participantId, envelope.OriginatingTime - result.Duration.Value, envelope.OriginatingTime, result.Text, envelope);
        }

        private void ReceiveAnnotation(uint participantId, TimeIntervalAnnotationSet annotation, Envelope envelope)
        {
            if (annotation == null || !annotation.ContainsTrack(this.configuration.AnnotationTrack))
            {
                return;
            }

            TimeSpan span = annotation[this.configuration.AnnotationTrack].Interval.Span;
            this.Publish(participantId, envelope.OriginatingTime - span, envelope.OriginatingTime, string.Empty, envelope);
        }

        private void Publish(uint participantId, DateTime start, DateTime end, string text, Envelope envelope)
        {
            if (envelope.OriginatingTime < this.taskStart || envelope.OriginatingTime > this.taskEnd)
            {
                return;
            }

            TimeSpan duration = end - start;
            if (duration <= TimeSpan.Zero || duration < this.configuration.MinimumDuration)
            {
                return;
            }

            var interval = new InteractionInterval(start, end, IndexCategories.Speaking, participantId, null, text ?? string.Empty);

            // Out is written from every participant's receiver, so it needs the guard.
            this.poster.Post(this.Out, interval, envelope.OriginatingTime);
            this.poster.Post(this.intervalEmitters[participantId], interval, envelope.OriginatingTime);

            if (this.configuration.PublishLegacyType)
            {
                var legacy = new SpeakingTimeIDData((int)participantId, start, end, duration.TotalSeconds, text ?? string.Empty, false);
                this.poster.Post(this.legacyEmitters[participantId], legacy, envelope.OriginatingTime);
            }

            if (this.configuration.LogToConsole)
            {
                if (participantId == 0)
                {
                    Console.WriteLine($"\n{ConsoleColors.Magenta}Speech {participantId + 1}: {start:HH:mm:ss.fff} -> {end:HH:mm:ss.fff} ({duration.TotalSeconds:0.00}s){ConsoleColors.Reset}");
                }
                else if (participantId == 1)
                {
                    Console.WriteLine($"\n{ConsoleColors.Cyan}Speech {participantId + 1}: {start:HH:mm:ss.fff} -> {end:HH:mm:ss.fff} ({duration.TotalSeconds:0.00}s){ConsoleColors.Reset}");
                }
            }
        }
    }
}
