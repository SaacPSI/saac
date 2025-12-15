using SAAC.PipelineServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SAAC.PipelineServices;

namespace ServerApplication
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public RendezVousPipelineConfiguration configuration;

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

        public MainWindow()
        {
            // Change the value with a config file
            DataContext = this;
            configuration = new RendezVousPipelineConfiguration();
            configuration.Debug = false;
            configuration.AutomaticPipelineRun = true;
            configuration.StoreMode = RendezVousPipeline.StoreMode.Dictionnary;
            configuration.SessionName = "RawData"; // Session name
            configuration.DatasetPath = @"C:\Users\dapi\Desktop\TestSession\Session\2702\20\";// C:\Users\dapi\Desktop\TestSession\2811\ C:\Users\Pampmousse\Desktop\Aurelien\TestSession\2911\
            configuration.DatasetName = "Dataset.pds"; // Dataset name
            configuration.RendezVousHost = "10.144.210.101"; // 10.144.210.101

            InitializeComponent();
        }
    }
}
