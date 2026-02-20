// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.PsiStudio.PipelinePlugin;
using Newtonsoft.Json;
using SAAC;
using SAAC.PipelineServices;

namespace ServerApplication
{
    /// <summary>
    /// Main window for the Server Application that manages connected devices and pipeline configuration.
    /// </summary>
    public partial class MainWindow : Window, Microsoft.Psi.PsiStudio.PipelinePlugin.IPsiStudioPipeline, INotifyPropertyChanged
    {
        private readonly Dictionary<string, DeviceRow> rowsByDeviceName = new Dictionary<string, DeviceRow>();

        private RendezVousPipelineConfiguration configuration;
        private RendezVousPipeline server;
        private Pipeline pipeline;
        private Timer statusTimer;
        private bool statusCheckRunning;
        private int rowIndex = 0;
        private List<Tuple<string, bool>> connectedProcesses = new List<Tuple<string, bool>>();
        private Dictionary<string, ConnectedApp> connectedApps = new Dictionary<string, ConnectedApp>();
        private string commandSource = "Server";
        private bool isDebug = false;
        private string externalConfigurationDirectory = string.Empty;
        private bool isAnnotationEnabled = false;
        private string annotationSchemaDirectory = string.Empty;
        private string annotationWebPage = string.Empty;
        private uint annotationPort = 8080;
        private string log = "Not Initialised\n";
        private SetupState setupState;
        private LogStatus internalLog;
        private Microsoft.Psi.Interop.Transport.WebSocketsManager? websocketManager;

        /// <summary>
        /// Represents the connection status of a remote application.
        /// </summary>
        public enum ConnectedAppStatus
        {
            /// <summary>Waiting for connection or initialization.</summary>
            Waiting,

            /// <summary>Application is running.</summary>
            Running,

            /// <summary>Application has stopped.</summary>
            Stop,

            /// <summary>Application encountered an error.</summary>
            Error,
        }

        /// <summary>
        /// Represents a connected application with its status information.
        /// </summary>
        public class ConnectedApp
        {
            /// <summary>
            /// Gets or sets the application name.
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the connection status.
            /// </summary>
            public ConnectedAppStatus Status { get; set; } = ConnectedAppStatus.Waiting;

            /// <summary>
            /// Gets or sets the last time a status message was received.
            /// </summary>
            public DateTime LastStatusReceivedTime { get; set; } = DateTime.UtcNow;

            /// <summary>
            /// Gets or sets the status indicator ellipse.
            /// </summary>
            public Ellipse StatusDot { get; set; } = null;
        }

        /// <summary>
        /// Represents a UI row for a device in the connected devices grid.
        /// </summary>
        public class DeviceRow
        {
            /// <summary>
            /// Gets or sets the row index in the grid.
            /// </summary>
            public int RowIndex { get; set; }

            /// <summary>
            /// Gets or sets the row definition.
            /// </summary>
            public RowDefinition RowDefinition { get; set; } = null;

            /// <summary>
            /// Gets or sets the status indicator ellipse.
            /// </summary>
            public Ellipse Dot { get; set; } = null;

            /// <summary>
            /// Gets or sets the device name text block.
            /// </summary>
            public TextBlock Text { get; set; } = null;

            /// <summary>
            /// Gets or sets the start button.
            /// </summary>
            public Button BtnStart { get; set; } = null;

            /// <summary>
            /// Gets or sets the stop button.
            /// </summary>
            public Button BtnStop { get; set; } = null;
        }

        /// <summary>
        /// Gets the list of available store modes.
        /// </summary>
        public List<RendezVousPipeline.StoreMode> StoreModeList { get; }

        /// <summary>
        /// Gets the list of available session naming modes.
        /// </summary>
        public List<RendezVousPipeline.SessionNamingMode> SessionModeList { get; }

        #region INotifyPropertyChanged

        /// <summary>
        /// Event raised when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Sets a property value and raises the PropertyChanged event if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">Reference to the backing field.</param>
        /// <param name="value">The new value.</param>
        /// <param name="propertyName">The name of the property (automatically provided by CallerMemberName).</param>
        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets the pipeline configuration.
        /// </summary>
        public RendezVousPipelineConfiguration Configuration
        {
            get => this.configuration;
            set => this.SetProperty(ref this.configuration, value);
        }

        /// <summary>
        /// Gets or sets the command source identifier.
        /// </summary>
        public string CommandSource
        {
            get => this.commandSource;
            set => this.SetProperty(ref this.commandSource, value);
        }

        /// <summary>
        /// Gets or sets the local dataset path.
        /// </summary>
        public string LocalDatasetPath
        {
            get => this.configuration.DatasetPath;
            set => this.SetProperty(ref this.configuration.DatasetPath, value);
        }

        /// <summary>
        /// Gets or sets the local dataset name.
        /// </summary>
        public string LocalDatasetName
        {
            get => this.configuration.DatasetName;
            set => this.SetProperty(ref this.configuration.DatasetName, value);
        }

        /// <summary>
        /// Gets or sets the local session name.
        /// </summary>
        public string LocalSessionName
        {
            get => this.configuration.SessionName;
            set => this.SetProperty(ref this.configuration.SessionName, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether debug mode is enabled.
        /// </summary>
        public bool IsDebug
        {
            get => this.isDebug;
            set => this.SetProperty(ref this.isDebug, value);
        }

        /// <summary>
        /// Gets or sets the external configuration directory path.
        /// </summary>
        public string ExternalConfigurationDirectory
        {
            get => this.externalConfigurationDirectory;
            set => this.SetProperty(ref this.externalConfigurationDirectory, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether annotations are enabled.
        /// </summary>
        public bool IsAnnotationEnabled
        {
            get => this.isAnnotationEnabled;
            set => this.SetProperty(ref this.isAnnotationEnabled, value);
        }

        /// <summary>
        /// Gets or sets the annotation schema directory path.
        /// </summary>
        public string AnnotationSchemaDirectory
        {
            get => this.annotationSchemaDirectory;
            set => this.SetProperty(ref this.annotationSchemaDirectory, value);
        }

        /// <summary>
        /// Gets or sets the annotation web page file path.
        /// </summary>
        public string AnnotationWebPage
        {
            get => this.annotationWebPage;
            set => this.SetProperty(ref this.annotationWebPage, value);
        }

        /// <summary>
        /// Gets or sets the annotation server port.
        /// </summary>
        public uint AnnotationPort
        {
            get => this.annotationPort;
            set => this.SetProperty(ref this.annotationPort, value);
        }

        /// <summary>
        /// Gets or sets the log text displayed in the UI.
        /// </summary>
        public string Log
        {
            get => this.log;
            set => this.SetProperty(ref this.log, value);
        }

        /// <summary>
        /// Represents the initialization state of the pipeline.
        /// </summary>
        private enum SetupState
        {
            /// <summary>Pipeline has not been initialized.</summary>
            NotInitialised,

            /// <summary>Pipeline has been initialized.</summary>
            PipelineInitialised,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.internalLog = (log) =>
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        Log += $"{log}\n";
                    }));
                }
            };
            this.StoreModeList = new List<RendezVousPipeline.StoreMode>(Enum.GetValues(typeof(RendezVousPipeline.StoreMode)).Cast<RendezVousPipeline.StoreMode>());
            this.SessionModeList = new List<RendezVousPipeline.SessionNamingMode>(Enum.GetValues(typeof(RendezVousPipeline.SessionNamingMode)).Cast<RendezVousPipeline.SessionNamingMode>());

            this.setupState = SetupState.NotInitialised;
            this.server = null;
            this.configuration = new RendezVousPipelineConfiguration();

            this.LoadConfig();
            this.InitializeComponent();
            this.DataContext = this;
            this.UpdateLayout();
            this.SetupAnnotationTab();
            this.RefreshUIFromConfiguration();
            this.UpdateLayout();
        }

        /// <summary>
        /// Sets up the annotation tab UI components.
        /// </summary>
        private void SetupAnnotationTab()
        {
            // Initialize annotation tab state
            UiGenerator.SetTextBoxPreviewTextChecker<uint>(this.AnnotationPortTextBox, uint.TryParse);
            this.UpdateAnnotationTab();
        }

        /// <summary>
        /// Loads configuration from application settings.
        /// </summary>
        private void LoadConfig()
        {
            this.Configuration.RendezVousHost = Properties.Settings.Default.RendezVousHost;
            this.Configuration.RendezVousPort = Properties.Settings.Default.RendezVousPort;
            this.Configuration.ClockPort = Properties.Settings.Default.ClockPort;
            this.LocalDatasetPath = Properties.Settings.Default.DatasetPath;
            this.LocalSessionName = Properties.Settings.Default.SessionName;
            this.LocalDatasetName = Properties.Settings.Default.DatasetName;
            this.isDebug = this.Configuration.Debug = Properties.Settings.Default.Debug;
            this.Configuration.AutomaticPipelineRun = Properties.Settings.Default.AutomaticPipelineRun;
            this.ExternalConfigurationDirectory = Properties.Settings.Default.ExternalConfigurationDirectory;

            // Annotation Tab
            this.IsAnnotationEnabled = Properties.Settings.Default.IsAnnotationEnabled;
            this.AnnotationSchemaDirectory = Properties.Settings.Default.AnnotationSchemasPath;
            this.AnnotationWebPage = Properties.Settings.Default.AnnotationHtmlPage;
            this.AnnotationPort = Properties.Settings.Default.AnnotationPort;
        }

        /// <summary>
        /// Refreshes UI elements from the current configuration.
        /// </summary>
        private void RefreshUIFromConfiguration()
        {
            // Configuration Tab
            this.LoadConfig();
            this.StoreModeComboBox.SelectedIndex = Properties.Settings.Default.StoreMode;
            this.SessionModeComboBox.SelectedIndex = Properties.Settings.Default.SessionMode;

            // Annotation Tab
            this.IsAnnotationEnabled = Properties.Settings.Default.IsAnnotationEnabled;
            this.AnnotationSchemaDirectory = Properties.Settings.Default.AnnotationSchemasPath;
            this.AnnotationWebPage = Properties.Settings.Default.AnnotationHtmlPage;
            this.AnnotationPort = Properties.Settings.Default.AnnotationPort;
            this.isDebug = this.Configuration.Debug = Properties.Settings.Default.Debug;
            this.UpdateAnnotationTab();

            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = false;
        }

        /// <summary>
        /// Refreshes the configuration from UI elements and saves to settings.
        /// </summary>
        private void RefreshConfigurationFromUI()
        {
            // Configuration Tab
            Properties.Settings.Default.RendezVousHost = this.Configuration.RendezVousHost;
            Properties.Settings.Default.RendezVousPort = this.Configuration.RendezVousPort;
            Properties.Settings.Default.ClockPort = this.Configuration.ClockPort;
            Properties.Settings.Default.DatasetPath = this.LocalDatasetPath;
            Properties.Settings.Default.SessionName = this.LocalSessionName;
            Properties.Settings.Default.DatasetName = this.LocalDatasetName;
            Properties.Settings.Default.Debug = this.Configuration.Debug = this.isDebug;
            Properties.Settings.Default.AutomaticPipelineRun = this.Configuration.AutomaticPipelineRun;
            Properties.Settings.Default.StoreMode = (int)this.StoreModeComboBox.SelectedIndex;
            Properties.Settings.Default.SessionMode = (int)this.SessionModeComboBox.SelectedIndex;
            Properties.Settings.Default.ExternalConfigurationDirectory = this.ExternalConfigurationDirectory;

            // Annotation Tab
            Properties.Settings.Default.IsAnnotationEnabled = this.IsAnnotationEnabled;
            Properties.Settings.Default.AnnotationSchemasPath = this.AnnotationSchemaDirectory;
            Properties.Settings.Default.AnnotationHtmlPage = this.AnnotationWebPage;
            Properties.Settings.Default.AnnotationPort = this.AnnotationPort;

            Properties.Settings.Default.Save();

            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = false;
        }

        /// <summary>
        /// Handles the store mode selection changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void StoreModeSelected(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            this.Configuration.StoreMode = (RendezVousPipeline.StoreMode)this.StoreModeComboBox.SelectedIndex;
        }

        /// <summary>
        /// Handles the session mode selection changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void SessionModeSelected(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            this.Configuration.SessionMode = (RendezVousPipeline.SessionNamingMode)this.SessionModeComboBox.SelectedIndex;
        }

        /// <summary>
        /// Handles the load configuration button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnLoadConfiguration(object sender, RoutedEventArgs e)
        {
            this.RefreshUIFromConfiguration();
            this.AddLog("Configuration Loaded");
            e.Handled = true;
        }

        /// <summary>
        /// Handles the save configuration button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnSaveConfiguration(object sender, RoutedEventArgs e)
        {
            this.RefreshConfigurationFromUI();
            this.AddLog("Configuration Saved");
            e.Handled = true;
        }

        /// <summary>
        /// Handles the setup configuration button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnSetupConfiguration(object sender, RoutedEventArgs e)
        {
            this.Tab.SelectedItem = this.ConfigurationTab;
        }

        /// <summary>
        /// Handles the start button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnStartClick(object sender, RoutedEventArgs e)
        {
            this.SetupPipeline();
        }

        /// <summary>
        /// Sets up and initializes the pipeline with the current configuration.
        /// </summary>
        private void SetupPipeline()
        {
            if (this.setupState >= SetupState.PipelineInitialised)
            {
                return;
            }

            if (this.ExternalConfigurationDirectory.Length > 0)
            {
                this.LoadExternalConfiguration(this.ExternalConfigurationDirectory);
            }

            this.configuration.Diagnostics = DatasetPipeline.DiagnosticsMode.Off;
            this.configuration.AutomaticPipelineRun = true;
            this.configuration.CommandDelegate = this.CommandReceived;
            this.configuration.Debug = this.IsDebug;
            this.configuration.RecordIncomingProcess = true;
            this.configuration.CommandPort = 11610;
            this.configuration.ClockPort = 11621;
            this.configuration.DatasetPath = this.LocalDatasetPath;
            this.configuration.DatasetName = this.LocalDatasetName;
            this.configuration.SessionName = this.LocalSessionName;
            try
            {
                this.server = new RendezVousPipeline(this.configuration, "Server", null, this.internalLog);
            }
            catch (Exception ex)
            {
                this.AddLog($"Error initializing server pipeline: {ex.Message}");
                return;
            }

            this.pipeline = this.server.Pipeline;
            this.AddLog("Server initialisation started");

            // Setup annotations if enabled
            this.SetupWebSocketsAndAnnotations();

            this.server.Start();
            this.AddLog("Server started");
            this.StartStatusMonitoring();
            this.AllDevicesStackPanel.IsEnabled = true;
            this.setupState = SetupState.PipelineInitialised;
        }

        /// <summary>
        /// Sets up WebSocket manager and annotation components if enabled.
        /// </summary>
        private void SetupWebSocketsAndAnnotations()
        {
            // Create list of addresses for WebSocket
            List<string> addresses = new List<string>() { $"http://{this.Configuration.RendezVousHost}:{this.AnnotationPort}/ws/" };

            if (!this.IsAnnotationEnabled)
            {
                // Instantiate the HTTPAnnotationsComponent
                this.websocketManager = new Microsoft.Psi.Interop.Transport.WebSocketsManager(true, addresses, false);
                this.pipeline.PipelineRun += (s, e) =>
                {
                    this.websocketManager?.Start((dt) => { });
                };
                this.pipeline.ComponentCompleted += (s, e) =>
                {
                    this.websocketManager?.Dispose();
                };
            }
            else
            {
                if (!System.IO.Directory.Exists(this.AnnotationSchemaDirectory))
                {
                    this.AddLog($"Warning: Annotation schema directory does not exist: {this.AnnotationSchemaDirectory}");
                    return;
                }

                if (!System.IO.File.Exists(this.AnnotationWebPage))
                {
                    this.AddLog($"Warning: Annotation web page does not exist: {this.AnnotationWebPage}");
                    return;
                }

                // Add HTTP
                addresses.Add($"http://{this.Configuration.RendezVousHost}:{this.AnnotationPort}/");

                // Instantiate the HTTPAnnotationsComponent
                this.websocketManager = new SAAC.AnnotationsComponents.HTTPAnnotationsComponent(this.server, addresses, this.AnnotationSchemaDirectory, this.AnnotationWebPage);
                this.AddLog("Annotations component initialized successfully");
            }

            this.websocketManager.OnNewWebSocketConnectedHandler += this.OnWebsocketConnection;
        }

        /// <summary>
        /// Handles new WebSocket connections.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The connection information.</param>
        private void OnWebsocketConnection(object sender, (string, string, Uri) e)
        {
            if (e.Item2 == "annotation" || !this.configuration.TopicsTypes.ContainsKey(e.Item2))
            {
                return;
            }

            Pipeline pipeline = this.server.GetOrCreateSubpipeline($"{e.Item1}-{e.Item2}");
            var source = this.websocketManager.ConnectWebsocketSource<string>(pipeline, this.configuration.TypesSerializers[this.configuration.TopicsTypes[e.Item2]].GetFormat(), e.Item1, e.Item2, false);
            this.server.CreateConnectorAndStore(e.Item2, e.Item1, this.server.CurrentSession, pipeline, typeof(string), source);
            pipeline.RunAsync();
        }

        /// <summary>
        /// Extracts the device name from a command source string.
        /// </summary>
        /// <param name="argument">The command source string.</param>
        /// <returns>The extracted device name.</returns>
        private string GetName(object argument)
        {
            string suffix = "-Command";
            string stringArgument = (string)argument;
            string name = stringArgument.EndsWith(suffix)
                ? stringArgument.Substring(0, stringArgument.Length - suffix.Length)
                : stringArgument;

            return name;
        }

        /// <summary>
        /// Handles the stop button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnStopClick(object sender, RoutedEventArgs e)
        {
            this.Stop();
        }

        /// <summary>
        /// Handles the quit button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            this.Stop();
            this.Close();
            e.Handled = true;
        }

        /// <summary>
        /// Stops the server pipeline and cleans up resources.
        /// </summary>
        private void Stop()
        {
            // Stop annotations component
            if (this.websocketManager != null)
            {
                try
                {
                    // Assuming the component has a Stop or Dispose method
                    this.AddLog("Stopping annotations component");
                    this.websocketManager = null;
                }
                catch (Exception ex)
                {
                    this.AddLog($"Error stopping annotations: {ex.Message}");
                }
            }

            this.server?.Dataset?.Save();
            this.server?.Dispose();
            this.StopStatusMonitoring();
        }

        /// <summary>
        /// Handles received commands from connected applications.
        /// </summary>
        /// <param name="source">The command source.</param>
        /// <param name="message">The command message.</param>
        private void CommandReceived(string source, Message<(RendezVousPipeline.Command, string)> message)
        {
            var args = message.Data.Item2.Split(';');

            if (args[0] != "Server" && args[0] != "Server-Command")
            {
                return;
            }

            string name = this.GetName(source);

            switch (message.Data.Item1)
            {
                case RendezVousPipeline.Command.Status:
                    this.CheckStatus(name, args, message.OriginatingTime);
                    break;
            }
        }

        /// <summary>
        /// Checks and updates the status of a connected application.
        /// </summary>
        /// <param name="name">The application name.</param>
        /// <param name="args">The status arguments.</param>
        /// <param name="time">The message timestamp.</param>
        private void CheckStatus(string name, string[] args, DateTime time)
        {
            if (args.Length < 2)
            {
                return;
            }

            switch (args[1])
            {
                case "Waiting":
                case "Running":
                    if (!this.connectedApps.ContainsKey(name))
                    {
                        this.connectedApps[name] = new ConnectedApp
                        {
                            Name = name,
                        };
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.SpawnEllipseTextButtonsRow(name);
                        }));
                    }

                    break;
            }

            switch (args[1])
            {
                case "Running":
                    if (this.connectedApps.ContainsKey(name))
                    {
                        this.connectedApps[name].Status = ConnectedAppStatus.Running;
                    }

                    break;
                case "Connected":
                case "Served":
                case "Initializing":
                case "Initialized":
                case "Waiting":
                    if (this.connectedApps.ContainsKey(name))
                    {
                        this.connectedApps[name].Status = ConnectedAppStatus.Waiting;
                    }

                    break;
                case "Stopping":
                case "Stopped":
                    if (this.connectedApps.ContainsKey(name))
                    {
                        this.connectedApps.Remove(name);
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            this.RemoveDeviceRow(name);
                        }));
                    }

                    break;
                case "Failed":
                case "Error":
                    if (this.connectedApps.ContainsKey(name))
                    {
                        this.connectedApps[name].Status = ConnectedAppStatus.Error;
                    }

                    break;
            }

            if (!this.connectedApps.ContainsKey(name))
            {
                return;
            }

            this.connectedApps[name].LastStatusReceivedTime = time;
            this.UpdateDotColor(this.connectedApps[name]);
        }

        #region Status Monitoring Connected Applications

        /// <summary>
        /// Starts the periodic status monitoring of connected applications.
        /// </summary>
        public void StartStatusMonitoring()
        {
            this.statusTimer = new Timer(callback: this.StatusTimerCallback, state: null, dueTime: TimeSpan.Zero, period: TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Timer callback for checking application statuses.
        /// </summary>
        /// <param name="state">The timer state.</param>
        private void StatusTimerCallback(object? state)
        {
            // Anti-reentrancy lock
            if (this.statusCheckRunning)
            {
                return;
            }

            this.statusCheckRunning = true;

            try
            {
                // Status request
                this.server.SendCommand(RendezVousPipeline.Command.Status, "*", string.Empty);

                foreach (var app in this.connectedApps.Values)
                {
                    // Timeout (e.g., 3s)
                    if (DateTime.UtcNow - app.LastStatusReceivedTime > TimeSpan.FromSeconds(3))
                    {
                        app.Status = ConnectedAppStatus.Error;
                        this.UpdateDotColor(app);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception if needed
            }
            finally
            {
                this.statusCheckRunning = false;
            }
        }

        /// <summary>
        /// Stops the status monitoring timer.
        /// </summary>
        public void StopStatusMonitoring()
        {
            if (this.statusTimer != null)
            {
                this.statusTimer.Dispose();
                this.statusTimer = null;
            }
        }

        #endregion

        #region UI Connected Application Managers

        /// <summary>
        /// Creates a new UI row for a connected device.
        /// </summary>
        /// <param name="argument">The device name argument.</param>
        private void SpawnEllipseTextButtonsRow(object argument)
        {
            string name = this.GetName(argument);

            if (this.rowsByDeviceName.ContainsKey(name))
            {
                return;
            }

            UiGenerator.AddRowsDefinitionToGrid(this.ConnectedDevicesGrid, GridLength.Auto, 1);
            int rowIndex = this.ConnectedDevicesGrid.RowDefinitions.Count - 1;
            var rowDef = this.ConnectedDevicesGrid.RowDefinitions[rowIndex];

            // Ellipse (left)
            var dot = UiGenerator.GenerateEllipse(size: 14, fill: Brushes.Orange, stroke: Brushes.Black, strokeThickness: 1, name: $"Dot_{this.rowIndex}");
            dot.Margin = new Thickness(0, 0, 10, 0);

            // TextBox (middle)
            var tb = UiGenerator.GenerateText(name, double.NaN, name: $"Text_{this.rowIndex}");
            tb.Loaded += (s, e) =>
            {
                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                tb.Width = tb.DesiredSize.Width + 10;
            };

            // Button 1 (right)
            var btnOk = UiGenerator.GenerateButton("Start", (s, e) =>
            {
                this.server.SendCommand(RendezVousPipeline.Command.Run, name, string.Empty);
            }, name: $"BtnOk_{this.rowIndex}");
            btnOk.Margin = new Thickness(0, 0, 15, 0);
            btnOk.IsEnabled = true;

            // Button 2 (right) - remove this row
            var btnRemove = UiGenerator.GenerateButton("Stop", (s, e) =>
            {
                this.server.SendCommand(RendezVousPipeline.Command.Close, name, string.Empty);
            }, name: $"BtnRemove_{this.rowIndex}");
            btnRemove.IsEnabled = false;
            btnRemove.Margin = new Thickness(0, 0, 15, 0);
            UiGenerator.SetElementInGrid(this.ConnectedDevicesGrid, dot, 0, this.ConnectedDevicesGrid.RowDefinitions.Count - 1);
            UiGenerator.SetElementInGrid(this.ConnectedDevicesGrid, tb, 1, this.ConnectedDevicesGrid.RowDefinitions.Count - 1);
            UiGenerator.SetElementInGrid(this.ConnectedDevicesGrid, btnOk, 2, this.ConnectedDevicesGrid.RowDefinitions.Count - 1);
            UiGenerator.SetElementInGrid(this.ConnectedDevicesGrid, btnRemove, 3, this.ConnectedDevicesGrid.RowDefinitions.Count - 1);

            this.connectedApps[name].StatusDot = dot;
            this.connectedApps[name].Status = ConnectedAppStatus.Waiting;
            this.connectedApps[name].LastStatusReceivedTime = DateTime.UtcNow;

            this.rowsByDeviceName[name] = new DeviceRow
            {
                RowIndex = rowIndex,
                RowDefinition = rowDef,
                Dot = dot,
                Text = tb,
                BtnStart = btnOk,
                BtnStop = btnRemove,
            };
        }

        /// <summary>
        /// Handles the debug checkbox click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CkbDebug(object sender, RoutedEventArgs e)
        {
            if (this.DebugCheckbox.IsChecked == true)
            {
                this.configuration.Debug = true;
            }
            else
            {
                this.configuration.Debug = false;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Removes a device row from the UI grid.
        /// </summary>
        /// <param name="name">The device name.</param>
        public void RemoveDeviceRow(string name)
        {
            if (!this.rowsByDeviceName.TryGetValue(name, out var row))
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                int removedRowIndex = row.RowIndex;

                // 1) Remove controls from Grid
                this.ConnectedDevicesGrid.Children.Remove(row.Dot);
                this.ConnectedDevicesGrid.Children.Remove(row.Text);
                this.ConnectedDevicesGrid.Children.Remove(row.BtnStart);
                this.ConnectedDevicesGrid.Children.Remove(row.BtnStop);

                // 2) Remove RowDefinition
                this.ConnectedDevicesGrid.RowDefinitions.Remove(row.RowDefinition);

                // 3) Remove from dictionary
                this.rowsByDeviceName.Remove(name);

                // 4) Move up elements that were below
                foreach (UIElement child in this.ConnectedDevicesGrid.Children)
                {
                    int r = Grid.GetRow(child);
                    if (r > removedRowIndex)
                    {
                        Grid.SetRow(child, r - 1);
                    }
                }

                // 5) Update stored RowIndex values
                foreach (var dr in this.rowsByDeviceName.Values)
                {
                    if (dr.RowIndex > removedRowIndex)
                    {
                        dr.RowIndex--;
                    }
                }
            });
        }

        /// <summary>
        /// Updates the status indicator color for a connected application.
        /// </summary>
        /// <param name="app">The connected application.</param>
        private void UpdateDotColor(ConnectedApp app)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                switch (app.Status)
                {
                    case ConnectedAppStatus.Running:
                        app.StatusDot.Fill = Brushes.Green;
                        this.rowsByDeviceName[app.Name].BtnStart.IsEnabled = false;
                        this.rowsByDeviceName[app.Name].BtnStop.IsEnabled = true;
                        break;
                    case ConnectedAppStatus.Waiting:
                        app.StatusDot.Fill = Brushes.Orange;
                        this.rowsByDeviceName[app.Name].BtnStart.IsEnabled = true;
                        this.rowsByDeviceName[app.Name].BtnStop.IsEnabled = true;
                        break;
                    case ConnectedAppStatus.Error:
                        app.StatusDot.Fill = Brushes.Red;
                        this.rowsByDeviceName[app.Name].BtnStart.IsEnabled = false;
                        this.rowsByDeviceName[app.Name].BtnStop.IsEnabled = false;
                        break;
                }
            }));
        }

        #endregion

        #region Buttons

        /// <summary>
        /// Handles the browse dataset path button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnBrowseNameClick(object sender, RoutedEventArgs e)
        {
            UiGenerator.FolderPicker openFileDialog = new UiGenerator.FolderPicker();
            if (openFileDialog.ShowDialog() == true)
            {
                this.DatasetPathTextBox.Text = openFileDialog.ResultName;
                this.LocalDatasetPath = openFileDialog.ResultName;
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            }
        }

        /// <summary>
        /// Handles the activate annotation checkbox click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CkbActivateAnnotation(object sender, RoutedEventArgs e)
        {
            this.UpdateAnnotationTab();
            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            e.Handled = true;
        }

        /// <summary>
        /// Handles the browse schema directory button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnBrowseSchemaDirectoryClick(object sender, RoutedEventArgs e)
        {
            UiGenerator.FolderPicker openFolderDialog = new UiGenerator.FolderPicker();
            if (openFolderDialog.ShowDialog() == true)
            {
                this.AnnotationSchemaDirectory = openFolderDialog.ResultName;
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles the browse web page button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnBrowseWebPageClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "HTML files (*.html;*.htm)|*.html;*.htm|All files (*.*)|*.*";
            openFileDialog.DefaultExt = ".html";
            if (openFileDialog.ShowDialog() == true)
            {
                this.AnnotationWebPage = openFileDialog.FileName;
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles the browse external configuration button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void BtnBrowseExternalConfiguration_Click(object sender, RoutedEventArgs e)
        {
            UiGenerator.FolderPicker openFileDialog = new UiGenerator.FolderPicker
            {
                Title = "External configuration directory",
            };

            if (openFileDialog.ShowDialog() == true)
            {
                this.ExternalConfigurationDirectory = openFileDialog.ResultName;
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles the annotation port text box text changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void AnnotationPortTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
        }

        /// <summary>
        /// Handles the start all devices button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void StartAllDevices(object sender, RoutedEventArgs e)
        {
            this.server.SendCommand(RendezVousPipeline.Command.Run, "*", string.Empty);
            e.Handled = true;
        }

        /// <summary>
        /// Handles the stop all devices button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void StopAllDevices(object sender, RoutedEventArgs e)
        {
            this.server.SendCommand(RendezVousPipeline.Command.Close, "*", string.Empty);
            e.Handled = true;
        }

        #endregion

        #region Browser

        /// <summary>
        /// Loads external configuration from JSON files in the specified directory.
        /// </summary>
        /// <param name="topicsFolder">The folder containing topic configuration files.</param>
        private void LoadExternalConfiguration(string topicsFolder)
        {
            // For each files inside the folder, load the json and store it in the dictionary
            foreach (string jsonFile in Directory.GetFiles(topicsFolder, "*.json"))
            {
                this.LoadTopicsAndAssembly(jsonFile, topicsFolder);
            }
        }

        #endregion

        /// <summary>
        /// Handles the log text box text changed event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Log_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            TextBox? log = sender as TextBox;
            if (log == null)
            {
                return;
            }

            log.CaretIndex = log.Text.Length;
            log.ScrollToEnd();
        }

        /// <summary>
        /// Adds a log message to the log window.
        /// </summary>
        /// <param name="logMessage">The log message to add.</param>
        private void AddLog(string logMessage)
        {
            Log += $"{logMessage}\n";
        }

        /// <summary>
        /// Updates the annotation tab UI elements based on the annotation enabled state.
        /// </summary>
        private void UpdateAnnotationTab()
        {
            foreach (UIElement annotationUIElement in this.AnnotationGrid.Children)
            {
                if (annotationUIElement is GroupBox groupBox)
                {
                    groupBox.IsEnabled = this.isAnnotationEnabled;
                }
            }
        }

        #region Load JSON Config

        /// <summary>
        /// Represents a topic format definition loaded from JSON configuration.
        /// </summary>
        public sealed class TopicFormatDefinition
        {
            /// <summary>
            /// Gets or sets the topic name.
            /// </summary>
            public string Topic { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the message type name.
            /// </summary>
            public string Type { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the format class name.
            /// </summary>
            public string ClassFormat { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the stream to store identifier.
            /// </summary>
            public string StreamToStore { get; set; } = string.Empty;
        }

        /// <summary>
        /// Loads topics and assemblies from a JSON configuration file.
        /// </summary>
        /// <param name="jsonFilePath">The path to the JSON configuration file.</param>
        /// <param name="folder">The folder containing the assembly.</param>
        /// <returns>True if the configuration was loaded successfully; otherwise false.</returns>
        public bool LoadTopicsAndAssembly(string jsonFilePath, string folder)
        {
            if (!File.Exists(jsonFilePath))
            {
                this.AddLog($"The file {jsonFilePath} does not exist");
                return false;
            }

            var json = File.ReadAllText(jsonFilePath);

            var items = JsonConvert.DeserializeObject<List<TopicFormatDefinition>>(json) ?? new List<TopicFormatDefinition>();
            if (items.Count == 0)
            {
                this.AddLog($"No topic definitions found in {jsonFilePath}");
                return false;
            }

            // Check first if there is an assembly to load types from
            string assemblyPath = $@"{folder}/{System.IO.Path.GetFileNameWithoutExtension(jsonFilePath)}/{System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(jsonFilePath), ".dll")}";
            if (!File.Exists(jsonFilePath))
            {
                this.AddLog($"The file {assemblyPath} does not exist");
                return false;
            }

            if (Assembly.LoadFrom(assemblyPath).GetExportedTypes().Length == 0)
            {
                this.AddLog($"No types found in assembly {assemblyPath}");
                return false;
            }

            foreach (var item in items)
            {
                var messageType = this.ResolveType(item.Type);
                if (messageType == null)
                {
                    this.AddLog($"Failed to resolve format type for topic {item.Topic}");
                    continue;
                }

                this.AddLog($"Topic {item.Topic} type is {messageType.ToString()}");

                var formatType = this.ResolvePsiFormatType(item.ClassFormat, Assembly.LoadFrom(assemblyPath).GetExportedTypes().ToList());
                if (formatType == null)
                {
                    this.AddLog($"Failed to resolve format type for topic {item.Topic}");
                    continue;
                }

                var formatInstance = (IPsiFormat)this.CreateInstance(formatType);
                this.AddLog($"Topic {item.Topic} format is {formatInstance.ToString()}");
                this.configuration.AddTopicFormatAndTransformer(item.Topic, messageType, formatInstance);
                this.configuration.StreamToStore.Add(item.Topic, item.StreamToStore);
            }

            return true;
        }

        /// <summary>
        /// Resolves a type by name from loaded assemblies.
        /// </summary>
        /// <param name="typeName">The type name to resolve.</param>
        /// <returns>The resolved type, or null if not found.</returns>
        private Type? ResolveType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves a Psi format type by class name.
        /// </summary>
        /// <param name="formatClassName">The format class name.</param>
        /// <param name="loadedType">The list of loaded types.</param>
        /// <returns>The resolved format type, or null if not found.</returns>
        private Type? ResolvePsiFormatType(string formatClassName, List<Type> loadedType)
        {
            if (string.IsNullOrWhiteSpace(formatClassName))
            {
                this.AddLog("The format class name cannot be empty");
                return null;
            }

            // First check in loaded types
            var type = loadedType.FirstOrDefault(t => t.Name == formatClassName && typeof(IPsiFormat).IsAssignableFrom(t));
            if (type is not null)
            {
                return type;
            }

            // Then in app domain assemblies
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(formatClassName);
                if (type != null && typeof(IPsiFormat).IsAssignableFrom(type))
                {
                    return type;
                }

                try
                {
                    // Search by simple name if the full name is not found
                    type = asm.GetTypes().FirstOrDefault(t =>
                        t.Name == formatClassName &&
                        typeof(IPsiFormat).IsAssignableFrom(t));
                }
                catch
                {
                    continue;
                }

                if (type != null)
                {
                    return type;
                }
            }

            this.AddLog($"PsiFormat type not found: {formatClassName}");
            return null;
        }

        /// <summary>
        /// Creates an instance of the specified type.
        /// </summary>
        /// <param name="type">The type to instantiate.</param>
        /// <returns>The created instance.</returns>
        private object CreateInstance(Type type)
        {
            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                this.AddLog($"Type {type.Name} does not have a parameterless constructor.");
            }

            return Activator.CreateInstance(type);
        }

        #endregion

        #region IPsiStudioPipeline Implementation

        /// <summary>
        /// Gets the dataset associated with this pipeline.
        /// </summary>
        /// <returns>The dataset.</returns>
        public Dataset GetDataset()
        {
            return this.server.Dataset;
        }

        /// <summary>
        /// Runs the pipeline with the specified time interval.
        /// </summary>
        /// <param name="timeInterval">The time interval to run.</param>
        public void RunPipeline(TimeInterval timeInterval)
        {
            this.SetupPipeline();
        }

        /// <summary>
        /// Stops the pipeline execution.
        /// </summary>
        public void StopPipeline()
        {
            this.Stop();
        }

        /// <summary>
        /// Disposes of the main window and its resources.
        /// </summary>
        public void Dispose()
        {
            this.Close();
        }

        /// <summary>
        /// Gets the pipeline start time.
        /// </summary>
        /// <returns>The start time.</returns>
        public DateTime GetStartTime()
        {
            return this.server.Pipeline.StartTime;
        }

        /// <summary>
        /// Gets the pipeline replayable mode.
        /// </summary>
        /// <returns>The replayable mode.</returns>
        public PipelineReplaybleMode GetReplaybleMode()
        {
            return PipelineReplaybleMode.Not;
        }

        #endregion
    }
}
