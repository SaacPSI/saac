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
using Casper.Formats;

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

        public PipelineSetting()
        {
            DataContext = this;

            configuration = new RendezVousPipelineConfiguration();
            configuration.RendezVousHost = "localhost"; //Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
            configuration.DatasetPath = "D:/Stores/Casper/";
            configuration.DatasetName = "Casper.pds";
            configuration.AutomaticPipelineRun = true;
            configuration.StoreMode = RendezVousPipeline.StoreMode.Dictionnary;

            configuration.AddTopicFormatAndTransformer("Vitesse Tapis", typeof(float), new PsiFormatFloat());
            configuration.AddTopicFormatAndTransformer("D1-Poubelle", typeof(int), new PsiFormatInteger());
            configuration.TopicsTypes.Add("D2-Poubelle", typeof(int));
            configuration.AddTopicFormatAndTransformer("Batteries", typeof(PsiBatterie), new PsiFormatBat());
            configuration.AddTopicFormatAndTransformer("Module status", typeof((int, string)), new PsiFormatIntString());
            configuration.AddTopicFormatAndTransformer("Area", typeof((int, bool, string)), new PsiFormatIntBoolString());
            configuration.TopicsTypes.Add("E1-extincteur", typeof((int, bool, string)));
            configuration.TopicsTypes.Add("E2-extincteur", typeof((int, bool, string)));
            configuration.TopicsTypes.Add("Gaze", typeof((int, bool, string)));
            configuration.TopicsTypes.Add("Grab", typeof((int, bool, string)));
            configuration.TopicsTypes.Add("Collision sol", typeof((int, bool, string)));
            configuration.TopicsTypes.Add("Collision tapis", typeof((int, bool, string)));
            configuration.TopicsTypes.Add("Collision batterie", typeof((int, bool, string)));
            configuration.TopicsTypes.Add("Commande TV", typeof(float));
            configuration.TopicsTypes.Add("Levier", typeof(float));
            configuration.AddTopicFormatAndTransformer("TV", typeof((int, int, int, string)), new PsiFormatTV());
            configuration.TopicsTypes.Add("M1-Module Selectionne", typeof(string));
            configuration.TopicsTypes.Add("M2-Module Selectionne", typeof(string));
            configuration.TopicsTypes.Add("ERROR", typeof(bool));
            configuration.TopicsTypes.Add("M2-Validation", typeof(bool));
            configuration.TopicsTypes.Add("M1-Validation", typeof(bool));
            configuration.TopicsTypes.Add("M1-ModuleOut", typeof(bool));
            configuration.TopicsTypes.Add("M2-ModuleOut", typeof(bool));
            configuration.TopicsTypes.Add("Bouton urgence", typeof(bool));
            configuration.AddTopicFormatAndTransformer("Porte1 ouverture", typeof((bool, System.Numerics.Vector3)), new PsiFormatBoolVector3());
            configuration.TopicsTypes.Add("Porte2 ouverture", typeof((bool, System.Numerics.Vector3)));
            configuration.AddTopicFormatAndTransformer("Right", typeof(Tuple<System.Numerics.Vector3, System.Numerics.Vector3>), new PsiFormatTupleOfVector());
            configuration.TopicsTypes.Add("Left", typeof(Tuple<System.Numerics.Vector3, System.Numerics.Vector3>));
            configuration.TopicsTypes.Add("Head", typeof(Tuple<System.Numerics.Vector3, System.Numerics.Vector3>));

            InitializeComponent();
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
        }

        private void BtnStopClick(object sender, RoutedEventArgs e)
        {
            server?.Stop();
        }

        private void BtnStartClick(object sender, RoutedEventArgs e)
        {
            status = "";
            server = new RendezVousPipeline(configuration, "Server", null, (log) => { Status += $"{log}\n"; });
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
