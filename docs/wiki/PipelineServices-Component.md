# PipelineServices Component

## Overview

**PipelineServices** is a core SAAC component providing high-level pipeline management abstractions for the Microsoft Platform for Situated Intelligence (\\psi). It simplifies complex scenarios involving dataset management, RendezVous-based distributed computing, and dataset replay.

**Key Features**:
- ? **RendezVousPipeline**: Distributed pipeline orchestration via RendezVous protocol
- ? **ReplayPipeline**: Dataset replay with processing capabilities
- ? **DatasetPipeline**: Base class for dataset-aware pipelines
- ? **ConnectorsManager**: Stream connection management
- ? Automatic store creation and session management
- ? Network streaming and remote process coordination
- ? Command protocol for remote control

## Components

### 1. RendezVousPipeline

Orchestrates distributed \\psi applications across multiple machines using the RendezVous discovery protocol.

**Use Cases**:
- Coordinate multiple remote applications (cameras, sensors, Unity, etc.)
- Centralized data collection from distributed sources
- Real-time multimodal data synchronization
- Remote application control (Run/Stop/Status commands)

**Example**:
```csharp
using SAAC.PipelineServices;

var config = new RendezVousPipelineConfiguration
{
    RendezVousHost = "192.168.1.100",
    DatasetPath = @"D:\Experiments",
    DatasetName = "Study2024.pds",
    StoreMode = DatasetPipeline.StoreMode.Process,
    CommandPort = 11511
};

// Add Unity stream formats
config.AddTopicFormatAndTransformer(
    "Head",
    typeof(System.Numerics.Matrix4x4),
    new PsiFormatMatrix4x4(),
    typeof(MatrixToCoordinateSystem)
);

var pipeline = new RendezVousPipeline(config, "Server");

// Handle new process connections
pipeline.AddNewProcessEvent((sender, e) =>
{
    Console.WriteLine($"New process: {e.Item1}");
    // Process streams in e.Item2
});

pipeline.Start();
```

**Architecture**:
```
???????????????????????????????????????????????????????????
?             RendezVous Server                           ?
?          (Process Discovery)                            ?
???????????????????????????????????????????????????????????
                   ?
    ?????????????????????????????????????????????????
    ?              ?              ?                 ?
??????????   ???????????   ??????????   ???????????????
? Unity  ?   ? Kinect  ?   ?Whisper ?   ?VideoCapture ?
?Process ?   ? Process ?   ?Process ?   ?   Process   ?
??????????   ???????????   ??????????   ???????????????
    ?              ?              ?                 ?
    ?????????????????????????????????????????????????
                   ?
         ??????????????????????
         ? RendezVousPipeline ?
         ?   (Stores All)     ?
         ??????????????????????
```

### 2. ReplayPipeline

Replays previously recorded \\psi datasets with optional processing.

**Use Cases**:
- Post-processing of recorded data
- Algorithm development and testing
- Feature extraction from historical data
- Re-transcription or re-analysis
- Streaming replayed data to network/Unity

**Example**:
```csharp
using SAAC.PipelineServices;

var config = new ReplayPipelineConfiguration
{
    ReplayType = ReplayPipeline.ReplayType.FullSpeed,
    DatasetPath = @"D:\Recordings",
    DatasetName = "Experiment.pds",
    ProgressReport = new ConsoleProgressReporter()
};

var replay = new ReplayPipeline(config);

if (replay.LoadDatasetAndConnectors())
{
    // Access streams via connectors
    var session = replay.Dataset.Sessions.First();
    var connectors = replay.Connectors[session.Name];
    
    // Create processing subpipeline
    var processor = replay.CreateSubpipeline("Processing");
    
    // Bridge and process streams
    if (connectors.TryGetValue("Temperature", out var connector))
    {
        var stream = connector.CreateBridge<double>(processor);
        var processed = stream.Out.Select(v => v * 1.8 + 32);
        
        // Save results
        var outputSession = replay.GetOrCreateSession("Processed");
        replay.CreateStore(
            processor,
            outputSession,
            "TemperatureFahrenheit",
            "ProcessedData",
            processed
        );
    }
    
    processor.RunAsync();
    replay.RunPipelineAndSubpipelines();
    replay.Pipeline.WaitAll();
}

replay.Dispose();
```

**Replay Modes**:

| Mode | Description | Use Case |
|------|-------------|----------|
| **FullSpeed** | Maximum processing speed | Batch processing |
| **RealTime** | Original recording speed | Visualization, real-time testing |
| **IntervalFullSpeed** | Fast segment replay | Event analysis |
| **IntervalRealTime** | Real-time segment replay | Demonstrations |

### 3. DatasetPipeline

Base class for pipelines that work with \\psi datasets.

**Features**:
- Automatic dataset creation and management
- Session management
- Store creation with naming conventions
- Connector management for stream access
- Subpipeline creation and tracking
- Diagnostic logging

**Example**:
```csharp
public class MyCustomPipeline : DatasetPipeline
{
    public MyCustomPipeline(DatasetPipelineConfiguration config)
        : base(config, "MyPipeline")
    {
    }
    
    protected override void PipelineRunAsync()
    {
        // Your custom run logic
        Pipeline.RunAsync();
    }
}
```

### 4. ConnectorsManager & ConnectorInfo

Manages stream connections across sessions and stores.

**ConnectorInfo** provides:
- Stream metadata (name, session, store, type)
- `CreateBridge<T>()` for cross-pipeline connections
- Type-safe stream access

**Example**:
```csharp
// Access connectors from ReplayPipeline
var sessionConnectors = replay.Connectors["Session_001"];

// List all available streams
foreach (var connector in sessionConnectors)
{
    Console.WriteLine($"Stream: {connector.Key}");
    Console.WriteLine($"  Type: {connector.Value.DataType.Name}");
    Console.WriteLine($"  Store: {connector.Value.StoreName}");
}

// Bridge specific stream to subpipeline
if (sessionConnectors.TryGetValue("RGB", out var rgbConnector))
{
    var subpipeline = replay.CreateSubpipeline("VideoProcessor");
    var videoStream = rgbConnector.CreateBridge<Shared<EncodedImage>>(
        subpipeline
    );
    
    // Use videoStream.Out for processing
}
```

## Configuration Classes

### RendezVousPipelineConfiguration

```csharp
public class RendezVousPipelineConfiguration : DatasetPipelineConfiguration
{
    // RendezVous Settings
    public string RendezVousHost { get; set; } = "localhost";
    public int RendezVousPort { get; set; } = 13330;
    public int CommandPort { get; set; } = 11511;
    
    // Topic Formats (for Unity, etc.)
    public Dictionary<string, (Type, dynamic, Type?)> TopicFormats { get; set; }
    
    // Methods
    public void AddTopicFormatAndTransformer(
        string topic,
        Type type,
        dynamic format,
        Type? transformer = null)
    {
        TopicFormats[topic] = (type, format, transformer);
    }
}
```

### ReplayPipelineConfiguration

```csharp
public class ReplayPipelineConfiguration : DatasetPipelineConfiguration
{
    public ReplayPipeline.ReplayType ReplayType { get; set; }
        = ReplayPipeline.ReplayType.FullSpeed;
    
    public TimeInterval ReplayInterval { get; set; }
        = TimeInterval.Infinite;
    
    public IProgress<double> ProgressReport { get; set; }
    
    public bool DatasetBackup { get; set; } = false;
}
```

### DatasetPipelineConfiguration

```csharp
public class DatasetPipelineConfiguration
{
    public string DatasetPath { get; set; }
    public string DatasetName { get; set; }
    
    public bool AutomaticPipelineRun { get; set; } = true;
    
    public DiagnosticsMode Diagnostics { get; set; }
        = DiagnosticsMode.Off;
    
    public StoreMode StoreMode { get; set; }
        = StoreMode.Process;
    
    public bool Debug { get; set; } = false;
}
```

## Integration Scenarios

### Scenario 1: RendezVous + Replay

Stream replayed data to Unity or other applications:

```csharp
// Create replay pipeline
var replayConfig = new ReplayPipelineConfiguration
{
    ReplayType = ReplayPipeline.ReplayType.RealTime,
    DatasetPath = @"D:\Recordings",
    DatasetName = "KinectCapture.pds",
    AutomaticPipelineRun = false
};

var replayPipeline = new ReplayPipeline(replayConfig);
replayPipeline.LoadDatasetAndConnectors();

// Create RendezVous pipeline with replay connectors
var rdvConfig = new RendezVousPipelineConfiguration
{
    RendezVousHost = "localhost",
    CommandPort = 0  // Disable command receiver
};

var rdvPipeline = new RendezVousPipeline(
    replayPipeline.Pipeline,
    rdvConfig,
    "ReplayServer",
    null,
    null,
    replayPipeline.Connectors
);

// Generate network exporters for all streams
rdvPipeline.GenerateRemoteProcessFromConnectors("ReplayedData", 11411);

// Start RendezVous and replay
rdvPipeline.Start();
replayPipeline.RunPipelineAndSubpipelines();
replayPipeline.Pipeline.WaitAll();

rdvPipeline.Dispose();
replayPipeline.Dispose();
```

### Scenario 2: Multiple Remote Applications

Collect data from multiple sources:

```csharp
var config = new RendezVousPipelineConfiguration
{
    RendezVousHost = "192.168.1.100",
    DatasetPath = @"D:\MultiModal",
    DatasetName = "Experiment.pds",
    StoreMode = DatasetPipeline.StoreMode.Process
};

// Add formats for different sources
config.AddTopicFormatAndTransformer(
    "RGB",
    typeof(Shared<EncodedImage>),
    new PsiFormatEncodedImage()
);

config.AddTopicFormatAndTransformer(
    "Skeleton",
    typeof(List<Bodies>),
    new PsiFormatBodies()
);

config.AddTopicFormatAndTransformer(
    "Transcription",
    typeof(string),
    new PsiFormatString()
);

var pipeline = new RendezVousPipeline(config, "CollectionServer");

// Event handler for new processes
pipeline.AddNewProcessEvent((sender, e) =>
{
    Console.WriteLine($"Connected: {e.Item1}");
    
    // Automatically stores all streams from:
    // - VideoRemoteApp
    // - CameraRemoteApp
    // - WhisperRemoteApp
    // - Unity application
});

pipeline.Start();

// Send commands to remote apps
pipeline.SendCommand(
    RendezVousPipeline.Command.Run,
    "VideoRemoteApp",
    ""
);

// Wait for user to stop
Console.ReadLine();

pipeline.SendCommand(
    RendezVousPipeline.Command.Stop,
    "VideoRemoteApp",
    ""
);

pipeline.Dispose();
```

### Scenario 3: Custom Processing Pipeline

Extend DatasetPipeline for custom workflows:

```csharp
public class FeatureExtractionPipeline : DatasetPipeline
{
    private List<IFeatureExtractor> extractors;
    
    public FeatureExtractionPipeline(
        DatasetPipelineConfiguration config,
        List<IFeatureExtractor> extractors)
        : base(config, "FeatureExtraction")
    {
        this.extractors = extractors;
    }
    
    public void ProcessDataset(string datasetPath, string sessionName)
    {
        // Load dataset
        var dataset = Dataset.Load(datasetPath);
        var session = dataset.Sessions.First(s => s.Name == sessionName);
        
        // Create processing subpipeline
        var processor = CreateSubpipeline("Processor");
        
        // Load streams
        var importer = PsiStore.Open(Pipeline, "VideoCapture1", session.Path);
        
        // Apply extractors
        foreach (var extractor in extractors)
        {
            var features = extractor.Extract(importer);
            
            // Store features
            CreateStore(
                processor,
                session,
                extractor.Name,
                "Features",
                features
            );
        }
        
        // Run
        processor.RunAsync();
        Pipeline.Run();
    }
    
    protected override void PipelineRunAsync()
    {
        // Custom run logic if needed
    }
}

// Usage
var config = new DatasetPipelineConfiguration
{
    DatasetPath = @"D:\Features",
    DatasetName = "ExtractedFeatures.pds"
};

var extractors = new List<IFeatureExtractor>
{
    new ColorHistogramExtractor(),
    new MotionFeatureExtractor(),
    new AudioFeatureExtractor()
};

var featurePipeline = new FeatureExtractionPipeline(config, extractors);
featurePipeline.ProcessDataset(@"D:\Recordings\Video.pds", "Session_001");
```

## Command Protocol

RendezVousPipeline supports remote command execution:

### Available Commands

| Command | Description | Arguments |
|---------|-------------|-----------|
| **Initialize** | Initialize remote application | Configuration string |
| **Run** | Start data capture | None |
| **Stop** | Stop data capture | None |
| **Pause** | Pause capture | None |
| **Resume** | Resume after pause | None |
| **Status** | Request status report | None |
| **Close** | Shutdown application | None |

### Sending Commands

```csharp
// Send Run command to all applications
pipeline.SendCommand(
    RendezVousPipeline.Command.Run,
    null,  // null = broadcast to all
    ""
);

// Send Stop to specific application
pipeline.SendCommand(
    RendezVousPipeline.Command.Stop,
    "CameraApp1",
    ""
);

// Send Initialize with arguments
pipeline.SendCommand(
    RendezVousPipeline.Command.Initialize,
    "WhisperApp",
    "model=large;language=en"
);
```

### Receiving Commands (in remote app)

```csharp
// In remote application
var commandDelegate = (string source, Message<(Command, string)> message) =>
{
    var (command, args) = message.Data;
    
    switch (command)
    {
        case RendezVousPipeline.Command.Run:
            StartCapture();
            break;
        case RendezVousPipeline.Command.Stop:
            StopCapture();
            break;
        case RendezVousPipeline.Command.Initialize:
            ParseAndApplyConfig(args);
            break;
    }
};

var config = new RendezVousPipelineConfiguration
{
    CommandDelegate = commandDelegate,
    CommandPort = 11511
};
```

## Store Modes

Control how data is organized:

### Process Mode

Each remote process gets its own store:
```
Dataset\
?? Session_001\
    ?? CameraApp1\
    ?   ?? RGB
    ?   ?? Depth
    ?? WhisperApp\
    ?   ?? Audio
    ?   ?? Transcription
    ?? UnityApp\
        ?? Head
```

### Single Mode

All streams in one store:
```
Dataset\
?? Session_001\
    ?? AllStreams\
        ?? CameraApp1.RGB
        ?? CameraApp1.Depth
        ?? WhisperApp.Audio
        ?? WhisperApp.Transcription
```

### Split Mode

Streams split by data type:
```
Dataset\
?? Session_001\
    ?? Video\
    ?   ?? RGB
    ?   ?? Depth
    ?? Audio\
    ?   ?? Microphone1
    ?   ?? Microphone2
    ?? Tracking\
        ?? Skeleton
```

## Diagnostics

Enable diagnostics for troubleshooting:

```csharp
var config = new RendezVousPipelineConfiguration
{
    Diagnostics = DatasetPipeline.DiagnosticsMode.Store,
    Debug = true
};
```

**Diagnostics Modes**:
- **Off**: No diagnostics
- **Console**: Log to console
- **Store**: Save diagnostics to \\psi store
- **Both**: Console and store

**Debug Mode**:
- Enables verbose logging
- Shows detailed connection info
- Reports stream statistics

## Best Practices

### 1. Resource Management

Always dispose pipelines:
```csharp
RendezVousPipeline pipeline = null;
try
{
    pipeline = new RendezVousPipeline(config, "Server");
    pipeline.Start();
    // Use pipeline
}
finally
{
    pipeline?.Dispose();
}
```

### 2. Error Handling

Handle pipeline errors:
```csharp
pipeline.AddNewProcessEvent((sender, e) =>
{
    try
    {
        // Process new connection
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing {e.Item1}: {ex.Message}");
    }
});
```

### 3. Configuration Validation

Validate before use:
```csharp
var config = new RendezVousPipelineConfiguration();
config.DatasetPath = @"D:\Data";
config.DatasetName = "Experiment.pds";

// Validate
if (!Directory.Exists(config.DatasetPath))
{
    Directory.CreateDirectory(config.DatasetPath);
}
```

### 4. Command Synchronization

Coordinate command timing:
```csharp
// Wait for all processes to connect
while (pipeline.GetConnectedProcessCount() < 3)
{
    Thread.Sleep(100);
}

// Send synchronized Run command
pipeline.SendCommand(RendezVousPipeline.Command.Run, null, "");
```

## Troubleshooting

### RendezVous Connection Failed

**Symptoms**:
- Remote applications can't connect
- No processes appear in pipeline

**Solutions**:
- Verify RendezVous server is running
- Check firewall rules (port 13330)
- Confirm IP addresses are correct
- Test with `telnet <host> 13330`

### Store Creation Failed

**Symptoms**:
- Exception when creating stores
- Data not being recorded

**Solutions**:
- Check DatasetPath exists and is writable
- Verify sufficient disk space
- Ensure no path length issues (<260 chars)
- Check store naming conflicts

### Replay Loading Failed

**Symptoms**:
- `LoadDatasetAndConnectors()` returns false
- Connectors dictionary is empty

**Solutions**:
- Verify dataset path and name
- Check `.pds` file exists
- Ensure compatible \\psi version
- Validate dataset integrity

### Memory Issues

**Symptoms**:
- Out of memory exceptions
- Slow performance

**Solutions**:
- Process in smaller time intervals
- Use FullSpeed replay mode
- Dispose resources promptly
- Monitor with diagnostics

## See Also

- [ReplayPipeline Guide](ReplayPipeline-Guide.md) - Detailed replay documentation
- [RendezVousPipeline Guide](RendezVousPipeline-Guide.md) - Distributed pipeline guide
- [ServerApplication](ServerApplication.md) - GUI application using PipelineServices
- [Architecture Overview](Architecture.md) - SAAC framework architecture
- [Components Overview](Components-Overview.md) - All SAAC components
