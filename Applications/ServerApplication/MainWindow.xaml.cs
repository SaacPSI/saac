using Microsoft.Psi;
using Newtonsoft.Json;
using SAAC;
using SAAC.PipelineServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Numerics;
using ServerApplication.Helpers;
using System.Reflection;

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
        private int _rowIndex = 0;

        public enum ConnectedAppStatus
        {
            Waiting,
            Running,
            Stop,
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

        private bool isDebug = false;
        public bool IsDebug
        {
            get => isDebug;
            set => SetProperty(ref isDebug, value);
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

        private uint annotationPort = 8080;
        public uint AnnotationPort
        {
            get => annotationPort;
            set => SetProperty(ref annotationPort, value);
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
        private Microsoft.Psi.Interop.Transport.WebSocketsManager? websocketManager;

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
            
            configuration.Debug = true;
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
            UiGenerator.SetTextBoxPreviewTextChecker<uint>(AnnotationPortTextBox, uint.TryParse);
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
            isDebug = Configuration.Debug = Properties.Settings.Default.Debug;
            Configuration.AutomaticPipelineRun = Properties.Settings.Default.AutomaticPipelineRun;
            
            // Annotation Tab
            IsAnnotationEnabled = Properties.Settings.Default.IsAnnotationEnabled;
            AnnotationSchemaDirectory = Properties.Settings.Default.AnnotationSchemasPath;
            AnnotationWebPage = Properties.Settings.Default.AnnotationHtmlPage;
            AnnotationPort = Properties.Settings.Default.AnnotationPort;
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
            AnnotationPort = Properties.Settings.Default.AnnotationPort;
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
            Properties.Settings.Default.Debug = Configuration.Debug = isDebug;
            Properties.Settings.Default.AutomaticPipelineRun = Configuration.AutomaticPipelineRun;
            Properties.Settings.Default.StoreMode = (int) StoreModeComboBox.SelectedIndex;
            
            // Annotation Tab
            Properties.Settings.Default.IsAnnotationEnabled = IsAnnotationEnabled;
            Properties.Settings.Default.AnnotationSchemasPath = AnnotationSchemaDirectory;
            Properties.Settings.Default.AnnotationHtmlPage = AnnotationWebPage;
            Properties.Settings.Default.AnnotationPort = AnnotationPort;
            
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
            AllDevicesStackPanel.IsEnabled = true;
        }

        private void SetupPipeline()
        {
            if (setupState >= SetupState.PipelineInitialised)
                return;

            configuration.Diagnostics = DatasetPipeline.DiagnosticsMode.Off;
            configuration.AutomaticPipelineRun = true;
            configuration.CommandDelegate = CommandReceived;
            configuration.Debug = true;
            configuration.RecordIncomingProcess = true;
            configuration.CommandPort = 11610;
            configuration.DatasetPath = LocalDatasetPath;
            configuration.DatasetName = LocalDatasetName;
            configuration.SessionName = LocalSessionName;
            server = new RendezVousPipeline(configuration, "Server", null, internalLog);
            //server.AddNewProcessEvent(SpawnProcessRow);
            pipeline = server.Pipeline;
            AddLog("Server initialisation started");
            
            // Setup annotations if enabled
            SetupWebSocketsAndAnnotations();
            
            server.Start();
            AddLog("Server started");
            setupState = SetupState.PipelineInitialised;
        }
        private void SetupWebSocketsAndAnnotations()
        {
            // Create list of addresses for WebSocket
            List<string> addresses = new List<string>() { $"http://{Configuration.RendezVousHost}:{AnnotationPort}/ws/"};

            if (!IsAnnotationEnabled)
            {
                // Instantiate the HTTPAnnotationsComponent
                websocketManager = new Microsoft.Psi.Interop.Transport.WebSocketsManager(true, addresses, false);
                pipeline.PipelineRun += (s, e) =>
                {
                    websocketManager?.Start((dt) => { });
                };
                pipeline.ComponentCompleted += (s, e) =>
                {
                    websocketManager?.Dispose();
                };
            }
            else 
            {
                if (!System.IO.Directory.Exists(AnnotationSchemaDirectory))
                {
                    AddLog($"Warning: Annotation schema directory does not exist: {AnnotationSchemaDirectory}");
                    return;
                }

                if (!System.IO.File.Exists(AnnotationWebPage))
                {
                    AddLog($"Warning: Annotation web page does not exist: {AnnotationWebPage}");
                    return;
                }

                // Add HTTP
                addresses.Add($"http://{Configuration.RendezVousHost}:{AnnotationPort}/");

                // Instantiate the HTTPAnnotationsComponent
                websocketManager = new SAAC.AnnotationsComponents.HTTPAnnotationsComponent(server, addresses, AnnotationSchemaDirectory, AnnotationWebPage);
                AddLog("Annotations component initialized successfully");
            }
            websocketManager.OnNewWebSocketConnectedHandler += OnWebsocketConnection;
        }

        private void OnWebsocketConnection(object sender, (string, string, Uri) e)
        {
            if (e.Item2 == "annotation" || !configuration.TopicsTypes.ContainsKey(e.Item2))
            {
                return;
            }
            Pipeline pipeline = server.GetOrCreateSubpipeline($"{e.Item1}-{e.Item2}");
            var source = websocketManager.ConnectWebsocketSource<string>(pipeline, configuration.TypesSerializers[configuration.TopicsTypes[e.Item2]].GetFormat(), e.Item1, e.Item2, false);
            server.CreateConnectorAndStore(e.Item2, e.Item1, server.CurrentSession, pipeline, typeof(string), source);
            pipeline.RunAsync();
        }

        /*private void SpawnProcessRow(object sender, (string, Dictionary<string, Dictionary<string, ConnectorInfo>>) e)
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
        }*/

        private string GetName(object argument)
        {
            string suffix = "-Command";
            string stringArgument = (string)argument;
            string name = stringArgument.EndsWith(suffix)
                ? stringArgument.Substring(0, stringArgument.Length - suffix.Length)
                : stringArgument;

            return name;
        }
       

        private void BtnStopClick(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            Stop();
            Close();
            e.Handled = true;
        }

        private void Stop()
        {
            // Stop annotations component
            if (websocketManager != null)
            {
                try
                {
                    // Assuming the component has a Stop or Dispose method
                    AddLog("Stopping annotations component");
                    websocketManager = null;
                }
                catch (Exception ex)
                {
                    AddLog($"Error stopping annotations: {ex.Message}");
                }
            }

            server?.Dataset?.Save();
            server?.Dispose();
            StopStatusMonitoring();
        }

        //Method loop status connected devices
        private void CommandReceived(string source, Message<(RendezVousPipeline.Command, string)> message)
        {
            var args = message.Data.Item2.Split(';');

            if (args[0] != "Server")
                return;
            string name = GetName(source);

            switch (message.Data.Item1)
            {
                //case RendezVousPipeline.Command.Initialize:
                //    if (!connectedApps.ContainsKey(name))
                //    {
                //        connectedApps[name] = new ConnectedApp
                //        {
                //            Name = name,
                //        };
                //        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                //        {
                //            SpawnEllipseTextButtonsRow(name);
                //        }));
                //    }
                //    break;
                //case RendezVousPipeline.Command.Run:
                //    connectedApps[name].Status = ConnectedAppStatus.Running;
                //    UpdateDotColor(connectedApps[name]);
                //    break;
                //case RendezVousPipeline.Command.Stop:
                //    connectedApps[name].Status = ConnectedAppStatus.Stop;
                //    UpdateDotColor(connectedApps[name]);
                //    connectedApps.Remove(name);
                //    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                //    {
                //        RemoveDeviceRow(name);
                //    }));
                //    break;
                case RendezVousPipeline.Command.Status:
                    CheckStatus(name, args, message.OriginatingTime);
                    break;
            }
        }

        private void CheckStatus(string name, string[] args, DateTime time)
        {
            if (args.Length < 2)
                return;
            switch(args[1])
            {                 
                case "Running":
                    if (connectedApps.ContainsKey(name))
                    {
                        connectedApps[name].Status = ConnectedAppStatus.Running;
                    }
                    break;
                case "Waiting":
                    if (!connectedApps.ContainsKey(name))
                    {
                        connectedApps[name] = new ConnectedApp
                        {
                            Name = name,
                        };
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            SpawnEllipseTextButtonsRow(name);
                        }));
                    }
                    connectedApps[name].Status = ConnectedAppStatus.Waiting;
                    break;
                case "Stopping":
                    if (connectedApps.ContainsKey(name))
                    {
                        connectedApps.Remove(name);
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            RemoveDeviceRow(name);
                        }));
                    }
                    break;
                case "Error":
                    if (connectedApps.ContainsKey(name))
                    {
                        connectedApps[name].Status = ConnectedAppStatus.Error;
                    }
                    break;
            }
            if (!connectedApps.ContainsKey(name))
                return;
            connectedApps[name].LastStatusReceivedTime = time;
            UpdateDotColor(connectedApps[name]);
        }


        #region Status Monitoring Connected Applications
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

        public void StopStatusMonitoring()
        {
            statusTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            statusTimer?.Dispose();
        }
        #endregion

        #region UI Connected Application Managers

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
        private void CkbDebug(object sender, RoutedEventArgs e)
        {
            if (DebugCheckbox.IsChecked == true) configuration.Debug = true;
            else configuration.Debug = false;
            e.Handled = true;
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
                foreach (var dr in _rowsByDeviceName.Values)
                {
                    if (dr.RowIndex > removedRowIndex)
                    {
                        dr.RowIndex--;
                    }
                }
            });
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
        #endregion

        #region Buttons
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
        private void BtnLoadAssembly_Click(object sender, RoutedEventArgs e)
        {
            LoadAssemblyFromFile();
            e.Handled = true;
        }
        private void BtnTopicClick(object sender, RoutedEventArgs e)
        {
            LoadConfigurationFromJsonFile();
            e.Handled = true;
        }
        
        private void AnnotationPortTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
        }

        private void StartAllDevices(object sender, RoutedEventArgs e)
        {
            server.SendCommand(RendezVousPipeline.Command.Run, "*", "");
            e.Handled = true;
        }

        private void StopAllDevices(object sender, RoutedEventArgs e)
        {
            server.SendCommand(RendezVousPipeline.Command.Close, "*", "");
            e.Handled = true;
        }
        #endregion

        #region Browser


        public void LoadAssemblyFromFile()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Dynamic Link Library (*.dll)|*.dll|All files (*.*)|*.*",
                Title = "Charger une DLL contenant les classes de données"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    Assembly customAssembly = ConfigurationLoader.LoadAssemblyTypes(openFileDialog.FileName).FirstOrDefault()?.Assembly;
                    AddLog($"✓ DLL chargée : {openFileDialog.FileName}");

                    // Optionnel : exporter un template JSON
                    //string templatePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(openFileDialog.FileName), "config_template.json");
                    //List<Type> types = ConfigurationLoader.LoadAssemblyTypes(openFileDialog.FileName);
                    //ConfigurationLoader.ExportConfigurationTemplate(templatePath, types);
                    //AddLog($"✓ Template JSON généré : {templatePath}");
                    
                    // Optionnel : lister les types dans la DLL
                    //ListTypesFromAssembly("TestDll");
                }
                catch (Exception ex)
                {
                    AddLog($"✗ Erreur lors du chargement de la DLL : {ex.Message}");
                }
            }
        }
        public void ListTypesFromAssembly(string assemblyName)
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);

            if (asm == null)
            {
                AddLog($"❌ Assembly '{assemblyName}' non chargé");
                return;
            }

            AddLog($"📦 Types dans {assemblyName}:");

            foreach (var type in asm.GetTypes())
            {
                AddLog($" - {type.FullName}");
            }
        }

        public void LoadConfigurationFromJsonFile()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Charger une configuration JSON"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    LoadJSONAndSpecifyTopic(openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    AddLog("Erreur dans le chargement du fichier");
                }
            }
        }
        #endregion

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

        #region Load JSON Config
        public sealed class TopicFormatDefinition
        {
            public string Topic { get; set; } = "";
            public string Type { get; set; } = "";
            public string ClassFormat { get; set; } = "";
            public string StreamToStore { get; set; } = "";
        }

        public List<TopicFormatDefinition> LoadJSONAndSpecifyTopic(string jsonFilePath) // remplace par le vrai type si possible
        {
            if (!File.Exists(jsonFilePath))
                throw new FileNotFoundException(jsonFilePath);

            var json = File.ReadAllText(jsonFilePath);

            var items = JsonConvert.DeserializeObject<List<TopicFormatDefinition>>(json)?? new List<TopicFormatDefinition>();

            foreach (var item in items)
            {
                var messageType = ResolveType(item.Type);
                AddLog($"Topic {item.Topic} type is {messageType.ToString()}");

                var formatType = ResolvePsiFormatType(item.ClassFormat);
                var formatInstance = (IPsiFormat)CreateInstance(formatType);
                AddLog($"Topic {item.Topic} format is {formatInstance.ToString()}");
                configuration.AddTopicFormatAndTransformer(item.Topic, messageType, formatInstance);
                configuration.StreamToStore.Add(item.Topic, item.StreamToStore);
            }

            return items;
        }

        private static Type ResolveType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
                return type;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(typeName);
                if (type != null)
                    return type;
            }

            throw new TypeLoadException($"Type introuvable : {typeName}");
        }

        private static Type ResolvePsiFormatType(string formatClassName)
        {
            if (string.IsNullOrWhiteSpace(formatClassName))
                throw new ArgumentException("Le nom de la classe de format ne peut pas être vide", nameof(formatClassName));

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(formatClassName);
                if (type != null && typeof(IPsiFormat).IsAssignableFrom(type))
                    return type;

                // Chercher par nom simple si le nom complet n'est pas trouvé
                type = asm.GetTypes().FirstOrDefault(t =>
                    t.Name == formatClassName &&
                    typeof(IPsiFormat).IsAssignableFrom(t));

                if (type != null)
                    return type;
            }

            throw new TypeLoadException($"Type de format PsiFormat introuvable : {formatClassName}");
        }

        private object CreateInstance(Type type)
        {
            if (type.GetConstructor(Type.EmptyTypes) == null)
                AddLog($"Le type {type.Name} n'a pas de constructeur sans paramètre.");

            return Activator.CreateInstance(type);
        }
        #endregion
    }
}
