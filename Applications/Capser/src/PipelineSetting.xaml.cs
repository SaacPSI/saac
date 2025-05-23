using System.Net;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using SAAC.PipelineServices;
using System.Windows.Controls;
using Microsoft.Psi.Data;
using Microsoft.Psi;
using Microsoft.Psi.PsiStudio.PipelinePlugin;
using Microsoft.Psi.Imaging;
using Casper.Formats;
using Microsoft.Psi.Media;
using SharpDX;
using Microsoft.Psi.Audio;

namespace Casper
{
    /// <summary>
    /// Interaction logic for PipelineSetting.xaml
    /// </summary>
    public partial class PipelineSetting : Window, INotifyPropertyChanged, Microsoft.Psi.PsiStudio.PipelinePlugin.IPsiStudioPipeline
    {
        private RendezVousPipelineConfiguration configuration;

        // UI
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        // LOG
        private string status = "";
        public string Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }
        public void DelegateMethod(string status)
        {
            Status = status;
        }

        // RendezVousHost
        public string RendezVousHost
        {
            get => configuration.RendezVousHost;
            set => SetProperty(ref configuration.RendezVousHost, value);
        }
        public void DelegateMethodSynchServerIP(string ip)
        {
            configuration.RendezVousHost = ip;
        }

        // RendezVousPort
        public int RendezVousPort
        {
            get => configuration.RendezVousPort;
            set => SetProperty(ref configuration.RendezVousPort, value);
        }
        public void DelegateMethodSynchServerPort(int port)
        {
            configuration.RendezVousPort = port;
        }

        // ClockPort
        public int ClockPort
        {
            get => configuration.ClockPort;
            set => SetProperty(ref configuration.ClockPort, value);
        }
        public void DelegateMethodClockPort(int port)
        {
            configuration.ClockPort = port;
        }

        // DatasetPath
        public string DatasetPath
        {
            get => configuration.DatasetPath;
            set => SetProperty(ref configuration.DatasetPath, value);
        }
        public void DelegateMethodDatasetPath(string path)
        {
            configuration.DatasetPath = path;
        }

        // DatasetName
        public string DatasetName
        {
            get => configuration.DatasetName;
            set => SetProperty(ref configuration.DatasetName, value);
        }
        public void DelegateMethodDatasetName(string path)
        {
            configuration.DatasetName = path;
        }

        // SessionName
        public string SessionName
        {
            get => configuration.SessionName;
            set => SetProperty(ref configuration.SessionName, value);
        }
        public void DelegateMethodSessionName(string path)
        {
            configuration.SessionName = path;
        }

        private RendezVousPipeline? server;
        public Subpipeline videoP = null;
        private bool _isPsiPipelineStarted;
        private bool _isServerInitialize;
        private bool _isQuest1RawInitialize;
        private bool _isQuest2RawInitialize;
        private bool _isVideoInitialize;
        private bool _isPipelineInitialize;

        public PipelineSetting()
        {
            DataContext = this;

            configuration = new RendezVousPipelineConfiguration();
            configuration.AutomaticPipelineRun = true;
            configuration.Debug = true;
            configuration.StoreMode = RendezVousPipeline.StoreMode.Dictionnary;
            configuration.SessionName = "Unity";
            configuration.DatasetName = "Casper";
            configuration.DatasetPath = "D:/Stores/Casper/";
            configuration.DatasetName = "Dataset.pds";
            configuration.RendezVousHost = "localhost"; //Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();

            SpecifyTopicTypeForEachStream();

            InitializeComponent();
        }

        private void SpecifyTopicTypeForEachStream()
        {
            configuration.AddTopicFormatAndTransformer("Vitesse Tapis", typeof(float), new PsiFormatFloat());
            configuration.AddTopicFormatAndTransformer("D1-Poubelle", typeof(int), new PsiFormatInteger());
            configuration.AddTopicFormatAndTransformer("D2-Poubelle", typeof(int), new PsiFormatInteger());
            configuration.AddTopicFormatAndTransformer("Batteries", typeof(PsiBatterie), new PsiFormatBat());
            configuration.AddTopicFormatAndTransformer("Module status", typeof((int, string)), new PsiFormatIntString());
            configuration.AddTopicFormatAndTransformer("Area", typeof((int, bool, string)), new PsiFormatIntBoolString());
            configuration.AddTopicFormatAndTransformer("E1-extincteur", typeof((int, bool, string)), new PsiFormatIntBoolString());
            configuration.AddTopicFormatAndTransformer("E2-extincteur", typeof((int, bool, string)), new PsiFormatIntBoolString());
            configuration.AddTopicFormatAndTransformer("Gaze", typeof((int, bool, string)), new PsiFormatIntBoolString());
            configuration.AddTopicFormatAndTransformer("Grab", typeof((int, bool, string)), new PsiFormatIntBoolString());
            configuration.AddTopicFormatAndTransformer("Collision sol", typeof((int, bool, string)), new PsiFormatIntBoolString());
            configuration.AddTopicFormatAndTransformer("Collision tapis", typeof((int, bool, string)), new PsiFormatIntBoolString());
            configuration.AddTopicFormatAndTransformer("Collision batterie", typeof((int, bool, string)), new PsiFormatIntBoolString());
            configuration.AddTopicFormatAndTransformer("Commande TV", typeof(float), new PsiFormatFloat());
            configuration.AddTopicFormatAndTransformer("Levier", typeof(float), new PsiFormatFloat());
            configuration.AddTopicFormatAndTransformer("M1-Module Selectionne", typeof(string), new PsiFormatString());
            configuration.AddTopicFormatAndTransformer("M2-Module Selectionne", typeof(string), new PsiFormatString());
            configuration.AddTopicFormatAndTransformer("ERROR", typeof(bool), new PsiFormatBoolean());
            configuration.AddTopicFormatAndTransformer("M1-Validation", typeof(bool), new PsiFormatBoolean());
            configuration.AddTopicFormatAndTransformer("M2-Validation", typeof(bool), new PsiFormatBoolean());
            configuration.AddTopicFormatAndTransformer("M1-ModuleOut", typeof(bool), new PsiFormatBoolean());
            configuration.AddTopicFormatAndTransformer("M2-ModuleOut", typeof(bool), new PsiFormatBoolean());
            configuration.AddTopicFormatAndTransformer("Bouton urgence", typeof(bool), new PsiFormatBoolean());
            configuration.AddTopicFormatAndTransformer("Porte1 ouverture", typeof((bool, System.Numerics.Vector3)), new PsiFormatBoolVector3());
            configuration.AddTopicFormatAndTransformer("Porte2 ouverture", typeof((bool, System.Numerics.Vector3)), new PsiFormatBoolVector3());
            configuration.AddTopicFormatAndTransformer("1-Head", typeof(Tuple<System.Numerics.Vector3, System.Numerics.Vector3>), new PsiFormatTupleOfVector());
            configuration.AddTopicFormatAndTransformer("1-LeftWrist", typeof(Tuple<System.Numerics.Vector3, System.Numerics.Vector3>), new PsiFormatTupleOfVector());
            configuration.AddTopicFormatAndTransformer("1-RightWrist", typeof(Tuple<System.Numerics.Vector3, System.Numerics.Vector3>), new PsiFormatTupleOfVector());
            configuration.AddTopicFormatAndTransformer("2-Head", typeof(Tuple<System.Numerics.Vector3, System.Numerics.Vector3>), new PsiFormatTupleOfVector());
            configuration.AddTopicFormatAndTransformer("2-LeftWrist", typeof(Tuple<System.Numerics.Vector3, System.Numerics.Vector3>), new PsiFormatTupleOfVector());
            configuration.AddTopicFormatAndTransformer("2-RightWrist", typeof(Tuple<System.Numerics.Vector3, System.Numerics.Vector3>), new PsiFormatTupleOfVector());
            //configuration.AddTopicFormatAndTransformer("TV", typeof((int, int, int, string)), new PsiFormatTV());
        }

        public Dataset GetDataset()
        {
            server?.Dataset?.Save();
            return server?.Dataset;
        }
        public void RunPipeline(TimeInterval timeInterval)
        {
            server?.RunPipeline();
            server?.CommandEmitter.Post((RendezVousPipeline.Command.Run, "All"), server.Pipeline.GetCurrentTime());
        }

        public void StopPipeline()
        {
            server?.Stop();
            server.Dataset.Save();
        }

        private void BtnStopClick(object sender, RoutedEventArgs e)
        {
            if (videoP != null) videoP.Dispose();
            server?.Dataset?.Save();
            try
            {
                server?.Stop();
                server?.Dispose();
            }
            catch { }
            //server?.Pipeline.
            //server.Dispose();
        }

        private void BtnStartClick(object sender, RoutedEventArgs e)
        {
            status = "";
            server = new RendezVousPipeline(configuration, "Server", null, (log) => { Status += $"{log}\n"; });
            server.AddNewProcessEvent(CheckAllProcessAreInitialized);

            server.Start();
            server?.TriggerNewProcessEvent("PsiPipeline");
            StartUnityVideoRecording(server, configuration);
        }

        private void CheckAllProcessAreInitialized(object sender, (string, Dictionary<string, Dictionary<string, ConnectorInfo>>) e)
        {
            RendezVousPipeline server = sender as RendezVousPipeline;


            switch (e.Item1)
            {
                case "PsiPipeline":
                    if (!_isPsiPipelineStarted) _isPsiPipelineStarted = true;
                    else _isPsiPipelineStarted = false;
                    break;
                case "UnityServer":
                    if (!_isServerInitialize && _isPsiPipelineStarted) _isServerInitialize = true;
                    else if (_isServerInitialize && !_isPsiPipelineStarted) _isServerInitialize = false;
                    break;
                case "Quest1":
                    if (!_isQuest1RawInitialize && _isPsiPipelineStarted) _isQuest1RawInitialize = true;
                    else if (_isQuest1RawInitialize && !_isPsiPipelineStarted) _isQuest1RawInitialize = false;
                    break;
                case "Quest2":
                    if (!_isQuest2RawInitialize && _isPsiPipelineStarted) _isQuest2RawInitialize = true;
                    else if (_isQuest2RawInitialize && !_isPsiPipelineStarted) _isQuest2RawInitialize = false;
                    break;
                case "VideoServer":
                    if (!_isVideoInitialize) _isVideoInitialize = true;
                    else _isVideoInitialize = false;
                    break;
                
                case "PipelineProcessInitialized":
                    if (!_isPipelineInitialize && _isPsiPipelineStarted) _isPipelineInitialize = true;
                    else if (_isPipelineInitialize && !_isPsiPipelineStarted) _isPipelineInitialize = false;
                    break;
                
               /* case "EndSession":
                    foreach (var writer in saac_Expe2.streamsWriters)
                    {
                        if (!saac_Expe2.writersDisposed && saac_Expe2._isServerInitialize) saac_Expe2.CloseAndDisposeWriter(writer);
                    }
                    saac_Expe2.writersDisposed = true;
                    Console.WriteLine("Writer are closed and Session is ended");
                    break;*/
                default:
                    break;
            }
        }

        public void StartUnityVideoRecording(RendezVousPipeline server, RendezVousPipelineConfiguration configuration)
        {
            videoP = new Subpipeline(server.Pipeline, "Video");
            Session? session = server.CreateOrGetSessionFromMode("Video");

            WindowCaptureConfiguration cfg = new WindowCaptureConfiguration() { Interval = TimeSpan.FromMilliseconds(50) };
            //cfg.WindowHandle = Process.GetProcessesByName(Configuration.AppName)[0].MainWindowHandle;
            WindowCapture capture = new WindowCapture(videoP, cfg);
            var encodedCapture = capture.Out.EncodeJpeg(50, DeliveryPolicy.LatestMessage);
            server.CreateConnectorAndStore($"VideoServer", "Video", session, videoP, typeof(Shared<EncodedImage>), encodedCapture);
            videoP.RunAsync();
            server.Log($"SubPipeline Video started.");
            server.TriggerNewProcessEvent("VideoServer");
        }
        private void BtnBrowseNameClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                DatasetPath = openFileDialog.FileName.Substring(0, openFileDialog.FileName.IndexOf(openFileDialog.SafeFileName));
                DatasetName = openFileDialog.SafeFileName;
            }
        }

        public string GetLayout()
        {
            return null;
        }

        public string GetAnnotation()
        {
            return null;
        }

        private void Log_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            TextBox? log = sender as TextBox;
            if (log == null) 
                return;
            log.CaretIndex = log.Text.Length;
            log.ScrollToEnd();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            server?.Pipeline.Dispose();
        }

        public DateTime GetStartTime()
        {
            return server == null ? DateTime.MinValue : server.Pipeline.StartTime;
        }

        public PipelineReplaybleMode GetReplaybleMode()
        {
            return PipelineReplaybleMode.Not;
        }
    }
}
