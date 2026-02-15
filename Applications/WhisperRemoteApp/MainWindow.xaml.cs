// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Data;
using Microsoft.Psi.Speech;
using Microsoft.Win32;
using Newtonsoft.Json;
using SAAC;
using SAAC.AudioRecording;
using SAAC.PipelineServices;
using SAAC.RemoteConnectors;
using SAAC.Whisper;

namespace WhisperRemoteApp
{
    /// <summary>
    /// Main window for the Whisper Remote Application that manages speech recognition and audio processing.
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private List<User> audioSoucesSetup;
        private List<(string, int)> micsList = Microsoft.Psi.Audio.AudioCapture.GetAvailableDevicesWithChannels().ToList();
        private List<string> notTriggerProperties;

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
        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        if (propertyName != null && !this.notTriggerProperties.Contains(propertyName))
                        {
                            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
                        }
                    }));
                    field = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        #endregion

        private RendezVousPipelineConfiguration pipelineConfiguration;
        private SystemVoiceActivityDetectorConfiguration vadConfiguration;
        private SAAC.Whisper.WhisperSpeechRecognizerConfiguration whisperConfiguration;
        private SAAC.RemoteConnectors.WhisperRemoteStreamsConfiguration remoteConfiguration;
        private string state = "Not Initialised";
        private bool isRemoteServer = true;
        private bool isStreaming = true;
        private bool isWhisper = true;
        private bool isLocalRecording = true;
        private AudioSource selectedAudioSource = AudioSource.Microphones;
        private string audioSourceDatasetPath = string.Empty;
        private string audioSourceSessionName = string.Empty;
        private string rendezVousServerIp = "localhost";
        private string commandSource = "Server";
        private int commandPort;
        private string localSessionName = string.Empty;
        private SAAC.Whisper.WhisperAudioProcessing.LocalStorageMode localStoringMode = WhisperAudioProcessing.LocalStorageMode.All;
        private string localDatasetPath = string.Empty;
        private string localDatasetName = string.Empty;
        private string log = string.Empty;
        private SetupState setupState;
        private RendezVousPipeline? rendezVousPipeline;
        private Pipeline pipeline;
        private IAudioSourcesManager audioManager;
        private WhisperAudioProcessing? whisperAudioProcessing;
        private WhisperTranscriptionManager? transcriptionManager;
        private Dataset? localDataset;
        private bool isMessageBoxOpen;
        private readonly List<System.Speech.Recognition.RecognizerInfo> availableRecognisers;
        private LogStatus internalLog;
        private Dictionary<SAAC.Whisper.Language, string> whisperToVadLanguageConfiguration;

        /// <summary>
        /// Gets or sets the current application state.
        /// </summary>
        public string State
        {
            get => this.state;
            set => this.SetProperty(ref this.state, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the application is acting as a remote server.
        /// </summary>
        public bool IsRemoteServer
        {
            get => this.isRemoteServer;
            set => this.SetProperty(ref this.isRemoteServer, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether streaming is enabled.
        /// </summary>
        public bool IsStreaming
        {
            get => this.isStreaming;
            set => this.SetProperty(ref this.isStreaming, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether Whisper speech recognition is enabled.
        /// </summary>
        public bool IsWhisper
        {
            get => this.isWhisper;
            set => this.SetProperty(ref this.isWhisper, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether local recording is enabled.
        /// </summary>
        public bool IsLocalRecording
        {
            get => this.isLocalRecording;
            set => this.SetProperty(ref this.isLocalRecording, value);
        }

        /// <summary>
        /// Represents the available audio source types.
        /// </summary>
        public enum AudioSource
        {
            /// <summary>Microphones as audio source.</summary>
            Microphones,

            /// <summary>Wave files as audio source.</summary>
            WaveFiles,

            /// <summary>Dataset as audio source.</summary>
            Dataset,
        }

        /// <summary>
        /// Gets or sets the audio source dataset path.
        /// </summary>
        public string AudioSourceDatasetPath
        {
            get => this.audioSourceDatasetPath;
            set => this.SetProperty(ref this.audioSourceDatasetPath, value);
        }

        /// <summary>
        /// Gets or sets the audio source session name.
        /// </summary>
        public string AudioSourceSessionName
        {
            get => this.audioSourceSessionName;
            set => this.SetProperty(ref this.audioSourceSessionName, value);
        }

        /// <summary>
        /// Gets the list of available audio source types.
        /// </summary>
        public List<string> AudioSourceList { get; }

        /// <summary>
        /// Gets the list of available IP addresses.
        /// </summary>
        public List<string> IPsList { get; }

        /// <summary>
        /// Gets or sets the RendezVous server IP address.
        /// </summary>
        public string RendezVousServerIp
        {
            get => this.rendezVousServerIp;
            set => this.SetProperty(ref this.rendezVousServerIp, value);
        }

        /// <summary>
        /// Gets or sets the pipeline configuration.
        /// </summary>
        public RendezVousPipelineConfiguration PipelineConfigurationUI
        {
            get => this.pipelineConfiguration;
            set => this.SetProperty(ref this.pipelineConfiguration, value);
        }

        /// <summary>
        /// Gets or sets the Whisper remote streams configuration.
        /// </summary>
        public WhisperRemoteStreamsConfiguration WhisperRemoteStreamsConfigurationUI
        {
            get => this.remoteConfiguration;
            set => this.SetProperty(ref this.remoteConfiguration, value);
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
        /// Gets or sets the command port.
        /// </summary>
        public int CommandPort
        {
            get => this.commandPort;
            set => this.SetProperty(ref this.commandPort, value);
        }

        /// <summary>
        /// Gets or sets the VAD configuration.
        /// </summary>
        public SystemVoiceActivityDetectorConfiguration VadConfigurationUI
        {
            get => this.vadConfiguration;
            set => this.SetProperty(ref this.vadConfiguration, value);
        }

        /// <summary>
        /// Gets or sets the Whisper speech recognizer configuration.
        /// </summary>
        public WhisperSpeechRecognizerConfiguration WhisperConfigurationUI
        {
            get => this.whisperConfiguration;
            set => this.SetProperty(ref this.whisperConfiguration, value);
        }

        /// <summary>
        /// Gets the list of available Whisper models.
        /// </summary>
        public List<Whisper.net.Ggml.GgmlType> WhisperModelsList { get; }

        /// <summary>
        /// Gets the list of available Whisper quantization types.
        /// </summary>
        public List<Whisper.net.Ggml.QuantizationType> WhisperQuantizationList { get; }

        /// <summary>
        /// Gets the list of available Whisper languages.
        /// </summary>
        public List<SAAC.Whisper.Language> WhisperLanguageList { get; }

        /// <summary>
        /// Gets or sets the local session name for recording.
        /// </summary>
        public string LocalSessionName
        {
            get => this.localSessionName;
            set => this.SetProperty(ref this.localSessionName, value);
        }

        /// <summary>
        /// Gets or sets the local dataset path.
        /// </summary>
        public string LocalDatasetPath
        {
            get => this.localDatasetPath;
            set => this.SetProperty(ref this.localDatasetPath, value);
        }

        /// <summary>
        /// Gets or sets the local dataset name.
        /// </summary>
        public string LocalDatasetName
        {
            get => this.localDatasetName;
            set => this.SetProperty(ref this.localDatasetName, value);
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
        /// Represents the initialization state of the application.
        /// </summary>
        private enum SetupState
        {
            /// <summary>Application has not been initialized.</summary>
            NotInitialised,

            /// <summary>Pipeline has been initialized.</summary>
            PipelineInitialised,

            /// <summary>Audio sources have been initialized.</summary>
            AudioInitialised,

            /// <summary>Whisper speech recognition has been initialized.</summary>
            WhisperInitialised,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.internalLog = (log) =>
            {
                Application.Current?.Dispatcher?.Invoke(new Action(() =>
                {
                    this.Log += $"{log}\n";
                }));
            };
            this.audioSoucesSetup = new List<User>();
            this.notTriggerProperties = new List<string> { "Log", "State", "AudioSourceDatasetPath", "AudioSourceSessionName" };
            this.AudioSourceList = new List<string> { AudioSource.Microphones.ToString(), AudioSource.WaveFiles.ToString(), AudioSource.Dataset.ToString() };

            this.IPsList = new List<string> { "localhost" };
            this.IPsList.AddRange(Dns.GetHostEntry(Dns.GetHostName()).AddressList.Select(ip => ip.ToString()));

            this.WhisperModelsList = new List<Whisper.net.Ggml.GgmlType>(Enum.GetValues(typeof(Whisper.net.Ggml.GgmlType)).Cast<Whisper.net.Ggml.GgmlType>());
            this.WhisperQuantizationList = new List<Whisper.net.Ggml.QuantizationType>(Enum.GetValues(typeof(Whisper.net.Ggml.QuantizationType)).Cast<Whisper.net.Ggml.QuantizationType>());
            this.WhisperLanguageList = new List<SAAC.Whisper.Language>(Enum.GetValues(typeof(SAAC.Whisper.Language)).Cast<SAAC.Whisper.Language>());

            this.whisperToVadLanguageConfiguration = new Dictionary<SAAC.Whisper.Language, string> { { SAAC.Whisper.Language.NotSet, "en" }, { SAAC.Whisper.Language.Afrikaans, "af" }, { SAAC.Whisper.Language.Arabic, "ar" }, { SAAC.Whisper.Language.Armenian, "hy" }, { SAAC.Whisper.Language.Azerbaijani, "az" }, { SAAC.Whisper.Language.Belarusian, "be" }, { SAAC.Whisper.Language.Bosnian, "bs" }, { SAAC.Whisper.Language.Bulgarian, "bg" }, { SAAC.Whisper.Language.Catalan, "ca" }, { SAAC.Whisper.Language.Chinese, "zh" }, { SAAC.Whisper.Language.Croatian, "hr" }, { SAAC.Whisper.Language.Czech, "cs" }, { SAAC.Whisper.Language.Danish, "da" }, { SAAC.Whisper.Language.Dutch, "nl" }, { SAAC.Whisper.Language.English, "en" }, { SAAC.Whisper.Language.Estonian, "et" }, { SAAC.Whisper.Language.Finnish, "fi" }, { SAAC.Whisper.Language.French, "fr" }, { SAAC.Whisper.Language.Galician, "gl" }, { SAAC.Whisper.Language.German, "de" }, { SAAC.Whisper.Language.Greek, "el" }, { SAAC.Whisper.Language.Hebrew, "he" }, { SAAC.Whisper.Language.Hindi, "hi" }, { SAAC.Whisper.Language.Hungarian, "hu" }, { SAAC.Whisper.Language.Icelandic, "is" }, { SAAC.Whisper.Language.Indonesian, "id" }, { SAAC.Whisper.Language.Italian, "it" }, { SAAC.Whisper.Language.Japanese, "ja" }, { SAAC.Whisper.Language.Kannada, "kn" }, { SAAC.Whisper.Language.Kazakh, "kk" }, { SAAC.Whisper.Language.Korean, "ko" }, { SAAC.Whisper.Language.Latvian, "lv" }, { SAAC.Whisper.Language.Lithuanian, "lt" }, { SAAC.Whisper.Language.Macedonian, "mk" }, { SAAC.Whisper.Language.Malay, "ms" }, { SAAC.Whisper.Language.Marathi, "mr" }, { SAAC.Whisper.Language.Maori, "mi" }, { SAAC.Whisper.Language.Nepali, "ne" }, { SAAC.Whisper.Language.Norwegian, "no" }, { SAAC.Whisper.Language.Persian, "fa" }, { SAAC.Whisper.Language.Polish, "pl" }, { SAAC.Whisper.Language.Portuguese, "pt" }, { SAAC.Whisper.Language.Romanian, "ro" }, { SAAC.Whisper.Language.Russian, "ru-RU" }, { SAAC.Whisper.Language.Serbian, "sr" }, { SAAC.Whisper.Language.Slovak, "sk" }, { SAAC.Whisper.Language.Slovenian, "sl" }, { SAAC.Whisper.Language.Spanish, "es" }, { SAAC.Whisper.Language.Swahili, "sw" }, { SAAC.Whisper.Language.Swedish, "sv" }, { SAAC.Whisper.Language.Tagalog, "tl" }, { SAAC.Whisper.Language.Tamil, "ta" }, { SAAC.Whisper.Language.Thai, "th" }, { SAAC.Whisper.Language.Turkish, "tr" }, { SAAC.Whisper.Language.Ukrainian, "uk" }, { SAAC.Whisper.Language.Urdu, "ur" }, { SAAC.Whisper.Language.Vietnamese, "vi" }, { SAAC.Whisper.Language.Welsh, "cy" } };

            this.setupState = SetupState.NotInitialised;
            this.rendezVousPipeline = null;
            this.pipelineConfiguration = new RendezVousPipelineConfiguration();
            this.vadConfiguration = new SystemVoiceActivityDetectorConfiguration();
            this.whisperConfiguration = new WhisperSpeechRecognizerConfiguration();
            this.remoteConfiguration = new SAAC.RemoteConnectors.WhisperRemoteStreamsConfiguration();
            this.DataContext = this;

            this.isMessageBoxOpen = false;
            this.availableRecognisers = System.Speech.Recognition.SpeechRecognitionEngine.InstalledRecognizers().ToList();

            this.InitializeComponent();
            this.UpdateLayout();
            this.RefreshUIFromConfiguration();
            this.GenerateMicrophonesGrid();
            this.SetupGeneralTab();
            this.SetupAudioTab();
            this.SetupNetworkTab();
            this.SetupWhipserTab();
            this.SetupLocalRecordingTab();
        }

        private void SetupGeneralTab()
        {
            UiGenerator.SetTextBoxOutFocusChecker<Uri>(this.TranscriptionPathTextBox, UiGenerator.UriTryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<string>(this.TranscriptionFilenameTextBox, UiGenerator.PathTryParse);

            this.TranscriptionFilenameTextBox.LostFocus += UiGenerator.IsFileExistChecker("Transcription file already exist, please choose another name or path.", ".docx", this.TranscriptionPathTextBox);
        }

        private void SetupAudioTab()
        {
            UiGenerator.SetTextBoxOutFocusChecker<Uri>(this.AudioSourceDatasetTextBox, UiGenerator.UriTryParse);
            this.AddWavFile();
        }

        private void SetupNetworkTab()
        {
            UiGenerator.SetTextBoxOutFocusChecker<System.Net.IPAddress>(this.RendezVousServerIpTextBox, UiGenerator.IPAddressTryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.RendezVousPortTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.CommandPortTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.StreamingPortRangeStartTextBox, int.TryParse);
            this.UpdateNetworkTab();
        }

        private void SetupWhipserTab()
        {
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.VadBufferLengthTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.VadVoiceActivityStartOffsetTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.VadVoiceActivityEndOffsetTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.VadInitialSilenceTimeoutTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.VadBabbleTimeoutTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.VadEndSilenceTimeoutAmbiguousTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(this.VadEndSilenceTimeoutTextBox, int.TryParse);
            UiGenerator.SetTextBoxOutFocusChecker<Uri>(this.WhisperModelDirectoryTextBox, UiGenerator.UriTryParse);
            UiGenerator.SetTextBoxOutFocusChecker<Uri>(this.WhisperModelSpecficPathTextBox, UiGenerator.UriTryParse);
        }

        private void SetupLocalRecordingTab()
        {
            UiGenerator.SetTextBoxOutFocusChecker<Uri>(this.LocalRecordingDatasetDirectoryTextBox, UiGenerator.UriTryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<string>(this.LocalRecordingDatasetNameTextBox, UiGenerator.PathTryParse);
            this.LocalRecordingDatasetNameTextBox.LostFocus += UiGenerator.IsFileExistChecker("Dataset file already exist, make sure to use a different session name.", ".pds", this.LocalRecordingDatasetDirectoryTextBox);
        }

        private void UpdateNetworkTab()
        {
            this.BtnStartNet.IsEnabled = this.isRemoteServer;
            foreach (UIElement networkUIElement in this.NetworkGrid.Children)
            {
                if (!(networkUIElement is CheckBox))
                {
                    networkUIElement.IsEnabled = this.isRemoteServer;
                }
            }

            this.isStreaming = this.isRemoteServer ? this.isStreaming : false;
            this.GeneralNetworkStreamingCheckBox.IsEnabled = this.NetworkStreamingCheckBox.IsEnabled = this.isRemoteServer;
            if (this.isRemoteServer)
            {
                this.UpdateStreamingPortRangeStartTextBox();
            }
        }

        private void UpdateStreamingPortRangeStartTextBox()
        {
            this.GeneralNetworkStreamingCheckBox.IsChecked = this.NetworkStreamingCheckBox.IsChecked = this.isStreaming;
            this.StreamingPortRangeStartTextBox.IsEnabled = this.isStreaming & this.isRemoteServer;
        }

        private void UpdateWhisperTab()
        {
            this.WhiperGroupBox.IsEnabled = this.VADGroupBox.IsEnabled = this.isWhisper;
        }

        private void UpdateLocalRecordingTab()
        {
            foreach (UIElement networkUIElement in this.LocalRecordingGrid.Children)
            {
                if (!(networkUIElement is CheckBox))
                {
                    networkUIElement.IsEnabled = this.isLocalRecording;
                }
            }
        }

        private void GenerateMicrophonesGrid()
        {
            this.micsList = Microsoft.Psi.Audio.AudioCapture.GetAvailableDevicesWithChannels().ToList();

            int micId = -1;
            foreach (var mics in this.micsList)
            {
                micId++;
                if (mics.Item2 < 1)
                {
                    continue;
                }

                UiGenerator.AddRowsDefinitionToGrid(this.MicrophonesGrid, GridLength.Auto, 1);
                UiGenerator.SetElementInGrid(this.MicrophonesGrid, UiGenerator.GenerateLabel(mics.Item1), 0, this.MicrophonesGrid.RowDefinitions.Count - 1);

                Grid channelsGrid = UiGenerator.GenerateGrid(GridLength.Auto, 2, 0);
                UiGenerator.SetElementInGrid(this.MicrophonesGrid, channelsGrid, 1, this.MicrophonesGrid.RowDefinitions.Count - 1);
                for (int channel = 1; channel <= mics.Item2; channel++)
                {
                    UiGenerator.AddRowsDefinitionToGrid(channelsGrid, GridLength.Auto, 1);
                    UiGenerator.SetElementInGrid(channelsGrid, UiGenerator.GenerateLabel($"Channel {channel}"), 0, channelsGrid.RowDefinitions.Count - 1);
                    TextBox input = UiGenerator.GeneratorTextBox($"i{micId}_{channel}", 50.0);

                    // Add TextChanged handler to enable configuration buttons
                    input.TextChanged += (sender, e) =>
                    {
                        this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
                    };

                    UiGenerator.SetElementInGrid(channelsGrid, input, 1, channelsGrid.RowDefinitions.Count - 1);
                    User? oldConfig = this.audioSoucesSetup.SingleOrDefault((user) => { return user.Microphone == mics.Item1 && user.Channel == channel; });
                    if (oldConfig is null || oldConfig is default(User))
                    {
                        continue;
                    }

                    input.Text = oldConfig.Id.ToString();
                }
            }
        }

        private void RefreshUIFromConfiguration()
        {
            // Network Tab
            this.IsRemoteServer = Properties.Settings.Default.IsServer;
            this.IsStreaming = Properties.Settings.Default.IsStreaming;
            var ipResult = this.IPsList.Where((ip) => { return ip == Properties.Settings.Default.IpToUse; });
            this.RendezVousHostComboBox.SelectedIndex = ipResult.Count() == 0 ? 0 : this.IPsList.IndexOf(ipResult.First());
            this.PipelineConfigurationUI.RendezVousHost = Properties.Settings.Default.IpToUse;
            this.RendezVousServerIp = Properties.Settings.Default.RendezVousServerIp;
            this.PipelineConfigurationUI.RendezVousPort = (int)(Properties.Settings.Default.RendezVousServerPort);
            this.CommandSource = Properties.Settings.Default.CommandSource;
            this.CommandPort = Properties.Settings.Default.CommandPort;
            this.PipelineConfigurationUI.CommandPort = this.CommandPort;
            this.WhisperRemoteStreamsConfigurationUI.RendezVousApplicationName = Properties.Settings.Default.ApplicationName;
            this.WhisperRemoteStreamsConfigurationUI.ExportPort = (int)Properties.Settings.Default.RemotePort;

            // Audio Tab
            this.AudioSourceComboBox.SelectedIndex = Properties.Settings.Default.AudioSourceType;
            this.selectedAudioSource = (AudioSource)Properties.Settings.Default.AudioSourceType;

            // Load audio sources configuration from JSON
            this.LoadAudioSourcesFromJson();

            // Whisper Tab
            this.IsWhisper = Properties.Settings.Default.IsWhisper;
            this.VadConfigurationUI.BufferLengthInMs = Properties.Settings.Default.VadBufferLength;
            this.VadConfigurationUI.VoiceActivityStartOffsetMs = Properties.Settings.Default.VadStartOffset;
            this.VadConfigurationUI.VoiceActivityEndOffsetMs = Properties.Settings.Default.VadEndOffset;
            this.VadConfigurationUI.InitialSilenceTimeoutMs = Properties.Settings.Default.VadInitialSilenceTimeout;
            this.VadConfigurationUI.BabbleTimeoutMs = Properties.Settings.Default.VadBabbleTimeout;
            this.VadConfigurationUI.EndSilenceTimeoutAmbiguousMs = Properties.Settings.Default.VadEndSilenceTimeoutAmbigous;
            this.VadConfigurationUI.EndSilenceTimeoutMs = Properties.Settings.Default.VadEndSilenceTimeout;
            this.VadConfigurationUI.Language = Properties.Settings.Default.VadLanguage;

            this.LanguageComboBox.SelectedIndex = Properties.Settings.Default.WhisperLanguage;
            this.WhisperModelComboBox.SelectedIndex = Properties.Settings.Default.WhisperModelType;
            this.WhisperQuantizationComboBox.SelectedIndex = Properties.Settings.Default.WhisperQuantizationType;
            this.WhisperModelDirectoryTextBox.Text = this.WhisperConfigurationUI.ModelDirectory = Properties.Settings.Default.WhisperModelDirectory;
            this.WhisperModelSpecficPathTextBox.Text = this.WhisperConfigurationUI.SpecificModelPath = Properties.Settings.Default.WhisperSpecificModelPath;

            if (Properties.Settings.Default.WhipserModelType)
            {
                this.WhisperModelSpecific.IsChecked = true;
            }
            else
            {
                this.WhisperModelGeneric.IsChecked = true;
            }

            // Local Recording Tab
            this.IsLocalRecording = Properties.Settings.Default.IsLocalRecording;
            this.LocalSessionName = Properties.Settings.Default.LocalSessionName;
            switch (Properties.Settings.Default.LocalStoringMode)
            {
                case 1:
                    this.LocalStoringModeAudio.IsChecked = true;
                    this.localStoringMode = WhisperAudioProcessing.LocalStorageMode.AudioOnly;
                    break;
                case 2:
                    this.LocalStoringModeVADSTT.IsChecked = true;
                    this.localStoringMode = WhisperAudioProcessing.LocalStorageMode.VAD_STT;
                    break;
                case 3:
                    this.LocalStoringModeAll.IsChecked = true;
                    this.localStoringMode = WhisperAudioProcessing.LocalStorageMode.All;
                    break;
            }

            this.LocalDatasetPath = Properties.Settings.Default.LocalDatasetPath;
            this.LocalDatasetName = Properties.Settings.Default.LocalDatasetName;

            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = false;
        }

        private void RefreshConfigurationFromUI()
        {
            // Network Tab
            Properties.Settings.Default.IsServer = this.IsRemoteServer;
            Properties.Settings.Default.IsStreaming = this.IsStreaming;
            Properties.Settings.Default.IpToUse = this.pipelineConfiguration.RendezVousHost;
            Properties.Settings.Default.RendezVousServerIp = this.RendezVousServerIp;
            Properties.Settings.Default.RendezVousServerPort = (uint)this.PipelineConfigurationUI.RendezVousPort;
            Properties.Settings.Default.CommandSource = this.CommandSource;
            Properties.Settings.Default.CommandPort = this.CommandPort;
            Properties.Settings.Default.ApplicationName = this.WhisperRemoteStreamsConfigurationUI.RendezVousApplicationName;
            Properties.Settings.Default.RemotePort = (uint)this.WhisperRemoteStreamsConfigurationUI.ExportPort;

            // Audio Tab
            Properties.Settings.Default.AudioSourceType = this.AudioSourceComboBox.SelectedIndex;
            Properties.Settings.Default.AudioSourceDatasetPath = this.AudioSourceDatasetPath;
            Properties.Settings.Default.AudioSourceSessionName = this.AudioSourceSessionName;

            // Save audio sources configuration to JSON
            this.SaveAudioSourcesToJson();

            // Whisper Tab
            Properties.Settings.Default.IsWhisper = this.IsWhisper;
            Properties.Settings.Default.VadBufferLength = this.VadConfigurationUI.BufferLengthInMs;
            Properties.Settings.Default.VadStartOffset = this.VadConfigurationUI.VoiceActivityStartOffsetMs;
            Properties.Settings.Default.VadEndOffset = this.VadConfigurationUI.VoiceActivityEndOffsetMs;
            Properties.Settings.Default.VadInitialSilenceTimeout = this.VadConfigurationUI.InitialSilenceTimeoutMs;
            Properties.Settings.Default.VadBabbleTimeout = this.VadConfigurationUI.BabbleTimeoutMs;
            Properties.Settings.Default.VadEndSilenceTimeoutAmbigous = this.VadConfigurationUI.EndSilenceTimeoutAmbiguousMs;
            Properties.Settings.Default.VadEndSilenceTimeout = this.VadConfigurationUI.EndSilenceTimeoutMs;
            Properties.Settings.Default.VadLanguage = this.VadConfigurationUI.Language;

            Properties.Settings.Default.WhisperLanguage = this.LanguageComboBox.SelectedIndex;
            Properties.Settings.Default.WhisperModelType = this.WhisperModelComboBox.SelectedIndex;
            Properties.Settings.Default.WhisperQuantizationType = this.WhisperQuantizationComboBox.SelectedIndex;
            Properties.Settings.Default.WhisperModelDirectory = this.WhisperConfigurationUI.ModelDirectory;
            Properties.Settings.Default.WhisperSpecificModelPath = this.WhisperModelSpecficPathTextBox.Text;
            Properties.Settings.Default.WhipserModelType = (bool)this.WhisperModelSpecific.IsChecked;

            // Local Recording Tab
            Properties.Settings.Default.IsLocalRecording = this.IsLocalRecording;
            Properties.Settings.Default.LocalSessionName = this.LocalSessionName;
            Properties.Settings.Default.LocalStoringMode = (bool)this.LocalStoringModeAudio.IsChecked ? 1 : ((bool)this.LocalStoringModeVADSTT.IsChecked ? 2 : 3);
            Properties.Settings.Default.LocalDatasetPath = this.LocalDatasetPath;
            Properties.Settings.Default.LocalDatasetName = this.LocalDatasetName;

            Properties.Settings.Default.Save();
            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = false;
        }

        private void CommandRecieved(string source, Message<(RendezVousPipeline.Command, string)> message)
        {
            if ($"{this.CommandSource}-Command" != source)
            {
                return;
            }

            var args = message.Data.Item2.Split([';']);

            if (args[0] != this.WhisperRemoteStreamsConfigurationUI.RendezVousApplicationName && args[0] != "*")
            {
                return;
            }

            switch (message.Data.Item1)
            {
                case RendezVousPipeline.Command.Run:
                    this.isMessageBoxOpen = true;
                    this.Start();
                    this.Run();
                    break;
                case RendezVousPipeline.Command.Close:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        this.Close();
                    }));
                    break;
                case RendezVousPipeline.Command.Status:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        this.rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, this.CommandSource, this.rendezVousPipeline?.Pipeline.StartTime == DateTime.MinValue ? "Waiting" : "Running");
                    }));
                    break;
            }
        }

        private void SetupTranscription()
        {
            Application.Current?.Dispatcher?.Invoke(new Action(() =>
            {
                if (this.TranscriptionFilenameTextBox.Text.Length > 5)
                {
                    this.transcriptionManager = new WhipserTranscriptionToWordManager();
                }
            }));
        }

        private void SetupPipeline()
        {
            if (this.setupState >= SetupState.PipelineInitialised)
            {
                return;
            }

            if (this.isRemoteServer)
            {
                this.pipelineConfiguration.Diagnostics = DatasetPipeline.DiagnosticsMode.Off;
                this.pipelineConfiguration.AutomaticPipelineRun = false;
                this.pipelineConfiguration.CommandDelegate = this.CommandRecieved;
                this.pipelineConfiguration.Debug = false;
                this.pipelineConfiguration.RecordIncomingProcess = false;
                this.pipelineConfiguration.CommandPort = this.CommandPort;
                this.pipelineConfiguration.ClockPort = 0;
                if (this.isLocalRecording)
                {
                    this.pipelineConfiguration.DatasetPath = this.LocalDatasetPath;
                    this.pipelineConfiguration.DatasetName = this.LocalDatasetName;
                }

                this.rendezVousPipeline = new RendezVousPipeline(this.pipelineConfiguration, this.remoteConfiguration.RendezVousApplicationName, this.RendezVousServerIp, this.internalLog);

                this.pipeline = this.rendezVousPipeline.Pipeline;

                if (!this.isStreaming)
                {
                    this.rendezVousPipeline.AddProcess(new Microsoft.Psi.Interop.Rendezvous.Rendezvous.Process(this.remoteConfiguration.RendezVousApplicationName));
                }
            }
            else
            {
                this.pipeline = Pipeline.Create("WhisperPipeline");
            }

            this.setupState = SetupState.PipelineInitialised;
        }

        private void SetupAudioSources()
        {
            if (this.setupState >= SetupState.AudioInitialised)
            {
                return;
            }

            switch (this.selectedAudioSource)
            {
                case AudioSource.Microphones:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        this.GetMicrophonesConfiguration();
                    }));
                    break;
                case AudioSource.WaveFiles:
                    this.GetWaveFilesConfiguration();
                    break;
                case AudioSource.Dataset:
                    this.GetAudioSourceStreamConfiguration();
                    break;
            }

            if (this.audioSoucesSetup.Count > 0)
            {
                switch (this.selectedAudioSource)
                {
                    case AudioSource.Microphones:
                        {
                            AudioMicrophonesManager manager = new AudioMicrophonesManager(this.pipeline, false);
                            manager.AddUsers(this.audioSoucesSetup);
                            manager.SetupAudioWithoutRDV();
                            this.audioManager = manager;
                        }

                        break;
                    case AudioSource.WaveFiles:
                        {
                            AudioFilesManager manager = new AudioFilesManager(this.pipeline);
                            List<string> waveFilesSetup = this.audioSoucesSetup.Select((user) => { return user.Microphone; }).ToList();
                            manager.SetupAudioFromFiles(waveFilesSetup);
                            this.audioManager = manager;
                        }

                        break;
                    case AudioSource.Dataset:
                        {
                            AudioDatasetManager manager = new AudioDatasetManager(this.pipeline);
                            List<string> streamsSetup = this.audioSoucesSetup.Select((user) => { return user.Microphone; }).ToList();
                            manager.OpenAudioStreamsFromDataset(this.AudioSourceDatasetPath, streamsSetup, this.AudioSourceSessionName);
                            this.audioManager = manager;
                        }

                        break;
                }

                this.AddLog(this.State = "Audio initialised");
                if (this.isWhisper is false)
                {
                    if (this.isStreaming && this.rendezVousPipeline is not null)
                    {
                        Dictionary<string, ConnectorInfo> audioStreams = new Dictionary<string, ConnectorInfo>();
                        foreach (var userAudio in this.audioManager.GetDictonaryIdAudioStream())
                        {
                            Session? session = this.rendezVousPipeline.CreateOrGetSessionFromMode(this.PipelineConfigurationUI.SessionName);
                            var names = this.rendezVousPipeline.GetStoreName("Audio", $"Audio_User_{userAudio.Key}", session);
                            this.rendezVousPipeline.CreateConnectorAndStore(names.Item1, names.Item2, session, this.pipeline, typeof(AudioBuffer), userAudio.Value, this.IsLocalRecording);
                            audioStreams.Add($"Audio_User_{userAudio.Key}", this.rendezVousPipeline.Connectors[$"Audio_User_{userAudio.Key}"]["Audio"]);
                        }

                        Microsoft.Psi.Interop.Rendezvous.Rendezvous.Process process = new Microsoft.Psi.Interop.Rendezvous.Rendezvous.Process(this.WhisperRemoteStreamsConfigurationUI.RendezVousApplicationName, "Version1.0");
                        this.rendezVousPipeline.GenerateRemoteEnpoint(this.pipeline, this.remoteConfiguration.ExportPort, audioStreams, ref process);
                        this.rendezVousPipeline.AddProcess(process);
                    }
                    else if (this.IsLocalRecording)
                    {
                        Session session;
                        if (!this.GetLocalSession(out this.localDataset, out session))
                        {
                            this.AddLog(this.State = "Whisper initialised Failed");
                            this.AddLog("Unable to create session.");
                            return;
                        }

                        foreach (var userAudio in this.audioManager.GetDictonaryIdAudioStream())
                        {
                            PsiExporter store = PsiStore.Create(this.pipeline, $"Audio_User_{userAudio.Key}", $"{this.PipelineConfigurationUI.DatasetPath}/{session.Name}/");
                            session.AddPartitionFromPsiStoreAsync($"Audio_User_{userAudio.Key}", $"{this.PipelineConfigurationUI.DatasetPath}/{session.Name}/");
                            store.Write(userAudio.Value, "Audio");
                        }

                        this.localDataset.Save();
                    }
                }

                this.setupState = SetupState.AudioInitialised;
            }
            else
            {
                MessageBox.Show("Missing audio source configuration, see 'Audio Sources' tab.", "Configuration Missing", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
            }
        }

        private void SetupWhisper()
        {
            if (this.setupState >= SetupState.WhisperInitialised)
            {
                return;
            }

            if (this.isWhisper && this.audioManager != null)
            {
                try
                {
                    this.whisperConfiguration.OnModelDownloadProgressHandler = (obj, message) =>
                    {
                        switch (message.Item1)
                        {
                            case WhisperSpeechRecognizerConfiguration.EWhisperModelDownloadState.Failed:
                                this.setupState = SetupState.AudioInitialised;
                                this.rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, this.commandSource, "Error");
                                break;
                            case WhisperSpeechRecognizerConfiguration.EWhisperModelDownloadState.Completed:
                                this.rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, this.commandSource, "Running");
                                break;
                        }

                        if (this.isMessageBoxOpen)
                        {
                            return;
                        }

                        lock (this)
                        {
                            this.isMessageBoxOpen = true;
                            MessageBox.Show(message.Item2, message.Item1.ToString(), MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                            this.isMessageBoxOpen = false;
                        }
                    };

                    if (this.isStreaming)
                    {
                        var remoteWhisper = new SAAC.WhisperRemoteServices.WhiperRemoteComponent(this.rendezVousPipeline, this.vadConfiguration, this.whisperConfiguration, this.remoteConfiguration, null, this.transcriptionManager is null ? null : this.transcriptionManager.GetDelegate(), this.internalLog);
                        remoteWhisper.SetupWhisperAudioProcessing(this.audioManager.GetDictonaryIdAudioStream(), this.remoteConfiguration.RendezVousApplicationName, this.localStoringMode);
                        this.whisperAudioProcessing = remoteWhisper;
                    }
                    else
                    {
                        this.whisperAudioProcessing = new WhisperAudioProcessing(this.pipeline, this.vadConfiguration, this.whisperConfiguration, this.transcriptionManager is null ? null : this.transcriptionManager.GetDelegate(), this.internalLog);
                        if (this.IsLocalRecording && this.localStoringMode > WhisperAudioProcessing.LocalStorageMode.None)
                        {
                            Session session;
                            if (!this.GetLocalSession(out this.localDataset, out session))
                            {
                                this.AddLog(this.State = "Whisper initialised Failed");
                                this.AddLog("Unable to create session.");
                                return;
                            }

                            this.whisperAudioProcessing.SetupUsersWhisper(this.audioManager.GetDictonaryIdAudioStream(), ref session, this.LocalDatasetPath, this.localStoringMode);
                            this.localDataset.Save();
                        }
                        else
                        {
                            this.whisperAudioProcessing.SetupUsersWhisper(this.audioManager.GetDictonaryIdAudioStream());
                        }
                    }

                    this.AddLog(this.State = "Whisper initialised");
                    this.setupState = SetupState.WhisperInitialised;
                }
                catch (Exception ex)
                {
                    this.rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, this.commandSource, $"Error");
                    this.AddLog(this.State = "Whisper initialised Failed");
                    this.AddLog($"Error setting up Whisper: {ex.Message}");
                    MessageBox.Show("Unable to setup Whisper with the current configuration. Please check the settings in 'Whisper' tab.", "Whisper setup error", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                }
            }
        }

        private bool GetLocalDataset(out Dataset dataset)
        {
            try
            {
                string fullPath = Path.Combine(this.LocalDatasetPath, this.LocalDatasetName);
                if (File.Exists(fullPath))
                {
                    dataset = Dataset.Load(fullPath, true);
                }
                else
                {
                    dataset = new Dataset(Path.GetFileNameWithoutExtension(this.LocalDatasetName), fullPath, true);
                }

                dataset.Save();
            }
            catch (Exception ex)
            {
                this.rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, this.commandSource, $"Error");
                this.AddLog($"Error opening/creating local dataset: {ex.Message}");
                MessageBox.Show("Unable to create or open the local dataset. Please change the Dataset fields in 'Local Recording' tab", "Dataset error", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                dataset = null;
                return false;
            }

            return true;
        }

        private bool GetLocalSession(out Dataset dataset, out Session session)
        {
            if (!this.GetLocalDataset(out dataset))
            {
                session = null;
                return false;
            }

            if (dataset.Sessions.Where((s) => { return s.Name == this.LocalSessionName; }).Count() > 0)
            {
                session = null;
                MessageBox.Show("Unable to create session. Please change the Session Name in 'Local Recording' tab", "Session error", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                return false;
            }

            session = dataset.AddEmptySession(this.LocalSessionName);
            dataset.Save();
            return true;
        }

        private void AddLog(string logMessage)
        {
            this.Log += $"{logMessage}\n";
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

        private void Stop()
        {
            this.AddLog(this.State = "Stopping");
            this.rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, this.commandSource, "Stopping");
            this.localDataset?.Save();
            this.transcriptionManager?.WriteTranscription(Path.Combine(this.TranscriptionPathTextBox.Text, this.TranscriptionFilenameTextBox.Text));
            this.rendezVousPipeline?.RemoveProcess(this.WhisperRemoteStreamsConfigurationUI.RendezVousApplicationName);
            this.rendezVousPipeline?.Stop();
            this.audioManager?.Stop();
            this.whisperAudioProcessing?.Stop();
            if (this.rendezVousPipeline is not null)
            {
                this.rendezVousPipeline.Dispose();
            }
            else
            {
                this.pipeline?.Dispose();
            }

            Application.Current.Shutdown();
        }

        private void StartNetwork()
        {
            this.SetupPipeline();
            if (this.setupState == SetupState.PipelineInitialised)
            {
                this.BtnStartNet.IsEnabled = false;
                this.AddLog(this.State = "Waiting for server");
                this.rendezVousPipeline?.Start((d) =>
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        this.AddLog(this.State = "Connected to server");
                        this.rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, this.commandSource, "Waiting");
                    }));
                });
            }
        }

        private void Start()
        {
            this.SetupTranscription();
            this.SetupPipeline();
            this.SetupAudioSources();
            this.SetupWhisper();
            if ((this.setupState == SetupState.WhisperInitialised && this.isWhisper) || (this.setupState == SetupState.AudioInitialised && !this.isWhisper))
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    this.BtnStart.IsEnabled = this.BtnStartNet.IsEnabled = false;
                    this.Run();
                    this.AddLog(this.State = "Started");
                }));
            }
        }

        private void Run()
        {
            if (!((this.setupState == SetupState.WhisperInitialised && this.isWhisper) || (this.setupState == SetupState.AudioInitialised && !this.isWhisper)))
            {
                return;
            }

            if (this.rendezVousPipeline is null)
            {
                if (this.selectedAudioSource != AudioSource.Microphones)
                {
                    this.pipeline?.RunAsync(ReplayDescriptor.ReplayAllRealTime);
                    this.pipeline.PipelineCompleted += this.MainWindow_PipelineCompleted;
                }
                else
                {
                    this.pipeline?.RunAsync();
                }
            }
            else
            {
                this.rendezVousPipeline.RunPipelineAndSubpipelines();
            }

            if (this.setupState == SetupState.AudioInitialised)
            {
                this.rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, this.commandSource, "Running");
            }
        }

        private void MainWindow_PipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            switch (this.selectedAudioSource)
            {
                case AudioSource.Microphones:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        this.GetMicrophonesConfiguration();
                    }));
                    break;
                case AudioSource.WaveFiles:
                    MessageBox.Show("Processing from wavefile(s) completed. \nYou can Quit the application.", "Processing completed", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                    break;
                case AudioSource.Dataset:
                    MessageBox.Show("Processing from dataset completed. \nYou can Quit the application.", "Processing completed", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                    break;
            }

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                this.AddLog(this.State = "Precssing complete");
            }));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.Stop();
            base.OnClosing(e);
        }

        private void BtnStartRendezVous(object sender, RoutedEventArgs e)
        {
            this.StartNetwork();
            e.Handled = true;
        }

        private void BtnStartAll(object sender, RoutedEventArgs e)
        {
            this.Start();
            e.Handled = true;
        }

        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            this.Close();
            e.Handled = true;
        }

        private void BtnLoadConfiguration(object sender, RoutedEventArgs e)
        {
            this.RefreshUIFromConfiguration();
            this.AddLog(this.State = "Configuration Loaded");
            e.Handled = true;
        }

        private void BtnSaveConfiguration(object sender, RoutedEventArgs e)
        {
            this.RefreshConfigurationFromUI();
            this.AddLog(this.State = "Configuration Saved");
            e.Handled = true;
        }

        private void AudioSourceSelected(object sender, RoutedEventArgs e)
        {
            this.MicrophonesGroupBox.Visibility = this.WavFilesGroupBox.Visibility = this.AudioSourceDatasetGroupBox.Visibility = Visibility.Collapsed;
            switch (this.AudioSourceComboBox.SelectedIndex)
            {
                case 0:
                    this.selectedAudioSource = AudioSource.Microphones;
                    this.MicrophonesGroupBox.Visibility = Visibility.Visible;
                    this.GeneralNetworkActivationCheckbox.IsEnabled = this.GeneralNetworkStreamingCheckBox.IsEnabled = this.NetworkActivationCheckbox.IsEnabled = true;
                    break;
                case 1:
                    this.selectedAudioSource = AudioSource.WaveFiles;
                    this.WavFilesGroupBox.Visibility = Visibility.Visible;
                    this.GeneralNetworkStreamingCheckBox.IsChecked = this.GeneralNetworkActivationCheckbox.IsChecked = this.NetworkActivationCheckbox.IsChecked = this.GeneralNetworkStreamingCheckBox.IsEnabled = this.GeneralNetworkActivationCheckbox.IsEnabled = this.NetworkActivationCheckbox.IsEnabled = false;
                    break;
                case 2:
                    this.selectedAudioSource = AudioSource.Dataset;
                    this.AudioSourceDatasetGroupBox.Visibility = Visibility.Visible;
                    this.GeneralNetworkStreamingCheckBox.IsChecked = this.GeneralNetworkActivationCheckbox.IsChecked = this.NetworkActivationCheckbox.IsChecked = this.GeneralNetworkStreamingCheckBox.IsEnabled = this.GeneralNetworkActivationCheckbox.IsEnabled = this.NetworkActivationCheckbox.IsEnabled = false;
                    break;
            }

            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
        }

        private void BtnRefreshMicrophones(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                this.GetMicrophonesConfiguration();
            }));
            this.MicrophonesGrid.RowDefinitions.RemoveRange(2, this.MicrophonesGrid.RowDefinitions.Count - 3);
            foreach (UIElement element in this.MicrophonesGrid.Children)
            {
                if (element is Grid)
                {
                    Grid grid = element as Grid;
                    grid.Children.RemoveRange(0, grid.Children.Count);
                }
            }

            this.MicrophonesGrid.Children.RemoveRange(3, this.MicrophonesGrid.Children.Count - 4);
            this.GenerateMicrophonesGrid();
            e.Handled = true;
        }

        private void BtnAddWavFile(object sender, RoutedEventArgs e)
        {
            this.AddWavFile();
            e.Handled = true;
        }

        private void AddWavFile()
        {
            UiGenerator.AddRowsDefinitionToGrid(this.WaveFilesGrid, GridLength.Auto, 1);
            int position = this.WaveFilesGrid.RowDefinitions.Count - 1;
            TextBox waveFileTextBox = UiGenerator.GeneratePathTextBox(300.0, $"WaveFile_{position}");

            // Add TextChanged handler to enable configuration buttons
            waveFileTextBox.TextChanged += (sender, e) =>
            {
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            };

            UiGenerator.SetElementInGrid(this.WaveFilesGrid, waveFileTextBox, 0, this.WaveFilesGrid.RowDefinitions.Count - 1);
            UiGenerator.SetElementInGrid(this.WaveFilesGrid, UiGenerator.GenerateBrowseFilenameButton("Browse", waveFileTextBox, "Wave (*.wav)|*.wav"), 1, this.WaveFilesGrid.RowDefinitions.Count - 1);
            UiGenerator.SetElementInGrid(this.WaveFilesGrid, UiGenerator.GenerateButton("Remove", (sender, e) =>
            {
                UiGenerator.RemoveRowInGrid(this.WaveFilesGrid, position);
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
                e.Handled = true;
            }), 2, this.WaveFilesGrid.RowDefinitions.Count - 1);
        }

        private void AudioSourceDatasetButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Dataset (*.pds)|*.pds";
            if (openFileDialog.ShowDialog() == true)
            {
                this.AudioSourceDatasetPath = openFileDialog.FileName;
            }
        }

        private void BtnAudioSourceOpenDataset(object sender, RoutedEventArgs e)
        {
            this.AudioSourceDatasetStreamsGrid.Children.Clear();
            Dataset dataset = Dataset.Load(this.AudioSourceDatasetPath);
            foreach (Session session in dataset.Sessions)
            {
                if (this.audioSourceSessionName != null && session.Name != this.audioSourceSessionName)
                {
                    continue;
                }

                foreach (var partition in session.Partitions)
                {
                    foreach (var streamMetadata in partition.AvailableStreams)
                    {
                        if (typeof(AudioBuffer) != Type.GetType(streamMetadata.TypeName))
                        {
                            continue;
                        }

                        this.GenerateAudioSourceDatasetRowStream(streamMetadata.Name);
                    }
                }
            }

            e.Handled = true;
        }

        private void GenerateAudioSourceDatasetRowStream(string streamName)
        {
            UiGenerator.AddRowsDefinitionToGrid(this.AudioSourceDatasetStreamsGrid, GridLength.Auto, 1);
            CheckBox checkBox = UiGenerator.GenerateCheckBox(streamName, false, null, streamName);

            // Add Checked/Unchecked handlers to enable configuration buttons
            checkBox.Checked += (sender, e) =>
            {
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            };
            checkBox.Unchecked += (sender, e) =>
            {
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            };

            UiGenerator.SetElementInGrid(this.AudioSourceDatasetStreamsGrid, checkBox, 0, this.AudioSourceDatasetStreamsGrid.RowDefinitions.Count - 1);
        }

        private void CkbActivateNetwork(object sender, RoutedEventArgs e)
        {
            this.UpdateNetworkTab();
            e.Handled = true;
        }

        private void CkbActivateStreaming(object sender, RoutedEventArgs e)
        {
            this.UpdateStreamingPortRangeStartTextBox();
            e.Handled = true;
        }

        private void CkbActivateWhisper(object sender, RoutedEventArgs e)
        {
            this.UpdateWhisperTab();
            e.Handled = true;
        }

        private void CkbActivateLocalRecording(object sender, RoutedEventArgs e)
        {
            this.UpdateLocalRecordingTab();
            e.Handled = true;
        }

        private void RendezVousHostSelected(object sender, RoutedEventArgs e)
        {
            this.PipelineConfigurationUI.RendezVousHost = this.IPsList.ElementAt(this.RendezVousHostComboBox.SelectedIndex);
            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
        }

        private void WhisperLanguageSelected(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            // if (WhisperConfigurationUI.Language == (SAAC.Whisper.Language)LanguageComboBox.SelectedIndex)
            //    return;
            var results = this.availableRecognisers.Where(info => info.Culture.TwoLetterISOLanguageName == this.whisperToVadLanguageConfiguration[(SAAC.Whisper.Language)this.LanguageComboBox.SelectedIndex]);
            if (results.Count() == 0)
            {
                MessageBox.Show("Unable to find matching Windows recognition grammar for the selected Whisper language. Please install it first.", "Language selection", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                this.VadConfigurationUI.Language = System.Globalization.CultureInfo.CurrentCulture.Name;
                this.LanguageComboBox.SelectedIndex = (int)this.WhisperConfigurationUI.Language;
            }
            else
            {
                this.WhisperConfigurationUI.Language = (SAAC.Whisper.Language)this.LanguageComboBox.SelectedIndex;
                if (results.Count() > 1)
                {
                    var dialog = new CultureInfoWindow(results.ToList());
                    if (dialog.ShowDialog() == true)
                    {
                        this.VadConfigurationUI.Language = dialog.SelectedCulture;
                    }
                    else
                    {
                        this.LanguageComboBox.SelectedIndex = (int)this.WhisperConfigurationUI.Language;
                    }
                }
                else
                {
                    this.VadConfigurationUI.Language = results.First().Culture.Name;
                }
            }

            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
        }

        private void WhisperModelSelected(object sender, RoutedEventArgs e)
        {
            this.WhisperConfigurationUI.ModelType = (Whisper.net.Ggml.GgmlType)this.WhisperModelComboBox.SelectedIndex;
            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
        }

        private void WhisperQuantitzationSelected(object sender, RoutedEventArgs e)
        {
            this.WhisperConfigurationUI.QuantizationType = (Whisper.net.Ggml.QuantizationType)this.WhisperQuantizationComboBox.SelectedIndex;
            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
        }

        private void TranscriptionPathButtonClick(object sender, RoutedEventArgs e)
        {
            UiGenerator.FolderPicker openFileDialog = new UiGenerator.FolderPicker();
            if (openFileDialog.ShowDialog() == true)
            {
                this.TranscriptionPathTextBox.Text = openFileDialog.ResultName;
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            }
        }

        private void LocalRecordingDatasetDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            UiGenerator.FolderPicker openFileDialog = new UiGenerator.FolderPicker();
            if (openFileDialog.ShowDialog() == true)
            {
                this.LocalDatasetPath = openFileDialog.ResultName;
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            }
        }

        private void LocalRecordingDatasetNameButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Dataset (*.pds)|*.pds";
            if (openFileDialog.ShowDialog() == true)
            {
                this.LocalDatasetPath = openFileDialog.FileName.Substring(0, openFileDialog.FileName.IndexOf(openFileDialog.SafeFileName));
                this.LocalDatasetName = openFileDialog.SafeFileName;
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            }
        }

        private void WhisperModelChecked(object sender, RoutedEventArgs e)
        {
            RadioButton button = (RadioButton)sender;
            if (button.Name.Contains("Generic"))
            {
                this.WhipserGenericModelConfiguration.Visibility = Visibility.Visible;
                this.WhipserSpecificModelConfiguration.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.WhipserGenericModelConfiguration.Visibility = Visibility.Collapsed;
                this.WhipserSpecificModelConfiguration.Visibility = Visibility.Visible;
            }

            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
        }

        private void WhisperModelDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            UiGenerator.FolderPicker openFileDialog = new UiGenerator.FolderPicker();
            if (openFileDialog.ShowDialog() == true)
            {
                this.WhisperModelDirectoryTextBox.Text = this.WhisperConfigurationUI.ModelDirectory = openFileDialog.ResultName;
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            }
        }

        private void WhisperModelSpecificPathButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Binary (*.bin)|*.bin";
            if (openFileDialog.ShowDialog() == true)
            {
                this.WhisperConfigurationUI.SpecificModelPath = this.WhisperModelSpecficPathTextBox.Text = openFileDialog.FileName;
                this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
            }
        }

        private void LocalStoringModeChecked(object sender, RoutedEventArgs e)
        {
            RadioButton button = (RadioButton)sender;
            switch (button.Name)
            {
                case "LocalStoringModeAudio":
                    this.localStoringMode = WhisperAudioProcessing.LocalStorageMode.AudioOnly;
                    break;

                case "LocalStoringModeVADSTT":
                    this.localStoringMode = WhisperAudioProcessing.LocalStorageMode.VAD_STT;
                    break;

                case "LocalStoringModeAll":
                    this.localStoringMode = WhisperAudioProcessing.LocalStorageMode.All;
                    break;
            }

            this.BtnLoadConfig.IsEnabled = this.BtnSaveConfig.IsEnabled = true;
        }

        private void GetMicrophonesConfiguration()
        {
            this.audioSoucesSetup.Clear();
            foreach (UIElement gridElement in this.MicrophonesGrid.Children)
            {
                if (gridElement is Grid)
                {
                    Grid? grid = gridElement as Grid;
                    foreach (UIElement element in grid?.Children)
                    {
                        if (element is TextBox)
                        {
                            TextBox? inputText = element as TextBox;
                            if (inputText is null || inputText.Text.Length < 1)
                            {
                                continue;
                            }

                            string[] micAndChannel = inputText.Name.Split('_');
                            int micId;
                            int.TryParse(micAndChannel[0].Substring(1), out micId);
                            int channel;
                            int.TryParse(micAndChannel[1], out channel);
                            this.audioSoucesSetup.Add(new User(inputText.Text, this.micsList.ElementAt(micId).Item1, channel));
                        }
                    }
                }
            }
        }

        private void GetWaveFilesConfiguration()
        {
            this.audioSoucesSetup.Clear();
            foreach (UIElement element in this.WaveFilesGrid.Children)
            {
                if (element is TextBox)
                {
                    TextBox? inputText = element as TextBox;
                    if (inputText is null || inputText.Text.Length < 1)
                    {
                        continue;
                    }

                    this.audioSoucesSetup.Add(new User(inputText.Text, inputText.Text, 1));
                }
            }
        }

        private void GetAudioSourceStreamConfiguration()
        {
            this.audioSoucesSetup.Clear();
            foreach (UIElement gridElement in this.AudioSourceDatasetStreamsGrid.Children)
            {
                if (gridElement is CheckBox)
                {
                    CheckBox? checkBox = gridElement as CheckBox;
                    if (checkBox != null && checkBox.IsChecked == true)
                    {
                        this.audioSoucesSetup.Add(new User(checkBox.Name, checkBox.Name, 1));
                    }
                }
            }
        }

        private void LoadAudioSourcesFromJson()
        {
            try
            {
                // Load dataset path and session name from Settings
                this.AudioSourceDatasetPath = Properties.Settings.Default.AudioSourceDatasetPath;
                this.AudioSourceSessionName = Properties.Settings.Default.AudioSourceSessionName;
                this.AudioSourceDatasetTextBox.Text = this.AudioSourceDatasetPath;
                this.AudioSourceSessionNameTextBox.Text = this.AudioSourceSessionName;

                // Load microphone configurations
                string audioSourcesJson = Properties.Settings.Default.AudioSourcesJson;
                if (!string.IsNullOrEmpty(audioSourcesJson))
                {
                    var loadedSources = JsonConvert.DeserializeObject<List<AudioSourceConfig>>(audioSourcesJson);
                    if (loadedSources != null)
                    {
                        this.audioSoucesSetup.Clear();
                        foreach (var source in loadedSources)
                        {
                            this.audioSoucesSetup.Add(new User(source.Id, source.Microphone, source.Channel));
                        }
                    }
                }

                // Load wave files
                string waveFilesJson = Properties.Settings.Default.WaveFilesJson;
                if (!string.IsNullOrEmpty(waveFilesJson))
                {
                    var loadedWaveFiles = JsonConvert.DeserializeObject<List<string>>(waveFilesJson);
                    if (loadedWaveFiles != null && this.selectedAudioSource == AudioSource.WaveFiles)
                    {
                        // Clear existing wave file rows (except the first empty one)
                        this.WaveFilesGrid.RowDefinitions.Clear();
                        this.WaveFilesGrid.Children.Clear();

                        foreach (var waveFile in loadedWaveFiles)
                        {
                            this.AddWavFile();

                            // Set the text of the last added TextBox
                            var lastTextBox = this.WaveFilesGrid.Children.OfType<TextBox>().LastOrDefault();
                            if (lastTextBox != null)
                            {
                                lastTextBox.Text = waveFile;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.AddLog($"Error loading audio sources from JSON: {ex.Message}");
            }
        }

        private void SaveAudioSourcesToJson()
        {
            try
            {
                // Save dataset path and session name
                this.AudioSourceDatasetPath = this.AudioSourceDatasetTextBox.Text;
                this.AudioSourceSessionName = this.AudioSourceSessionNameTextBox.Text;
                Properties.Settings.Default.AudioSourceDatasetPath = this.AudioSourceDatasetPath;
                Properties.Settings.Default.AudioSourceSessionName = this.AudioSourceSessionName;

                // Save microphone configurations
                switch (this.selectedAudioSource)
                {
                    case AudioSource.Microphones:
                        this.GetMicrophonesConfiguration();
                        break;
                    case AudioSource.WaveFiles:
                        this.GetWaveFilesConfiguration();
                        break;
                    case AudioSource.Dataset:
                        this.GetAudioSourceStreamConfiguration();
                        break;
                }

                var audioSourceConfigs = this.audioSoucesSetup.Select(u => new AudioSourceConfig
                {
                    Id = u.Id,
                    Microphone = u.Microphone,
                    Channel = u.Channel
                }).ToList();
                Properties.Settings.Default.AudioSourcesJson = JsonConvert.SerializeObject(audioSourceConfigs);

                // Save wave files
                var waveFiles = new List<string>();
                foreach (UIElement element in this.WaveFilesGrid.Children)
                {
                    if (element is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
                    {
                        waveFiles.Add(textBox.Text);
                    }
                }

                Properties.Settings.Default.WaveFilesJson = JsonConvert.SerializeObject(waveFiles);
            }
            catch (Exception ex)
            {
                this.AddLog($"Error saving audio sources to JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper class for JSON serialization of audio source configuration.
        /// </summary>
        private class AudioSourceConfig
        {
            public string Id { get; set; }

            public string Microphone { get; set; }

            public int Channel { get; set; }
        }
    }
}
