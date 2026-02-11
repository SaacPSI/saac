// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

#pragma warning disable SA1200 // Using directives should be placed correctly
using Whisper.net;
using Whisper.net.Ggml;
#pragma warning restore SA1200 // Using directives should be placed correctly

namespace SAAC.Whisper
{
    using System.Buffers;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.CompilerServices;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Whisper-based speech recognition component for Platform for Situated Intelligence.
    /// </summary>
    public sealed class WhisperSpeechRecognizer : IConsumerProducer<(AudioBuffer, bool), IStreamingSpeechRecognitionResult>, IDisposable, INotifyPropertyChanged
    {
        /// <remarks>
        /// This value should be set as low as possible, while still being high enough to ensure that gap filling is never triggered when the delivery policy is set to Unlimited.
        /// </remarks>
        private const int GapSampleThreshold = 1;

        private readonly List<Section> sections;
        private readonly List<SegmentData> segments;
        private readonly Lazy<WhisperProcessor> processor;

        private double progress = 0;
        private string? modelFilename;
        private WhisperSpeechRecognizerConfiguration configuration;
        private TimeSpan bufferedDuration = TimeSpan.Zero;
        private TimeSpan lastPartialDuration = TimeSpan.Zero;
        private string name;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WhisperSpeechRecognizer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="name">The component name.</param>
        public WhisperSpeechRecognizer(Pipeline pipeline, WhisperSpeechRecognizerConfiguration config, string name = nameof(WhisperSpeechRecognizer))
        {
            this.name = name;
            this.modelFilename = config.SpecificModelPath;
            this.processor = new Lazy<WhisperProcessor>(this.LazyInitialize);
            this.sections = new List<Section>();
            this.segments = new List<SegmentData>();

            this.In = pipeline.CreateReceiver<(AudioBuffer, bool)>(this, this.Process, $"{name}-In");
            this.PartialOut = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, $"{name}-PartialOut");
            this.FinalOut = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, $"{name}-FinalOut");
            this.Out = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, $"{name}-Out");

            this.configuration = config;

            pipeline.PipelineRun += this.OnPipelineRun;
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the current progress of speech recognition processing.
        /// </summary>
        public double Progress
        {
            get => this.progress;
            private set => this.SetProperty(ref this.progress, value);
        }

        /// <summary>
        /// Gets the input receiver for audio buffer and speech state.
        /// </summary>
        public Receiver<(AudioBuffer, bool)> In { get; }

        /// <summary>
        /// Gets the emitter for partial speech recognition results.
        /// </summary>
        public Emitter<IStreamingSpeechRecognitionResult> PartialOut { get; }

        /// <summary>
        /// Gets the emitter for final speech recognition results.
        /// </summary>
        public Emitter<IStreamingSpeechRecognitionResult> FinalOut { get; }

        /// <summary>
        /// Gets the emitter for all speech recognition results.
        /// </summary>
        public Emitter<IStreamingSpeechRecognitionResult> Out { get; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            if (this.processor.IsValueCreated)
            {
                this.processor.Value.Dispose();
            }
        }

        private static byte[] SegmentAudioBuffer(SegmentData segment, WaveFormat format, IReadOnlyList<Section> sections)
        {
            var factor = format.Channels * format.BitsPerSample / 8;

            /* Buffer Length */
            var sectionIdx = 0;
            var sectionStartTime = TimeSpan.Zero;
            var length = 0L;
            while (true)
            {
                if (sectionIdx >= sections.Count)
                {
                    break;
                }

                var section = sections[sectionIdx].Buffer;
                var sectionEndTime = sectionStartTime + section.Duration;
                if (segment.Start <= sectionEndTime && sectionStartTime < segment.End)
                {
                    long startIdx;
                    if (segment.Start <= sectionStartTime)
                    {
                        startIdx = 0;
                    }
                    else
                    {
                        startIdx = (long)((segment.Start - sectionStartTime).TotalSeconds * format.SamplesPerSec) * factor;
                    }

                    long endIdx;
                    if (sectionEndTime <= segment.End)
                    {
                        endIdx = section.Length;
                    }
                    else
                    {
                        endIdx = (long)((segment.End - sectionStartTime).TotalSeconds * format.SamplesPerSec) * factor;
                    }

                    length += endIdx - startIdx;
                }
                else if (sectionStartTime >= segment.End)
                {
                    break;
                }

                sectionIdx += 1;
                sectionStartTime = sectionEndTime;
            }

            var result = new byte[length];

            /* Copy */
            sectionIdx = 0;
            sectionStartTime = TimeSpan.Zero;
            var offset = 0L;
            while (true)
            {
                if (sectionIdx >= sections.Count)
                {
                    Debug.Assert(false, "Should not reach here.");
                    break;
                }

                var section = sections[sectionIdx].Buffer;
                var sectionEndTime = sectionStartTime + section.Duration;
                if (segment.Start <= sectionEndTime && sectionStartTime < segment.End)
                {
                    long startIdx;
                    if (segment.Start <= sectionStartTime)
                    {
                        startIdx = 0;
                    }
                    else
                    {
                        startIdx = (long)((segment.Start - sectionStartTime).TotalSeconds * format.SamplesPerSec) * factor;
                    }

                    long endIdx;
                    if (sectionEndTime <= segment.End)
                    {
                        endIdx = section.Length;
                    }
                    else
                    {
                        endIdx = (long)((segment.End - sectionStartTime).TotalSeconds * format.SamplesPerSec) * factor;
                    }

                    var bytes = endIdx - startIdx;
                    Array.Copy(section.Data, startIdx, result, offset, bytes);
                    offset += bytes;
                    if (offset >= length)
                    {
                        Debug.Assert(offset == length, "Offset should equal length.");
                        break;
                    }
                }
                else if (sectionStartTime >= segment.End)
                {
                    Debug.Assert(false, "Should not reach here.");
                    break;
                }

                sectionIdx += 1;
                sectionStartTime = sectionEndTime;
            }

            return result;
        }

        private static string GetTypeModelFileName(GgmlType modelType) => modelType switch
        {
            GgmlType.Tiny => "tiny__v1",
            GgmlType.TinyEn => "tiny_en__v1",
            GgmlType.Base => "base__v1",
            GgmlType.BaseEn => "base_en__v1",
            GgmlType.Small => "small__v1",
            GgmlType.SmallEn => "small_en__v1",
            GgmlType.Medium => "medium__v1",
            GgmlType.MediumEn => "medium_en__v1",
            GgmlType.LargeV1 => "large__v1",
            GgmlType.LargeV2 => "large__v2",
            _ => throw new InvalidOperationException(),
        };

        private static string GetQuantizationModelFileName(QuantizationType quantizationType) => quantizationType switch
        {
            QuantizationType.NoQuantization => "classic",
            QuantizationType.Q4_0 => "q4_0",
            QuantizationType.Q4_1 => "q4_1",
            QuantizationType.Q5_0 => "q5_0",
            QuantizationType.Q5_1 => "q5_1",
            QuantizationType.Q8_0 => "q8_0",
            _ => throw new InvalidOperationException(),
        };

        private static string GetLanguageCode(Language language) => language switch
        {
            Language.NotSet => "auto",
            Language.Afrikaans => "af",
            Language.Arabic => "ar",
            Language.Armenian => "hy",
            Language.Azerbaijani => "az",
            Language.Belarusian => "be",
            Language.Bosnian => "bs",
            Language.Bulgarian => "bg",
            Language.Catalan => "ca",
            Language.Chinese => "zh",
            Language.Croatian => "hr",
            Language.Czech => "cs",
            Language.Danish => "da",
            Language.Dutch => "nl",
            Language.English => "en",
            Language.Estonian => "et",
            Language.Finnish => "fi",
            Language.French => "fr",
            Language.Galician => "gl",
            Language.German => "de",
            Language.Greek => "el",
            Language.Hebrew => "he",
            Language.Hindi => "hi",
            Language.Hungarian => "hu",
            Language.Icelandic => "is",
            Language.Indonesian => "id",
            Language.Italian => "it",
            Language.Japanese => "ja",
            Language.Kannada => "kn",
            Language.Kazakh => "kk",
            Language.Korean => "ko",
            Language.Latvian => "lv",
            Language.Lithuanian => "lt",
            Language.Macedonian => "mk",
            Language.Malay => "ms",
            Language.Marathi => "mr",
            Language.Maori => "mi",
            Language.Nepali => "ne",
            Language.Norwegian => "no",
            Language.Persian => "fa",
            Language.Polish => "pl",
            Language.Portuguese => "pt",
            Language.Romanian => "ro",
            Language.Russian => "ru",
            Language.Serbian => "sr",
            Language.Slovak => "sk",
            Language.Slovenian => "sl",
            Language.Spanish => "es",
            Language.Swahili => "sw",
            Language.Swedish => "sv",
            Language.Tagalog => "tl",
            Language.Tamil => "ta",
            Language.Thai => "th",
            Language.Turkish => "tr",
            Language.Ukrainian => "uk",
            Language.Urdu => "ur",
            Language.Vietnamese => "vi",
            Language.Welsh => "cy",
            _ => throw new InvalidOperationException(),
        };

        private void OnPipelineRun(object sender, PipelineRunEventArgs args)
        {
            using var tokenSource = new CancellationTokenSource();
            var t = Task.Factory.StartNew(this.DownloadAsync, tokenSource.Token).Result;
            TimeSpan downloadTimeout = TimeSpan.FromSeconds(this.configuration.DownloadTimeoutInSeconds * 100);
            var timeout = (int)downloadTimeout.TotalMilliseconds;
            var succeed = t.Wait(timeout);
            if (!succeed)
            {
                tokenSource.Cancel();
                t.Wait();
                this.configuration.OnModelDownloadProgressHandler?.Invoke(this, (WhisperSpeechRecognizerConfiguration.EWhisperModelDownloadState.Completed, "Download Whisper model timeout."));
                throw new TimeoutException("Download Whisper model timeout.");
            }
        }

        private async Task DownloadAsync(object state)
        {
            if (this.modelFilename is null)
            {
                var modelType = this.configuration.ModelType;
                var quantizationType = this.configuration.QuantizationType;
                var cancellationToken = (CancellationToken)state;
                var fn = string.Join("__", "ggml", GetTypeModelFileName(modelType), GetQuantizationModelFileName(quantizationType)) + ".bin";
                this.modelFilename = Path.Combine(this.configuration.ModelDirectory, fn);
                if (this.configuration.ForceDownload || !File.Exists(this.modelFilename))
                {
                    try
                    {
                        this.configuration.OnModelDownloadProgressHandler?.Invoke(this, (WhisperSpeechRecognizerConfiguration.EWhisperModelDownloadState.InProgress, "Starting download Whisper model..."));
                        Console.WriteLine("Downloading Whisper model.");
                        using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(modelType, quantizationType, cancellationToken);
                        using var fileWriter = File.OpenWrite(this.modelFilename);
                        const int bufferSize = 32 * 1024 * 1024;
                        await modelStream.CopyToAsync(fileWriter, bufferSize, cancellationToken);
                        Console.WriteLine("Downloaded Whisper model.");
                        this.configuration.OnModelDownloadProgressHandler?.Invoke(this, (WhisperSpeechRecognizerConfiguration.EWhisperModelDownloadState.Completed, "Downloaded !"));
                    }
                    catch (OperationCanceledException ex)
                    {
                        Console.WriteLine(ex.Message);
                        File.Delete(this.modelFilename);
                        this.configuration.OnModelDownloadProgressHandler?.Invoke(this, (WhisperSpeechRecognizerConfiguration.EWhisperModelDownloadState.Failed, $"Failed to downloaded Whisper model : {ex.Message}"));
                    }
                }
            }

            if (!this.configuration.LazyInitialization)
            {
                _ = this.processor.Value;
            }
        }

        private WhisperProcessor LazyInitialize()
        {
            Debug.Assert(this.modelFilename is not null, "Model filename should not be null.");
            if (this.modelFilename is null)
            {
                this.configuration.OnModelDownloadProgressHandler?.Invoke(this, (WhisperSpeechRecognizerConfiguration.EWhisperModelDownloadState.Failed, $"Whisper model file is null !"));
                throw new InvalidOperationException();
            }

            if (!File.Exists(this.modelFilename))
            {
                this.configuration.OnModelDownloadProgressHandler?.Invoke(this, (WhisperSpeechRecognizerConfiguration.EWhisperModelDownloadState.Failed, $"Whisper model file not exist : {this.modelFilename}"));
                throw new FileNotFoundException("Whisper model file not exist.", this.modelFilename);
            }

            var code = GetLanguageCode(this.configuration.Language);
            var builder = WhisperFactory
                .FromPath(this.modelFilename)
                .CreateBuilder()
                .WithLanguage(code)
                .WithProgressHandler(this.OnProgress)
                .WithSegmentEventHandler(this.OnSegment)
                .WithProbabilities()
                .WithTokenTimestamps();

            var prompt = this.configuration.Prompt;
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                builder.WithPrompt(prompt!);
            }

            switch (this.configuration.SegmentationRestriction)
            {
                case SegmentationRestriction.OnePerWord:
                    builder.SplitOnWord();
                    break;
                case SegmentationRestriction.OnePerUtterence:
                    builder.WithSingleSegment();
                    break;
            }

            var result = builder.Build();

            Console.WriteLine("Whisper model is loaded.");
            this.configuration.OnModelDownloadProgressHandler?.Invoke(this, (WhisperSpeechRecognizerConfiguration.EWhisperModelDownloadState.Completed, "Loaded !"));
            return result;
        }

        private void Process((AudioBuffer, bool) frame, Envelope envelope)
        {
            var (data, state) = frame;

            if (state)
            {
                this.AppendAudio(data, envelope.OriginatingTime);
                TimeSpan partialEvalueationInverval = TimeSpan.FromSeconds(this.configuration.PartialEvalueationInvervalInSeconds);
                if (this.configuration.OutputPartialResults && this.bufferedDuration - this.lastPartialDuration >= partialEvalueationInverval)
                {
                    this.lastPartialDuration = this.bufferedDuration;
                    this.ProcessAndPost(isFinal: false);
                }

                return;
            }

            this.lastPartialDuration = TimeSpan.Zero;
            this.ProcessAndPost(isFinal: true);
        }

        private void AppendAudio(AudioBuffer data, DateTime timestamp)
        {
            var inputTimeMode = this.configuration.InputTimestampMode;

            Debug.Assert(timestamp.Kind == DateTimeKind.Utc, "Timestamp should be UTC.");
            if (!data.HasValidData)
            {
                return;
            }

            if (data.Format is not { FormatTag: WaveFormatTag.WAVE_FORMAT_PCM, SamplesPerSec: 16_000, })
            {
                throw new Exception("Please use 16kHz PCM audio as Whisper's input.");
            }

            if (data.Format.BitsPerSample != 16)
            {
                throw new Exception("Only 16-bit PCM audio is currently supported.");
            }

            if (this.sections.Count > 0)
            {
                var format = this.sections[0].Buffer.Format;
                if (!data.Format.Equals(format))
                {
                    throw new Exception("Audio format mismatch.");
                }
            }

            var newSection = new Section(data.DeepClone(), timestamp, data.Duration);

            if (this.sections.Count > 0)
            {
                var last = this.sections.Last();
                var blankTime = inputTimeMode switch
                {
                    TimestampMode.AtEnd => (newSection.OriginatingTime - newSection.Buffer.Duration) - last.OriginatingTime,
                    TimestampMode.AtStart => newSection.OriginatingTime - (last.OriginatingTime + last.Buffer.Duration),
                    _ => throw new InvalidOperationException(),
                };
                var format = this.sections[0].Buffer.Format;
                var missedSamples = (int)(blankTime.TotalSeconds * format.SamplesPerSec);
                if (missedSamples > GapSampleThreshold)
                {
                    var factor = format.Channels * format.BitsPerSample / 8;
                    var gapBufferLength = missedSamples * factor;
                    var gap = new AudioBuffer(gapBufferLength, format);
                    var gapTimestamp = inputTimeMode switch
                    {
                        TimestampMode.AtEnd => last.OriginatingTime + gap.Duration,
                        TimestampMode.AtStart => newSection.OriginatingTime - gap.Duration,
                        _ => throw new InvalidOperationException(),
                    };
                    var gapSection = new Section(gap, gapTimestamp, gap.Duration);

                    this.sections.Add(gapSection);
                    this.bufferedDuration += gapSection.Buffer.Duration;
                }
            }

            this.sections.Add(newSection);
            this.bufferedDuration += newSection.Buffer.Duration;

            Debug.Assert(this.bufferedDuration <= TimeSpan.FromSeconds(30), "Buffered duration should not exceed 30 seconds.");
        }

        private void ProcessAndPost(bool isFinal)
        {
            if (this.sections.Count <= 0)
            {
                return;
            }

            var inputTimeMode = this.configuration.InputTimestampMode;
            var outputTimeMode = this.configuration.OutputTimestampMode;
            var processorValue = this.processor.Value;
            var format = this.sections[0].Buffer.Format;
            var factor = format.Channels * format.BitsPerSample / 8;
            var size = this.sections.Sum(s => s.Buffer.Length) / factor;
            var samples = ArrayPool<float>.Shared.Rent(size);
            try
            {
                var sampleOffset = 0;
                foreach (var section in this.sections)
                {
                    var bufferOffset = 0;
                    while (bufferOffset < section.Buffer.Length)
                    {
                        var channelIdx = 0;
                        long sum = 0;
                        while (channelIdx < section.Buffer.Format.Channels)
                        {
                            switch (section.Buffer.Format.BitsPerSample)
                            {
                                case 16:
                                    sum += BitConverter.ToInt16(section.Buffer.Data, bufferOffset);
                                    bufferOffset += 2;
                                    break;
                                default:
                                    throw new InvalidOperationException("Not supported bit-depth.");
                            }

                            channelIdx += 1;
                        }

                        samples[sampleOffset] = sum / (float)section.Buffer.Format.Channels / (short.MaxValue + 1);
                        sampleOffset += 1;
                    }

                    Debug.Assert(bufferOffset == section.Buffer.Length, "Buffer offset should equal buffer length.");
                }

                Debug.Assert(sampleOffset == size, "Sample offset should equal size.");

                var valid = samples.AsSpan(0, size);
                processorValue.Process(valid);

                if (this.segments.Count == 0)
                {
                    return;
                }

                var firstSection = this.sections.First();
                Debug.Assert(Math.Abs(this.bufferedDuration.TotalMilliseconds - this.sections.Aggregate(TimeSpan.Zero, (v, s) => v + s.Buffer.Duration).TotalMilliseconds) < 1, "Buffered duration mismatch.");
                foreach (var segment in this.segments)
                {
                    var text = segment.Text;
                    var confidence = segment.Probability;
                    var actualEnd = segment.End > this.bufferedDuration ? this.bufferedDuration : segment.End;
                    var duration = actualEnd - segment.Start;
                    AudioBuffer? audio;
                    if (!this.configuration.OutputAudio)
                    {
                        audio = null;
                    }
                    else
                    {
                        var audioBuffer = SegmentAudioBuffer(segment, format, this.sections);
                        audio = new AudioBuffer(audioBuffer, format);
                    }

                    var result = new StreamingSpeechRecognitionResult(isFinal, text, confidence, Enumerable.Empty<SpeechRecognitionAlternate>(), audio, duration);
                    var timestamp = inputTimeMode switch
                    {
                        TimestampMode.AtStart => firstSection.OriginatingTime,
                        TimestampMode.AtEnd => firstSection.OriginatingTime - firstSection.Duration,
                        _ => throw new InvalidOperationException(),
                    }
                    + outputTimeMode switch
                    {
                        TimestampMode.AtStart => segment.Start,
                        TimestampMode.AtEnd => actualEnd,
                        _ => throw new InvalidOperationException(),
                    };

                    switch (isFinal)
                    {
                        case false:
                            this.SafePost(this.PartialOut, result, timestamp);
                            break;
                        case true:
                            this.SafePost(this.FinalOut, result, timestamp);
                            break;
                    }

                    this.SafePost(this.Out, result, timestamp);
                }
            }
            finally
            {
                ArrayPool<float>.Shared.Return(samples);
                if (isFinal)
                {
                    this.sections.Clear();
                    this.bufferedDuration = TimeSpan.Zero;
                }

                this.segments.Clear();
            }
        }

        private void OnSegment(SegmentData segment)
        {
            this.segments.Add(segment);
        }

        private void OnProgress(int progress)
        {
            this.Progress = progress;
        }

        private void SafePost(Emitter<IStreamingSpeechRecognitionResult> emitter, IStreamingSpeechRecognitionResult data, DateTime timestamp)
        {
            var minTimestamp = emitter.LastEnvelope.OriginatingTime + TimeSpan.FromMilliseconds(1);
            if (timestamp < minTimestamp)
            {
                timestamp = minTimestamp;
            }

            emitter.Post(data, timestamp);
        }

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private readonly struct Section
        {
            public readonly AudioBuffer Buffer;
            public readonly DateTime OriginatingTime;
            public readonly TimeSpan Duration;

            public Section(AudioBuffer buffer, DateTime originatingTime, TimeSpan duration)
            {
                this.Buffer = buffer;
                this.OriginatingTime = originatingTime;
                this.Duration = duration;
            }
        }
    }
}