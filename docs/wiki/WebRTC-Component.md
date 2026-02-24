# WebRTC Component

## Overview

**WebRTC** provides WebRTC protocol support for streaming audio and video from Unity and Unreal Engine to \\psi pipelines. Built on top of SipSorcery library, it enables real-time multimedia communication with game engines and web browsers.

**Key Features**:
- ? WebRTC video streaming (VP8, H.264)
- ? WebRTC audio streaming (Opus codec)
- ? WebRTC Data Channels for bidirectional communication
- ? Unity integration with custom package
- ? Unreal Engine PixelStreaming support
- ? WebSocket signaling for peer connection
- ? Multiple peer support

## Architecture

```
???????????????????????????????????????????????????????????
?                WebRTC Component                         ?
???????????????????????????????????????????????????????????
?                                                         ?
?  ????????????????????????????????????????????????????  ?
?  ?     WebRTCVideoStream                            ?  ?
?  ?  • Video capture (VP8/H.264)                     ?  ?
?  ?  • Audio capture (Opus)                          ?  ?
?  ?  • Data channel management                       ?  ?
?  ????????????????????????????????????????????????????  ?
?                   ?                                    ?
?  ????????????????????????????????????????????????????  ?
?  ?     WebRTCConnector (Base)                       ?  ?
?  ?  • WebSocket signaling                           ?  ?
?  ?  • Peer connection lifecycle                     ?  ?
?  ?  • ICE/STUN/TURN handling                        ?  ?
?  ????????????????????????????????????????????????????  ?
?                                                         ?
?         Outputs                                        ?
?         ?? OutImage (Shared<Image>)                    ?
?         ?? OutAudio (AudioBuffer)                      ?
?         ?? Data Channels (custom types)                ?
?                                                         ?
???????????????????????????????????????????????????????????
        ?
        ? WebRTC (SRTP/DTLS)
        ?
???????????????????????????????????????
?      Unity / Unreal Engine          ?
?  • WebRTC peer                      ?
?  • Camera capture                   ?
?  • Microphone capture               ?
?  • Data channel send/receive        ?
???????????????????????????????????????
```

## Components

### 1. WebRTCVideoStream

Main component for video and audio streaming.

**Outputs**:
```csharp
public IProducer<Shared<Image>> OutImage { get; }
public IProducer<AudioBuffer> OutAudio { get; }
```

**Configuration**:
```csharp
public class WebRTCVideoStreamConfiguration : WebRTCDataConnectorConfiguration
{
    // Network
    public IPAddress WebsocketAddress { get; set; }
    public int WebsocketPort { get; set; }
    
    // Streaming
    public bool AudioStreaming { get; set; } = true;
    public bool PixelStreamingConnection { get; set; } = false;
    
    // Encoding
    public string FFMPEGFullPath { get; set; }
    public LogLevel Log { get; set; } = LogLevel.Warning;
    
    // Data Channels
    public Dictionary<string, IConsumer> OutputChannels { get; set; }
    public Dictionary<string, IProducer> InputChannels { get; set; }
}
```

### 2. WebRTCDataConnector

Base class for WebRTC with data channel support.

**Features**:
- Manage data channels
- Convert \\psi streams to data channels
- Convert data channels to \\psi streams

**Data Channel Components**:
```csharp
// Unity/Unreal ? \\psi
public class WebRTCDataChannelToEmitter<T> : IProducer<T>
{
    public IProducer<T> Out { get; }
}

// \\psi ? Unity/Unreal
public class WebRTCDataReceiverToChannel
{
    public static IConsumer<T> Create<T>(Pipeline pipeline, string channelName);
}
```

### 3. WebRTCConnector (Base)

Low-level WebRTC connection management.

**Responsibilities**:
- WebSocket signaling protocol
- SDP offer/answer exchange
- ICE candidate exchange
- Peer connection establishment
- DTLS/SRTP security

### 4. OpusAudioEncoder

Custom Opus audio encoder for Unity float audio.

**Features**:
- Float audio encoding (Unity format)
- Standard PCM encoding
- Configurable bit rate
- Low latency encoding

**Modified from**: [SIPSorceryMedia.SDL2](https://github.com/sipsorcery-org/SIPSorceryMedia.SDL2)

## Basic Usage

### Example 1: Unity Video and Audio Streaming

```csharp
using Microsoft.Psi;
using WebRTC;

var pipeline = Pipeline.Create();

// Configure WebRTC
var config = new WebRTCVideoStreamConfiguration
{
    WebsocketAddress = IPAddress.Parse("127.0.0.1"),
    WebsocketPort = 8080,
    AudioStreaming = true,
    PixelStreamingConnection = false,
    FFMPEGFullPath = @"D:\ffmpeg\bin\",
    Log = LogLevel.Information
};

// Create WebRTC stream
var webrtc = new WebRTCVideoStream(pipeline, config);

// Store video and audio
var store = PsiStore.Create(pipeline, "UnityCapture", @"D:\Recordings");
store.Write(webrtc.OutImage.EncodeJpeg(90), "Video");
store.Write(webrtc.OutAudio, "Audio");

// Run pipeline
pipeline.RunAsync();

Console.WriteLine("Waiting for Unity connection on ws://127.0.0.1:8080");
Console.WriteLine("Press Enter to stop...");
Console.ReadLine();

pipeline.Dispose();
```

### Example 2: Unity with Data Channels

```csharp
using Microsoft.Psi;
using WebRTC;
using System;

var pipeline = Pipeline.Create();

var config = new WebRTCVideoStreamConfiguration
{
    WebsocketAddress = IPAddress.Loopback,
    WebsocketPort = 8080,
    AudioStreaming = false,
    FFMPEGFullPath = @"D:\ffmpeg\bin\"
};

// Create data channel receivers
// Unity ? \\psi: Receive events from Unity
var eventsFromUnity = new WebRTCDataChannelToEmitter<string>(pipeline);
config.OutputChannels.Add("Events", eventsFromUnity);

// \\psi ? Unity: Send timing data to Unity
var timingToUnity = WebRTCDataReceiverToChannelFactory.Create<TimeSpan>(
    pipeline,
    "Timing"
);
config.InputChannels.Add("Timing", timingToUnity);

// Create WebRTC stream
var webrtc = new WebRTCVideoStream(pipeline, config);

// Process events from Unity
eventsFromUnity.Out.Do((msg, env) =>
{
    Console.WriteLine($"Unity event: {msg} at {env.OriginatingTime}");
});

// Send periodic timing to Unity
var timer = Timers.Timer(pipeline, TimeSpan.FromSeconds(1));
timer.Out.Select((_, env) => env.OriginatingTime - pipeline.StartTime)
    .PipeTo(timingToUnity);

// Store everything
var store = PsiStore.Create(pipeline, "UnityData", @"D:\Recordings");
store.Write(webrtc.OutImage.EncodeJpeg(), "Video");
store.Write(eventsFromUnity.Out, "UnityEvents");

pipeline.RunAsync();
Console.ReadLine();
pipeline.Dispose();
```

### Example 3: Unreal Engine PixelStreaming

```csharp
using Microsoft.Psi;
using WebRTC;

var pipeline = Pipeline.Create();

var config = new WebRTCVideoStreamConfiguration
{
    WebsocketAddress = IPAddress.Parse("127.0.0.1"),
    WebsocketPort = 8888,
    AudioStreaming = false,  // Audio crashes with Unreal
    PixelStreamingConnection = true,  // Enable PixelStreaming mode
    FFMPEGFullPath = @"C:\ffmpeg\bin\",
    Log = LogLevel.Information
};

var webrtc = new WebRTCVideoStream(pipeline, config);

// Process H.264 video from Unreal
webrtc.OutImage.Do((image, env) =>
{
    Console.WriteLine($"Frame received: {image.Resource.Width}x{image.Resource.Height}");
});

// Store video
var store = PsiStore.Create(pipeline, "UnrealCapture", @"D:\Recordings");
store.Write(webrtc.OutImage.EncodeJpeg(85), "Video");

pipeline.RunAsync();

Console.WriteLine("Waiting for Unreal PixelStreaming connection...");
Console.ReadLine();

pipeline.Dispose();
```

## Unity Integration

### Unity Package

A Unity package is provided in the `assets` folder containing:
- WebRTC scripts
- Native plugin DLLs
- Example scenes
- Configuration scripts

**Package Contents**:
```
Assets/WebRTC/
??? Scripts/
?   ??? WebRTCController.cs
?   ??? CameraStreamer.cs
?   ??? DataChannelManager.cs
??? Plugins/
?   ??? WebRTC.dll
?   ??? opus.dll
??? Scenes/
    ??? WebRTCExample.unity
```

### Unity Setup

1. **Import Package**:
   - Import `WebRTC.unitypackage` into Unity project
   - Ensure plugins are in correct architecture (x64)

2. **Configure WebRTC**:
   ```csharp
   using WebRTC;
   
   public class WebRTCManager : MonoBehaviour
   {
       private WebRTCConnection connection;
       
       void Start()
       {
           connection = new WebRTCConnection();
           connection.Connect("ws://127.0.0.1:8080");
           
           // Stream camera
           var camera = GetComponent<Camera>();
           connection.AddVideoTrack(camera);
           
           // Stream audio
           var audio = GetComponent<AudioListener>();
           connection.AddAudioTrack(audio);
       }
   }
   ```

3. **Data Channels in Unity**:
   ```csharp
   // Send data to \\psi
   connection.SendDataChannel("Events", "ButtonClicked");
   
   // Receive data from \\psi
   connection.OnDataChannel("Timing", (data) =>
   {
       var timing = JsonUtility.FromJson<TimeData>(data);
       Debug.Log($"Time: {timing.elapsed}");
   });
   ```

### Unity Best Practices

1. **Video Quality**:
   - Use 1280x720 or 1920x1080 resolution
   - 30 FPS recommended for balance
   - VP8 encoding for Unity

2. **Audio**:
   - 48 kHz sample rate
   - Mono or stereo
   - Opus encoding

3. **Performance**:
   - Disable audio if not needed
   - Reduce resolution if CPU-limited
   - Use async operations

## Unreal Engine Integration

### PixelStreaming Support

WebRTC component works with Unreal's PixelStreaming plugin.

**Unreal Configuration**:
1. Enable PixelStreaming plugin
2. Configure signaling server
3. Set resolution and frame rate
4. Launch with PixelStreaming arguments

**Command Line**:
```bash
UnrealEditor.exe MyProject.uproject -game -PixelStreamingURL=ws://127.0.0.1:8888 -RenderOffScreen
```

### Unreal Best Practices

1. **Video Encoding**:
   - H.264 hardware encoding
   - 1920x1080 @ 60fps capable
   - Adjust based on GPU

2. **Known Issues**:
   - Audio streaming may cause crashes (disable in config)
   - Data channels not yet supported (use HTTP alternative)

3. **Recommended Settings**:
   ```csharp
   var config = new WebRTCVideoStreamConfiguration
   {
       PixelStreamingConnection = true,
       AudioStreaming = false,  // Disable to prevent crashes
       // ... other settings
   };
   ```

## Advanced Features

### Custom Data Channel Types

Create type-safe data channels:

```csharp
// Define custom data type
public class PlayerAction
{
    public string Action { get; set; }
    public Vector3 Position { get; set; }
    public DateTime Timestamp { get; set; }
}

// Unity ? \\psi: Receive player actions
var actionsChannel = new WebRTCDataChannelToEmitter<PlayerAction>(pipeline);
config.OutputChannels.Add("PlayerActions", actionsChannel);

// Process actions
actionsChannel.Out.Do((action, env) =>
{
    Console.WriteLine($"{action.Action} at {action.Position}");
});
```

### Multiple Peer Connections

Support multiple Unity/Unreal instances:

```csharp
var webrtc1 = new WebRTCVideoStream(pipeline, new WebRTCVideoStreamConfiguration
{
    WebsocketAddress = IPAddress.Loopback,
    WebsocketPort = 8080  // First peer
});

var webrtc2 = new WebRTCVideoStream(pipeline, new WebRTCVideoStreamConfiguration
{
    WebsocketAddress = IPAddress.Loopback,
    WebsocketPort = 8081  // Second peer
});

// Store both streams
store.Write(webrtc1.OutImage, "Unity1_Video");
store.Write(webrtc2.OutImage, "Unity2_Video");
```

### Integration with RendezVous

Stream Unity data to distributed pipeline:

```csharp
using SAAC.PipelineServices;

var rdvConfig = new RendezVousPipelineConfiguration
{
    DatasetPath = @"D:\UnityData",
    DatasetName = "VRExperiment.pds"
};

var rdvPipeline = new RendezVousPipeline(rdvConfig, "UnityServer");

// Create WebRTC in RendezVous pipeline
var webrtcConfig = new WebRTCVideoStreamConfiguration
{
    WebsocketAddress = IPAddress.Loopback,
    WebsocketPort = 8080
};

var webrtc = new WebRTCVideoStream(rdvPipeline.Pipeline, webrtcConfig);

// Automatically stored by RendezVous
rdvPipeline.Start();
```

## Configuration Reference

### WebRTCVideoStreamConfiguration

```csharp
public class WebRTCVideoStreamConfiguration
{
    // Network Settings
    public IPAddress WebsocketAddress { get; set; }     // WebSocket server address
    public int WebsocketPort { get; set; }              // WebSocket server port
    
    // Streaming Options
    public bool AudioStreaming { get; set; } = true;    // Enable audio capture
    public bool PixelStreamingConnection { get; set; }  // Unreal PixelStreaming mode
    
    // Encoding
    public string FFMPEGFullPath { get; set; }          // Path to ffmpeg binaries
    public LogLevel Log { get; set; }                   // Logging level
    
    // Data Channels
    public Dictionary<string, IConsumer> OutputChannels { get; set; }   // Unity ? \\psi
    public Dictionary<string, IProducer> InputChannels { get; set; }    // \\psi ? Unity
}
```

### FFmpeg Configuration

**Required FFmpeg Components**:
- `ffmpeg.exe`
- `avcodec-*.dll`
- `avformat-*.dll`
- `avutil-*.dll`
- `swscale-*.dll`

**Download**: https://ffmpeg.org/download.html

**Recommended Version**: 4.4 or later

## Troubleshooting

### Video Not Appearing

**Symptoms**:
- No frames received
- OutImage stream empty

**Solutions**:
- Verify Unity/Unreal is sending video
- Check WebSocket connection established
- Ensure correct port and address
- Verify FFmpeg path is correct
- Check firewall rules

### Audio Issues

**Symptoms**:
- No audio stream
- Distorted audio
- Application crashes

**Solutions**:
- Disable audio for Unreal Engine
- Verify Opus codec is available
- Check sample rate (48 kHz)
- Reduce audio quality if needed

### WebSocket Connection Failed

**Symptoms**:
- Unity can't connect
- "Connection refused" error

**Solutions**:
- Ensure \\psi pipeline is running first
- Check WebSocket address and port
- Test with `ws://localhost:PORT`
- Verify no firewall blocking

### Data Channel Not Working

**Symptoms**:
- Messages not received
- Channel not established

**Solutions**:
- Verify channel names match exactly
- Check data channel added before connection
- Ensure proper serialization format
- Monitor WebRTC logs

### Performance Issues

**Symptoms**:
- Dropped frames
- High CPU usage
- Lag/latency

**Solutions**:
- Reduce resolution (720p instead of 1080p)
- Lower frame rate (30fps instead of 60fps)
- Disable audio if not needed
- Use hardware encoding if available
- Close other applications

## Known Issues

1. **Audio with Unreal**: Audio streaming with Unreal Engine may cause crashes. Workaround: Disable audio streaming.

2. **Random VP8 Glitch**: Occasional video glitches with VP8 encoding (not seen recently). Consider using H.264 if persistent.

3. **Data Channels with Unreal**: Data channels not yet tested with Unreal Engine. Use HTTP alternative (UnrealRemoteConnector).

## Future Enhancements

- ? Fix audio streaming issues
- ? Bring H.264 to Unity
- ? Data channel support for Unreal Engine
- ? STUN/TURN server support
- ? Support for additional game engines
- ? Improved error handling and reconnection

## See Also

- [Components Overview](Components-Overview.md) - All SAAC components
- [Unity Component](Unity-Component.md) - Unity-specific integration
- [UnrealRemoteConnector Component](UnrealRemoteConnector-Component.md) - Unreal HTTP integration
- [InteropExtension Component](InteropExtension-Component.md) - WebSocket implementation
- [Architecture Overview](Architecture.md) - SAAC framework architecture
