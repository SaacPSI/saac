using Microsoft.Psi;
using Microsoft.Psi.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Net;
using System.IO;
using Microsoft.Win32;
using SAAC;
using SAAC.RemoteConnectors;
using SAAC.PipelineServices;
using System.Windows.Controls;
using Microsoft.Psi.Media;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Components;

namespace CameraRemoteApp
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
                bool enableConfigurationButton = !notTriggerProperties.Contains(propertyName);
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = enableConfigurationButton;
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private KinectRemoteStreamsComponentConfiguration kinectConfiguration = new KinectRemoteStreamsComponentConfiguration();
        private KinectAzureRemoteStreamsConfiguration azureConfiguration = new KinectAzureRemoteStreamsConfiguration();
        private NuitrackRemoteStreamsConfiguration nuitrackConfiguration = new NuitrackRemoteStreamsConfiguration();
        private RendezVousPipelineConfiguration pipelineConfiguration = new RendezVousPipelineConfiguration();

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

        // Network Tab
        public List<string> IPsList { get; set; }

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

        private int exportPort;
        public int ExportPortUI
        {
            get => exportPort;
            set => SetProperty(ref exportPort, value);
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

        // VideoSources Tab

        public KinectRemoteStreamsComponentConfiguration KinectRemoteStreamsConfigurationUI
        {
            get => kinectConfiguration;
            set => SetProperty(ref kinectConfiguration, value);
        }
        public KinectAzureRemoteStreamsConfiguration KinectAzureRemoteStreamsConfigurationUI
        {
            get => azureConfiguration;
            set => SetProperty(ref azureConfiguration, value);
        }

        public NuitrackRemoteStreamsConfiguration NuitrackRemoteStreamsConfigurationUI
        {
            get => nuitrackConfiguration;
            set => SetProperty(ref nuitrackConfiguration, value);
        }

        private int encodingLevel = 90;

        public int EncodingLevel
        {
            get => encodingLevel;
            set => SetProperty(ref encodingLevel, value);
        }
        public enum ESensorType { Camera, Kinect, AzureKinect, Nuitrack }

        private ESensorType sensorType;

        public List<ESensorType> SensorTypeList { get; }
        public List<string> VideoSourceList { get; set; }

        public List<Microsoft.Azure.Kinect.Sensor.ColorResolution> ResolutionsList { get; }
        public List<Microsoft.Azure.Kinect.Sensor.FPS> FPSList { get; }

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
        public void DelegateMethod(string logs)
        {
            Log = logs;
        }

        // varialbles
        ///ToDo add more resolution definition
        private enum SetupState
        {
            NotInitialised,
            PipelineInitialised,
            CameraInitialised
        };
        private DatasetPipeline? datasetPipeline;
        private SetupState setupState;
        private LogStatus internalLog;

        public MainWindow()
        {
            notTriggerProperties = new List<string> { "Log", "State", "AudioSourceDatasetPath", "AudioSourceSessionName" };
            internalLog = (log) =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    Log += $"{log}\n";
                }));
            };

            DataContext = this;
            pipelineConfiguration.ClockPort = pipelineConfiguration.CommandPort = 0;
            pipelineConfiguration.AutomaticPipelineRun = true;
            pipelineConfiguration.RecordIncomingProcess = false;
            SensorTypeList = new List<ESensorType>(Enum.GetValues(typeof(ESensorType)).Cast<ESensorType>());
            VideoSourceList = Microsoft.Psi.Media.MediaCapture.GetAvailableCameras();
            ResolutionsList = new List<Microsoft.Azure.Kinect.Sensor.ColorResolution>(Enum.GetValues(typeof(Microsoft.Azure.Kinect.Sensor.ColorResolution)).Cast<Microsoft.Azure.Kinect.Sensor.ColorResolution>());
            FPSList = new List<Microsoft.Azure.Kinect.Sensor.FPS>(Enum.GetValues(typeof(Microsoft.Azure.Kinect.Sensor.FPS)).Cast<Microsoft.Azure.Kinect.Sensor.FPS>());

            IPsList = new List<string> {"localhost"};
            IPsList.AddRange(Dns.GetHostEntry(Dns.GetHostName()).AddressList.Select(ip => ip.ToString()));
            datasetPipeline = null;

            LoadConfigurations();
            InitializeComponent();
            UpdateLayout();

            SetupNetworkTab();
            SetupLocalRecordingTab();
            SetupVideoSourceTab();
            RefreshUIFromConfiguration();
        }

        private void SetupVideoSourceTab()
        {
            UiGenerator.SetTextBoxPreviewTextChecker<int>(EncodingLevelTextBox, int.TryParse);
        }

        private void SetupNetworkTab()
        {
            UiGenerator.SetTextBoxOutFocusChecker<System.Net.IPAddress>(RendezVousServerIpTextBox, UiGenerator.IPAddressTryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(RendezVousPortTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(CommandPortTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(StreamingPortRangeStartTextBox, int.TryParse);
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
            if (isRemoteServer)
                UpdateStreamingPortRangeStartTextBox();
        }

        private void UpdateStreamingPortRangeStartTextBox()
        {
            GeneralNetworkStreamingCheckBox.IsChecked = NetworkStreamingCheckBox.IsChecked = isStreaming;
            StreamingPortRangeStartTextBox.IsEnabled = isStreaming & isRemoteServer;
        }

        private void UpdateLocalRecordingTab()
        {
            foreach (UIElement networkUIElement in LocalRecordingGrid.Children)
                if (!(networkUIElement is CheckBox))
                    networkUIElement.IsEnabled = isLocalRecording;
        }

        private void RefreshUIFromConfiguration()
        {
            RendezVousServerIp = Properties.Settings.Default.rendezVousServerIp;
            PipelineConfigurationUI.RendezVousPort = (int)(Properties.Settings.Default.rendezVousServerPort);
            PipelineConfigurationUI.CommandPort = Properties.Settings.Default.commandPort;
            LocalRecordingDatasetDirectoryTextBox.Text = PipelineConfigurationUI.DatasetPath = Properties.Settings.Default.DatasetPath;
            LocalRecordingDatasetNameTextBox.Text = PipelineConfigurationUI.DatasetName = Properties.Settings.Default.DatasetName;
            RendezVousApplicationNameUI = Properties.Settings.Default.ApplicationName;
            IpSelectedUI = Properties.Settings.Default.IpToUse;
            EncodingLevel = Properties.Settings.Default.encodingLevel;
            SensorTypeComboBox.SelectedIndex = Properties.Settings.Default.sensorType;
            VideoSourceComboBox.SelectedIndex = VideoSourceList.IndexOf(Properties.Settings.Default.videoSource);

            IsRemoteServer = Properties.Settings.Default.isServer;
            IsStreaming = Properties.Settings.Default.isStreaming;
            IsLocalRecording = Properties.Settings.Default.isLocalRecording;
            LocalSessionName = Properties.Settings.Default.localSessionName;


            LoadConfigurations();
            Properties.Settings.Default.Save();
            UpdateLayout();
        }

        private void LoadConfigurations()
        {
            CommandSource = Properties.Settings.Default.commandSource;

            // Load CommandPort
            PipelineConfigurationUI.CommandPort = Properties.Settings.Default.commandPort;
            PipelineConfigurationUI.CommandPort = CommandPort;

            KinectAzureRemoteStreamsConfigurationUI.StartingPort = KinectRemoteStreamsConfigurationUI.StartingPort = NuitrackRemoteStreamsConfigurationUI.StartingPort = (int)Properties.Settings.Default.remotePort;
            KinectAzureRemoteStreamsConfigurationUI.OutputAudio = KinectRemoteStreamsConfigurationUI.OutputAudio = Properties.Settings.Default.audio;
            KinectAzureRemoteStreamsConfigurationUI.OutputBodies = KinectRemoteStreamsConfigurationUI.OutputBodies = NuitrackRemoteStreamsConfigurationUI.OutputSkeletonTracking = Properties.Settings.Default.skeleton;
            KinectAzureRemoteStreamsConfigurationUI.OutputColor = KinectRemoteStreamsConfigurationUI.OutputColor = NuitrackRemoteStreamsConfigurationUI.OutputColor = Properties.Settings.Default.rgb;
            KinectAzureRemoteStreamsConfigurationUI.OutputDepth = KinectRemoteStreamsConfigurationUI.OutputDepth = NuitrackRemoteStreamsConfigurationUI.OutputDepth = Properties.Settings.Default.depth;
            KinectAzureRemoteStreamsConfigurationUI.OutputCalibration = KinectRemoteStreamsConfigurationUI.OutputCalibration = Properties.Settings.Default.depthCalibration;
            KinectAzureRemoteStreamsConfigurationUI.OutputInfrared = KinectRemoteStreamsConfigurationUI.OutputInfrared = Properties.Settings.Default.infrared;
            KinectAzureRemoteStreamsConfigurationUI.OutputImu = Properties.Settings.Default.IMU;
            KinectAzureRemoteStreamsConfigurationUI.CameraFPS = (Microsoft.Azure.Kinect.Sensor.FPS)Properties.Settings.Default.FPS;
            KinectAzureRemoteStreamsConfigurationUI.ColorResolution = (Microsoft.Azure.Kinect.Sensor.ColorResolution)Properties.Settings.Default.videoResolution;

            KinectRemoteStreamsConfigurationUI.OutputLongExposureInfrared = Properties.Settings.Default.longExposureInfrared;
            KinectRemoteStreamsConfigurationUI.OutputColorToCameraMapping = Properties.Settings.Default.colorToCameraMapping;
            KinectRemoteStreamsConfigurationUI.OutputRGBD = Properties.Settings.Default.rgbd;

            NuitrackRemoteStreamsConfigurationUI.OutputHandTracking = Properties.Settings.Default.hands;
            NuitrackRemoteStreamsConfigurationUI.OutputGestureRecognizer = Properties.Settings.Default.gestures;
            NuitrackRemoteStreamsConfigurationUI.ActivationKey = Properties.Settings.Default.nuitrackKey;
            NuitrackRemoteStreamsConfigurationUI.DeviceSerialNumber = Properties.Settings.Default.nuitrackDevice;
        }

        private void RefreshConfigurationFromUI()
        {
            // General Tab
            Properties.Settings.Default.rendezVousServerIp = RendezVousServerIp;
            Properties.Settings.Default.rendezVousServerPort = (uint)PipelineConfigurationUI.RendezVousPort;
            Properties.Settings.Default.commandSource = CommandSource;
            Properties.Settings.Default.commandPort = CommandPort;
            Properties.Settings.Default.DatasetPath = PipelineConfigurationUI.DatasetPath;
            Properties.Settings.Default.DatasetName = PipelineConfigurationUI.DatasetName;
            Properties.Settings.Default.ApplicationName = RendezVousApplicationNameUI;
            Properties.Settings.Default.IpToUse = IpSelectedUI;
            Properties.Settings.Default.encodingLevel = EncodingLevel;
            Properties.Settings.Default.sensorType = SensorTypeComboBox.SelectedIndex;
            Properties.Settings.Default.videoSource = VideoSourceComboBox.SelectedValue as string ?? "";

            // Network Tab - Common settings for all sensors
            Properties.Settings.Default.remotePort = (uint)KinectAzureRemoteStreamsConfigurationUI.StartingPort;
            Properties.Settings.Default.audio = KinectAzureRemoteStreamsConfigurationUI.OutputAudio;
            Properties.Settings.Default.skeleton = KinectAzureRemoteStreamsConfigurationUI.OutputBodies;
            Properties.Settings.Default.rgb = KinectAzureRemoteStreamsConfigurationUI.OutputColor;
            Properties.Settings.Default.depth = KinectAzureRemoteStreamsConfigurationUI.OutputDepth;
            Properties.Settings.Default.depthCalibration = KinectAzureRemoteStreamsConfigurationUI.OutputCalibration;
            Properties.Settings.Default.IMU = KinectAzureRemoteStreamsConfigurationUI.OutputImu;

            // Sensor-specific settings based on selected sensor type
            switch ((ESensorType)SensorTypeComboBox.SelectedIndex)
            {
                case ESensorType.Camera :// Camera
                        // No specific settings for camera
                    break;

                case ESensorType.Kinect : // Kinect
                    Properties.Settings.Default.remotePort = (uint)KinectRemoteStreamsConfigurationUI.StartingPort;
                    Properties.Settings.Default.audio = KinectRemoteStreamsConfigurationUI.OutputAudio;
                    Properties.Settings.Default.skeleton = KinectRemoteStreamsConfigurationUI.OutputBodies;
                    Properties.Settings.Default.rgb = KinectRemoteStreamsConfigurationUI.OutputColor;
                    Properties.Settings.Default.depth = KinectRemoteStreamsConfigurationUI.OutputDepth;
                    Properties.Settings.Default.depthCalibration = KinectRemoteStreamsConfigurationUI.OutputCalibration;
                    Properties.Settings.Default.infrared = KinectRemoteStreamsConfigurationUI.OutputInfrared;
                    Properties.Settings.Default.longExposureInfrared = KinectRemoteStreamsConfigurationUI.OutputLongExposureInfrared;
                    Properties.Settings.Default.colorToCameraMapping = KinectRemoteStreamsConfigurationUI.OutputColorToCameraMapping;
                    Properties.Settings.Default.rgbd = KinectRemoteStreamsConfigurationUI.OutputRGBD;
                    break;

                case ESensorType.AzureKinect : // Azure Kinect
                    Properties.Settings.Default.remotePort = (uint)KinectAzureRemoteStreamsConfigurationUI.StartingPort;
                    Properties.Settings.Default.audio = KinectAzureRemoteStreamsConfigurationUI.OutputAudio;
                    Properties.Settings.Default.skeleton = KinectAzureRemoteStreamsConfigurationUI.OutputBodies;
                    Properties.Settings.Default.rgb = KinectAzureRemoteStreamsConfigurationUI.OutputColor;
                    Properties.Settings.Default.infrared = KinectAzureRemoteStreamsConfigurationUI.OutputInfrared;
                    Properties.Settings.Default.depth = KinectAzureRemoteStreamsConfigurationUI.OutputDepth;
                    Properties.Settings.Default.depthCalibration = KinectAzureRemoteStreamsConfigurationUI.OutputCalibration;
                    Properties.Settings.Default.IMU = KinectAzureRemoteStreamsConfigurationUI.OutputImu;
                    Properties.Settings.Default.FPS = FPSList.IndexOf(KinectAzureRemoteStreamsConfigurationUI.CameraFPS);
                    Properties.Settings.Default.videoResolution = ResolutionsList.IndexOf(KinectAzureRemoteStreamsConfigurationUI.ColorResolution);
                    break;

                case ESensorType.Nuitrack : // Nuitrack
                    Properties.Settings.Default.remotePort = (uint)NuitrackRemoteStreamsConfigurationUI.StartingPort;
                    Properties.Settings.Default.skeleton = NuitrackRemoteStreamsConfigurationUI.OutputSkeletonTracking;
                    Properties.Settings.Default.rgb = NuitrackRemoteStreamsConfigurationUI.OutputColor;
                    Properties.Settings.Default.depth = NuitrackRemoteStreamsConfigurationUI.OutputDepth;
                    Properties.Settings.Default.hands = NuitrackRemoteStreamsConfigurationUI.OutputHandTracking;
                    Properties.Settings.Default.gestures = NuitrackRemoteStreamsConfigurationUI.OutputGestureRecognizer;
                    break;
            }

            Properties.Settings.Default.isServer = IsRemoteServer;
            Properties.Settings.Default.isStreaming = IsStreaming;
            Properties.Settings.Default.isLocalRecording = IsLocalRecording;
            Properties.Settings.Default.localSessionName = LocalSessionName;
            Properties.Settings.Default.nuitrackKey = NuitrackRemoteStreamsConfigurationUI.ActivationKey;
            Properties.Settings.Default.nuitrackDevice = NuitrackRemoteStreamsConfigurationUI.DeviceSerialNumber;


            Properties.Settings.Default.Save();
        }

        private bool UpdateConfigurationFromArgs(string[] args)
        {
            try
            {
                KinectAzureRemoteStreamsConfigurationUI.KinectDeviceIndex = int.Parse(args[1]);
                KinectAzureRemoteStreamsConfigurationUI.OutputAudio = bool.Parse(args[2]);
                KinectAzureRemoteStreamsConfigurationUI.OutputBodies = bool.Parse(args[3]);
                KinectAzureRemoteStreamsConfigurationUI.OutputColor = bool.Parse(args[4]);
                KinectAzureRemoteStreamsConfigurationUI.OutputDepth = bool.Parse(args[5]);
                KinectAzureRemoteStreamsConfigurationUI.OutputCalibration = bool.Parse(args[6]);
                KinectAzureRemoteStreamsConfigurationUI.OutputImu = bool.Parse(args[7]);
                KinectAzureRemoteStreamsConfigurationUI.EncodingVideoLevel = int.Parse(args[8]);
                KinectAzureRemoteStreamsConfigurationUI.ColorResolution = (Microsoft.Azure.Kinect.Sensor.ColorResolution)int.Parse(args[9]);
                KinectAzureRemoteStreamsConfigurationUI.CameraFPS = (Microsoft.Azure.Kinect.Sensor.FPS)int.Parse(args[10]);
                KinectAzureRemoteStreamsConfigurationUI.IpToUse = args[11];
                KinectAzureRemoteStreamsConfigurationUI.StartingPort = int.Parse(args[12]);
            } 
            catch(Exception ex)
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

        private void CommandRecieved(string source, Message<(RendezVousPipeline.Command, string)> message)
        {
            if ($"{CommandSource}-Command" != source)
                return;
            var args = message.Data.Item2.Split([';']);

            if (args[0] != KinectAzureRemoteStreamsConfigurationUI.RendezVousApplicationName || args[0] == "*")
                return;

            datasetPipeline.Log($"CommandRecieved with {message.Data.Item1} command, args: {message.Data.Item2}.");
            switch (message.Data.Item1)
            {
                case RendezVousPipeline.Command.Initialize:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        UpdateConfigurationFromArgs(args);
                    }));
                    break;
                case RendezVousPipeline.Command.Run:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        SetupSensor();
                    }));
                    break;
                case RendezVousPipeline.Command.Stop:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        datasetPipeline.Stop();
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
                    (datasetPipeline as RendezVousPipeline)?.SendCommand(RendezVousPipeline.Command.Status, datasetPipeline.Pipeline.StartTime.ToString(), "");
                    break;
            }
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
            pipelineConfiguration.AutomaticPipelineRun = true;
            pipelineConfiguration.CommandDelegate = CommandRecieved;
            pipelineConfiguration.Debug = false;
            pipelineConfiguration.RecordIncomingProcess = false;
            pipelineConfiguration.ClockPort = 0;
            pipelineConfiguration.CommandPort = CommandPort;
            if (isRemoteServer)
            {
                var rendezVousPipeline = new RendezVousPipeline(pipelineConfiguration, rendezVousApplicationName, RendezVousServerIp, internalLog);
                datasetPipeline = rendezVousPipeline;
            }
            else
                datasetPipeline = new DatasetPipeline(pipelineConfiguration, rendezVousApplicationName, internalLog);
            setupState = SetupState.PipelineInitialised;
        }

        private void SetupSensor()
        {
            if (setupState >= SetupState.CameraInitialised)
                return;
            switch(sensorType)
            {
                case ESensorType.Camera:
                    SetupCamera();
                    break;
                case ESensorType.Kinect:
                    SetupKinect(); 
                    break;
                case ESensorType.AzureKinect:
                    SetupAzureKinect(); 
                    break;
                case ESensorType.Nuitrack: 
                    SetupNuitrack(); 
                    break;
            }
            setupState = SetupState.CameraInitialised;
        }

        private void SetupAzureKinect()
        {
            azureConfiguration.EncodingVideoLevel = encodingLevel;
            if (isRemoteServer)
            {
                RendezVousPipeline rdv = datasetPipeline as RendezVousPipeline;
                azureConfiguration.RendezVousApplicationName = rendezVousApplicationName;
                azureConfiguration.IpToUse = ipSelected;
                azureConfiguration.StartingPort = exportPort;
                var kinectStreams = new SAAC.RemoteConnectors.KinectAzureStreamsComponent(rdv, azureConfiguration, isLocalRecording);
                rdv.AddProcess(kinectStreams.GenerateProcess());
            }
            else
            {
                if (azureConfiguration.OutputBodies == true)
                    azureConfiguration.BodyTrackerConfiguration = new Microsoft.Psi.AzureKinect.AzureKinectBodyTrackerConfiguration();
                var Sensor = new Microsoft.Psi.AzureKinect.AzureKinectSensor(datasetPipeline.Pipeline, azureConfiguration);
                Session session = datasetPipeline.CreateOrGetSession(localSessionName);
                if (azureConfiguration.OutputAudio == true)
                {
                    string streamName = "Audio";
                    Microsoft.Psi.Audio.AudioCaptureConfiguration configuration = new Microsoft.Psi.Audio.AudioCaptureConfiguration();
                    int index = Microsoft.Psi.Audio.AudioCapture.GetAvailableDevices().ToList().FindIndex(value => { return value.Contains("Azure"); });
                    configuration.DeviceName = Microsoft.Psi.Audio.AudioCapture.GetAvailableDevices().ElementAt(index);
                    Microsoft.Psi.Audio.AudioCapture audioCapture = new Microsoft.Psi.Audio.AudioCapture(datasetPipeline.Pipeline, configuration);
                    datasetPipeline.CreateConnectorAndStore("Audio", "Audio", session, datasetPipeline.Pipeline, audioCapture.GetType(), audioCapture.Out, isLocalRecording);
                }
                if (azureConfiguration.OutputBodies == true)
                    datasetPipeline.CreateConnectorAndStore("Bodies", "Bodies", session, datasetPipeline.Pipeline, Sensor.Bodies.GetType(), Sensor.Bodies, isLocalRecording);
                if (azureConfiguration.OutputColor == true)
                {
                    var compressed = Sensor.ColorImage.EncodeJpeg(encodingLevel);
                    datasetPipeline.CreateConnectorAndStore("RGB", "RGB", session, datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, isLocalRecording);
                }
                if (azureConfiguration.OutputInfrared == true)
                {
                    var compressed = Sensor.InfraredImage.EncodeJpeg(encodingLevel);
                    datasetPipeline.CreateConnectorAndStore("Infrared", "Infrared", session, datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, isLocalRecording);
                }
                if (azureConfiguration.OutputDepth == true)
                {
                    var compressed = Sensor.DepthImage.EncodePng();
                    datasetPipeline.CreateConnectorAndStore("Depth", "Depth", session, datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, isLocalRecording);
                }
                if (azureConfiguration.OutputCalibration == true)
                     datasetPipeline.CreateConnectorAndStore("Calibration", "Calibration", session, datasetPipeline.Pipeline, Sensor.DepthDeviceCalibrationInfo.GetType(), Sensor.DepthDeviceCalibrationInfo, isLocalRecording);
                if (azureConfiguration.OutputImu == true)
                    datasetPipeline.CreateConnectorAndStore("Imu", "Imu", session, datasetPipeline.Pipeline, Sensor.Imu.GetType(), Sensor.Imu, isLocalRecording);
            }
        }

        private void SetupKinect()
        {
            kinectConfiguration.EncodingVideoLevel = encodingLevel;
            if (isRemoteServer)
            {
                RendezVousPipeline rdv = datasetPipeline as RendezVousPipeline;
                kinectConfiguration.RendezVousApplicationName = rendezVousApplicationName;
                kinectConfiguration.IpToUse = ipSelected;
                kinectConfiguration.StartingPort = exportPort;
                var KinectStreams = new SAAC.RemoteConnectors.KinectStreamsComponent(rdv, kinectConfiguration, isLocalRecording);
                rdv.AddProcess(KinectStreams.GenerateProcess());
            }
            else 
            {
                var Sensor = new Microsoft.Psi.Kinect.KinectSensor(datasetPipeline.Pipeline, kinectConfiguration);
                var session = datasetPipeline.CreateOrGetSessionFromMode(localSessionName);
                if (kinectConfiguration.OutputAudio == true)
                    datasetPipeline.CreateConnectorAndStore("Audio", "Audio", session, datasetPipeline.Pipeline, Sensor.Audio.GetType(), Sensor.Audio, isLocalRecording);

                if (kinectConfiguration.OutputBodies == true)
                    datasetPipeline.CreateConnectorAndStore("Bodies", "Bodies", session, datasetPipeline.Pipeline, Sensor.Bodies.GetType(), Sensor.Bodies, isLocalRecording);

                if (kinectConfiguration.OutputColor == true)
                {
                    var compressed = Sensor.ColorImage.EncodeJpeg(kinectConfiguration.EncodingVideoLevel);
                    datasetPipeline.CreateConnectorAndStore("RGB", "RGB", session, datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, isLocalRecording);
                }
                if (kinectConfiguration.OutputRGBD == true)
                {
                    var compressed = Sensor.RGBDImage.EncodeJpeg(kinectConfiguration.EncodingVideoLevel);
                    datasetPipeline.CreateConnectorAndStore("RGBD", "RGBD", session, datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, isLocalRecording);
                }
                if (kinectConfiguration.OutputDepth == true)
                {
                    var compressed = Sensor.DepthImage.EncodePng();
                    datasetPipeline.CreateConnectorAndStore("Depth", "Depth", session, datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, isLocalRecording);
                }
                if (kinectConfiguration.OutputInfrared == true)
                {
                    var compressed = Sensor.InfraredImage.EncodeJpeg(kinectConfiguration.EncodingVideoLevel);
                    datasetPipeline.CreateConnectorAndStore("Infrared", "Infrared", session, datasetPipeline.Pipeline, compressed.GetType(), compressed, isLocalRecording);
                }
                if (kinectConfiguration.OutputLongExposureInfrared == true)
                {
                    var compressed = Sensor.LongExposureInfraredImage.EncodeJpeg(kinectConfiguration.EncodingVideoLevel);
                    datasetPipeline.CreateConnectorAndStore("LongExposureInfrared", "LongExposureInfrared", session, datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, isLocalRecording);
                }
                if (kinectConfiguration.OutputColorToCameraMapping == true)
                    datasetPipeline.CreateConnectorAndStore("ColorToCameraMapper", "ColorToCameraMapper", session, datasetPipeline.Pipeline, Sensor.ColorToCameraMapper.GetType(), Sensor.ColorToCameraMapper, isLocalRecording);
                if (kinectConfiguration.OutputCalibration == true)
                    datasetPipeline.CreateConnectorAndStore("Calibration", "Calibration", session, datasetPipeline.Pipeline, Sensor.DepthDeviceCalibrationInfo.GetType(), Sensor.DepthDeviceCalibrationInfo, isLocalRecording);
            }
        }

        private void SetupNuitrack()
        {
            nuitrackConfiguration.EncodingVideoLevel = encodingLevel;
            if (isRemoteServer)
            {
                RendezVousPipeline rdv = datasetPipeline as RendezVousPipeline;
                nuitrackConfiguration.RendezVousApplicationName = rendezVousApplicationName;
                nuitrackConfiguration.IpToUse = ipSelected;
                nuitrackConfiguration.StartingPort = exportPort;
                var nuitrackStreams = new SAAC.RemoteConnectors.NuitrackRemoteStreamsComponent(rdv, nuitrackConfiguration, isLocalRecording);
                rdv.AddProcess(nuitrackStreams.GenerateProcess());
            }
            else
            {
                var Sensor = new SAAC.Nuitrack.NuitrackSensor(datasetPipeline.Pipeline, nuitrackConfiguration);
                var session = datasetPipeline.CreateOrGetSessionFromMode(localSessionName);
                if (nuitrackConfiguration.OutputSkeletonTracking == true)
                    datasetPipeline.CreateConnectorAndStore("Bodies", "Bodies", session, datasetPipeline.Pipeline, Sensor.OutBodies.GetType(), Sensor.OutBodies, isLocalRecording);

                if (nuitrackConfiguration.OutputColor == true)
                {
                    var compressed = Sensor.OutColorImage.EncodeJpeg(nuitrackConfiguration.EncodingVideoLevel);
                    datasetPipeline.CreateConnectorAndStore("RGB", "RGB", session, datasetPipeline.Pipeline, compressed.GetType(), compressed, isLocalRecording);
                }
                if (nuitrackConfiguration.OutputDepth == true)
                {
                    var compressed = Sensor.OutDepthImage.EncodePng();
                    datasetPipeline.CreateConnectorAndStore("Depth", "Depth", session, datasetPipeline.Pipeline, compressed.GetType(), compressed, isLocalRecording);
                }
                if (nuitrackConfiguration.OutputHandTracking == true)
                    datasetPipeline.CreateConnectorAndStore("Hands", "Hands", session, datasetPipeline.Pipeline, Sensor.OutHands.GetType(), Sensor.OutHands, isLocalRecording);
                if (nuitrackConfiguration.OutputUserTracking == true)
                    datasetPipeline.CreateConnectorAndStore("Users", "Users", session, datasetPipeline.Pipeline, Sensor.OutUsers.GetType(), Sensor.OutUsers, isLocalRecording);
                if (nuitrackConfiguration.OutputGestureRecognizer == true)
                    datasetPipeline.CreateConnectorAndStore("Gestures", "Gestures", session, datasetPipeline.Pipeline, Sensor.OutGestures.GetType(), Sensor.OutGestures, isLocalRecording);
            }
        }

        private void SetupCamera()
        {
            MediaCaptureConfiguration configuration = new MediaCaptureConfiguration();
            configuration.DeviceId = VideoSourceComboBox.SelectedValue as string;
            MediaCapture camera = new MediaCapture(datasetPipeline.Pipeline, configuration);
            var compressed = camera.Out.EncodeJpeg(encodingLevel);
            string streamName = "RGB";
            var session = datasetPipeline.CreateOrGetSessionFromMode(isLocalRecording ? localSessionName : rendezVousApplicationName);
            datasetPipeline.CreateConnectorAndStore(streamName, $"{rendezVousApplicationName}-{streamName}", session, datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, IsLocalRecording);
            if (isRemoteServer && isStreaming)
            {
                RendezVousPipeline rdv = datasetPipeline as RendezVousPipeline;
                if (isStreaming)
                {
                    Microsoft.Psi.Remoting.RemoteExporter cameraExporter = new Microsoft.Psi.Remoting.RemoteExporter(datasetPipeline.Pipeline, exportPort);
                    cameraExporter.Exporter.Write(compressed, streamName); 
                    rdv.AddProcess(new Rendezvous.Process(rendezVousApplicationName, [cameraExporter.ToRendezvousEndpoint(ipSelected)], "Version1.0"));
                }
            }
        }

        private void Stop()
        {
            AddLog(State = "Stopping");
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
                (datasetPipeline as RendezVousPipeline)?.Start((d) => { Application.Current.Dispatcher.Invoke(new Action(() => { AddLog(State = "Connected to server"); }));}); 
            }
        }

        private void Start()
        {
            SetupPipeline();
            SetupSensor();
            if (setupState == SetupState.CameraInitialised)
            {
                BtnStart.IsEnabled = BtnStartNet.IsEnabled = false;
                datasetPipeline.RunPipelineAndSubpipelines();
                AddLog(State = "Started");
            }
        }
 
        protected override void OnClosing(CancelEventArgs e)
        {
            Stop();
            base.OnClosing(e);
        }

        private void BtnStartRendezVous(object sender, RoutedEventArgs e)
        {
            StartNetwork();
            e.Handled = true;
        }

        private void BtnStartAll(object sender, RoutedEventArgs e)
        {
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

        private void SensorTypeSelected(object sender, RoutedEventArgs e)
        {
            KinectConfiguration.Visibility = NuitrackConfiguration.Visibility = AzureKinectConfiguration.Visibility = Visibility.Collapsed;
            switch (SensorTypeComboBox.SelectedIndex)
            {
                case 3:
                    sensorType = ESensorType.Nuitrack;
                    NuitrackConfiguration.Visibility = Visibility.Visible;
                    break;
                case 2:
                    sensorType = ESensorType.AzureKinect;
                    AzureKinectConfiguration.Visibility = Visibility.Visible;
                    break;
                case 1:
                    sensorType = ESensorType.Kinect;
                    KinectConfiguration.Visibility = Visibility.Visible;
                    break;
                case 0:
                    sensorType = ESensorType.Camera;
                    break;
            }

            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
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

        private void CkbActivateNetwork(object sender, RoutedEventArgs e)
        {
            UpdateNetworkTab();
            e.Handled = true;
        }

        private void CkbActivateStreaming(object sender, RoutedEventArgs e)
        {
            UpdateStreamingPortRangeStartTextBox();
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
    }
}
