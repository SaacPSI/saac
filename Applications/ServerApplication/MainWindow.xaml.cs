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
using System.Threading;
using System.Xml.Linq;
using static ServerApplication.MainWindow;
using System.Net.NetworkInformation;

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
        private Timer statusTimer;
        private bool statusCheckRunning;

        public enum ConnectedAppStatus
        {
            Waiting,
            Running,
            Error
        }
        public class ConnectedApp
        {
            public string Name { get; set; } = "";
            public ConnectedAppStatus Status { get; set; } = ConnectedAppStatus.Waiting;
            public DateTime LastStatusReceivedTime { get; set; } = DateTime.UtcNow;
            public Ellipse StatusDot { get; set; } = null;
        }
        public class DeviceRow
        {
            public int RowIndex { get; set; }

            public RowDefinition RowDefinition { get; set; } = null;

            public Ellipse Dot { get; set; } = null;
            public TextBlock Text { get; set; } = null;
            public Button BtnStart { get; set; } = null;
            public Button BtnStop { get; set; } = null;
        }

        private List<Tuple<string, bool>> connectedProcesses = new List<Tuple<string, bool>>();
        private Dictionary<string, ConnectedApp> connectedApps = new Dictionary<string, ConnectedApp>();
        private readonly Dictionary<string, DeviceRow> _rowsByDeviceName = new Dictionary<string, DeviceRow>();


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

        // Annotation Tab
        private bool isAnnotationEnabled = false;
        public bool IsAnnotationEnabled
        {
            get => isAnnotationEnabled;
            set => SetProperty(ref isAnnotationEnabled, value);
        }

        private string annotationSchemaDirectory = "";
        public string AnnotationSchemaDirectory
        {
            get => annotationSchemaDirectory;
            set => SetProperty(ref annotationSchemaDirectory, value);
        }

        private string annotationWebPage = "";
        public string AnnotationWebPage
        {
            get => annotationWebPage;
            set => SetProperty(ref annotationWebPage, value);
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
        private SAAC.AnnotationsComponents.HTTPAnnotationsComponent? annotationsComponent;

        public MainWindow()
        {
            internalLog = (log) =>
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        Log += $"{log}\n";
                    }));
                }
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
            SetupAnnotationTab();
            RefreshUIFromConfiguration();
            UpdateLayout();
        }

        private void SetupAnnotationTab()
        {
            // Initialize annotation tab state
            UpdateAnnotationTab();
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
            
            // Annotation Tab
            IsAnnotationEnabled = Properties.Settings.Default.IsAnnotationEnabled;
            AnnotationSchemaDirectory = Properties.Settings.Default.AnnotationSchemasPath;
            AnnotationWebPage = Properties.Settings.Default.AnnotationHtmlPage;
        }
        private void RefreshUIFromConfiguration()
        {
            //Configuration Tab
            LoadConfig();
            StoreModeComboBox.SelectedIndex = Properties.Settings.Default.StoreMode;
            
            // Annotation Tab
            IsAnnotationEnabled = Properties.Settings.Default.IsAnnotationEnabled;
            AnnotationSchemaDirectory = Properties.Settings.Default.AnnotationSchemasPath;
            AnnotationWebPage = Properties.Settings.Default.AnnotationHtmlPage;
            UpdateAnnotationTab();
            
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
            
            // Annotation Tab
            Properties.Settings.Default.IsAnnotationEnabled = IsAnnotationEnabled;
            Properties.Settings.Default.AnnotationSchemasPath = AnnotationSchemaDirectory;
            Properties.Settings.Default.AnnotationHtmlPage = AnnotationWebPage;
            
            Properties.Settings.Default.Save();

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

            configuration.Diagnostics = DatasetPipeline.DiagnosticsMode.Store;
            configuration.AutomaticPipelineRun = true;
            configuration.CommandDelegate = CommandReceived;
            configuration.Debug = false;
            configuration.RecordIncomingProcess = true;
            configuration.DatasetPath = LocalDatasetPath;
            configuration.DatasetName = LocalDatasetName;
            configuration.SessionName = LocalSessionName;
            server = new RendezVousPipeline(configuration, "Server", null, internalLog);
            server.AddNewProcessEvent(SpawnProcessRow);
            pipeline = server.Pipeline;
            AddLog("Server initialisation started");
            
            // Setup annotations if enabled
            SetupAnnotations();
            
            server.Start();
            AddLog("Server started");
            setupState = SetupState.PipelineInitialised;
        }
        private void SetupAnnotations()
        {
            if (!IsAnnotationEnabled)
            {
                AddLog("Annotations disabled");
                return;
            }

            if (server == null)
            {
                AddLog("Cannot setup annotations: server not initialized");
                return;
            }

            if (!System.IO.Directory.Exists(AnnotationSchemaDirectory))
            {
                AddLog($"Warning: Annotation schema directory does not exist: {AnnotationSchemaDirectory}");
            }

            if (!System.IO.File.Exists(AnnotationWebPage))
            {
                AddLog($"Warning: Annotation web page does not exist: {AnnotationWebPage}");
            }

            try
            {
                // Create list of addresses for WebSocket and HTTP
                List<string> addresses = new List<string>()
                {
                    $"http://{Configuration.RendezVousHost}:8080/ws/",
                    $"http://{Configuration.RendezVousHost}:8080/"
                };

                // Instantiate the HTTPAnnotationsComponent
                annotationsComponent = new SAAC.AnnotationsComponents.HTTPAnnotationsComponent(server, addresses, AnnotationSchemaDirectory, AnnotationWebPage);

                AddLog("Annotations component initialized successfully");
            }
            catch (Exception ex)
            {
                AddLog($"Failed to setup annotations: {ex.Message}");
            }
        }

        private void SpawnProcessRow(object sender, (string, Dictionary<string, Dictionary<string, ConnectorInfo>>) e)
        {
            if (e.Item1 == "EndSession") return;

            string name = GetName(e.Item1);
            if (!connectedApps.ContainsKey(name))
            {
                connectedApps[name] = new ConnectedApp
                {
                    Name = name,
                };
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    SpawnEllipseTextButtonsRow(e.Item1);
                }));
            }
            else
            {
                connectedApps.Remove(name);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    RemoveDeviceRow(name);
                }));
            }
            
            if (connectedApps.Count >= 1 && !statusCheckRunning) StartStatusMonitoring();
        }

        private string GetName(object argument)
        {
            string suffix = "-Command";
            string stringArgument = (string)argument;
            string name = stringArgument.EndsWith(suffix)
                ? stringArgument.Substring(0, stringArgument.Length - suffix.Length)
                : stringArgument;

            return name;
        }
        public void RemoveDeviceRow(string name)
        {
            if (!_rowsByDeviceName.TryGetValue(name, out var row))
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                int removedRowIndex = row.RowIndex;

                // 1) Retirer les contrôles du Grid
                ConnectedDevicesGrid.Children.Remove(row.Dot);
                ConnectedDevicesGrid.Children.Remove(row.Text);
                ConnectedDevicesGrid.Children.Remove(row.BtnStart);
                ConnectedDevicesGrid.Children.Remove(row.BtnStop);

                // 2) Retirer la RowDefinition
                ConnectedDevicesGrid.RowDefinitions.Remove(row.RowDefinition);

                // 3) Retirer du dictionnaire
                _rowsByDeviceName.Remove(name);

                // 4) Remonter les éléments qui étaient en dessous
                foreach (UIElement child in ConnectedDevicesGrid.Children)
                {
                    int r = Grid.GetRow(child);
                    if (r > removedRowIndex)
                        Grid.SetRow(child, r - 1);
                }

                // 5) Mettre à jour les RowIndex stockés
                foreach (var kvp in _rowsByDeviceName)
                {
                    var deviceName = kvp.Key;
                    var dr = kvp.Value;

                    if (dr.RowIndex > removedRowIndex)
                    {
                        _rowsByDeviceName[deviceName] = new DeviceRow
                        {
                            RowIndex = dr.RowIndex - 1,
                            RowDefinition = dr.RowDefinition, // attention: les RowDefs ont “glissé” mais l’objet reste valide
                            Dot = dr.Dot,
                            Text = dr.Text,
                            BtnStart = dr.BtnStart,
                            BtnStop = dr.BtnStop
                        };
                    }
                }
            });
        }

        private void BtnStopClick(object sender, RoutedEventArgs e)
        {
            // Stop annotations component
            if (annotationsComponent != null)
            {
                try
                {
                    // Assuming the component has a Stop or Dispose method
                    AddLog("Stopping annotations component");
                    annotationsComponent = null;
                }
                catch (Exception ex)
                {
                    AddLog($"Error stopping annotations: {ex.Message}");
                }
            }

            server?.Dataset?.Save();
            server?.Stop();
            server?.TriggerNewProcessEvent("EndSession");
            StopStatusMonitoring();
        }

        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            Close();
            e.Handled = true;
        }
        //Method loop status connected devices

        private void CommandReceived(string source, Message<(RendezVousPipeline.Command, string)> message)
        {
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
        private int _rowIndex = 0;

        // Called by a button click
        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            server.TriggerNewProcessEvent("WhisperStreaming-Command");
            //SpawnEllipseTextButtonsRow();
        }
        public void StartStatusMonitoring()
        {
            statusTimer = new Timer(callback: StatusTimerCallback, state: null, dueTime: TimeSpan.Zero, period: TimeSpan.FromSeconds(1));
        }

        private void StatusTimerCallback(object? state)
        {
            // 🔒 Anti-réentrance
            if (statusCheckRunning)
                return;
            statusCheckRunning = true;

            try
            {
                foreach (var app in connectedApps.Values)
                {
                    // Demande de statut
                    server.SendCommand(
                        RendezVousPipeline.Command.Status,
                        app.Name,
                        "");

                    // Timeout (ex: 3s)
                    if (DateTime.UtcNow - app.LastStatusReceivedTime > TimeSpan.FromSeconds(3))
                    {
                        app.Status = ConnectedAppStatus.Error;
                        UpdateDotColor(app);
                    }
                }
            }
            catch (Exception ex)
            {
                // log
            }
            finally
            {
                statusCheckRunning = false;
            }
        }

        private void UpdateDotColor(ConnectedApp app)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                app.StatusDot.Fill = app.Status switch
                {
                    ConnectedAppStatus.Running => Brushes.Green,
                    ConnectedAppStatus.Waiting => Brushes.Orange,
                    ConnectedAppStatus.Error => Brushes.Red
                };
            }));
        }

        public void StopStatusMonitoring()
        {
            statusTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            statusTimer?.Dispose();
        }


        private void SpawnEllipseTextButtonsRow(object argument)
        {
            string name = GetName(argument);

            if (_rowsByDeviceName.ContainsKey(name))
                return;

            UiGenerator.AddRowsDefinitionToGrid(ConnectedDevicesGrid, GridLength.Auto, 1);
            int rowIndex = ConnectedDevicesGrid.RowDefinitions.Count - 1;
            var rowDef = ConnectedDevicesGrid.RowDefinitions[rowIndex];
            
            // Ellipse (left)
            var dot = UiGenerator.GenerateEllipse(size: 14, fill: Brushes.Orange, stroke: Brushes.Orange, strokeThickness: 1, name: $"Dot_{_rowIndex}");
            dot.Margin = new Thickness(0, 0, 10, 0);

            // TextBox (middle)
            var tb = UiGenerator.GenerateText(name, double.NaN, name: $"Text_{_rowIndex}");
            tb.Loaded += (s, e) =>
            {
                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                tb.Width = tb.DesiredSize.Width + 10;
            };

            // Button 1 (right)
            var btnOk = UiGenerator.GenerateButton("Start", (s, e) =>
            {
                server.SendCommand(RendezVousPipeline.Command.Run, name, "");
            }, name: $"BtnOk_{_rowIndex}");
            btnOk.Margin = new Thickness(0, 0, 15, 0);

            // Button 2 (right) - remove this row
            var btnRemove = UiGenerator.GenerateButton("Stop", (s, e) =>
            {
                server.SendCommand(RendezVousPipeline.Command.Close, name, "");
            }, name: $"BtnRemove_{_rowIndex}");
            btnRemove.Margin = new Thickness(0, 0, 15, 0);
            UiGenerator.SetElementInGrid(ConnectedDevicesGrid, dot, 0, ConnectedDevicesGrid.RowDefinitions.Count - 1);
            UiGenerator.SetElementInGrid(ConnectedDevicesGrid, tb, 1, ConnectedDevicesGrid.RowDefinitions.Count - 1);
            UiGenerator.SetElementInGrid(ConnectedDevicesGrid, btnOk, 2, ConnectedDevicesGrid.RowDefinitions.Count - 1);
            UiGenerator.SetElementInGrid(ConnectedDevicesGrid, btnRemove, 3, ConnectedDevicesGrid.RowDefinitions.Count - 1);

            connectedApps[name].StatusDot = dot;
            connectedApps[name].Status = ConnectedAppStatus.Waiting;
            connectedApps[name].LastStatusReceivedTime = DateTime.UtcNow;

            _rowsByDeviceName[name] = new DeviceRow
            {
                RowIndex = rowIndex,
                RowDefinition = rowDef,
                Dot = dot,
                Text = tb,
                BtnStart = btnOk,
                BtnStop = btnRemove
            };
        }

        private void UpdateVisualisationProcess(object sender, (string, Dictionary<string, Dictionary<string, ConnectorInfo>>) e)
        {
            Thread otherThread = new Thread(ChangeEllipseColorThreadMethod);
            otherThread.Start(e.Item1); ;
        }

        private void ChangeEllipseColorThreadMethod(object arguments)
        {
            string stringArgument = (string)arguments;
            bool isConnected = connectedProcess[stringArgument];

            Application.Current.Dispatcher.Invoke(() =>
            {
                Ellipse ellipse = FindName(stringArgument) as Ellipse;
                if (ellipse != null)
                {
                    if (isConnected) ellipse.Fill = Brushes.Green;
                    else if (!isConnected) ellipse.Fill = Brushes.DarkRed;
                }
            });
        }
        private Dictionary<string, bool> connectedProcess = new Dictionary<string, bool>();
        
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

        private void UpdateAnnotationTab()
        {
            foreach (UIElement annotationUIElement in AnnotationGrid.Children)
            {
                if (annotationUIElement is GroupBox groupBox)
                {
                    groupBox.IsEnabled = isAnnotationEnabled;
                }
            }
        }

        private void CkbActivateAnnotation(object sender, RoutedEventArgs e)
        {
            UpdateAnnotationTab();
            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            e.Handled = true;
        }

        private void BtnBrowseSchemaDirectoryClick(object sender, RoutedEventArgs e)
        {
            UiGenerator.FolderPicker openFolderDialog = new UiGenerator.FolderPicker();
            if (openFolderDialog.ShowDialog() == true)
            {
                AnnotationSchemaDirectory = openFolderDialog.ResultName;
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            }
            e.Handled = true;
        }

        private void BtnBrowseWebPageClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "HTML files (*.html;*.htm)|*.html;*.htm|All files (*.*)|*.*";
            openFileDialog.DefaultExt = ".html";
            if (openFileDialog.ShowDialog() == true)
            {
                AnnotationWebPage = openFileDialog.FileName;
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            }
            e.Handled = true;
        }
    }
}
