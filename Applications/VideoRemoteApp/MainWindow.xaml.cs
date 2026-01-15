using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Psi;
using SAAC.PipelineServices;
using SAAC.GlobalHelpers;
using System.Windows.Controls;
using SAAC;
using Microsoft.Psi.Media;
using Microsoft.Psi.Imaging;
using System.Diagnostics;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Data;
using System.IO;
using Microsoft.Win32;

namespace VideoRemoteApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private List<string> notTriggerProperties;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                if (propertyName != null)
                {
                    bool enableConfigurationButton = !notTriggerProperties.Contains(propertyName);
                    BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = enableConfigurationButton;
                }
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private RendezVousPipelineConfiguration pipelineConfiguration;

        // General Tab
        private string state = "Not Initialised";
        public string State
        {
            get => state;
            set => SetProperty(ref state, value);
        }

        private bool isRemoteServer = true;
        public bool IsRemoteServer
        {
            get => isRemoteServer;
            set => SetProperty(ref isRemoteServer, value);
        }

        private bool isStreaming = true;
        public bool IsStreaming
        {
            get => isStreaming;
            set => SetProperty(ref isStreaming, value);
        }

        private bool isLocalRecording = true;
        public bool IsLocalRecording
        {
            get => isLocalRecording;
            set => SetProperty(ref isLocalRecording, value);
        }

        // Video Tab
        private VideoRemoteAppConfiguration videoRemoteAppConfiguration;

        public VideoRemoteAppConfiguration VideoRemoteAppConfigurationUI
        {
            get => videoRemoteAppConfiguration;
            set => SetProperty(ref videoRemoteAppConfiguration, value);
        }

        // Network Tab
        public List<string> IPsList { get; }

        private string rendezVousServerIp = "localhost";
        public string RendezVousServerIp
        {
            get => rendezVousServerIp;
            set => SetProperty(ref rendezVousServerIp, value);
        }

        public RendezVousPipelineConfiguration PipelineConfigurationUI
        {
            get => pipelineConfiguration;
            set => SetProperty(ref pipelineConfiguration, value);
        }

        private string rendezVousApplicationName;
        public string RendezVousApplicationNameUI
        {
            get => rendezVousApplicationName;
            set => SetProperty(ref rendezVousApplicationName, value);
        }

        private string ipSelected;
        public string IpSelectedUI
        {
            get => ipSelected;
            set => SetProperty(ref ipSelected, value);
        }

        private string commandSource = "Server";
        public string CommandSource
        {
            get => commandSource;
            set => SetProperty(ref commandSource, value);
        }

        private int commandPort;
        public int CommandPort
        {
            get => commandPort;
            set => SetProperty(ref commandPort, value);
        }

        // LocalRecording Tab
        private string localSessionName = "";
        public string LocalSessionName
        {
            get => localSessionName;
            set => SetProperty(ref localSessionName, value);
        }

        public string LocalDatasetPath
        {
            get => PipelineConfigurationUI.DatasetPath;
            set => SetProperty(ref PipelineConfigurationUI.DatasetPath, value);
        }

        public string LocalDatasetName
        {
            get => PipelineConfigurationUI.DatasetName;
            set => SetProperty(ref PipelineConfigurationUI.DatasetName, value);
        }

        // Log Tab
        private string log = "";
        public string Log
        {
            get => log;
            set => SetProperty(ref log, value);
        }

        // Variables
        private enum SetupState
        {
            NotInitialised,
            PipelineInitialised,
            VideoInitialised
        }

        private SetupState setupState;
        private DatasetPipeline? datasetPipeline;
        private LogStatus internalLog;

        private int captureInterval;
        public int CaptureInterval
        {
            get => captureInterval;
            set => SetProperty(ref captureInterval, value);
        }

        private int cropX;
        public int CropX
        {
            get => cropX;
            set => SetProperty(ref cropX, value);
        }

        private int cropY;
        public int CropY
        {
            get => cropY;
            set => SetProperty(ref cropY, value);
        }

        private int cropWidth;
        public int CropWidth
        {
            get => cropWidth;
            set => SetProperty(ref cropWidth, value);
        }

        private int cropHeight;
        public int CropHeight
        {
            get => cropHeight;
            set => SetProperty(ref cropHeight, value);
        }

        private string selectedCroppingAreaName;
        public string SelectedCroppingAreaName
        {
            get => selectedCroppingAreaName;
            set => SetProperty(ref selectedCroppingAreaName, value);
        }

        public MainWindow()
        {
            internalLog = (log) =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    Log += $"{log}\n";
                }));
            };

            notTriggerProperties = new List<string> { "Log", "State" };
            DataContext = this;
            pipelineConfiguration = new RendezVousPipelineConfiguration();
            pipelineConfiguration.ClockPort = pipelineConfiguration.CommandPort = 0;
            pipelineConfiguration.AutomaticPipelineRun = true;
            pipelineConfiguration.RecordIncomingProcess = false;

            videoRemoteAppConfiguration = new VideoRemoteAppConfiguration();

            IPsList = new List<string> { "localhost" };
            IPsList.AddRange(Dns.GetHostEntry(Dns.GetHostName()).AddressList.Select(ip => ip.ToString()));

            setupState = SetupState.NotInitialised;
            datasetPipeline = null;

            LoadConfigurations();
            InitializeComponent();
            UpdateLayout();

            SetupVideoTab();
            SetupNetworkTab();
            SetupLocalRecordingTab();
            RefreshUIFromConfiguration();
        }

        private void SetupVideoTab()
        {
            // Validate Capture Interval as integer
            UiGenerator.SetTextBoxPreviewTextChecker<int>(VideoCaptureIntervalTextBox, int.TryParse);

            // Validate Encoding Level as integer
            UiGenerator.SetTextBoxPreviewTextChecker<int>(VideoEncodingLevelTextBox, int.TryParse);

            // Validate Cropping Area coordinates as integers
            UiGenerator.SetTextBoxPreviewTextChecker<int>(VideoCropXTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(VideoCropYTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(VideoCropWidthTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(VideoCropHeightTextBox, int.TryParse);
            
            // Initialize TextBox values from configuration
            VideoCaptureIntervalTextBox.Text = videoRemoteAppConfiguration.Interval.TotalMilliseconds.ToString();
            
            // Load cropping areas into ListBox
            RefreshCroppingAreasList();
        }

        private void SetupNetworkTab()
        {
            // Validate Server IP address
            UiGenerator.SetTextBoxOutFocusChecker<System.Net.IPAddress>(RendezVousServerIpTextBox, UiGenerator.IPAddressTryParse);
            
            // Validate Server Port as integer
            UiGenerator.SetTextBoxPreviewTextChecker<int>(RendezVousPortTextBox, int.TryParse);
            
            // Validate Command Port as integer
            UiGenerator.SetTextBoxPreviewTextChecker<int>(CommandPortTextBox, int.TryParse);
            
            UpdateNetworkTab();
        }

        private void SetupLocalRecordingTab()
        {
            UiGenerator.SetTextBoxOutFocusChecker<Uri>(LocalRecordingDatasetDirectoryTextBox, UiGenerator.UriTryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<string>(LocalRecordingDatasetNameTextBox, UiGenerator.PathTryParse);
            LocalRecordingDatasetNameTextBox.LostFocus += UiGenerator.IsFileExistChecker("Dataset file already exist, make sure to use a different session name.", ".pds", LocalRecordingDatasetDirectoryTextBox);
        }

        private void UpdateNetworkTab()
        {
            BtnStartNet.IsEnabled = isRemoteServer;
            foreach (UIElement networkUIElement in NetworkGrid.Children)
                if (!(networkUIElement is CheckBox))
                    networkUIElement.IsEnabled = isRemoteServer;
            isStreaming = isRemoteServer ? isStreaming : false;
            GeneralNetworkStreamingCheckBox.IsEnabled = NetworkStreamingCheckBox.IsEnabled = isRemoteServer;
        }

        private void UpdateLocalRecordingTab()
        {
            foreach (UIElement networkUIElement in LocalRecordingGrid.Children)
                if (!(networkUIElement is CheckBox))
                    networkUIElement.IsEnabled = isLocalRecording;
        }

        private void RefreshUIFromConfiguration()
        {
            // Video Tab
            CaptureInterval = (int)videoRemoteAppConfiguration.Interval.TotalMilliseconds;
            VideoCaptureIntervalTextBox.Text = CaptureInterval.ToString();
            
            RefreshCroppingAreasList();

            // Network Tab
            IsRemoteServer = Properties.Settings.Default.isServer;
            IsStreaming = Properties.Settings.Default.isStreaming;
            var ipResult = IPsList.Where((ip) => { return ip == Properties.Settings.Default.ipToUse; });
            RendezVousHostComboBox.SelectedIndex = ipResult.Count() == 0 ? 0 : IPsList.IndexOf(ipResult.First());
            PipelineConfigurationUI.RendezVousHost = Properties.Settings.Default.ipToUse;
            RendezVousServerIp = Properties.Settings.Default.rendezVousServerIp;
            PipelineConfigurationUI.RendezVousPort = (int)(Properties.Settings.Default.rendezVousServerPort);
            CommandPort = Properties.Settings.Default.commandPort;
            PipelineConfigurationUI.CommandPort = CommandPort;
            RendezVousApplicationNameUI = Properties.Settings.Default.applicationName;

            // Local Recording Tab
            IsLocalRecording = Properties.Settings.Default.isLocalRecording;
            LocalSessionName = Properties.Settings.Default.localSessionName;
            LocalDatasetPath = Properties.Settings.Default.datasetPath;
            LocalDatasetName = Properties.Settings.Default.datasetName;

            LoadConfigurations();
            Properties.Settings.Default.Save();
            UpdateLayout();
        }

        private void LoadConfigurations()
        {           
            // Load CommandPort
            CommandPort = Properties.Settings.Default.commandPort;
            PipelineConfigurationUI.CommandPort = CommandPort;

            // Load video configurations
            videoRemoteAppConfiguration.Interval = TimeSpan.FromMilliseconds(CaptureInterval > 0 ? CaptureInterval : Properties.Settings.Default.captureInterval);
            videoRemoteAppConfiguration.EncodingVideoLevel = Properties.Settings.Default.encodingLevel;
        }

        private void RefreshConfigurationFromUI()
        {
            // Update capture interval from TextBox
            if (int.TryParse(VideoCaptureIntervalTextBox.Text, out int interval))
            {
                videoRemoteAppConfiguration.Interval = TimeSpan.FromMilliseconds(interval);
            }

            // Save the current selected cropping area if valid
            if (SelectedCroppingAreaName != null &&
                int.TryParse(VideoCropXTextBox.Text, out int x) &&
                int.TryParse(VideoCropYTextBox.Text, out int y) &&
                int.TryParse(VideoCropWidthTextBox.Text, out int width) &&
                int.TryParse(VideoCropHeightTextBox.Text, out int height))
            {
                videoRemoteAppConfiguration.CroppingAreas[SelectedCroppingAreaName] = 
                    new System.Drawing.Rectangle(x, y, width, height);
            }

            // Network Tab
            Properties.Settings.Default.isServer = IsRemoteServer;
            Properties.Settings.Default.isStreaming = IsStreaming;
            Properties.Settings.Default.ipToUse = PipelineConfigurationUI.RendezVousHost;
            Properties.Settings.Default.rendezVousServerIp = RendezVousServerIp;
            Properties.Settings.Default.rendezVousServerPort = (uint)PipelineConfigurationUI.RendezVousPort;
            Properties.Settings.Default.commandPort = CommandPort;
            Properties.Settings.Default.applicationName = RendezVousApplicationNameUI;

            // Video Tab
            Properties.Settings.Default.captureInterval = (uint)videoRemoteAppConfiguration.Interval.TotalMilliseconds;
            Properties.Settings.Default.encodingLevel = videoRemoteAppConfiguration.EncodingVideoLevel;

            Properties.Settings.Default.cropX = CropX;
            Properties.Settings.Default.cropY = CropY;
            Properties.Settings.Default.cropWidth = CropWidth;
            Properties.Settings.Default.cropHeight = CropHeight;

            // Local Recording Tab
            Properties.Settings.Default.isLocalRecording = IsLocalRecording;
            Properties.Settings.Default.localSessionName = LocalSessionName;
            Properties.Settings.Default.datasetPath = LocalDatasetPath;
            Properties.Settings.Default.datasetName = LocalDatasetName;

            Properties.Settings.Default.Save();
        }

        private void CommandRecieved(string source, Message<(RendezVousPipeline.Command, string)> message)
        {
            if ($"{CommandSource}-Command" != source)
                return;

            var args = message.Data.Item2.Split([';']);

            if (args[0] != RendezVousApplicationNameUI || args[0] == "*")
                return;

            datasetPipeline?.Log($"CommandRecieved with {message.Data.Item1} command, args: {message.Data.Item2}.");
            switch (message.Data.Item1)
            {
                case RendezVousPipeline.Command.Initialize:
                    UpdateConfigurationFromArgs(args);
                    break;
                case RendezVousPipeline.Command.Run:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        SetupVideo();
                    }));
                    break;
                case RendezVousPipeline.Command.Stop:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        datasetPipeline?.Stop();
                    }));
                    break;
                case RendezVousPipeline.Command.Close:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        Stop();
                        Close();
                    }));
                    break;
                case RendezVousPipeline.Command.Status:
                    (datasetPipeline as RendezVousPipeline)?.SendCommand(RendezVousPipeline.Command.Status, source, datasetPipeline == null ? "Not Initialised" : datasetPipeline.Pipeline.StartTime.ToString());
                    break;
            }
        }

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
                RefreshUIFromConfiguration();
            }));

            return true;
        }

        private void SetupPipeline()
        {
            if (setupState >= SetupState.PipelineInitialised)
                return;

            if (!isRemoteServer && !isLocalRecording)
            {
                MessageBox.Show("You cannot start the application without Network or Local Recording.", "Configuration error", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                return;
            }

            pipelineConfiguration.Diagnostics = DatasetPipeline.DiagnosticsMode.Off;
            pipelineConfiguration.AutomaticPipelineRun = false;
            pipelineConfiguration.CommandDelegate = CommandRecieved;
            pipelineConfiguration.Debug = false;
            pipelineConfiguration.RecordIncomingProcess = false;
            pipelineConfiguration.ClockPort = 0;
            pipelineConfiguration.CommandPort = CommandPort;
            pipelineConfiguration.DatasetPath = LocalDatasetPath;
            pipelineConfiguration.DatasetName = LocalDatasetName;

            if (isRemoteServer)
            {
                datasetPipeline = new RendezVousPipeline(pipelineConfiguration, rendezVousApplicationName, RendezVousServerIp, internalLog);
            }
            else
            {
                datasetPipeline = new DatasetPipeline(pipelineConfiguration);
            }

            setupState = SetupState.PipelineInitialised;
        }

        private void SetupVideo()
        {
            if (setupState >= SetupState.VideoInitialised)
                return;

            if (datasetPipeline is null)
                SetupPipeline();

            WindowCaptureConfiguration cfg = new WindowCaptureConfiguration() { Interval = videoRemoteAppConfiguration.Interval};
            WindowCapture capture = new WindowCapture(datasetPipeline.Pipeline, cfg);
            IProducer<Shared<Microsoft.Psi.Imaging.EncodedImage>> videoSource;

            if (videoRemoteAppConfiguration.CroppingAreas.Count == 0)
            {
                ProcessProducer(capture.Out.EncodeJpeg(videoRemoteAppConfiguration.EncodingVideoLevel, DeliveryPolicy.LatestMessage), "VideoStreaming");
            }
            else 
            {
                foreach (KeyValuePair<string, System.Drawing.Rectangle> cropArea in videoRemoteAppConfiguration.CroppingAreas)
                {
                    if (cropArea.Value.IsEmpty)
                        continue;
                    SAAC.Helpers.ImageCropper cropRect = new SAAC.Helpers.ImageCropper(datasetPipeline.Pipeline, cropArea.Value);
                    capture.Out.PipeTo(cropRect.In);
                    ProcessProducer(cropRect.Out.EncodeJpeg(videoRemoteAppConfiguration.EncodingVideoLevel, DeliveryPolicy.LatestMessage), cropArea.Key);
                }
            }

            AddLog(State = "Video initialised");
            setupState = SetupState.VideoInitialised;
        }

        private void ProcessProducer(IProducer<Shared<EncodedImage>> producer, string streamName)
        {
            if (datasetPipeline is null)
            {
                return;
            }
            if (isRemoteServer)
            {
                RemoteExporter imageExporter = new RemoteExporter(datasetPipeline.Pipeline, pipelineConfiguration.RendezVousPort, TransportKind.Tcp);
                imageExporter.Exporter.Write(producer, streamName);
                (datasetPipeline as RendezVousPipeline)?.AddProcess(new Rendezvous.Process(rendezVousApplicationName, [imageExporter.ToRendezvousEndpoint(pipelineConfiguration.RendezVousHost)]));
            }
            else
            {
                datasetPipeline.CreateStore(datasetPipeline.Pipeline, datasetPipeline.CreateOrGetSession(LocalSessionName), streamName, rendezVousApplicationName, producer);
            }
        }

        private void Stop()
        {
            AddLog(State = "Stopping");
            datasetPipeline?.Stop();
            datasetPipeline?.Dispose();
            Application.Current.Shutdown();
        }

        private void StartNetwork()
        {
            SetupPipeline();
            if (setupState == SetupState.PipelineInitialised)
            {
                BtnStartNet.IsEnabled = false;
                AddLog(State = "Waiting for server");
                (datasetPipeline as RendezVousPipeline)?.Start();
            }
        }

        private void Start()
        {
            SetupPipeline();
            SetupVideo();
            if (setupState == SetupState.VideoInitialised)
            {
                BtnStart.IsEnabled = BtnStartNet.IsEnabled = false;
                var remotePipeline = datasetPipeline as RendezVousPipeline;
                if (remotePipeline != null)
                {
                    remotePipeline.Start();
                }
                else
                {
                    datasetPipeline?.RunPipelineAndSubpipelines();
                }
                AddLog(State = "Started");
            }
        }

        private void AddLog(string logMessage)
        {
            Log += $"{logMessage}\n";
        }

        private void Log_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            TextBox? log = sender as TextBox;
            if (log == null)
                return;
            log.CaretIndex = log.Text.Length;
            log.ScrollToEnd();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Stop();
            RefreshConfigurationFromUI();
            base.OnClosing(e);
        }

        private void BtnStartRendezVous(object sender, RoutedEventArgs e)
        {
            State = "Initializing RendezVous";
            RefreshConfigurationFromUI();
            StartNetwork();
            e.Handled = true;
        }

        private void BtnStartAll(object sender, RoutedEventArgs e)
        {
            State = "Initializing Video";
            RefreshConfigurationFromUI();
            Start();
            e.Handled = true;
        }

        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            Close();
            e.Handled = true;
        }

        private void BtnLoadConfiguration(object sender, RoutedEventArgs e)
        {
            RefreshUIFromConfiguration();
            AddLog(State = "Configuration Loaded");
            e.Handled = true;
        }

        private void BtnSaveConfiguration(object sender, RoutedEventArgs e)
        {
            RefreshConfigurationFromUI();
            AddLog(State = "Configuration Saved");
            e.Handled = true;
        }

        private void CkbActivateNetwork(object sender, RoutedEventArgs e)
        {
            UpdateNetworkTab();
            e.Handled = true;
        }

        private void CkbActivateStreaming(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void CkbActivateLocalRecording(object sender, RoutedEventArgs e)
        {
            UpdateLocalRecordingTab();
            e.Handled = true;
        }

        private void LocalRecordingDatasetDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            UiGenerator.FolderPicker openFileDialog = new UiGenerator.FolderPicker();
            if (openFileDialog.ShowDialog() == true)
            {
                LocalDatasetPath = openFileDialog.ResultName;
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            }
        }

        private void LocalRecordingDatasetNameButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Dataset (*.pds)|*.pds";
            if (openFileDialog.ShowDialog() == true)
            {
                LocalDatasetPath = openFileDialog.FileName.Substring(0, openFileDialog.FileName.IndexOf(openFileDialog.SafeFileName));
                LocalDatasetName = openFileDialog.SafeFileName;
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            }
        }

        private void VideoCaptureIntervalTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
        }

        private void VideoEncodingLevelTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
        }

        private void VideoCropCoordinateTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            
            // Update the cropping area in real-time if all fields are valid
            if (SelectedCroppingAreaName != null &&
                int.TryParse(VideoCropXTextBox.Text, out int x) &&
                int.TryParse(VideoCropYTextBox.Text, out int y) &&
                int.TryParse(VideoCropWidthTextBox.Text, out int width) &&
                int.TryParse(VideoCropHeightTextBox.Text, out int height))
            {
                videoRemoteAppConfiguration.CroppingAreas[SelectedCroppingAreaName] = 
                    new System.Drawing.Rectangle(x, y, width, height);
            }
        }

        private void RefreshCroppingAreasList()
        {
            CroppingAreaListBox.Items.Clear();
            foreach (var areaName in videoRemoteAppConfiguration.CroppingAreas.Keys)
            {
                CroppingAreaListBox.Items.Add(areaName);
            }
            
            // Clear edit fields if no areas
            if (CroppingAreaListBox.Items.Count == 0)
            {
                ClearCroppingAreaEditFields();
            }
        }

        private void ClearCroppingAreaEditFields()
        {
            VideoCropNameTextBox.Text = "";
            VideoCropXTextBox.Text = "0";
            VideoCropYTextBox.Text = "0";
            VideoCropWidthTextBox.Text = "0";
            VideoCropHeightTextBox.Text = "0";
            SelectedCroppingAreaName = null;
        }

        private void CroppingAreaListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CroppingAreaListBox.SelectedItem is string selectedName)
            {
                SelectedCroppingAreaName = selectedName;
                if (videoRemoteAppConfiguration.CroppingAreas.TryGetValue(selectedName, out var rect))
                {
                    VideoCropNameTextBox.Text = selectedName;
                    VideoCropXTextBox.Text = rect.X.ToString();
                    VideoCropYTextBox.Text = rect.Y.ToString();
                    VideoCropWidthTextBox.Text = rect.Width.ToString();
                    VideoCropHeightTextBox.Text = rect.Height.ToString();
                }
            }
        }

        private void VideoCropNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (SelectedCroppingAreaName != null && !string.IsNullOrWhiteSpace(VideoCropNameTextBox.Text))
            {
                string newName = VideoCropNameTextBox.Text;
                if (newName != SelectedCroppingAreaName)
                {
                    // Rename the cropping area
                    if (videoRemoteAppConfiguration.CroppingAreas.TryGetValue(SelectedCroppingAreaName, out var rect))
                    {
                        videoRemoteAppConfiguration.CroppingAreas.Remove(SelectedCroppingAreaName);
                        videoRemoteAppConfiguration.CroppingAreas[newName] = rect;
                        SelectedCroppingAreaName = newName;
                        RefreshCroppingAreasList();
                        CroppingAreaListBox.SelectedItem = newName;
                    }
                }
            }
            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
        }

        private void BtnAddCroppingArea(object sender, RoutedEventArgs e)
        {
            // Create a new cropping area with a default name
            int count = videoRemoteAppConfiguration.CroppingAreas.Count + 1;
            string newName = $"CropArea_{count}";
            while (videoRemoteAppConfiguration.CroppingAreas.ContainsKey(newName))
            {
                count++;
                newName = $"CropArea_{count}";
            }
            
            videoRemoteAppConfiguration.CroppingAreas[newName] = new System.Drawing.Rectangle(0, 0, 100, 100);
            RefreshCroppingAreasList();
            CroppingAreaListBox.SelectedItem = newName;
            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            AddLog($"Cropping area added: {newName}");
        }

        private void BtnDeleteCroppingArea(object sender, RoutedEventArgs e)
        {
            if (CroppingAreaListBox.SelectedItem is string selectedName)
            {
                videoRemoteAppConfiguration.CroppingAreas.Remove(selectedName);
                RefreshCroppingAreasList();
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
                AddLog($"Cropping area deleted: {selectedName}");
            }
        }

        private void BtnSelectCroppingArea(object sender, RoutedEventArgs e)
        {
            if (SelectedCroppingAreaName == null)
            {
                MessageBox.Show("Please select or create a cropping area first.", "No Area Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            CropSelectionWindow cropWindow = new CropSelectionWindow();
            if (cropWindow.ShowDialog() == true)
            {
                var selectedRect = cropWindow.SelectedRectangle;
                videoRemoteAppConfiguration.CroppingAreas[SelectedCroppingAreaName] = selectedRect;
                
                VideoCropXTextBox.Text = selectedRect.X.ToString();
                VideoCropYTextBox.Text = selectedRect.Y.ToString();
                VideoCropWidthTextBox.Text = selectedRect.Width.ToString();
                VideoCropHeightTextBox.Text = selectedRect.Height.ToString();

                AddLog($"Cropping area '{SelectedCroppingAreaName}' set: X={selectedRect.X}, Y={selectedRect.Y}, Width={selectedRect.Width}, Height={selectedRect.Height}");
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            }
            e.Handled = true;
        }
    }
}
