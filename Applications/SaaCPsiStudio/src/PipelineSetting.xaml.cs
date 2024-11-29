using System.Net;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using SAAC.RendezVousPipelineServices;
using System.Windows.Controls;
using Microsoft.Psi.Data;
using SAAC.KinectAzureRemoteServices;
using SAAC.RemoteConnectors;
using Microsoft.Psi;
using static SAAC.RendezVousPipelineServices.RendezVousPipeline;
using System.Windows.Media.Animation;

namespace SaaCPsiStudio
{
    /// <summary>
    /// Interaction logic for PipelineSetting.xaml
    /// </summary>
    public partial class PipelineSetting : Window, INotifyPropertyChanged, Microsoft.Psi.PsiStudio.IPsiStudioPipeline
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

        public PipelineSetting()
        {
            DataContext = this;

            configuration = new RendezVousPipelineConfiguration();
            configuration.RendezVousHost = "localhost"; //Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
            configuration.DatasetPath = "D:/Stores/SAAC/";
            configuration.DatasetName = "SAAC.pds";
            configuration.AutomaticPipelineRun = true;
            configuration.StoreMode = RendezVousPipeline.StoreMode.Dictionnary;

            //configuration.TopicsTypes.Add();
            //configuration.TypesSerializers.Add();
            //configuration.NotStoredTopics.Add();

            InitializeComponent();
        }

        public Dataset GetDataset()
        {
            server?.Dataset?.Save();
            return server?.Dataset;
        }
        public void RunPipeline()
        {
            server?.RunPipeline();
            server?.CommandEmitter.Post((Command.Run, "KinectStreaming"), server.Pipeline.GetCurrentTime());
        }

        public void StopPipeline()
        {
            server?.Stop();
        }

        private void BtnStopClick(object sender, RoutedEventArgs e)
        {
            server?.Stop();
        }

        private void BtnStartClick(object sender, RoutedEventArgs e)
        {
            status = "";
            server = new RendezVousPipeline(configuration, "Server", null, (log) => { Status += $"{log}\n"; });
            KinectAzureRemoteConnectorConfiguration configuration1 = new KinectAzureRemoteConnectorConfiguration();
            configuration1.RendezVousApplicationName = "KinectStreaming";
            configuration1.Debug = true;
            KinectAzureRemoteComponent service = new KinectAzureRemoteComponent(server, configuration1);
            server.Start();
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
            server?.Stop();
        }
    }
}
