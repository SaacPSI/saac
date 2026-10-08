// <copyright file="SpeechFromAudio.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Speech;
using SAAC.PipelineServices;
using SAAC.Whisper;
using Whisper.net.Ggml;

namespace SAAC.CollaborationIndices.Verbal
{
    /// <summary>
    /// Settings of the transcription computed from the audio.
    /// </summary>
    public class SpeechFromAudioConfiguration
    {
        /// <summary>Gets or sets the folder of the Whisper models. A model that is not there is downloaded into it.</summary>
        public string WhisperModelDirectory { get; set; } = string.Empty;

        /// <summary>Gets or sets the Whisper model, by the name of its <c>GgmlType</c>: TinyEn, Tiny, Base, Small, Medium...</summary>
        public string WhisperModel { get; set; } = "TinyEn";

        /// <summary>Gets or sets the spoken language, by its name in <c>SAAC.Whisper.Language</c>: English, French...</summary>
        public string Language { get; set; } = "English";

        /// <summary>The problems of the settings.</summary>
        /// <returns>One line per problem; empty when there is none.</returns>
        public IEnumerable<string> Validate()
        {
            if (string.IsNullOrWhiteSpace(this.WhisperModelDirectory) || !Directory.Exists(this.WhisperModelDirectory))
            {
                yield return $"the folder of the Whisper models does not exist: \"{this.WhisperModelDirectory}\"";
            }

            if (!Enum.TryParse(this.WhisperModel, true, out GgmlType _))
            {
                yield return $"\"{this.WhisperModel}\" is not a Whisper model ({string.Join(", ", Enum.GetNames(typeof(GgmlType)))})";
            }

            if (!Enum.TryParse(this.Language, true, out Language _))
            {
                yield return $"\"{this.Language}\" is not a language of the Whisper component";
            }
        }
    }

    /// <summary>
    /// Voice activity and transcription of one participant computed from the audio, for the
    /// sessions that only recorded the audio. It is <see cref="VadWhisper"/>, the component
    /// the reprocessing of the studies uses, with the same settings: the speech found is the
    /// same in both.
    ///
    /// The audio must be 16 kHz, one channel, 16 bit PCM: resample it first when it is not.
    ///
    /// <code>
    /// IProducer&lt;bool&gt; voiceActivity = SpeechFromAudio.VoiceActivity(pipeline, server, audio, 0);
    /// var transcription = SpeechFromAudio.Transcription(pipeline, server, audio, voiceActivity, 0, configuration);
    /// transcription.PipeTo(verbalization.GetSttInput(0));
    /// </code>
    /// </summary>
    public static class SpeechFromAudio
    {
        /// <summary>Voice activity of one participant, from the system voice activity detector.</summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="server">The dataset pipeline; nothing is stored through it.</param>
        /// <param name="audio">Audio of the participant: 16 kHz, one channel, 16 bit PCM.</param>
        /// <param name="participantIndex">Index of the participant, from 0.</param>
        /// <returns>True while the participant speaks, one value per audio buffer.</returns>
        public static IProducer<bool> VoiceActivity(Pipeline pipeline, DatasetPipeline server, IProducer<AudioBuffer> audio, int participantIndex)
            => new VadWhisper().SetupVad(pipeline, server, new VadWhisperConfiguration { ParticipantId = participantIndex }, audio, participantIndex, null);

        /// <summary>Transcription of one participant by Whisper, one result per utterance.</summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="server">The dataset pipeline; nothing is stored through it.</param>
        /// <param name="audio">Audio of the participant: 16 kHz, one channel, 16 bit PCM.</param>
        /// <param name="voiceActivity">Voice activity of the participant, recorded or from <see cref="VoiceActivity"/>.</param>
        /// <param name="participantIndex">Index of the participant, from 0.</param>
        /// <param name="configuration">Model and language.</param>
        /// <param name="report">Receives what Whisper says of its model when the pipeline starts: loaded, or downloaded first.</param>
        /// <returns>The final recognition results, stamped with the end of the utterance.</returns>
        public static IProducer<IStreamingSpeechRecognitionResult> Transcription(
            Pipeline pipeline, DatasetPipeline server, IProducer<AudioBuffer> audio, IProducer<bool> voiceActivity, int participantIndex, SpeechFromAudioConfiguration configuration, Action<string>? report = null)
        {
            var whisper = new WhisperSpeechRecognizerConfiguration
            {
                Language = (Language)Enum.Parse(typeof(Language), configuration.Language, true),
                ModelType = (GgmlType)Enum.Parse(typeof(GgmlType), configuration.WhisperModel, true),
                QuantizationType = QuantizationType.NoQuantization,
                ModelDirectory = configuration.WhisperModelDirectory,
            };
            if (report != null)
            {
                whisper.OnModelDownloadProgressHandler = (_, progress) => report(progress.Item2);
            }

            // Each audio buffer with the voice activity at that time: Whisper transcribes the spoken stretches.
            IProducer<(AudioBuffer, bool)> annotatedAudio = audio.Join(voiceActivity, RelativeTimeInterval.Past());
            return new VadWhisper().SetupWhisper(pipeline, server, new VadWhisperConfiguration { ParticipantId = participantIndex }, annotatedAudio, whisper, participantIndex);
        }
    }
}
