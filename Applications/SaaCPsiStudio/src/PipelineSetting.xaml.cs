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
            configuration.DatasetPath = "D:/Stores/Unity/";
            configuration.DatasetName = "Unity.pds";

            InitializeComponent();
        }

        public string GetDataset()
        {
            return configuration.DatasetPath + configuration.DatasetName;
        }

        public void RunPipeline()
        {
            RunPipeline();
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
            server = new RendezVousPipeline(configuration,(log) => { Status += $"{log}\n"; });
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
            return "{\"LayoutVersion\":5.0,\"Layout\":{\"$id\":\"1\",\"Panels\":[{\"$id\":\"2\",\"$type\":\"Microsoft.Psi.Visualization.VisualizationPanels.InstantVisualizationContainer,Microsoft.Psi.Visualization.Windows\",\"Panels\":[{\"$id\":\"3\",\"$type\":\"Microsoft.Psi.Visualization.VisualizationPanels.XYVisualizationPanel,Microsoft.Psi.Visualization.Windows\",\"AxisComputeMode\":0,\"XAxis\":{\"$id\":\"4\",\"maximum\":640.0,\"minimum\":0.0},\"YAxis\":{\"$id\":\"5\",\"maximum\":480.0,\"minimum\":0.0},\"ViewportPadding\":\"1063.16666666667,0,1063.16666666667,0\",\"CompatiblePanelTypes\":[2],\"DefaultCursorEpsilonNegMs\":500,\"DefaultCursorEpsilonPosMs\":0,\"RelativeWidth\":100,\"Name\":\"2DPanel\",\"Visible\":true,\"Height\":400.0,\"BackgroundColor\":\"#FF252526\",\"Width\":2659.0,\"VisualizationObjects\":[{\"$id\":\"6\",\"$type\":\"Microsoft.Psi.Visualization.VisualizationObjects.ImageVisualizationObject,Microsoft.Psi.Visualization.Windows\",\"HorizontalFlip\":false,\"StreamBinding\":{\"$id\":\"7\",\"PartitionName\":\"Webcam\",\"SourceStreamName\":\"Image\",\"StreamName\":\"Image\",\"VisualizerStreamAdapterArguments\":[],\"VisualizerSummarizerArguments\":[],\"VisualizerStreamAdapterTypeName\":\"Microsoft.Psi.Visualization.Adapters.EncodedImageToImageAdapter,Microsoft.Psi.Visualization.Windows,Version=0.18.72.1,Culture=neutral,PublicKeyToken=null\"},\"Name\":\"Image\",\"Visible\":true,\"CursorEpsilonPosMs\":0,\"CursorEpsilonNegMs\":500}]}],\"Name\":\"InstantVisualizationContainer\",\"Visible\":true,\"Height\":400.0,\"CompatiblePanelTypes\":[],\"BackgroundColor\":\"#FF252526\",\"Width\":400.0,\"VisualizationObjects\":[]}]}}";
        }
    }
}
