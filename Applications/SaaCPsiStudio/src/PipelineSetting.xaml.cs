// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.PsiStudio.PipelinePlugin;
using Microsoft.Win32;
using SAAC.PipelineServices;

namespace SaaCPsiStudio
{
    /// <summary>
    /// Interaction logic for PipelineSetting.xaml.
    /// </summary>
    public partial class PipelineSetting : Window, INotifyPropertyChanged, Microsoft.Psi.PsiStudio.PipelinePlugin.IPsiStudioPipeline
    {
        private RendezVousPipelineConfiguration configuration;

        // UI
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        // LOG
        private string status = string.Empty;

        public string Status
        {
            get => this.status;
            set => this.SetProperty(ref this.status, value);
        }

        public void DelegateMethod(string status)
        {
            this.Status = status;
        }

        // RendezVousPipelineConfiguration UI Bindings
        public RendezVousPipelineConfiguration Configuration
        {
            get => this.configuration;
            set => this.SetProperty(ref this.configuration, value);
        }

        // DatasetPath
        public string DatasetPath
        {
            get => this.configuration.DatasetPath;
            set => this.SetProperty(ref this.configuration.DatasetPath, value);
        }

        // DatasetName
        public string DatasetName
        {
            get => this.configuration.DatasetName;
            set => this.SetProperty(ref this.configuration.DatasetName, value);
        }

        // SessionName
        public string SessionName
        {
            get => this.configuration.SessionName;
            set => this.SetProperty(ref this.configuration.SessionName, value);
        }

        private RendezVousPipeline? server;

        public PipelineSetting()
        {
            this.DataContext = this;

            this.configuration = new RendezVousPipelineConfiguration();
            this.configuration.RendezVousHost = "localhost"; // Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
            this.configuration.DatasetPath = "D:/Stores/SAAC/";
            this.configuration.DatasetName = "SAAC.pds";
            this.configuration.AutomaticPipelineRun = true;
            this.configuration.StoreMode = RendezVousPipeline.StoreMode.Dictionnary;

            // configuration.TopicsTypes.Add();
            // configuration.TypesSerializers.Add();
            // configuration.NotStoredTopics.Add();
            this.InitializeComponent();
        }

        public Dataset GetDataset()
        {
            this.server?.Dataset?.Save();
            return this.server?.Dataset;
        }

        public void RunPipeline(TimeInterval timeInterval)
        {
            this.server?.RunPipelineAndSubpipelines();
            this.server?.SendCommand(RendezVousPipeline.Command.Run, "All", string.Empty);
        }

        public void StopPipeline()
        {
            this.server?.Stop();
        }

        private void BtnStopClick(object sender, RoutedEventArgs e)
        {
            this.server?.Stop();
        }

        private void BtnStartClick(object sender, RoutedEventArgs e)
        {
            this.status = string.Empty;
            this.server = new RendezVousPipeline(this.configuration, "Server", null, (log) => { this.Status += $"{log}\n"; });
            this.server.Start();
        }

        private void BtnBrowseNameClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                this.DatasetPath = openFileDialog.FileName.Substring(0, openFileDialog.FileName.IndexOf(openFileDialog.SafeFileName));
                this.DatasetName = openFileDialog.SafeFileName;
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

        public bool IsReplayble()
        {
            return false;
        }

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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.Dispose();
        }

        public void Dispose()
        {
            this.server?.Pipeline.Dispose();
            this.Close();
        }

        public DateTime GetStartTime()
        {
            return this.server == null ? DateTime.MinValue : this.server.Pipeline.StartTime;
        }

        public PipelineReplaybleMode GetReplaybleMode()
        {
            return PipelineReplaybleMode.Not;
        }
    }
}
