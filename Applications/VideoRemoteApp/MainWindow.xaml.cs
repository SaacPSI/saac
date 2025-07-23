using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
using Microsoft.Psi;
using Microsoft.Psi.Media;
using Microsoft.Psi.Imaging;
using SAAC.RendezVousPipelineServices;
using Microsoft.Psi.Remoting;
using SharpDX;


namespace VideoRemoteApp
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private string state = "Hello";
        public string State
        {
            get => state;
            set => SetProperty(ref state, value);
        }
        public void DelegateMethodStatus(string status)
        {
            State = status;
        }

        private RendezVousPipelineConfiguration PipelineConfiguration = new RendezVousPipelineConfiguration();

        private string rendezVousServerIp = "10.144.210.100";
        public string RendezVousServerIp
        {
            get => rendezVousServerIp;
            set => SetProperty(ref rendezVousServerIp, value);
        }
        public void DelegateMethodRendezVousServerIP(string ip)
        {
            RendezVousServerIp = ip;
        }
        public int ServerPort
        {
            get => PipelineConfigurationUI.RendezVousPort;
            set => SetProperty(ref PipelineConfigurationUI.RendezVousPort, value);
        }
        /*private uint rendezVousServerPort = 13331;
        public uint RendezVousServerPort
        {
            get => rendezVousServerPort;
            set => SetProperty(ref rendezVousServerPort, value);
        }
        public void DelegateMethodRendezVousServerPort(uint ip)
        {
            RendezVousServerPort = ip;
        }*/

        public RendezVousPipelineConfiguration PipelineConfigurationUI
        {
            get => PipelineConfiguration;
            set => SetProperty(ref PipelineConfiguration, value);
        }

        private string applicationName = "VideoRemoteApp";
        public string ApplicationName
        {
            get => applicationName;
            set => SetProperty(ref applicationName, value);
        }
        public void DelegateMethodApplicationName(string name)
        {
            ApplicationName = name;
        }

        private string commandSource = "Server";
        public string CommandSource
        {
            get => commandSource;
            set => SetProperty(ref commandSource, value);
        }
        public void DelegateMethodCS(string commandSource)
        {
            CommandSource = commandSource;
        }

        private string log = "";
        public string Log
        {
            get => log;
            set => SetProperty(ref log, value);
        }
        public void DelegateMethodLog(string log)
        {
            Log = log;
        }


        public List<string> IPsList { get; }
        /*public string IPSelected
        {
            get => PipelineConfiguration.RendezVousHost;
            set => SetProperty(ref PipelineConfiguration.RendezVousHost, value);
        }
        public void DelegateMethodColorResolution(string val)
        {
            PipelineConfiguration.RendezVousHost = val;
        }*/

        private RendezVousPipeline? Pipeline;
        private VideoRemoteConnectorConfiguration Configuration = new VideoRemoteConnectorConfiguration();
        private VideoRemoteConnector videoConnector; // to modify/remove

        public MainWindow()
        {
            DataContext = this;
            PipelineConfiguration.ClockPort = PipelineConfiguration.CommandPort = 0;
            //PipelineConfiguration.AutomaticPipelineRun = true;
            IPsList = new List<string>() { "localhost" };
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                IPsList.Add(ip.ToString());
            }

            Pipeline = null;
            RendezVousServerIp = Properties.Settings.Default.rendezVousServerIp;
            PipelineConfiguration.RendezVousPort = (int)(Properties.Settings.Default.rendezVousServerPort);
            PipelineConfigurationUI.RendezVousPort = (int)(Properties.Settings.Default.rendezVousServerPort);
            ServerPort = (int)(Properties.Settings.Default.rendezVousServerPort);
            ApplicationName = Properties.Settings.Default.ApplicationName;
            InitializeComponent();
            UpdateLayout();
            IPs.SelectionChanged += IPs_SelectionChanged;
        }
        private void IPs_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Configuration.RendezVousAddress = PipelineConfiguration.RendezVousHost = (string)IPs.SelectedValue;
        }
        private void RefreshUIFromConfiguration()
        {
            PipelineConfigurationUI.RendezVousPort = (int)Properties.Settings.Default.rendezVousServerPort;
            ServerPort = (int)(Properties.Settings.Default.rendezVousServerPort);
            RendezVousServerIp = Properties.Settings.Default.rendezVousServerIp; 
            PipelineConfiguration.RendezVousHost = Properties.Settings.Default.IpToUse;
            ApplicationName = Properties.Settings.Default.ApplicationName;
            Properties.Settings.Default.Save();
            UpdateLayout();
        }
        private void RefreshConfigurationFromUI()
        {
            //Properties.Settings.Default.remotePort = (uint)PipelineConfigurationUI.RendezVousPort;
            Properties.Settings.Default.rendezVousServerPort = (uint)ServerPort;
            Properties.Settings.Default.rendezVousServerIp = RendezVousServerIp;
            Properties.Settings.Default.ApplicationName = ApplicationName;
            Properties.Settings.Default.IpToUse = PipelineConfiguration.RendezVousHost;
            Properties.Settings.Default.Save();
        }
        private bool UpdateConfigurationFromArgs(string[] args)
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                RefreshUIFromConfiguration();
            }));

            return true;
        }

        private void CommandRecieved(string source, Message<(RendezVousPipeline.Command, string)> message)
        {
            if (CommandSource != source)
                return;
            var args = message.Data.Item2.Split([';']);

            if (args[0] != Configuration.RendezVousApplicationName)
                return;

            switch (message.Data.Item1)
            {
                case RendezVousPipeline.Command.Initialize:
                    UpdateConfigurationFromArgs(args);
                    break;
                case RendezVousPipeline.Command.Run:
                    SetupVideo();
                    break;
                case RendezVousPipeline.Command.Stop:
                    StopVideo();
                    break;
                case RendezVousPipeline.Command.Close:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        StopPipeline();
                        Close();
                    }));
                    break;
                case RendezVousPipeline.Command.Restart:
                    if (UpdateConfigurationFromArgs(args))
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            OnRestart();
                        }));
                    }
                    break;
                case RendezVousPipeline.Command.Status:
                    Pipeline?.CommandEmitter.Post((RendezVousPipeline.Command.Status, videoConnector == null ? "Not Initialised" : videoConnector.StartTime.ToString()), Pipeline.Pipeline.GetCurrentTime());
                    break;
            }
        }

        private void SetupRendezVous()
        {
            //disable ui
            RendezVousGrid.IsEnabled = false;
            PipelineConfiguration.Diagnostics = (bool)Diagnostics.IsChecked ? RendezVousPipeline.DiagnosticsMode.Export : RendezVousPipeline.DiagnosticsMode.Store;
            PipelineConfiguration.CommandDelegate = CommandRecieved;
            Pipeline = new RendezVousPipeline(PipelineConfiguration, ApplicationName, RendezVousServerIp);
            State = "Waiting for server";
            Pipeline.Start();
            State = "Ready to start Video";
        }
        private void SetupVideo()
        {
            if (Pipeline == null)
                SetupRendezVous();
            //disable ui
            DataFormular.IsEnabled = false;
            Configuration.AppName = "SpacePipeline_Main";
            videoConnector = new VideoRemoteConnector(Pipeline.CreateSubpipeline("Video"), Configuration);
            Pipeline.AddProcess(videoConnector.GenerateProcess());
            Pipeline.RunPipeline();
            //videoConnector.RunAsync();
            State = "Running";
        }   
        private void StopPipeline()
        {
            // Stop correctly the everything.
            State = "Stopping";
            if (Pipeline != null)
            {
                Pipeline.Stop();
            }
        }
        private void StopVideo()
        {
            State = "Stopping Video";
            if (videoConnector != null && videoConnector.Image != null && Pipeline != null)
            {
                if (!Pipeline.RemoveProcess(ApplicationName))
                    Console.WriteLine("error remove rendezvous");
                try
                {
                    videoConnector.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
        protected void OnRestart()
        {
            StopVideo();
            RefreshUIFromConfiguration();
            SetupVideo();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            StopPipeline();
            RefreshConfigurationFromUI();
            base.OnClosing(e);
        }
        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            StopPipeline();
            Close();
        }

        private void BtnStartRendezVous(object sender, RoutedEventArgs e)
        {
            State = "Initializing RendezVous";
            RefreshConfigurationFromUI();
            SetupRendezVous();
        }
        private void BtnStartAll(object sender, RoutedEventArgs e)
        {
            State = "Initializing Video";
            RefreshConfigurationFromUI();
            SetupVideo();
        }

        private void BtnStopClick(object sender, RoutedEventArgs e)
        {
            StopVideo();
            DataFormular.IsEnabled = true;
            RendezVousGrid.IsEnabled = true;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
