using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Rendezvous;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Net;
using Microsoft.Psi.Diagnostics;
using SAAC.RemoteConnectors;
using SAAC.RendezVousPipelineServices;

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

        private KinectAzureRemoteStreamsConfiguration Configuration = new KinectAzureRemoteStreamsConfiguration();
        private RendezVousPipelineConfiguration PipelineConfiguration = new RendezVousPipelineConfiguration();

        public KinectAzureRemoteStreamsConfiguration ConfigurationUI
        {
            get => Configuration;
            set => SetProperty(ref Configuration, value);
        }

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

        private string log = "";
        public string Log
        {
            get => log;
            set => SetProperty(ref log, value);
        }
        public void DelegateMethodLog(string log)
        {
            Log = log;
        }

        //ToDo add more resolution definition
        public enum Resolution{ Native, R1920_1080, R960_540, R640_360, Custom };
        private Dictionary<Resolution, Tuple<float, float>> resolutionDictionary;
        public List<Resolution> ResolutionsList { get; }

        private Resolution colorResolution = Resolution.R640_360;
        public Resolution ColorResolution
        {
            get => colorResolution;
            set => SetProperty(ref colorResolution, value);
        }
        public void DelegateMethodColorResolution(Resolution val)
        {
            ColorResolution = val;
        }

     
        public List<string> IPsList { get; }
        public string IPSelected
        {
            get => PipelineConfiguration.RendezVousHost;
            set => SetProperty(ref PipelineConfiguration.RendezVousHost, value);
        }
        public void DelegateMethodColorResolution(string val)
        {
            PipelineConfiguration.RendezVousHost = val;
        }

        private RendezVousPipeline? Pipeline;
        private KinectAzureRemoteStreams? KinectStreams;

        public MainWindow()
        {
            DataContext = this;
            resolutionDictionary = new Dictionary<Resolution, Tuple<float, float>>
            {
                 { Resolution.R1920_1080, new Tuple<float, float>(1920.0f, 1080.0f) }
                ,{ Resolution.R960_540, new Tuple<float, float>(960.0f, 540.0f) }
                ,{ Resolution.R640_360, new Tuple<float, float>(640.0f, 360.0f) }
                ,{ Resolution.Custom, new Tuple<float, float>(640.0f, 360.0f) }
            };
            ResolutionsList = new List<Resolution>();
            foreach (Resolution name in Enum.GetValues(typeof(Resolution)))
            {
                ResolutionsList.Add(name);
            }
            IPsList = new List<string>();
            foreach(var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                IPsList.Add(ip.ToString()); 
            }
            KinectStreams = null;
            Pipeline = null;

            RendezVousServerIp = Properties.Settings.Default.rendezVousServerIp;
            ConfigurationUI.RendezVousPort = (int)(Properties.Settings.Default.rendezVousServerPort);
            ConfigurationUI.EncodingVideoLevel = (int)Properties.Settings.Default.encodingLevel;
            ConfigurationUI.RendezVousApplicationName = Properties.Settings.Default.ApplicationName;
            Configuration.VideoResolution = resolutionDictionary[(Resolution)Properties.Settings.Default.videoResolution];
            InitializeComponent();
            Audio.IsChecked = Configuration.StreamAudio = Properties.Settings.Default.audio;
            Skeleton.IsChecked = Configuration.StreamSkeleton = Properties.Settings.Default.skeleton;
            RGB.IsChecked = Configuration.StreamVideo = Properties.Settings.Default.rgb;
            Depth.IsChecked = Configuration.StreamDepth = Properties.Settings.Default.depth;
            DepthCalibration.IsChecked = Configuration.StreamDepthCalibration = Properties.Settings.Default.depthCalibration;
            IMU.IsChecked = Configuration.StreamIMU = Properties.Settings.Default.IMU;
            PipelineConfiguration.RendezVousHost = Configuration.RendezVousAddress = Properties.Settings.Default.IpToUse;
            UpdateLayout();
        }

        private void RefreshUIFromConfiguration()
        {
            ConfigurationUI.RendezVousPort = (int)Properties.Settings.Default.rendezVousServerPort;
            Audio.IsChecked = Properties.Settings.Default.audio = Configuration.StreamAudio;
            Skeleton.IsChecked = Properties.Settings.Default.skeleton = Configuration.StreamSkeleton;
            RGB.IsChecked = Properties.Settings.Default.rgb = Configuration.StreamVideo;
            Depth.IsChecked = Properties.Settings.Default.depth = Configuration.StreamDepth;
            DepthCalibration.IsChecked = Properties.Settings.Default.depthCalibration = Configuration.StreamDepthCalibration;
            IMU.IsChecked = Properties.Settings.Default.IMU = Configuration.StreamIMU;
            PipelineConfiguration.RendezVousHost = Properties.Settings.Default.IpToUse = Configuration.RendezVousAddress;
            if (Configuration.VideoResolution != null)
            {
                bool found = false;
                foreach (var tuple in resolutionDictionary)
                {
                    if (tuple.Value.Item1 == Configuration.VideoResolution.Item1 && tuple.Value.Item2 == Configuration.VideoResolution.Item2)
                    {
                        Properties.Settings.Default.videoResolution = (int)(colorResolution = tuple.Key);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    resolutionDictionary[Resolution.Custom] = Configuration.VideoResolution;
                    Properties.Settings.Default.videoResolution = (int)(colorResolution = Resolution.Custom);
                }
            }
            else
                Properties.Settings.Default.videoResolution = (int)(colorResolution =  Resolution.Native);
            Properties.Settings.Default.Save();
            UpdateLayout();
        }

        private void RefreshConfigurationFromUI()
        {
            Properties.Settings.Default.rendezVousServerPort = (uint)ConfigurationUI.RendezVousPort;

            Configuration.StreamAudio = Properties.Settings.Default.audio = (bool)(Audio.IsChecked != null ? Audio.IsChecked : false);
            Configuration.StreamSkeleton = Properties.Settings.Default.skeleton = (bool)(Skeleton.IsChecked != null ? Skeleton.IsChecked : false);
            Configuration.StreamVideo = Properties.Settings.Default.rgb = (bool)(RGB.IsChecked != null ? RGB.IsChecked : false);
            Configuration.StreamDepth = Properties.Settings.Default.depth = (bool)(Depth.IsChecked != null ? Depth.IsChecked : false);
            Configuration.StreamDepthCalibration = Properties.Settings.Default.depthCalibration = (bool)(DepthCalibration.IsChecked != null ? DepthCalibration.IsChecked : false);
            Configuration.StreamIMU = Properties.Settings.Default.IMU = (bool)(IMU.IsChecked != null ? IMU.IsChecked : false);

            Configuration.RendezVousAddress = Properties.Settings.Default.IpToUse = PipelineConfiguration.RendezVousHost;
            if (Configuration.VideoResolution != null)
            {
                bool found = false;
                foreach (var tuple in resolutionDictionary)
                {
                    if (tuple.Value.Item1 == Configuration.VideoResolution.Item1 && tuple.Value.Item2 == Configuration.VideoResolution.Item2)
                    {
                        Properties.Settings.Default.videoResolution = (int)(colorResolution = tuple.Key);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    resolutionDictionary[Resolution.Custom] = Configuration.VideoResolution;
                    Properties.Settings.Default.videoResolution = (int)(colorResolution = Resolution.Custom);
                }
            }
            else
                Properties.Settings.Default.videoResolution = (int)(colorResolution = Resolution.Native);
            Properties.Settings.Default.Save();
        }

        private bool UpdateConfigurationFromArgs(string arguments)
        {
            try
            {
                var args = arguments.Split([';']);
                Configuration.KinectDeviceIndex = int.Parse(args[0]);
                Configuration.StreamAudio = bool.Parse(args[1]);
                Configuration.StreamSkeleton = bool.Parse(args[2]);
                Configuration.StreamVideo = bool.Parse(args[3]);
                Configuration.StreamDepth = bool.Parse(args[4]);
                Configuration.StreamDepthCalibration = bool.Parse(args[5]);
                Configuration.StreamIMU = bool.Parse(args[6]);
                Configuration.EncodingVideoLevel = int.Parse(args[7]);
                float videoWidth = float.Parse(args[8]);
                float videoHeigth = float.Parse(args[9]);
                if (videoWidth == 0.0 || videoHeigth == 0)
                    Configuration.VideoResolution = null;
                else
                    Configuration.VideoResolution = new Tuple<float, float>(videoWidth, videoHeigth);
                Configuration.RendezVousAddress = args[10];
                Configuration.RendezVousPort = int.Parse(args[11]);
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
            switch(message.Data.Item1)
            {
                case RendezVousPipeline.Command.Initialize:
                    UpdateConfigurationFromArgs(message.Data.Item2);
                    break;
                case RendezVousPipeline.Command.Run:
                    SetupKinect();
                    break;
                case RendezVousPipeline.Command.Stop:
                    StopKinect();
                    break;
                case RendezVousPipeline.Command.Close:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        StopPipeline();
                        Close();
                    }));
                    break;
                case RendezVousPipeline.Command.Restart:
                    if (UpdateConfigurationFromArgs(message.Data.Item2))
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            OnRestart();
                        }));
                    }
                    break;
                case RendezVousPipeline.Command.Status:
                    Pipeline?.CommandEmitter.Post((RendezVousPipeline.Command.Status, KinectStreams == null ? "Not Initialised": KinectStreams.StartTime.ToString()), Pipeline.Pipeline.GetCurrentTime());
                    break;
            }
        }

        private void SetupRendezVous()
        {
            //disable ui
            RendezVousGrid.IsEnabled = false;
            PipelineConfiguration.Diagnostics = (bool)Diagnostics.IsChecked ? RendezVousPipeline.DiagnosticsMode.Export : RendezVousPipeline.DiagnosticsMode.Store;
            PipelineConfiguration.CommandDelegate = CommandRecieved;
            Pipeline = new RendezVousPipeline(PipelineConfiguration, "KinectAzure", RendezVousServerIp);
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
            KinectStreams = new KinectAzureRemoteStreams(Pipeline.Pipeline, Configuration);
            Pipeline.AddProcess(KinectStreams.GenerateProcess());
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
    }
}
