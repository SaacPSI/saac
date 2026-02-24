# ServerApplication - User Guide

## Overview

**ServerApplication** is the central coordination hub for the SAAC framework. It implements the RendezVous protocol, manages distributed Psi applications, provides synchronized control, and optionally handles local data recording and annotation services.

**Key Features**:
-  RendezVous server for process coordination
-  Distributed application discovery
-  Synchronized command distribution (Run, Stop, Close)
-  Process status monitoring
-  Clock synchronization across processes
-  Web-based annotation interface
-  Multi-session management
-  Centralized logging
-  Configuration persistence

## Architecture Role

```
┌────────────────────────────────────────────────────────┐
│               ServerApplication                        │
│            (Central Coordinator)                       │
├────────────────────────────────────────────────────────┤
│                                                        │
│  ┌──────────────────────────────────────────────────┐  │
│  │         RendezVous Server                        │  │
│  │  • Process Registry                              │  │
│  │  • Stream Advertisement                          │  │
│  │  • Command Distribution                          │  │
│  │  • Local Recording Pipeline                      │  │
│  │  • Clock Synchronization                         │  │
│  └────────────┬─────────────────────────────────────┘  │
│               │                                        │
│  ┌────────────▼─────────────────────────────────────┐  │
│  │         Optional Services                        │  │
│  │  • Unity & stream recording configuration        │  │
│  │  • Annotation Web Server                         │  │
│  └──────────────────────────────────────────────────┘  │
│                                                        │
└────────────┬───────────────────────────────────────────┘
             │
      [Network Commands]
             │
   ┌─────────┴─────────┬──────────────────┬─────────────────┐
   │                   │                  │                 │
┌──▼──────────┐   ┌────▼────────────┐  ┌──▼──────────┐  ┌───▼─────────────┐
│VideoRemote  │   │CameraRemote     │  │Whisper      │  │KinectAzure      │
│    App      │   │    App          │  │RemoteApp    │  │RemoteConsole    │
└─────────────┘   └─────────────────┘  └─────────────┘  └─────────────────┘
```

## User Interface

The ServerApplication window is organized into three tabs: **Overview**, **Configuration**, and **Annotation**.

[[/images/ServerApplication_MainWindow.png]]

### Tab 1: Overview

[[/images/ServerApplication_OverviewTab.png]]

The Overview tab provides real-time monitoring and control of connected processes.

#### Connected Devices Display

**Purpose**: Shows all processes registered with the RendezVous server.

[[/images/ServerApplication_ConnectedDevices.png]]

**Device List Display**:
```
┌──────────────────────────────────────────────────────────┐
│ Connected Devices                                        │
├──────────────────────────────────────────────────────────┤
│                                                          │
│ Process Name        Status                               │
│ ─────────────────────────────────────────────────────────┤
│ VideoCapture1       ● Running                            │
│   └─ Streams: FullScreen, TopLeft, TopRight              │
│                                                          │
│ CameraApp1          ● Running                            │
│   └─ Streams: RGB                                        │
│                                                          │
│ WhisperApp          ● Running                            │
│   └─ Streams: Audio, Transcription                       │
│                                                          │
│ KinectAzure1        ● Running                            │
│   └─ Streams: Bodies, ColorImage, DepthImage, IMU        │
│                                                          │
│ UnityVR             ◐ Standby  Unity                     │
│   └─ Awaiting Run command                                │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

**Status Indicators**:
- ● **Running** (Green): Process active and streaming data
- ◐ **Standby** (Orange): Connected but not started
- ✗ **Error** (Red): Connection lost or error state

**For Each Connected Process**:

| Information | Description | Example |
|-------------|-------------|---------|
| **Process Name** | Unique identifier | "VideoCapture1" |
| **Status** | Current state | Green (Running), Orange (Standby), Red (Error) |

**Process Controls**:

| Button | Function | Availability |
|--------|----------|--------------|
| **Run** | Send Run command to process | Standby state |
| **Stop** | Send Stop command to process | Running state |

**Device List Features**:
- **Auto-Discovery**: New processes appear automatically
- **Live Updates**: Status refreshes in real-time

#### All Devices Controls

Bulk operations for all connected processes:

| Control | Function | When Enabled |
|---------|----------|--------------|
| **Start All** | Send Run command to all processes | On session start |
| **Stop All** | Send Stop command to all running | On session start |

**Workflow**:
```
1. Start ServerApplication Session
   ↓
2. All processes connect to ServerApplication (Standby)
   ↓
3. Click "Start All"
   ↓
4. All processes receive Run command simultaneously
   ↓
5. All begin sending data, ServerApplication records them in stores
   (Dataset can be opened in PsiStudio)
   ↓
6. Click "Stop All" when finished
   ↓
7. All processes stop simultaneously
   ↓
8. Stop Session (properly closes all stores)
```

**Enabled States**:
- Initially **disabled** (no devices connected)
- **Enabled** when at least one device connected
- Grayed buttons when no applicable devices

#### Actions Panel

[[/images/ServerApplication_ActionsPanel.png]]

Main application controls:

| Button | Function |
|--------|----------|
| **Configure Server** | Open Configuration tab |
| **Start Session** | Begin new recording session |
| **Stop Session** | End current session |
| **Quit** | Close ServerApplication |

**Button States**:
- **Configure Server**: Always enabled
- **Start Session**: Always enabled
- **Stop Session**: Enabled when session active
- **Quit**: Always enabled (prompts if session active)

#### Log Display

[[/images/ServerApplication_LogDisplay.png]]

Real-time activity log.

**Log Features**:
- **Auto-scroll**: Automatically scrolls to newest entries


### Tab 2: Configuration

[[/images/ServerApplication_ConfigurationTab.png]]

The Configuration tab contains all server and recording settings.

#### Network Configuration

**RendezVous Server Settings**:

| Control | Description | Default | Valid Range |
|---------|-------------|---------|-------------|
| **RendezVous Host** | IP address to bind | localhost | IP or hostname |
| **RendezVous Port** | TCP port for coordination | 13330 | 1024-65535 |
| **Clock Port** | Port for time sync | 11500 | 1024-65535 |

**Host Address Options**:
- **0.0.0.0**: Listen on all network interfaces (recommended)
- **localhost**: Localhost only (local testing)
- **Specific IP**: Listen on specific network adapter

#### Store Mode Configuration

**Store Mode**:

| Mode | Description | Use Case |
|------|-------------|----------|
| **Independant** | 1 Stream = 1 store | Raw data storage before processing |
| **Process** | 1 process = 1 store | Default (best practice) |
| **Dictionnary** | user defined | RGPD (audio, webcam stream separate from others) |

[[/images/ServerApplication_StoreModeSelection.png]]

**Independant Mode**:
- Each remote stream have a store.
- Best for data performance.
- Limitation of data management.

**Process Mode**:
- Each remote process records into a store.
- Best for data management.
- Limitation of performance for high frequency/high size data.

**Dictonnary Mode**:
- Stream are recorded in user defined store (or Independant mode)
- The configuration is provided inside the extra configuration file see External Configuration part.


#### Dataset Configuration (Centralized Mode Only)

Enabled only when Store Mode = Centralized:

| Control | Description | Example |
|---------|-------------|---------|
| **Dataset Path** | Directory for stores | C:\SAAC_Data |
| **Dataset Name** | Dataset identifier | Experiment_2024 |
| **Session Mode** | Unique, Increment, Overwrite | Auto-increment |
| **Session Name** | Current session name | SessionCVE_2user |

**Dataset Path**:
- **Requirements**:
  - Fast storage (SSD recommended)
  - Sufficient space (estimate: streams × duration × data rate)
  - Permissions to write
- **Best Practices**:
  - Use dedicated data drive
  - Avoid network shares
  - Regular backups

**Dataset Structure**:

Depending on Store Mode, the structure varies:

**Independent Mode** (each stream has its own store):
```
C:\SAAC_Data\
└─ Experiment_2024\                      (Dataset Name)
    ├─ Experiment_2024.pds               (Dataset descriptor)
    └─ Session_001\                      (Session Name)
        ├─ VideoCapture1.FullScreen\    (Process.Stream folders)
        │   ├─ 000000.psi
        │   └─ 000000.psi.catalog
        ├─ VideoCapture1.TopLeft\
        │   ├─ 000000.psi
        │   └─ 000000.psi.catalog
        ├─ CameraApp1.Camera1\
        │   ├─ 000000.psi
        │   └─ 000000.psi.catalog
        └─ WhisperApp.Audio\
            ├─ 000000.psi
            └─ 000000.psi.catalog
```

**Process Mode** (streams grouped by process):
```
C:\SAAC_Data\
└─ Experiment_2024\
    ├─ Experiment_2024.pds
    └─ Session_001\
        ├─ VideoCapture1\              (Process folders)
        │   ├─ 000000.psi               (Multiple streams in one store)
        │   └─ 000000.psi.catalog
        ├─ CameraApp1\
        │   ├─ 000000.psi
        │   └─ 000000.psi.catalog
        └─ WhisperApp\
            ├─ 000000.psi
            └─ 000000.psi.catalog
```

**Dictionary Mode** (custom organization via configuration):
```
C:\SAAC_Data\
└─ Experiment_2024\
    ├─ Experiment_2024.pds
    └─ Session_001\
        ├─ Heads\                      (Custom store names)
        │   ├─ 000000.psi               (Multiple streams per store)
        │   └─ 000000.psi.catalog
        ├─ Hands\
        │   ├─ 000000.psi
        │   └─ 000000.psi.catalog
        └─ Referentials\
            ├─ 000000.psi
            └─ 000000.psi.catalog
```

#### Session Mode

Controls how sessions are created and named:

| Mode | Behavior | Use Case |
|------|----------|----------|
| **Unique** | All data goes into one session | Single long recording session |
| **Increment** | Creates new session each time | Multiple trials/recordings |
| **Overwrite** | Replaces existing session | Testing, limited disk space |

**Unique Mode**:
- One session for entire experiment
- Multiple Start/Stop cycles append to same session
- Best for: Continuous data collection
- Example: `MainSession` (keeps appending)

**Increment Mode**:
- Automatic sequential naming
- New session created each "Start Session"
- Pattern: `Session_001`, `Session_002`, `Session_003`
- Best for: Multiple trials, participants, conditions
- Zero-padded to 3 digits

**Overwrite Mode**:
- Deletes and recreates session
- Use with caution (data loss!)
- Best for: Testing configurations
- Example: `TestSession` (replaced each time)

**Custom Session Names**:
- User enters name in text box
- Can include timestamps, participant IDs, conditions
- Examples:
  - `Participant_P01_Baseline`
  - `Trial_2024-01-15_Morning`
  - `Experiment_ConditionA_Rep1`
  - `User1_Task2_Attempt3`

#### Debug Options

| Control | Description | Performance Impact |
|---------|-------------|-------------------|
| **Get Streams Logs** | Log every incoming message | Medium-High |

**Use Cases**:
- Logs every incoming stream messages
- Tracks dropped frames
- Troubleshooting stream or application issues
- Increases log verbosity


#### External Configuration

| Control | Description | Example |
|---------|-------------|---------|
| **Extra Configuration** | JSON config file path | C:\Configs\experiment.json |

**External Configuration File**:

Provides advanced stream organization, primarily used for **Dictionary Store Mode**. This JSON file maps individual streams to custom store names, allowing flexible data organization.
It include stream definitions like topic, type, classFormat, and streamToStore for Unity application as their are technical limitations.

**Purpose**:
- Group related streams into custom stores
- Organize data by body part, sensor type, or function
- Control which streams go into which store files

**Example Configuration** (from testDLL.json):

```json
[
  {
    "topic": "1-Head",
    "type": "System.Tuple`2[[System.Numerics.Vector3, System.Numerics.Vectors],[System.Numerics.Vector3, System.Numerics.Vectors]]",
    "classFormat": "PsiFormatTupleOfVector",
    "streamToStore": "Heads"
  },
  {
    "topic": "2-Head",
    "type": "System.Tuple`2[[System.Numerics.Vector3, System.Numerics.Vectors],[System.Numerics.Vector3, System.Numerics.Vectors]]",
    "classFormat": "PsiFormatTupleOfVector",
    "streamToStore": "Heads"
  },
  {
    "topic": "1-LeftWrist",
    "type": "System.Tuple`2[[System.Numerics.Vector3, System.Numerics.Vectors],[System.Numerics.Vector3, System.Numerics.Vectors]]",
    "classFormat": "PsiFormatTupleOfVector",
    "streamToStore": "LeftHands"
  },
  {
    "topic": "2-LeftWrist",
    "type": "System.Tuple`2[[System.Numerics.Vector3, System.Numerics.Vectors],[System.Numerics.Vector3, System.Numerics.Vectors]]",
    "classFormat": "PsiFormatTupleOfVector",
    "streamToStore": "LeftHands"
  },
  {
    "topic": "1-RightWrist",
    "type": "System.Tuple`2[[System.Numerics.Vector3, System.Numerics.Vectors],[System.Numerics.Vector3, System.Numerics.Vectors]]",
    "classFormat": "PsiFormatTupleOfVector",
    "streamToStore": "RightHands"
  },
  {
    "topic": "TaskEvent",
    "type": "System.String",
    "classFormat": "PsiFormatString",
    "streamToStore": "Task"
  },
  {
    "topic": "Generator1Pos",
    "type": "System.Tuple`2[[System.Numerics.Vector3, System.Numerics.Vectors],[System.Numerics.Vector3, System.Numerics.Vectors]]",
    "classFormat": "PsiFormatTupleOfVector",
    "streamToStore": "Referentials"
  },
  {
    "topic": "Generator2Pos",
    "type": "System.Tuple`2[[System.Numerics.Vector3, System.Numerics.Vectors],[System.Numerics.Vector3, System.Numerics.Vectors]]",
    "classFormat": "PsiFormatTupleOfVector",
    "streamToStore": "Referentials"
  }
]
```

**Configuration Fields**:

| Field | Description | Example |
|-------|-------------|---------|
| **topic** | Stream name from remote process | "1-Head", "TaskEvent" |
| **type** | Full .NET type name | "System.String" |
| **classFormat** | Serialization format class | "PsiFormatString" |
| **streamToStore** | Target store name | "Heads", "Task" |

**Result**:
```
Session_001\
├─ Heads\            (Contains 1-Head, 2-Head, 3-Head)
├─ LeftHands\        (Contains all LeftWrist streams)
├─ RightHands\       (Contains all RightWrist streams)
├─ Task\             (Contains TaskEvent)
└─ Referentials\     (Contains Generator positions)
```

**Benefits**:
- **Logical Organization**: Group related data together
- **Performance**: Separate high-frequency from low-frequency data
- **Analysis**: Easier to load related streams
- **Storage**: Different stores can have different settings

#### Configuration Persistence

**Save/Load Buttons**:

| Button | Function |
|--------|----------|
| **Load Configuration** | Load settings |
| **Save Configuration** | Save current settings |

#### Configuration Logs

Same as in the overview.

### Tab 3: Annotation

[[/images/ServerApplication_AnnotationTab.png]]

The Annotation tab configures the web-based annotation interface for live labeling.

#### Enable Annotations

| Control | Description | Default |
|---------|-------------|---------|
| **Enable Annotations** | Activate annotation server | inactive |

**When Enabled**:
- Starts embedded web server
- Hosts annotation interface
- Stores annotations in Psi stores

**When Disabled**:
- No annotation server

#### Annotation Settings

**Annotation Schema Directory**:

| Control | Description | Example |
|---------|-------------|---------|
| **Schema Directory** | Path to annotation schemas | C:\Users\username\Documents\PsiStudio\AnnotationSchemas |

**Schema Location**:
- Can use PsiStudio's default schemas: `C:\Users\<username>\Documents\PsiStudio\AnnotationSchemas`
- Or custom directory with your own schemas
- Schemas are compatible with [PsiStudio Time-Interval Annotations](https://github.com/microsoft/psi/wiki/Time-Interval-Annotations)
- Multiple schema files (.schema.json) can be in the same directory

**Annotation Web Page**:

| Control | Description | Default |
|---------|-------------|---------|
| **Web Page (HTML)** | Custom annotation UI | Built-in interface |

**Options**:
- **Built-in**: Use default SAAC annotation interface
- **Custom**: Provide custom HTML/JavaScript interface

**Annotation Port**:

| Control | Description | Default | Range |
|---------|-------------|---------|-------|
| **Annotation Port** | HTTP port for web interface | 8080 | 1024-65535 |

**Accessing Annotation Interface**:
- URL: `http://localhost:8080` (local)
- URL: `http://<server-ip>:8080` (remote)
- Opens in any modern web browser
- Real-time updates via WebSocket

**Annotation Workflow**:
```
1. Configure annotation settings
   ↓
2. Enable annotations
   ↓
3. Start session
   ↓
4. Open browser to http://<server-ip>:8080
   ↓
5. Annotation interface loads with schemas
   ↓
6. Select the annotation schema to use and a username
   ↓
7. Add annotations via buttons/hotkeys
   ↓
8. Annotations saved to Psi store in real-time
   ↓
9. Stop session
   ↓
10. Annotations available in PsiStudio for analysis
```

**Annotation Interface Features**:
- **Timeline Visualization**: See all streams on synchronized timeline
- **Multi-User**: Multiple annotators simultaneously

**Annotation Storage**:
```
Annotations stored as Psi stream:
├─ Stream name: "Annotations"
├─ Data type: Annotation
├─ Fields:
│   ├─ Timestamp (DateTime)
│   ├─ Type (string)
│   ├─ Value (object)
│   ├─ Annotator (string)
│   └─ Confidence (double)
└─ Indexed for fast retrieval
```

## Common Workflows

### Workflow 1: Basic Coordination (Independent Recording)

**Scenario**: Coordinate multiple applications with local independent recording

**Configuration**:
1. **Configuration Tab**:
   - RendezVous Host: localhost
   - RendezVous Port: 13330
   - Store Mode: **Independent**
   - Dataset Path: D:\Recordings
   - Dataset Name: Experiment_2024
   - Session Mode: Unique
   - Session Name: MainSession

2. Save configuration

3. Click **"Start Session"**

4. Start remote applications:
   - VideoRemoteApp
   - CameraRemoteApp
   - WhisperRemoteApp

5. **Overview Tab**:
   - Verify all appear in Connected Devices
   - Click **"Start All"**

6. All applications begin simultaneously
   - Each stream recorded in separate store folder
   - Best performance for high-frequency data

7. Click **"Stop All"** when finished

8. Click **"Stop Session"**

**Result**: Synchronized multi-modal capture with independent stores per stream

### Workflow 2: Process-Based Recording

**Scenario**: Group streams by process for better data management

**Configuration**:
1. **Configuration Tab**:
   - RendezVous Host: 0.0.0.0
   - Store Mode: **Process**
   - Dataset Path: D:\Experiments
   - Dataset Name: UserStudy_2024
   - Session Mode: Increment
   - Session Name: Session

2. Save configuration

3. Click **"Start Session"**
   - Session_001 created automatically

4. Start remote applications

5. **Overview Tab**:
   - All devices appear
   - Click **"Start All"**

6. ServerApplication records all streams grouped by process:
   - D:\Experiments\UserStudy_2024\Session_001\VideoCapture1\ (all video streams)
   - D:\Experiments\UserStudy_2024\Session_001\CameraApp1\ (all camera streams)
   - D:\Experiments\UserStudy_2024\Session_001\WhisperApp\ (all audio streams)

7. Click **"Stop All"** then **"Stop Session"** when finished

8. Repeat for more trials:
   - Click **"Start Session"** again → creates Session_002

**Result**: Organized data by process, multiple sessions for trials

### Workflow 3: Live Annotation

**Scenario**: Real-time behavioral annotation during experiment

**Setup**:
1. **Configuration Tab**:
   - Store Mode: Centralized
   - Dataset Path: C:\Studies
   - Dataset Name: Behavior_Study

2. **Annotation Tab**:
   - Enable Annotations: ?
   - Schema Directory: C:\Schemas\Behavior
   - Annotation Port: 8080

3. Save configuration

4. Start session

5. Start remote applications

6. Open browser to http://localhost:8080

7. Annotation interface loads with behavior schema

8. Click **"Start All"**

9. Annotator watches live video and marks behaviors:
   - Press "1" for "Hand Raise"
   - Press "2" for "Turn Head"
   - Type notes for interesting events

10. Stop session

**Result**: Video data + synchronized behavioral annotations

## Troubleshooting

### Connection Issues

**No Processes Connecting**:
- Verify ServerApplication is running
- Check firewall allows port 13330
- Test with: `telnet <server-ip> 13330`
- Verify remote apps configured with correct server address
- Check network connectivity: `ping <server-ip>`

**Process Connects Then Disconnects**:
- Check network stability
- Verify no port conflicts
- Review server logs for errors
- Check process logs for exceptions

**"Connection Refused" Error**:
- ServerApplication not started
- Wrong port number
- Firewall blocking
- Server crashed (check logs)

### Recording Issues

**Store Creation Failed**:
- Verify Dataset Path exists
- Check disk permissions
- Ensure sufficient free space (>10 GB)
- Check path length (<260 characters)
- Avoid special characters

**Streams Not Recording**:
- Verify Store Mode = Centralized
- Check process is Running (not Standby)
- Review debug logs (enable "Get Streams Logs")
- Verify stream is being exported by remote app
- Check network connectivity

**High CPU/Memory Usage**:
- Too many streams for single machine
- Consider Process Store Mode (distributed)
- Reduce stream count
- Lower frame rates
- Check for memory leaks (restart)

### Annotation Issues

**Annotation Page Won't Load**:
- Verify "Enable Annotations" is checked
- Check annotation port (default 8080)
- Test URL: `http://localhost:8080`
- Check firewall allows port 8080
- Verify web server started (check logs)

**Schemas Not Loading**:
- Verify Schema Directory path is correct
- Check schema JSON files are valid
- Ensure schema files have .json extension
- Review configuration logs for errors

**Annotations Not Saving**:
- Verify session is running
- Check disk space
- Review annotation logs
- Ensure web interface connected (check console)

### Performance Issues

**Slow Response Times**:
- Too many connected processes
- Network bandwidth saturated
- Check CPU/memory usage
- Reduce stream count
- Use faster network (gigabit)

**Dropped Frames**:
- Disk too slow (use SSD)
- CPU overloaded
- Check per-stream statistics
- Consider distributed recording
- Reduce simultaneous streams

## Best Practices

### Network Configuration

1. **Dedicated Network**:
   - Use isolated network for SAAC
   - Gigabit Ethernet minimum
   - Switch, not hub
   - Document network topology

2. **Port Management**:
   - Document all port assignments
   - Avoid conflicts with other services
   - Use consistent port ranges
   - Configure firewalls beforehand

3. **Testing**:
   - Test connectivity before experiments
   - Verify all devices can reach server
   - Check bandwidth availability
   - Run dry runs

### Recording Strategy

1. **Centralized vs Distributed**:
   - **Centralized**: Simplifies analysis, single location
   - **Distributed**: Better for bandwidth-limited networks
   - Hybrid: Critical data centralized, rest distributed

2. **Storage Planning**:
   - Estimate required space:
     - Video: ~14 GB/hour (Full HD, 30fps)
     - Audio: ~100 MB/hour
     - Body tracking: ~50 MB/hour
     - Total: ~15-20 GB/hour for full setup
   - Plan for 2-3x estimated (safety margin)
   - Use fast storage (SSD, RAID)

3. **Session Management**:
   - Use auto-increment for trials
   - Manual names for distinct experiments
   - Keep sessions under 2 hours
   - Document session purposes

### Annotation Workflow

1. **Schema Design**:
   - Define schemas before capture
   - Test with annotators beforehand
   - Use clear, unambiguous labels
   - Assign intuitive hotkeys

2. **Annotation Team**:
   - Train annotators on interface
   - Establish coding guidelines
   - Inter-rater reliability checks
   - Regular calibration meetings

3. **Live vs Post-Capture**:
   - **Live**: Real-time, better context, can be stressful
   - **Post-Capture**: More flexible, can replay, slower
   - Hybrid: Mark events live, detail post-capture

### Production Deployment

1. **Pre-Deployment Checklist**:
   - [ ] Network configured and tested
   - [ ] Firewall rules created
   - [ ] Storage capacity verified
   - [ ] All applications installed
   - [ ] Configurations saved and backed up

2. **Monitoring**:
   - Watch Connected Devices panel
   - Monitor logs for warnings
   - Check disk space periodically
   - Track session durations
   - Note any anomalies

3. **Maintenance**:
   - Clean old session data
   - Update applications periodically
   - Review and update documentation