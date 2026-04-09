# Examples

## Overview

**TestingConsole** is a console application that serves as a testing ground and example repository for various SAAC components and integration scenarios. It contains numerous commented examples demonstrating different features and use cases.

**Location**: `Applications/TestingConsole/Program.cs`

## Running Examples

### Basic Steps

1. Open `Applications/TestingConsole/Program.cs`
2. Find the example you want to run
3. Uncomment the relevant code
4. Install required dependencies (if any)
5. Build and run

## Available Examples

### 1. OpenFace Integration

**Purpose**: Face detection, landmark extraction, and face blurring

**Requirements**:
- OpenFace dependencies in `Dependencies/OpenFace`
- Kinect Azure or webcam

**Example**:

```csharp
static void OpenFace()
{
	Pipeline p = Pipeline.Create();

    // Configure Kinect Azure for video input
    AzureKinectSensor webcam = new AzureKinectSensor(p);

    // Configure OpenFace
    OpenFaceConfiguration configuration = new OpenFaceConfiguration("./");
    configuration.Face = true;
    configuration.Eyes = true;
    configuration.Pose = true;
    OpenFace.OpenFace facer = new OpenFace.OpenFace(p, configuration);
    
    // Connect camera to OpenFace
    webcam.ColorImage.PipeTo(facer.In);

    // Face blurring example
    FaceBlurrer faceBlurrer = new FaceBlurrer(p, "Blurrer");
    facer.OutBoundingBoxes.PipeTo(faceBlurrer.InBBoxes);
    webcam.ColorImage.PipeTo(faceBlurrer.InImage);

    // Store results
    var store = PsiStore.Create(p, "FaceTracking", "D:\\Stores");
    store.Write(facer.OutBoundingBoxes, "FaceBoundingBoxes");
    store.Write(facer.OutLandmarks, "FaceLandmarks");
    store.Write(faceBlurrer.Out, "BlurredVideo");
    
    p.Run();
}
```

**To Run**:
1. Connect Kinect Azure or webcam
2. Run application
3. Data stored in `D:\Stores\FaceTracking`

**Output**:
- Face bounding boxes
- Facial landmarks 
- Blurred video (faces obscured)

---

### 2. WebRTC Streaming

**Purpose**: Stream video and audio via WebRTC to web browsers

**Requirements**:
- ffmpeg binaries in specified path
- WebRTC browser client

#### Simple Video/Audio Streaming

```csharp
static void WebRTCVideoAudio()
{
	Pipeline p = Pipeline.Create();
	
    WebRTCVideoStreamConfiguration config = new WebRTCVideoStreamConfiguration();
    config.WebsocketAddress = System.Net.IPAddress.Loopback;
    config.WebsocketPort = 80;
    config.AudioStreaming = true;
    config.FFMPEGFullPath = "D:\\ffmpeg\\bin";
    config.Log = Microsoft.Extensions.Logging.LogLevel.Information;
    
    WebRTCVideoStream stream = new WebRTCVideoStream(p, config);
    
    // Store streams
    var store = PsiStore.Create(p, "WebRTC", "D:\\Stores");
    store.Write(stream.OutImage.EncodeJpeg(), "Image");
    store.Write(stream.OutAudio, "Audio");
    
    p.Run();
}
```

#### Full WebRTC with Data Channels

```csharp
static void FullWebRTC()
{
	Pipeline p = Pipeline.Create();
	
    WebRTCVideoStreamConfiguration config = new WebRTCVideoStreamConfiguration();
    config.WebsocketAddress = System.Net.IPAddress.Parse("127.0.0.1");
    config.WebsocketPort = 80;
    config.AudioStreaming = true;
    config.FFMPEGFullPath = "D:\\ffmpeg\\bin\\";
    
    // Data channels for bidirectional communication
    var emitter = new WebRTCDataChannelToEmitter<string>(p);
    var incoming = WebRTCDataReceiverToChannelFactory.Create<TimeSpan>(p, "timing");
    
    config.OutputChannels.Add("Events", emitter);
    config.InputChannels.Add("Timing", incoming);

    WebRTCVideoStream stream = new WebRTCVideoStream(p, config);
    
    // Store everything
    var store = PsiStore.Create(p, "WebRTCFull", "D:\\Stores");
    store.Write(stream.OutImage.EncodeJpeg(), "Image");
    store.Write(stream.OutAudio, "Audio");
    store.Write(emitter.Out, "Events");

    // Send timer data through WebRTC
    var timer = Timers.Timer(p, TimeSpan.FromSeconds(1));
    timer.Out.PipeTo(incoming.In);
    
    p.Run();
}
```

**Browser Client**:
```html
<!DOCTYPE html>
<html>
<body>
    <video id="video" autoplay></video>
    <script>
        const pc = new RTCPeerConnection();
        const ws = new WebSocket('ws://localhost:80');
        
        // Handle WebRTC connection...
        pc.ontrack = (event) => {
            document.getElementById('video').srcObject = event.streams[0];
        };
    </script>
</body>
</html>
```

---

### 4. Body Tracking with Analysis

**Purpose**: Kinect Azure body tracking with gesture and posture detection

**Example**:

```csharp
static void testBodies()
{
	Pipeline p = Pipeline.Create();
	
    // Configure Kinect Azure with body tracking
    AzureKinectSensorConfiguration configKinect = new AzureKinectSensorConfiguration();
    configKinect.DeviceIndex = 0;
    configKinect.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
    configKinect.BodyTrackerConfiguration.CpuOnlyMode = false; // Use GPU
    
    AzureKinectSensor sensor = new AzureKinectSensor(p, configKinect);

    // Convert Kinect bodies to SAAC format
    Bodies.BodiesConverter bodiesConverter = new Bodies.BodiesConverter(p);
    sensor.Bodies.PipeTo(bodiesConverter.InBodiesAzure);

    // Hands proximity detection
    Bodies.HandsProximityDetectorConfiguration configHands = 
        new Bodies.HandsProximityDetectorConfiguration();
    configHands.IsPairToCheckGiven = false; // Auto-detect pairs
    Bodies.HandsProximityDetector detector = 
        new Bodies.HandsProximityDetector(p, configHands);
    
    // Body postures detection
    Bodies.BodyPosturesDetectorConfiguration configPostures = 
        new Bodies.BodyPosturesDetectorConfiguration();
    Bodies.BodyPosturesDetector postures = 
        new Bodies.BodyPosturesDetector(p, configPostures);

    // Connect components
    bodiesConverter.Out.PipeTo(detector.In);
    bodiesConverter.Out.PipeTo(postures.In);

    // Output results
    detector.Out.Do((m, e) => {
        foreach (var data in m)
        {
            foreach(var proximity in data.Value)
                Console.WriteLine($"Body {data.Key} - Hands: {proximity}");
        }
    });

    postures.Out.Do((m, e) => {
        foreach (var data in m)
        {
            foreach (var posture in data.Value)
                Console.WriteLine($"Body {data.Key} - Posture: {posture}");
        }
    });
    
    // Store data
    var store = PsiStore.Create(p, "BodyTracking", "D:\\Stores");
    store.Write(bodiesConverter.Out, "Bodies");
    store.Write(detector.Out, "HandProximity");
    store.Write(postures.Out, "Postures");
    
    p.Run();
}
```

**Output**:
```
Body 1 - Hands: HandsClose (distance: 0.15m)
Body 1 - Posture: ArmsRaised
Body 2 - Hands: HandsApart (distance: 1.2m)
Body 2 - Posture: Standing
```

---

### 5. Ollama AI Integration

**Purpose**: Large language model integration for conversational AI

**Requirements**:
- Ollama installed and running
- Model downloaded (e.g., gemma3:1b)

**Example**:

```csharp
static void testOllama()
{
    Pipeline p = Pipeline.Create();
    
    // Configure Ollama
    OllamaConectorConfiguration config = new OllamaConectorConfiguration();
    config.OllamaAddress = new Uri("http://localhost:11434");
    config.Model = "gemma3:1b";  // Or llama2, mistral, etc.
    config.MaxTokens = 500;
    config.Temperature = 0.7;
    
    OllamaConnector ollama = new OllamaConnector(p, config, true);
    
    // Keyboard input
    KeyboardReader.KeyboardReader reader = new KeyboardReader.KeyboardReader(p);
    reader.Out.PipeTo(ollama.In);
    
    // Display responses
    ollama.Out.Do((response, env) => {
        var latency = (env.CreationTime - env.OriginatingTime).TotalSeconds;
        Console.WriteLine($"[{latency:F2}s latency]\n{response}\n>");
    });
    
    Console.WriteLine("Ollama Chat - Type your messages:");
    p.Run();
}
```

**Usage**:
```
Ollama Chat - Type your messages:
> Hello, how are you?
[1.23s latency]
I'm doing well, thank you for asking! How can I help you today?

> Explain SAAC framework
[2.45s latency]
SAAC (System As A Collaborator) is a framework built on Microsoft Psi for
multimodal data capture and processing in collaborative environments...
>
```

---

### 6. Lab Streaming Layer (LSL)

**Purpose**: Integration with LSL for physiological sensors


**Requirements**:
- LSL library (liblsl)
- LSL-compatible sensors

**Example**:

```csharp
static void TestLSL()
{
    Pipeline p = Pipeline.Create();
    
    // Configure LSL manager
    LabStreamLayerManager manager = new LabStreamLayerManager(
        p,
        log => Console.WriteLine(log),
        samplingRateHz: 500,
        bufferSizeMs: 100
    );
    
    // Start discovering LSL streams
    manager.Start();
    
    // Wait for streams to be discovered
    Thread.Sleep(2000);
    
    // Access discovered streams
    foreach (var stream in manager.LabStreamComponents)
    {
        Console.WriteLine($"Found LSL stream: {stream.Key}");
        
        // For float data streams
        if (stream.Value is LabStreamLayerComponent<float> floatStream)
        {
            floatStream.Out.Do((data, env) => {
                Console.WriteLine($"LSL Data @ {env.OriginatingTime}: " +
                                  string.Join(", ", data));
            });
        }
    }
    
    // Store LSL data
    var store = PsiStore.Create(p, "LSL_Data", "D:\\Stores");
    foreach (var stream in manager.LabStreamComponents)
    {
        store.Write(stream.Value.Out, stream.Key);
    }
    
    p.RunAsync();
    Console.ReadLine();
}
```

**Creating LSL Outlet** (separate application or sensor):
```csharp
// Create LSL outlet for EEG data
StreamInfo info = new StreamInfo(
    "MyEEG",           // Stream name
    "EEG",             // Stream type
    8,                 // Channel count
    250,               // Sampling rate
    channel_format_t.cf_float32,
    "unique_id_123"
);

StreamOutlet outlet = new StreamOutlet(info);

// Send data
float[] data = new float[8];
while (running)
{
    // Fill data array...
    outlet.push_sample(data);
    Thread.Sleep(4); // 250 Hz = 4ms per sample
}
```

---


### 8. PLUME File Integration

**Purpose**: Parse and import PLUME recording files

**Example**:

```csharp
static void PlumeSample()
{
    DatasetPipelineConfiguration config = new DatasetPipelineConfiguration();
    config.AutomaticPipelineRun = false;
    config.Diagnostics = DatasetPipeline.DiagnosticsMode.Off;
    config.DatasetPath = @"E:\SAAC\Stores\";
    config.DatasetName = "SAAC.pds";

    // Create PLUME dataset pipeline
    PlumeDatasetPipeline pipeline = new PlumeDatasetPipeline(
        config,
        "exampleParser"
    );
    
    // Load PLUME file with stream type mappings
    var streamTypes = new Dictionary<string, Type>
    {
        { "Main Camera", typeof(CoordinateSystem) },
        { "PlayerPosition", typeof(Vector3) },
        { "PlayerRotation", typeof(Quaternion) }
    };
    
    pipeline.LoadPlumeFile(
        @"E:\SAAC\record.plm",
        streamTypes
    );
    
    // Data is now available in the dataset
    pipeline.Dispose();
    
    // Can be opened in PsiStudio
    Console.WriteLine("PLUME data imported to SAAC.pds");
}
```

---

### 9. RendezVous Pipeline Example

**Purpose**: Full RendezVous pipeline with topic transformers

**Example**:

```csharp
static void Main(string[] args)
{
    RendezVousPipelineConfiguration configuration = 
        new RendezVousPipelineConfiguration();
    
    configuration.AutomaticPipelineRun = true;
    configuration.Debug = false;
    configuration.DatasetPath = @"D:\Stores\RendezVousPipeline\";
    configuration.DatasetName = "RendezVousPipeline.pds";
    configuration.RendezVousHost = "localhost";
    configuration.Diagnostics = DatasetPipeline.DiagnosticsMode.Off;
    configuration.StoreMode = DatasetPipeline.StoreMode.Process;
    configuration.CommandDelegate = CommandReceived;

    // Register custom topic transformers
    configuration.AddTopicFormatAndTransformer(
        "Left",
        typeof(Matrix4x4),
        new PsiFormatMatrix4x4(),
        typeof(MatrixToCoordinateSystem)
    );
    
    configuration.AddTopicFormatAndTransformer(
        "Right",
        typeof(Matrix4x4),
        new PsiFormatMatrix4x4(),
        typeof(MatrixToCoordinateSystem)
    );
    
    configuration.AddTopicFormatAndTransformer(
        "Hand-Left",
        typeof(Hand),
        new PsiFormatHand()
    );
    
    configuration.AddTopicFormatAndTransformer(
        "EyeTracking",
        typeof(Tuple<Vector3, Vector3>),
        new PsiFormatTupleOfVector(),
        typeof(TupleOfVectorToRay)
    );

    // Create and start pipeline
    RendezVousPipeline rdvPipeline = new RendezVousPipeline(
        configuration,
        "Server"
    );
    
    rdvPipeline.Start();
    
    // Keep running
    Console.WriteLine("Press Enter to stop...");
    Console.ReadLine();
    rdvPipeline.Dispose();
}

static void CommandReceived(string source, Message<(Command, string)> message)
{
    Console.WriteLine($"Command from {source}: {message.Data.Item1} - {message.Data.Item2}");
}
```


## Debugging Examples

### Enable Detailed Logging in DatasetPipeline

```csharp
configuration.Debug = true;
configuration.Diagnostics = DatasetPipeline.DiagnosticsMode.Store;
```

### Monitor Pipeline Events

```csharp
p.PipelineRun += (s, e) => Console.WriteLine("Pipeline started");
p.PipelineCompleted += (s, e) => Console.WriteLine("Pipeline completed");
p.ComponentError += (s, e) => Console.WriteLine($"Error: {e.Exception}");
```