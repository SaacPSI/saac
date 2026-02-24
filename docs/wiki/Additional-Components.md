# SAAC Components - Additional Documentation

This page provides documentation for additional SAAC components. For more detailed documentation on specific components, see the individual component pages.

## Audio & Speech Components

### AudioRecording Component

**Purpose**: Capture audio from multiple sources (microphones, WAV files, datasets).

**Key Features**:
- Multi-microphone support
- WAV file batch processing
- Dataset replay
- Audio splitting and routing

**Basic Usage**:
```csharp
using SAAC.AudioRecording;

var config = new AudioMicrophonesManagerConfiguration
{
    Microphones = new List<MicrophoneConfig>
    {
        new MicrophoneConfig { DeviceId = 0, Name = "Mic1" },
        new MicrophoneConfig { DeviceId = 1, Name = "Mic2" }
    }
};

var audioManager = new AudioMicrophonesManager(pipeline, config);

// Access individual microphone streams
audioManager.GetMicrophone("Mic1").Do((audio, env) =>
{
    // Process audio from Mic1
});
```

**See Also**: [WhisperRemoteApp](WhisperRemoteApp.md) for audio transcription

---

### Whisper Component

**Purpose**: OpenAI Whisper speech-to-text integration.

**Key Features**:
- Multiple model sizes (tiny to large-v3)
- 100+ language support
- Voice Activity Detection (VAD)
- Real-time and batch processing
- Quantization support (Int8, Q4, Q5)

**Basic Usage**:
```csharp
using SAAC.Whisper;

var config = new WhisperConfiguration
{
    ModelPath = @"C:\Models\ggml-base.bin",
    Language = "en",
    VADEnabled = true
};

var whisper = new WhisperComponent(pipeline, config);

audioStream.PipeTo(whisper.In);

whisper.Out.Do((transcription, env) =>
{
    Console.WriteLine($"[{env.OriginatingTime}] {transcription.Text}");
});
```

**Related**: [WhisperRemoteApp](WhisperRemoteApp.md), [WhisperRemoteServices](WhisperRemoteApp.md#network-streaming)

---

### OpenSmile Component

**Purpose**: Audio feature extraction using OpenSMILE toolkit.

**Key Features**:
- Prosodic features (pitch, intensity, formants)
- Voice quality features
- Spectral features
- Cepstral coefficients (MFCC)
- Real-time or batch processing

**Basic Usage**:
```csharp
using SAAC.OpenSmile;

var config = new OpenSmileConfiguration
{
    ConfigFile = @"C:\opensmile\config\IS09_emotion.conf",
    Features = FeatureSet.Emotion
};

var opensmile = new OpenSmileComponent(pipeline, config);

audioStream.PipeTo(opensmile.In);

opensmile.Out.Do((features, env) =>
{
    Console.WriteLine($"Pitch: {features.Pitch}, Energy: {features.Energy}");
});
```

---

## Sensor & Hardware Components

### Biopac Component

**Purpose**: Interface with Biopac data acquisition hardware for physiological signals.

**Supported Hardware**: MP150, MP160

**Signals**:
- ECG (electrocardiogram)
- GSR (galvanic skin response)
- EEG (electroencephalogram)
- EMG (electromyogram)
- Respiration
- Blood pressure

**Basic Usage**:
```csharp
using SAAC.Biopac;

var config = new BiopacConfiguration
{
    SamplingRate = 1000,  // Hz
    Channels = new List<BiopacChannel>
    {
        new BiopacChannel { Index = 0, Type = SignalType.ECG },
        new BiopacChannel { Index = 1, Type = SignalType.GSR }
    }
};

var biopac = new BiopacSensor(pipeline, config);

biopac.GetChannel(0).Do((ecg, env) =>
{
    // Process ECG data
});
```

**Dependencies**: Biopac MP SDK, Native interop (BiopacInterop.dll)

---

### LabJack Component

**Purpose**: LabJack data acquisition device integration.

**Supported Devices**: U3, U6, T4, T7

**Capabilities**:
- Analog input (AI)
- Digital I/O
- Timers/Counters
- High-speed streaming
- Triggered acquisition

**Basic Usage**:
```csharp
using SAAC.LabJack;

var config = new LabJackConfiguration
{
    DeviceType = LabJackType.U6,
    AnalogInputs = new List<int> { 0, 1, 2, 3 },
    SamplingRate = 10000  // Hz
};

var labjack = new LabJackDevice(pipeline, config);

labjack.AnalogInputs[0].Do((voltage, env) =>
{
    Console.WriteLine($"AI0: {voltage} V");
});
```

**Dependencies**: LabJack UD driver

---

### LabStreamLayer Component

**Purpose**: Lab Streaming Layer (LSL) protocol integration.

**Features**:
- Discover LSL streams on network
- Subscribe to multiple streams
- Timestamp synchronization
- Multi-modal data integration
- Compatible with EEG, eye trackers, etc.

**Basic Usage**:
```csharp
using SAAC.LabStreamLayer;

var manager = new LabStreamLayerManager(
    pipeline,
    (log) => Console.WriteLine(log),
    refreshRate: 500,
    searchTime: 100
);

manager.Start();

// Access specific LSL stream
var eegComponent = manager.LabStreamComponents["EEG_Stream"] as LabStreamLayerComponent<float>;

eegComponent?.Out.Do((data, env) =>
{
    Console.WriteLine($"EEG data: {data.Length} samples");
});
```

**Compatible Devices**: Any LSL-compatible device (BrainVision, Eye trackers, etc.)

---

### Optitrack Component

**Purpose**: Optitrack motion capture system integration.

**Features**:
- Real-time marker tracking
- Rigid body tracking
- Skeleton tracking
- Multiple body support

**Basic Usage**:
```csharp
using SAAC.Optitrack;

var config = new OptitrackConfiguration
{
    ServerAddress = "192.168.1.100",
    LocalAddress = "192.168.1.101"
};

var optitrack = new OptitrackClient(pipeline, config);

optitrack.RigidBodies.Do((bodies, env) =>
{
    foreach (var body in bodies)
    {
        Console.WriteLine($"Body {body.Id}: {body.Position}");
    }
});
```

**Dependencies**: Optitrack Motive software, NatNet SDK

---

### TeslaSuit Component

**Purpose**: TeslaSuit haptic suit integration.

**Features**:
- Full-body haptic feedback
- Motion capture
- Climate control
- EMG sensing

**Basic Usage**:
```csharp
using SAAC.TeslaSuit;

var teslasuit = new TeslaSuitDevice(pipeline);

// Send haptic feedback
teslasuit.SendHaptic(HapticZone.LeftArm, intensity: 0.8, duration: 1.0);

// Receive motion data
teslasuit.Motion.Do((motion, env) =>
{
    Console.WriteLine($"Orientation: {motion.Orientation}");
});
```

**Dependencies**: TeslaSuit SDK

---

## Vision & Tracking Components

### OpenFace Component

**Purpose**: Facial analysis and recognition using OpenFace.

**Features**:
- Face detection
- Facial landmark detection (68 points)
- Head pose estimation
- Eye gaze estimation
- Facial action units (AU)
- Face recognition

**Basic Usage**:
```csharp
using SAAC.OpenFace;

var config = new OpenFaceConfiguration
{
    ModelDirectory = @"C:\OpenFace\models\",
    Face = true,
    Eyes = true,
    Pose = true
};

var openface = new OpenFaceComponent(pipeline, config);

videoStream.PipeTo(openface.In);

openface.OutFaceLandmarks.Do((landmarks, env) =>
{
    Console.WriteLine($"Detected {landmarks.Count} faces");
});
```

**Dependencies**: OpenFace SDK, Native interop (OpenFaceInterop.dll)

---

### Nuitrack Components

**NuitrackRemoteServices**: Server-side Nuitrack SDK integration
**NuitrackRemoteConnector**: Client-side connector

**Purpose**: Nuitrack SDK integration for depth sensors (Orbbec, RealSense, Kinect v1).

**Features**:
- Body skeleton tracking
- Hand tracking
- Gesture recognition
- User segmentation
- Remote streaming via RendezVous

**Basic Usage** (Server):
```csharp
using SAAC.NuitrackRemoteServices;

var config = new NuitrackRemoteStreamsConfiguration
{
    ActivationKey = "XXXX-XXXX-XXXX-XXXX",
    OutputSkeletonTracking = true,
    OutputColor = true,
    OutputDepth = true,
    OutputHandTracking = true
};

var service = new NuitrackRemoteService(pipeline, config, 11411, "192.168.1.100");
```

**Related**: [CameraRemoteApp](CameraRemoteApp.md#nuitrack-configuration)

---

### Kinect Components

**KinectAzureRemoteServices**: Azure Kinect DK server
**KinectRemoteServices**: Kinect v2 server
**KinectAzureRemoteConnector**: Azure Kinect client
**BodiesRemoteServices**: Unified body streaming

**Purpose**: Remote streaming of Kinect sensors via RendezVous.

**Features**:
- Network streaming of RGB, Depth, Skeleton, Audio, IMU
- Configuration via network commands
- Multiple client support
- Synchronized streams

**See**: [CameraRemoteApp](CameraRemoteApp.md) for detailed configuration

---

## Interaction & Analysis Components

### Gestures Component

**Purpose**: Gesture recognition and classification.

**Features**:
- Predefined gesture library
- Custom gesture definition
- Continuous gesture recognition
- Temporal gesture segmentation
- Gesture confidence scores

**Basic Usage**:
```csharp
using SAAC.Gestures;

var gestureRecognizer = new GestureRecognizer(pipeline);

bodyStream.PipeTo(gestureRecognizer.In);

gestureRecognizer.Out.Do((gesture, env) =>
{
    Console.WriteLine($"Detected: {gesture.Type} (confidence: {gesture.Confidence})");
});
```

**Predefined Gestures**:
- Wave
- Point
- Thumbs up/down
- Swipe left/right
- Circle
- Push/Pull

---

### AttentionMeasures Component

**Purpose**: Attention and gaze analysis.

**Features**:
- Gaze direction estimation
- Visual attention heatmaps
- Attention duration tracking
- Multi-person attention analysis
- Object-of-interest detection

**Basic Usage**:
```csharp
using SAAC.AttentionMeasures;

var attentionAnalyzer = new AttentionAnalyzer(pipeline);

gazeStream.PipeTo(attentionAnalyzer.InGaze);
objectsStream.PipeTo(attentionAnalyzer.InObjects);

attentionAnalyzer.Out.Do((attention, env) =>
{
    Console.WriteLine($"Looking at: {attention.TargetObject} for {attention.Duration}s");
});
```

---

### Groups Component

**Purpose**: Group dynamics and flock behavior detection.

**Features**:
- Flock group detection
- Social distance analysis
- Group formation/dissolution
- Spatial patterns
- Interpersonal distance

**Basic Usage**:
```csharp
using SAAC.Groups;

var config = new SimplifiedFlockGroupsDetectorConfiguration
{
    MinGroupSize = 2,
    MaxDistance = 2.0,  // meters
    MinDuration = 5.0   // seconds
};

var groupDetector = new SimplifiedFlockGroupsDetector(pipeline, config);

bodyPositionsStream.PipeTo(groupDetector.In);

groupDetector.Out.Do((groups, env) =>
{
    Console.WriteLine($"Detected {groups.Count} groups");
});
```

---

## AI & Machine Learning Components

### Ollama Component

**Purpose**: Local LLM integration via Ollama.

**Features**:
- Local LLM inference
- Multiple model support (Llama, Gemma, Mistral, etc.)
- Streaming responses
- Context management
- CPU and GPU inference

**Basic Usage**:
```csharp
using SAAC.Ollama;

var config = new OllamaConnectorConfiguration
{
    OllamaAddress = new Uri("http://localhost:11434"),
    Model = "llama2:7b",
    SystemPrompt = "You are a helpful assistant."
};

var ollama = new OllamaConnector(pipeline, config);

textStream.PipeTo(ollama.In);

ollama.Out.Do((response, env) =>
{
    Console.WriteLine($"LLM: {response}");
});
```

**Supported Models**: llama2, gemma, mistral, codellama, phi, etc.

**Dependencies**: Ollama server running locally

---

## Game Engine Integration Components

### Unity Component

**Purpose**: Unity engine integration utilities and helpers.

**Features**:
- Coordinate system conversion (Unity ↔ \\psi)
- Quaternion/Matrix transformations
- Unity-specific data formats
- Integration helpers

**Basic Usage**:
```csharp
using SAAC.Unity;

// Convert Unity position to \\psi coordinate system
var unityPosition = new Vector3(1, 2, 3);
var psiPosition = UnityHelpers.UnityToPs iCoordinates(unityPosition);

// Convert matrix to coordinate system
var matrix4x4 = Matrix4x4.Identity;
var coordinateSystem = MatrixToCoordinateSystem.Transform(matrix4x4);
```

**Related**: [WebRTC Component](WebRTC-Component.md) for Unity streaming

---

### UnrealRemoteConnector Component

**Purpose**: Unreal Engine HTTP-based integration.

**Features**:
- HTTP request/response
- JSON data exchange
- Remote function calls
- Event handling
- Alternative to WebRTC data channels

**Basic Usage**:
```csharp
using SAAC.UnrealRemoteConnector;

var config = new UnrealConnectorConfiguration
{
    ServerAddress = "http://192.168.1.100:8080",
    PollInterval = TimeSpan.FromMilliseconds(100)
};

var connector = new UnrealConnector(pipeline, config);

// Send command to Unreal
connector.SendCommand("SpawnActor", new { type = "Cube", position = new { x = 0, y = 0, z = 1 } });

// Receive events from Unreal
connector.Events.Do((eventData, env) =>
{
    Console.WriteLine($"Unreal event: {eventData}");
});
```

**Related**: [WebRTC Component](WebRTC-Component.md) for video streaming

---

## Utility Components

### GlobalHelpers Component

**Purpose**: Global helper utilities used across SAAC framework.

**Features**:
- Mathematical utilities
- Geometry helpers
- Type conversion utilities
- Extension methods

---

### Helpers Component

**Purpose**: General helper components for common tasks.

**Features**:
- Image processing utilities
- Data transformation helpers
- Stream manipulation utilities

---

### PsiFormats Component

**Purpose**: Custom \\psi serialization formats.

**Formats**:
- `PsiFormatMatrix4x4`: 4x4 matrix serialization
- `PsiFormatVector3`: Vector3 serialization
- `PsiFormatBodies`: Body list serialization
- `PsiFormatEncodedImage`: Image serialization
- `PsiFormatString`: String serialization
- `PsiFormatBoolean`: Boolean serialization
- Custom format creation templates

**Basic Usage**:
```csharp
using SAAC.PsiFormats;

// Register format with RendezVous
config.AddTopicFormatAndTransformer(
    "Position",
    typeof(Vector3),
    new PsiFormatVector3()
);
```

---

### InteropExtension Component

**Purpose**: Extended interop capabilities for \\psi.

**Features**:
- WebSocket server/client (`WebSocketsManager`)
- TCP improvements
- Custom transport protocols
- Enhanced network communication

**Basic Usage**:
```csharp
using SAAC.InteropExtension;

var wsManager = new WebSocketsManager(
    isServer: true,
    isSecure: false,
    address: "http://localhost:8080/ws/"
);

wsManager.OnNewWebSocketConnectedHandler += (sender, e) =>
{
    Console.WriteLine($"Client connected: {e.Item1}:{e.Item2}");
};

wsManager.Start((error) => { });
```

---

### PLUME Component

**Purpose**: PLUME file format integration.

**Features**:
- Import PLUME recordings
- Convert to \\psi format
- Timeline synchronization
- Multi-modal data support

**Basic Usage**:
```csharp
using SAAC.PLUME;

var config = new DatasetPipelineConfiguration
{
    DatasetPath = @"D:\Data",
    DatasetName = "PLUMEData.pds"
};

var plumePipeline = new PlumeDatasetPipeline(config, "PLUMEImporter");

plumePipeline.LoadPlumeFile(
    @"E:\Recordings\recording.plm",
    new Dictionary<string, Type>
    {
        { "Main Camera", typeof(CoordinateSystem) }
    }
);
```

---

### Visualizations Component

**Purpose**: Custom PsiStudio visualization objects.

**Visualization Objects**:
- `AugmentedBodyVisualizationObject`: 3D body rendering
- `AugmentedSkeletonVisualizationObject`: Skeleton overlay
- `SimplifiedBodyListVisualizationObject`: Multi-body view
- `SimplifiedFlockGroupVisualisationObject`: Group visualization
- `SocialGraphXYVisualizationObject`: Social network graph
- `LabeledPoint2DVisualizationObject`: 2D point annotations
- `PositionOrientationVisualizationObject`: Pose visualization

**Usage**: Install as PsiStudio plugin, visualizations appear in PsiStudio visualization panel

**Related**: [PsiStudio Modifications](PsiStudio-Modifications.md)

---

### PsiStudioReplayExtension Component

**Purpose**: PsiStudio replay extensions and enhancements.

**Features**:
- Enhanced replay controls
- Custom replay modes
- Extended timeline features
- Replay automation

---

## Remote Service Components Summary

The following components provide remote streaming capabilities via RendezVous protocol:

| Component | Purpose | Related App |
|-----------|---------|-------------|
| **WhisperRemoteServices** | Remote speech-to-text | [WhisperRemoteApp](WhisperRemoteApp.md) |
| **BodiesRemoteServices** | Remote body streaming | - |
| **KinectAzureRemoteServices** | Azure Kinect streaming | [CameraRemoteApp](CameraRemoteApp.md) |
| **KinectRemoteServices** | Kinect v2 streaming | [CameraRemoteApp](CameraRemoteApp.md) |
| **NuitrackRemoteServices** | Nuitrack streaming | [CameraRemoteApp](CameraRemoteApp.md) |

**Common Pattern** (Server):
```csharp
var service = new RemoteService(pipeline, config, port, host);
// Service automatically exports streams via RendezVous
```

**Common Pattern** (Client):
```csharp
var connector = new RemoteConnector(pipeline);
var importer = connector.ConnectToProcess("ProcessName");
var stream = importer.OpenStream<DataType>("StreamName");
```

## Component Development

### Creating a New Component

1. **Choose Base Interface**:
   ```csharp
   // Source component
   public class MySource : ISourceComponent
   {
       public IProducer<T> Out { get; }
   }
   
   // Consumer-Producer
   public class MyProcessor : IConsumerProducer<TIn, TOut>
   {
       public Receiver<TIn> In { get; }
       public IProducer<TOut> Out { get; }
   }
   
   // Consumer (sink)
   public class MySink : IConsumer<T>
   {
       public Receiver<T> In { get; }
   }
   ```

2. **Add Configuration Class**:
   ```csharp
   public class MyComponentConfiguration
   {
       public string Parameter { get; set; } = "default";
       
       public void Validate()
       {
           if (string.IsNullOrEmpty(Parameter))
               throw new ArgumentException(nameof(Parameter));
       }
   }
   ```

3. **Implement Component**:
   ```csharp
   public class MyComponent : IConsumerProducer<TIn, TOut>
   {
       private readonly MyComponentConfiguration config;
       
       public MyComponent(Pipeline pipeline, MyComponentConfiguration config = null)
       {
           this.config = config ?? new MyComponentConfiguration();
           this.config.Validate();
           
           In = pipeline.CreateReceiver<TIn>(this, Process, nameof(In));
           Out = pipeline.CreateEmitter<TOut>(this, nameof(Out));
       }
       
       private void Process(TIn input, Envelope envelope)
       {
           // Process data
           var output = TransformData(input);
           Out.Post(output, envelope.OriginatingTime);
       }
   }
   ```

4. **Add Tests**:
   ```csharp
   [TestClass]
   public class MyComponentTests
   {
       [TestMethod]
       public void TestProcessing()
       {
           using (var pipeline = Pipeline.Create())
           {
               var component = new MyComponent(pipeline);
               // Test logic
               pipeline.Run();
           }
       }
   }
   ```

5. **Create Documentation**:
   - Add README.md in component folder
   - Create wiki page: `docs/wiki/MyComponent-Component.md`
   - Include usage examples
   - Document configuration options
