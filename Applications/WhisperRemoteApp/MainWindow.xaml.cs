

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
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace WhisperRemoteApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private List<User> audioSoucesSetup;
        private List<(string,int)> micsList = Microsoft.Psi.Audio.AudioCapture.GetAvailableDevicesWithChannels().ToList();
        private List<string> notTriggerProperties;

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {

            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
            {
                if(Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        if (propertyName != null && !notTriggerProperties.Contains(propertyName))
                        {
                            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
                        }
                    }));
                    field = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }
        #endregion

        private RendezVousPipelineConfiguration pipelineConfiguration;
        private SystemVoiceActivityDetectorConfiguration vadConfiguration;
        private SAAC.Whisper.WhisperSpeechRecognizerConfiguration whisperConfiguration;
        private SAAC.RemoteConnectors.WhisperRemoteStreamsConfiguration remoteConfiguration;

        //General Tab
        private string state = "Not Initialised";
        public string State
        {
            get => state;
            set => SetProperty(ref state, value);
        }

        private bool isRemoteServer = true;
        public bool IsRemoteServer
        {
            get => isRemoteServer;
            set => SetProperty(ref isRemoteServer, value);
        }

        private bool isStreaming = true;
        public bool IsStreaming
        {
            get => isStreaming;
            set => SetProperty(ref isStreaming, value);
        }

        private bool isWhisper = true;
        public bool IsWhisper
        {
            get => isWhisper;
            set => SetProperty(ref isWhisper, value);
        }

        private bool isLocalRecording = true;
        public bool IsLocalRecording
        {
            get => isLocalRecording;
            set => SetProperty(ref isLocalRecording, value);
        }

        // Audio Tab
        public enum AudioSource
        {
            Microphones,
            WaveFiles,
            Dataset
        }

        private AudioSource selectedAudioSource = AudioSource.Microphones;

        private string audioSourceDatasetPath = "";
        public string AudioSourceDatasetPath
        {
            get => audioSourceDatasetPath;
            set => SetProperty(ref audioSourceDatasetPath, value);
        }

        private string audioSourceSessionName = "";
        public string AudioSourceSessionName
        {
            get => audioSourceSessionName;
            set => SetProperty(ref audioSourceSessionName, value);
        }

        public List<string> AudioSourceList { get; }

        // Network Tab
        public List<string> IPsList { get; }

        private string rendezVousServerIp = "localhost";
        public string RendezVousServerIp
        {
            get => rendezVousServerIp;
            set => SetProperty(ref rendezVousServerIp, value);
        }

        public RendezVousPipelineConfiguration PipelineConfigurationUI
        {
            get => pipelineConfiguration;
            set => SetProperty(ref pipelineConfiguration, value);
        }

        public WhisperRemoteStreamsConfiguration WhisperRemoteStreamsConfigurationUI
        {
            get => remoteConfiguration;
            set => SetProperty(ref remoteConfiguration, value);
        }

        private string commandSource = "Server";
        public string CommandSource
        {
            get => commandSource;
            set => SetProperty(ref commandSource, value);
        }

        private int commandPort;
        public int CommandPort
        {
            get => commandPort;
            set => SetProperty(ref commandPort, value);
        }

        //Whipser Tab

        public SystemVoiceActivityDetectorConfiguration VadConfigurationUI
        {
            get => vadConfiguration;
            set => SetProperty(ref vadConfiguration, value);
        }

        public WhisperSpeechRecognizerConfiguration WhisperConfigurationUI
        {
            get => whisperConfiguration;
            set => SetProperty(ref whisperConfiguration, value);
        }

        public List<Whisper.net.Ggml.GgmlType> WhisperModelsList { get; }
        public List<Whisper.net.Ggml.QuantizationType> WhisperQuantizationList { get; }
        public List<SAAC.Whisper.Language> WhisperLanguageList { get; }
        private Dictionary<SAAC.Whisper.Language, string> whisperToVadLanguageConfiguration;

        // LocalRecording Tab

        private string localSessionName = "";
        public string LocalSessionName
        {
            get => localSessionName;
            set => SetProperty(ref localSessionName, value);
        }

        private SAAC.Whisper.WhisperAudioProcessing.LocalStorageMode localStoringMode = WhisperAudioProcessing.LocalStorageMode.All;

        private string localDatasetPath = "";
        public string LocalDatasetPath
        {
            get => localDatasetPath;
            set => SetProperty(ref localDatasetPath, value);
        }

        private string localDatasetName = "";
        public string LocalDatasetName
        {
            get => localDatasetName;
            set => SetProperty(ref localDatasetName, value);
        }

        // Log Tab
        private string log = "";
        public string Log
        {
            get => log;
            set => SetProperty(ref log, value);
        }


        // varialbles
        private enum SetupState
        {
            NotInitialised,
            PipelineInitialised,
            AudioInitialised,
            WhisperInitialised
        };
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

        public MainWindow()
        {
            internalLog = (log) =>
            {
                Application.Current?.Dispatcher?.Invoke(new Action(() =>
                {
                    Log += $"{log}\n";
                }));
            };
            audioSoucesSetup = new List<User>();
            notTriggerProperties = new List<string> { "Log", "State", "AudioSourceDatasetPath", "AudioSourceSessionName" };
            AudioSourceList = new List<string> { AudioSource.Microphones.ToString(), AudioSource.WaveFiles.ToString(), AudioSource.Dataset.ToString() };
            
            IPsList = new List<string>{ "localhost" };
            IPsList.AddRange(Dns.GetHostEntry(Dns.GetHostName()).AddressList.Select(ip => ip.ToString()));

            WhisperModelsList = new List<Whisper.net.Ggml.GgmlType>(Enum.GetValues(typeof(Whisper.net.Ggml.GgmlType)).Cast<Whisper.net.Ggml.GgmlType>());
            WhisperQuantizationList = new List<Whisper.net.Ggml.QuantizationType>(Enum.GetValues(typeof(Whisper.net.Ggml.QuantizationType)).Cast<Whisper.net.Ggml.QuantizationType>());
            WhisperLanguageList = new List<SAAC.Whisper.Language>(Enum.GetValues(typeof(SAAC.Whisper.Language)).Cast<SAAC.Whisper.Language>());
   
            whisperToVadLanguageConfiguration = new Dictionary<SAAC.Whisper.Language, string> { { SAAC.Whisper.Language.NotSet, "en" }, { SAAC.Whisper.Language.Afrikaans, "af" }, { SAAC.Whisper.Language.Arabic, "ar" }, { SAAC.Whisper.Language.Armenian, "hy" }, { SAAC.Whisper.Language.Azerbaijani, "az" }, { SAAC.Whisper.Language.Belarusian, "be" }, { SAAC.Whisper.Language.Bosnian, "bs" }, { SAAC.Whisper.Language.Bulgarian, "bg" }, { SAAC.Whisper.Language.Catalan, "ca" }, { SAAC.Whisper.Language.Chinese, "zh" }, { SAAC.Whisper.Language.Croatian, "hr" }, { SAAC.Whisper.Language.Czech, "cs" }, { SAAC.Whisper.Language.Danish, "da" }, { SAAC.Whisper.Language.Dutch, "nl" }, { SAAC.Whisper.Language.English, "en" }, { SAAC.Whisper.Language.Estonian, "et" }, { SAAC.Whisper.Language.Finnish, "fi" }, { SAAC.Whisper.Language.French, "fr" }, { SAAC.Whisper.Language.Galician, "gl" }, { SAAC.Whisper.Language.German, "de" }, { SAAC.Whisper.Language.Greek, "el" }, { SAAC.Whisper.Language.Hebrew, "he" }, { SAAC.Whisper.Language.Hindi, "hi" }, { SAAC.Whisper.Language.Hungarian, "hu" }, { SAAC.Whisper.Language.Icelandic, "is" }, { SAAC.Whisper.Language.Indonesian, "id" }, { SAAC.Whisper.Language.Italian, "it" }, { SAAC.Whisper.Language.Japanese, "ja" }, { SAAC.Whisper.Language.Kannada, "kn" }, { SAAC.Whisper.Language.Kazakh, "kk" }, { SAAC.Whisper.Language.Korean, "ko" }, { SAAC.Whisper.Language.Latvian, "lv" }, { SAAC.Whisper.Language.Lithuanian, "lt" }, { SAAC.Whisper.Language.Macedonian, "mk" }, { SAAC.Whisper.Language.Malay, "ms" }, { SAAC.Whisper.Language.Marathi, "mr" }, { SAAC.Whisper.Language.Maori, "mi" }, { SAAC.Whisper.Language.Nepali, "ne" }, { SAAC.Whisper.Language.Norwegian, "no" }, { SAAC.Whisper.Language.Persian, "fa" }, { SAAC.Whisper.Language.Polish, "pl" }, { SAAC.Whisper.Language.Portuguese, "pt" }, { SAAC.Whisper.Language.Romanian, "ro" }, { SAAC.Whisper.Language.Russian, "ru-RU" }, { SAAC.Whisper.Language.Serbian, "sr" }, { SAAC.Whisper.Language.Slovak, "sk" }, { SAAC.Whisper.Language.Slovenian, "sl" }, { SAAC.Whisper.Language.Spanish, "es" }, { SAAC.Whisper.Language.Swahili, "sw" }, { SAAC.Whisper.Language.Swedish, "sv" }, { SAAC.Whisper.Language.Tagalog, "tl" }, { SAAC.Whisper.Language.Tamil, "ta" }, { SAAC.Whisper.Language.Thai, "th" }, { SAAC.Whisper.Language.Turkish, "tr" }, { SAAC.Whisper.Language.Ukrainian, "uk" }, { SAAC.Whisper.Language.Urdu, "ur" }, { SAAC.Whisper.Language.Vietnamese, "vi" }, { SAAC.Whisper.Language.Welsh, "cy" } };

            setupState = SetupState.NotInitialised;
            rendezVousPipeline = null;
            pipelineConfiguration = new RendezVousPipelineConfiguration();
            vadConfiguration = new SystemVoiceActivityDetectorConfiguration();
            whisperConfiguration = new WhisperSpeechRecognizerConfiguration();
            remoteConfiguration = new SAAC.RemoteConnectors.WhisperRemoteStreamsConfiguration();
            DataContext = this;

            isMessageBoxOpen = false;
            availableRecognisers = System.Speech.Recognition.SpeechRecognitionEngine.InstalledRecognizers().ToList();

            InitializeComponent();
            UpdateLayout();
            RefreshUIFromConfiguration();
            GenerateMicrophonesGrid();
            SetupGeneralTab();
            SetupAudioTab();
            SetupNetworkTab();
            SetupWhipserTab();
            SetupLocalRecordingTab();
        }
   
        private void SetupGeneralTab()
        {
            UiGenerator.SetTextBoxOutFocusChecker<Uri>(TranscriptionPathTextBox, UiGenerator.UriTryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<string>(TranscriptionFilenameTextBox, UiGenerator.PathTryParse);

            TranscriptionFilenameTextBox.LostFocus += UiGenerator.IsFileExistChecker("Transcription file already exist, please choose another name or path.", ".docx", TranscriptionPathTextBox);
        }

        private void SetupAudioTab()
        {
            UiGenerator.SetTextBoxOutFocusChecker<Uri>(AudioSourceDatasetTextBox, UiGenerator.UriTryParse);
            AddWavFile();
        }

        private void SetupNetworkTab()
        {
            UiGenerator.SetTextBoxOutFocusChecker<System.Net.IPAddress>(RendezVousServerIpTextBox, UiGenerator.IPAddressTryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(RendezVousPortTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(CommandPortTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(StreamingPortRangeStartTextBox, int.TryParse);
            UpdateNetworkTab();
        }

        private void SetupWhipserTab()
        {
            UiGenerator.SetTextBoxPreviewTextChecker<int>(VadBufferLengthTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(VadVoiceActivityStartOffsetTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(VadVoiceActivityEndOffsetTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(VadInitialSilenceTimeoutTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(VadBabbleTimeoutTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(VadEndSilenceTimeoutAmbiguousTextBox, int.TryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<int>(VadEndSilenceTimeoutTextBox, int.TryParse);
            UiGenerator.SetTextBoxOutFocusChecker<Uri>(WhisperModelDirectoryTextBox, UiGenerator.UriTryParse);
            UiGenerator.SetTextBoxOutFocusChecker<Uri>(WhisperModelSpecficPathTextBox, UiGenerator.UriTryParse);
        }

        private void SetupLocalRecordingTab()
        {
            UiGenerator.SetTextBoxOutFocusChecker<Uri>(LocalRecordingDatasetDirectoryTextBox, UiGenerator.UriTryParse);
            UiGenerator.SetTextBoxPreviewTextChecker<string>(LocalRecordingDatasetNameTextBox, UiGenerator.PathTryParse);
            LocalRecordingDatasetNameTextBox.LostFocus += UiGenerator.IsFileExistChecker("Dataset file already exist, make sure to use a different session name.", ".pds", LocalRecordingDatasetDirectoryTextBox);
        }

        private void UpdateNetworkTab()
        {
            BtnStartNet.IsEnabled = isRemoteServer;
            foreach (UIElement networkUIElement in NetworkGrid.Children)
                if (!(networkUIElement is CheckBox))
                    networkUIElement.IsEnabled = isRemoteServer;
            isStreaming = isRemoteServer ? isStreaming : false;
            GeneralNetworkStreamingCheckBox.IsEnabled = NetworkStreamingCheckBox.IsEnabled = isRemoteServer;
            if (isRemoteServer)
                UpdateStreamingPortRangeStartTextBox();
        } 
        private void UpdateStreamingPortRangeStartTextBox()
        {
            GeneralNetworkStreamingCheckBox.IsChecked = NetworkStreamingCheckBox.IsChecked = isStreaming;
            StreamingPortRangeStartTextBox.IsEnabled = isStreaming & isRemoteServer;
        }

        private void UpdateWhisperTab()
        {
            WhiperGroupBox.IsEnabled = VADGroupBox.IsEnabled = isWhisper;
        }

        private void UpdateLocalRecordingTab()
        {
            foreach (UIElement networkUIElement in LocalRecordingGrid.Children)
                if (!(networkUIElement is CheckBox))
                    networkUIElement.IsEnabled = isLocalRecording;
        }

        private void GenerateMicrophonesGrid()
        {
            micsList = Microsoft.Psi.Audio.AudioCapture.GetAvailableDevicesWithChannels().ToList();
            
            int micId = -1;
            foreach (var mics in micsList)
            {
                micId++;
                if (mics.Item2 < 1)
                    continue;

                UiGenerator.AddRowsDefinitionToGrid(MicrophonesGrid, GridLength.Auto, 1);
                UiGenerator.SetElementInGrid(MicrophonesGrid, UiGenerator.GenerateLabel(mics.Item1), 0, MicrophonesGrid.RowDefinitions.Count - 1);
               
                Grid channelsGrid = UiGenerator.GenerateGrid(GridLength.Auto, 2, 0);
                UiGenerator.SetElementInGrid(MicrophonesGrid, channelsGrid, 1, MicrophonesGrid.RowDefinitions.Count - 1);
                for (int channel = 1; channel <= mics.Item2; channel++)
                {
                    UiGenerator.AddRowsDefinitionToGrid(channelsGrid, GridLength.Auto, 1);
                    UiGenerator.SetElementInGrid(channelsGrid, UiGenerator.GenerateLabel($"Channel {channel}"), 0, channelsGrid.RowDefinitions.Count - 1);
                    TextBox input = UiGenerator.GeneratorTextBox($"i{micId}_{channel}", 50.0);
                    
                    // Add TextChanged handler to enable configuration buttons
                    input.TextChanged += (sender, e) =>
                    {
                        BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
                    };
                    
                    UiGenerator.SetElementInGrid(channelsGrid, input, 1, channelsGrid.RowDefinitions.Count - 1);
                    User? oldConfig = audioSoucesSetup.SingleOrDefault((user) => { return user.Microphone == mics.Item1 && user.Channel == channel; });
                    if (oldConfig is null || oldConfig is default(User))
                        continue;
                    input.Text = oldConfig.Id.ToString();
                }
            }
        }

        private void RefreshUIFromConfiguration()
        {
            // Network Tab
            IsRemoteServer = Properties.Settings.Default.IsServer;
            IsStreaming = Properties.Settings.Default.IsStreaming;
            var ipResult = IPsList.Where((ip) => { return ip == Properties.Settings.Default.IpToUse; });
            RendezVousHostComboBox.SelectedIndex = ipResult.Count() == 0 ? 0 : IPsList.IndexOf(ipResult.First());
            PipelineConfigurationUI.RendezVousHost = Properties.Settings.Default.IpToUse;
            RendezVousServerIp = Properties.Settings.Default.RendezVousServerIp;
            PipelineConfigurationUI.RendezVousPort = (int)(Properties.Settings.Default.RendezVousServerPort);
            CommandSource = Properties.Settings.Default.CommandSource;
            CommandPort = Properties.Settings.Default.CommandPort;
            PipelineConfigurationUI.CommandPort = CommandPort;
            WhisperRemoteStreamsConfigurationUI.RendezVousApplicationName = Properties.Settings.Default.ApplicationName;
            WhisperRemoteStreamsConfigurationUI.ExportPort = (int)Properties.Settings.Default.RemotePort;

            // Audio Tab
            AudioSourceComboBox.SelectedIndex = Properties.Settings.Default.AudioSourceType;
            selectedAudioSource = (AudioSource)Properties.Settings.Default.AudioSourceType;
            
            // Load audio sources configuration from JSON
            LoadAudioSourcesFromJson();


            // Whisper Tab
            IsWhisper = Properties.Settings.Default.IsWhisper;
            VadConfigurationUI.BufferLengthInMs = Properties.Settings.Default.VadBufferLength;
            VadConfigurationUI.VoiceActivityStartOffsetMs = Properties.Settings.Default.VadStartOffset;
            VadConfigurationUI.VoiceActivityEndOffsetMs = Properties.Settings.Default.VadEndOffset;
            VadConfigurationUI.InitialSilenceTimeoutMs = Properties.Settings.Default.VadInitialSilenceTimeout;
            VadConfigurationUI.BabbleTimeoutMs = Properties.Settings.Default.VadBabbleTimeout;
            VadConfigurationUI.EndSilenceTimeoutAmbiguousMs = Properties.Settings.Default.VadEndSilenceTimeoutAmbigous;
            VadConfigurationUI.EndSilenceTimeoutMs = Properties.Settings.Default.VadEndSilenceTimeout;
            VadConfigurationUI.Language = Properties.Settings.Default.VadLanguage; 

            LanguageComboBox.SelectedIndex = Properties.Settings.Default.WhisperLanguage;
            WhisperModelComboBox.SelectedIndex = Properties.Settings.Default.WhisperModelType;
            WhisperQuantizationComboBox.SelectedIndex = Properties.Settings.Default.WhisperQuantizationType;
            WhisperModelDirectoryTextBox.Text = WhisperConfigurationUI.ModelDirectory = Properties.Settings.Default.WhisperModelDirectory;
            WhisperModelSpecficPathTextBox.Text = WhisperConfigurationUI.SpecificModelPath = Properties.Settings.Default.WhisperSpecificModelPath;

            if (Properties.Settings.Default.WhipserModelType)
            {
                WhisperModelSpecific.IsChecked = true;
            }
            else
            {
                WhisperModelGeneric.IsChecked = true;
            }

            // Local Recording Tab
            IsLocalRecording = Properties.Settings.Default.IsLocalRecording;
            LocalSessionName = Properties.Settings.Default.LocalSessionName;
            switch (Properties.Settings.Default.LocalStoringMode)
            {
                case 1:
                    LocalStoringModeAudio.IsChecked = true;
                    localStoringMode = WhisperAudioProcessing.LocalStorageMode.AudioOnly;
                    break;
                case 2:
                    LocalStoringModeVADSTT.IsChecked = true;
                    localStoringMode = WhisperAudioProcessing.LocalStorageMode.VAD_STT;
                    break;
                case 3:
                    LocalStoringModeAll.IsChecked = true;
                    localStoringMode = WhisperAudioProcessing.LocalStorageMode.All;
                    break;
            }
            LocalDatasetPath = Properties.Settings.Default.LocalDatasetPath;
            LocalDatasetName = Properties.Settings.Default.LocalDatasetName;

            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = false;
        }

        private void RefreshConfigurationFromUI()
        {
            // Network Tab
            Properties.Settings.Default.IsServer = IsRemoteServer;
            Properties.Settings.Default.IsStreaming = IsStreaming;
            Properties.Settings.Default.IpToUse = pipelineConfiguration.RendezVousHost;
            Properties.Settings.Default.RendezVousServerIp = RendezVousServerIp;
            Properties.Settings.Default.RendezVousServerPort = (uint)PipelineConfigurationUI.RendezVousPort;
            Properties.Settings.Default.CommandSource = CommandSource;
            Properties.Settings.Default.CommandPort = CommandPort;
            Properties.Settings.Default.ApplicationName = WhisperRemoteStreamsConfigurationUI.RendezVousApplicationName;
            Properties.Settings.Default.RemotePort = (uint)WhisperRemoteStreamsConfigurationUI.ExportPort;

            // Audio Tab
            Properties.Settings.Default.AudioSourceType = AudioSourceComboBox.SelectedIndex;
            Properties.Settings.Default.AudioSourceDatasetPath = AudioSourceDatasetPath;
            Properties.Settings.Default.AudioSourceSessionName = AudioSourceSessionName;
            
            // Save audio sources configuration to JSON
            SaveAudioSourcesToJson();

            // Whisper Tab
            Properties.Settings.Default.IsWhisper = IsWhisper;
            Properties.Settings.Default.VadBufferLength = VadConfigurationUI.BufferLengthInMs;
            Properties.Settings.Default.VadStartOffset = VadConfigurationUI.VoiceActivityStartOffsetMs;
            Properties.Settings.Default.VadEndOffset = VadConfigurationUI.VoiceActivityEndOffsetMs;
            Properties.Settings.Default.VadInitialSilenceTimeout = VadConfigurationUI.InitialSilenceTimeoutMs;
            Properties.Settings.Default.VadBabbleTimeout = VadConfigurationUI.BabbleTimeoutMs;
            Properties.Settings.Default.VadEndSilenceTimeoutAmbigous = VadConfigurationUI.EndSilenceTimeoutAmbiguousMs;
            Properties.Settings.Default.VadEndSilenceTimeout = VadConfigurationUI.EndSilenceTimeoutMs;
            Properties.Settings.Default.VadLanguage = VadConfigurationUI.Language;

            Properties.Settings.Default.WhisperLanguage = LanguageComboBox.SelectedIndex;
            Properties.Settings.Default.WhisperModelType = WhisperModelComboBox.SelectedIndex;
            Properties.Settings.Default.WhisperQuantizationType = WhisperQuantizationComboBox.SelectedIndex;
            Properties.Settings.Default.WhisperModelDirectory = WhisperConfigurationUI.ModelDirectory;
            Properties.Settings.Default.WhisperSpecificModelPath = WhisperModelSpecficPathTextBox.Text;
            Properties.Settings.Default.WhipserModelType = (bool)WhisperModelSpecific.IsChecked;

            // Local Recording Tab
            Properties.Settings.Default.IsLocalRecording = IsLocalRecording;
            Properties.Settings.Default.LocalSessionName = LocalSessionName;
            Properties.Settings.Default.LocalStoringMode = (bool)LocalStoringModeAudio.IsChecked ? 1 : ((bool)LocalStoringModeVADSTT.IsChecked ? 2 : 3);
            Properties.Settings.Default.LocalDatasetPath = LocalDatasetPath;
            Properties.Settings.Default.LocalDatasetName = LocalDatasetName;
            
            Properties.Settings.Default.Save();
            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = false;
        }

        private void CommandRecieved(string source, Message<(RendezVousPipeline.Command, string)> message)
        {
            if ($"{CommandSource}-Command" != source)
                return; 
            
            var args = message.Data.Item2.Split([';']);

            if (args[0] != WhisperRemoteStreamsConfigurationUI.RendezVousApplicationName && args[0] != "*")
                return;

            switch (message.Data.Item1)
            {
                case RendezVousPipeline.Command.Run:
                    isMessageBoxOpen = true;
                    Start();
                    Run();
                    break;
                case RendezVousPipeline.Command.Close:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        Close();
                    }));
                    break;
                case RendezVousPipeline.Command.Status:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, source, rendezVousPipeline == null ? "Not Initialised" : rendezVousPipeline.Pipeline.StartTime.ToString());
                    }));
                    break;
            }
        }

        private void SetupTranscription()
        {
            Application.Current?.Dispatcher?.Invoke(new Action(() =>
            {
                if (TranscriptionFilenameTextBox.Text.Length > 5)
                    transcriptionManager = new WhipserTranscriptionToWordManager();
            }));
        }

        private void SetupPipeline()
        {
            if (setupState >= SetupState.PipelineInitialised) 
                return;
            if (isRemoteServer)
            { 
                pipelineConfiguration.Diagnostics = DatasetPipeline.DiagnosticsMode.Off;
                pipelineConfiguration.AutomaticPipelineRun = false;
                pipelineConfiguration.CommandDelegate = CommandRecieved;
                pipelineConfiguration.Debug = false;
                pipelineConfiguration.RecordIncomingProcess = false;
                pipelineConfiguration.CommandPort = CommandPort;
                pipelineConfiguration.ClockPort = 0;
                if (isLocalRecording)
                {
                    pipelineConfiguration.DatasetPath = LocalDatasetPath;
                    pipelineConfiguration.DatasetName = LocalDatasetName;
                }

                rendezVousPipeline = new RendezVousPipeline(pipelineConfiguration, remoteConfiguration.RendezVousApplicationName, RendezVousServerIp, internalLog);

                pipeline = rendezVousPipeline.Pipeline;

                if (!isStreaming)
                    rendezVousPipeline.AddProcess(new Microsoft.Psi.Interop.Rendezvous.Rendezvous.Process(remoteConfiguration.RendezVousApplicationName));
            }
            else
                pipeline = Pipeline.Create("WhisperPipeline");
            setupState = SetupState.PipelineInitialised;
        }

        private void SetupAudioSources()
        {
            if (setupState >= SetupState.AudioInitialised)
                return;
            switch (selectedAudioSource)
            {
                case AudioSource.Microphones:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        GetMicrophonesConfiguration();
                    }));
                    break;
                case AudioSource.WaveFiles:
                    GetWaveFilesConfiguration();
                    break;
                case AudioSource.Dataset:
                    GetAudioSourceStreamConfiguration();
                    break;
            }
            if (audioSoucesSetup.Count > 0)
            {
                switch(selectedAudioSource)
                {
                    case AudioSource.Microphones:
                        {
                            AudioMicrophonesManager manager = new AudioMicrophonesManager(pipeline, false);
                            manager.AddUsers(audioSoucesSetup);
                            manager.SetupAudioWithoutRDV();
                            audioManager = manager;
                        }
                        break;
                    case AudioSource.WaveFiles:
                        {
                            AudioFilesManager manager = new AudioFilesManager(pipeline); 
                            List<string> waveFilesSetup = audioSoucesSetup.Select((user) => { return user.Microphone; }).ToList();
                            manager.SetupAudioFromFiles(waveFilesSetup);
                            audioManager = manager;
                        }
                        break;
                    case AudioSource.Dataset:
                        {
                            AudioDatasetManager manager = new AudioDatasetManager(pipeline);
                            List<string> streamsSetup = audioSoucesSetup.Select((user) => { return user.Microphone; }).ToList();
                            manager.OpenAudioStreamsFromDataset(AudioSourceDatasetPath, streamsSetup, AudioSourceSessionName);
                            audioManager = manager;
                        }
                        break;
                }
                AddLog(State = "Audio initialised");
                if (isWhisper is false)
                {
                    if (isStreaming && rendezVousPipeline is not null)
                    {
                        Dictionary<string, ConnectorInfo> audioStreams = new Dictionary<string, ConnectorInfo>();
                        foreach (var userAudio in audioManager.GetDictonaryIdAudioStream())
                        {
                            rendezVousPipeline.CreateConnectorAndStore("Audio", $"Audio_User_{userAudio.Key}", rendezVousPipeline.CreateOrGetSessionFromMode(PipelineConfigurationUI.SessionName), pipeline, typeof(AudioBuffer), userAudio.Value, IsLocalRecording);
                            audioStreams.Add($"Audio_User_{userAudio.Key}", rendezVousPipeline.Connectors[$"Audio_User_{userAudio.Key}"]["Audio"]);
                        }
                        Microsoft.Psi.Interop.Rendezvous.Rendezvous.Process process = new Microsoft.Psi.Interop.Rendezvous.Rendezvous.Process(WhisperRemoteStreamsConfigurationUI.RendezVousApplicationName, "Version1.0");
                        rendezVousPipeline.GenerateRemoteEnpoint(pipeline, remoteConfiguration.ExportPort, audioStreams, ref process);
                        rendezVousPipeline.AddProcess(process);
                    }
                    else if (IsLocalRecording)
                    {
                        Session session;
                        if (!GetLocalSession(out localDataset, out session))
                        {
                            AddLog(State = "Whisper initialised Failed");
                            AddLog("Unable to create session.");
                            return;
                        }

                        foreach (var userAudio in audioManager.GetDictonaryIdAudioStream())
                        {
                            PsiExporter store = PsiStore.Create(pipeline, $"Audio_User_{userAudio.Key}", $"{PipelineConfigurationUI.DatasetPath}/{session.Name}/");
                            session.AddPartitionFromPsiStoreAsync($"Audio_User_{userAudio.Key}", $"{PipelineConfigurationUI.DatasetPath}/{session.Name}/");
                            store.Write(userAudio.Value, "Audio");
                        }

                        localDataset.Save();
                    }
                }
                    
                setupState = SetupState.AudioInitialised;
            }
            else
                MessageBox.Show("Missing audio source configuration, see 'Audio Sources' tab.", "Configuration Missing", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
        }

        private void SetupWhisper()
        {
            if (setupState >= SetupState.WhisperInitialised)
                return;
            if (isWhisper && audioManager != null)
            {
                try
                {
                    whisperConfiguration.OnModelDownloadProgressHandler = (obj, message) =>
                    {
                        switch (message.Item1)
                        {
                            case WhisperSpeechRecognizerConfiguration.EWhisperModelDownloadState.Failed:
                                setupState = SetupState.AudioInitialised;
                                rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, commandSource, "Error");
                                break;
                            case WhisperSpeechRecognizerConfiguration.EWhisperModelDownloadState.Completed:
                                rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, commandSource, "Running");
                                break;
                        }

                        if (isMessageBoxOpen)
                            return;
                        lock (this)
                        {
                            isMessageBoxOpen = true; 
                            MessageBox.Show(message.Item2, message.Item1.ToString(), MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                            isMessageBoxOpen = false;
                        }
                    };
              
                    if (isStreaming)
                    {
                        var remoteWhisper = new SAAC.WhisperRemoteServices.WhiperRemoteComponent(rendezVousPipeline, vadConfiguration, whisperConfiguration, remoteConfiguration, null, transcriptionManager is null ? null : transcriptionManager.GetDelegate(), internalLog);
                        remoteWhisper.SetupWhisperAudioProcessing(audioManager.GetDictonaryIdAudioStream(), remoteConfiguration.RendezVousApplicationName, localStoringMode);
                        whisperAudioProcessing = remoteWhisper;
                    }
                    else
                    {
                        whisperAudioProcessing = new WhisperAudioProcessing(pipeline, vadConfiguration, whisperConfiguration, transcriptionManager is null ? null : transcriptionManager.GetDelegate(), internalLog);
                        if (IsLocalRecording && localStoringMode > WhisperAudioProcessing.LocalStorageMode.None)
                        {
                            Session session;
                            if (!GetLocalSession(out localDataset, out session))
                            {
                                AddLog(State = "Whisper initialised Failed");
                                AddLog("Unable to create session.");
                                return;
                            }
                            whisperAudioProcessing.SetupUsersWhisper(audioManager.GetDictonaryIdAudioStream(), ref session, LocalDatasetPath, localStoringMode);
                            localDataset.Save();
                        }
                        else
                            whisperAudioProcessing.SetupUsersWhisper(audioManager.GetDictonaryIdAudioStream());
                    }
                    AddLog(State = "Whisper initialised");
                    setupState = SetupState.WhisperInitialised;
                }
                catch (Exception ex)
                {
                    rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, commandSource, $"Error");
                    AddLog(State = "Whisper initialised Failed");
                    AddLog($"Error setting up Whisper: {ex.Message}");
                    MessageBox.Show("Unable to setup Whisper with the current configuration. Please check the settings in 'Whisper' tab.", "Whisper setup error", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                }
            }
        }

        private bool GetLocalDataset(out Dataset dataset)
        {
            try
            {
                string fullPath = Path.Combine(LocalDatasetPath, LocalDatasetName);
                if (File.Exists(fullPath))
                    dataset = Dataset.Load(fullPath, true);
                else
                    dataset = new Dataset(Path.GetFileNameWithoutExtension(LocalDatasetName), fullPath, true);
                dataset.Save();
            }
            catch (Exception ex)
            {
                rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, commandSource, $"Error");
                AddLog($"Error opening/creating local dataset: {ex.Message}");
                MessageBox.Show("Unable to create or open the local dataset. Please change the Dataset fields in 'Local Recording' tab", "Dataset error", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                dataset = null;
                return false;
            }
            return true;
        }

        private bool GetLocalSession(out Dataset dataset, out Session session)
        {
            if (!GetLocalDataset(out dataset))
            {
                session = null;
                return false;
            }
            if (dataset.Sessions.Where((s) => { return s.Name == LocalSessionName; }).Count() > 0)
            {
                session = null;
                MessageBox.Show("Unable to create session. Please change the Session Name in 'Local Recording' tab", "Session error", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                return false;
            }
            session = dataset.AddEmptySession(LocalSessionName);
            dataset.Save();
            return true;
        }

        private void AddLog(string logMessage)
        {
            Log += $"{logMessage}\n";
        }

        private void Log_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            TextBox? log = sender as TextBox;
            if (log == null)
                return;
            log.CaretIndex = log.Text.Length;
            log.ScrollToEnd();
        }

        private void Stop()
        {
            AddLog(State = "Stopping");
            rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, commandSource, "Stopping");
            localDataset?.Save();
            transcriptionManager?.WriteTranscription(Path.Combine(TranscriptionPathTextBox.Text, TranscriptionFilenameTextBox.Text));
            rendezVousPipeline?.RemoveProcess(WhisperRemoteStreamsConfigurationUI.RendezVousApplicationName);
            rendezVousPipeline?.Stop();
            audioManager?.Stop();
            whisperAudioProcessing?.Stop();
            if (rendezVousPipeline is not null)
            {
                rendezVousPipeline.Dispose();
            }
            else
            {
                pipeline?.Dispose();
            }
            Application.Current.Shutdown();
        }

        private void StartNetwork()
        {
            SetupPipeline();
            if (setupState == SetupState.PipelineInitialised)
            {
                BtnStartNet.IsEnabled = false;
                AddLog(State = "Waiting for server");
                rendezVousPipeline?.Start((d) => { Application.Current.Dispatcher.Invoke(new Action(() => { AddLog(State = "Connected to server");
                    rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, commandSource, "Waiting");
                })); });
            }
        }

        private void Start()
        {
            SetupTranscription();
            SetupPipeline();
            SetupAudioSources();
            SetupWhisper();
            if ((setupState == SetupState.WhisperInitialised && isWhisper) || (setupState == SetupState.AudioInitialised && !isWhisper))
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    BtnStart.IsEnabled = BtnStartNet.IsEnabled = false;
                    Run();
                    AddLog(State = "Started");
                }));
            }
        }

        private void Run()
        {
            if (!((setupState == SetupState.WhisperInitialised && isWhisper) || (setupState == SetupState.AudioInitialised && !isWhisper)))
                return;
            if (rendezVousPipeline is null)
            { 
                if (selectedAudioSource != AudioSource.Microphones)
                {
                    pipeline?.RunAsync(ReplayDescriptor.ReplayAllRealTime);
                    pipeline.PipelineCompleted += MainWindow_PipelineCompleted;
                }
                else
                    pipeline?.RunAsync();
            }
            else
                rendezVousPipeline.RunPipelineAndSubpipelines();
            if (setupState == SetupState.AudioInitialised)
                rendezVousPipeline?.SendCommand(RendezVousPipeline.Command.Status, commandSource, "Running");
        }

        private void MainWindow_PipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            switch(selectedAudioSource)
            {
                case AudioSource.Microphones:
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        GetMicrophonesConfiguration();
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
                AddLog(State = "Precssing complete");
            }));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Stop();
            base.OnClosing(e);
        }

        private void BtnStartRendezVous(object sender, RoutedEventArgs e)
        {
            StartNetwork();
            e.Handled = true;
        }

        private void BtnStartAll(object sender, RoutedEventArgs e)
        {
            Start();
            e.Handled = true;
        }

        private void BtnQuitClick(object sender, RoutedEventArgs e)
        {
            Close();
            e.Handled = true;
        }

        private void BtnLoadConfiguration(object sender, RoutedEventArgs e)
        {
            RefreshUIFromConfiguration();
            AddLog(State = "Configuration Loaded");
            e.Handled = true;
        }

        private void BtnSaveConfiguration(object sender, RoutedEventArgs e)
        {
            RefreshConfigurationFromUI();
            AddLog(State = "Configuration Saved");
            e.Handled = true;
        }

        private void AudioSourceSelected(object sender, RoutedEventArgs e)
        {
            MicrophonesGroupBox.Visibility = WavFilesGroupBox.Visibility = AudioSourceDatasetGroupBox.Visibility = Visibility.Collapsed;
            switch (AudioSourceComboBox.SelectedIndex)
            {
                case 0:
                    selectedAudioSource = AudioSource.Microphones;
                    MicrophonesGroupBox.Visibility = Visibility.Visible;
                    GeneralNetworkActivationCheckbox.IsEnabled = GeneralNetworkStreamingCheckBox.IsEnabled = NetworkActivationCheckbox.IsEnabled = true;
            break;
                case 1:
                    selectedAudioSource = AudioSource.WaveFiles;
                    WavFilesGroupBox.Visibility = Visibility.Visible;
                    GeneralNetworkStreamingCheckBox.IsChecked = GeneralNetworkActivationCheckbox.IsChecked = NetworkActivationCheckbox.IsChecked = GeneralNetworkStreamingCheckBox.IsEnabled = GeneralNetworkActivationCheckbox.IsEnabled = NetworkActivationCheckbox.IsEnabled =  false;
                    break;
                case 2:
                    selectedAudioSource = AudioSource.Dataset;
                    AudioSourceDatasetGroupBox.Visibility = Visibility.Visible;
                    GeneralNetworkStreamingCheckBox.IsChecked = GeneralNetworkActivationCheckbox.IsChecked = NetworkActivationCheckbox.IsChecked = GeneralNetworkStreamingCheckBox.IsEnabled = GeneralNetworkActivationCheckbox.IsEnabled = NetworkActivationCheckbox.IsEnabled = false;
                    break;
            }

            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
        }

        private void BtnRefreshMicrophones(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                GetMicrophonesConfiguration();
            }));
            MicrophonesGrid.RowDefinitions.RemoveRange(2, MicrophonesGrid.RowDefinitions.Count - 3);
            foreach(UIElement element in MicrophonesGrid.Children)
            {
                if (element is Grid)
                {
                    Grid grid = element as Grid;
                    grid.Children.RemoveRange(0, grid.Children.Count);
                }
            }
            MicrophonesGrid.Children.RemoveRange(3, MicrophonesGrid.Children.Count - 4);
            GenerateMicrophonesGrid();
            e.Handled = true;
        }

        private void BtnAddWavFile(object sender, RoutedEventArgs e)
        {
            AddWavFile();
            e.Handled = true;
        }

        private void AddWavFile()
        {
            UiGenerator.AddRowsDefinitionToGrid(WaveFilesGrid, GridLength.Auto, 1);
            int position = WaveFilesGrid.RowDefinitions.Count - 1;
            TextBox waveFileTextBox = UiGenerator.GeneratePathTextBox(300.0, $"WaveFile_{position}");
            
            // Add TextChanged handler to enable configuration buttons
            waveFileTextBox.TextChanged += (sender, e) =>
            {
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            };
            
            UiGenerator.SetElementInGrid(WaveFilesGrid, waveFileTextBox, 0, WaveFilesGrid.RowDefinitions.Count - 1);
            UiGenerator.SetElementInGrid(WaveFilesGrid, UiGenerator.GenerateBrowseFilenameButton("Browse", waveFileTextBox, "Wave (*.wav)|*.wav"), 1, WaveFilesGrid.RowDefinitions.Count - 1);
            UiGenerator.SetElementInGrid(WaveFilesGrid, UiGenerator.GenerateButton("Remove", (sender, e) => 
            { 
                UiGenerator.RemoveRowInGrid(WaveFilesGrid, position); 
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
                e.Handled = true; 
            }), 2, WaveFilesGrid.RowDefinitions.Count - 1);
        }

        private void AudioSourceDatasetButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Dataset (*.pds)|*.pds";
            if (openFileDialog.ShowDialog() == true)
                AudioSourceDatasetPath = openFileDialog.FileName;
        }

        private void BtnAudioSourceOpenDataset(object sender, RoutedEventArgs e)
        {
            AudioSourceDatasetStreamsGrid.Children.Clear();
            Dataset dataset = Dataset.Load(AudioSourceDatasetPath);
            foreach (Session session in dataset.Sessions)
            {
                if (audioSourceSessionName != null && session.Name != audioSourceSessionName)
                    continue;
                foreach (var partition in session.Partitions)
                {
                    foreach (var streamMetadata in partition.AvailableStreams)
                    {
                        if (typeof(AudioBuffer) != Type.GetType(streamMetadata.TypeName))
                            continue;
                        GenerateAudioSourceDatasetRowStream(streamMetadata.Name);
                    }
                }
            }
            e.Handled = true;
        }

        private void GenerateAudioSourceDatasetRowStream(string streamName)
        {
            UiGenerator.AddRowsDefinitionToGrid(AudioSourceDatasetStreamsGrid, GridLength.Auto, 1);
            CheckBox checkBox = UiGenerator.GenerateCheckBox(streamName, false, null, streamName);
            
            // Add Checked/Unchecked handlers to enable configuration buttons
            checkBox.Checked += (sender, e) =>
            {
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            };
            checkBox.Unchecked += (sender, e) =>
            {
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            };
            
            UiGenerator.SetElementInGrid(AudioSourceDatasetStreamsGrid, checkBox, 0, AudioSourceDatasetStreamsGrid.RowDefinitions.Count - 1);
        }

        private void CkbActivateNetwork(object sender, RoutedEventArgs e)
        {
            UpdateNetworkTab();
            e.Handled = true;
        }

        private void CkbActivateStreaming(object sender, RoutedEventArgs e)
        {
            UpdateStreamingPortRangeStartTextBox();
            e.Handled = true;
        }

        private void CkbActivateWhisper(object sender, RoutedEventArgs e)
        {
            UpdateWhisperTab();
            e.Handled = true;
        }

        private void CkbActivateLocalRecording(object sender, RoutedEventArgs e)
        {
            UpdateLocalRecordingTab();
            e.Handled = true;
        }

        private void RendezVousHostSelected(object sender, RoutedEventArgs e)
        {
            PipelineConfigurationUI.RendezVousHost = IPsList.ElementAt(RendezVousHostComboBox.SelectedIndex);
            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
        }

        private void WhisperLanguageSelected(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            //if (WhisperConfigurationUI.Language == (SAAC.Whisper.Language)LanguageComboBox.SelectedIndex)
            //    return;
            var results = availableRecognisers.Where(info => info.Culture.TwoLetterISOLanguageName == whisperToVadLanguageConfiguration[(SAAC.Whisper.Language)LanguageComboBox.SelectedIndex]);
            if (results.Count() == 0)
            {
                MessageBox.Show("Unable to find matching Windows recognition grammar for the selected Whisper language. Please install it first.", "Language selection", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                VadConfigurationUI.Language = System.Globalization.CultureInfo.CurrentCulture.Name;
                LanguageComboBox.SelectedIndex = (int)WhisperConfigurationUI.Language;
            }
            else
            {
                WhisperConfigurationUI.Language = (SAAC.Whisper.Language)LanguageComboBox.SelectedIndex;
                if (results.Count() > 1)
                {
                    var dialog = new CultureInfoWindow(results.ToList());
                    if (dialog.ShowDialog() == true)
                        VadConfigurationUI.Language = dialog.SelectedCulture;
                    else
                        LanguageComboBox.SelectedIndex = (int)WhisperConfigurationUI.Language;
                }
                else
                    VadConfigurationUI.Language = results.First().Culture.Name;
            }

            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
        }

        private void WhisperModelSelected(object sender, RoutedEventArgs e)
        {
            WhisperConfigurationUI.ModelType = (Whisper.net.Ggml.GgmlType)WhisperModelComboBox.SelectedIndex;
            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
        }

        private void WhisperQuantitzationSelected(object sender, RoutedEventArgs e)
        {
            WhisperConfigurationUI.QuantizationType = (Whisper.net.Ggml.QuantizationType)WhisperQuantizationComboBox.SelectedIndex;
            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
        }

        private void TranscriptionPathButtonClick(object sender, RoutedEventArgs e)
        {
            UiGenerator.FolderPicker openFileDialog = new UiGenerator.FolderPicker();
            if (openFileDialog.ShowDialog() == true)
            {
                TranscriptionPathTextBox.Text = openFileDialog.ResultName;
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            }
        }

        private void LocalRecordingDatasetDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            UiGenerator.FolderPicker openFileDialog = new UiGenerator.FolderPicker();
            if (openFileDialog.ShowDialog() == true)
            { 
                LocalDatasetPath = openFileDialog.ResultName;
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            }
        }

        private void LocalRecordingDatasetNameButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Dataset (*.pds)|*.pds";
            if (openFileDialog.ShowDialog() == true)
            {
                LocalDatasetPath = openFileDialog.FileName.Substring(0, openFileDialog.FileName.IndexOf(openFileDialog.SafeFileName));
                LocalDatasetName = openFileDialog.SafeFileName;
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            }
        }

        private void WhisperModelChecked(object sender, RoutedEventArgs e)
        {
            RadioButton button = (RadioButton)sender;
            if (button.Name.Contains("Generic"))
            {
                WhipserGenericModelConfiguration.Visibility = Visibility.Visible;
                WhipserSpecificModelConfiguration.Visibility = Visibility.Collapsed;
            }
            else
            {
                WhipserGenericModelConfiguration.Visibility = Visibility.Collapsed;
                WhipserSpecificModelConfiguration.Visibility = Visibility.Visible;
            }
            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
        }

        private void WhisperModelDirectoryButtonClick(object sender, RoutedEventArgs e)
        {
            UiGenerator.FolderPicker openFileDialog = new UiGenerator.FolderPicker();
            if (openFileDialog.ShowDialog() == true)
            {
                WhisperModelDirectoryTextBox.Text = WhisperConfigurationUI.ModelDirectory = openFileDialog.ResultName;
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            }  
        }

        private void WhisperModelSpecificPathButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Binary (*.bin)|*.bin";
            if (openFileDialog.ShowDialog() == true)
            { 
                WhisperConfigurationUI.SpecificModelPath = WhisperModelSpecficPathTextBox.Text = openFileDialog.FileName;
                BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
            }
        }

        private void LocalStoringModeChecked(object sender, RoutedEventArgs e)
        {
            RadioButton button = (RadioButton)sender;
            switch(button.Name)
            {
                case "LocalStoringModeAudio":
                    localStoringMode = WhisperAudioProcessing.LocalStorageMode.AudioOnly;
                    break;

                case "LocalStoringModeVADSTT":
                    localStoringMode = WhisperAudioProcessing.LocalStorageMode.VAD_STT;
                    break;

                case "LocalStoringModeAll":
                    localStoringMode = WhisperAudioProcessing.LocalStorageMode.All;
                    break;
            }
            BtnLoadConfig.IsEnabled = BtnSaveConfig.IsEnabled = true;
        }

        private void GetMicrophonesConfiguration()
        {
            audioSoucesSetup.Clear();
            foreach (UIElement gridElement in MicrophonesGrid.Children)
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
                                continue;
                            string[] micAndChannel = inputText.Name.Split('_');
                            int micId;
                            int.TryParse(micAndChannel[0].Substring(1), out micId);
                            int channel;
                            int.TryParse(micAndChannel[1], out channel);
                            audioSoucesSetup.Add(new User(inputText.Text, micsList.ElementAt(micId).Item1, channel));
                        }
                    }
                }   
            }
        }

        private void GetWaveFilesConfiguration()
        {
            audioSoucesSetup.Clear();
            foreach (UIElement element in WaveFilesGrid.Children)
            {
                if (element is TextBox)
                {
                    TextBox? inputText = element as TextBox;
                    if (inputText is null || inputText.Text.Length < 1)
                        continue;
                    audioSoucesSetup.Add(new User(inputText.Text, inputText.Text, 1));
                }
            }
        }

        private void GetAudioSourceStreamConfiguration()
        {
            audioSoucesSetup.Clear();
            foreach (UIElement gridElement in AudioSourceDatasetStreamsGrid.Children)
            {
                if (gridElement is CheckBox)
                {
                    CheckBox? checkBox = gridElement as CheckBox;
                    if (checkBox != null && checkBox.IsChecked == true)
                        audioSoucesSetup.Add(new User(checkBox.Name, checkBox.Name, 1));
                }
            }
        }

        private void LoadAudioSourcesFromJson()
        {
            try
            {
                // Load dataset path and session name from Settings
                AudioSourceDatasetPath = Properties.Settings.Default.AudioSourceDatasetPath;
                AudioSourceSessionName = Properties.Settings.Default.AudioSourceSessionName;
                AudioSourceDatasetTextBox.Text = AudioSourceDatasetPath;
                AudioSourceSessionNameTextBox.Text = AudioSourceSessionName;

                // Load microphone configurations
                string audioSourcesJson = Properties.Settings.Default.AudioSourcesJson;
                if (!string.IsNullOrEmpty(audioSourcesJson))
                {
                    var loadedSources = JsonConvert.DeserializeObject<List<AudioSourceConfig>>(audioSourcesJson);
                    if (loadedSources != null)
                    {
                        audioSoucesSetup.Clear();
                        foreach (var source in loadedSources)
                        {
                            audioSoucesSetup.Add(new User(source.Id, source.Microphone, source.Channel));
                        }
                    }
                }

                // Load wave files
                string waveFilesJson = Properties.Settings.Default.WaveFilesJson;
                if (!string.IsNullOrEmpty(waveFilesJson))
                {
                    var loadedWaveFiles = JsonConvert.DeserializeObject<List<string>>(waveFilesJson);
                    if (loadedWaveFiles != null && selectedAudioSource == AudioSource.WaveFiles)
                    {
                        // Clear existing wave file rows (except the first empty one)
                        WaveFilesGrid.RowDefinitions.Clear();
                        WaveFilesGrid.Children.Clear();
                        
                        foreach (var waveFile in loadedWaveFiles)
                        {
                            AddWavFile();
                            // Set the text of the last added TextBox
                            var lastTextBox = WaveFilesGrid.Children.OfType<TextBox>().LastOrDefault();
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
                AddLog($"Error loading audio sources from JSON: {ex.Message}");
            }
        }

        private void SaveAudioSourcesToJson()
        {
            try
            {
                // Save dataset path and session name
                AudioSourceDatasetPath = AudioSourceDatasetTextBox.Text;
                AudioSourceSessionName = AudioSourceSessionNameTextBox.Text;
                Properties.Settings.Default.AudioSourceDatasetPath = AudioSourceDatasetPath;
                Properties.Settings.Default.AudioSourceSessionName = AudioSourceSessionName;

                // Save microphone configurations
                switch (selectedAudioSource)
                {
                    case AudioSource.Microphones:
                        GetMicrophonesConfiguration();
                        break;
                    case AudioSource.WaveFiles:
                        GetWaveFilesConfiguration();
                        break;
                    case AudioSource.Dataset:
                        GetAudioSourceStreamConfiguration();
                        break;
                }

                var audioSourceConfigs = audioSoucesSetup.Select(u => new AudioSourceConfig
                {
                    Id = u.Id,
                    Microphone = u.Microphone,
                    Channel = u.Channel
                }).ToList();
                Properties.Settings.Default.AudioSourcesJson = JsonConvert.SerializeObject(audioSourceConfigs);

                // Save wave files
                var waveFiles = new List<string>();
                foreach (UIElement element in WaveFilesGrid.Children)
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
                AddLog($"Error saving audio sources to JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper class for JSON serialization of audio source configuration
        /// </summary>
        private class AudioSourceConfig
        {
            public string Id { get; set; }
            public string Microphone { get; set; }
            public int Channel { get; set; }
        }
    }
}
