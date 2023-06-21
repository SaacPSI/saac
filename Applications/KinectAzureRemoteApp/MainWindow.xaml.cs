using Microsoft.Psi;
using Microsoft.Psi.AzureKinect;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Configuration;
using System.Collections.Specialized;
using Microsoft.Psi.Interop.Transport;
using System.Net;
using Stride.Core.Extensions;
using Microsoft.Psi.Diagnostics;

namespace KinectAzureRemoteApp
{
    //public class Resolution
    //{
    //    public enum EResolution { Native, R1920_1080, R1280_800, R800_600 };

    //    public EResolution Id { get; set; } = EResolution.Native;
    //    public 
    //    public Resolution() { }

    //}

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

        private string synchServerIp = "localhost";
        public string SynchServerIp
        {
            get => synchServerIp;
            set => SetProperty(ref synchServerIp, value);
        }
        public void DelegateMethodSynchServerIP(string ip)
        {
            SynchServerIp = ip;
        }

        private uint synchServerPort = 13331;
        public uint SynchServerPort
        {
            get => synchServerPort;
            set => SetProperty(ref synchServerPort, value);
        }
        public void DelegateMethodSynchServerPort(uint port)
        {
            SynchServerPort = port;
        }

        private uint kinectIndex = 0;
        public uint KinectIndex
        {
            get => kinectIndex;
            set => SetProperty(ref kinectIndex, value);
        }
        public void DelegateMethodKinect(uint index)
        {
            KinectIndex = index;
        }

        private string kinectApplicationName = "KinectStreaming";
        public string KinectApplicationName
        {
            get => kinectApplicationName;
            set => SetProperty(ref kinectApplicationName, value);
        }
        public void DelegateMethodKinect(string kinectApplicationName)
        {
            KinectApplicationName = kinectApplicationName;
        }

        private uint remotePort = 11411;
        public uint RemotePort
        {
            get => remotePort;
            set => SetProperty(ref remotePort, value);
        }
        public void DelegateMethodRemote(uint port)
        {
            RemotePort = port;
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
        public enum Resolution{ Native, R1920_1080, R960_540, R640_360 };
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
        //private Resolution depthResolution = Resolution.Native;
        //public Resolution DepthResolution
        //{
        //    get => depthResolution;
        //    set => SetProperty(ref depthResolution, value);
        //}
        //public void DelegateMethodDepthResolution(Resolution val)
        //{
        //    DepthResolution = val;
        //}

        private RendezvousServer? server;
        private Pipeline pipeline;

        public MainWindow()
        {
            DataContext = this;
            resolutionDictionary = new Dictionary<Resolution, Tuple<float, float>>
            {
                 { Resolution.R1920_1080, new Tuple<float, float>(1920.0f, 1080.0f) }
                ,{ Resolution.R960_540, new Tuple<float, float>(960.0f, 540.0f) }
                ,{ Resolution.R640_360, new Tuple<float, float>(640.0f, 360.0f) }
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

            InitializeComponent();
            SyncServerIsActive.IsChecked = Properties.Settings.Default.synchServerIsActive; 
            SynchServerIp = Properties.Settings.Default.synchServerIp;
            SynchServerPort = Properties.Settings.Default.synchServerPort;
            RemotePort = Properties.Settings.Default.remotePort;
            Audio.IsChecked = Properties.Settings.Default.audio;
            Skeleton.IsChecked = Properties.Settings.Default.skeleton;
            RGB.IsChecked = Properties.Settings.Default.rgb;
            Depth.IsChecked = Properties.Settings.Default.depth;
            DepthCalibration.IsChecked = Properties.Settings.Default.depthCalibration;
            IMU.IsChecked = Properties.Settings.Default.IMU;
            kinectApplicationName = Properties.Settings.Default.ApplicationName;
            iPSelected = Properties.Settings.Default.IpToUse;
        }

        private void PipelineSetup()
        {
            if(Diagnostics.IsChecked == false)
            {
                pipeline = Pipeline.Create(KinectApplicationName, enableDiagnostics: false);
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
                pipeline = Pipeline.Create(KinectApplicationName, enableDiagnostics: true, diagnosticsConfiguration: config);
                var store = PsiStore.Create(pipeline, "Diagnostics", @"./");
                pipeline.Diagnostics.Write("Diagnostics", store);
            }

            DataFormular.IsEnabled = false;
            if (SyncServerIsActive.IsChecked == true)
            {
                var client = new RendezvousClient(SynchServerIp, (int)synchServerPort);
                State = "Waiting for synch server";
                client.Rendezvous.ProcessAdded += (_, p) =>
                {
                    foreach (var endpoint in p.Endpoints)
                    {
                        if (endpoint is Rendezvous.RemoteClockExporterEndpoint remoteClockEndpoint)
                        {
                            var remoteClockImporter = remoteClockEndpoint.ToRemoteClockImporter(pipeline);
                            break;
                        }
                    } 
                };
                client.Start();
                client.Connected.WaitOne();
                //client.Stop();
            }

            /*** KINECT SENSORS ***/
            int portCount = (int)RemotePort+1;
            TransportKind type = UDP.IsChecked == true ? TransportKind.Udp : TransportKind.Tcp;

            // Only need Skeleton for the moment.
            AzureKinectSensorConfiguration configKinect = new AzureKinectSensorConfiguration();
            configKinect.DeviceIndex = (int)KinectIndex;
            if (Skeleton.IsChecked == true)
                configKinect.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
            AzureKinectSensor sensor = new AzureKinectSensor(pipeline, configKinect);

            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();  
            if (Audio.IsChecked == true)
            {
                AudioCaptureConfiguration configuration = new AudioCaptureConfiguration();
                AudioCapture audioCapture = new AudioCapture(pipeline, configuration);
                RemoteExporter soundExporter = new RemoteExporter(pipeline, (int)RemotePort + portCount++, type);
                soundExporter.Exporter.Write(audioCapture.Out, "Kinect_" + KinectIndex.ToString() + "_Audio");
                exporters.Add(soundExporter.ToRendezvousEndpoint(iPSelected));
            }
            if (Skeleton.IsChecked == true)
            {
                RemoteExporter skeletonExporter = new RemoteExporter(pipeline, (int)RemotePort + portCount++, type);
                skeletonExporter.Exporter.Write(sensor.Bodies, "Kinect_" + KinectIndex.ToString() + "_Bodies");
                exporters.Add(skeletonExporter.ToRendezvousEndpoint(iPSelected));
            }
            if (RGB.IsChecked == true)
            {
                RemoteExporter imageExporter = new RemoteExporter(pipeline, (int)RemotePort + portCount++, type);
                if (colorResolution != Resolution.Native)
                {
                    Tuple<float, float> res = resolutionDictionary[colorResolution];
                    imageExporter.Exporter.Write(sensor.ColorImage.Resize(res.Item1, res.Item2).EncodeJpeg(), "Kinect_" + KinectIndex.ToString() + "_RGB");
                }
                else
                    imageExporter.Exporter.Write(sensor.ColorImage.EncodeJpeg(), "Kinect_" + KinectIndex.ToString() + "_RGB");
                exporters.Add(imageExporter.ToRendezvousEndpoint(iPSelected));
            }
            if (Depth.IsChecked == true)
            {
                RemoteExporter depthExporter = new RemoteExporter(pipeline, (int)RemotePort + portCount++, type);

                //if (depthResolution != Resolution.Native)
                //{
                //    Tuple<float, float> res = resolutionDictionary[colorResolution];
                //    depthExporter.Exporter.Write(sensor.DepthImage.EncodePng()., "Kinect_" + KinectIndex.ToString() + "_Depth");
                //}
                //else
                depthExporter.Exporter.Write(sensor.DepthImage.EncodePng(), "Kinect_" + KinectIndex.ToString() + "_Depth"); 
                exporters.Add(depthExporter.ToRendezvousEndpoint(iPSelected));
            }
            if(DepthCalibration.IsChecked == true)
            {
                RemoteExporter depthCalibrationExporter = new RemoteExporter(pipeline, (int)RemotePort + portCount++, type);
                depthCalibrationExporter.Exporter.Write(sensor.DepthDeviceCalibrationInfo, "Kinect_" + KinectIndex.ToString() + "_Calibration");
                exporters.Add(depthCalibrationExporter.ToRendezvousEndpoint(iPSelected));
            }
            if (IMU.IsChecked == true)
            {
                RemoteExporter imuExporter = new RemoteExporter(pipeline, (int)RemotePort + portCount++, type);
                imuExporter.Exporter.Write(sensor.Imu, "Kinect_" + KinectIndex.ToString() + "_IMU");
                exporters.Add(imuExporter.ToRendezvousEndpoint(iPSelected));
            }

            server = new RendezvousServer((int)RemotePort);
            server.Rendezvous.TryAddProcess(
                    new Rendezvous.Process(
                        KinectApplicationName,
                        exporters,
                        "Version2.0"));
            server.Start();
            pipeline.RunAsync(ReplayDescriptor.ReplayAllRealTime);
            State = "Running";

        }

        private void StopPipeline()
        {
            // Stop correctly the pipeline.
            State = "Stopping";
            pipeline.Dispose();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if(server != null)
                server.Stop();
            StopPipeline();
            base.OnClosing(e);
            Properties.Settings.Default.synchServerIsActive = (bool)(SyncServerIsActive.IsChecked != null ? SyncServerIsActive.IsChecked : false);
            Properties.Settings.Default.synchServerIp = synchServerIp;
            Properties.Settings.Default.synchServerPort = synchServerPort;
            Properties.Settings.Default.remotePort = remotePort;
            Properties.Settings.Default.IpToUse = iPSelected;
            Properties.Settings.Default.ApplicationName = kinectApplicationName;
            Properties.Settings.Default.audio = (bool)(Audio.IsChecked != null ? Audio.IsChecked : false); 
            Properties.Settings.Default.skeleton = (bool)(Skeleton.IsChecked != null ? Skeleton.IsChecked : false);
            Properties.Settings.Default.rgb = (bool)(RGB.IsChecked != null ? RGB.IsChecked : false);
            Properties.Settings.Default.depth = (bool)(Depth.IsChecked != null ? Depth.IsChecked : false);
            Properties.Settings.Default.depthCalibration = (bool)(DepthCalibration.IsChecked != null ? DepthCalibration.IsChecked : false);
            Properties.Settings.Default.IMU = (bool)(IMU.IsChecked != null ? IMU.IsChecked : false);
            Properties.Settings.Default.Save();
        }

        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            StopPipeline();
            Close();
        }

        private void BtnStartClick(object sender, RoutedEventArgs e)
        {
            State = "Initializing";
            PipelineSetup();
        }
    }
}
