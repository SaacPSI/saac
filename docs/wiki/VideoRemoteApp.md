# VideoRemoteApp - User Guide

## Overview

**VideoRemoteApp** is a screen capture application that captures desktop regions and streams them to the SAAC framework. It provides flexible cropping, JPEG encoding, network streaming via RendezVous, and optional local recording capabilities.

**Key Features**:
- ✅ Full screen capture
- ✅ Multiple cropping regions with custom naming
- ✅ Visual cropping area selector
- ✅ JPEG encoding with quality control (0-100)
- ✅ Network streaming via RendezVous protocol
- ✅ Local recording to Psi stores
- ✅ Hybrid mode (simultaneous stream + record)
- ✅ Frame rate control per capture

## User Interface

The VideoRemoteApp window is organized into five tabs: **General**, **Captures**, **Network**, **Local Recording**, and **Log**.

[[/images/VideoRemoteApp_MainWindow.png]]

### Tab 1: General

[[/images/VideoRemoteApp_GeneralTab.png]]

The General tab provides application status, quick configuration toggles, and main actions.

#### General Configuration

Quick access toggles for main features:

| Control | Description | Default |
|---------|-------------|---------|
| **Network** | Enable RendezVous connection | ☑ |
| **Streaming** | Enable data streaming to network | ☑ |
| **Local Recording** | Enable local Psi store recording | ☐ |

**Control Relationships**:
```
Network Enabled
    ↓
Streaming Enabled (requires Network)
    ↓
Captures streamed to RendezVous server
    ↓
ServerApplication receives streams

Local Recording Enabled (independent)
    ↓
Captures saved to local Psi store
```

**Configuration Persistence**:

| Button | Function |
|--------|----------|
| **Load Configuration** | Load previous settings  | 
| **Save Configuration** | Save current settings  |

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
4. If Local Recording enabled: Create Psi stores
   ↓
5. Begin screen capture with configured regions
   ↓
6. Application state: Running
```

**Start Network Only Workflow**:
```
1. Click "Start Network Only"
   ↓
2. Connect to RendezVous server (Application state: Connected)
   ↓
3. Wait for Run command from ServerApplication
   ↓
4. Register process and stream endpoints
   ↓
5. Application state: Running
```

### Tab 2: Captures

[[/images/VideoRemoteApp_CapturesTab.png]]

The Captures tab configures screen capture parameters and cropping regions.

#### Capture Settings

**Capture Interval**:

| Control | Description | Unit | Example |
|---------|-------------|------|---------|
| **Capture Interval (ms)** | Milliseconds between frames | ms | 33 |

**Frame Rate Calculation**:
- 30 fps = 33 ms interval
- 60 fps = 16 ms interval  
- 15 fps = 66 ms interval
- 10 fps = 100 ms interval
- 1 fps = 1000 ms interval

**Encoding Level**:

| Control | Description | Range | Recommended |
|---------|-------------|-------|-------------|
| **Encoding Level (0-100)** | JPEG compression quality | 0-100 | 85 |

**JPEG Quality Guide**:
- **95-100**: Maximum quality, ~200 KB/frame
- **80-95**: High quality, ~100-150 KB/frame (**recommended**)
- **60-80**: Medium quality, ~50-80 KB/frame
- **40-60**: Low quality, ~20-40 KB/frame
- **<40**: Very low quality, visible artifacts

**Performance Impact**:
```
Higher Quality (95-100)
  ↓
  • Larger file sizes
  • Higher bandwidth usage
  • Better image fidelity
  • More CPU for encoding

Lower Quality (40-60)
  ↓
  • Smaller file sizes
  • Lower bandwidth usage
  • Reduced image fidelity
  • Less CPU for encoding
```

#### Cropping Areas

Manage multiple screen regions for simultaneous capture:

[[/images/VideoRemoteApp_CroppingAreas.png]]

**Cropping Area List**:
```
┌────────────────────────────────────────┐
│ Cropping Areas                         │
├────────────────────────────────────────┤
│ • FullScreen                           │
│   └─ Stream: "FullScreen"              │
│                                        │
│ • TopLeft                              │
│   └─ Stream: "TopLeft"                 │
│                                        │
│ • TopRight                             │
│   └─ Stream: "TopRight"                │
│                                        │
│ • CustomRegion                         │
│   └─ Stream: "Application"             │
└────────────────────────────────────────┘
```

**Manage Buttons**:

| Button | Function |
|--------|----------|
| **Add** | Create new cropping area |
| **Delete** | Remove selected area |
| **Select Zone** | Visual region selector |

**Add Cropping Area Workflow**:
```
1. Click "Add" button
   ↓
2. New area appears in list
   ↓
3. Edit Name, X, Y, Width, Height below
   ↓
4. Or click "Select Zone" for visual editor
   ↓
5. Area ready for capture
```

#### Edit Selected Cropping Area

Configure the selected cropping region:

| Control | Description | Range | Example |
|---------|-------------|-------|---------|
| **Name** | Display name for this region | Text | "TopLeft" |
| **X** | Left edge position | pixels | 0 |
| **Y** | Top edge position | pixels | 0 |
| **Width** | Region width | pixels | 960 |
| **Height** | Region height | pixels | 540 |

**Coordinate System**:
```
(0,0) ┌──────────────────────────────┐
      │                              │
      │  Screen Coordinates          │
      │                              │
      │  (X,Y) ┌─────────┐           │
      │        │ Region  │           │
      │        │  W×H    │           │
      │        └─────────┘           │
      │                              │
      └──────────────────────────────┘
                               (ScreenWidth, ScreenHeight)
```

**Common Cropping Configurations**:

**Full Screen**:
```
Name: FullScreen
X: 0, Y: 0
Width: 1920, Height: 1080
```

**Screen Quadrants** (for 1920x1080):
```
TopLeft:     X=0,    Y=0,   W=960, H=540
TopRight:    X=960,  Y=0,   W=960, H=540
BottomLeft:  X=0,    Y=540, W=960, H=540
BottomRight: X=960,  Y=540, W=960, H=540
```

**Horizontal Halves**:
```
Top:    X=0, Y=0,   W=1920, H=540
Bottom: X=0, Y=540, W=1920, H=540
```

**Vertical Halves**:
```
Left:  X=0,    Y=0, W=960,  H=1080
Right: X=960,  Y=0, W=960,  H=1080
```

#### Visual Zone Selector

Click **"Select Zone"** to open interactive region selector:

[[/images/VideoRemoteApp_VisualSelector.png]]

**Visual Selector Features**:
- **Live Preview**: Current screen content
- **Click & Drag**: Define region visually
- **Resize Handles**: Adjust boundaries
- **Coordinate Display**: Real-time X, Y, W, H
- **Apply**: Save region to list

**Visual Selector Workflow**:
```
1. Click "Select Zone" button
   ↓
2. Screenshot of current desktop appears
   ↓
3. Click and drag to create rectangular region
   ↓
4. Fine-tune with resize handles
   ↓
5. Coordinates populate automatically
   ↓
6. Click "Apply" to save
   ↓
7. Return to Captures tab
```

**Tips**:
- Use ruler/grid overlay for precision
- Test region with preview before saving
- Document region purposes
- Name regions descriptively

### Tab 3: Network

[[/images/VideoRemoteApp_NetworkTab.png]]

The Network tab configures RendezVous connection and streaming settings.

#### Network Activation

| Control | Description | Effect |
|---------|-------------|--------|
| **Activate** | Enable network features | Enables all network settings |

**When Activated**:
- Application will attempt RendezVous connection
- Streams will be exported on Run command
- Command listener will be active

**When Deactivated**:
- Local recording only mode
- No network communication
- Standalone operation

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
- **Hostname**: DNS-resolvable name

**Port Configuration**:
```
Default Ports:
├─ RendezVous: 13330 (TCP)
├─ Command: 11511 (TCP)
└─ Streaming: 11411+ (TCP)

Firewall Rules Needed:
├─ Outbound: 13330 (RendezVous registration)
├─ Outbound: 11411-11420 (Stream export)
└─ Inbound: 11511 (Commands from server)
```

#### Application Configuration

Settings for this VideoRemoteApp instance:

| Control | Description | Example |
|---------|-------------|---------|
| **Activate Streaming** | Enable stream export | ☑ |
| **IP To Use** | Local IP address for streams | 192.168.1.105 |
| **Application Name** | Unique process identifier | VideoCapture1 |
| **Streaming Port Range Start** | First port for stream export | 11411 |

**IP To Use**:
- Dropdown populated with available network adapters
- Select adapter connected to RendezVous network

**Application Name**:
- Must be unique across all processes
- Appears in ServerApplication's process list
- Used as part of store naming in datasets.

**Streaming Port Allocation**:
```
Base Port: 11411

Cropping Areas:
├─ FullScreen → Port 11411
├─ TopLeft → Port 11412
├─ TopRight → Port 11413
└─ CustomRegion → Port 11414

Each region increments port by 1
```

**Streaming Workflow**:
```
1. VideoRemoteApp connects to RendezVous server
   ↓
2. Registers process with Application Name
   ↓
3. Advertises stream endpoints:
   - Stream "FullScreen" on 192.168.1.105:11411
   - Stream "TopLeft" on 192.168.1.105:11412
   - etc.
   ↓
4. ServerApplication sees streams
   ↓
5. Sends Run command
   ↓
6. VideoRemoteApp begins streaming
   ↓
7. ServerApplication receives and records streams
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

### Tab 4: Local Recording

[[/images/VideoRemoteApp_LocalRecordingTab.png]]

The Local Recording tab configures local Psi store recording.

#### Local Recording Activation

| Control | Description | Default |
|---------|-------------|---------|
| **Activate** | Enable local recording | ☐ |

**When Activated**:
- Captures saved to local Psi stores
- Works independently of network streaming
- Guaranteed local backup

**When Deactivated**:
- Network streaming only
- No local data storage

#### Recording Settings

| Control | Description | Example |
|---------|-------------|---------|
| **Session Name** | Current recording session | Session_001 |
| **Dataset Directory** | Path to store datasets | D:\VideoCaptures |
| **Dataset Name** | Dataset identifier | ScreenCaptures |

**Session Name**:
- Unique identifier for this recording session
- Can include timestamps, participant IDs
- Examples:
  - `Session_2024-01-15_Morning`
  - `Participant_P01_Baseline`
  - `Trial_05_ConditionA`

**Dataset Directory**:
- Root directory for all datasets
- Click **Browse** to select folder
- Requirements:
  - Fast storage (SSD recommended)
  - Sufficient free space
  - Write permissions
- Recommended: Dedicated data drive

**Dataset Name**:
- Base name for this dataset
- Click **Browse** to select existing dataset or create new
- Creates `.pds` file automatically
- Examples:
  - `ScreenCaptures_2024`
  - `ExperimentData`
  - `UserStudy_VideoData`

#### Dataset Structure

```
D:\VideoCaptures\                        (Dataset Directory)
└─ ScreenCaptures\                       (Dataset Name)
    ├─ ScreenCaptures.pds                (Dataset descriptor)
    └─ Session_001\                      (Session Name)
        ├─ VideoCapture1.FullScreen\     (Process.Stream folders)
        │   ├─ 000000.psi
        │   └─ 000000.psi.catalog
        ├─ VideoCapture1.TopLeft\
        │   ├─ 000000.psi
        │   └─ 000000.psi.catalog
        └─ VideoCapture1.TopRight\
            ├─ 000000.psi
            └─ 000000.psi.catalog
```

**Storage Size Estimates** (at 30 fps, JPEG quality 85):
- Full HD (1920x1080): ~230 MB/min (~14 GB/hour)
- HD (1280x720): ~100 MB/min (~6 GB/hour)
- Quarter screen (960x540): ~50 MB/min (~3 GB/hour)

**Per Cropping Region**:
- Each region = separate store
- Multiple regions multiply storage requirements
- Example: 4 regions × 50 MB/min = 200 MB/min

### Tab 5: Log

[[/images/VideoRemoteApp_LogTab.png]]

The Log tab displays real-time application events and status messages.

## Common Workflows

### Workflow 1: Basic Full Screen Recording

**Scenario**: Capture full desktop and stream to ServerApplication

**Configuration**:
1. **General Tab**:
   - Network: ☑
   - Streaming: ☑
   - Local Recording: ☐

2. **Captures Tab**:
   - Capture Interval: 33 ms (30 fps)
   - Encoding Level: 85
   - Cropping Areas: "FullScreen" (0, 0, 1920, 1080)

3. **Network Tab**:
   - Server IP: 192.168.1.100
   - Server Port: 13330
   - Application Name: VideoCapture1
   - Streaming Port: 11411

4. Save configuration

5. Click **"Start"**

6. In ServerApplication, click **"Start All"** when ready

**Result**: Full screen streamed to ServerApplication at 30 fps

### Workflow 2: Multi-Region Capture

**Scenario**: Capture screen divided into quadrants for separate analysis

**Configuration**:
1. **Captures Tab** - Add 4 regions:
   - TopLeft: (0, 0, 960, 540)
   - TopRight: (960, 0, 960, 540)
   - BottomLeft: (0, 540, 960, 540)
   - BottomRight: (960, 540, 960, 540)

2. **Network Tab**:
   - Streaming Port Range: 11411
   - Result: 4 streams on ports 11411-11414

3. Click **"Start"**

**Result**: 4 separate synchronized streams, each showing screen quadrant

### Workflow 3: Local Recording with Streaming

**Scenario**: Stream to ServerApplication with local backup

**Configuration**:
1. **General Tab**:
   - Network: ☑
   - Streaming: ☑
   - Local Recording: ☑

2. **Network Tab**:
   - Configure RendezVous connection

3. **Local Recording Tab**:
   - Session Name: Backup_Session_001
   - Dataset Directory: D:\Backups
   - Dataset Name: VideoBackups

4. Click **"Start"**

**Result**: Streams to network AND saves locally for redundancy

### Workflow 4: Network-Only Mode

**Scenario**: Connect to RendezVous but wait for Run command

**Configuration**:
1. Configure all settings as needed

2. **General Tab**:
   - Click **"Start Network Only"**

3. Wait for Run command from ServerApplication

4. ServerApplication sends Run command

5. Capture begins automatically

**Result**: Coordinated start with other applications

## Integration with SAAC

### ServerApplication Configuration

In ServerApplication, streams are automatically discovered:

```csharp
// ServerApplication receives streams automatically
// Each cropping region appears as separate stream:
// - VideoCapture1.FullScreen
// - VideoCapture1.TopLeft
// - VideoCapture1.TopRight
```

**In ServerApplication Configuration Tab**:
```
Store Mode: Process
Dataset Path: D:\Experiments
Dataset Name: ScreenCapture_Study

On "Start Session":
  → Creates stores for all discovered streams
  → D:\Experiments\ScreenCapture_Study\Session_001\VideoCapture1\
```

### PsiStudio Visualization

After recording:

1. Open PsiStudio
2. File → Open Dataset
3. Navigate to Dataset Path
4. Select `.pds` file
5. All video streams appear on timeline
6. Synchronized with other modalities

## Troubleshooting

### Connection Issues

**Cannot Connect to RendezVous Server**:
- Verify ServerApplication is running
- Check Server IP and Port
- Test with: `telnet <server-ip> 13330`
- Check firewall allows outbound connection
- Verify network connectivity: `ping <server-ip>`

**Connection Established But No Streams**:
- Check "Streaming" checkbox is enabled
- Verify "Activate Streaming" in Network tab
- Check application name is unique
- Review logs for registration errors

### Capture Issues

**Black/Empty Frames**:
- Run as Administrator (required for Desktop Duplication)
- Check screen resolution matches cropping coordinates
- Verify no DRM-protected content (blocks capture)

**Low Frame Rate**:
- Reduce Encoding Level (lower quality)
- Increase Capture Interval (lower fps)
- Reduce number of cropping regions
- Use faster CPU/GPU

**High CPU Usage**:
- Increase Capture Interval
- Reduce Encoding Level
- Reduce capture resolution
- Limit number of cropping regions

### Streaming Issues

**Streams Not Appearing in ServerApplication**:
- Check Application Name is correct
- Verify Streaming Port Range is available
- Check firewall allows outbound connections
- Review Network tab configuration

**Choppy/Laggy Streams**:
- Check network bandwidth
- Reduce Encoding Level
- Increase Capture Interval
- Check network latency: `ping <server-ip>`

### Recording Issues

**Store Creation Failed**:
- Verify Dataset Directory exists
- Check write permissions
- Ensure sufficient disk space

**Recording Stops Unexpectedly**:
- Check disk space
- Monitor disk write speed
- Review logs for errors
- Check for file system errors

## Best Practices

### Capture Configuration

1. **Frame Rate**:
   - 30 fps for general capture
   - 60 fps for motion-intensive content
   - 15 fps for static dashboards
   - 10 fps for monitoring

2. **Encoding Quality**:
   - 85-95 for analysis/archival
   - 70-85 for streaming
   - 50-70 for bandwidth-limited
   - Test to find optimal balance

3. **Cropping Regions**:
   - Limit to 4-6 active regions
   - Name regions descriptively
   - Document region purposes
   - Test before production

### Network Configuration

1. **Naming Convention**:
   - Use descriptive Application Names
   - Include location/purpose
   - Avoid spaces (use underscores)
   - Keep unique per instance

2. **Port Management**:
   - Document port assignments
   - Reserve port range (e.g., 11411-11420)
   - Avoid conflicts with other services
   - Configure firewall beforehand

3. **Testing**:
   - Test connectivity before experiments
   - Verify streams appear in ServerApplication
   - Check bandwidth with all regions
   - Run test captures

### Storage Strategy

1. **Local Recording**:
   - Use for critical data backup
   - Fast SSD recommended
   - Monitor disk space
   - Plan for 2-3x estimated size

2. **Dataset Organization**:
   - One dataset per experiment/study
   - Multiple sessions per dataset
   - Descriptive session names
   - Regular backups

3. **Storage Planning**:
   - Estimate space: # regions × fps × quality × duration
   - Full HD, 30fps, quality 85 ≈ 14 GB/hour
   - Multiple regions multiply requirements
   - Keep sessions under 2 hours

### Production Deployment

1. **Pre-Deployment Checklist**:
   - [ ] Administrator privileges confirmed
   - [ ] Network connectivity tested
   - [ ] Firewall rules configured
   - [ ] Storage capacity verified
   - [ ] Configuration saved and backed up
   - [ ] Cropping regions tested
   - [ ] Test capture completed

2. **During Capture**:
   - Monitor Log tab for errors
   - Check frame rate stability
   - Watch disk space (if recording)
   - Note any anomalies

3. **Post-Capture**:
   - Verify all streams recorded
   - Check data integrity
   - Archive/backup data
   - Document any issues