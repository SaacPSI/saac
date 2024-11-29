using Microsoft.Psi;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Net;
using Microsoft.Win32;
using SAAC.RemoteConnectors;
using SAAC.RendezVousPipelineServices;
using SAAC.KinectAzureRemoteServices;
using System.Windows.Controls;
using SharpDX;

namespace KinectAzureRemoteApp
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion


        private string state = "Hello";
        public string State
        {
            get => state;
            set => SetProperty(ref state, value);
        }
        public void DelegateMethodStatus(string status)
        {
            State = status;
        }

        // LOG
        private string logs = "";
        public string Logs
        {
            get => logs;
            set => SetProperty(ref logs, value);
        }
        public void DelegateMethod(string logs)
        {
            Logs = logs;
        }

        private KinectAzureRemoteStreamsConfiguration Configuration = new KinectAzureRemoteStreamsConfiguration();
        private RendezVousPipelineConfiguration PipelineConfiguration = new RendezVousPipelineConfiguration();
        
        private string rendezVousServerIp = "localhost";
        public string RendezVousServerIp
        {
            get => rendezVousServerIp;
            set => SetProperty(ref rendezVousServerIp, value);
        }
        public void DelegateMethodRendezVousServerIP(string ip)
        {
            RendezVousServerIp = ip;
        }

        public KinectAzureRemoteStreamsConfiguration ConfigurationUI
        {
            get => Configuration;
            set => SetProperty(ref Configuration, value);
        }

        public RendezVousPipelineConfiguration PipelineConfigurationUI
        {
            get => PipelineConfiguration;
            set => SetProperty(ref PipelineConfiguration, value);
        }

        // DatasetPath
        public string DatasetPath
        {
            get => PipelineConfiguration.DatasetPath;
            set => SetProperty(ref PipelineConfiguration.DatasetPath, value);
        }
        public void DelegateMethodDatasetPath(string path)
        {
            PipelineConfiguration.DatasetPath = path;
        }

        // DatasetName
        public string DatasetName
        {
            get => PipelineConfiguration.DatasetName;
            set => SetProperty(ref PipelineConfiguration.DatasetName, value);
        }
        public void DelegateMethodDatasetName(string path)
        {
            PipelineConfiguration.DatasetName = path;
        }

        public int ServerPort
        {
            get => PipelineConfigurationUI.RendezVousPort;
            set => SetProperty(ref PipelineConfigurationUI.RendezVousPort, value);
        }

        private string commandSource = "Server";
        public string CommandSource
        {
            get => commandSource;
            set => SetProperty(ref commandSource, value);
        }
        public void DelegateMethodCS(string commandSource)
        {
            CommandSource = commandSource;
        }

    

        //ToDo add more resolution definition
        public List<Microsoft.Azure.Kinect.Sensor.ColorResolution> ResolutionsList { get; }
        public List<Microsoft.Azure.Kinect.Sensor.FPS> FPSList { get; }
        public List<string> IPsList { get; }
        private RendezVousPipeline? Pipeline;
        private KinectAzureStreamsComponent? KinectStreams;

        public MainWindow()
        {
            DataContext = this;
            PipelineConfiguration.ClockPort = PipelineConfiguration.CommandPort = 0;
            PipelineConfiguration.AutomaticPipelineRun = true;
            ResolutionsList = new List<Microsoft.Azure.Kinect.Sensor.ColorResolution>();
            foreach (Microsoft.Azure.Kinect.Sensor.ColorResolution name in Enum.GetValues(typeof(Microsoft.Azure.Kinect.Sensor.ColorResolution)))
            {
                ResolutionsList.Add(name);
            }
            FPSList = new List<Microsoft.Azure.Kinect.Sensor.FPS>();
            foreach (Microsoft.Azure.Kinect.Sensor.FPS name in Enum.GetValues(typeof(Microsoft.Azure.Kinect.Sensor.FPS)))
            {
                FPSList.Add(name);
            }
            IPsList = new List<string> {"localhost"};
            foreach(var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                IPsList.Add(ip.ToString()); 
            }
            KinectStreams = null;
            Pipeline = null;

            RendezVousServerIp = Properties.Settings.Default.rendezVousServerIp;
            ServerPort = (int)(Properties.Settings.Default.rendezVousServerPort);
            ConfigurationUI.StartingPort = (int)(Properties.Settings.Default.remotePort);
            ConfigurationUI.EncodingVideoLevel = (int)Properties.Settings.Default.encodingLevel;
            ConfigurationUI.RendezVousApplicationName = Properties.Settings.Default.ApplicationName;
            Configuration.VideoResolution = (Microsoft.Azure.Kinect.Sensor.ColorResolution)Properties.Settings.Default.videoResolution;
            PipelineConfiguration.DatasetName = Properties.Settings.Default.DatasetName;
            PipelineConfiguration.DatasetPath = Properties.Settings.Default.DatasetPath;
            InitializeComponent();
            Audio.IsChecked = Configuration.StreamAudio = Properties.Settings.Default.audio;
            Skeleton.IsChecked = Configuration.StreamSkeleton = Properties.Settings.Default.skeleton;
            RGB.IsChecked = Configuration.StreamVideo = Properties.Settings.Default.rgb;
            Depth.IsChecked = Configuration.StreamDepth = Properties.Settings.Default.depth;
            DepthCalibration.IsChecked = Configuration.StreamDepthCalibration = Properties.Settings.Default.depthCalibration;
            IMU.IsChecked = Configuration.StreamIMU = Properties.Settings.Default.IMU;
            PipelineConfiguration.RendezVousHost = Configuration.IpToUse = Properties.Settings.Default.IpToUse;
            IPs.SelectedIndex = IPsList.IndexOf(PipelineConfiguration.RendezVousHost);
            ColoRes.SelectedIndex = ResolutionsList.IndexOf((Microsoft.Azure.Kinect.Sensor.ColorResolution)Properties.Settings.Default.videoResolution);
            FPS.SelectedIndex = FPSList.IndexOf((Microsoft.Azure.Kinect.Sensor.FPS)Properties.Settings.Default.FPS);
            UpdateLayout();
            IPs.SelectionChanged += IPs_SelectionChanged;
            ColoRes.SelectionChanged += ColoRes_SelectionChanged;
            FPS.SelectionChanged += FPS_SelectionChanged;
        }

        private void FPS_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Configuration.FPS = (Microsoft.Azure.Kinect.Sensor.FPS)FPS.SelectedValue;
        }

        private void IPs_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Configuration.IpToUse = PipelineConfiguration.RendezVousHost = (string)IPs.SelectedValue;
        }

        private void ColoRes_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Configuration.VideoResolution = (Microsoft.Azure.Kinect.Sensor.ColorResolution)ColoRes.SelectedValue;
        }

        private void RefreshUIFromConfiguration()
        {
            ConfigurationUI.StartingPort = (int)Properties.Settings.Default.remotePort;
            RendezVousServerIp = Properties.Settings.Default.rendezVousServerIp;
            ServerPort = (int)(Properties.Settings.Default.rendezVousServerPort);
            Audio.IsChecked = Configuration.StreamAudio = Properties.Settings.Default.audio;
            Skeleton.IsChecked = Configuration.StreamSkeleton = Properties.Settings.Default.skeleton;
            RGB.IsChecked = Configuration.StreamVideo = Properties.Settings.Default.rgb;
            Depth.IsChecked = Configuration.StreamDepth = Properties.Settings.Default.depth;
            DepthCalibration.IsChecked = Configuration.StreamDepthCalibration = Properties.Settings.Default.depthCalibration;
            IMU.IsChecked = Configuration.StreamIMU = Properties.Settings.Default.IMU;
            ConfigurationUI.RendezVousApplicationName = Properties.Settings.Default.ApplicationName;
            PipelineConfiguration.RendezVousHost = Configuration.IpToUse = Properties.Settings.Default.IpToUse;
            IPs.SelectedIndex = IPsList.IndexOf(PipelineConfiguration.RendezVousHost);
            ColoRes.SelectedIndex = ResolutionsList.IndexOf((Microsoft.Azure.Kinect.Sensor.ColorResolution)Properties.Settings.Default.videoResolution);
            FPS.SelectedIndex = FPSList.IndexOf((Microsoft.Azure.Kinect.Sensor.FPS)Properties.Settings.Default.FPS);
     
            Properties.Settings.Default.Save();
            UpdateLayout();
        }

        private void RefreshConfigurationFromUI()
        {
            Properties.Settings.Default.remotePort = (uint)ConfigurationUI.StartingPort;
            Properties.Settings.Default.rendezVousServerPort = (uint)ServerPort;
            Configuration.StreamAudio = Properties.Settings.Default.audio = (bool)(Audio.IsChecked != null ? Audio.IsChecked : false);
            Configuration.StreamSkeleton = Properties.Settings.Default.skeleton = (bool)(Skeleton.IsChecked != null ? Skeleton.IsChecked : false);
            Configuration.StreamVideo = Properties.Settings.Default.rgb = (bool)(RGB.IsChecked != null ? RGB.IsChecked : false);
            Configuration.StreamDepth = Properties.Settings.Default.depth = (bool)(Depth.IsChecked != null ? Depth.IsChecked : false);
            Configuration.StreamDepthCalibration = Properties.Settings.Default.depthCalibration = (bool)(DepthCalibration.IsChecked != null ? DepthCalibration.IsChecked : false);
            Configuration.StreamIMU = Properties.Settings.Default.IMU = (bool)(IMU.IsChecked != null ? IMU.IsChecked : false);
            Configuration.IpToUse = Properties.Settings.Default.IpToUse = PipelineConfiguration.RendezVousHost;
            Properties.Settings.Default.videoResolution =ResolutionsList.IndexOf((Microsoft.Azure.Kinect.Sensor.ColorResolution)Configuration.VideoResolution);
            Properties.Settings.Default.FPS = FPSList.IndexOf((Microsoft.Azure.Kinect.Sensor.FPS)Configuration.FPS);
            Properties.Settings.Default.IpToUse = PipelineConfiguration.RendezVousHost;
            Properties.Settings.Default.rendezVousServerIp = RendezVousServerIp;
            Properties.Settings.Default.ApplicationName = ConfigurationUI.RendezVousApplicationName;
            Properties.Settings.Default.IpToUse = PipelineConfiguration.RendezVousHost;
            Properties.Settings.Default.DatasetName = PipelineConfiguration.DatasetName;
            Properties.Settings.Default.DatasetPath = PipelineConfiguration.DatasetPath;
            Properties.Settings.Default.Save();
        }

        private bool UpdateConfigurationFromArgs(string[] args)
        {
            try
            {
                Configuration.KinectDeviceIndex = int.Parse(args[1]);
                Configuration.StreamAudio = bool.Parse(args[2]);
                Configuration.StreamSkeleton = bool.Parse(args[3]);
                Configuration.StreamVideo = bool.Parse(args[4]);
                Configuration.StreamDepth = bool.Parse(args[5]);
                Configuration.StreamDepthCalibration = bool.Parse(args[6]);
                Configuration.StreamIMU = bool.Parse(args[7]);
                Configuration.EncodingVideoLevel = int.Parse(args[8]);
                Configuration.VideoResolution = (Microsoft.Azure.Kinect.Sensor.ColorResolution)int.Parse(args[9]);
                Configuration.FPS = (Microsoft.Azure.Kinect.Sensor.FPS)int.Parse(args[10]);
                Configuration.IpToUse = args[11];
                Configuration.StartingPort = int.Parse(args[12]);
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

            if (args[0] != ConfigurationUI.RendezVousApplicationName)
                return;

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
                        SetupKinect();
                    }));
                    break;
                case RendezVousPipeline.Command.Stop:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        StopKinect();
                    }));
                    break;
                case RendezVousPipeline.Command.Close:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        StopPipeline();
                        Close();
                    }));
                    break;
                case RendezVousPipeline.Command.Restart:
                    if (UpdateConfigurationFromArgs(args))
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            OnRestart();
                        }));
                    }
                    break;
                case RendezVousPipeline.Command.Status:
                    Pipeline?.CommandEmitter.Post((RendezVousPipeline.Command.Status, KinectStreams == null ? "Not Initialised": Pipeline.Pipeline.StartTime.ToString()), Pipeline.Pipeline.GetCurrentTime());
                    break;
            }
        }

        private void SetupRendezVous()
        {
            //disable ui
            RendezVousGrid.IsEnabled = false;
            PipelineConfiguration.Diagnostics = (bool)Diagnostics.IsChecked ? RendezVousPipeline.DiagnosticsMode.Export : RendezVousPipeline.DiagnosticsMode.Store;
            PipelineConfiguration.CommandDelegate = CommandRecieved;
            Pipeline = new RendezVousPipeline(PipelineConfiguration, ConfigurationUI.RendezVousApplicationName, RendezVousServerIp, (log) => { Logs += $"{log}\n"; });
            State = "Waiting for server";
            Pipeline.Start();
            State = "Ready to start Kinect";
        }

        private void SetupKinect()
        {
            if (Pipeline == null)
                SetupRendezVous();

            //disable ui
            DataFormular.IsEnabled = false;
            Configuration.StreamSkeleton = true;
            KinectStreams = new SAAC.KinectAzureRemoteServices.KinectAzureStreamsComponent(Pipeline, Configuration);
            //Pipeline.AddProcess(KinectStreams.GenerateProcess());
            KinectStreams.GenerateProcess();
            KinectStreams.RunAsync();
            State = "Running";
        }

        private void StopPipeline()
        {
            // Stop correctly the everything.
            State = "Stopping";
            if (Pipeline != null)
            {
                Pipeline.Stop();
            }
        }

        private void StopKinect()
        {
            State = "Stopping Kinect";
            if (KinectStreams != null && KinectStreams.Sensor != null && Pipeline != null)
            {
                if(!Pipeline.RemoveProcess(Configuration.RendezVousApplicationName))
                    Console.WriteLine("error remove rendezvous");
                try
                {
                    KinectStreams.Dispose();
                }
                catch(Exception ex) 
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        protected void OnRestart()
        {
            StopKinect();
            RefreshUIFromConfiguration();
            SetupKinect();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            StopPipeline();
            RefreshConfigurationFromUI();
            base.OnClosing(e);
        }

        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            StopPipeline();
            Close();
        }

        private void BtnStartRendezVous(object sender, RoutedEventArgs e)
        {
            State = "Initializing RendezVous";
            RefreshConfigurationFromUI();
            SetupRendezVous();
        }

        private void BtnStartAll(object sender, RoutedEventArgs e)
        {
            State = "Initializing Kinect";
            RefreshConfigurationFromUI();
            SetupKinect();
        }

        private void BtnStopClick(object sender, RoutedEventArgs e)
        {
            StopKinect();
            DataFormular.IsEnabled = true;
            RendezVousGrid.IsEnabled = true;
        }

        private void BtnLoadClick(object sender, RoutedEventArgs e)
        {
            RefreshUIFromConfiguration();
            UpdateLayout();
        }

        private void BtnSaveClick(object sender, RoutedEventArgs e)
        {
            RefreshConfigurationFromUI();
        }

        private void Log_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            TextBox? log = sender as TextBox;
            if (log == null)
                return;
            log.CaretIndex = log.Text.Length;
            log.ScrollToEnd();
        }

        private void BtnBrowseNameClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                PipelineConfiguration.DatasetPath = openFileDialog.FileName.Substring(0, openFileDialog.FileName.IndexOf(openFileDialog.SafeFileName));
                PipelineConfiguration.DatasetName = openFileDialog.SafeFileName;
            }
        }
    }
}
