using System.Net;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using SAAC.RendezVousPipelineServices;

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
            configuration.RendezVousHost = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
            configuration.DatasetPath = "D:/Stores/SAAC/";
            configuration.DatasetName = "SAAC.pds";
            //configuration.TopicsTypes.Add();
            //configuration.TypesSerializers.Add();
            //configuration.NotStoredTopics.Add();

            InitializeComponent();
        }

        public string GetDataset()
        {
            return configuration.DatasetPath + configuration.DatasetName;
        }

        public void RunPipeline()
        {
            server?.RunPipeline();
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
            server = new RendezVousPipeline(configuration, "SaaCPsiStudioApplication", null, (log) => { Status += $"{log}\n"; });
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
            return "";
        }

        public string GetAnnotation()
        {
            return "";
        }
    }
}
