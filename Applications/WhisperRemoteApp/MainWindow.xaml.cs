using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Rendezvous;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Net;
using Microsoft.Psi.Diagnostics;
using SAAC.PipelineServices;

namespace WhisperRemoteApp
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

        public RendezVousPipelineConfiguration PipelineConfigurationUI
        {
            get => PipelineConfiguration;
            set => SetProperty(ref PipelineConfiguration, value);
        }

        private string applicationName = "Whisper";
        public string ApplicationName
        {
            get => applicationName;
            set => SetProperty(ref applicationName, value);
        }
        public void DelegateMethodApplicationName(string name)
        {
            ApplicationName = name;
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
        private Pipeline WhipserPipeline; // to modify/remove

        public MainWindow()
        {
            DataContext = this;
            
            IPsList = new List<string>();
            foreach(var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                IPsList.Add(ip.ToString()); 
            }

            Pipeline = null;

            PipelineConfiguration.RendezVousHost = Properties.Settings.Default.IpToUse;
            RendezVousServerIp = Properties.Settings.Default.rendezVousServerIp;
            PipelineConfigurationUI.RendezVousPort = (int)(Properties.Settings.Default.rendezVousServerPort);
            ApplicationName = Properties.Settings.Default.ApplicationName;
            //ConfigurationUI.EncodingVideoLevel = (int)Properties.Settings.Default.encodingLevel;
            //ConfigurationUI.RendezVousApplicationName = Properties.Settings.Default.ApplicationName;
            //Configuration.VideoResolution = resolutionDictionary[(Resolution)Properties.Settings.Default.videoResolution];
            //Audio.IsChecked = Configuration.StreamAudio = Properties.Settings.Default.audio;
            //Skeleton.IsChecked = Configuration.StreamSkeleton = Properties.Settings.Default.skeleton;
            //RGB.IsChecked = Configuration.StreamVideo = Properties.Settings.Default.rgb;
            //Depth.IsChecked = Configuration.StreamDepth = Properties.Settings.Default.depth;
            //DepthCalibration.IsChecked = Configuration.StreamDepthCalibration = Properties.Settings.Default.depthCalibration;
            //IMU.IsChecked = Configuration.StreamIMU = Properties.Settings.Default.IMU;
            InitializeComponent();
            UpdateLayout();
        }

        private void RefreshUIFromConfiguration()
        {
            //ConfigurationUI.RendezVousPort = (int)Properties.Settings.Default.rendezVousServerPort;
            //Audio.IsChecked = Properties.Settings.Default.audio = Configuration.StreamAudio;
            //Skeleton.IsChecked = Properties.Settings.Default.skeleton = Configuration.StreamSkeleton;
            //RGB.IsChecked = Properties.Settings.Default.rgb = Configuration.StreamVideo;
            //Depth.IsChecked = Properties.Settings.Default.depth = Configuration.StreamDepth;
            //DepthCalibration.IsChecked = Properties.Settings.Default.depthCalibration = Configuration.StreamDepthCalibration;
            //IMU.IsChecked = Properties.Settings.Default.IMU = Configuration.StreamIMU;
            PipelineConfiguration.RendezVousHost = Properties.Settings.Default.IpToUse;
            RendezVousServerIp = Properties.Settings.Default.rendezVousServerIp;
            PipelineConfigurationUI.RendezVousPort = (int)(Properties.Settings.Default.rendezVousServerPort);
            ApplicationName = Properties.Settings.Default.ApplicationName;
            Properties.Settings.Default.Save();
            UpdateLayout();
        }

        private void RefreshConfigurationFromUI()
        {
            //Properties.Settings.Default.rendezVousServerPort = (uint)ConfigurationUI.RendezVousPort;

            //Configuration.StreamAudio = Properties.Settings.Default.audio = (bool)(Audio.IsChecked != null ? Audio.IsChecked : false);
            //Configuration.StreamSkeleton = Properties.Settings.Default.skeleton = (bool)(Skeleton.IsChecked != null ? Skeleton.IsChecked : false);
            //Configuration.StreamVideo = Properties.Settings.Default.rgb = (bool)(RGB.IsChecked != null ? RGB.IsChecked : false);
            //Configuration.StreamDepth = Properties.Settings.Default.depth = (bool)(Depth.IsChecked != null ? Depth.IsChecked : false);
            //Configuration.StreamDepthCalibration = Properties.Settings.Default.depthCalibration = (bool)(DepthCalibration.IsChecked != null ? DepthCalibration.IsChecked : false);
            //Configuration.StreamIMU = Properties.Settings.Default.IMU = (bool)(IMU.IsChecked != null ? IMU.IsChecked : false);
            Properties.Settings.Default.IpToUse = PipelineConfiguration.RendezVousHost;
            Properties.Settings.Default.rendezVousServerIp = RendezVousServerIp;
            Properties.Settings.Default.rendezVousServerPort = (uint)PipelineConfigurationUI.RendezVousPort;
            Properties.Settings.Default.ApplicationName = ApplicationName;
            Properties.Settings.Default.Save();
        }

        private bool UpdateConfigurationFromArgs(string[] args)
        {
            try
            {
                
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
            if (CommandSource != source)
                return; 
            
            var args = message.Data.Item2.Split([';']);

            if (args[0] != ApplicationName)
                return;

            switch (message.Data.Item1)
            {
                case RendezVousPipeline.Command.Initialize:
                    UpdateConfigurationFromArgs(args);
                    break;
                case RendezVousPipeline.Command.Run:
                    SetupWhisper();
                    break;
                case RendezVousPipeline.Command.Stop:
                    StopWhisper();
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
                    Pipeline?.CommandEmitter.Post((RendezVousPipeline.Command.Status, WhipserPipeline == null ? "Not Initialised": WhipserPipeline.StartTime.ToString()), Pipeline.Pipeline.GetCurrentTime());
                    break;
            }
        }

        private void SetupRendezVous()
        {
            //disable ui
            RendezVousGrid.IsEnabled = false;
            PipelineConfiguration.Diagnostics = (bool)Diagnostics.IsChecked ? RendezVousPipeline.DiagnosticsMode.Export : RendezVousPipeline.DiagnosticsMode.Store;
            PipelineConfiguration.CommandDelegate = CommandRecieved;
            Pipeline = new RendezVousPipeline(PipelineConfiguration, ApplicationName, RendezVousServerIp);
            State = "Waiting for server";
            Pipeline.Start();
            State = "Ready to start Kinect";
        }

        private void SetupWhisper()
        {
            if (Pipeline == null)
                SetupRendezVous();

            //disable ui
            DataFormular.IsEnabled = false;
            WhipserPipeline = Pipeline.CreateSubpipeline(); // to modify or remove
            //Pipeline.AddProcess(KinectStreams.GenerateProcess());
            WhipserPipeline.RunAsync();
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

        private void StopWhisper()
        {
            State = "Stopping Whisper";
            if (WhipserPipeline != null && Pipeline != null)
            {
                if(!Pipeline.RemoveProcess(ApplicationName))
                    Console.WriteLine("error remove rendezvous");
                try
                {
                    WhipserPipeline.Dispose();
                }
                catch(Exception ex) 
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        protected void OnRestart()
        {
            StopWhisper();
            RefreshUIFromConfiguration();
            SetupWhisper();
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
            StopWhisper();
        }

        private void BtnStopClick(object sender, RoutedEventArgs e)
        {
            StopWhisper();
            DataFormular.IsEnabled = true;
            RendezVousGrid.IsEnabled = true;
        }
    }
}
