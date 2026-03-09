using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;
using System.Dynamic;
using Application = System.Windows.Application;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.PsiStudio.PipelinePlugin;
using CASPERAnalysis.StreamProcessors;
using Newtonsoft.Json;

namespace CASPERAnalysis
{
    /// <summary>
    /// Main window for CASPER Analysis application
    /// </summary>
    public partial class MainWindow : Window, IPsiStudioPipeline, INotifyPropertyChanged
    {
        private Pipeline pipeline;
        private Dataset dataset;
        private Session session;
        private string datasetPath = "";
        private string sessionName = "";
        private string experimentJsonPath = "";
        private List<ClassificationResult> results = new List<ClassificationResult>();
        private List<Session> sessions = new List<Session>();
        private Session selectedSession;
        private bool hasSessions = false;
        private List<TopicInfo> topics = new List<TopicInfo>();
        private bool hasSession = false;
        private SessionImporter currentSessionImporter;
        private bool isLoadingTopicsData = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public string DatasetPath
        {
            get => datasetPath;
            set => SetProperty(ref datasetPath, value);
        }

        public string SessionName
        {
            get => sessionName;
            set => SetProperty(ref sessionName, value);
        }

        public string ExperimentJsonPath
        {
            get => experimentJsonPath;
            set
            {
                if (SetProperty(ref experimentJsonPath, value))
                {
                    // Save to settings
                    Properties.Settings.Default.ExperimentJsonPath = value;
                    Properties.Settings.Default.Save();
                    
                    // Reload topics if path is valid
                    if (!string.IsNullOrEmpty(value) && File.Exists(value))
                    {
                        LoadTopicsFromConfig();
                    }
                }
            }
        }

        public List<Session> Sessions
        {
            get => sessions;
            set => SetProperty(ref sessions, value);
        }

        public Session SelectedSession
        {
            get => selectedSession;
            set
            {
                if (SetProperty(ref selectedSession, value))
                {
                    session = value;
                    SessionName = value?.Name ?? "";
                    AddLog($"Session selected: {SessionName}");
                }
            }
        }

        public bool HasSessions
        {
            get => hasSessions;
            set => SetProperty(ref hasSessions, value);
        }

        public List<TopicInfo> Topics
        {
            get => topics;
            set => SetProperty(ref topics, value);
        }

        public bool HasSession
        {
            get => hasSession;
            set => SetProperty(ref hasSession, value);
        }

        public bool IsLoadingTopicsData
        {
            get => isLoadingTopicsData;
            set => SetProperty(ref isLoadingTopicsData, value);
        }

        public string Log { get; set; } = "";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            // Initialize Topics collection
            Topics = new List<TopicInfo>();
            
            // Load saved experiment.json path
            var savedPath = Properties.Settings.Default.ExperimentJsonPath;
            if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath))
            {
                ExperimentJsonPath = savedPath;
            }
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }

        private void BrowseDataset_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Psi Dataset (*.pds)|*.pds|All files (*.*)|*.*",
                Title = "Select Dataset File"
            };

            if (dialog.ShowDialog() == true)
            {
                DatasetPath = dialog.FileName;
                LoadDataset();
            }
        }

        private void BrowseExperimentJson_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select experiment.json File",
                FileName = "experiment.json"
            };

            if (dialog.ShowDialog() == true)
            {
                ExperimentJsonPath = dialog.FileName;
            }
        }

        private void LoadDataset()
        {
            try
            {
                if (string.IsNullOrEmpty(DatasetPath) || !File.Exists(DatasetPath))
                {
                    AddLog("Invalid dataset path");
                    return;
                }

                dataset = Dataset.Load(DatasetPath);
                AddLog($"Dataset loaded: {dataset.Name}");
                AddLog($"Sessions available: {dataset.Sessions.Count()}");

                // Populate sessions list
                Sessions = dataset.Sessions.ToList();
                HasSessions = Sessions.Any();

                if (Sessions.Any())
                {
                    // Select first session by default
                    SelectedSession = Sessions.First();
                    HasSession = true;
                    AddLog($"Selected session: {SessionName}");
                    LoadTopicsFromConfig();
                    CheckTopicsAvailability();
                }
                else
                {
                    SelectedSession = null;
                    HasSession = false;
                    Topics.Clear();
                    AddLog("No sessions found in dataset");
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error loading dataset: {ex.Message}");
            }
        }

        private void SessionComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SelectedSession != null)
            {
                session = SelectedSession;
                SessionName = session.Name;
                HasSession = true;
                AddLog($"Session changed to: {SessionName}");
                // Reset topics when session changes
                foreach (var topic in Topics)
                {
                    topic.MessageCount = 0;
                    topic.IsAvailable = false;
                    topic.IsAnalyzed = false;
                }
                CheckTopicsAvailability();
            }
            else
            {
                HasSession = false;
            }
        }

        private void RunAnalysis_Click(object sender, RoutedEventArgs e)
        {
            if (dataset == null || SelectedSession == null)
            {
                AddLog("Please load a dataset and select a session first");
                return;
            }

            session = SelectedSession;
            results.Clear();
            RunAnalysisPipeline();
        }

        private void RunAnalysisPipeline()
        {
            try
            {
                pipeline = Pipeline.Create("CASPERAnalysis", enableDiagnostics: true);

                // Open session for replay
                var sessionImporter = SessionImporter.Open(pipeline, session);

                // Load required streams
                // Note: Stream names should match those in your experiment.json configuration

                // Module generation success
                var moduleStatusStream = sessionImporter.OpenStream<ValueTuple<int, string>>("Module status");
                var moduleSuccess = moduleStatusStream.Select(status => 
                    status.Item1 == 1 || status.Item2.Contains("success", StringComparison.OrdinalIgnoreCase));

                // Hand-Door proximity (requires combining wrist and door streams)
                // This is simplified - you'll need to adapt based on your actual stream structure
                var leftWristStream = sessionImporter.OpenStream<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>("1-LeftWrist");
                var rightWristStream = sessionImporter.OpenStream<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>("1-RightWrist");
                var doorStream = sessionImporter.OpenStream<ValueTuple<bool, System.Numerics.Vector3>>("Porte1 ouverture");

                // Gaze streams
                var gazeIndicatorStream = sessionImporter.OpenStream<ValueTuple<int, bool, string>>("Gaze1IndicatorDoor");
                var gazeDoorStream = sessionImporter.OpenStream<ValueTuple<int, bool, string>>("Gaze1");

                // Speech comprehension (if available)
                // Try to open speech stream from dataset, or create an empty stream if not available
                IProducer<string> speechStream = null;
                try
                {
                    // Try to open speech transcription stream (adjust stream name as needed)
                    speechStream = sessionImporter.OpenStream<string>("SpeechTranscription");
                }
                catch
                {
                    // If speech stream doesn't exist, create an empty stream
                    speechStream = Generators.Repeat(pipeline, "", TimeSpan.FromSeconds(1));
                }
                
                var speechComprehension = new SpeechComprehensionClassifier(pipeline);
                speechStream.PipeTo(speechComprehension);

                // Door closure
                var doorClosed = doorStream.Select(door => !door.Item1); // Door closed when Boolean is false

                // Button presses
                var validationButtonStream = sessionImporter.OpenStream<bool>("M1-Validation");
                var buttonCounter = new ButtonPressCounter(pipeline, TimeSpan.FromSeconds(5));
                validationButtonStream.PipeTo(buttonCounter);

                // Visual feedback (may need to be configured or inferred)
                // Try to open from dataset, or default to true if not available
                IProducer<bool> visualFeedback = null;
                try
                {
                    visualFeedback = sessionImporter.OpenStream<bool>("VisualFeedbackEnabled");
                }
                catch
                {
                    // Default to true if not available
                    visualFeedback = Generators.Repeat(pipeline, true, TimeSpan.FromSeconds(1));
                }

                // Create analyzer
                // Note: This is a simplified version - you'll need to adapt the LogigrammeAnalyzer
                // to work with actual Psi stream operators

                // For now, let's create a simpler version that processes events
                ProcessStreamsSimplified(
                    moduleSuccess,
                    doorClosed,
                    gazeIndicatorStream,
                    gazeDoorStream,
                    speechComprehension.Out,
                    buttonCounter.Out);

                // Run pipeline
                pipeline.RunAsync();
                AddLog("Analysis pipeline started");
            }
            catch (Exception ex)
            {
                AddLog($"Error running analysis: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ProcessStreamsSimplified(
            IProducer<bool> moduleSuccess,
            IProducer<bool> doorClosed,
            IProducer<ValueTuple<int, bool, string>> gazeIndicator,
            IProducer<ValueTuple<int, bool, string>> gazeDoor,
            IProducer<SpeechComprehensionState> speechComprehension,
            IProducer<int> buttonCount)
        {
            // Simplified processing - emit classifications based on stream events
            moduleSuccess.Do((success, env) =>
            {
                if (!success)
                {
                    // Start: Non -> F3
                    AddLog($"Module generation failed at {env.OriginatingTime:HH:mm:ss.fff}");
                    // Process perturbation path
                }
            });

            doorClosed.Do((closed, env) =>
            {
                if (closed)
                {
                    var result = new ClassificationResult
                    {
                        Timestamp = env.OriginatingTime,
                        Classification = ClassificationType.Gamma,
                        Reason = "Door closed"
                    };
                    results.Add(result);
                    AddLog($"Classification: {result}");
                }
            });

            // Store results
            var resultsEmitter = pipeline.CreateEmitter<ClassificationResult>(this, "Results");
            // Connect results to store or output
        }

        private void SpeechReceiver(string transcription, Envelope envelope)
        {
            // Handle speech input if available
        }

        private void VisualFeedbackReceiver(bool enabled, Envelope envelope)
        {
            // Handle visual feedback status
        }

        /// <summary>
        /// Returns true if the topic type is a heavy media type (video, audio, etc.) that we should not export
        /// because it takes too much time and is not displayed in the grid as data.
        /// </summary>
        private static bool IsHeavyMediaType(string topicType)
        {
            if (string.IsNullOrEmpty(topicType))
                return false;
            var lower = topicType.ToLowerInvariant();
            return lower.Contains("video") || lower.Contains("audio") || lower.Contains("encodedimage")
                || lower.Contains("media") || lower.Contains("bitmap") || lower.Contains("videoframe");
        }

        private async void ExportResults_Click(object sender, RoutedEventArgs e)
        {
            if (dataset == null || SelectedSession == null)
            {
                AddLog("Please load a dataset and select a session first");
                return;
            }

            // Export = one CSV per analyzed topic (same as clicking Export CSV on each row).
            // Only include topics that are analyzed and not heavy media (video/audio).
            var topicsToExport = Topics
                .Where(t => t.IsAnalyzed && t.MessageCount > 0 && !IsHeavyMediaType(t.TopicType))
                .ToList();

            if (topicsToExport.Count == 0)
            {
                AddLog("No results to export. Analyze one or more topics first (click Analyze on each row). Video and audio streams are not exported.");
                return;
            }

            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select folder to save CSV files (one per event/topic)"
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            var folder = dialog.SelectedPath;
            var exportTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            AddLog($"Exporting {topicsToExport.Count} topic(s) to {folder}...");

            int successCount = 0;
            foreach (var topic in topicsToExport)
            {
                var safeName = string.Join("_", topic.TopicName.Split(Path.GetInvalidFileNameChars()));
                var filename = Path.Combine(folder, $"{safeName}_{SelectedSession.Name}_{exportTimestamp}.csv");
                var success = await ExportTopicDataToCsvAsync(topic.TopicName, filename);
                if (success)
                    successCount++;
            }

            AddLog($"Export complete: {successCount}/{topicsToExport.Count} topic(s) exported to {folder}");
        }

        private void ExportToCsv(string filename)
        {
            using var writer = new StreamWriter(filename);
            writer.WriteLine("Timestamp,Classification,Reason");
            foreach (var result in results)
            {
                writer.WriteLine($"{result.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{result.Classification},{result.Reason}");
            }
        }

        private void ExportToJson(string filename)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(results, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filename, json);
        }

        private void AddLog(string message)
        {
            // Ensure UI updates happen on the UI thread
            if (Application.Current != null && Application.Current.Dispatcher.CheckAccess())
            {
                // We're on the UI thread, update directly
                Log += $"{DateTime.Now:HH:mm:ss.fff} - {message}\n";
                LogTextBox.Text = Log;
                // Scroll to end using the ScrollViewer
                LogScrollViewer.ScrollToEnd();
            }
            else
            {
                // We're on a different thread, use Dispatcher
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Log += $"{DateTime.Now:HH:mm:ss.fff} - {message}\n";
                    LogTextBox.Text = Log;
                    // Scroll to end using the ScrollViewer
                    LogScrollViewer.ScrollToEnd();
                }));
            }
        }

        // IPsiStudioPipeline implementation
        public Dataset GetDataset() => dataset;

        public void RunPipeline(TimeInterval timeInterval)
        {
            RunAnalysisPipeline();
        }

        public void StopPipeline()
        {
            pipeline?.Dispose();
        }

        public void Dispose()
        {
            pipeline?.Dispose();
            // Dataset doesn't implement IDisposable - no need to dispose
        }

        public DateTime GetStartTime() => pipeline?.StartTime ?? DateTime.MinValue;

        public PipelineReplaybleMode GetReplaybleMode() => PipelineReplaybleMode.PsiStudio;

        private void LoadTopicsFromConfig()
        {
            try
            {
                string configPath = null;

                // First, try the user-specified path
                if (!string.IsNullOrEmpty(ExperimentJsonPath) && File.Exists(ExperimentJsonPath))
                {
                    configPath = ExperimentJsonPath;
                }

                if (configPath == null)
                {
                    AddLog("experiment.json not found. Please specify the path using the 'Browse...' button above.");
                    Topics = new List<TopicInfo>();
                    TopicsDataGrid.Items.Refresh();
                    return;
                }

                var json = File.ReadAllText(configPath);
                var topicConfigs = JsonConvert.DeserializeObject<List<TopicConfig>>(json) ?? new List<TopicConfig>();

                if (topicConfigs.Count == 0)
                {
                    AddLog("WARNING: experiment.json is empty or contains no topics");
                    Topics = new List<TopicInfo>();
                    TopicsDataGrid.Items.Refresh();
                    return;
                }

                var newTopics = topicConfigs.Select(tc => new TopicInfo
                {
                    TopicName = tc.topic,
                    TopicType = tc.type,
                    MessageCount = 0,
                    IsAvailable = false,
                    IsAnalyzed = false
                }).ToList();

                // Update on UI thread to ensure binding works
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Topics = newTopics;
                    TopicsDataGrid.ItemsSource = Topics;
                    TopicsDataGrid.Items.Refresh();
                    AddLog($"Loaded {Topics.Count} topics from experiment.json");
                }));
            }
            catch (Exception ex)
            {
                AddLog($"Error loading topics config: {ex.Message}");
                Topics = new List<TopicInfo>();
                TopicsDataGrid.Items.Refresh();
            }
        }

        private void CheckTopicsAvailability()
        {
            if (dataset == null || SelectedSession == null || Topics == null || !Topics.Any())
                return;

            try
            {
                // Get available streams from the session metadata (fast, no data reading)
                var availableStreams = new HashSet<string>();
                foreach (var partition in SelectedSession.Partitions)
                {
                    foreach (var streamMeta in partition.AvailableStreams)
                    {
                        availableStreams.Add(streamMeta.Name);
                    }
                }

                // Update availability for each topic
                foreach (var topicInfo in Topics)
                {
                    topicInfo.IsAvailable = availableStreams.Contains(topicInfo.TopicName);
                }

                TopicsDataGrid.Items.Refresh();
                AddLog($"Checked topics availability. {Topics.Count(t => t.IsAvailable)}/{Topics.Count} topics available");
            }
            catch (Exception ex)
            {
                AddLog($"Error checking topics availability: {ex.Message}");
            }
        }

        private async void AnalyzeTopic_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.Tag is string topicName)
            {
                await AnalyzeTopicAsync(topicName);
            }
        }

        private async Task AnalyzeTopicAsync(string topicName)
        {
            if (dataset == null || SelectedSession == null)
            {
                AddLog("Please load a dataset and select a session first");
                return;
            }

            var topicInfo = Topics.FirstOrDefault(t => t.TopicName == topicName);
            if (topicInfo == null)
            {
                AddLog($"Topic {topicName} not found");
                return;
            }

            if (!topicInfo.IsAvailable)
            {
                AddLog($"Topic {topicName} is not available in the selected session");
                return;
            }

            AddLog($"Analyzing topic: {topicName}...");

            try
            {
                await Task.Run(() =>
                {
                    // Try to get message count from metadata first (fast)
                    long messageCountFromMetadata = 0;
                    bool metadataFound = false;

                    foreach (var partition in SelectedSession.Partitions)
                    {
                        var streamMeta = partition.AvailableStreams.FirstOrDefault(s => s.Name == topicName);
                        if (streamMeta != null)
                        {
                            messageCountFromMetadata = streamMeta.MessageCount;
                            metadataFound = true;
                            break;
                        }
                    }

                    // Always count by reading the stream if metadata shows 0 or is unreliable
                    long actualMessageCount = 0;
                    bool countingSucceeded = false;
                    
                    if (messageCountFromMetadata == 0 || !metadataFound)
                    {
                        Pipeline tempPipeline = null;
                        try
                        {
                            // Create a temporary pipeline to count messages (READ-ONLY)
                            tempPipeline = Pipeline.Create("MessageCounter", enableDiagnostics: false);
                            
                            // Add exception handler
                            bool pipelineError = false;
                            bool criticalError = false;
                            tempPipeline.PipelineExceptionNotHandled += (sender, e) =>
                            {
                                var exMessage = e.Exception?.Message ?? "Unknown error";
                                
                                // Ignore non-critical serialization errors that don't affect counting
                                if (exMessage.Contains("IntPtr") || 
                                    exMessage.Contains("CloneIntPtrFields") ||
                                    exMessage.Contains("System.WeakReference"))
                                {
                                    // These are warnings, not critical errors
                                    return;
                                }
                                
                                pipelineError = true;
                                
                                // Check if this is a critical error that prevents operation
                                if (exMessage.Contains("schema") || exMessage.Contains("version"))
                                {
                                    // Schema errors are expected for some streams
                                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        AddLog($"Warning: Schema compatibility issue for {topicName}: {exMessage}");
                                    }));
                                }
                                else
                                {
                                    criticalError = true;
                                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        AddLog($"Pipeline error while counting {topicName}: {exMessage}");
                                    }));
                                }
                            };
                            
                            var tempImporter = SessionImporter.Open(tempPipeline, SelectedSession);

                            // Try to open the stream - use multiple fallback methods
                            IProducer<object> stream = null;
                            
                            // First try dynamic stream (most robust for schema compatibility)
                            try
                            {
                                foreach (var partition in tempImporter.PartitionImporters.Values)
                                {
                                    if (partition.AvailableStreams.Any(s => s.Name == topicName))
                                    {
                                        var dynamicStream = partition.OpenDynamicStream(topicName);
                                        if (dynamicStream != null)
                                        {
                                            stream = dynamicStream.Select(d => (object)d) as IProducer<object>;
                                            break;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    AddLog($"Warning: Could not open {topicName} as dynamic stream: {ex.Message}");
                                }));
                            }
                            
                            // If dynamic failed, try with the configured type
                            if (stream == null)
                            {
                                stream = OpenStreamByType(tempImporter, topicName, topicInfo.TopicType);
                            }
                            
                            // Last resort: try as object
                            if (stream == null)
                            {
                                try
                                {
                                    stream = tempImporter.OpenStream<object>(topicName) as IProducer<object>;
                                }
                                catch { }
                            }
                            
                            if (stream != null)
                            {
                                // Count messages by reading them (READ-ONLY)
                                stream.Do(_ => actualMessageCount++);
                                
                                // Run pipeline to count all messages (READ-ONLY replay)
                                // Don't use 'using' with RunAsync as it can cause disposal race conditions
                                tempPipeline.RunAsync(ReplayDescriptor.ReplayAll);
                                
                                // Wait for pipeline to complete
                                if (tempPipeline.WaitAll(TimeSpan.FromSeconds(60)))
                                {
                                    countingSucceeded = true;
                                }
                                else
                                {
                                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        AddLog($"Warning: Pipeline timeout while counting {topicName}");
                                    }));
                                }
                            }
                            else
                            {
                                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    AddLog($"Warning: Could not open stream {topicName} for counting");
                                }));
                            }
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                AddLog($"Warning: Error counting messages for {topicName}: {ex.Message}");
                            }));
                        }
                        finally
                        {
                            // CRITICAL: Always dispose the pipeline to release file locks
                            // Add a small delay to ensure pipeline has fully stopped before disposing
                            if (tempPipeline != null)
                            {
                                try
                                {
                                    // Wait a bit to ensure all pipeline operations have completed
                                    System.Threading.Thread.Sleep(100);
                                    
                                    // Dispose pipeline safely
                                    tempPipeline.Dispose();
                                    tempPipeline = null;
                                }
                                catch (ObjectDisposedException)
                                {
                                    // Pipeline already disposed, ignore
                                    tempPipeline = null;
                                }
                                catch
                                {
                                    // Ignore other disposal errors
                                    tempPipeline = null;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Metadata shows a non-zero count, use it directly
                        actualMessageCount = messageCountFromMetadata;
                        countingSucceeded = true;
                    }

                    // Use actual count if counting succeeded, otherwise use metadata (even if 0)
                    long finalCount = countingSucceeded ? actualMessageCount : messageCountFromMetadata;

                    // Update on UI thread
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        topicInfo.MessageCount = finalCount;
                        topicInfo.IsAnalyzed = true;
                        TopicsDataGrid.Items.Refresh();
                        if (actualMessageCount > 0 && messageCountFromMetadata != actualMessageCount)
                        {
                            AddLog($"Topic {topicName} analyzed: {finalCount:N0} messages (metadata showed {messageCountFromMetadata}, counted {actualMessageCount})");
                        }
                        else
                        {
                            AddLog($"Topic {topicName} analyzed: {finalCount:N0} messages");
                        }
                    }));
                });
            }
            catch (Exception ex)
            {
                AddLog($"Error analyzing topic {topicName}: {ex.Message}");
            }
        }

        private string SerializeObjectToCsvRow(object data, string typeName, ref List<string> headers, ref bool isFirstRow)
        {
            if (data == null)
            {
                return "";
            }

            // Special case for plain strings: export the full string value in a single "Value" column.
            // Without this, System.String was being treated as a custom type and we only got "Chars,Length"
            // (with empty Chars and non-zero Length) in the CSV.
            if (data is string s)
            {
                if (isFirstRow)
                {
                    headers.Add("Value");
                    isFirstRow = false;
                }

                return EscapeCsvValue(s);
            }

            var dataType = data.GetType();
            var values = new List<string>();

            // Handle ExpandoObject (dynamic objects) - these come from OpenDynamicStream
            // Unity sends fields with camelCase names, we need to map them properly
            if (data is ExpandoObject expando)
            {
                var dict = (IDictionary<string, object>)expando;
                
                // Create a mapping from all possible key variations to normalized names
                var keyToNormalizedName = new Dictionary<string, string>();
                var normalizedNames = new HashSet<string>();
                
                foreach (var key in dict.Keys)
                {
                    string normalizedName = key;
                    
                    // Remove compiler-generated backing field markers
                    if (key.Contains("k__BackingField"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(key, @"<([^>]+)>k__BackingField");
                        if (match.Success)
                        {
                            normalizedName = match.Groups[1].Value;
                        }
                        else
                        {
                            normalizedName = key.Replace("k__BackingField", "").Trim('<', '>');
                        }
                    }
                    else if (key.StartsWith("<") && key.EndsWith(">"))
                    {
                        normalizedName = key.Trim('<', '>');
                    }
                    
                    // Convert camelCase to PascalCase for consistency
                    normalizedName = ToPascalCase(normalizedName);
                    
                    keyToNormalizedName[key] = normalizedName;
                    normalizedNames.Add(normalizedName);
                }
                
                // Create ordered list of normalized names
                var orderedNames = normalizedNames.OrderBy(n => n).ToList();
                
                if (isFirstRow)
                {
                    headers.AddRange(orderedNames);
                    isFirstRow = false;
                }
                
                // Get values in the order of headers (skip "Timestamp")
                foreach (var headerName in headers.Skip(1))
                {
                    // Try to find the original key that maps to this normalized name
                    // Check both exact match and camelCase version
                    var originalKey = keyToNormalizedName.FirstOrDefault(kvp => 
                        kvp.Value == headerName || 
                        kvp.Value == ToCamelCase(headerName) ||
                        ToPascalCase(kvp.Value) == headerName).Key;
                    
                    if (originalKey != null && dict.ContainsKey(originalKey))
                    {
                        var value = dict[originalKey];
                        values.Add(EscapeCsvValue(FormatValue(value)));
                    }
                    else
                    {
                        // Try direct lookup with camelCase version
                        var camelCaseKey = ToCamelCase(headerName);
                        if (dict.ContainsKey(camelCaseKey))
                        {
                            var value = dict[camelCaseKey];
                            values.Add(EscapeCsvValue(FormatValue(value)));
                        }
                        else
                        {
                            values.Add("");
                        }
                    }
                }
                
                return string.Join(",", values);
            }

            // Handle ValueTuple types
            if (dataType.IsGenericType && dataType.Name.StartsWith("ValueTuple"))
            {
                var fields = dataType.GetFields();
                if (isFirstRow)
                {
                    for (int i = 0; i < fields.Length; i++)
                    {
                        headers.Add($"Item{i + 1}");
                    }
                    isFirstRow = false;
                }
                foreach (var field in fields)
                {
                    var value = field.GetValue(data);
                    values.Add(EscapeCsvValue(FormatValue(value)));
                }
                return string.Join(",", values);
            }

            // Handle Tuple types
            if (dataType.IsGenericType && dataType.Name.StartsWith("Tuple"))
            {
                var properties = dataType.GetProperties();
                if (isFirstRow)
                {
                    foreach (var prop in properties)
                    {
                        headers.Add(prop.Name);
                    }
                    isFirstRow = false;
                }
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(data);
                    values.Add(EscapeCsvValue(FormatValue(value)));
                }
                return string.Join(",", values);
            }

            // Handle custom types (structs/classes) - use reflection to get all properties AND fields
            // Unity uses fields (camelCase), C# uses properties (PascalCase)
            var allProperties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !p.Name.Contains("k__BackingField") && !p.Name.StartsWith("<"))
                .OrderBy(p => p.Name)
                .ToList();
            
            var allFields = dataType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.Name.Contains("k__BackingField") && !f.Name.StartsWith("<"))
                .OrderBy(f => f.Name)
                .ToList();
            
            // Prefer properties, but also include fields if they exist (for Unity compatibility)
            var members = new List<(string Name, Func<object> GetValue)>();
            
            // Add properties first
            foreach (var prop in allProperties)
            {
                members.Add((prop.Name, () => prop.GetValue(data)));
            }
            
            // Add fields that don't have a corresponding property (Unity fields)
            foreach (var field in allFields)
            {
                // Check if there's already a property with the same name (case-insensitive)
                var hasProperty = allProperties.Any(p => 
                    string.Equals(p.Name, field.Name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.Name, ToPascalCase(field.Name), StringComparison.OrdinalIgnoreCase));
                
                if (!hasProperty)
                {
                    members.Add((field.Name, () => field.GetValue(data)));
                }
            }
            
            if (members.Count > 0)
            {
                if (isFirstRow)
                {
                    foreach (var member in members)
                    {
                        // Normalize names: convert camelCase to PascalCase for consistency
                        string normalizedName = ToPascalCase(member.Name);
                        headers.Add(normalizedName);
                    }
                    isFirstRow = false;
                }
                
                foreach (var member in members)
                {
                    try
                    {
                        var value = member.GetValue();
                        values.Add(EscapeCsvValue(FormatValue(value)));
                    }
                    catch (Exception ex)
                    {
                        values.Add("");
                    }
                }
                return string.Join(",", values);
            }

            // Fallback: simple value
            if (isFirstRow)
            {
                headers.Add("Value");
                isFirstRow = false;
            }
            return EscapeCsvValue(FormatValue(data));
        }

        private string FormatValue(object value)
        {
            if (value == null)
                return "";

            // Handle arrays - extract actual values
            if (value is Array array)
            {
                if (array.Length == 0)
                    return "[]";
                
                var items = new List<string>();
                foreach (var item in array)
                {
                    if (item == null)
                        items.Add("");
                    else
                        items.Add(item.ToString());
                }
                return "[" + string.Join(";", items) + "]";
            }

            // Handle generic collections (List<T>, etc.) - but not strings
            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                var items = new List<string>();
                foreach (var item in enumerable)
                {
                    if (item == null)
                        items.Add("");
                    else
                        items.Add(item.ToString());
                }
                return "[" + string.Join(";", items) + "]";
            }

            // Handle primitive types and simple values
            // Check if it's a type name (like "System.Object[]") - this shouldn't happen but handle it
            var type = value.GetType();
            if (type.IsArray && value.ToString() == type.ToString())
            {
                // This is likely a boxed array type, try to unbox it
                return "[]";
            }

            return value.ToString();
        }

        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Escape commas, quotes, and newlines
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        private string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            
            // Convert camelCase to PascalCase
            if (char.IsLower(name[0]))
            {
                return char.ToUpper(name[0]) + name.Substring(1);
            }
            
            return name;
        }

        private string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            
            // Convert PascalCase to camelCase
            if (char.IsUpper(name[0]) && name.Length > 1)
            {
                return char.ToLower(name[0]) + name.Substring(1);
            }
            
            return name;
        }

        private IProducer<object> OpenStreamByType(SessionImporter importer, string topicName, string typeName)
        {
            try
            {
                // Map common types to their stream opening methods
                if (typeName.Contains("System.Single") || typeName.Contains("System.Float") || typeName == "System.Single")
                {
                    var stream = importer.OpenStream<float>(topicName);
                    return stream as IProducer<object>;
                }
                else if (typeName.Contains("System.Int32") || typeName.Contains("System.Integer") || typeName == "System.Int32")
                {
                    var stream = importer.OpenStream<int>(topicName);
                    return stream as IProducer<object>;
                }
                else if (typeName.Contains("System.Boolean") || typeName == "System.Boolean")
                {
                    var stream = importer.OpenStream<bool>(topicName);
                    return stream as IProducer<object>;
                }
                else if (typeName.Contains("System.String") || typeName == "System.String")
                {
                    var stream = importer.OpenStream<string>(topicName);
                    return stream as IProducer<object>;
                }
                else if (typeName.Contains("ValueTuple`2[System.Int32,System.String]"))
                {
                    var stream = importer.OpenStream<ValueTuple<int, string>>(topicName);
                    return stream as IProducer<object>;
                }
                else if (typeName.Contains("ValueTuple`3[System.Int32,System.Boolean,System.String]"))
                {
                    var stream = importer.OpenStream<ValueTuple<int, bool, string>>(topicName);
                    return stream as IProducer<object>;
                }
                else if (typeName.Contains("ValueTuple`2[System.Boolean,System.Numerics.Vector3]"))
                {
                    var stream = importer.OpenStream<ValueTuple<bool, System.Numerics.Vector3>>(topicName);
                    return stream as IProducer<object>;
                }
                else if (typeName.Contains("Tuple`2[[System.Numerics.Vector3"))
                {
                    var stream = importer.OpenStream<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>(topicName);
                    return stream as IProducer<object>;
                }
                
                // Try to load custom types from SAAC.PsiFormats namespace
                if (typeName.StartsWith("SAAC.PsiFormats."))
                {
                    try
                    {
                        // Load the type from the PsiFormats assembly
                        var type = Type.GetType(typeName);
                        if (type == null)
                        {
                            // Try loading from all loaded assemblies
                            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                            {
                                type = assembly.GetType(typeName);
                                if (type != null)
                                {
                                    Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        AddLog($"Found type {typeName} in assembly {assembly.FullName}");
                                    }));
                                    break;
                                }
                            }
                        }
                        
                        if (type != null)
                        {
                            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                AddLog($"Attempting to open stream {topicName} with type {type.FullName}");
                            }));
                            
                            // Use reflection to call OpenStream<T> with the specific type
                            var openStreamMethod = typeof(SessionImporter).GetMethods()
                                .FirstOrDefault(m => m.Name == "OpenStream" && m.IsGenericMethod && m.GetParameters().Length == 1);
                            
                            if (openStreamMethod != null)
                            {
                                var genericMethod = openStreamMethod.MakeGenericMethod(type);
                                var typedStream = genericMethod.Invoke(importer, new object[] { topicName });
                                
                                if (typedStream != null)
                                {
                                    Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        AddLog($"Successfully opened stream {topicName} with typed method");
                                    }));
                                    
                                    // Convert IProducer<T> to IProducer<object> using Select
                                    // Create a converter function: T -> object using expression trees
                                    var param = System.Linq.Expressions.Expression.Parameter(type, "x");
                                    var convert = System.Linq.Expressions.Expression.Convert(param, typeof(object));
                                    var converterType = typeof(Func<,>).MakeGenericType(type, typeof(object));
                                    var lambda = System.Linq.Expressions.Expression.Lambda(converterType, convert, param);
                                    var converter = lambda.Compile();
                                    
                                    // Call Select on the typed stream
                                    var selectMethod = typeof(Operators).GetMethods()
                                        .Where(m => m.Name == "Select" && m.IsGenericMethod && m.GetParameters().Length == 2)
                                        .Select(m => m.MakeGenericMethod(type, typeof(object)))
                                        .FirstOrDefault(m => m.GetParameters()[1].ParameterType == converterType);
                                    
                                    if (selectMethod != null)
                                    {
                                        var objectStream = selectMethod.Invoke(null, new[] { typedStream, converter });
                                        return objectStream as IProducer<object>;
                                    }
                                    
                                    // Fallback: try to cast directly (may not work for generic types)
                                    return typedStream as IProducer<object>;
                                }
                            }
                        }
                        else
                        {
                            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                AddLog($"Warning: Could not find type {typeName} in any loaded assembly");
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            AddLog($"Warning: Could not load custom type {typeName}: {ex.Message}");
                            AddLog($"Stack trace: {ex.StackTrace}");
                        }));
                    }
                }
                
                // Try as object as fallback
                return importer.OpenStream<object>(topicName);
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    AddLog($"Error opening stream {topicName} with type {typeName}: {ex.Message}");
                }));
                return null;
            }
        }
        

        private void ExportTopicCsv_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.Tag is string topicName)
            {
                ExportTopicToCsv(topicName);
            }
        }

        private async void ExportTopicToCsv(string topicName)
        {
            if (dataset == null || SelectedSession == null)
            {
                AddLog("Please load a dataset and select a session first");
                return;
            }

            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"{topicName.Replace(" ", "_")}_{SelectedSession.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    AddLog($"Exporting {topicName} to {dialog.FileName}...");
                    var success = await ExportTopicDataToCsvAsync(topicName, dialog.FileName);
                    if (success)
                    {
                        AddLog($"Successfully exported {topicName} to {dialog.FileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error exporting topic {topicName}: {ex.Message}");
                AddLog($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task<bool> ExportTopicDataToCsvAsync(string topicName, string filename)
        {
            return await Task.Run(() =>
            {
                Pipeline tempPipeline = null;
                try
                {
                    // Create pipeline with explicit read-only configuration
                    tempPipeline = Pipeline.Create("TopicExporter", enableDiagnostics: false);
                    
                    // Open session importer (READ-ONLY - no writing to stores)
                    var tempImporter = SessionImporter.Open(tempPipeline, SelectedSession);
                    
                    var topicInfo = Topics.FirstOrDefault(t => t.TopicName == topicName);
                    
                    if (topicInfo == null)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            AddLog($"Topic {topicName} not found");
                        }));
                        return false;
                    }

                    var dataRows = new List<string>();

                    // First, try to find the actual stream type from metadata
                    string actualTypeName = topicInfo.TopicType;
                    foreach (var partition in SelectedSession.Partitions)
                    {
                        var streamMeta = partition.AvailableStreams.FirstOrDefault(s => s.Name == topicName);
                        if (streamMeta != null)
                        {
                            actualTypeName = streamMeta.TypeName;
                            break;
                        }
                    }

                    IProducer<object> stream = null;
                    
                    // First try opening as dynamic stream (most robust for schema compatibility)
                    try
                    {
                        // Try OpenDynamicStream via partition importers which handles serialization issues better
                        foreach (var partition in tempImporter.PartitionImporters.Values)
                        {
                            try
                            {
                                var dynamicStream = partition.OpenDynamicStream(topicName);
                                if (dynamicStream != null)
                                {
                                    // Convert dynamic to object producer
                                    stream = dynamicStream.Select(d => (object)d) as IProducer<object>;
                                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        AddLog($"Opened {topicName} as dynamic stream");
                                    }));
                                    break;
                                }
                            }
                            catch
                            {
                                // Try next partition
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            AddLog($"Warning: Could not open {topicName} as dynamic stream: {ex.Message}");
                        }));
                    }
                    
                    // If dynamic failed, try to open with specific type
                    if (stream == null)
                    {
                        try
                        {
                            stream = OpenStreamByType(tempImporter, topicName, actualTypeName);
                            if (stream != null)
                            {
                                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    AddLog($"Opened {topicName} with type {actualTypeName}");
                                }));
                            }
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                AddLog($"Warning: Could not open {topicName} with type {actualTypeName}: {ex.Message}");
                            }));
                        }
                    }
                    
                    // Last resort: try as object
                    if (stream == null)
                    {
                        try
                        {
                            stream = tempImporter.OpenStream<object>(topicName) as IProducer<object>;
                            if (stream != null)
                            {
                                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    AddLog($"Opened {topicName} as object (last resort)");
                                }));
                            }
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                AddLog($"Failed to open stream {topicName}: {ex.Message}");
                            }));
                        }
                    }
                    
                    if (stream == null)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            AddLog($"Could not open stream for topic {topicName}. Stream may not exist or be corrupted.");
                        }));
                        return false;
                    }

                    // Add exception handler for pipeline errors
                    bool pipelineError = false;
                    bool criticalError = false;
                    string pipelineErrorMessage = "";
                    tempPipeline.PipelineExceptionNotHandled += (sender, e) =>
                    {
                        pipelineErrorMessage = e.Exception?.Message ?? "Unknown pipeline error";
                        
                        // Ignore non-critical serialization errors that don't prevent data export
                        if (pipelineErrorMessage.Contains("IntPtr") || 
                            pipelineErrorMessage.Contains("CloneIntPtrFields") ||
                            pipelineErrorMessage.Contains("System.WeakReference"))
                        {
                            // These are warnings, not critical errors
                            return;
                        }
                        
                        pipelineError = true;
                        
                        // Check if this is a critical error
                        if (pipelineErrorMessage.Contains("schema") || pipelineErrorMessage.Contains("version"))
                        {
                            // Schema errors might be recoverable with dynamic streams
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                AddLog($"Warning: Schema compatibility issue for {topicName}: {pipelineErrorMessage}");
                            }));
                        }
                        else
                        {
                            criticalError = true;
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                AddLog($"Pipeline error for {topicName}: {pipelineErrorMessage}");
                                if (e.Exception != null)
                                {
                                    AddLog($"Exception type: {e.Exception.GetType().Name}");
                                }
                            }));
                        }
                    };

                    // Determine CSV header based on type
                    List<string> csvHeaders = new List<string> { "Timestamp" };
                    bool isFirstRow = true;
                    
                    int messageIndex = 0;
                    // Collect data (READ-ONLY operation)
                    stream.Do((data, env) =>
                    {
                        try
                        {
                            messageIndex++;
                            
                            // Debug: log ALL values for first few messages to diagnose data issues
                            if (messageIndex <= 3 && data != null)
                            {
                                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    AddLog($"Debug [{messageIndex}]: {topicName} - Type: {data.GetType().FullName}");
                                    
                                    // Try to extract ALL values for debugging
                                    if (data is ExpandoObject expando)
                                    {
                                        var dict = (IDictionary<string, object>)expando;
                                        
                                        // Log all keys/values
                                        var allValues = new List<string>();
                                        foreach (var kvp in dict.OrderBy(k => k.Key))
                                        {
                                            string valueStr = FormatValue(kvp.Value);
                                            allValues.Add($"{kvp.Key}={valueStr}");
                                        }
                                        AddLog($"Debug [{messageIndex}]: ALL ExpandoObject values ({dict.Count}): {string.Join(", ", allValues)}");
                                        
                                        // Also log specific important fields
                                        var importantFields = new[] { "completedSpaces", "totalSpaces", "givenVoltage", "voltagesRequired", "matchVoltages", "regulated" };
                                        var importantValues = new List<string>();
                                        foreach (var field in importantFields)
                                        {
                                            // Try both camelCase and PascalCase
                                            var keys = dict.Keys.Where(k => k.Contains(field, StringComparison.OrdinalIgnoreCase));
                                            foreach (var key in keys)
                                            {
                                                importantValues.Add($"{field} (key: {key}) = {FormatValue(dict[key])}");
                                            }
                                        }
                                        if (importantValues.Count > 0)
                                        {
                                            AddLog($"Debug [{messageIndex}]: Important fields: {string.Join(", ", importantValues)}");
                                        }
                                    }
                                    else
                                    {
                                        // Try reflection to get all values
                                        var props = data.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                                        var fields = data.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                                        var allValues = new List<string>();
                                        
                                        foreach (var prop in props)
                                        {
                                            try 
                                            { 
                                                var val = prop.GetValue(data);
                                                allValues.Add($"{prop.Name}={FormatValue(val)}"); 
                                            }
                                            catch { allValues.Add($"{prop.Name}=<error>"); }
                                        }
                                        foreach (var field in fields)
                                        {
                                            try 
                                            { 
                                                var val = field.GetValue(data);
                                                allValues.Add($"{field.Name}={FormatValue(val)}"); 
                                            }
                                            catch { allValues.Add($"{field.Name}=<error>"); }
                                        }
                                        AddLog($"Debug [{messageIndex}]: ALL Properties/Fields ({allValues.Count}): {string.Join(", ", allValues)}");
                                    }
                                }));
                            }
                            
                            // Serialize object to CSV row with all fields
                            var csvRow = SerializeObjectToCsvRow(data, actualTypeName, ref csvHeaders, ref isFirstRow);
                            var row = $"{env.OriginatingTime:yyyy-MM-dd HH:mm:ss.fff},{csvRow}";
                            dataRows.Add(row);
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                AddLog($"Warning: Error processing message {messageIndex} for {topicName}: {ex.Message}");
                                AddLog($"Stack trace: {ex.StackTrace}");
                            }));
                        }
                    });

                    // Run pipeline to collect all data (READ-ONLY replay)
                    // Don't use 'using' with RunAsync as it can cause disposal race conditions
                    try
                    {
                        // Start the pipeline without using statement to avoid premature disposal
                        tempPipeline.RunAsync(ReplayDescriptor.ReplayAll);
                        
                        // Wait for pipeline to complete
                        if (!tempPipeline.WaitAll(TimeSpan.FromSeconds(60)))
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                AddLog($"Warning: Pipeline timeout for {topicName} after 60 seconds");
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            AddLog($"Error running pipeline for {topicName}: {ex.Message}");
                            AddLog($"Exception type: {ex.GetType().Name}");
                        }));
                        
                        if (pipelineError)
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                AddLog($"Pipeline error details: {pipelineErrorMessage}");
                            }));
                        }
                        
                        // Still try to export what we collected
                        if (dataRows.Count == 0)
                        {
                            return false;
                        }
                    }

                    // Write to CSV (external file, not Psi store)
                    using (var writer = new StreamWriter(filename))
                    {
                        // Write header with all fields
                        writer.WriteLine(string.Join(",", csvHeaders));
                        
                        // Write data rows
                        foreach (var row in dataRows)
                        {
                            writer.WriteLine(row);
                        }
                        writer.Flush();
                    }

                    // Verify file was created
                    if (!File.Exists(filename))
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            AddLog($"Error: File was not created: {filename}");
                        }));
                        return false;
                    }

                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        AddLog($"Exported {dataRows.Count} messages from {topicName} to {filename}");
                    }));

                    return true;
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        AddLog($"Error exporting topic data for {topicName}: {ex.Message}");
                        AddLog($"Stack trace: {ex.StackTrace}");
                    }));
                    return false;
                }
                finally
                {
                    // CRITICAL: Always dispose the pipeline to release file locks
                    // Add a small delay to ensure pipeline has fully stopped before disposing
                    if (tempPipeline != null)
                    {
                        try
                        {
                            // Wait a bit to ensure all pipeline operations have completed
                            System.Threading.Thread.Sleep(100);
                            
                            // Dispose pipeline safely
                            tempPipeline.Dispose();
                            tempPipeline = null;
                        }
                        catch (ObjectDisposedException)
                        {
                            // Pipeline already disposed, ignore
                            tempPipeline = null;
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                AddLog($"Warning: Error disposing pipeline: {ex.Message}");
                            }));
                            tempPipeline = null;
                        }
                    }
                }
            });
        }
    }
}

