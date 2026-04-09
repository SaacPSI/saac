# Quick Start Guide

This guide will help you get started with SAAC by walking through a simple example: capturing and streaming screen content using VideoRemoteApp.

## Prerequisites

Before starting, ensure you have completed the [Installation Guide](Installation.md).

## Your First SAAC Application

We'll set up a simple scenario:
1. **ServerApplication** as the coordination hub
2. **VideoRemoteApp** to capture and stream screen content

### Step 1: Start the RendezVous Server

1. Navigate to the ServerApplication output directory:
   ```
   cd Applications\ServerApplication\bin\Debug
   ```

2. Run ServerApplication:
   ```
   ServerApplication.exe
   ```

3. In the ServerApplication UI:
   - **Server IP**: Select your network adapter IP (e.g., `192.168.1.100`)
   - **RendezVous Port**: Leave as `13330` (default)
   - **Command Port**: Leave as `13331` (default)
   - Click **"Start Server"**

You should see: `State: Server Started`

### Step 2: Start VideoRemoteApp

1. Open a new terminal and navigate to VideoRemoteApp:
   ```
   cd Applications\VideoRemoteApp\bin\Debug
   ```

2. Run VideoRemoteApp:
   ```
   VideoRemoteApp.exe
   ```

3. Configure the **General Tab**:
   - ? **Is Remote Server** (checked)
   - ? **Is Streaming** (checked)
   - ? **Is Local Recording** (unchecked)

4. Configure the **Video Tab**:
   - **Capture Interval**: `100` (capture every 100ms)
   - **Encoding Level**: `85` (JPEG quality)
   - Click **"Add Cropping Area"**
   - A new area "CropArea_1" appears
   - Click **"Select Cropping Area"**
   - A transparent overlay appears on your screen
   - Click and drag to select a region (e.g., your web browser window)
   - Release to confirm

5. Configure the **Network Tab**:
   - **Server IP**: Select the same IP as ServerApplication
   - **RendezVous Server IP**: Enter ServerApplication's IP (e.g., `192.168.1.100`)
   - **RendezVous Port**: `13330`
   - **Application Name**: `VideoCapture1`
   - **Command Source**: `Server`
   - **Command Port**: `13331`
   - **Streaming Port Range Start**: `50000`

6. Click **"Save Configuration"**

7. Click **"Start Network"**
   - State changes to: `Waiting for server`
   - After a moment: `Connected to server`

8. Click **"Start All"**
   - State changes to: `Video initialised` ? `Started`

### Step 3: Verify Connection

Back in ServerApplication:
1. Click the **"Processes"** tab
2. You should see `VideoCapture1` listed
3. Expand it to see available endpoints
4. You'll see stream information including port numbers

### Step 4: View the Stream (Optional)

To consume the stream, you can:

**Option A: Use PsiStudio**
1. Open PsiStudio (from your Psi build)
2. File ? Connect to Store
3. Enter the RemoteImporter details
4. Visualize the stream

**Option B: Write a simple consumer**
```csharp
using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Imaging;

using var pipeline = Pipeline.Create("Consumer");

// Connect to VideoRemoteApp
var importer = new RemoteImporter(pipeline, "192.168.1.100", 50000);

// Open the video stream
var videoStream = importer.Importer.OpenStream<Shared<EncodedImage>>("CropArea_1");

// Display or process frames
videoStream.Do((frame, env) => 
{
    Console.WriteLine($"Received frame at {env.OriginatingTime}");
});

await pipeline.RunAsync();
```

### Step 5: Stop Everything

1. In VideoRemoteApp, click **"Quit"**
   - Pipeline stops gracefully
   - Unregisters from RendezVous

2. In ServerApplication, click **"Stop Server"**
   - Server shuts down

## Recording Data Locally

Let's modify the setup to record data locally instead of streaming.

### Step 1: Configure Local Recording

1. Start VideoRemoteApp again

2. Configure the **General Tab**:
   - ? **Is Remote Server** (unchecked)
   - ? **Is Streaming** (unchecked)
   - ? **Is Local Recording** (checked)

3. Configure the **Local Recording Tab**:
   - **Session Name**: `MyFirstRecording`
   - **Dataset Path**: Click **"..."** and select a folder (e.g., `C:\Data\SAAC`)
   - **Dataset Name**: `VideoCapture.pds`

4. Configure the **Video Tab**:
   - Set up cropping area as before

5. Click **"Save Configuration"**

6. Click **"Start All"**
   - Capture begins, writing to disk

7. After 10-20 seconds, click **"Quit"**

### Step 2: View Recorded Data

1. Open PsiStudio

2. File ? Open Store

3. Navigate to your dataset:
   ```
   C:\Data\SAAC\VideoCapture.pds
   ```

4. Open session `MyFirstRecording_YYYYMMDD_HHMMSS`

5. You'll see streams:
   - `CropArea_1` (or your named area)

6. Drag the stream to the visualization area

7. Use the timeline to play back captured frames

## Adding Audio (Optional)

Let's add audio capture to the mix using WhisperRemoteApp.

### Step 1: Start ServerApplication

Start ServerApplication as in the first example.

### Step 2: Start VideoRemoteApp

Start VideoRemoteApp in streaming mode as before.

### Step 3: Start WhisperRemoteApp

1. Navigate to WhisperRemoteApp:
   ```
   cd Applications\WhisperRemoteApp\bin\Debug
   ```

2. Run WhisperRemoteApp:
   ```
   WhisperRemoteApp.exe
   ```

3. Configure:
   - **Application Name**: `WhisperCapture1`
   - **RendezVous Server IP**: Same as before
   - Other network settings similar to VideoRemoteApp
   - **Streaming Port**: `52000`

4. Click **"Start Network"** then **"Start All"**

### Step 4: Verify Both Applications

In ServerApplication:
- Check **Processes** tab
- Both `VideoCapture1` and `WhisperCapture1` should be listed
- Each with their respective endpoints

Now you have synchronized video and audio streaming!

## Common Operations

### Sending Remote Commands

From ServerApplication, you can control applications:

1. Select a process (e.g., `VideoCapture1`)
2. Use command buttons:
   - **Run**: Start capture
   - **Stop**: Stop capture
   - **Status**: Query status

Or use the command interface:
```
Command: Run
Target: VideoCapture1
```

### Multiple Capture Instances

Run multiple VideoRemoteApp instances:

1. First instance:
   - Application Name: `VideoCapture_Room1`
   - Streaming Port Start: `50000`

2. Second instance:
   - Application Name: `VideoCapture_Room2`
   - Streaming Port Start: `51000`

Both appear in ServerApplication and can be controlled independently.

### Hybrid Mode (Stream + Record)

Enable both modes in VideoRemoteApp:
- ? **Is Remote Server**
- ? **Is Streaming**
- ? **Is Local Recording**

Data is simultaneously:
- Streamed over network
- Recorded to local disk

## Troubleshooting

### "Cannot connect to RendezVous server"

**Check:**
1. ServerApplication is running
2. IP address matches exactly
3. Port 13330 is not blocked by firewall
4. Network connectivity: `ping <server-ip>`

**Solution:**
```
# Windows Firewall
# Allow port 13330 inbound
netsh advfirewall firewall add rule name="SAAC RendezVous" dir=in action=allow protocol=TCP localport=13330
```

### "No valid cropping area"

**Error when starting VideoRemoteApp**

**Solution:**
1. Go to Video tab
2. Click "Add Cropping Area"
3. Either:
   - Manually set X, Y, Width, Height to valid values
   - Use "Select Cropping Area" to visually select
4. Ensure Width > 0 and Height > 0

### "Process not appearing in ServerApplication"

**Check:**
1. Application successfully started
2. "Connected to server" message appears
3. Network configuration matches
4. Application name is unique

**Debug:**
- Check ServerApplication logs
- Verify network settings in both applications

### Video stream has no data

**Check:**
1. Cropping area is within screen bounds
2. Something is visible in the cropped region
3. Application is in "Started" state
4. Streaming is enabled

## Next Steps

Now that you understand the basics:

1. **Explore More Applications**
   - [CameraRemoteApp](CameraRemoteApp.md) - Camera capture
   - [WhisperRemoteApp](WhisperRemoteApp.md) - Speech recognition

2. **Learn the Architecture**
   - [Architecture Overview](Architecture.md)
   - [RendezVous Protocol](RendezVous-Protocol.md)

3. **Build Custom Components**
   - [Component Development Guide](Component-Development.md)
   - [Pipeline Services](PipelineServices.md)

4. **Advanced Configuration**
   - Multiple machines
   - Performance tuning
   - Custom stream processing

5. **Integration**
   - Unity integration
   - Custom visualization
   - Machine learning pipelines

## Sample Scenarios

### Scenario 1: Meeting Recording

**Setup:**
- VideoRemoteApp: Screen capture (presentation)
- CameraRemoteApp: Webcam (presenter)
- WhisperRemoteApp: Audio + transcription
- All in recording mode

**Result:** Multi-modal recording synchronized to Psi stores

### Scenario 2: Remote Collaboration

**Setup:**
- Multiple VideoRemoteApps: Different rooms
- ServerApplication: Central coordination
- All in streaming mode

**Result:** Distributed capture with central monitoring

### Scenario 3: VR Experiment

**Setup:**
- VideoRemoteApp: Monitor capture
- Body tracking app: Participant movements
- Annotation app: Experimenter notes
- Hybrid mode (stream for monitoring + record for analysis)

**Result:** Complete experiment recording with live monitoring

## Tips and Best Practices

### Performance

- Start with lower frame rates (100-200ms interval)
- Adjust JPEG quality based on network bandwidth
- Use UDP for real-time streaming
- Use TCP for reliable recording

### Organization

- Use descriptive application names: `Video_Lab1_Station2`
- Document your port assignments
- Keep configuration files for repeated setups
- Use consistent dataset naming conventions

### Debugging

- Enable logging in applications
- Check Windows Event Viewer for system errors
- Use Wireshark to inspect network traffic
- Monitor CPU and memory usage

### Workflow

1. Plan your setup (applications, ports, data flow)
2. Start ServerApplication first
3. Start producer applications (capture/sensors)
4. Start consumer applications (processing/visualization)
5. Verify connections in ServerApplication
6. Start capture/streaming
7. Stop in reverse order

## Resources

- [VideoRemoteApp Full Documentation](VideoRemoteApp.md)
- [ServerApplication Guide](ServerApplication.md)
- [Configuration Management](Configuration-Management.md)
- [Troubleshooting Guide](Troubleshooting.md)
- [Microsoft Psi Documentation](https://github.com/microsoft/psi/wiki)

## Getting Help

If you're stuck:

1. Check this wiki
2. Review component README files
3. Check GitHub issues
4. Contact maintainers (see [Home](Home.md))

Happy coding with SAAC! ??
