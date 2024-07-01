using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Rendezvous;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Net;
using Microsoft.Psi.Diagnostics;
using SAAC.RemoteConnectors;

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

        private KinectAzureRemoteStreamsConfiguration Configuration = new KinectAzureRemoteStreamsConfiguration(); 

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
        private string iPSelected = "localhost";
        public string IPSelected
        {
            get => iPSelected;
            set => SetProperty(ref iPSelected, value);
        }
        public void DelegateMethodColorResolution(string val)
        {
            IPSelected = val;
        }

        private RendezvousClient? Client;
        private Pipeline? Pipeline;
        private KinectAzureRemoteStreams? KinectStreams;
        private Pipeline? KinectStreamsPipline;

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
            Client = null;

            InitializeComponent();
            RendezVousServerIp = Properties.Settings.Default.rendezVousServerIp;
            ConfigurationUI.RendezVousPort = (int)(Properties.Settings.Default.rendezVousServerPort);

            Audio.IsChecked = Configuration.StreamAudio = Properties.Settings.Default.audio;
            Skeleton.IsChecked = Configuration.StreamSkeleton = Properties.Settings.Default.skeleton;
            RGB.IsChecked = Configuration.StreamVideo = Properties.Settings.Default.rgb;
            Depth.IsChecked = Configuration.StreamDepth = Properties.Settings.Default.depth;
            DepthCalibration.IsChecked = Configuration.StreamDepthCalibration = Properties.Settings.Default.depthCalibration;
            IMU.IsChecked = Configuration.StreamIMU = Properties.Settings.Default.IMU;
            ConfigurationUI.RendezVousApplicationName = Properties.Settings.Default.ApplicationName;
            iPSelected = Configuration.RendezVousAddress = Properties.Settings.Default.IpToUse;
            ConfigurationUI.EncodingVideoLevel = (int)Properties.Settings.Default.encodingLevel;
            Configuration.VideoResolution = resolutionDictionary[(Resolution)Properties.Settings.Default.videoResolution];
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
            iPSelected = Properties.Settings.Default.IpToUse = Configuration.RendezVousAddress;
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

            Configuration.RendezVousAddress = Properties.Settings.Default.IpToUse = iPSelected;
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

        private void SetupRendezVous()
        {
            //disable ui
            RendezVousGrid.IsEnabled = false;
            Client = new RendezvousClient(Configuration.RendezVousAddress, Configuration.RendezVousPort);
            if (Diagnostics.IsChecked == false)
            {
                Pipeline = Pipeline.Create(Configuration.RendezVousApplicationName, enableDiagnostics: false);
            }
            else
            {
                var config = new DiagnosticsConfiguration()
                {
                    TrackMessageSize = true,
                    AveragingTimeSpan = TimeSpan.FromSeconds(2),
                    SamplingInterval = TimeSpan.FromSeconds(10),
                    IncludeStoppedPipelines = false,
                    IncludeStoppedPipelineElements = false,
                };
                Pipeline = Pipeline.Create(Configuration.RendezVousApplicationName, enableDiagnostics: true, diagnosticsConfiguration: config);
                RemoteExporter diagnosticsExporter = new RemoteExporter(Pipeline, Configuration.RendezVousPort * 2, Configuration.ConnectionType);
                diagnosticsExporter.Exporter.Write(Pipeline.Diagnostics, $"{Configuration.RendezVousApplicationName}_Diagnostics");
                Rendezvous.Process diagProcess = new Rendezvous.Process($"{Configuration.RendezVousApplicationName}_Diagnostics", new List<Rendezvous.Endpoint> { diagnosticsExporter.ToRendezvousEndpoint(Configuration.RendezVousAddress) });
                Client.Rendezvous.TryAddProcess(diagProcess);
            }

            // SynchClock
            // Might make sure that the clock of the server... check by name ?
            Client.Rendezvous.ProcessAdded += (_, p) =>
            {
                foreach (var endpoint in p.Endpoints)
                {
                    if (endpoint is Rendezvous.RemoteClockExporterEndpoint remoteClockEndpoint)
                    {
                        var remoteClockImporter = remoteClockEndpoint.ToRemoteClockImporter(Pipeline);
                        break;
                    }
                }
            };

            // Configuration Change
            Client.Rendezvous.ProcessAdded += (_, p) =>
            {
                if (p.Name != $"{Configuration.RendezVousApplicationName}_Configuration")
                    return;
                foreach (var endpoint in p.Endpoints)
                {
                    if (endpoint is Rendezvous.RemoteExporterEndpoint remoteExporterEndpoint)
                    {
                        Subpipeline configurationSubpipeline = new Subpipeline(Pipeline);
                        var remoteImporter = remoteExporterEndpoint.ToRemoteImporter(configurationSubpipeline);
                        foreach (var stream in remoteExporterEndpoint.Streams)
                        {
                            if (stream.StreamName.Contains("Configuration"))
                            {
                                if (remoteImporter.Connected.WaitOne() == false)
                                {
                                    Log += $"{Configuration.RendezVousApplicationName} failed to connect stream Configuration";
                                    return;
                                }
                                Log += $"{Configuration.RendezVousApplicationName} failed to connect stream Configuration";
                                remoteImporter.Importer.OpenStream<KinectAzureRemoteStreamsConfiguration?>("Configuration").Out.Do((c, e) =>
                                {
                                    //KinectConfigurationEvent.Invoke(this, c);
                                    Application.Current.Dispatcher.Invoke(new Action(() => 
                                    {
                                        OnRestart(c);
                                    }));
                                });
                                configurationSubpipeline.RunAsync();
                                return;
                            }
                        }
                    }
                };
            };

            State = "Waiting for server";
            Client.Start();
            Pipeline.RunAsync(ReplayDescriptor.ReplayAllRealTime);
            State = "Ready to start Kinect";
        }

        private void SetupKinect()
        {
            if (Pipeline == null || Client == null)
                SetupRendezVous();

            //disable ui
            DataFormular.IsEnabled = false;
            KinectStreamsPipline = Pipeline.CreateSynchedPipeline(Pipeline);
            KinectStreams = new KinectAzureRemoteStreams(KinectStreamsPipline, Configuration);
            Client.Rendezvous.TryAddProcess(KinectStreams.GenerateProcess());
            KinectStreamsPipline.RunAsync();
            State = "Running";
        }

        private void StopPipeline()
        {
            // Stop correctly the everything.
            State = "Stopping";
            if (Client != null)
            {
                Client.Stop();
                Client.Rendezvous.TryRemoveProcess(Configuration.RendezVousApplicationName);
                if(Diagnostics.IsChecked == true)
                    Client.Rendezvous.TryRemoveProcess($"{Configuration.RendezVousApplicationName}_Diagnostics");
                Client.Dispose();
            }
            if(Pipeline != null)
                Pipeline.Dispose();
        }

        private void StopKinect()
        {
            State = "Stopping Kinect";
            if (KinectStreams != null && KinectStreams.Sensor != null)
            {
                if(!Client.Rendezvous.TryRemoveProcess(Configuration.RendezVousApplicationName))
                    Console.WriteLine("error remove rendezvous");
                try
                {
                    KinectStreamsPipline.Dispose();
                }
                catch(Exception ex) 
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        protected void OnRestart(KinectAzureRemoteStreamsConfiguration? e)
        {
            StopKinect();
            if (e != null)
            {
                Configuration = e;
                RefreshUIFromConfiguration();
                SetupKinect();
            }
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
