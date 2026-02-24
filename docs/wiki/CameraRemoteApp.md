# CameraRemoteApp - User Guide

## Overview

**CameraRemoteApp** is a multi-sensor capture application supporting various depth cameras and RGB cameras. It provides unified configuration for Azure Kinect, Nuitrack-compatible sensors (Orbbec, RealSense), Kinect v2, and standard cameras with network streaming and local recording capabilities.

**Key Features**:
- ✅ Azure Kinect DK support (RGB, Depth, Skeleton, Audio, IMU)
- ✅ Nuitrack SDK support (Orbbec, RealSense, etc.)
- ✅ Kinect v2 support (RGB, Depth, Skeleton, Audio, Infrared)
- ✅ Standard camera support 
- ✅ Multiple output streams per sensor
- ✅ JPEG encoding with quality control
- ✅ Network streaming via RendezVous protocol
- ✅ Local recording to Psi stores
- ✅ Configurable frame rates and resolutions
- ✅ Hybrid mode (simultaneous stream + record)

## User Interface

The CameraRemoteApp window is organized into five tabs: **General**, **Video Sources**, **Network**, **Local Recording**, and **Log**.

[[/images/CameraRemoteApp_MainWindow.png]]

### Tab 1: General

[[/images/CameraRemoteApp_GeneralTab.png]]

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
Sensor data streamed to RendezVous server
    ↓
ServerApplication receives streams

Local Recording Enabled (independent)
    ↓
Sensor data saved to local Psi store
```

**Configuration Persistence**:

| Button | Function |
|--------|----------|
| **Load Configuration** | Load settings |
| **Save Configuration** | Save current settings |


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
3. Initialize selected sensor (Azure Kinect/Nuitrack/Kinect/Camera)
   ↓
4. If Streaming enabled: Export streams to network
   ↓
5. If Local Recording enabled: Create Psi stores
   ↓
6. Start capturing enabled streams
   ↓
7. Application state: Running
```

### Tab 2: Video Sources

[[/images/CameraRemoteApp_VideoSourcesTab.png]]

The Video Sources tab configures sensor selection and output stream settings.

#### Video Source Selection

| Control | Description | Options |
|---------|-------------|---------|
| **Video Source** | Select capture source | Azure Kinect, Nuitrack, Kinect, Camera |

**Video Source Types**:

```
┌─────────────────────────────────────────┐
│ Video Source: [Azure Kinect        ▼]   │
└─────────────────────────────────────────┘

Available Sources:
├─ Azure Kinect   → Azure Kinect DK
├─ Nuitrack       → Orbbec, RealSense, etc.
├─ Kinect         → Kinect v2
└─ Camera         → USB/IP cameras (Recognized by Windows)
```

**Source Selection Effects**:
- **Azure Kinect**: Activates Azure Kinect Configuration GroupBox
- **Nuitrack**: Activates Nuitrack Configuration GroupBox
- **Kinect**: Activates Kinect Configuration GroupBox
- **Camera**: Activates Camera Configuration GroupBox

#### Sensor Type Selection

| Control | Description | Options |
|---------|-------------|---------|
| **Sensor type** | Specific sensor model | Varies by Video Source |

**Sensor Type Options** (depends on Video Source):
- For Nuitrack: Orbbec Astra, RealSense D435, etc.
- For Camera: USB Camera, IP Camera
- Not applicable for Azure Kinect and Kinect v2

#### Azure Kinect Configuration

[[/images/CameraRemoteApp_AzureKinect.png]]

Configure streams for Azure Kinect DK:

```
┌──────────────────────────────────────────────────────────┐
│ Azure Kinect Configuration                               │
├──────────────────────────────────────────────────────────┤
│ ☑ Audio                                                 │
│ ☑ Skeleton                                              │
│ ☑ RGB                    [1920x1080 (16:9)        ▼]    │
│ ☑ Infrared                                              │
│ ☑ Depth                                                 │
│ ☑ DepthCalibration                                      │
│ ☑ IMU                                                   │
│ FPS: [30 fps                                      ▼]     │
└──────────────────────────────────────────────────────────┘
```

**Output Streams**:

| Stream | Description | Data Type | Size/Rate |
|--------|-------------|-----------|-----------|
| **Audio** | 7-microphone array | AudioBuffer | 48 kHz |
| **Skeleton** | Body tracking (up to 10 people) | List<Bodies> | 30 fps |
| **RGB** | Color camera | EncodedImage | Configurable resolution |
| **Infrared** | IR image | Image | 1024x1024 |
| **Depth** | Depth map | DepthImage | 640x576 or 512x512 |
| **DepthCalibration** | Camera calibration data | IDepthDeviceCalibrationInfo | Once at start |
| **IMU** | Accelerometer + Gyroscope | ImuSample | 1.6 kHz |

**Color Resolution Options**:
- 1280x720 (16:9) - HD
- 1920x1080 (16:9) - Full HD
- 2560x1440 (16:9) - 2K
- 2048x1536 (4:3) - 3 MP
- 3840x2160 (16:9) - 4K
- 4096x3072 (4:3) - 12 MP

**FPS Options**:
- 5 fps
- 15 fps
- 30 fps (recommended)

**Azure Kinect Features**:
- **Body Tracking**: Requires Azure Kinect Body Tracking SDK
- **Audio**: Beam-forming microphone array
- **IMU**: High-frequency inertial measurement
- **Depth + RGB**: Synchronized capture
- **Wide FOV**: 120° horizontal, 120° vertical (depth)

#### Nuitrack Configuration

[[/images/CameraRemoteApp_Nuitrack.png]]

Configure streams for Nuitrack-compatible sensors:

```
┌──────────────────────────────────────────────────────────┐
│ Nuitrack Configuration                                   │
├──────────────────────────────────────────────────────────┤
│ Activation Key:  [____________________________]          │
│ Device Serial Number: [____________________________]     │
│                                                          │
│ ☑ Skeleton                                               │
│ ☑ RGB                                                    │
│ ☑ Depth                                                  │
│ ☑ Hand                                                   │
│ ☑ User                                                   │
│ ☑ Gesture                                                │
└──────────────────────────────────────────────────────────┘
```

**Configuration Fields**:

| Field | Description | Required | Example |
|-------|-------------|----------|---------|
| **Activation Key** | Nuitrack license key | Yes | XXXX-XXXX-XXXX-XXXX |
| **Device Serial Number** | Specific sensor to use | Yes | 123456789 |

**Output Streams**:

| Stream | Description | Data Type | Notes |
|--------|-------------|-----------|-------|
| **Skeleton** | Body skeleton tracking | Skeleton data | Up to 6 people |
| **RGB** | Color image | Image | Sensor-dependent resolution |
| **Depth** | Depth map | DepthImage | Sensor-dependent resolution |
| **Hand** | Hand tracking | Hand data | Position, gestures |
| **User** | User segmentation | User mask | Per-user segmentation |
| **Gesture** | Gesture recognition | Gesture events | Wave, click, etc. |

**Supported Sensors**:
- **Orbbec Astra** series (Astra, Astra Pro, Astra+)
- **Intel RealSense** (D415, D435, D435i, D455)
- **Kinect v1** (Xbox 360 Kinect)
- **Other Nuitrack-compatible** depth cameras

**Nuitrack Features**:
- **AI Skeleton Tracking**: Works with various depth sensors
- **Gesture Recognition**: Built-in gesture library
- **Hand Tracking**: Detailed hand pose estimation

**License Requirements**:
- Free trial available (limited time)
- Perpetual license for production
- Online activation required
- Obtain from: https://nuitrack.com

#### Kinect Configuration

[[/images/CameraRemoteApp_Kinect.png]]

Configure streams for Kinect v2 (Xbox One Kinect):

```
┌──────────────────────────────────────────────────────────┐
│ Kinect Configuration                                     │
├──────────────────────────────────────────────────────────┤
│ ☑ Audio                                                 │
│ ☑ Skeleton                                              │
│ ☑ RGB                                                   │
│ ☑ RGB + Depth                                           │
│ ☑ Depth                                                 │
│ ☑ Depth Calibration                                     │
│ ☑ Infrared                                              │
│ ☑ Long Exposure Infrared                                │
│ ☑ Color To Camera Mapping                               │
└──────────────────────────────────────────────────────────┘
```

**Output Streams**:

| Stream | Description | Data Type | Resolution |
|--------|-------------|-----------|------------|
| **Audio** | 4-microphone array | AudioBuffer | 16 kHz |
| **Skeleton** | Body tracking | Bodies | Up to 6 people |
| **RGB** | Color image | EncodedImage | 1920x1080 @ 30fps |
| **RGB + Depth** | Aligned RGB and depth | EncodedImage + DepthImage | 1920x1080 |
| **Depth** | Depth map | DepthImage | 512x424 @ 30fps |
| **Depth Calibration** | Calibration info | IDepthDeviceCalibrationInfo | Once at start |
| **Infrared** | Infrared image | Image | 512x424 |
| **Long Exposure Infrared** | Long exposure IR | Image | 512x424 |
| **Color To Camera Mapping** | Pixel mapping | CoordinateMapper | Per frame |

**Kinect v2 Features**:
- **1080p RGB**: High-resolution color camera
- **Time-of-Flight Depth**: Accurate depth sensing
- **Wide FOV**: 70° horizontal, 60° vertical
- **Body Tracking**: Built-in skeleton tracking (no GPU required)
- **Audio**: Beam-forming microphone array

**Requirements**:
- Kinect v2 sensor + USB 3.0 adapter
- Kinect for Windows SDK 2.0
- USB 3.0 controller (dedicated recommended)
- Windows 8 or later

#### Camera Configuration

[[/images/CameraRemoteApp_Camera.png]]

Configure standard cameras:

```
┌──────────────────────────────────────────────────────────┐
│ Camera Configuration                                     │
├──────────────────────────────────────────────────────────┤
│ Capture Format: [MJPEG 1920x1080 @ 30fps         ▼]    │
└──────────────────────────────────────────────────────────┘
```

**Capture Format Dropdown**:
- Lists all formats supported by selected camera
- Format: `<Encoding> <Width>x<Height> @ <FPS>fps`
- Examples:
  - `MJPEG 1920x1080 @ 30fps`
  - `YUY2 1280x720 @ 60fps`
  - `RGB24 640x480 @ 30fps`

**Common Camera Encodings**:
- **MJPEG**: Motion JPEG (compressed)
- **YUY2**: YUV 4:2:2 (uncompressed)
- **RGB24**: 24-bit RGB (uncompressed)
- **H.264**: Hardware-encoded H.264 (if supported)

**Camera Detection**:
- Application automatically detects connected cameras
- Lists all available DirectShow/MediaFoundation devices
- Shows supported formats per camera

#### Image Encoding Level

Apply JPEG compression to all output images:

| Control | Description | Range | Default |
|---------|-------------|-------|---------|
| **Image Encoding Level** | JPEG quality (0-100) | 0-100 | 85 |

**JPEG Quality Guide**:
- **95-100**: Maximum quality, ~200 KB/frame
- **80-95**: High quality, ~100-150 KB/frame (**recommended**)
- **60-80**: Medium quality, ~50-80 KB/frame
- **40-60**: Low quality, ~20-40 KB/frame
- **<40**: Very low quality, visible artifacts

**Encoding Notes**:
- Applies to RGB, Color, and Infrared streams
- Does not affect Depth (uses lossless compression)
- Does not affect Skeleton, Audio, IMU (different formats)
- Higher quality = larger file sizes

### Tab 3: Network

[[/images/CameraRemoteApp_NetworkTab.png]]

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
- **x.x.x.x**: Any network
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

Settings for this CameraRemoteApp instance:

| Control | Description | Example |
|---------|-------------|---------|
| **Activate Streaming** | Enable stream export | ☑ |
| **IP To Use** | Local IP address for streams | 192.168.1.105 |
| **Application Name** | Unique process identifier | CameraApp1 |
| **Streaming Port Range Start** | First port for stream export | 11411 |

**IP To Use**:
- Dropdown populated with available network adapters
- Select adapter connected to RendezVous network
- **Auto-detect**: Application chooses best interface
- **Manual**: Specify exact IP address

**Application Name**:
- Must be unique across all processes
- Appears in ServerApplication's process list
- Naming convention: `Camera_<Sensor>_<Location>`
- Examples:
  - `Camera_AzureKinect_Lab1`
  - `Camera_Nuitrack_Room2`
  - `Camera_Kinect_Station3`

**Streaming Port Allocation**:
```
Base Port: 11411

Azure Kinect Example:
├─ Audio → Port 11411
├─ Skeleton → Port 11412
├─ RGB → Port 11413
├─ Infrared → Port 11414
├─ Depth → Port 11415
├─ DepthCalibration → Port 11416
└─ IMU → Port 11417

Each enabled stream increments port by 1
```

**Streaming Workflow**:
```
1. CameraRemoteApp connects to RendezVous server
   ↓
2. Registers process with Application Name
   ↓
3. Advertises stream endpoints:
   - Stream "Audio" on 192.168.1.105:11411
   - Stream "Skeleton" on 192.168.1.105:11412
   - Stream "RGB" on 192.168.1.105:11413
   - etc.
   ↓
4. ServerApplication sees streams
   ↓
5. Sends Run command
   ↓
6. CameraRemoteApp begins capturing and streaming
   ↓
7. ServerApplication receives all sensor streams
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

### Tab 4: Local Recording

[[/images/CameraRemoteApp_LocalRecordingTab.png]]

The Local Recording tab configures local Psi store recording.

#### Local Recording Activation

| Control | Description | Default |
|---------|-------------|---------|
| **Activate** | Enable local recording | ☐ |

**When Activated**:
- Sensor data saved to local Psi stores
- Works independently of network streaming
- Guaranteed local backup

**When Deactivated**:
- Network streaming only
- No local data storage

#### Recording Settings

| Control | Description | Example |
|---------|-------------|---------|
| **Session Name** | Current recording session | AzureKinect_Session_001 |
| **Dataset Directory** | Path to store datasets | D:\SensorData |
| **Dataset Name** | Dataset identifier | CameraCaptures |

**Session Name**:
- Unique identifier for this recording session
- Can include timestamps, sensor type, location
- Examples:
  - `AzureKinect_Lab1_2024-01-15`
  - `Nuitrack_Study_P01_Baseline`
  - `Kinect_RoomA_Trial05`

**Dataset Directory**:
- Root directory for all datasets
- Click **Browse** to select folder
- Requirements:
  - Fast storage (SSD strongly recommended)
  - Sufficient free space
  - Write permissions
- Recommended: Dedicated high-speed drive

**Dataset Name**:
- Base name for this dataset
- Click **Browse** to select existing dataset or create new
- Creates `.pds` file automatically
- Examples:
  - `AzureKinectData_2024`
  - `DepthSensors_Study`
  - `MultimodalCapture`

#### Dataset Structure

```
D:\SensorData\                           (Dataset Directory)
└─ CameraCaptures\                       (Dataset Name)
    ├─ CameraCaptures.pds                (Dataset descriptor)
    └─ AzureKinect_Session_001\          (Session Name)
        ├─ CameraApp1.Audio\             (Process.Stream folders)
        │   ├─ 000000.psi
        │   └─ 000000.psi.catalog
        ├─ CameraApp1.Skeleton\
        │   ├─ 000000.psi
        │   └─ 000000.psi.catalog
        ├─ CameraApp1.RGB\
        │   ├─ 000000.psi
        │   └─ 000000.psi.catalog
        ├─ CameraApp1.Depth\
        │   ├─ 000000.psi
        │   └─ 000000.psi.catalog
        └─ CameraApp1.IMU\
            ├─ 000000.psi
            └─ 000000.psi.catalog
```

**Storage Size Estimates** (at 30 fps):

**Azure Kinect** (per hour):
- RGB (1080p, JPEG 85): ~5 GB
- Depth: ~2 GB
- Skeleton: ~50 MB
- Audio: ~350 MB
- IMU: ~100 MB
- **Total**: ~7.5 GB/hour

**Kinect v2** (per hour):
- RGB (1080p, JPEG 85): ~5 GB
- Depth: ~1.5 GB
- Skeleton: ~50 MB
- Audio: ~100 MB
- **Total**: ~6.7 GB/hour

**Nuitrack** (varies by sensor, typical):
- RGB (720p, JPEG 85): ~2 GB
- Depth: ~1 GB
- Skeleton: ~50 MB
- **Total**: ~3 GB/hour

### Tab 5: Log

[[/images/CameraRemoteApp_LogTab.png]]

The Log tab displays real-time application events and status messages.

## Common Workflows

### Workflow 1: Azure Kinect Body Tracking

**Scenario**: Capture body motion with Azure Kinect DK

**Configuration**:
1. **General Tab**:
   - Network: ☑
   - Streaming: ☑
   - Local Recording: ☑

2. **Video Sources Tab**:
   - Video Source: Azure Kinect
   - Enable: Skeleton, RGB, Depth
   - Color Resolution: 1920x1080
   - FPS: 30 fps
   - Image Encoding Level: 85

3. **Network Tab**:
   - Server IP: 192.168.1.100
   - Application Name: AzureKinect_Lab1

4. **Local Recording Tab**:
   - Session Name: BodyTracking_2024-01-15
   - Dataset Directory: D:\MotionData
   - Dataset Name: BodyTracking

5. Click **"Start"**

6. In ServerApplication, click **"Start All"** when ready

**Result**:
- Body skeleton tracking at 30 fps
- RGB and depth streams
- Synchronized local recording and network streaming
- Data available for PsiStudio visualization

### Workflow 2: Multi-Kinect Setup

**Scenario**: Capture from 3 Azure Kinect sensors simultaneously

**Setup**:
1. Launch 3 CameraRemoteApp instances
2. Connect each to different Azure Kinect sensor
3. Configure each instance:
   - Instance 1: Application Name = "AzureKinect_Front"
   - Instance 2: Application Name = "AzureKinect_Left"
   - Instance 3: Application Name = "AzureKinect_Right"
4. Each uses different port ranges:
   - Instance 1: Base Port 11411
   - Instance 2: Base Port 11421
   - Instance 3: Base Port 11431

5. Start ServerApplication
6. Wait for all 3 processes to connect
7. Click **"Start All"**

**Result**:
- 3 synchronized Azure Kinect streams
- Multi-view body tracking
- Combined dataset in ServerApplication

### Workflow 3: Nuitrack with Orbbec Astra

**Scenario**: Budget-friendly body tracking with Orbbec Astra

**Configuration**:
1. **General Tab**:
   - Network: ☑
   - Streaming: ☑

2. **Video Sources Tab**:
   - Video Source: Nuitrack
   - Sensor type: Orbbec Astra
   - Activation Key: [Your Nuitrack License]
   - Enable: Skeleton, RGB, Depth, Hand
   - Image Encoding Level: 85

3. **Network Tab**:
   - Server IP: ServerApplication address
   - Application Name: Nuitrack_Orbbec

4. Click **"Start"**

**Result**:
- Body skeleton tracking with Orbbec Astra
- Hand tracking and gestures
- RGB and depth streams
- Lower cost than Azure Kinect

### Workflow 4: Kinect v2 Audio Analysis

**Scenario**: Capture audio with Kinect v2 microphone array

**Configuration**:
1. **Video Sources Tab**:
   - Video Source: Kinect
   - Enable: Audio, RGB
   - Disable: Skeleton, Depth (not needed)
   - Image Encoding Level: 90

2. **Local Recording Tab**:
   - Session Name: AudioCapture_2024-01-15
   - Dataset: D:\AudioData

3. Click **"Start"**

**Result**:
- 4-microphone array audio capture
- Beam-forming audio
- Optional RGB for video context
- Suitable for meeting recordings

### Workflow 5: Standard USB Camera

**Scenario**: Simple webcam capture for video annotation

**Configuration**:
1. **Video Sources Tab**:
   - Video Source: Camera
   - Sensor type: USB Camera
   - Capture Format: MJPEG 1920x1080 @ 30fps
   - Image Encoding Level: 85

2. **Network Tab**:
   - Configure RendezVous connection

3. Click **"Start"**

**Result**:
- Standard webcam capture
- Works with any USB camera
- Network streaming to ServerApplication

## Integration with SAAC

### ServerApplication Configuration

When CameraRemoteApp connects to ServerApplication:

```
ServerApplication automatically discovers:
├─ Process: CameraApp1
├─ Streams (Azure Kinect example):
│   ├─ Audio
│   ├─ Skeleton
│   ├─ RGB
│   ├─ Infrared
│   ├─ Depth
│   ├─ DepthCalibration
│   └─ IMU
└─ Endpoints with port assignments
```

**In ServerApplication Configuration Tab**:
```
Store Mode: Process
Dataset Path: D:\Experiments
Dataset Name: MultimodalCapture

On "Start Session":
  → Creates stores for all discovered streams
  → D:\Experiments\MultimodalCapture\Session_001\CameraApp1\
     ├─ Audio
     ├─ Skeleton
     ├─ RGB
     ├─ Depth
     └─ IMU
```

### PsiStudio Visualization

After recording:

1. Open PsiStudio
2. File → Open Dataset
3. Navigate to Dataset Path
4. Select `.pds` file
5. Visualizations available:
   - RGB video timeline
   - Depth map visualization
   - 3D skeleton overlay
   - Audio waveform
   - IMU data plots
   - Synchronized with other modalities

## Troubleshooting

### Sensor Issues

**Azure Kinect Not Detected**:
- Check USB 3.0 connection (must be USB 3.0, not 2.0)
- Verify Azure Kinect SDK installed
- Install latest firmware: `AzureKinectFirmwareTool.exe`
- Check Device Manager for "Azure Kinect 4K Camera"
- Try different USB 3.0 port
- Check USB 3.0 controller power

**Nuitrack Activation Failed**:
- Verify Activation Key is correct
- Check internet connection (online activation)
- Ensure sensor is connected
- Visit Nuitrack website for key issues
- Check Nuitrack service is running
- Review Nuitrack logs

**Kinect v2 Not Working**:
- Verify Kinect for Windows SDK 2.0 installed
- Check USB 3.0 connection + power adapter
- Install Kinect v2 drivers
- Check Device Manager for "Xbox NUI Sensor"
- Try different USB 3.0 controller
- Update firmware via Kinect Configuration Verifier

**Camera Not Detected**:
- Check camera is plugged in
- Verify drivers installed
- Test with Windows Camera app
- Check Device Manager for camera
- Try different USB port
- Update camera drivers

### Stream Issues

**Body Tracking Not Working**:
- **Azure Kinect**: Install Azure Kinect Body Tracking SDK
- Verify CUDA/DirectML installed (GPU tracking)
- Check GPU meets requirements (NVIDIA GTX 1070+)
- Enable CPU mode if no GPU available
- Check Depth stream is enabled

**Depth Stream Black/Empty**:
- Check sensor is not obstructed
- Verify depth is enabled in configuration
- Check lighting (avoid direct sunlight/IR interference)
- Test sensor with vendor's viewer application
- Check minimum/maximum depth range

**Audio Not Captured**:
- Verify Audio checkbox is enabled
- Check Windows audio settings
- Test microphone in Sound Settings
- Update audio drivers
- Check sensor USB connection

**High Frame Rate Drops**:
- Reduce resolution
- Decrease frame rate
- Lower JPEG quality
- Close other applications
- Check USB 3.0 performance
- Use dedicated USB 3.0 controller

### Network Issues

**Connection Failed**:
- Verify ServerApplication is running
- Check Server IP and Port
- Test network: `ping <server-ip>`
- Check firewall rules (port 13330)
- Verify network connectivity
- Try "Start Network Only" to test

**Streams Not Appearing**:
- Check "Activate Streaming" is enabled
- Verify Application Name is unique
- Check Streaming Port Range is available
- Review firewall rules (ports 11411+)
- Check Network tab configuration
- Monitor Log tab for registration errors

**High Network Latency**:
- Use wired connection instead of WiFi
- Check network bandwidth
- Close bandwidth-intensive applications
- Reduce JPEG quality
- Check router/switch performance
- Monitor network with Task Manager

### Recording Issues

**Store Creation Failed**:
- Verify Dataset Directory exists
- Check write permissions
- Ensure sufficient disk space (>50 GB recommended)
- Check path length (<260 characters)
- Avoid special characters in paths
- Try different drive

**Recording Stops Unexpectedly**:
- Check disk space (need continuous free space)
- Monitor disk write speed (SSD strongly recommended)
- Review Log tab for errors
- Check for file system errors
- Verify no disk quota restrictions
- Try different storage location

**Large Store Files**:
- Expected for high-resolution sensors
- Azure Kinect: ~7.5 GB/hour typical
- Reduce JPEG quality if needed
- Disable unused streams
- Use external storage for archival

## Performance Optimization

### CPU/GPU Usage

**For High-Performance Systems**:
- Enable all desired streams
- Use maximum resolution
- Enable body tracking
- Multiple sensors simultaneously

**For Limited Systems**:
- Reduce resolution (720p instead of 1080p)
- Decrease frame rate (15 fps instead of 30 fps)
- Disable unused streams
- Use CPU-only body tracking
- Close other applications

### Memory Management

**Reduce Memory Usage**:
- Disable unused streams
- Lower resolution
- Reduce frame rate
- Close other applications
- Restart application periodically

### Storage Optimization

**Reduce Storage Size**:
- Lower JPEG quality (70-80 instead of 85-95)
- Reduce resolution
- Decrease frame rate
- Disable unused streams
- Regular archival to external storage

### Latency Optimization

**For Real-Time Applications**:
- Use wired network
- Reduce JPEG quality
- Lower resolution if needed
- Close other applications
- Use dedicated NIC for streaming

## Best Practices

### Sensor Configuration

1. **Azure Kinect**:
   - Always enable DepthCalibration for 3D reconstruction
   - Use 30 fps for body tracking
   - Minimum 1080p for RGB (better person detection)
   - Enable IMU for motion analysis

2. **Nuitrack**:
   - Test sensor compatibility before purchase
   - Keep sensors at recommended height (1-2 meters)
   - Avoid IR interference (direct sunlight, other sensors)
   - Update Nuitrack SDK regularly

3. **Kinect v2**:
   - Use dedicated USB 3.0 controller
   - Keep sensor firmware updated
   - Position at optimal height (chest level)
   - Avoid moving sensor during capture

4. **Standard Cameras**:
   - Use highest resolution supported at 30 fps
   - Enable hardware encoding (MJPEG/H.264) if available
   - Test capture formats before recording

### Storage Strategy

1. **Drive Selection**:
   - Use fast NVMe SSD for recording
   - Dedicated drive for sensor data
   - Regular archival to external/network storage

2. **Session Management**:
   - Descriptive session names
   - Keep sessions under 1 hour
   - Document session purposes
   - Regular cleanup of old sessions

3. **Space Planning**:
   - Estimate: ~10 GB/hour per Azure Kinect
   - Plan for 2-3x estimated (safety margin)
   - Monitor disk space continuously
   - Set up low-space alerts

### Network Configuration

1. **Naming Convention**:
   - Descriptive Application Names
   - Include sensor type and location
   - Avoid spaces (use underscores)
   - Keep unique per instance

2. **Port Management**:
   - Document port assignments
   - Reserve sufficient port range
   - Avoid conflicts with other services
   - Configure firewall beforehand

3. **Testing**:
   - Test connectivity before sessions
   - Verify streams in ServerApplication
   - Check bandwidth with all streams
   - Run test captures

### Production Deployment

1. **Pre-Session Checklist**:
   - [ ] Sensors connected and detected
   - [ ] Firmware/drivers up to date
   - [ ] Network connectivity tested
   - [ ] Storage location has space
   - [ ] Configuration saved
   - [ ] Test capture completed
   - [ ] Lighting conditions acceptable

2. **During Session**:
   - Monitor Log tab for errors
   - Check sensor indicators
   - Watch disk space
   - Note any anomalies

3. **Post-Session**:
   - Verify data recorded
   - Check data integrity
   - Archive/backup data
   - Document any issues