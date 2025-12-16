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
using SAAC;
using Microsoft.Psi;
using System.Windows.Media.Animation;

namespace ServerApplication
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public RendezVousPipelineConfiguration configuration;
        private RendezVousPipeline server;
        private Pipeline pipeline;
        public List<RendezVousPipeline.StoreMode> storeModeList { get;  }
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

        public RendezVousPipelineConfiguration Configuration
        {
            get => configuration;
            set => SetProperty(ref configuration, value);
        }
        private string commandSource = "Server";
        public string CommandSource
        {
            get => commandSource;
            set => SetProperty(ref commandSource, value);
        }
        
        public string LocalDatasetPath
        {
            get => configuration.DatasetPath;
            set => SetProperty(ref configuration.DatasetPath, value);
        }

        public string LocalDatasetName
        {
            get => configuration.DatasetName;
            set => SetProperty(ref configuration.DatasetName, value);
        }

        // SessionName
        public string LocalSessionName
        {
            get => configuration.SessionName;
            set => SetProperty(ref configuration.SessionName, value);
        }

        // Log Tab
        private string log = "Not Initialised\n";
        public string Log
        {
            get => log;
            set => SetProperty(ref log, value);
        }


        // variables
        private enum SetupState
        {
            NotInitialised,
            PipelineInitialised,
            AudioInitialised,
            WhisperInitialised
        };
        private SetupState setupState;
        private LogStatus internalLog;

        public MainWindow()
        {
            internalLog = (log) =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    Log += $"{log}\n";
                }));
            };
            // Change the value with a config file
            storeModeList = new List<RendezVousPipeline.StoreMode>(Enum.GetValues(typeof(RendezVousPipeline.StoreMode)).Cast<RendezVousPipeline.StoreMode>());

            setupState = SetupState.NotInitialised;
            server = null;
            configuration = new RendezVousPipelineConfiguration();
            
            //configuration.Debug = false;
            //configuration.AutomaticPipelineRun = true;
            //configuration.StoreMode = RendezVousPipeline.StoreMode.Dictionnary;
            //configuration.SessionName = "RawData"; // Session name
            //configuration.DatasetPath = @"C:\Users\dapi\Desktop\TestSession\Session\2702\20\";// C:\Users\dapi\Desktop\TestSession\2811\ C:\Users\Pampmousse\Desktop\Aurelien\TestSession\2911\
            //configuration.DatasetName = "Dataset.pds"; // Dataset name
            //configuration.RendezVousHost = "10.144.210.101"; // 10.144.210.101

            
            LoadConfig();
            InitializeComponent();
            DataContext = this;
            UpdateLayout();
            RefreshUIFromConfiguration();
            UpdateLayout();
        }

        private void LoadConfig()
        {
            Configuration.RendezVousHost = Properties.Settings.Default.RendezVousHost;
            Configuration.RendezVousPort = Properties.Settings.Default.RendezVousPort;
            Configuration.ClockPort = Properties.Settings.Default.ClockPort;
            LocalDatasetPath = Properties.Settings.Default.DatasetPath;
            LocalSessionName = Properties.Settings.Default.SessionName;
            LocalDatasetName = Properties.Settings.Default.DatasetName;
            Configuration.Debug = Properties.Settings.Default.Debug;
            Configuration.AutomaticPipelineRun = Properties.Settings.Default.AutomaticPipelineRun;
        }
        private void RefreshUIFromConfiguration()
        {
            //Configuration Tab
            LoadConfig();
            StoreModeComboBox.SelectedIndex = Properties.Settings.Default.StoreMode;
            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = false;
        }

        private void RefreshConfigurationFromUI()
        {
            Properties.Settings.Default.RendezVousHost = Configuration.RendezVousHost;
            Properties.Settings.Default.RendezVousPort = Configuration.RendezVousPort;
            Properties.Settings.Default.ClockPort = Configuration.ClockPort;
            Properties.Settings.Default.DatasetPath = LocalDatasetPath;
            Properties.Settings.Default.SessionName = LocalSessionName;
            Properties.Settings.Default.DatasetName = LocalDatasetName;
            Properties.Settings.Default.Debug = Configuration.Debug;
            Properties.Settings.Default.AutomaticPipelineRun = Configuration.AutomaticPipelineRun;
            Properties.Settings.Default.StoreMode = (int) StoreModeComboBox.SelectedIndex;

            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = false;
        }

        private void StoreModeSelected(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            Configuration.StoreMode = (RendezVousPipeline.StoreMode)StoreModeComboBox.SelectedIndex;
        }
        private void BtnLoadConfiguration(object sender, RoutedEventArgs e)
        {
            RefreshUIFromConfiguration();
            AddLog("Configuration Loaded");
            e.Handled = true;
        }

        private void BtnSaveConfiguration(object sender, RoutedEventArgs e)
        {
            RefreshConfigurationFromUI();
            AddLog("Configuration Saved");
            e.Handled = true;
        }
        private void BtnSetupConfiguration(object sender, RoutedEventArgs e)
        {
            Tab.SelectedItem = ConfigurationTab;
        }

        private void BtnStartClick(object sender, RoutedEventArgs e)
        {
            SetupPipeline();
        }

        private void SetupPipeline()
        {
            if (setupState >= SetupState.PipelineInitialised)
                return;

            configuration.Diagnostics = DatasetPipeline.DiagnosticsMode.Off;
            configuration.AutomaticPipelineRun = true;
            configuration.CommandDelegate = CommandReceived;
            configuration.Debug = false;
            configuration.RecordIncomingProcess = false;
            configuration.DatasetPath = LocalDatasetPath;
            configuration.DatasetName = LocalDatasetName;
            configuration.SessionName = LocalSessionName;
            server = new RendezVousPipeline(configuration, "Server", null, internalLog);
            pipeline = server.Pipeline;
            AddLog("Server initialisation started");
            server.Start();
            AddLog("Server started");
            setupState = SetupState.PipelineInitialised;
        }

        private void BtnStopClick(object sender, RoutedEventArgs e)
        {
            server?.Dataset?.Save();
            server?.Stop();
            server?.TriggerNewProcessEvent("EndSession");
        }

        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            Close();
            e.Handled = true;
        }
        //Method loop status connected devices

        private void CommandReceived(string source, Message<(RendezVousPipeline.Command, string)> message)
        {
            if (CommandSource != source)
                return;

            var args = message.Data.Item2.Split(';');

            if (args[0] != "Server")
                return;

            switch (message.Data.Item1)
            {
                case RendezVousPipeline.Command.Run:
                    Start();
                    break;
                case RendezVousPipeline.Command.Close:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        Close();
                    }));
                    break;
                case RendezVousPipeline.Command.Status:
                    server?.SendCommand(RendezVousPipeline.Command.Status, source, server == null ? "Not Initialised" : server.Pipeline.StartTime.ToString());
                    break;
            }
        }
        private void Start()
        {
            //SetupPipeline();
            
            /*if (setupState == SetupState.WhisperInitialised)
            {
                Run();
                AddLog(State = "Started");
            }*/
        }
        private void Run()
        {
            if (server is null)
            {
                pipeline?.RunAsync();
            }
            else
                server.Start();
        }
        private void BtnBrowseNameClick(object sender, RoutedEventArgs e)
        {
            UiGenerator.FolderPicker openFileDialog = new UiGenerator.FolderPicker();
            if (openFileDialog.ShowDialog() == true)
            {
                DatasetPathTextBox.Text = openFileDialog.ResultName;
                LocalDatasetPath = openFileDialog.ResultName;
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            }
        }
        private void Log_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            TextBox? log = sender as TextBox;
            if (log == null)
                return;
            log.CaretIndex = log.Text.Length;
            log.ScrollToEnd();
        }

        private void AddLog(string logMessage)
        {
            Log += $"{logMessage}\n";
        }
    }
}
