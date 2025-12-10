using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Components;
using Microsoft.Psi.Speech;
using Whisper.net;
using Whisper.net.Ggml;
using static System.Net.Mime.MediaTypeNames;

namespace SAAC.Whisper
{
    public sealed class WhisperSpeechRecognizer : IConsumerProducer<(AudioBuffer, bool), IStreamingSpeechRecognitionResult>, IDisposable, INotifyPropertyChanged {

        /// <remarks>
        /// This value should be set as low as possible, while still being high enough to ensure that gap filling is never triggered when the delivery policy is set to Unlimited.
        /// </remarks>
        private const int GapSampleThreshold = 1;

        private readonly List<Section> sections;

        private readonly List<SegmentData> segments;

        private readonly Lazy<WhisperProcessor> processor;

        #region Options
        /*private string ModelDirectory = "";

        public string ModelDirectory {
            get => ModelDirectory;
            set => SetProperty(ref ModelDirectory, value);
        }

        private GgmlType ModelType = GgmlType.BaseEn;

        public GgmlType ModelType {
            get => ModelType;
            set => SetProperty(ref ModelType, value);
        }

        private QuantizationType QuantizationType = QuantizationType.Q5_1;

        public QuantizationType QuantizationType {
            get => QuantizationType;
            set => SetProperty(ref QuantizationType, value);
        }

        private bool ForceDownload = false;

        public bool ForceDownload {
            get => ForceDownload;
            set => SetProperty(ref ForceDownload, value);
        }

        private TimeSpan downloadTimeout = TimeSpan.FromSeconds(15);

        public TimeSpan DownloadTimeout {
            get => downloadTimeout;
            set => SetProperty(ref downloadTimeout, value);
        }

        private bool LazyInitialization = false;

        public bool LazyInitialization {
            get => LazyInitialization;
            set => SetProperty(ref LazyInitialization, value);
        }

        private Language Language = Language.English;

        public Language Language {
            get => Language;
            set => SetProperty(ref Language, value);
        }

        private string Prompt = "";

        public string Prompt {
            get => Prompt;
            set => SetProperty(ref Prompt, value);
        }

        private SegmentationRestriction SegmentationRestriction = SegmentationRestriction.OnePerUtterence;

        public SegmentationRestriction SegmentationRestriction {
            get => SegmentationRestriction;
            set => SetProperty(ref SegmentationRestriction, value);
        }

        private TimestampMode InputTimestampMode = TimestampMode.AtEnd;//\psi convention

        public TimestampMode InputTimestampMode {
            get => InputTimestampMode;
            set => SetProperty(ref InputTimestampMode, value);
        }

        private TimestampMode OutputTimestampMode = TimestampMode.AtEnd;

        public TimestampMode OutputTimestampMode {
            get => OutputTimestampMode;
            set => SetProperty(ref OutputTimestampMode, value);
        }

        private bool OutputPartialResults = false;

        public bool OutputPartialResults {
            get => OutputPartialResults;
            set => SetProperty(ref OutputPartialResults, value);
        }

        private TimeSpan partialEvalueationInverval = TimeSpan.FromMilliseconds(500);

        public TimeSpan PartialEvalueationInverval {
            get => partialEvalueationInverval;
            set => SetProperty(ref partialEvalueationInverval, value);
        }

        private bool OutputAudio = false;

        public bool OutputAudio {
            get => OutputAudio;
            set => SetProperty(ref OutputAudio, value);
        }

        private ILogger? logger;

        public ILogger? Logger {
            get => logger;
            set => SetProperty(ref logger, value);
        }*/
        #endregion

        private double progress = 0;

        public double Progress {
            get => progress;
            private set => SetProperty(ref progress, value);
        }

        #region Ports
        public Receiver<(AudioBuffer, bool)> In { get; }

        public Emitter<IStreamingSpeechRecognitionResult> PartialOut { get; }

        public Emitter<IStreamingSpeechRecognitionResult> FinalOut { get; }

        public Emitter<IStreamingSpeechRecognitionResult> Out { get; } 
        #endregion

        private string? modelFilename;
        private WhisperSpeechRecognizerConfiguration configuration;
        private TimeSpan bufferedDuration = TimeSpan.Zero;
        private TimeSpan lastPartialDuration = TimeSpan.Zero;
        private string name;
        Dictionary<int, List<double>> filteredEnergy = new Dictionary<int, List<double>>();


        public WhisperSpeechRecognizer(Pipeline pipeline, WhisperSpeechRecognizerConfiguration config, string name = nameof(WhisperSpeechRecognizer)) 
        {
            this.name = name;
            processor = new Lazy<WhisperProcessor>(LazyInitialize); 
            sections = new List<Section>();
            segments = new List<SegmentData>();

            In = pipeline.CreateReceiver<(AudioBuffer, bool)>(this, Process, $"{name}-In");
            PartialOut = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, $"{name}-PartialOut");
            FinalOut = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, $"{name}-FinalOut");
            Out = pipeline.CreateEmitter<IStreamingSpeechRecognitionResult>(this, $"{name}-Out");

            filteredEnergy[0] = new List<double>();
            filteredEnergy[1] = new List<double>();
            filteredEnergy[2] = new List<double>();

            configuration = config;

            pipeline.PipelineRun += OnPipelineRun;
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void OnPipelineRun(object sender, PipelineRunEventArgs args)
        {
            using var tokenSource = new CancellationTokenSource(/*-1*/);
            var t = Task.Factory.StartNew(DownloadAsync, tokenSource.Token).Result;//Put on a worker thread. Otherwise, the pipeline will be blocked.
            TimeSpan DownloadTimeout = TimeSpan.FromSeconds(configuration.DownloadTimeoutInSeconds * 100);
            var timeout = (int)DownloadTimeout.TotalMilliseconds;
            var succeed = t.Wait(timeout);
            if (!succeed)
            {
                tokenSource.Cancel();
                t.Wait();//Wait deletion to complete
                throw new TimeoutException("Download Whisper model timeout.");
            }
        }

        private async Task DownloadAsync(object state)
        {
            var cancellationToken = (CancellationToken)state;
            var modelType = configuration.ModelType;
            var quantizationType = configuration.QuantizationType;
            var fn = string.Join("__", "ggml", GetTypeModelFileName(modelType), GetQuantizationModelFileName(quantizationType)) + ".bin";
            modelFilename = Path.Combine(configuration.ModelDirectory, fn);
            if (configuration.ForceDownload || !File.Exists(modelFilename))
            {
                try
                {
                    Console.WriteLine("Downloading Whisper model.");
                    using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(modelType, quantizationType, cancellationToken);
                    using var fileWriter = File.OpenWrite(modelFilename);
                    const int bufferSize = 32 * 1024 * 1024;
                    await modelStream.CopyToAsync(fileWriter, bufferSize, cancellationToken);//TaskCanceledExpcetion will be thrown at here if canceled
                    Console.WriteLine("Downloaded Whisper model.");
                }
                catch (OperationCanceledException ex)
                {
                    Console.WriteLine(ex.Message);
                    File.Delete(modelFilename);//Delete incomplete file

                }
            }
            if (!configuration.LazyInitialization)
            {
                _ = processor.Value;
            }
        }

        private WhisperProcessor LazyInitialize()
        {
            Debug.Assert(modelFilename is not null);
            if (modelFilename is null)
            {
                throw new InvalidOperationException();
            }
            if (!File.Exists(modelFilename))
            {
                throw new FileNotFoundException("Whisper model file not exist.", modelFilename);
            }
            var code = GetLanguageCode(configuration.Language);
            var builder = WhisperFactory
                .FromPath(modelFilename/*, false*/)
                .CreateBuilder()
                .WithLanguage(code)
                .WithProgressHandler(OnProgress)
                .WithSegmentEventHandler(OnSegment)
                .WithProbabilities()
                .WithTokenTimestamps()
                ;
            var prompt = configuration.Prompt;
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                builder.WithPrompt(prompt!);
            }
            switch (configuration.SegmentationRestriction)
            {
                case SegmentationRestriction.OnePerWord:
                    builder.SplitOnWord();//TODO: not working?
                    break;
                case SegmentationRestriction.OnePerUtterence:
                    builder.WithSingleSegment();
                    break;
            }
            var result = builder.Build();
            //Logger?.LogInformation("Whisper model is loaded.");
            Console.WriteLine("Whisper model is loaded.");
            return result;
        }

        private void Process((AudioBuffer, bool) frame, Envelope envelope)
        {
            var (data, state) = frame;

            /* Append Data */
            if (state)
            {
                AppendAudio(data, envelope.OriginatingTime);
                /* Post Partial */
                TimeSpan PartialEvalueationInverval = TimeSpan.FromSeconds(configuration.PartialEvalueationInvervalInSeconds);
                if (configuration.OutputPartialResults && bufferedDuration - lastPartialDuration >= PartialEvalueationInverval)
                {
                    lastPartialDuration = bufferedDuration;
                    ProcessAndPost(isFinal: false);
                }
                return;
            }

            /* Post Final*/
            lastPartialDuration = TimeSpan.Zero;
            ProcessAndPost(isFinal: true);
        }

        private void AppendAudio(AudioBuffer data, DateTime timestamp)
        {
            var inputTimeMode = configuration.InputTimestampMode;

            /* Check Format */
            Debug.Assert(timestamp.Kind == DateTimeKind.Utc);
            if (!data.HasValidData)
            {
                return;
            }
            if (data.Format is not { FormatTag: WaveFormatTag.WAVE_FORMAT_PCM, SamplesPerSec: 16_000, })
            {//AudioResampler is platform dependent, so we are not silently resample here
                throw new Exception("Please use 16kHz PCM audio as Whisper's input.");
            }
            if (data.Format.BitsPerSample != 16)
            {
                throw new Exception("Only 16-bit PCM audio is currently supported.");//TODO: 24-bit on demand
            }
            if (sections.Count > 0)
            {
                var format = sections[0].Buffer.Format;
                if (!data.Format.Equals(format))
                {
                    throw new Exception("Audio format mismatch.");
                }
            }

            var newSection = new Section(data.DeepClone(), timestamp, data.Duration);

            /* Fill Gap */
            if (sections.Count > 0)
            {
                var last = sections.Last();
                var blankTime = inputTimeMode switch
                {
                    TimestampMode.AtEnd => (newSection.OriginatingTime - newSection.Buffer.Duration) - last.OriginatingTime,
                    TimestampMode.AtStart => newSection.OriginatingTime - (last.OriginatingTime + last.Buffer.Duration),
                    _ => throw new InvalidOperationException(),
                };
                var format = sections[0].Buffer.Format;
                var missedSamples = (int)(blankTime.TotalSeconds * format.SamplesPerSec);
                if (missedSamples > GapSampleThreshold)
                {
                    var factor = format.Channels * format.BitsPerSample / 8;
                    var gapBufferLength = missedSamples * factor;
                    var gap = new AudioBuffer(gapBufferLength, format);//TODO: mark in Section only, do not actually allocate memory
                    var gapTimestamp = inputTimeMode switch
                    {
                        TimestampMode.AtEnd => last.OriginatingTime + gap.Duration,
                        TimestampMode.AtStart => newSection.OriginatingTime - gap.Duration,
                        _ => throw new InvalidOperationException(),
                    };
                    var gapSection = new Section(gap, gapTimestamp, gap.Duration);

                    sections.Add(gapSection);
                    bufferedDuration += gapSection.Buffer.Duration;
                }
            }

            sections.Add(newSection);
            bufferedDuration += newSection.Buffer.Duration;

            Debug.Assert(bufferedDuration <= TimeSpan.FromSeconds(30));//TODO: we haven't tested what will happen if we feed more than 30 seconds of audio
        }

        /*private void ProcessAndPost(bool isFinal)
        {
            if (sections.Count <= 0)
            {
                return;
            }

            var inputTimeMode = configuration.InputTimestampMode;
            var outputTimeMode = configuration.OutputTimestampMode;
            var processorValue = processor.Value;
            var format = sections[0].Buffer.Format;
            var factor = format.Channels * format.BitsPerSample / 8;
            int numChannels = sections[0].Buffer.Format.Channels;
            //var size = sections.Sum(s => s.Buffer.Length) / factor;
            var size = sections.Sum(s => s.Buffer.Length) / (2 * numChannels);
            var samples = ArrayPool<float>.Shared.Rent(size);
            var samplesPerChannel = new float[numChannels][];

            try
            {
                // Merge audio into float array
                var sampleOffset = 0;
                foreach (var section in sections)
                {
                    var bufferOffset = 0;
                    while (bufferOffset < section.Buffer.Length)
                    {
                        var sum = 0L;
                        for (int ch = 0; ch < section.Buffer.Format.Channels; ch++)
                        {
                            short sample = BitConverter.ToInt16(section.Buffer.Data, bufferOffset);
                            sum += sample;
                            bufferOffset += 2;
                        }
                        samples[sampleOffset++] = sum / (float)section.Buffer.Format.Channels / (short.MaxValue + 1);
                    }
                }

                var valid = samples.AsSpan(0, size);
                processorValue.Process(valid);

                if (segments.Count == 0) return;

                var firstSection = sections.First();
                foreach (var segment in segments)
                {
                    var text = segment.Text;
                    var confidence = segment.Probability;
                    var actualEnd = segment.End > bufferedDuration ? bufferedDuration : segment.End;
                    var duration = actualEnd - segment.Start;

                    AudioBuffer? audio = null;
                    byte[]? segmentBuffer = null;

                    if (configuration.OutputAudio)
                    {
                        segmentBuffer = SegmentAudioBuffer(segment, format, sections);
                        audio = new AudioBuffer(segmentBuffer, format);
                    }


                    var result = new StreamingSpeechRecognitionResult(
                        isFinal,
                        "",
                        confidence,
                        Enumerable.Empty<SpeechRecognitionAlternate>(),
                        audio,
                        duration
                    );

                    var timestamp = (inputTimeMode switch
                    {
                        TimestampMode.AtStart => firstSection.OriginatingTime,
                        TimestampMode.AtEnd => firstSection.OriginatingTime - firstSection.Duration,
                        _ => throw new InvalidOperationException(),
                    }) + (outputTimeMode switch
                    {
                        TimestampMode.AtStart => segment.Start,
                        TimestampMode.AtEnd => actualEnd,
                        _ => throw new InvalidOperationException(),
                    });

                    *//*if (!isFinal) SafePost(PartialOut, result, timestamp);
                    else SafePost(FinalOut, result, timestamp);

                    SafePost(Out, result, timestamp);*//*
                    bool isDominance = false;
                    double[] userEnergy = new double[] { 0, 0, 0 };
                    var val = configuration.speechProcessing.energyLogs.DeepClone();
                    double bestDelta = double.MinValue;
                    foreach (var value in val)
                    {
                        int userId = value.Key;
                        var energyList = value.Value;
                        double delta = CalculateAverageEnergy(energyList, timestamp.AddMilliseconds(-result.Duration.Value.TotalMilliseconds), timestamp, userId);
                        userEnergy[userId] = delta;
                        if (delta > bestDelta)
                        {
                            bestDelta = delta;
                        }
                        //Console.WriteLine($"ON {configuration.userID}_Mean energy for user {userId+1} is {avg}");
                    }

                    if (userEnergy[configuration.userID - 1] == userEnergy.Max() || result.Duration.Value.TotalMilliseconds <= 1100)
                    {
                        isDominance = true;
                        Console.WriteLine($"Speaker {configuration.userID}_ " + result.Text);

                        if (!isFinal) SafePost(PartialOut, result, timestamp);
                        else SafePost(FinalOut, result, timestamp);

                        SafePost(Out, result, timestamp);
                    }

                }
            }
            finally
            {
                ArrayPool<float>.Shared.Return(samples);
                if (isFinal)
                {
                    sections.Clear();
                    bufferedDuration = TimeSpan.Zero;
                }
                segments.Clear();
            }
        }*/

        private void ProcessAndPost(bool isFinal)
        {
            if (sections.Count <= 0)
            {
                return;
            }
            var inputTimeMode = configuration.InputTimestampMode;
            var outputTimeMode = configuration.OutputTimestampMode;
            var processorValue = processor.Value;//Lazy initialize, but before data pre-processing
            var format = sections[0].Buffer.Format;
            var factor = format.Channels * format.BitsPerSample / 8;
            var size = sections.Sum(s => s.Buffer.Length) / factor;
            var samples = ArrayPool<float>.Shared.Rent(size);
            try
            {
                // Merge 
                var sampleOffset = 0;
                foreach (var section in sections)
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
                    Debug.Assert(bufferOffset == section.Buffer.Length);
                }
                Debug.Assert(sampleOffset == size);

                // Process 
                var valid = samples.AsSpan(0, size);
                processorValue.Process(valid);

                // Output 
                if (segments.Count == 0)
                {
                    return;
                }
                var firstSection = sections.First();
                Debug.Assert(Math.Abs(bufferedDuration.TotalMilliseconds - sections.Aggregate(TimeSpan.Zero, (v, s) => v + s.Buffer.Duration).TotalMilliseconds) < 1);
                foreach (var segment in segments)
                {
                    // Basic Info 
                    var text = segment.Text;
                    var confidence = segment.Probability;
                    var actualEnd = segment.End > bufferedDuration ? bufferedDuration : segment.End;//Input is padded to 30 seconds, so the end time may be larger than the actual end time
                    var duration = actualEnd - segment.Start;
                    AudioBuffer? audio;
                    if (!configuration.OutputAudio)
                    {
                        audio = null;
                    }
                    else
                    {
                        var audioBuffer = SegmentAudioBuffer(segment, format, sections);
                        audio = new AudioBuffer(audioBuffer, format);
                    }
                    var result = new StreamingSpeechRecognitionResult(isFinal, text, confidence, Enumerable.Empty<SpeechRecognitionAlternate>(), audio, duration);
                    // Timestamp 
                    var timestamp = (inputTimeMode switch
                    {
                        TimestampMode.AtStart => firstSection.OriginatingTime,
                        TimestampMode.AtEnd => firstSection.OriginatingTime - firstSection.Duration,
                        _ => throw new InvalidOperationException(),
                    }) + (outputTimeMode switch
                    {
                        TimestampMode.AtStart => segment.Start,
                        TimestampMode.AtEnd => actualEnd,
                        _ => throw new InvalidOperationException(),
                    });

                    bool isDominance = false;
                    double[] userEnergy = new double[] { 0, 0, 0 };
                    var energyLogs = configuration.speechProcessing.energyLogs.DeepClone();
                    double bestDelta = double.MinValue;
                    DateTime start = timestamp.AddMilliseconds(-result.Duration.Value.TotalMilliseconds);
                    DateTime end = timestamp;

                    /*var confidenceScore = AnalyzeVerbalizationConfidence(energyLogs, configuration.speechProcessing.profiles, start, end);

                    double threshold = 0.75;
                    var strongCandidates = confidenceScore
                        .Where(kv => kv.Value > threshold)
                        .OrderByDescending(kv => kv.Value)
                        .ToList();

                    if (strongCandidates.Count == 1)
                    {
                        int speakerId = strongCandidates[0].Key;
                        Console.WriteLine($"Locuteur dominant détecté : {speakerId} (score: {strongCandidates[0].Value:F2})");

                        // Mettre à jour le profil avec les énergies de cette verbalization
                        var energies = energyLogs[speakerId]
                            .Where(e => e.Key >= start && e.Key <= end)
                            .Select(e => e.Value);

                        *//*foreach (var energy in energies)
                            configuration.speechProcessing.profiles[speakerId].AddSample(energy);*//*

                        if (!isFinal) SafePost(PartialOut, result, timestamp);
                        else SafePost(FinalOut, result, timestamp);

                        SafePost(Out, result, timestamp);
                    }*/

                    //var allPercentiles = ComputePercentileInInterval(energyLogs, timestamp.AddMilliseconds(-result.Duration.Value.TotalMilliseconds), timestamp);
                    //var percentiles = Compute80thPercentileInInterval(val, timestamp.AddMilliseconds(-result.Duration.Value.TotalMilliseconds), timestamp);

                    /*if (dominantSpeaker.HasValue)
                    {
                        if (dominantSpeaker.Value == configuration.userID - 1)
                        {
                           
                        }
                    }*/
                    foreach (var value in energyLogs)
                    {
                        int userId = value.Key;
                        var energyList = value.Value;
                        double delta = CalculateAverageEnergy(energyList, start, end, userId);
                        //double delta = CalculateAverageEnergy(energyList, timestamp.AddMilliseconds(-1000), timestamp, userId);
                        userEnergy[userId] = delta;
                        if (delta > bestDelta)
                        {
                            bestDelta = delta;
                        }
                        //Console.WriteLine($"ON {configuration.userID}_Mean energy for user {userId+1} is {avg}");
                    }

                    if (userEnergy[configuration.userID - 1] == userEnergy.Max() || result.Duration.Value.TotalMilliseconds <= 1100)
                    {
                        isDominance = true;
                        //Console.WriteLine($"Speaker {configuration.userID}_ " + result.Text);

                        if (!isFinal) SafePost(PartialOut, result, timestamp);
                        else SafePost(FinalOut, result, timestamp);

                        SafePost(Out, result, timestamp);
                    }
                    /*switch (isFinal)
                    {
                        case false:
                            SafePost(PartialOut, result, timestamp);
                            break;
                        case true:
                            SafePost(FinalOut, result, timestamp);
                            break;
                    }
                    SafePost(Out, result, timestamp);*/
                }
            }
            finally
            {
                ArrayPool<float>.Shared.Return(samples);
                if (isFinal)
                {
                    sections.Clear();
                    bufferedDuration = TimeSpan.Zero;
                }
                segments.Clear();
            }
        }

        public static Dictionary<int, double> AnalyzeVerbalizationConfidence(Dictionary<int, SortedList<DateTime, double>> energyLogs, Dictionary<int, SpeakerEnergyProfile> profiles, DateTime start, DateTime end)
        {
            var confidenceBySpeaker = new Dictionary<int, double>();

            foreach (var kvp in energyLogs)
            {
                int speakerId = kvp.Key;
                var energySeries = kvp.Value;

                

                // Extraire les log energy dans l'intervalle
                var energiesInInterval = energySeries
                    .Where(e => e.Key >= start && e.Key <= end)
                    .Select(e => e.Value)
                    .ToList();

                //Console.WriteLine($"TEST for {speakerId} and {energiesInInterval.Count()} in list");

                if (!profiles.ContainsKey(speakerId)) continue;

                double confidence = ComputeConfidenceScore(energiesInInterval, profiles[speakerId]);
                confidenceBySpeaker[speakerId] = confidence;
                //Console.WriteLine($"TEST for {speakerId} and {energiesInInterval.Count()} in list and {profiles[speakerId].IsReady}");
            }

            return confidenceBySpeaker;
        }

        public static double ComputeConfidenceScore(List<double> verbalizationEnergies, SpeakerEnergyProfile profile, float percentileValue = 0.8f)
        {
            if (!verbalizationEnergies.Any() || !profile.IsReady)
                return 0;

            //Console.WriteLine($"TEST after is ready");

            var sorted = verbalizationEnergies.OrderBy(x => x).ToList();
            int index = (int)(percentileValue * (sorted.Count - 1));
            double observedPerc = sorted[index];
            double historicalPerc = profile.Percentile90;

            if (historicalPerc <= 0) return 0;

            return observedPerc / historicalPerc;
        }

        public double CalculateAverageEnergy(SortedList<DateTime, double> energyList, DateTime startTime, DateTime endTime, int id)
        {
            filteredEnergy[id] = energyList
                .Where(kvp => kvp.Key >= startTime && kvp.Key <= endTime)
                .Select(kvp => kvp.Value)
                //.Where(kvp => kvp >= 0)
                .ToList();

            if (filteredEnergy[id].Count == 0)
                return 0.0;

            return filteredEnergy[id].Average();
        }

        public double ComputeDelta(SortedList<DateTime, double> energyList, DateTime startTime, DateTime endTime, int id, double percentile = 0.2)
        {
            filteredEnergy[id] = energyList
                .Where(kvp => kvp.Key >= startTime && kvp.Key <= endTime)
                .Select(kvp => kvp.Value)
                .ToList();
            if (filteredEnergy[id].Count == 0)
                return 0.0;

            int count = filteredEnergy[id].Count;

            int lowCount = (int)(count * percentile);
            int highCount = (int)(count * percentile);

            var lowValues = filteredEnergy[id].Take(lowCount);
            var highValues = filteredEnergy[id].Skip(count - highCount);

            double meanLow = lowValues.Any() ? lowValues.Average() : 0;
            double meanHigh = highValues.Any() ? highValues.Average() : 0;

            return meanHigh - meanLow;
        }
        private void OnSegment(SegmentData segment)
        {
            segments.Add(segment);
        }

        private void OnProgress(int progress)
        {
            Progress = progress;//What is this? 100 times of 0.01sec units? https://github.com/ggerganov/whisper.cpp/blob/2f52783a080e8955e80e4324fed73e2f906bb80c/whisper.cpp#L4270C84-L4270C84
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
                {//has overlap
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
                    Debug.Assert(false);//should not reach here
                    break;
                }
                var section = sections[sectionIdx].Buffer;
                var sectionEndTime = sectionStartTime + section.Duration;
                if (segment.Start <= sectionEndTime && sectionStartTime < segment.End)
                {//has overlap
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
                        Debug.Assert(offset == length);
                        break;
                    }
                }
                else if (sectionStartTime >= segment.End)
                {
                    Debug.Assert(false);//should not reach here
                    break;
                }
                sectionIdx += 1;
                sectionStartTime = sectionEndTime;
            }

            return result;
        }

        private void SafePost(Emitter<IStreamingSpeechRecognitionResult> emitter, IStreamingSpeechRecognitionResult data, DateTime timestamp) {
            var minTimestamp = emitter.LastEnvelope.OriginatingTime + TimeSpan.FromMilliseconds(1);
            if (timestamp < minTimestamp) {
                timestamp = minTimestamp;
            }
            emitter.Post(data, timestamp);
        }

        private static string GetTypeModelFileName(GgmlType modelType) => modelType switch { 
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

        private static string GetQuantizationModelFileName(QuantizationType quantizationType) => quantizationType switch {
            QuantizationType.NoQuantization => "classic",
            QuantizationType.Q4_0 => "q4_0",
            QuantizationType.Q4_1 => "q4_1",
            QuantizationType.Q5_0 => "q5_0",
            QuantizationType.Q5_1 => "q5_1",
            QuantizationType.Q8_0 => "q8_0",
            _ => throw new InvalidOperationException(),
        };

        private static string GetLanguageCode(Language language) => language switch {//Generated, not tested
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

        #region IDisposable
        private bool disposed;

        public void Dispose() {
            if (disposed) {
                return;
            }
            disposed = true;

            if (processor.IsValueCreated) {
                processor.Value.Dispose();
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
            if (!EqualityComparer<T>.Default.Equals(field, value)) {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private readonly struct Section {

            public readonly AudioBuffer Buffer;

            public readonly DateTime OriginatingTime;

            public readonly TimeSpan Duration;

            public Section(AudioBuffer buffer, DateTime originatingTime, TimeSpan duration) {
                Buffer = buffer;
                OriginatingTime = originatingTime;
                Duration = duration;
            }
        }
    }

    [System.Serializable]
    public class SpeakerEnergyProfile
    {
        private readonly Queue<double> history = new Queue<double>();
        private readonly int maxSamples;

        public SpeakerEnergyProfile(int maxSamples = 300)
        {
            this.maxSamples = maxSamples;
        }

        public void AddSample(double logEnergy)
        {
            if (history.Count >= maxSamples)
                history.Dequeue();
            history.Enqueue(logEnergy);
        }

        public double Percentile90
        {
            get
            {
                if (!history.Any()) return 0;
                var queue = history.DeepClone();
                var sorted = queue.OrderBy(x => x).ToList();
                int index = (int)(0.8 * (sorted.Count - 1));
                return sorted[index];
            }
        }

        public bool IsReady => history.Count >= 20;
    }
}
