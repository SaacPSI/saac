# WhisperRemoteApp - User Guide

## Overview

**WhisperRemoteApp** is a speech recognition and transcription application powered by OpenAI's Whisper models. It captures audio from multiple sources (microphones, WAV files, Psi datasets), performs speech-to-text conversion with Voice Activity Detection (VAD), and streams results via the SAAC framework.

**Key Features**:
- ✅ Real-time speech recognition using Whisper models
- ✅ Multiple audio sources: microphones, WAV files, Psi datasets
- ✅ Voice Activity Detection (VAD) for speech segmentation
- ✅ Multiple language support (100+ languages)
- ✅ Multiple Whisper model sizes (tiny to large-v3)
- ✅ Network streaming via RendezVous protocol
- ✅ Local recording to Psi stores
- ✅ Multi-user support with user ID assignment
- ✅ Transcription file export (docx format)
- ✅ Configurable storage modes (Audio only, VAD+STT, All)

## User Interface

The WhisperRemoteApp window is organized into six tabs: **General**, **Audio Sources**, **Network**, **Whisper**, **Local Recording**, and **Log**.

[[/images/WhisperRemoteApp_MainWindow.png]]

### Tab 1: General

[[/images/WhisperRemoteApp_GeneralTab.png]]

The General tab provides application status, quick configuration toggles, transcription file settings, and main actions.

#### General Configuration

Quick access toggles for main features:

| Control | Description | Default |
|---------|-------------|---------|
| **Network** | Enable RendezVous connection | ☑ |
| **Streaming** | Enable data streaming to network | ☑ |
| **Whisper** | Enable Whisper transcription | ☑ |
| **Local Recording** | Enable local Psi store recording | ☐ |

**Control Relationships**:
```
Network Enabled
    ↓
Streaming Enabled (requires Network)
    ↓
Audio and transcriptions streamed to RendezVous server
    ↓
ServerApplication receives streams

Whisper Enabled
    ↓
Speech-to-text transcription active
    ↓
Generates transcription stream

Local Recording Enabled (independent)
    ↓
Audio/transcriptions saved to local Psi store
```

**Configuration Persistence**:

| Button | Function |
|--------|----------|
| **Load Configuration** | Load settings from file |
| **Save Configuration** | Save current settings to file |

#### Transcription File

Export transcriptions to text file:

| Control | Description | Example |
|---------|-------------|---------|
| **File Directory** | Path to save transcription file | D:\Transcriptions |
| **Filename** | Text file name | meeting_transcript.txt |

**File Directory**:
- Click **Browse** to select folder
- Creates directory if it doesn't exist
- Requires write permissions

**Transcription File Format**:
```
[14:30:05] User1: Hello, this is a test of the speech recognition system.
[14:30:12] User2: It works very well and provides accurate transcriptions.
[14:30:18] User1: The Whisper model is quite impressive for real-time use.
[14:30:25] User2: I agree, the quality is excellent.

```

#### Actions Panel

Main application controls:

| Button | Function | When Available |
|--------|----------|----------------|
| **Start** | Initialize and start all enabled features | Always |
| **Start Network Only** | Connect to RendezVous without capturing | Network enabled |
| **Quit** | Close application | Always (prompts if running) |

**Start Workflow**:
```
1. Click "Start"
   ↓
2. If Network enabled: Connect to RendezVous server
   ↓
3. If Streaming enabled: Export streams to network
   ↓
4. If Whisper enabled: Load Whisper model
   ↓
5. If Local Recording enabled: Create Psi stores
   ↓
6. Begin audio capture from configured sources
   ↓
7. Start Voice Activity Detection (VAD)
   ↓
8. Process detected speech with Whisper
   ↓
9. Application state: Running
   ↓ 
10. On stop : save transcriptions
```

**Start Network Only Workflow**:
```
1. Click "Start Network Only"
   ↓
2. Connect to RendezVous server
   ↓
3. Wait for Run command from ServerApplication
   ↓
4. Register process and stream endpoints 
   ↓
5. Application state: Connected (not capturing)
```

### Tab 2: Audio Sources

[[/images/WhisperRemoteApp_AudioSourcesTab.png]]

The Audio Sources tab configures where audio comes from: microphones, WAV files, or existing Psi datasets.

#### Audio Source Selection

| Control | Description | Options |
|---------|-------------|---------|
| **Audio Source** | Select input type | Microphones, Wave Files, Dataset |

**Audio Source Types**:

```
┌─────────────────────────────────────────┐
│ Audio Source: [Microphones        ▼]    │
└─────────────────────────────────────────┘

Available Sources:
├─ Microphones     → Live audio capture
├─ Wave Files      → Pre-recorded WAV files
└─ Dataset         → Replay from Psi store
```

**Source Selection Effects**:
- **Microphones**: Activates Microphones GroupBox
- **Wave Files**: Activates Wave Files GroupBox
- **Dataset**: Activates Dataset GroupBox

#### Microphones Configuration

[[/images/WhisperRemoteApp_Microphones.png]]

Capture audio from connected microphones:

**Microphone List**:
```
┌──────────────────────────────────────────────────────────┐
│ Microphones                                              │
├──────────────────────────────────────────────────────────┤
│ [Refresh list]                                           │
├──────────────────────────────────────────────────────────┤
│ Microphone                      User id affectation      │
├──────────────────────────────────────────────────────────┤
│ ☑ Headset Microphone            [User1        ▼]        │
│ ☑ Desktop Microphone            [User2        ▼]        │
│ ☐ Webcam Microphone             [User3        ▼]        │
│ ☐ USB Microphone Array          [User4        ▼]        │
└──────────────────────────────────────────────────────────┘
```

**Controls**:
- **Refresh list**: Scan for connected microphones
- **Checkbox**: Enable/disable microphone
- **User id affectation**: Assign speaker identifier

**User ID Assignment**:
- Associates microphone with specific speaker
- Used for multi-user transcription
- Appears in transcription output
- Dropdown populated from configuration

**Multi-Microphone Setup**:
```
Scenario: Group Meeting with 4 Participants

Microphone 1 (Headset) → User ID: "Alice"
Microphone 2 (Desktop) → User ID: "Bob"
Microphone 3 (USB)     → User ID: "Charlie"
Microphone 4 (Webcam)  → User ID: "David"

Result: Separate transcription streams per person
```

**Microphone Detection**:
- Automatic detection on application start
- Click **Refresh list** to rescan
- Shows device name from Windows
- Indicates default device with icon

#### Wave Files Configuration

[[/images/WhisperRemoteApp_WaveFiles.png]]

Process pre-recorded WAV files:

```
┌──────────────────────────────────────────────────────────┐
│ Wave Files                                               │
├──────────────────────────────────────────────────────────┤
│ [Add file]                                               │
├──────────────────────────────────────────────────────────┤
│ meeting_audio.wav                [Browse]  [Remove]      │
│ interview.wav                    [Browse]  [Remove]      │
│ podcast_episode_01.wav           [Browse]  [Remove]      │
└──────────────────────────────────────────────────────────┘
```

**Add File Button**:
- Opens file dialog
- Select one or more WAV files
- Adds to processing queue
- Each filename will be used as user ID

**Supported Formats**:
- WAV (PCM 16-bit)
- Sample rates: 8000, 16000, 44100, 48000 Hz
- Mono or Stereo (converted to mono)

**Use Cases**:
- Batch transcription of recordings
- Post-meeting transcription
- Podcast transcription
- Interview analysis

#### Dataset Configuration

[[/images/WhisperRemoteApp_Dataset.png]]

Replay audio from existing Psi datasets:

| Control | Description | Example |
|---------|-------------|---------|
| **Dataset Path** | Path to Psi dataset | D:\Recordings\AudioCapture.pds |
| **Session to load** | Session name to replay | Session_001 |

**Dataset Path**:
- Click **Browse** to select `.pds` file
- Must be valid Psi dataset
- Can be from previous WhisperRemoteApp recording
- Or any dataset containing audio streams

**Open Dataset Button**:
- Loads dataset and scans for audio streams
- Populates Available Streams list
- Validates dataset integrity

**Available Streams**:
```
┌──────────────────────────────────────────────────────────┐
│ Available Streams                                        │
├──────────────────────────────────────────────────────────┤
│ ☑ Audio_User1                                           │
│ ☑ Audio_User2                                           │
│ ☐ Audio_Ambient                                         │
│ ☐ MicrophoneArray                                       │
└──────────────────────────────────────────────────────────┘
```

**Stream Selection**:
- Check streams to process
- Use stream name as user ID
- Multiple streams processed simultaneously
- Synchronized with original timestamps

**Replay Workflow**:
```
1. Select Dataset Path
   ↓
2. Enter Session name
   ↓
3. Click "Open Dataset"
   ↓
4. Available audio streams appear
   ↓
5. Select streams to process
   ↓
6. Assign user IDs
   ↓
7. Click "Start"
   ↓
8. Dataset replayed with Whisper transcription
   ↓
9. Results streamed/saved
```

**Use Cases**:
- Post-processing existing recordings
- Re-transcribing with different Whisper models
- Extracting audio from multi-modal datasets
- Testing Whisper configurations

### Tab 3: Network

[[/images/WhisperRemoteApp_NetworkTab.png]]

The Network tab configures RendezVous connection and streaming settings.

#### Network Activation

| Control | Description | Effect |
|---------|-------------|--------|
| **Activate** | Enable network features | Enables all network settings |

**When Activated**:
- Application will connect to RendezVous server
- Streams will be exported on Run command
- Command listener will be active

**When Deactivated**:
- Local operation only
- No network communication
- Standalone mode

#### Psi Server Configuration

RendezVous server connection settings:

| Control | Description | Example | Default |
|---------|-------------|---------|---------|
| **Server IP** | RendezVous server address | 192.168.1.100 | localhost |
| **Server Port** | RendezVous server port | 13330 | 13330 |
| **Command Port** | Port for receiving commands | 11511 | 11511 |

**Server IP Options**:
- **localhost** or **127.0.0.1**: Local testing
- **192.168.x.x**: Local network
- **x.x.x.x**: Any network configuration
- **Hostname**: DNS-resolvable name

**Port Configuration**:
```
Default Ports:
├─ RendezVous: 13330 (TCP)
├─ Command: 11511 (TCP)
└─ Streaming: 12000+ (TCP)

Firewall Rules Needed:
├─ Outbound: 13330 (RendezVous registration)
├─ Outbound: 12000-12010 (Stream export)
└─ Inbound: 11511 (Commands from server)
```

#### Application Configuration

Settings for this WhisperRemoteApp instance:

| Control | Description | Example |
|---------|-------------|---------|
| **Activate Streaming** | Enable stream export | ☑ |
| **IP To Use** | Local IP address for streams | 192.168.1.105 |
| **Streaming Port Range Start** | First port for stream export | 12000 |
| **Application Name** | Unique process identifier | WhisperApp1 |

**IP To Use**:
- Dropdown populated with available network adapters
- Select adapter connected to RendezVous network
- **Auto-detect**: Application chooses best interface
- **Manual**: Specify exact IP address

**Application Name**:
- Must be unique across all processes
- Appears in ServerApplication's process list
- Naming convention: `Whisper_<Location>_<ID>`
- Examples:
  - `Whisper_Lab1_Room1`
  - `Whisper_Meeting_RecorderA`
  - `Whisper_Interview_Main`

**Streaming Port Allocation**:
```
Base Port: 12000

Audio Sources (per microphone/file):
├─ Audio_User1 → Port 12000
├─ Transcription_User1 → Port 12001
├─ Audio_User2 → Port 12002
├─ Transcription_User2 → Port 12003
└─ ... (increments by 2 per user)

Each user gets 2 ports: Audio + Transcription
```

**Streaming Workflow**:
```
1. WhisperRemoteApp connects to RendezVous server
   ↓
2. Registers process with Application Name
   ↓
3. Advertises stream endpoints:
   - Stream "Audio_User1" on 192.168.1.105:12000
   - Stream "Transcription_User1" on 192.168.1.105:12001
   - etc.
   ↓
4. ServerApplication sees streams
   ↓
5. Sends Run command
   ↓
6. WhisperRemoteApp begins capturing and transcribing
   ↓
7. ServerApplication receives audio and transcription streams
   ↓
8. Synchronized recording in ServerApplication's dataset
```

#### Other Settings

| Control | Description | Purpose |
|---------|-------------|---------|
| **Command Application Name** | Source process for commands | Filter commands |

**Command Source**:
- Specify which process can send commands
- Typically: "ServerApplication" or "ControlCenter"
- Empty: Accept commands from any source
- Security feature to prevent unauthorized control

### Tab 4: Whisper

[[/images/WhisperRemoteApp_WhisperTab.png]]

The Whisper tab configures Voice Activity Detection (VAD) and Whisper model settings for speech recognition.

#### Whisper Activation

| Control | Description | Default |
|---------|-------------|---------|
| **Activate** | Enable Whisper transcription | ☑ |

**When Activated**:
- Whisper model loaded on startup
- Speech-to-text processing enabled
- Transcription streams available

**When Deactivated**:
- Audio capture only
- No transcription processing
- Lower resource usage

#### Voice Activity Detection Configuration

VAD segments audio into speech and silence regions before transcription:

| Control | Description | Default | Unit |
|---------|-------------|---------|------|
| **Buffer Length (ms)** | Audio buffer size | 1000 | ms |
| **Voice Activity Start Offset (ms)** | Pre-speech padding | 300 | ms |
| **Voice Activity End Offset (ms)** | Post-speech padding | 300 | ms |
| **Initial Silence Timeout (ms)** | Max silence before dropping | 3000 | ms |
| **Babble Timeout (ms)** | Max continuous speech | 10000 | ms |
| **End Silence Timeout Ambiguous (ms)** | Ambiguous pause threshold | 800 | ms |
| **End Silence Timeout (ms)** | Definite pause threshold | 1200 | ms |

**VAD Parameter Explanations**:

**Buffer Length**:
- Size of audio buffer processed at once
- Smaller = lower latency, more CPU
- Larger = higher latency, less CPU
- Recommended: 1000 ms

**Voice Activity Start Offset**:
- Adds audio before detected speech start
- Prevents cutting off beginning of words
- 300 ms captures typical speech onset

**Voice Activity End Offset**:
- Adds audio after detected speech end
- Prevents cutting off word endings
- 300 ms captures typical word fadeout

**Initial Silence Timeout**:
- Maximum silence before discarding buffer
- Prevents processing pure silence
- Too low: cuts off slow speakers
- Too high: processes long silence

**Babble Timeout**:
- Maximum continuous speech duration
- Forces segmentation of long utterances
- Prevents memory overflow
- 10000 ms (10 seconds) typical

**End Silence Timeout Ambiguous**:
- Pause duration that *might* indicate end
- Used for real-time partial results
- Lower = more responsive, more partials

**End Silence Timeout**:
- Pause duration that *definitely* indicates end
- Triggers final transcription
- Higher = better sentence capture
- Lower = more responsive

**VAD Workflow**:
```
Audio Input
    ↓
Buffer (1000 ms)
    ↓
VAD Detection
    ├─ Silence detected → Drop buffer or wait
    └─ Speech detected
         ↓
         Add Start Offset (300 ms before)
         ↓
         Continue until silence
         ↓
         Check silence duration:
         ├─ < 800 ms → Continue (word pause)
         ├─ 800-1200 ms → Maybe end (partial result)
         └─ > 1200 ms → Definite end
              ↓
              Add End Offset (300 ms after)
              ↓
              Send to Whisper for transcription
```

**VAD Tuning Guide**:

**For Quiet Speakers**:
- Decrease End Silence Timeout (800 ms)
- Increase Voice Activity Start Offset (500 ms)
- Decrease Babble Timeout (8000 ms)

**For Fast Talkers**:
- Decrease End Silence Timeout Ambiguous (600 ms)
- Decrease End Silence Timeout (1000 ms)
- Increase Babble Timeout (15000 ms)

**For Noisy Environments**:
- Increase Initial Silence Timeout (5000 ms)
- Use higher quality Whisper model
- Enable audio preprocessing

#### Whisper Configuration

Whisper model selection and settings:

**Model Type**:

| Option | Description | Use Case |
|--------|-------------|----------|
| **Generic** | Standard Whisper models | Most common, easier setup |
| **Specific** | Custom/fine-tuned models | Specialized domains |

**Generic Model Configuration**:

[[/images/WhisperRemoteApp_GenericModel.png]]

| Control | Description | Options |
|---------|-------------|---------|
| **Language** | Target transcription language | Auto, English, French, Spanish, etc. (100+) |
| **Model** | Whisper model size | tiny, base, small, medium, large, large-v2, large-v3 |
| **Quantization** | Model compression | No quantization, Int8, Q4, Q5 |
| **Model Directory** | Path to store models | C:\WhisperModels |

**Whisper Model Comparison**:

| Model | Size | Speed | Accuracy | VRAM | Recommended Use |
|-------|------|-------|----------|------|------------------|
| **tiny** | 39 MB | ⚡⚡⚡⚡⚡ | ★★☆☆☆ | 1 GB | Quick testing |
| **base** | 74 MB | ⚡⚡⚡⚡☆ | ★★★☆☆ | 1 GB | Real-time, low resources |
| **small** | 244 MB | ⚡⚡⚡☆☆ | ★★★★☆ | 2 GB | **Balanced (recommended)** |
| **medium** | 769 MB | ⚡⚡☆☆☆ | ★★★★★ | 5 GB | High accuracy |
| **large** | 1550 MB | ⚡☆☆☆☆ | ★★★★★ | 10 GB | Maximum accuracy |
| **large-v2** | 1550 MB | ⚡☆☆☆☆ | ★★★★★ | 10 GB | Improved large |
| **large-v3** | 1550 MB | ⚡☆☆☆☆ | ★★★★★ | 10 GB | Latest, best quality |

**Language Selection**:
- **Auto**: Automatic language detection (recommended)
- **Specific**: Choose if known (faster, more accurate)
- Supports 100+ languages including:
  - English, Spanish, French, German, Italian, Portuguese
  - Russian, Chinese, Japanese, Korean, Arabic, Hindi
  - Dutch, Polish, Turkish, Swedish, and many more

**Quantization Options**:

| Quantization | Size Reduction | Speed Increase | Quality Loss | Use When |
|--------------|----------------|----------------|--------------|----------|
| **No quantization** | 0% | Baseline | None | GPU available |
| **Int8** | ~50% | +20% | Minimal | CPU only, good specs |
| **Q4** | ~75% | +40% | Small | CPU only, limited RAM |
| **Q5** | ~70% | +35% | Very small | CPU only, balanced |

**Model Directory**:
- Where Whisper models are downloaded/stored
- First use downloads model (~40 MB to 1.5 GB)
- Subsequent uses load from directory
- Click **Browse** to select location
- Recommended: Fast SSD drive

**Specific Model Configuration**:

[[/images/WhisperRemoteApp_SpecificModel.png]]

| Control | Description | Example |
|---------|-------------|---------|
| **Specific Model Path** | Path to custom ONNX model | D:\Models\whisper_medical.onnx |

**Custom Model Requirements**:
- ONNX format
- Compatible with Whisper.cpp or similar
- Can be fine-tuned for specific domains:
  - Medical terminology
  - Legal proceedings
  - Technical documentation
  - Regional accents/dialects

**Model Loading**:
```
First Run with "small" model:
  ↓
1. Check Model Directory: C:\WhisperModels
   ↓
2. Model not found
   ↓
3. Download from Hugging Face (~244 MB)
   ↓
4. Extract to Model Directory
   ↓
5. Load model into memory
   ↓
6. Ready for transcription (10-30 seconds)

Subsequent Runs:
  ↓
1. Check Model Directory
   ↓
2. Model found
   ↓
3. Load from disk (5-10 seconds)
   ↓
4. Ready for transcription
```

### Tab 5: Local Recording

[[/images/WhisperRemoteApp_LocalRecordingTab.png]]

The Local Recording tab configures local Psi store recording.

#### Local Recording Activation

| Control | Description | Default |
|---------|-------------|---------|
| **Activate** | Enable local recording | ☐ |

**When Activated**:
- Audio and/or transcriptions saved to local Psi stores
- Works independently of network streaming
- Guaranteed local backup

**When Deactivated**:
- Network streaming only
- No local data storage

#### Recording Settings

| Control | Description | Example |
|---------|-------------|---------|
| **Session Name** | Current recording session | Interview_2024-01-15 |
| **Dataset Directory** | Path to store datasets | D:\AudioRecordings |
| **Dataset Name** | Dataset identifier | SpeechData |

**Session Name**:
- Unique identifier for this recording session
- Can include timestamps, participant IDs, descriptions
- Examples:
  - `Meeting_2024-01-15_TeamStandup`
  - `Interview_Participant_P05`
  - `Lecture_QuantumPhysics_Week3`
  - `Podcast_Episode_042`

**Dataset Directory**:
- Root directory for all datasets
- Click **Browse** to select folder
- Requirements:
  - Fast storage (SSD recommended for real-time)
  - Sufficient free space (estimate below)
  - Write permissions
- Recommended: Dedicated data drive

**Dataset Name**:
- Base name for this dataset
- Click **Browse** to select existing dataset or create new
- Creates `.pds` file automatically
- Examples:
  - `MeetingTranscripts_2024`
  - `InterviewData`
  - `PodcastEpisodes`

#### Storing Mode

Choose what data to save:

| Mode | Description | Storage Size | Use Case |
|------|-------------|--------------|----------|
| **Audio Only** | Save raw audio streams | High (~100 MB/hour) | Audio archival |
| **VAD and STT** | Save transcriptions + VAD segments | Low (~5 MB/hour) | Transcription only |
| **All** | Save audio, VAD, transcriptions | High (~105 MB/hour) | Complete data |

**Storage Mode Details**:

**Audio Only**:
```
Stores:
├─ Raw audio streams (per user/microphone)
└─ Timestamps

Use cases:
├─ Audio archival for later processing
├─ Re-transcription with different models
└─ Audio analysis (prosody, emotion)

Storage: ~100 MB/hour per source
```

**VAD and STT**:
```
Stores:
├─ VAD segments (speech/silence boundaries)
├─ Transcription text
├─ Confidence scores
└─ Timestamps

Use cases:
├─ Text-only transcription
├─ Meeting minutes
└─ Searchable audio database

Storage: ~5 MB/hour per source
```

**All**:
```
Stores:
├─ Raw audio streams
├─ VAD segments
├─ Transcription text
├─ Confidence scores
├─ Word-level timestamps
└─ All metadata

Use cases:
├─ Complete multimodal datasets
├─ Research data collection
└─ Maximum flexibility for analysis

Storage: ~105 MB/hour per source
```

**Storage Size Estimates**:

```
Single Microphone (1 hour):
├─ Audio Only: ~100 MB
├─ VAD and STT: ~5 MB
└─ All: ~105 MB

4 Microphones (1 hour):
├─ Audio Only: ~400 MB
├─ VAD and STT: ~20 MB
└─ All: ~420 MB

4 Microphones (8 hour day):
├─ Audio Only: ~3.2 GB
├─ VAD and STT: ~160 MB
└─ All: ~3.36 GB
```

#### Dataset Structure

```
D:\AudioRecordings\                      (Dataset Directory)
└─ SpeechData\                           (Dataset Name)
    ├─ SpeechData.pds                    (Dataset descriptor)
    └─ Interview_2024-01-15\             (Session Name)
        ├─ WhisperApp1.Audio_User1\      (Process.Stream folders)
        │   ├─ 000000.psi
        │   └─ 000000.psi.catalog
        ├─ WhisperApp1.Transcription_User1\
        │   ├─ 000000.psi
        │   └─ 000000.psi.catalog
        ├─ WhisperApp1.Audio_User2\
        │   ├─ 000000.psi
        │   └─ 000000.psi.catalog
        └─ WhisperApp1.Transcription_User2\
            ├─ 000000.psi
            └─ 000000.psi.catalog
```

### Tab 6: Log

[[/images/WhisperRemoteApp_LogTab.png]]

The Log tab displays real-time application events and status messages.

## Common Workflows

### Workflow 1: Basic Meeting Transcription

**Scenario**: Transcribe team meeting with 2 participants

**Configuration**:
1. **General Tab**:
   - Network: ☐ (local only)
   - Whisper: ☑
   - Local Recording: ☑
   - Transcription File:
     - Directory: D:\Meetings
     - Filename: TeamMeeting_2024-01-15.txt

2. **Audio Sources Tab**:
   - Audio Source: Microphones
   - Click **Refresh list**
   - Enable 2 microphones:
     - Headset → User ID: "Alice"
     - Desktop → User ID: "Bob"

3. **Whisper Tab**:
   - Model: small
   - Language: Auto
   - VAD: Default settings

4. **Local Recording Tab**:
   - Session Name: TeamMeeting_2024-01-15
   - Storing: VAD and STT
   - Dataset: D:\MeetingData

5. Click **"Start"**

6. Conduct meeting (application transcribes in real-time)

7. Click **"Quit"** when finished

8. Check transcription file: D:\Meetings\TeamMeeting_2024-01-15.txt

**Result**:
- Real-time transcription with speaker identification
- Text file with timestamps
- Psi store with transcriptions for PsiStudio analysis

### Workflow 2: Batch WAV File Transcription

**Scenario**: Transcribe 10 recorded interview files

**Configuration**:
1. **General Tab**:
   - Network: ☐
   - Whisper: ☑
   - Local Recording: ☑

2. **Audio Sources Tab**:
   - Audio Source: Wave Files
   - Click **"Add file"** 10 times
   - Add all interview WAV files
   - Assign User IDs: "Interviewer", "Participant"

3. **Whisper Tab**:
   - Model: medium (better accuracy for transcription)
   - Language: English

4. **Local Recording Tab**:
   - Session Name: Interviews_Batch_01
   - Storing: All (audio + transcriptions)

5. Click **"Start"**

6. Application processes files sequentially

7. Monitor progress in Log tab

**Result**:
- All 10 files transcribed
- Separate transcriptions per file
- Complete dataset with audio and text

### Workflow 3: Multi-User SAAC Recording

**Scenario**: Record meeting with synchronized video, audio, and transcriptions

**Setup**:
1. Start ServerApplication

2. **WhisperRemoteApp**:
   - **General Tab**:
     - Network: ☑
     - Streaming: ☑
     - Whisper: ☑
   - **Network Tab**:
     - Server IP: ServerApplication address
     - Application Name: WhisperMeeting
   - **Audio Sources Tab**:
     - 3 microphones with user IDs
   - **Whisper Tab**:
     - Model: small
     - Language: Auto

3. Also start:
   - VideoRemoteApp (for video)
   - CameraRemoteApp (for cameras)

4. From ServerApplication:
   - Click **"Start Session"**
   - Wait for all processes to connect
   - Click **"Start All"**

5. Conduct meeting

6. From ServerApplication:
   - Click **"Stop All"**
   - Click **"Stop Session"**

**Result**:
- Synchronized multimodal dataset:
  - Video streams
  - Audio streams (3 users)
  - Transcriptions (3 users)
  - All with matching timestamps
- Single Psi dataset in ServerApplication
- Ready for PsiStudio visualization

### Workflow 4: Dataset Replay and Re-transcription

**Scenario**: Re-transcribe existing audio with better Whisper model

**Configuration**:
1. **General Tab**:
   - Whisper: ☑
   - Local Recording: ☑

2. **Audio Sources Tab**:
   - Audio Source: Dataset
   - Dataset Path: D:\OldRecordings\MeetingAudio.pds
   - Session: Meeting_Jan_10
   - Click **"Open Dataset"**
   - Select audio streams to process

3. **Whisper Tab**:
   - Model: large-v3 (best quality)
   - Language: English

4. **Local Recording Tab**:
   - Session Name: Retranscribed_HighQuality
   - Storing: VAD and STT

5. Click **"Start"**

**Result**:
- Original audio replayed through Whisper
- Higher quality transcriptions
- New Psi store with improved results
- Original audio preserved

## Integration with SAAC

### ServerApplication Configuration

When WhisperRemoteApp connects to ServerApplication:

```
ServerApplication automatically discovers:
├─ Process: WhisperApp1
├─ Streams (per user):
│   ├─ Audio_User1
│   ├─ Transcription_User1
│   ├─ Audio_User2
│   └─ Transcription_User2
└─ Endpoints with port assignments
```

**In ServerApplication Configuration Tab**:
```
Store Mode: Process
Dataset Path: D:\Experiments
Dataset Name: MultimodalMeeting

On "Start Session":
  → Creates stores for all discovered streams
  → D:\Experiments\MultimodalMeeting\Session_001\WhisperApp1\
     ├─ Audio_User1
     ├─ Transcription_User1
     ├─ Audio_User2
     └─ Transcription_User2
```

### PsiStudio Visualization

After recording:

1. Open PsiStudio
2. File → Open Dataset
3. Navigate to Dataset Path
4. Select `.pds` file
5. Visualizations available:
   - Audio waveform timeline
   - Transcription text annotations
   - Multi-user timeline comparison
   - Confidence score overlay
   - Synchronized with video/other modalities

### Custom Processing

```csharp
// Access transcriptions programmatically
var dataset = Dataset.Load(@"D:\Experiments\MultimodalMeeting.pds");

foreach (var session in dataset.Sessions)
{
    using (var pipeline = Pipeline.Create())
    {
        // Open WhisperApp store
        var store = PsiStore.Open(pipeline, "WhisperApp1", session.Path);
        
        // Access transcription stream
        var transcriptions = store.OpenStream<string>("Transcription_User1");
        
        // Process transcriptions
        transcriptions.Do((text, env) =>
        {
            Console.WriteLine($"[{env.OriginatingTime}] {text}");
            // Perform text analysis, sentiment, keywords, etc.
        });
        
        pipeline.Run();
    }
}
```

## Troubleshooting

### Audio Issues

**No Audio Detected**:
- Verify microphone is connected
- Check Windows Sound Settings (microphone enabled)
- Test with Windows Voice Recorder
- Click **"Refresh list"** in Audio Sources tab
- Check application has microphone permissions
- Try different microphone

**Poor Audio Quality**:
- Check microphone is not muted
- Adjust microphone gain in Windows
- Move closer to microphone
- Reduce background noise
- Check microphone cable/connection
- Test with different USB port

**VAD Not Detecting Speech**:
- Speak louder or closer to microphone
- Lower VAD thresholds (more sensitive)
- Increase Voice Activity Start Offset
- Check audio levels are adequate
- Verify microphone is selected
- Test microphone with other applications

**Multiple Microphones Not Working**:
- Check each microphone individually
- Verify USB hub has sufficient power
- Try different USB ports
- Update audio drivers
- Check Windows Sound Settings for conflicts
- Disable unused audio devices

### Whisper Issues

**Model Loading Fails**:
- Check Model Directory path exists
- Ensure sufficient disk space for download
- Verify internet connection (first download)
- Check write permissions on Model Directory
- Try different model (start with tiny)
- Clear Model Directory and re-download

**Transcription Quality Poor**:
- Use larger model (medium, large, large-v3)
- Manually select language (don't use Auto)
- Improve audio quality
- Adjust VAD timeouts
- Check for background noise
- Use Int8 quantization instead of Q4/Q5

**Transcription Too Slow**:
- Use smaller model (tiny, base, small)
- Enable quantization (Int8 or Q4)
- Close other applications
- Check CPU usage
- Consider hardware upgrade
- Adjust VAD to create shorter segments

**Wrong Language Detected**:
- Manually select language in Whisper Configuration
- Use larger model (better language detection)
- Check audio quality
- Ensure speech is clear
- Avoid mixed-language content
- Use language-specific model

**Out of Memory Errors**:
- Use smaller model
- Close other applications
- Increase system virtual memory
- Reduce number of simultaneous microphones
- Restart application periodically
- Consider hardware upgrade

### Network Issues

**Cannot Connect to RendezVous**:
- Verify ServerApplication is running
- Check Server IP and Port
- Test network: `ping <server-ip>`
- Check firewall rules (port 13330)
- Verify network connectivity
- Try "Start Network Only" to test connection

**Streams Not Appearing in ServerApplication**:
- Check "Activate Streaming" is enabled
- Verify Application Name is unique
- Check Streaming Port Range is available
- Review firewall rules (ports 12000+)
- Check Network tab configuration
- Monitor Log tab for registration errors

**High Network Latency**:
- Use wired connection instead of WiFi
- Reduce number of streams
- Check network bandwidth
- Close bandwidth-intensive applications
- Check router/switch performance
- Monitor network with Task Manager

### Recording Issues

**Store Creation Failed**:
- Verify Dataset Directory exists
- Check write permissions
- Ensure sufficient disk space (>10 GB)
- Check path length (<260 characters)
- Avoid special characters in paths
- Try different drive

**Recording Stops Unexpectedly**:
- Check disk space (need continuous free space)
- Monitor disk write speed (SSD recommended)
- Review Log tab for errors
- Check for file system errors
- Verify no disk quota restrictions
- Try different storage location

**Transcription File Not Created**:
- Check File Directory path is valid
- Verify write permissions
- Ensure Filename is specified
- Check disk space
- Review Log tab for errors
- Try creating directory manually

## Performance Optimization

### CPU/GPU Usage

**For CPU-Only Systems**:
- Use smaller models (tiny, base, small)
- Enable quantization (Int8 or Q4)
- Reduce number of simultaneous microphones
- Adjust VAD Buffer Length to 500 ms
- Close unnecessary applications
- Consider dedicated transcription machine

**For GPU Systems** (if supported):
- Use larger models (medium, large, large-v3)
- No quantization needed
- Can process more microphones simultaneously
- Better real-time performance
- Update GPU drivers regularly

### Memory Management

**Reduce Memory Usage**:
- Use smaller Whisper models
- Reduce VAD Buffer Length
- Limit number of microphones
- Use "VAD and STT" storage mode
- Close other applications
- Restart application periodically

### Storage Optimization

**Reduce Storage Size**:
- Use "VAD and STT" instead of "All"
- Don't store audio if not needed
- Split into shorter sessions
- Compress old datasets
- Regular archival to external storage

### Latency Optimization

**For Real-Time Subtitles**:
- Use small or base model
- Reduce End Silence Timeout (800 ms)
- Reduce End Silence Timeout Ambiguous (600 ms)
- Use wired network
- Close other applications
- Enable GPU acceleration

## Advanced Features

### Multi-User Speaker Diarization

Automatic speaker identification:

```
User ID Assignment:
├─ Microphone-based (recommended)
│   └─ Each user has dedicated microphone
│
└─ Acoustic-based (future feature)
    └─ Single microphone, automatic speaker separation
```

**Current Implementation**:
- One microphone per speaker
- User ID assigned per microphone
- Appears in transcription output:
  ```
  [14:30:05] Alice: "We need to discuss the project timeline."
  [14:30:10] Bob: "I agree, let's review the milestones."
  [14:30:15] Alice: "The first milestone is due next week."
  ```

### Custom Whisper Models

Fine-tuning for specific domains:

**Medical**:
- Trained on medical terminology
- Better recognition of drug names, procedures
- Higher accuracy for clinical conversations

**Legal**:
- Legal terminology and jargon
- Court proceedings format
- Better punctuation for legal documents

**Technical**:
- Programming terminology
- Technical product names
- Engineering discussions

**How to Use**:
1. Obtain ONNX format custom model
2. Place in accessible directory
3. Select "Specific" in Model Type
4. Browse to model path
5. Start application

## Best Practices

### Audio Configuration

1. **Microphone Setup**:
   - Use high-quality USB microphones
   - Position 6-12 inches from speaker
   - Reduce background noise
   - Test audio levels before recording
   - Assign clear user IDs

2. **VAD Tuning**:
   - Start with defaults
   - Adjust based on speaker pace
   - Test with actual use case
   - Document final settings

3. **Multi-User Setup**:
   - One microphone per speaker (recommended)
   - Clear user ID labeling
   - Test all microphones before session
   - Have backup microphones available

### Whisper Configuration

1. **Model Selection**:
   - Development/testing: tiny or base
   - Production/real-time: small
   - High-accuracy transcription: medium or large
   - Research/archival: large-v3

2. **Language Settings**:
   - Use Auto for unknown/mixed languages
   - Specify language if known (faster, more accurate)
   - Test with sample audio before production

3. **Storage Strategy**:
   - Audio Only: For later processing flexibility
   - VAD and STT: For text-only use cases
   - All: For research and complete datasets

### Network Configuration

1. **Naming Convention**:
   - Descriptive Application Names
   - Include location/purpose
   - Avoid spaces (use underscores)
   - Keep unique per instance

2. **Port Management**:
   - Document port assignments
   - Reserve port range (12000-12020)
   - Avoid conflicts with other services
   - Configure firewall beforehand

3. **Testing**:
   - Test connectivity before sessions
   - Verify streams in ServerApplication
   - Check bandwidth with all microphones
   - Run test transcriptions

### Production Deployment

1. **Pre-Session Checklist**:
   - [ ] All microphones tested
   - [ ] User IDs assigned
   - [ ] Network connectivity verified
   - [ ] Whisper model loaded
   - [ ] Storage location has space
   - [ ] Transcription file configured
   - [ ] Test capture completed

2. **During Session**:
   - Monitor Log tab for errors
   - Check audio levels
   - Watch VAD activity
   - Note any anomalies

3. **Post-Session**:
   - Verify transcription file
   - Check Psi store integrity
   - Archive/backup data
   - Document any issues