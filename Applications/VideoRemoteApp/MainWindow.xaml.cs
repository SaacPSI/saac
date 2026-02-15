// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Psi;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Media;
using Microsoft.Psi.Remoting;
using Microsoft.Win32;
using Newtonsoft.Json;
using SAAC;
using SAAC.PipelineServices;

namespace VideoRemoteApp
{
    /// <summary>
    /// Main window for the Video Remote Application that manages video capture and streaming.
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        /// <summary>
        /// Event raised when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private List<string> notTriggerProperties;

        /// <summary>
        /// Sets a property value and raises the PropertyChanged event if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">Reference to the backing field.</param>
        /// <param name="value">The new value.</param>
        /// <param name="propertyName">The name of the property (automatically provided by CallerMemberName).</param>
        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                if (propertyName != null && !this.notTriggerProperties.Contains(propertyName))
                {
                    this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
                }

                field = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        private RendezVousPipelineConfiguration pipelineConfiguration;
        private string state = "Not Initialised";
        private bool isRemoteServer = true;
        private bool isStreaming = true;
        private bool isLocalRecording = true;
        private VideoRemoteAppConfiguration videoRemoteAppConfiguration;
        private string rendezVousServerIp = "localhost";
        private string rendezVousApplicationName;
        private string ipSelected;
        private string commandSource = "Server";
        private int commandPort;
        private int exportPort;
        private string localSessionName = string.Empty;
        private string log = string.Empty;
        private SetupState setupState;
        private DatasetPipeline? datasetPipeline;
        private LogStatus internalLog;
        private int captureInterval;
        private int cropX;
        private int cropY;
        private int cropWidth;
        private int cropHeight;
        private string selectedCroppingAreaName;

        /// <summary>
        /// Gets or sets the current application state.
        /// </summary>
        public string State
        {
            get => this.state;
            set => this.SetProperty(ref this.state, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the application is acting as a remote server.
        /// </summary>
        public bool IsRemoteServer
        {
            get => this.isRemoteServer;
            set => this.SetProperty(ref this.isRemoteServer, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether streaming is enabled.
        /// </summary>
        public bool IsStreaming
        {
            get => this.isStreaming;
            set => this.SetProperty(ref this.isStreaming, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether local recording is enabled.
        /// </summary>
        public bool IsLocalRecording
        {
            get => this.isLocalRecording;
            set => this.SetProperty(ref this.isLocalRecording, value);
        }

        /// <summary>
        /// Gets or sets the video remote application configuration.
        /// </summary>
        public VideoRemoteAppConfiguration VideoRemoteAppConfigurationUI
        {
            get => this.videoRemoteAppConfiguration;
            set => this.SetProperty(ref this.videoRemoteAppConfiguration, value);
        }

        /// <summary>
        /// Gets the list of available IP addresses.
        /// </summary>
        public List<string> IPsList { get; }

        /// <summary>
        /// Gets or sets the RendezVous server IP address.
        /// </summary>
        public string RendezVousServerIp
        {
            get => this.rendezVousServerIp;
            set => this.SetProperty(ref this.rendezVousServerIp, value);
        }

        /// <summary>
        /// Gets or sets the pipeline configuration.
        /// </summary>
        public RendezVousPipelineConfiguration PipelineConfigurationUI
        {
            get => this.pipelineConfiguration;
            set => this.SetProperty(ref this.pipelineConfiguration, value);
        }

        /// <summary>
        /// Gets or sets the RendezVous application name.
        /// </summary>
        public string RendezVousApplicationNameUI
        {
            get => this.rendezVousApplicationName;
            set => this.SetProperty(ref this.rendezVousApplicationName, value);
        }

        /// <summary>
        /// Gets or sets the selected IP address.
        /// </summary>
        public string IpSelectedUI
        {
            get => this.ipSelected;
            set => this.SetProperty(ref this.ipSelected, value);
        }

        /// <summary>
        /// Gets or sets the command source identifier.
        /// </summary>
        public string CommandSource
        {
            get => this.commandSource;
            set => this.SetProperty(ref this.commandSource, value);
        }

        /// <summary>
        /// Gets or sets the command port.
        /// </summary>
        public int CommandPort
        {
            get => this.commandPort;
            set => this.SetProperty(ref this.commandPort, value);
        }

        /// <summary>
        /// Gets or sets the export port for streaming.
        /// </summary>
        public int ExportPort
        {
            get => this.exportPort;
            set => this.SetProperty(ref this.exportPort, value);
        }

        /// <summary>
        /// Gets or sets the local session name for recording.
        /// </summary>
        public string LocalSessionName
        {
            get => this.localSessionName;
            set => this.SetProperty(ref this.localSessionName, value);
        }

        /// <summary>
        /// Gets or sets the local dataset path.
        /// </summary>
        public string LocalDatasetPath
        {
            get => this.PipelineConfigurationUI.DatasetPath;
            set => this.SetProperty(ref this.PipelineConfigurationUI.DatasetPath, value);
        }

        /// <summary>
        /// Gets or sets the local dataset name.
        /// </summary>
        public string LocalDatasetName
        {
            get => this.PipelineConfigurationUI.DatasetName;
            set => this.SetProperty(ref this.PipelineConfigurationUI.DatasetName, value);
        }

        /// <summary>
        /// Gets or sets the log text displayed in the UI.
        /// </summary>
        public string Log
        {
            get => this.log;
            set => this.SetProperty(ref this.log, value);
        }

        /// <summary>
        /// Represents the initialization state of the application.
        /// </summary>
        private enum SetupState
        {
            /// <summary>Application has not been initialized.</summary>
            NotInitialised,

            /// <summary>Pipeline has been initialized.</summary>
            PipelineInitialised,

            /// <summary>Video capture has been initialized.</summary>
            VideoInitialised,
        }

        /// <summary>
        /// Gets or sets the capture interval in milliseconds.
        /// </summary>
        public int CaptureInterval
        {
            get => this.captureInterval;
            set => this.SetProperty(ref this.captureInterval, value);
        }

        /// <summary>
        /// Gets or sets the crop X coordinate.
        /// </summary>
        public int CropX
        {
            get => this.cropX;
            set => this.SetProperty(ref this.cropX, value);
        }

        /// <summary>
        /// Gets or sets the crop Y coordinate.
        /// </summary>
        public int CropY
        {
            get => this.cropY;
            set => this.SetProperty(ref this.cropY, value);
        }

        /// <summary>
        /// Gets or sets the crop width.
        /// </summary>
        public int CropWidth
        {
            get => this.cropWidth;
            set => this.SetProperty(ref this.cropWidth, value);
        }

        /// <summary>
        /// Gets or sets the crop height.
        /// </summary>
        public int CropHeight
        {
            get => this.cropHeight;
            set => this.SetProperty(ref this.cropHeight, value);
        }

        /// <summary>
        /// Gets or sets the name of the selected cropping area.
        /// </summary>
        public string SelectedCroppingAreaName
        {
            get => this.selectedCroppingAreaName;
            set => this.SetProperty(ref this.selectedCroppingAreaName, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.internalLog = (log) =>
            {
                Application.Current?.Dispatcher?.Invoke(new Action(() =>
                {
                    this.Log += $"{log}\n";
                }));
            };

            this.notTriggerProperties = new List<string> { "Log", "State" };
            this.DataContext = this;
            this.pipelineConfiguration = new RendezVousPipelineConfiguration();
            this.pipelineConfiguration.ClockPort = this.pipelineConfiguration.CommandPort = 0;
            this.pipelineConfiguration.AutomaticPipelineRun = true;
            this.pipelineConfiguration.RecordIncomingProcess = false;

            this.videoRemoteAppConfiguration = new VideoRemoteAppConfiguration();

            this.IPsList = new List<string> { "localhost" };
            this.IPsList.AddRange(Dns.GetHostEntry(Dns.GetHostName()).AddressList.Select(ip => ip.ToString()));

            this.setupState = SetupState.NotInitialised;
            this.datasetPipeline = null;

            this.InitializeComponent();
            this.UpdateLayout();
            this.LoadConfigurations();

            this.SetupVideoTab();
            this.SetupNetworkTab();
            this.SetupLocalRecordingTab();
            this.RefreshUIFromConfiguration();
        }

        /// <summary>
        /// Sets up the video tab UI components and validation.
        /// </summary>
        private void SetupVideoTab()
        {
            // Validate Capture Interval as integer
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.VideoCaptureIntervalTextBox, int.TryParse);

            // Validate Encoding Level as integer
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.VideoEncodingLevelTextBox, int.TryParse);

            // Validate Cropping Area coordinates as integers
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.VideoCropXTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.VideoCropYTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.VideoCropWidthTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.VideoCropHeightTextBox, int.TryParse);

            // Initialize TextBox values from configuration
            this.VideoCaptureIntervalTextBox.Text = this.videoRemoteAppConfiguration.Interval.TotalMilliseconds.ToString();

            // Load cropping areas into ListBox
            this.RefreshCroppingAreasList();
        }

        /// <summary>
        /// Sets up the network tab UI components and validation.
        /// </summary>
        private void SetupNetworkTab()
        {
            // Validate Server IP address
            UiGenerator.SetTextBoxOutFocusChecker<System.Net.IPAddress>(this.RendezVousServerIpTextBox, UiGenerator.IPAddressTryParse);

            // Validate Server Port as integer
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.RendezVousPortTextBox, int.TryParse);

            // Validate Command Port as integer
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.CommandPortTextBox, int.TryParse);

            // Validate Export Port as integer
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.StreamingPortRangeStartTextBox, int.TryParse);

            this.UpdateNetworkTab();
        }

        /// <summary>
        /// Sets up the local recording tab UI components and validation.
        /// </summary>
        private void SetupLocalRecordingTab()
        {
            UiGenerator.SetTextBoxOutFocusChecker<Uri>(this.LocalRecordingDatasetDirectoryTextBox, UiGenerator.UriTryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<string>(this.LocalRecordingDatasetNameTextBox, UiGenerator.PathTryParse);
            this.LocalRecordingDatasetNameTextBox.LostFocus += UiGenerator.IsFileExistChecker("Dataset file already exist, make sure to use a different session name.", ".pds", this.LocalRecordingDatasetDirectoryTextBox);
        }

        /// <summary>
        /// Updates the network tab UI elements based on the remote server state.
        /// </summary>
        private void UpdateNetworkTab()
        {
            this.BtnStartNet.IsEnabled = this.isRemoteServer;
            foreach (UIElement networkUIElement in this.NetworkGrid.Children)
            {
                if (!(networkUIElement is CheckBox))
                {
                    networkUIElement.IsEnabled = this.isRemoteServer;
                }
            }

            this.isStreaming = this.isRemoteServer ? this.isStreaming : false;
            this.GeneralNetworkStreamingCheckBox.IsEnabled = this.NetworkStreamingCheckBox.IsEnabled = this.isRemoteServer;
        }

        /// <summary>
        /// Updates the local recording tab UI elements based on the local recording state.
        /// </summary>
        private void UpdateLocalRecordingTab()
        {
            foreach (UIElement networkUIElement in this.LocalRecordingGrid.Children)
            {
                if (!(networkUIElement is CheckBox))
                {
                    networkUIElement.IsEnabled = this.isLocalRecording;
                }
            }
        }

        /// <summary>
        /// Refreshes UI elements from the current configuration.
        /// </summary>
        private void RefreshUIFromConfiguration()
        {
            // Network Tab
            this.IsRemoteServer = Properties.Settings.Default.isServer;
            this.IsStreaming = Properties.Settings.Default.isStreaming;
            var ipResult = this.IPsList.Where((ip) => { return ip == Properties.Settings.Default.ipToUse; });
            if (ipResult.Count() > 0)
            {
                this.RendezVousHostComboBox.SelectedIndex = ipResult.Count() == 0 ? 0 : this.IPsList.IndexOf(ipResult.First());
                this.IpSelectedUI = ipResult.First();
            }

            this.PipelineConfigurationUI.RendezVousHost = Properties.Settings.Default.ipToUse;
            this.RendezVousServerIp = Properties.Settings.Default.rendezVousServerIp;
            this.PipelineConfigurationUI.RendezVousPort = (int)(Properties.Settings.Default.rendezVousServerPort);
            this.CommandSource = Properties.Settings.Default.commandSource;
            this.CommandPort = Properties.Settings.Default.commandPort;
            this.PipelineConfigurationUI.CommandPort = this.CommandPort;
            this.RendezVousApplicationNameUI = Properties.Settings.Default.applicationName;
            this.ExportPort = Properties.Settings.Default.streamingPortRangeStart;

            // Local Recording Tab
            this.IsLocalRecording = Properties.Settings.Default.isLocalRecording;
            this.LocalSessionName = Properties.Settings.Default.localSessionName;
            this.LocalDatasetPath = Properties.Settings.Default.datasetPath;
            this.LocalDatasetName = Properties.Settings.Default.datasetName;

            // Video Tab
            this.LoadConfigurations();
            this.CaptureInterval = (int)this.videoRemoteAppConfiguration.Interval.TotalMilliseconds;
            this.VideoCaptureIntervalTextBox.Text = this.CaptureInterval.ToString();
            this.VideoEncodingLevelTextBox.Text = this.videoRemoteAppConfiguration.EncodingVideoLevel.ToString();

            this.RefreshCroppingAreasList();

            Properties.Settings.Default.Save();
            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = false;
            this.UpdateLayout();
            this.UpdateLocalRecordingTab();
        }

        /// <summary>
        /// Loads configuration settings from application properties.
        /// </summary>
        private void LoadConfigurations()
        {
            // Load video configurations
            int captureIntervalMs = (int)Properties.Settings.Default.captureInterval;
            this.videoRemoteAppConfiguration.Interval = TimeSpan.FromMilliseconds(captureIntervalMs > 0 ? captureIntervalMs : 100);
            this.videoRemoteAppConfiguration.EncodingVideoLevel = Properties.Settings.Default.encodingLevel > 0 ? Properties.Settings.Default.encodingLevel : 90;

            // Load cropping areas from JSON
            string croppingAreasJson = Properties.Settings.Default.croppingAreasJson;
            if (!string.IsNullOrEmpty(croppingAreasJson))
            {
                try
                {
                    var loadedAreas = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(croppingAreasJson);
                    if (loadedAreas != null)
                    {
                        this.videoRemoteAppConfiguration.CroppingAreas.Clear();
                        foreach (var area in loadedAreas)
                        {
                            if (area.Value.Length == 4)
                            {
                                this.videoRemoteAppConfiguration.CroppingAreas[area.Key] = new System.Drawing.Rectangle(
                                    area.Value[0], area.Value[1], area.Value[2], area.Value[3]);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.AddLog($"Error loading cropping areas: {ex.Message}");
                }
            }

            // Create default full screen cropping area if none exists
            if (this.videoRemoteAppConfiguration.CroppingAreas.Count == 0)
            {
                // Get primary screen dimensions
                int screenWidth = (int)System.Windows.SystemParameters.PrimaryScreenWidth;
                int screenHeight = (int)System.Windows.SystemParameters.PrimaryScreenHeight;

                this.videoRemoteAppConfiguration.CroppingAreas["FullScreen"] = new System.Drawing.Rectangle(0, 0, screenWidth, screenHeight);
            }
        }

        /// <summary>
        /// Refreshes the configuration from UI elements and saves to settings.
        /// </summary>
        private void RefreshConfigurationFromUI()
        {
            // Network Tab
            Properties.Settings.Default.isServer = this.IsRemoteServer;
            Properties.Settings.Default.isStreaming = this.IsStreaming;
            Properties.Settings.Default.ipToUse = this.IpSelectedUI;
            Properties.Settings.Default.rendezVousServerIp = this.RendezVousServerIp;
            Properties.Settings.Default.rendezVousServerPort = (uint)this.PipelineConfigurationUI.RendezVousPort;
            Properties.Settings.Default.commandPort = this.CommandPort;
            Properties.Settings.Default.commandSource = this.CommandSource;
            Properties.Settings.Default.applicationName = this.RendezVousApplicationNameUI;
            Properties.Settings.Default.streamingPortRangeStart = this.ExportPort;

            // Local Recording Tab
            Properties.Settings.Default.isLocalRecording = this.IsLocalRecording;
            Properties.Settings.Default.localSessionName = this.LocalSessionName;
            Properties.Settings.Default.datasetPath = this.LocalDatasetPath;
            Properties.Settings.Default.datasetName = this.LocalDatasetName;

            // Video Tab - Update capture interval from TextBox
            if (int.TryParse(this.VideoCaptureIntervalTextBox.Text, out int interval))
            {
                this.videoRemoteAppConfiguration.Interval = TimeSpan.FromMilliseconds(interval);
                Properties.Settings.Default.captureInterval = (uint)interval;
            }

            // Update encoding level from TextBox
            if (int.TryParse(this.VideoEncodingLevelTextBox.Text, out int encodingLevel))
            {
                this.videoRemoteAppConfiguration.EncodingVideoLevel = encodingLevel;
                Properties.Settings.Default.encodingLevel = encodingLevel;
            }

            // Save cropping areas as JSON
            try
            {
                var areasToSave = new Dictionary<string, int[]>();
                foreach (var area in this.videoRemoteAppConfiguration.CroppingAreas)
                {
                    areasToSave[area.Key] = new int[] { area.Value.X, area.Value.Y, area.Value.Width, area.Value.Height };
                }

                Properties.Settings.Default.croppingAreasJson = JsonConvert.SerializeObject(areasToSave);
            }
            catch (Exception ex)
            {
                this.AddLog($"Error saving cropping areas: {ex.Message}");
            }

            Properties.Settings.Default.Save();
            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = false;
        }

        /// <summary>
        /// Handles received commands from the RendezVous server.
        /// </summary>
        /// <param name="source">The command source.</param>
        /// <param name="message">The command message.</param>
        private void CommandRecieved(string source, Message<(RendezVousPipeline.Command, string)> message)
        {
            if ($"{this.CommandSource}-Command" != source)
            {
                return;
            }

            var args = message.Data.Item2.Split([';']);

            if (args[0] != this.RendezVousApplicationNameUI && args[0] != "*")
            {
                return;
            }

            this.datasetPipeline?.Log($"CommandRecieved with {message.Data.Item1} command, args: {message.Data.Item2}.");
            switch (message.Data.Item1)
            {
                case RendezVousPipeline.Command.Initialize:
                    this.UpdateConfigurationFromArgs(args);
                    break;
                case RendezVousPipeline.Command.Run:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        this.Start();
                    }));
                    break;
                case RendezVousPipeline.Command.Stop:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        this.Stop();
                    }));
                    break;
                case RendezVousPipeline.Command.Close:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        this.Close();
                    }));
                    break;
                case RendezVousPipeline.Command.Status:
                    (this.datasetPipeline as RendezVousPipeline)?.SendCommand(RendezVousPipeline.Command.Status, this.CommandSource, this.datasetPipeline?.Pipeline.StartTime == DateTime.MinValue ? "Waiting" : "Running");
                    break;
            }
        }

        /// <summary>
        /// Updates configuration from command line arguments.
        /// </summary>
        /// <param name="args">The command line arguments array.</param>
        /// <returns>True if configuration was updated successfully; otherwise false.</returns>
        private bool UpdateConfigurationFromArgs(string[] args)
        {
            try
            {
                // Placeholder for future argument parsing
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                this.RefreshUIFromConfiguration();
            }));

            return true;
        }

        /// <summary>
        /// Sets up the pipeline based on current configuration.
        /// </summary>
        private void SetupPipeline()
        {
            if (this.setupState >= SetupState.PipelineInitialised)
            {
                return;
            }

            if (!this.isRemoteServer && !this.isLocalRecording)
            {
                MessageBox.Show("You cannot start the application without Network or Local Recording.", "Configuration error", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                return;
            }

            this.pipelineConfiguration.Diagnostics = DatasetPipeline.DiagnosticsMode.Off;
            this.pipelineConfiguration.AutomaticPipelineRun = false;
            this.pipelineConfiguration.CommandDelegate = this.CommandRecieved;
            this.pipelineConfiguration.Debug = false;
            this.pipelineConfiguration.RecordIncomingProcess = false;
            this.pipelineConfiguration.CommandPort = this.CommandPort;
            this.pipelineConfiguration.ClockPort = 0;
            if (this.isLocalRecording)
            {
                this.pipelineConfiguration.DatasetPath = this.LocalDatasetPath;
                this.pipelineConfiguration.DatasetName = this.LocalDatasetName;
            }

            this.pipelineConfiguration.RendezVousHost = this.IpSelectedUI;

            if (this.isRemoteServer)
            {
                this.datasetPipeline = new RendezVousPipeline(this.pipelineConfiguration, this.rendezVousApplicationName, this.RendezVousServerIp, this.internalLog);
            }
            else
            {
                this.datasetPipeline = new DatasetPipeline(this.pipelineConfiguration);
            }

            this.setupState = SetupState.PipelineInitialised;
        }

        /// <summary>
        /// Sets up video capture with configured cropping areas.
        /// </summary>
        private void SetupVideo()
        {
            if (this.setupState >= SetupState.VideoInitialised)
            {
                return;
            }

            if (this.datasetPipeline is null)
            {
                this.SetupPipeline();
            }

            // Verify that at least one valid cropping area exists
            bool hasValidCroppingArea = this.videoRemoteAppConfiguration.CroppingAreas.Any(area => !area.Value.IsEmpty && area.Value.Width > 0 && area.Value.Height > 0);
            if (!hasValidCroppingArea)
            {
                (this.datasetPipeline as RendezVousPipeline)?.SendCommand(RendezVousPipeline.Command.Status, this.commandSource, "Error");
                MessageBox.Show("At least one valid cropping area is required. Please add a cropping area in the Video tab.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.AddLog("Error: No valid cropping area configured.");
                return;
            }

            WindowCaptureConfiguration cfg = new WindowCaptureConfiguration() { Interval = this.videoRemoteAppConfiguration.Interval };
            WindowCapture capture = new WindowCapture(this.datasetPipeline.Pipeline, cfg);
            IProducer<Shared<Microsoft.Psi.Imaging.EncodedImage>> videoSource;
            Rendezvous.Process proc = new Rendezvous.Process(this.rendezVousApplicationName);
            foreach (KeyValuePair<string, System.Drawing.Rectangle> cropArea in this.videoRemoteAppConfiguration.CroppingAreas)
            {
                if (cropArea.Value.IsEmpty || cropArea.Value.Width <= 0 || cropArea.Value.Height <= 0)
                {
                    this.AddLog($"Skipping invalid cropping area: {cropArea.Key}");
                    continue;
                }

                SAAC.Helpers.ImageCropper cropRect = new SAAC.Helpers.ImageCropper(this.datasetPipeline.Pipeline, cropArea.Value);
                capture.Out.PipeTo(cropRect.In);
                this.ProcessProducer(cropRect.Out.EncodeJpeg(this.videoRemoteAppConfiguration.EncodingVideoLevel, DeliveryPolicy.LatestMessage), cropArea.Key, this.ExportPort++, ref proc);
                this.AddLog($"Cropping area '{cropArea.Key}' configured: X={cropArea.Value.X}, Y={cropArea.Value.Y}, Width={cropArea.Value.Width}, Height={cropArea.Value.Height}");
            }

            (this.datasetPipeline as RendezVousPipeline)?.AddProcess(proc);
            this.AddLog(this.State = "Video initialised");
            this.setupState = SetupState.VideoInitialised;
        }

        /// <summary>
        /// Processes a video producer for export or storage.
        /// </summary>
        /// <param name="producer">The video producer.</param>
        /// <param name="streamName">The stream name.</param>
        /// <param name="exportPort">The export port.</param>
        /// <param name="proc">The RendezVous process.</param>
        private void ProcessProducer(IProducer<Shared<EncodedImage>> producer, string streamName, int exportPort, ref Rendezvous.Process proc)
        {
            if (this.datasetPipeline is null)
            {
                return;
            }

            if (this.isRemoteServer)
            {
                RemoteExporter imageExporter = new RemoteExporter(this.datasetPipeline.Pipeline, exportPort, TransportKind.Tcp);
                imageExporter.Exporter.Write(producer, streamName);
                proc.AddEndpoint(imageExporter.ToRendezvousEndpoint(this.pipelineConfiguration.RendezVousHost));
            }
            else
            {
                this.datasetPipeline.CreateStore(this.datasetPipeline.Pipeline, this.datasetPipeline.CreateOrGetSession(this.LocalSessionName), streamName, this.rendezVousApplicationName, producer);
            }
        }

        /// <summary>
        /// Stops the pipeline and disposes of resources.
        /// </summary>
        private void Stop()
        {
            this.AddLog(this.State = "Stopping");
            (this.datasetPipeline as RendezVousPipeline)?.SendCommand(RendezVousPipeline.Command.Status, this.commandSource, "Stopping");
            if (this.datasetPipeline is RendezVousPipeline)
            {
                (this.datasetPipeline as RendezVousPipeline)?.Dispose();
            }
            else
            {
                this.datasetPipeline?.Dispose();
            }

            Application.Current.Shutdown();
        }

        /// <summary>
        /// Starts the network connection to the RendezVous server.
        /// </summary>
        private void StartNetwork()
        {
            this.SetupPipeline();
            if (this.setupState == SetupState.PipelineInitialised)
            {
                this.BtnStartNet.IsEnabled = false;
                this.AddLog(this.State = "Waiting for server");
                (this.datasetPipeline as RendezVousPipeline)?.Start((d) => { Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        this.AddLog(this.State = "Connected to server");
                        (this.datasetPipeline as RendezVousPipeline)?.SendCommand(RendezVousPipeline.Command.Status, this.commandSource, "Waiting");
                    }));
                });
            }
        }

        /// <summary>
        /// Starts the pipeline and video capture.
        /// </summary>
        private void Start()
        {
            this.SetupPipeline();
            this.SetupVideo();
            if (this.setupState == SetupState.VideoInitialised)
            {
                this.BtnStart.IsEnabled = this.BtnStartNet.IsEnabled = false;
                var remotePipeline = this.datasetPipeline as RendezVousPipeline;
                if (remotePipeline != null)
                {
                    remotePipeline.Start();
                    remotePipeline.RunPipelineAndSubpipelines();
                    remotePipeline.SendCommand(RendezVousPipeline.Command.Status, this.commandSource, "Running");
                }
                else
                {
                    this.datasetPipeline?.RunPipelineAndSubpipelines();
                }

                this.AddLog(this.State = "Started");
            }
        }

        /// <summary>
        /// Adds a log message to the log window.
        /// </summary>
        /// <param name="logMessage">The log message to add.</param>
        private void AddLog(string logMessage)
        {
            this.Log += $"{logMessage}\n";
        }

        /// <summary>
        /// Handles the log text box text changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Log_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            TextBox? log = sender as TextBox;
            if (log == null)
            {
                return;
            }

            log.CaretIndex = log.Text.Length;
            log.ScrollToEnd();
        }

        /// <summary>
        /// Handles the window closing event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            this.Stop();
            this.RefreshConfigurationFromUI();
            base.OnClosing(e);
        }

        /// <summary>
        /// Handles the start RendezVous button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnStartRendezVous(object sender, RoutedEventArgs e)
        {
            this.State = "Initializing RendezVous";
            this.RefreshConfigurationFromUI();
            this.StartNetwork();
            e.Handled = true;
        }

        /// <summary>
        /// Handles the start all button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnStartAll(object sender, RoutedEventArgs e)
        {
            this.State = "Initializing Video";
            this.RefreshConfigurationFromUI();
            this.Start();
            e.Handled = true;
        }

        /// <summary>
        /// Handles the quit button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            this.Close();
            e.Handled = true;
        }

        /// <summary>
        /// Handles the load configuration button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnLoadConfiguration(object sender, RoutedEventArgs e)
        {
            this.RefreshUIFromConfiguration();
            this.AddLog(this.State = "Configuration Loaded");
            e.Handled = true;
        }

        /// <summary>
        /// Handles the save configuration button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnSaveConfiguration(object sender, RoutedEventArgs e)
        {
            this.RefreshConfigurationFromUI();
            this.AddLog(this.State = "Configuration Saved");
            e.Handled = true;
        }

        /// <summary>
        /// Handles the activate network checkbox click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CkbActivateNetwork(object sender, RoutedEventArgs e)
        {
            this.UpdateNetworkTab();
            e.Handled = true;
        }

        /// <summary>
        /// Handles the activate streaming checkbox click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CkbActivateStreaming(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Handles the activate local recording checkbox click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CkbActivateLocalRecording(object sender, RoutedEventArgs e)
        {
            this.UpdateLocalRecordingTab();
            e.Handled = true;
        }

        /// <summary>
        /// Handles the local recording dataset directory button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void LocalRecordingDatasetDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            UiGenerator.FolderPicker openFileDialog = new UiGenerator.FolderPicker();
            if (openFileDialog.ShowDialog() == true)
            {
                this.LocalDatasetPath = openFileDialog.ResultName;
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            }
        }

        /// <summary>
        /// Handles the local recording dataset name button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void LocalRecordingDatasetNameButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Dataset (*.pds)|*.pds";
            if (openFileDialog.ShowDialog() == true)
            {
                this.LocalDatasetPath = openFileDialog.FileName.Substring(0, openFileDialog.FileName.IndexOf(openFileDialog.SafeFileName));
                this.LocalDatasetName = openFileDialog.SafeFileName;
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            }
        }

        /// <summary>
        /// Handles the video capture interval text box text changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void VideoCaptureIntervalTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
        }

        /// <summary>
        /// Handles the video encoding level text box text changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void VideoEncodingLevelTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
        }

        /// <summary>
        /// Handles the video crop coordinate text box text changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void VideoCropCoordinateTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;

            // Update the cropping area in real-time if all fields are valid
            if (this.SelectedCroppingAreaName != null &&
                int.TryParse(this.VideoCropXTextBox.Text, out int x) &&
                int.TryParse(this.VideoCropYTextBox.Text, out int y) &&
                int.TryParse(this.VideoCropWidthTextBox.Text, out int width) &&
                int.TryParse(this.VideoCropHeightTextBox.Text, out int height))
            {
                this.videoRemoteAppConfiguration.CroppingAreas[this.SelectedCroppingAreaName] =
                    new System.Drawing.Rectangle(x, y, width, height);
            }
        }

        /// <summary>
        /// Refreshes the cropping areas list in the UI.
        /// </summary>
        private void RefreshCroppingAreasList()
        {
            this.CroppingAreaListBox.Items.Clear();
            foreach (var areaName in this.videoRemoteAppConfiguration.CroppingAreas.Keys)
            {
                this.CroppingAreaListBox.Items.Add(areaName);
            }

            // Clear edit fields if no areas
            if (this.CroppingAreaListBox.Items.Count == 0)
            {
                this.ClearCroppingAreaEditFields();
            }
        }

        /// <summary>
        /// Clears the cropping area edit fields.
        /// </summary>
        private void ClearCroppingAreaEditFields()
        {
            this.VideoCropNameTextBox.Text = string.Empty;
            this.VideoCropXTextBox.Text = "0";
            this.VideoCropYTextBox.Text = "0";
            this.VideoCropWidthTextBox.Text = "0";
            this.VideoCropHeightTextBox.Text = "0";
            this.SelectedCroppingAreaName = null;
        }

        /// <summary>
        /// Handles the cropping area list box selection changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CroppingAreaListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (this.CroppingAreaListBox.SelectedItem is string selectedName)
            {
                this.SelectedCroppingAreaName = selectedName;
                if (this.videoRemoteAppConfiguration.CroppingAreas.TryGetValue(selectedName, out var rect))
                {
                    this.VideoCropNameTextBox.Text = selectedName;
                    this.VideoCropXTextBox.Text = rect.X.ToString();
                    this.VideoCropYTextBox.Text = rect.Y.ToString();
                    this.VideoCropWidthTextBox.Text = rect.Width.ToString();
                    this.VideoCropHeightTextBox.Text = rect.Height.ToString();
                }
            }

            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
        }

        /// <summary>
        /// Handles the video crop name text box text changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void VideoCropNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (this.SelectedCroppingAreaName != null && !string.IsNullOrWhiteSpace(this.VideoCropNameTextBox.Text))
            {
                string newName = this.VideoCropNameTextBox.Text;
                if (newName != this.SelectedCroppingAreaName)
                {
                    // Rename the cropping area
                    if (this.videoRemoteAppConfiguration.CroppingAreas.TryGetValue(this.SelectedCroppingAreaName, out var rect))
                    {
                        this.videoRemoteAppConfiguration.CroppingAreas.Remove(this.SelectedCroppingAreaName);
                        this.videoRemoteAppConfiguration.CroppingAreas[newName] = rect;
                        this.SelectedCroppingAreaName = newName;
                        this.RefreshCroppingAreasList();
                        this.CroppingAreaListBox.SelectedItem = newName;
                    }
                }
            }

            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
        }

        /// <summary>
        /// Handles the add cropping area button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnAddCroppingArea(object sender, RoutedEventArgs e)
        {
            // Create a new cropping area with a default name
            int count = this.videoRemoteAppConfiguration.CroppingAreas.Count + 1;
            string newName = $"CropArea_{count}";
            while (this.videoRemoteAppConfiguration.CroppingAreas.ContainsKey(newName))
            {
                count++;
                newName = $"CropArea_{count}";
            }

            this.videoRemoteAppConfiguration.CroppingAreas[newName] = new System.Drawing.Rectangle(0, 0, 100, 100);
            this.RefreshCroppingAreasList();
            this.CroppingAreaListBox.SelectedItem = newName;
            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            this.AddLog($"Cropping area added: {newName}");
        }

        /// <summary>
        /// Handles the delete cropping area button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnDeleteCroppingArea(object sender, RoutedEventArgs e)
        {
            if (this.CroppingAreaListBox.SelectedItem is string selectedName)
            {
                this.videoRemoteAppConfiguration.CroppingAreas.Remove(selectedName);
                this.RefreshCroppingAreasList();
                this.AddLog($"Cropping area deleted: {selectedName}");
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            }
        }

        /// <summary>
        /// Handles the select cropping area button click event to open the crop selection window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnSelectCroppingArea(object sender, RoutedEventArgs e)
        {
            if (this.SelectedCroppingAreaName == null)
            {
                MessageBox.Show("Please select or create a cropping area first.", "No Area Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CropSelectionWindow cropWindow = new CropSelectionWindow();
            if (cropWindow.ShowDialog() == true)
            {
                var selectedRect = cropWindow.SelectedRectangle;
                this.videoRemoteAppConfiguration.CroppingAreas[this.SelectedCroppingAreaName] = selectedRect;

                this.VideoCropXTextBox.Text = selectedRect.X.ToString();
                this.VideoCropYTextBox.Text = selectedRect.Y.ToString();
                this.VideoCropWidthTextBox.Text = selectedRect.Width.ToString();
                this.VideoCropHeightTextBox.Text = selectedRect.Height.ToString();

                this.AddLog($"Cropping area '{this.SelectedCroppingAreaName}' set: X={selectedRect.X}, Y={selectedRect.Y}, Width={selectedRect.Width}, Height={selectedRect.Height}");
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            }

            e.Handled = true;
        }
    }
}
