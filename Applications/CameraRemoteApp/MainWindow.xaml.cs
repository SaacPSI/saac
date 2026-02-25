// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Media;
using Microsoft.Psi.Media_Interop;
using Microsoft.Win32;
using SAAC;
using SAAC.PipelineServices;
using SAAC.RemoteConnectors;

namespace CameraRemoteApp
{
    /// <summary>
    /// Main window for the Camera Remote Application that manages camera and sensor streaming.
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
                    this.BtnLoadConfig.IsEnabled = true;
                    this.BtnSaveConfig.IsEnabled = true;
                }

                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        private KinectRemoteStreamsComponentConfiguration kinectConfiguration = new KinectRemoteStreamsComponentConfiguration();
        private KinectAzureRemoteStreamsConfiguration azureConfiguration = new KinectAzureRemoteStreamsConfiguration();
        private NuitrackRemoteStreamsConfiguration nuitrackConfiguration = new NuitrackRemoteStreamsConfiguration();
        private RendezVousPipelineConfiguration pipelineConfiguration = new RendezVousPipelineConfiguration();
        private string state = "Not Initialised";
        private bool isRemoteServer = true;
        private bool isStreaming = true;
        private bool isLocalRecording = true;
        private string rendezVousServerIp = "localhost";
        private int exportPort;
        private string rendezVousApplicationName;
        private string ipSelected;
        private string commandSource = "Server";
        private int commandPort;
        private int encodingLevel = 90;
        private ESensorType sensorType;
        private string localSessionName = string.Empty;
        private string log = string.Empty;
        private DatasetPipeline? datasetPipeline;
        private SetupState setupState;
        private LogStatus internalLog;
        private List<CaptureFormat> cameraFormats;

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
        /// Gets or sets the list of available IP addresses.
        /// </summary>
        public List<string> IPsList { get; set; }

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
        /// Gets or sets the export port for streaming.
        /// </summary>
        public int ExportPort
        {
            get => this.exportPort;
            set => this.SetProperty(ref this.exportPort, value);
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
        /// Gets or sets the Kinect remote streams configuration.
        /// </summary>
        public KinectRemoteStreamsComponentConfiguration KinectRemoteStreamsConfigurationUI
        {
            get => this.kinectConfiguration;
            set => this.SetProperty(ref this.kinectConfiguration, value);
        }

        /// <summary>
        /// Gets or sets the Azure Kinect remote streams configuration.
        /// </summary>
        public KinectAzureRemoteStreamsConfiguration KinectAzureRemoteStreamsConfigurationUI
        {
            get => this.azureConfiguration;
            set => this.SetProperty(ref this.azureConfiguration, value);
        }

        /// <summary>
        /// Gets or sets the Nuitrack remote streams configuration.
        /// </summary>
        public NuitrackRemoteStreamsConfiguration NuitrackRemoteStreamsConfigurationUI
        {
            get => this.nuitrackConfiguration;
            set => this.SetProperty(ref this.nuitrackConfiguration, value);
        }

        /// <summary>
        /// Gets or sets the encoding quality level.
        /// </summary>
        public int EncodingLevel
        {
            get => this.encodingLevel;
            set => this.SetProperty(ref this.encodingLevel, value);
        }

        /// <summary>
        /// Represents the available sensor types.
        /// </summary>
        public enum ESensorType
        {
            /// <summary>Standard camera.</summary>
            Camera,

            /// <summary>Microsoft Kinect sensor.</summary>
            Kinect,

            /// <summary>Azure Kinect sensor.</summary>
            AzureKinect,

            /// <summary>Nuitrack sensor.</summary>
            Nuitrack,
        }

        /// <summary>
        /// Gets the list of available sensor types.
        /// </summary>
        public List<ESensorType> SensorTypeList { get; }

        /// <summary>
        /// Gets or sets the list of available video sources.
        /// </summary>
        public List<string> VideoSourceList { get; set; }

        /// <summary>
        /// Gets the list of camera capture formats.
        /// </summary>
        public List<string> CameraCaptureFormat { get; private set; }

        /// <summary>
        /// Gets the list of available Azure Kinect color resolutions.
        /// </summary>
        public List<Microsoft.Azure.Kinect.Sensor.ColorResolution> ResolutionsList { get; }

        /// <summary>
        /// Gets the list of available Azure Kinect FPS settings.
        /// </summary>
        public List<Microsoft.Azure.Kinect.Sensor.FPS> FPSList { get; }

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
        /// Delegate method for adding log entries.
        /// </summary>
        /// <param name="logs">The log message to add.</param>
        public void DelegateMethod(string logs)
        {
            this.Log = logs;
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

            /// <summary>Camera/sensor has been initialized.</summary>
            CameraInitialised,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.notTriggerProperties = new List<string> { "Log", "State", "AudioSourceDatasetPath", "AudioSourceSessionName" };
            this.internalLog = (log) =>
            {
                Application.Current?.Dispatcher?.Invoke(new Action(() =>
                {
                    this.Log += $"{log}\n";
                }));
            };

            this.DataContext = this;
            this.pipelineConfiguration.ClockPort = this.pipelineConfiguration.CommandPort = 0;
            this.pipelineConfiguration.AutomaticPipelineRun = true;
            this.pipelineConfiguration.RecordIncomingProcess = false;
            this.SensorTypeList = new List<ESensorType>(Enum.GetValues(typeof(ESensorType)).Cast<ESensorType>());
            this.VideoSourceList = Microsoft.Psi.Media.MediaCapture.GetAvailableCameras();
            this.ResolutionsList = new List<Microsoft.Azure.Kinect.Sensor.ColorResolution>(Enum.GetValues(typeof(Microsoft.Azure.Kinect.Sensor.ColorResolution)).Cast<Microsoft.Azure.Kinect.Sensor.ColorResolution>());
            this.FPSList = new List<Microsoft.Azure.Kinect.Sensor.FPS>(Enum.GetValues(typeof(Microsoft.Azure.Kinect.Sensor.FPS)).Cast<Microsoft.Azure.Kinect.Sensor.FPS>());
            this.CameraCaptureFormat = new List<string>();
            this.cameraFormats = new List<CaptureFormat>();

            this.IPsList = new List<string> { "localhost" };
            this.IPsList.AddRange(Dns.GetHostEntry(Dns.GetHostName()).AddressList.Select(ip => ip.ToString()));
            this.datasetPipeline = null;

            this.InitializeComponent();
            this.LoadConfigurations();
            this.UpdateLayout();

            this.SetupNetworkTab();
            this.SetupLocalRecordingTab();
            this.SetupVideoSourceTab();
            this.RefreshUIFromConfiguration();
            this.UpdateLocalRecordingTab();
        }

        /// <summary>
        /// Sets up the video source tab UI components.
        /// </summary>
        private void SetupVideoSourceTab()
        {
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.EncodingLevelTextBox, int.TryParse);
        }

        /// <summary>
        /// Sets up the network tab UI components and validation.
        /// </summary>
        private void SetupNetworkTab()
        {
            UiGenerator.SetTextBoxOutFocusChecker<System.Net.IPAddress>(this.RendezVousServerIpTextBox, UiGenerator.IPAddressTryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.RendezVousPortTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.CommandPortTextBox, int.TryParse);
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
            if (this.isRemoteServer)
            {
                this.UpdateStreamingPortRangeStartTextBox();
            }
        }

        /// <summary>
        /// Updates the streaming port range start text box state.
        /// </summary>
        private void UpdateStreamingPortRangeStartTextBox()
        {
            this.GeneralNetworkStreamingCheckBox.IsChecked = this.NetworkStreamingCheckBox.IsChecked = this.isStreaming;
            this.StreamingPortRangeStartTextBox.IsEnabled = this.isStreaming & this.isRemoteServer;
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
            this.RendezVousServerIp = Properties.Settings.Default.rendezVousServerIp;
            this.PipelineConfigurationUI.RendezVousPort = (int)(Properties.Settings.Default.rendezVousServerPort);
            this.PipelineConfigurationUI.CommandPort = Properties.Settings.Default.commandPort;
            this.RendezVousApplicationNameUI = Properties.Settings.Default.ApplicationName;
            this.IpSelectedUI = Properties.Settings.Default.IpToUse;
            this.ExportPort = (int)Properties.Settings.Default.remotePort;

            this.IsRemoteServer = Properties.Settings.Default.isServer;
            this.IsStreaming = Properties.Settings.Default.isStreaming;
            this.IsLocalRecording = Properties.Settings.Default.isLocalRecording;

            // Local Recording Tab
            this.LocalRecordingDatasetDirectoryTextBox.Text = this.PipelineConfigurationUI.DatasetPath = Properties.Settings.Default.DatasetPath;
            this.LocalRecordingDatasetNameTextBox.Text = this.PipelineConfigurationUI.DatasetName = Properties.Settings.Default.DatasetName;
            this.LocalSessionName = Properties.Settings.Default.localSessionName;

            // VideoSources Tab
            this.EncodingLevel = Properties.Settings.Default.encodingLevel;
            this.SensorTypeComboBox.SelectedIndex = Properties.Settings.Default.sensorType;
            this.VideoSourceComboBox.SelectedIndex = this.VideoSourceList.IndexOf(Properties.Settings.Default.videoSource);

            this.LoadConfigurations();
            Properties.Settings.Default.Save();
            this.UpdateLayout();
            this.UpdateCameraCaptureFormat(Properties.Settings.Default.cameraCaptureFormat);
        }

        /// <summary>
        /// Loads configuration settings from application properties.
        /// </summary>
        private void LoadConfigurations()
        {
            // Load Command settings
            this.CommandSource = Properties.Settings.Default.commandSource;
            this.CommandPort = Properties.Settings.Default.commandPort;
            this.PipelineConfigurationUI.CommandPort = this.CommandPort;

            // Load common sensor configurations
            int startingPort = (int)Properties.Settings.Default.remotePort;
            this.KinectAzureRemoteStreamsConfigurationUI.StartingPort = startingPort;
            this.KinectRemoteStreamsConfigurationUI.StartingPort = startingPort;
            this.NuitrackRemoteStreamsConfigurationUI.StartingPort = startingPort;

            // Load Azure Kinect configuration
            this.AzureKinectAudio.IsChecked = this.KinectAzureRemoteStreamsConfigurationUI.OutputAudio = Properties.Settings.Default.audio;
            this.AzureKinectSkeleton.IsChecked = this.KinectAzureRemoteStreamsConfigurationUI.OutputBodies = Properties.Settings.Default.skeleton;
            this.AzureKinectRGB.IsChecked = this.KinectAzureRemoteStreamsConfigurationUI.OutputColor = Properties.Settings.Default.rgb;
            this.AzureKinectDepth.IsChecked = this.KinectAzureRemoteStreamsConfigurationUI.OutputDepth = Properties.Settings.Default.depth;
            this.AzureKinectDepthCalibration.IsChecked = this.KinectAzureRemoteStreamsConfigurationUI.OutputCalibration = Properties.Settings.Default.depthCalibration;
            this.AzureKinectInfrared.IsChecked = this.KinectAzureRemoteStreamsConfigurationUI.OutputInfrared = Properties.Settings.Default.infrared;
            this.AzureKinectIMU.IsChecked = this.KinectAzureRemoteStreamsConfigurationUI.OutputImu = Properties.Settings.Default.IMU;
            this.KinectAzureRemoteStreamsConfigurationUI.CameraFPS = (Microsoft.Azure.Kinect.Sensor.FPS)Properties.Settings.Default.FPS;
            this.KinectAzureRemoteStreamsConfigurationUI.ColorResolution = (Microsoft.Azure.Kinect.Sensor.ColorResolution)Properties.Settings.Default.videoResolution;

            // Load Kinect configuration
            this.KinectAudio.IsChecked = this.KinectRemoteStreamsConfigurationUI.OutputAudio = Properties.Settings.Default.audio;
            this.KinectSkeleton.IsChecked = this.KinectRemoteStreamsConfigurationUI.OutputBodies = Properties.Settings.Default.skeleton;
            this.KinectRGB.IsChecked = this.KinectRemoteStreamsConfigurationUI.OutputColor = Properties.Settings.Default.rgb;
            this.KinectDepth.IsChecked = this.KinectRemoteStreamsConfigurationUI.OutputDepth = Properties.Settings.Default.depth;
            this.KinectDepthCalibration.IsChecked = this.KinectRemoteStreamsConfigurationUI.OutputCalibration = Properties.Settings.Default.depthCalibration;
            this.KinectInfrared.IsChecked = this.KinectRemoteStreamsConfigurationUI.OutputInfrared = Properties.Settings.Default.infrared;
            this.KinectLongExposureInfrared.IsChecked = this.KinectRemoteStreamsConfigurationUI.OutputLongExposureInfrared = Properties.Settings.Default.longExposureInfrared;
            this.KinectColorToCameraMapping.IsChecked = this.KinectRemoteStreamsConfigurationUI.OutputColorToCameraMapping = Properties.Settings.Default.colorToCameraMapping;
            this.KinectRGBD.IsChecked = this.KinectRemoteStreamsConfigurationUI.OutputRGBD = Properties.Settings.Default.rgbd;

            // Load Nuitrack configuration
            this.NuitrackSkeleton.IsChecked = this.NuitrackRemoteStreamsConfigurationUI.OutputSkeletonTracking = Properties.Settings.Default.skeleton;
            this.NuitrackRGB.IsChecked = this.NuitrackRemoteStreamsConfigurationUI.OutputColor = Properties.Settings.Default.rgb;
            this.NuitrackDepth.IsChecked = this.NuitrackRemoteStreamsConfigurationUI.OutputDepth = Properties.Settings.Default.depth;
            this.NuitrackHand.IsChecked = this.NuitrackRemoteStreamsConfigurationUI.OutputHandTracking = Properties.Settings.Default.hands;
            this.NuitrackGesture.IsChecked = this.NuitrackRemoteStreamsConfigurationUI.OutputGestureRecognizer = Properties.Settings.Default.gestures;
            this.NuitrackActivationKey.Text = this.NuitrackRemoteStreamsConfigurationUI.ActivationKey = Properties.Settings.Default.nuitrackKey;
            this.NuitrackDeviceSerialNumber.Text = this.NuitrackRemoteStreamsConfigurationUI.DeviceSerialNumber = Properties.Settings.Default.nuitrackDevice;
        }

        /// <summary>
        /// Refreshes the configuration from UI elements and saves to settings.
        /// </summary>
        private void RefreshConfigurationFromUI()
        {
            // Network Tab
            Properties.Settings.Default.rendezVousServerIp = this.RendezVousServerIp;
            Properties.Settings.Default.rendezVousServerPort = (uint)this.PipelineConfigurationUI.RendezVousPort;
            Properties.Settings.Default.commandSource = this.CommandSource;
            Properties.Settings.Default.commandPort = this.CommandPort;
            Properties.Settings.Default.ApplicationName = this.RendezVousApplicationNameUI;
            Properties.Settings.Default.IpToUse = this.IpSelectedUI;

            Properties.Settings.Default.isServer = this.IsRemoteServer;
            Properties.Settings.Default.isStreaming = this.IsStreaming;
            Properties.Settings.Default.isLocalRecording = this.IsLocalRecording;

            // Local Recording Tab
            Properties.Settings.Default.DatasetPath = this.PipelineConfigurationUI.DatasetPath;
            Properties.Settings.Default.DatasetName = this.PipelineConfigurationUI.DatasetName;
            Properties.Settings.Default.localSessionName = this.LocalSessionName;

            // VideoSources Tab - Common settings
            Properties.Settings.Default.encodingLevel = this.EncodingLevel;
            Properties.Settings.Default.sensorType = this.SensorTypeComboBox.SelectedIndex;
            Properties.Settings.Default.videoSource = this.VideoSourceComboBox.SelectedValue as string ?? string.Empty;
            Properties.Settings.Default.cameraCaptureFormat = this.CameraCaptureFormatComboBox.SelectedIndex >= 0 ? this.CameraCaptureFormatComboBox.SelectedIndex : 0;

            // Save sensor-specific settings based on selected sensor type
            switch ((ESensorType)this.SensorTypeComboBox.SelectedIndex)
            {
                case ESensorType.Camera:
                    // No specific settings for camera
                    break;

                case ESensorType.Kinect:
                    Properties.Settings.Default.remotePort = (uint)this.KinectRemoteStreamsConfigurationUI.StartingPort;
                    Properties.Settings.Default.audio = this.KinectRemoteStreamsConfigurationUI.OutputAudio;
                    Properties.Settings.Default.skeleton = this.KinectRemoteStreamsConfigurationUI.OutputBodies;
                    Properties.Settings.Default.rgb = this.KinectRemoteStreamsConfigurationUI.OutputColor;
                    Properties.Settings.Default.depth = this.KinectRemoteStreamsConfigurationUI.OutputDepth;
                    Properties.Settings.Default.depthCalibration = this.KinectRemoteStreamsConfigurationUI.OutputCalibration;
                    Properties.Settings.Default.infrared = this.KinectRemoteStreamsConfigurationUI.OutputInfrared;
                    Properties.Settings.Default.longExposureInfrared = this.KinectRemoteStreamsConfigurationUI.OutputLongExposureInfrared;
                    Properties.Settings.Default.colorToCameraMapping = this.KinectRemoteStreamsConfigurationUI.OutputColorToCameraMapping;
                    Properties.Settings.Default.rgbd = this.KinectRemoteStreamsConfigurationUI.OutputRGBD;
                    break;

                case ESensorType.AzureKinect:
                    Properties.Settings.Default.remotePort = (uint)this.KinectAzureRemoteStreamsConfigurationUI.StartingPort;
                    Properties.Settings.Default.audio = this.KinectAzureRemoteStreamsConfigurationUI.OutputAudio;
                    Properties.Settings.Default.skeleton = this.KinectAzureRemoteStreamsConfigurationUI.OutputBodies;
                    Properties.Settings.Default.rgb = this.KinectAzureRemoteStreamsConfigurationUI.OutputColor;
                    Properties.Settings.Default.infrared = this.KinectAzureRemoteStreamsConfigurationUI.OutputInfrared;
                    Properties.Settings.Default.depth = this.KinectAzureRemoteStreamsConfigurationUI.OutputDepth;
                    Properties.Settings.Default.depthCalibration = this.KinectAzureRemoteStreamsConfigurationUI.OutputCalibration;
                    Properties.Settings.Default.IMU = this.KinectAzureRemoteStreamsConfigurationUI.OutputImu;
                    Properties.Settings.Default.FPS = this.FPSList.IndexOf(this.KinectAzureRemoteStreamsConfigurationUI.CameraFPS);
                    Properties.Settings.Default.videoResolution = this.ResolutionsList.IndexOf(this.KinectAzureRemoteStreamsConfigurationUI.ColorResolution);
                    break;

                case ESensorType.Nuitrack:
                    Properties.Settings.Default.remotePort = (uint)this.NuitrackRemoteStreamsConfigurationUI.StartingPort;
                    Properties.Settings.Default.skeleton = this.NuitrackRemoteStreamsConfigurationUI.OutputSkeletonTracking;
                    Properties.Settings.Default.rgb = this.NuitrackRemoteStreamsConfigurationUI.OutputColor;
                    Properties.Settings.Default.depth = this.NuitrackRemoteStreamsConfigurationUI.OutputDepth;
                    Properties.Settings.Default.hands = this.NuitrackRemoteStreamsConfigurationUI.OutputHandTracking;
                    Properties.Settings.Default.gestures = this.NuitrackRemoteStreamsConfigurationUI.OutputGestureRecognizer;
                    Properties.Settings.Default.nuitrackKey = this.NuitrackRemoteStreamsConfigurationUI.ActivationKey;
                    Properties.Settings.Default.nuitrackDevice = this.NuitrackRemoteStreamsConfigurationUI.DeviceSerialNumber;
                    break;
            }

            Properties.Settings.Default.Save();
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
                this.KinectAzureRemoteStreamsConfigurationUI.KinectDeviceIndex = int.Parse(args[1]);
                this.KinectAzureRemoteStreamsConfigurationUI.OutputAudio = bool.Parse(args[2]);
                this.KinectAzureRemoteStreamsConfigurationUI.OutputBodies = bool.Parse(args[3]);
                this.KinectAzureRemoteStreamsConfigurationUI.OutputColor = bool.Parse(args[4]);
                this.KinectAzureRemoteStreamsConfigurationUI.OutputDepth = bool.Parse(args[5]);
                this.KinectAzureRemoteStreamsConfigurationUI.OutputCalibration = bool.Parse(args[6]);
                this.KinectAzureRemoteStreamsConfigurationUI.OutputImu = bool.Parse(args[7]);
                this.KinectAzureRemoteStreamsConfigurationUI.EncodingVideoLevel = int.Parse(args[8]);
                this.KinectAzureRemoteStreamsConfigurationUI.ColorResolution = (Microsoft.Azure.Kinect.Sensor.ColorResolution)int.Parse(args[9]);
                this.KinectAzureRemoteStreamsConfigurationUI.CameraFPS = (Microsoft.Azure.Kinect.Sensor.FPS)int.Parse(args[10]);
                this.KinectAzureRemoteStreamsConfigurationUI.IpToUse = args[11];
                this.KinectAzureRemoteStreamsConfigurationUI.StartingPort = int.Parse(args[12]);
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

            this.datasetPipeline.Log($"CommandRecieved with {message.Data.Item1} command, args: {message.Data.Item2}.");
            switch (message.Data.Item1)
            {
                case RendezVousPipeline.Command.Initialize:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        this.UpdateConfigurationFromArgs(args);
                    }));
                    break;
                case RendezVousPipeline.Command.Run:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        this.SetupSensor();
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
                        this.Stop();
                        this.Close();
                    }));
                    break;
                case RendezVousPipeline.Command.Status:
                    (this.datasetPipeline as RendezVousPipeline)?.SendCommand(RendezVousPipeline.Command.Status, this.CommandSource, this.datasetPipeline.Pipeline.StartTime == DateTime.MinValue ? "Waiting" : "Running");
                    break;
            }
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
            this.pipelineConfiguration.ClockPort = 0;
            this.pipelineConfiguration.CommandPort = this.CommandPort;
            if (this.isLocalRecording)
            {
                this.pipelineConfiguration.DatasetPath = this.LocalDatasetPath;
                this.pipelineConfiguration.DatasetName = this.LocalDatasetName;
            }
            else
            {
                this.pipelineConfiguration.DatasetPath = string.Empty;
                this.pipelineConfiguration.DatasetName = string.Empty;
            }

            this.pipelineConfiguration.RendezVousHost = this.IpSelectedUI;

            if (this.isRemoteServer)
            {
                this.datasetPipeline = new RendezVousPipeline(this.pipelineConfiguration, this.rendezVousApplicationName, this.RendezVousServerIp, this.internalLog);
            }
            else
            {
                this.datasetPipeline = new DatasetPipeline(this.pipelineConfiguration, this.rendezVousApplicationName, this.internalLog);
            }

            this.setupState = SetupState.PipelineInitialised;
        }

        /// <summary>
        /// Sets up the selected sensor based on current configuration.
        /// </summary>
        private void SetupSensor()
        {
            if (this.setupState >= SetupState.CameraInitialised)
            {
                return;
            }

            try
            {
                switch (this.sensorType)
                {
                    case ESensorType.Camera:
                        this.SetupCamera();
                        break;
                    case ESensorType.Kinect:
                        this.SetupKinect();
                        break;
                    case ESensorType.AzureKinect:
                        this.SetupAzureKinect();
                        break;
                    case ESensorType.Nuitrack:
                        this.SetupNuitrack();
                        break;
                }

                this.setupState = SetupState.CameraInitialised;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during sensor setup: {ex.Message}", "Sensor Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.AddLog(this.State = "Sensor setup failed");
                (this.datasetPipeline as RendezVousPipeline)?.SendCommand(RendezVousPipeline.Command.Status, this.commandSource, "Error");
            }
        }

        /// <summary>
        /// Sets up Azure Kinect sensor configuration and streams.
        /// </summary>
        private void SetupAzureKinect()
        {
            this.azureConfiguration.EncodingVideoLevel = this.encodingLevel;
            if (this.isRemoteServer)
            {
                RendezVousPipeline rdv = this.datasetPipeline as RendezVousPipeline;
                this.azureConfiguration.RendezVousApplicationName = this.rendezVousApplicationName;
                this.azureConfiguration.IpToUse = this.ipSelected;
                this.azureConfiguration.StartingPort = this.exportPort;
                var kinectStreams = new SAAC.RemoteConnectors.KinectAzureStreamsComponent(rdv, this.azureConfiguration, this.isLocalRecording);
                rdv.AddProcess(kinectStreams.GenerateProcess());
            }
            else
            {
                if (this.azureConfiguration.OutputBodies == true)
                {
                    this.azureConfiguration.OutputDepth = this.azureConfiguration.OutputInfrared = this.azureConfiguration.OutputCalibration = true;
                    this.azureConfiguration.BodyTrackerConfiguration = new Microsoft.Psi.AzureKinect.AzureKinectBodyTrackerConfiguration();
                }

                var sensor = new Microsoft.Psi.AzureKinect.AzureKinectSensor(this.datasetPipeline.Pipeline, this.azureConfiguration);
                Session session = this.datasetPipeline.CreateOrGetSession(this.localSessionName);
                if (this.azureConfiguration.OutputAudio == true)
                {
                    Microsoft.Psi.Audio.AudioCaptureConfiguration configuration = new Microsoft.Psi.Audio.AudioCaptureConfiguration();
                    int index = Microsoft.Psi.Audio.AudioCapture.GetAvailableDevices().ToList().FindIndex(value => { return value.Contains("Azure"); });
                    configuration.DeviceName = Microsoft.Psi.Audio.AudioCapture.GetAvailableDevices().ElementAt(index);
                    Microsoft.Psi.Audio.AudioCapture audioCapture = new Microsoft.Psi.Audio.AudioCapture(this.datasetPipeline.Pipeline, configuration);
                    this.datasetPipeline.CreateConnectorAndStore("Audio", "Audio", session, this.datasetPipeline.Pipeline, audioCapture.GetType(), audioCapture.Out, this.isLocalRecording);
                }

                if (this.azureConfiguration.OutputBodies == true)
                {
                    this.datasetPipeline.CreateConnectorAndStore("Bodies", "Bodies", session, this.datasetPipeline.Pipeline, sensor.Bodies.GetType(), sensor.Bodies, this.isLocalRecording);
                }

                if (this.azureConfiguration.OutputColor == true)
                {
                    var compressed = sensor.ColorImage.EncodeJpeg(this.encodingLevel);
                    this.datasetPipeline.CreateConnectorAndStore("RGB", "RGB", session, this.datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, this.isLocalRecording);
                }

                if (this.azureConfiguration.OutputInfrared == true)
                {
                    var compressed = sensor.InfraredImage.EncodeJpeg(this.encodingLevel);
                    this.datasetPipeline.CreateConnectorAndStore("Infrared", "Infrared", session, this.datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, this.isLocalRecording);
                }

                if (this.azureConfiguration.OutputDepth == true)
                {
                    var compressed = sensor.DepthImage.EncodePng();
                    this.datasetPipeline.CreateConnectorAndStore("Depth", "Depth", session, this.datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, this.isLocalRecording);
                }

                if (this.azureConfiguration.OutputCalibration == true)
                {
                    this.datasetPipeline.CreateConnectorAndStore("Calibration", "Calibration", session, this.datasetPipeline.Pipeline, sensor.DepthDeviceCalibrationInfo.GetType(), sensor.DepthDeviceCalibrationInfo, this.isLocalRecording);
                }

                if (this.azureConfiguration.OutputImu == true)
                {
                    this.datasetPipeline.CreateConnectorAndStore("Imu", "Imu", session, this.datasetPipeline.Pipeline, sensor.Imu.GetType(), sensor.Imu, this.isLocalRecording);
                }
            }
        }

        /// <summary>
        /// Sets up Kinect sensor configuration and streams.
        /// </summary>
        private void SetupKinect()
        {
            this.kinectConfiguration.EncodingVideoLevel = this.encodingLevel;
            if (this.isRemoteServer)
            {
                RendezVousPipeline rdv = this.datasetPipeline as RendezVousPipeline;
                this.kinectConfiguration.RendezVousApplicationName = this.rendezVousApplicationName;
                this.kinectConfiguration.IpToUse = this.ipSelected;
                this.kinectConfiguration.StartingPort = this.exportPort;
                var kinectStreams = new SAAC.RemoteConnectors.KinectRemoteStreamsComponent(rdv, this.kinectConfiguration, this.isLocalRecording);
                rdv.AddProcess(kinectStreams.GenerateProcess());
            }
            else
            {
                var sensor = new Microsoft.Psi.Kinect.KinectSensor(this.datasetPipeline.Pipeline, this.kinectConfiguration);
                var session = this.datasetPipeline.CreateOrGetSessionFromMode(this.localSessionName);
                if (this.kinectConfiguration.OutputAudio == true)
                {
                    this.datasetPipeline.CreateConnectorAndStore("Audio", "Audio", session, this.datasetPipeline.Pipeline, sensor.Audio.GetType(), sensor.Audio, this.isLocalRecording);
                }

                if (this.kinectConfiguration.OutputBodies == true)
                {
                    this.datasetPipeline.CreateConnectorAndStore("Bodies", "Bodies", session, this.datasetPipeline.Pipeline, sensor.Bodies.GetType(), sensor.Bodies, this.isLocalRecording);
                }

                if (this.kinectConfiguration.OutputColor == true)
                {
                    var compressed = sensor.ColorImage.EncodeJpeg(this.kinectConfiguration.EncodingVideoLevel);
                    this.datasetPipeline.CreateConnectorAndStore("RGB", "RGB", session, this.datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, this.isLocalRecording);
                }

                if (this.kinectConfiguration.OutputRGBD == true)
                {
                    var compressed = sensor.RGBDImage.EncodeJpeg(this.kinectConfiguration.EncodingVideoLevel);
                    this.datasetPipeline.CreateConnectorAndStore("RGBD", "RGBD", session, this.datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, this.isLocalRecording);
                }

                if (this.kinectConfiguration.OutputDepth == true)
                {
                    var compressed = sensor.DepthImage.EncodePng();
                    this.datasetPipeline.CreateConnectorAndStore("Depth", "Depth", session, this.datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, this.isLocalRecording);
                }

                if (this.kinectConfiguration.OutputInfrared == true)
                {
                    var compressed = sensor.InfraredImage.EncodeJpeg(this.kinectConfiguration.EncodingVideoLevel);
                    this.datasetPipeline.CreateConnectorAndStore("Infrared", "Infrared", session, this.datasetPipeline.Pipeline, compressed.GetType(), compressed, this.isLocalRecording);
                }

                if (this.kinectConfiguration.OutputLongExposureInfrared == true)
                {
                    var compressed = sensor.LongExposureInfraredImage.EncodeJpeg(this.kinectConfiguration.EncodingVideoLevel);
                    this.datasetPipeline.CreateConnectorAndStore("LongExposureInfrared", "LongExposureInfrared", session, this.datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, this.isLocalRecording);
                }

                if (this.kinectConfiguration.OutputColorToCameraMapping == true)
                {
                    this.datasetPipeline.CreateConnectorAndStore("ColorToCameraMapper", "ColorToCameraMapper", session, this.datasetPipeline.Pipeline, sensor.ColorToCameraMapper.GetType(), sensor.ColorToCameraMapper, this.isLocalRecording);
                }

                if (this.kinectConfiguration.OutputCalibration == true)
                {
                    this.datasetPipeline.CreateConnectorAndStore("Calibration", "Calibration", session, this.datasetPipeline.Pipeline, sensor.DepthDeviceCalibrationInfo.GetType(), sensor.DepthDeviceCalibrationInfo, this.isLocalRecording);
                }
            }
        }

        /// <summary>
        /// Sets up Nuitrack sensor configuration and streams.
        /// </summary>
        private void SetupNuitrack()
        {
            this.nuitrackConfiguration.EncodingVideoLevel = this.encodingLevel;
            if (this.isRemoteServer)
            {
                RendezVousPipeline rdv = this.datasetPipeline as RendezVousPipeline;
                this.nuitrackConfiguration.RendezVousApplicationName = this.RendezVousApplicationNameUI;
                this.nuitrackConfiguration.IpToUse = this.ipSelected;
                this.nuitrackConfiguration.StartingPort = this.exportPort;
                var nuitrackStreams = new SAAC.RemoteConnectors.NuitrackRemoteStreamsComponent(rdv, this.nuitrackConfiguration, this.isLocalRecording);
                rdv.AddProcess(nuitrackStreams.GenerateProcess());
            }
            else
            {
                var Sensor = new SAAC.Nuitrack.NuitrackSensor(this.datasetPipeline.Pipeline, this.nuitrackConfiguration);
                var session = this.datasetPipeline.CreateOrGetSessionFromMode(this.localSessionName);
                if (this.nuitrackConfiguration.OutputSkeletonTracking == true)
                {
                    this.datasetPipeline.CreateConnectorAndStore("Bodies", "Bodies", session, this.datasetPipeline.Pipeline, Sensor.OutBodies.GetType(), Sensor.OutBodies, this.isLocalRecording);
                }

                if (this.nuitrackConfiguration.OutputColor == true)
                {
                    var compressed = Sensor.OutColorImage.EncodeJpeg(this.nuitrackConfiguration.EncodingVideoLevel);
                    this.datasetPipeline.CreateConnectorAndStore("RGB", "RGB", session, this.datasetPipeline.Pipeline, compressed.GetType(), compressed, this.isLocalRecording);
                }

                if (this.nuitrackConfiguration.OutputDepth == true)
                {
                    var compressed = Sensor.OutDepthImage.EncodePng();
                    this.datasetPipeline.CreateConnectorAndStore("Depth", "Depth", session, this.datasetPipeline.Pipeline, compressed.GetType(), compressed, this.isLocalRecording);
                }

                if (this.nuitrackConfiguration.OutputHandTracking == true)
                {
                    this.datasetPipeline.CreateConnectorAndStore("Hands", "Hands", session, this.datasetPipeline.Pipeline, Sensor.OutHands.GetType(), Sensor.OutHands, this.isLocalRecording);
                }

                if (this.nuitrackConfiguration.OutputUserTracking == true)
                {
                    this.datasetPipeline.CreateConnectorAndStore("Users", "Users", session, this.datasetPipeline.Pipeline, Sensor.OutUsers.GetType(), Sensor.OutUsers, this.isLocalRecording);
                }

                if (this.nuitrackConfiguration.OutputGestureRecognizer == true)
                {
                    this.datasetPipeline.CreateConnectorAndStore("Gestures", "Gestures", session, this.datasetPipeline.Pipeline, Sensor.OutGestures.GetType(), Sensor.OutGestures, this.isLocalRecording);
                }
            }
        }

        /// <summary>
        /// Sets up camera configuration and streams.
        /// </summary>
        private void SetupCamera()
        {
            MediaCaptureConfiguration configuration = new MediaCaptureConfiguration();
            CaptureFormat formats = this.cameraFormats[this.CameraCaptureFormatComboBox.SelectedIndex];
            configuration.Framerate = formats.nFrameRateNumerator / formats.nFrameRateDenominator;
            configuration.Height = formats.nHeight;
            configuration.Width = formats.nWidth;
            configuration.DeviceId = this.VideoSourceComboBox.SelectedValue as string;
            MediaCapture camera = new MediaCapture(this.datasetPipeline.Pipeline, configuration);
            var compressed = camera.Out.EncodeJpeg(this.encodingLevel);
            string streamName = "RGB";
            var session = this.datasetPipeline.CreateOrGetSessionFromMode(this.isLocalRecording ? this.localSessionName : this.rendezVousApplicationName);
            this.datasetPipeline.CreateConnectorAndStore(streamName, $"{this.rendezVousApplicationName}-{streamName}", session, this.datasetPipeline.Pipeline, compressed.GetType(), compressed.Out, this.IsLocalRecording);
            if (this.isRemoteServer && this.isStreaming)
            {
                RendezVousPipeline rdv = this.datasetPipeline as RendezVousPipeline;
                if (this.isStreaming)
                {
                    Microsoft.Psi.Remoting.RemoteExporter cameraExporter = new Microsoft.Psi.Remoting.RemoteExporter(datasetPipeline.Pipeline, exportPort, Microsoft.Psi.Remoting.TransportKind.Tcp);
                    cameraExporter.Exporter.Write(compressed, streamName); 
                    rdv.AddProcess(new Rendezvous.Process(rendezVousApplicationName, [cameraExporter.ToRendezvousEndpoint(ipSelected)], "Version1.0"));
                }
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
                (this.datasetPipeline as RendezVousPipeline)?.Start((d) =>
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        this.AddLog(this.State = "Connected to server");
                        (this.datasetPipeline as RendezVousPipeline)?.SendCommand(RendezVousPipeline.Command.Status, this.commandSource, "Waiting");
                    }));
                });
            }
        }

        /// <summary>
        /// Starts the pipeline and sensor capture.
        /// </summary>
        private void Start()
        {
            this.SetupPipeline();
            this.SetupSensor();
            if (this.setupState == SetupState.CameraInitialised)
            {
                this.BtnStart.IsEnabled = this.BtnStartNet.IsEnabled = false;
                this.datasetPipeline.RunPipelineAndSubpipelines();
                (this.datasetPipeline as RendezVousPipeline)?.SendCommand(RendezVousPipeline.Command.Status, this.commandSource, "Running");
                this.AddLog(this.State = "Started");
            }
        }

        /// <summary>
        /// Handles the window closing event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            this.Stop();
            base.OnClosing(e);
        }

        /// <summary>
        /// Handles the start RendezVous button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnStartRendezVous(object sender, RoutedEventArgs e)
        {
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
        /// Updates the camera capture format list based on the selected video source.
        /// </summary>
        /// <param name="index">The selected format index.</param>
        private void UpdateCameraCaptureFormat(int index = 0)
        {
            if ((this.VideoSourceComboBox.SelectedValue as string) is null && Properties.Settings.Default.videoSource?.Length == 0)
            {
                return;
            }

            this.CameraCaptureFormat.Clear();
            this.cameraFormats = MediaCapture.GetAvailableFormats(this.VideoSourceComboBox.SelectedValue as string ?? Properties.Settings.Default.videoSource);
            if (this.cameraFormats is null)
            {
                return;
            }

            foreach (CaptureFormat format in this.cameraFormats)
            {
                this.CameraCaptureFormat.Add($"{format.nWidth}x{format.nHeight}@{format.nFrameRateNumerator}");
            }

            if (this.CameraCaptureFormat.Count > 0)
            {
                this.CameraCaptureFormatComboBox.SelectedIndex = index > this.CameraCaptureFormat.Count ? 0 : index;
            }
        }

        /// <summary>
        /// Handles the video source selection changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void VideoSourceSelected(object sender, RoutedEventArgs e)
        {
            this.UpdateCameraCaptureFormat();
            e.Handled = true;
        }

        /// <summary>
        /// Handles the sensor type selection changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void SensorTypeSelected(object sender, RoutedEventArgs e)
        {
            this.CameraConfiguration.Visibility = this.KinectConfiguration.Visibility = this.NuitrackConfiguration.Visibility = this.AzureKinectConfiguration.Visibility = Visibility.Collapsed;
            switch (this.SensorTypeComboBox.SelectedIndex)
            {
                case 3:
                    this.sensorType = ESensorType.Nuitrack;
                    this.NuitrackConfiguration.Visibility = Visibility.Visible;
                    break;
                case 2:
                    this.sensorType = ESensorType.AzureKinect;
                    this.AzureKinectConfiguration.Visibility = Visibility.Visible;
                    break;
                case 1:
                    this.sensorType = ESensorType.Kinect;
                    this.KinectConfiguration.Visibility = Visibility.Visible;
                    break;
                case 0:
                    this.sensorType = ESensorType.Camera;
                    this.UpdateCameraCaptureFormat();
                    this.CameraConfiguration.Visibility = Visibility.Visible;
                    break;
            }

            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            e.Handled = true;
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
            this.UpdateStreamingPortRangeStartTextBox();
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
    }
}
