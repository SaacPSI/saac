# ReplayPipeline - Dataset Replay Guide

## Overview

**ReplayPipeline** is a specialized pipeline class that enables replaying previously recorded Psi datasets. It extends `DatasetPipeline` and implements `ISourceComponent`, providing a robust framework for loading and replaying stored sensor data, processed results, and multi-modal recordings.

**Key Features**:
- ✓ Four replay modes (FullSpeed, RealTime, IntervalFullSpeed, IntervalRealTime)
- ✓ Dataset loading and validation
- ✓ Progress reporting during replay
- ✓ Read-only store protection
- ✓ Dataset backup creation
- ✓ Integration with RendezVous protocol
- ✓ Support for processing during replay
- ✓ PsiStudio visualization integration

## Use Cases

**Common Scenarios**:
- **Post-Processing**: Apply new algorithms to recorded data
- **Re-Analysis**: Extract different features from existing datasets
- **Validation**: Test processing pipelines on known datasets
- **Debugging**: Step through recorded data to diagnose issues
- **Visualization**: Replay data in PsiStudio for analysis
- **Batch Processing**: Process multiple recorded sessions automatically
- **Algorithm Development**: Iterate on algorithms using recorded data

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                  ReplayPipeline                         │
│         (extends DatasetPipeline)                       │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │         Dataset Loader                            │  │
│  │  • Load Psi stores                                │  │
│  │  • Discover streams                               │  │
│  │  • Validate dataset integrity                     │  │
│  └───────────────────────────────────────────────────┘  │
│               ↓                                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │         Replay Engine                             │  │
│  │  • Stream data at configured rate                 │  │
│  │  • Maintain temporal consistency                  │  │
│  │  • Report progress                                │  │
│  └───────────────────────────────────────────────────┘  │
│               ↓                                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │         Processing Pipeline                       │  │
│  │  • Apply new components                           │  │
│  │  • Generate new streams                           │  │
│  │  • Save processed results                         │  │
│  └───────────────────────────────────────────────────┘  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

## Replay Modes

ReplayPipeline supports four replay modes:

### 1. FullSpeed

Replay data as fast as the system can process it.

**Characteristics**:
- ✓ Maximum processing speed
- ✓ No real-time constraints
- ✓ Ideal for batch processing
- ✓ Temporal relationships preserved

**When to Use**:
- Batch processing of large datasets
- Extracting features for offline analysis
- Testing processing pipeline performance
- Re-transcoding or format conversion

**Example**:
```csharp
var config = new ReplayPipelineConfiguration();
config.ReplayType = ReplayPipeline.ReplayType.FullSpeed;
config.DatasetPath = @"D:\Recordings\ExperimentData";
config.DatasetName = "Study2024.pds";

var replay = new ReplayPipeline(config);
replay.LoadDatasetAndConnectors();
replay.RunPipelineAndSubpipelines();
```

### 2. RealTime

Replay data at the original recording speed.

**Characteristics**:
- ✓ Original playback speed
- ✓ Synchronized playback
- ✓ Suitable for visualization
- ✓ Audio playback compatible

**When to Use**:
- Visualizing recordings in real-time
- Testing real-time processing algorithms
- Demonstrating recorded sessions
- Debugging temporal synchronization issues

**Example**:
```csharp
var config = new ReplayPipelineConfiguration();
config.ReplayType = ReplayPipeline.ReplayType.RealTime;
config.DatasetPath = @"D:\Recordings\MeetingCapture";
config.DatasetName = "Team_Meeting.pds";

var replay = new ReplayPipeline(config);
replay.LoadDatasetAndConnectors();
replay.RunPipelineAndSubpipelines();
```

### 3. IntervalFullSpeed

Replay a specific time interval at maximum speed.

**Characteristics**:
- ✓ Fast processing of segments
- ✓ Targeted analysis
- ✓ Configurable time range
- ✓ Efficient for specific events

**When to Use**:
- Analyzing specific events or segments
- Processing only relevant portions
- Testing on known time windows
- Reducing processing time for large datasets

**Example**:
```csharp
var config = new ReplayPipelineConfiguration();
config.ReplayType = ReplayPipeline.ReplayType.IntervalFullSpeed;
config.ReplayInterval = new TimeInterval(
    DateTime.Parse("2024-01-15 14:30:00"),
    DateTime.Parse("2024-01-15 14:45:00")
);
config.DatasetPath = @"D:\Recordings\LongSession";
config.DatasetName = "AllDayCapture.pds";

var replay = new ReplayPipeline(config);
replay.LoadDatasetAndConnectors();
replay.RunPipelineAndSubpipelines();
```

### 4. IntervalRealTime

Replay a specific time interval at original speed.

**Characteristics**:
- ✓ Segment selection
- ✓ Real-time playback
- ✓ Visualization friendly
- ✓ Focused analysis

**When to Use**:
- Visualizing specific events
- Presenting selected portions
- Debugging specific time ranges
- Real-time testing on segments

**Example**:
```csharp
var config = new ReplayPipelineConfiguration();
config.ReplayType = ReplayPipeline.ReplayType.IntervalRealTime;
config.ReplayInterval = new TimeInterval(
    DateTime.Parse("2024-01-15 14:30:00"),
    TimeSpan.FromMinutes(15)
);
config.DatasetPath = @"D:\Recordings\Presentation";
config.DatasetName = "Conference.pds";

var replay = new ReplayPipeline(config);
replay.LoadDatasetAndConnectors();
replay.RunPipelineAndSubpipelines();
```

## Configuration

### ReplayPipelineConfiguration

Complete configuration options:

```csharp
public class ReplayPipelineConfiguration : DatasetPipelineConfiguration
{
    // Replay Mode
    public ReplayPipeline.ReplayType ReplayType { get; set; } 
        = ReplayPipeline.ReplayType.FullSpeed;
    
    // Time Interval (for Interval modes)
    public TimeInterval ReplayInterval { get; set; } 
        = TimeInterval.Infinite;
    
    // Progress Reporting
    public IProgress<double> ProgressReport { get; set; } = null;
    
    // Dataset Configuration
    public string DatasetPath { get; set; }
    public string DatasetName { get; set; }
    
    // Backup Options
    public bool DatasetBackup { get; set; } = false;
    
    // Pipeline Configuration (inherited from DatasetPipelineConfiguration)
    public bool AutomaticPipelineRun { get; set; } = true;
    public DiagnosticsMode Diagnostics { get; set; } = DiagnosticsMode.Off;
    public StoreMode StoreMode { get; set; } = StoreMode.Process;
}
```

### Progress Reporting

Implement `IProgress<double>` to track replay progress:

```csharp
public class ReplayProgressReporter : IProgress<double>
{
    public void Report(double value)
    {
        Console.WriteLine($"Replay progress: {value:P}");
        
        // Update UI progress bar
        // Log to file
        // Trigger events
    }
}

// Usage
var config = new ReplayPipelineConfiguration();
config.ProgressReport = new ReplayProgressReporter();
```

**Progress Values**:
- `0.0` - Replay started
- `0.0` to `1.0` - Progress percentage
- `1.0` - Replay completed

## Basic Usage

### Example 1: Simple Dataset Replay

```csharp
using Microsoft.Psi;
using SAAC.PipelineServices;

// Configure replay
var config = new ReplayPipelineConfiguration
{
    ReplayType = ReplayPipeline.ReplayType.FullSpeed,
    DatasetPath = @"D:\Recordings",
    DatasetName = "ExperimentData.pds",
    AutomaticPipelineRun = true
};

// Create replay pipeline
var replay = new ReplayPipeline(config);

// Load dataset
if (replay.LoadDatasetAndConnectors())
{
    Console.WriteLine("Dataset loaded successfully");
    
    // Run replay
    replay.RunPipelineAndSubpipelines();
    
    // Wait for completion
    replay.Pipeline.WaitAll();
    
    Console.WriteLine("Replay completed");
}
else
{
    Console.WriteLine("Failed to load dataset");
}

// Cleanup
replay.Dispose();
```

### Example 2: Replay with Progress Reporting

```csharp
using Microsoft.Psi;
using SAAC.PipelineServices;
using System;

// Progress reporter
public class ConsoleProgressReporter : IProgress<double>
{
    private DateTime startTime;
    
    public ConsoleProgressReporter()
    {
        startTime = DateTime.Now;
    }
    
    public void Report(double value)
    {
        var elapsed = DateTime.Now - startTime;
        var estimated = value > 0 
            ? TimeSpan.FromSeconds(elapsed.TotalSeconds / value)
            : TimeSpan.Zero;
        var remaining = estimated - elapsed;
        
        Console.Write($"\rProgress: {value:P} | ");
        Console.Write($"Elapsed: {elapsed:hh\\:mm\\:ss} | ");
        Console.Write($"Remaining: {remaining:hh\\:mm\\:ss}");
    }
}

// Usage
var config = new ReplayPipelineConfiguration
{
    ReplayType = ReplayPipeline.ReplayType.FullSpeed,
    DatasetPath = @"D:\Recordings",
    DatasetName = "LargeDataset.pds",
    ProgressReport = new ConsoleProgressReporter()
};

var replay = new ReplayPipeline(config);
replay.LoadDatasetAndConnectors();
replay.RunPipelineAndSubpipelines();
replay.Pipeline.WaitAll();
replay.Dispose();
```

### Example 3: Replay with Dataset Backup

```csharp
var config = new ReplayPipelineConfiguration
{
    ReplayType = ReplayPipeline.ReplayType.FullSpeed,
    DatasetPath = @"D:\Recordings",
    DatasetName = "ImportantData.pds",
    DatasetBackup = true  // Creates ImportantData_backup.pds
};

var replay = new ReplayPipeline(config);
replay.LoadDatasetAndConnectors();
replay.RunPipelineAndSubpipelines();
replay.Pipeline.WaitAll();
replay.Dispose();
```

## Processing During Replay

### Example 4: Apply New Processing Pipeline

```csharp
using Microsoft.Psi;
using Microsoft.Psi.Imaging;
using SAAC.PipelineServices;

var config = new ReplayPipelineConfiguration
{
    ReplayType = ReplayPipeline.ReplayType.FullSpeed,
    DatasetPath = @"D:\Recordings",
    DatasetName = "VideoCapture.pds",
    AutomaticPipelineRun = false  // Manual pipeline control
};

var replay = new ReplayPipeline(config);

// Load dataset
if (replay.LoadDatasetAndConnectors())
{
    // Create subpipeline for processing
    var processingPipeline = replay.CreateSubpipeline("VideoProcessing");
    
    // Access stream from loaded connectors
    var session = replay.Dataset.Sessions.First();
    var sessionConnectors = replay.Connectors[session.Name];
    
    // Find the RGB video stream connector
    if (sessionConnectors.TryGetValue("RGB", out var rgbConnector))
    {
        // Bridge the stream to processing pipeline
        var videoStream = rgbConnector.CreateBridge<Shared<EncodedImage>>(
            processingPipeline
        );
        
        // Apply processing
        var processedVideo = videoStream.Out
            .Decode(new ImageFromStreamDecoder())
            .Select(img => ApplyImageProcessing(img));
        
        // Save processed results using replay's store management
        var outputSession = replay.GetOrCreateSession("ProcessedSession");
        replay.CreateStore(
            processingPipeline,
            outputSession,
            "ProcessedRGB",
            "ProcessedVideo",
            processedVideo.EncodeJpeg(90)
        );
        
        // Run processing
        processingPipeline.RunAsync();
        replay.RunPipelineAndSubpipelines();
        
        // Wait for completion
        replay.Pipeline.WaitAll();
    }
}

replay.Dispose();

Shared<Image> ApplyImageProcessing(Shared<Image> image)
{
    // Your image processing logic here
    // Example: Apply blur, edge detection, color correction, etc.
    return image;
}
```

### Example 5: Replay with RendezVous Integration

```csharp
using Microsoft.Psi;
using SAAC.PipelineServices;

// Configure replay pipeline
var replayConfig = new ReplayPipelineConfiguration
{
    ReplayType = ReplayPipeline.ReplayType.RealTime,
    DatasetPath = @"D:\Recordings",
    DatasetName = "SensorData.pds",
    AutomaticPipelineRun = false,
    ProgressReport = new ConsoleProgressReporter()
};

// Create replay pipeline
var replayPipeline = new ReplayPipeline(replayConfig);

// Load dataset and connectors
if (replayPipeline.LoadDatasetAndConnectors())
{
    // Configure RendezVous pipeline
    var rdvConfig = new RendezVousPipelineConfiguration
    {
        DatasetPath = @"D:\Output",
        DatasetName = "ReplayOutput.pds",
        RendezVousHost = "localhost",
        StoreMode = DatasetPipeline.StoreMode.Process,
        AutomaticPipelineRun = false,
        CommandPort = 0  // Disable command receiver for replay
    };
    
    // Create RendezVous pipeline with replay's pipeline and connectors
    var rdvPipeline = new RendezVousPipeline(
        replayPipeline.Pipeline,
        rdvConfig,
        "ReplayServer",
        null,  // No log override
        null,  // No dataset override
        replayPipeline.Connectors  // Pass replay connectors
    );
    
    // Generate remote process from loaded connectors
    // This creates network exporters for all streams in the dataset
    rdvPipeline.GenerateRemoteProcessFromConnectors(
        "ReplayedData",  // Process name
        11411  // Starting port
    );
    
    // Start RendezVous server
    rdvPipeline.Start();
    
    Console.WriteLine("Replay server started. Clients can connect now.");
    Console.WriteLine("Press any key to start replay...");
    Console.ReadLine();
    
    // Run replay (streams data through RendezVous)
    replayPipeline.RunPipelineAndSubpipelines();
    
    // Wait for completion
    replayPipeline.Pipeline.WaitAll();
    
    Console.WriteLine("Replay completed.");
    
    // Cleanup
    rdvPipeline.Dispose();
}

replayPipeline.Dispose();
```

## Integration with RendezVous

### Example 6: Replay with Custom Processing and Network Export

```csharp
using Microsoft.Psi;
using SAAC.PipelineServices;

// Configure replay
var replayConfig = new ReplayPipelineConfiguration
{
    ReplayType = ReplayPipeline.ReplayType.RealTime,
    DatasetPath = @"D:\Recordings",
    DatasetName = "MultiModalData.pds",
    AutomaticPipelineRun = false
};

var replayPipeline = new ReplayPipeline(replayConfig);

if (replayPipeline.LoadDatasetAndConnectors())
{
    // Configure RendezVous with Unity stream formats
    var rdvConfig = new RendezVousPipelineConfiguration
    {
        RendezVousHost = "localhost",
        AutomaticPipelineRun = false,
        CommandPort = 0
    };
    
    // Add topic formats for Unity
    rdvConfig.AddTopicFormatAndTransformer(
        "Left",
        typeof(System.Numerics.Matrix4x4),
        new PsiFormatMatrix4x4(),
        typeof(MatrixToCoordinateSystem)
    );
    
    rdvConfig.AddTopicFormatAndTransformer(
        "Right",
        typeof(System.Numerics.Matrix4x4),
        new PsiFormatMatrix4x4(),
        typeof(MatrixToCoordinateSystem)
    );
    
    rdvConfig.AddTopicFormatAndTransformer(
        "Head",
        typeof(System.Numerics.Matrix4x4),
        new PsiFormatMatrix4x4(),
        typeof(MatrixToCoordinateSystem)
    );
    
    // Create RendezVous pipeline with replay connectors
    var rdvPipeline = new RendezVousPipeline(
        replayPipeline.Pipeline,
        rdvConfig,
        "ReplayServer",
        null,
        null,
        replayPipeline.Connectors
    );
    
    // Generate network exporters from replay connectors
    rdvPipeline.GenerateRemoteProcessFromConnectors(
        "ReplayedUnityData",
        18852  // Starting port for streams
    );
    
    // Start RendezVous
    rdvPipeline.Start();
    
    Console.WriteLine("Replay server ready. Unity can connect now.");
    Console.WriteLine("Press Enter to start replay...");
    Console.ReadLine();
    
    // Run replay (data streams through RendezVous to Unity)
    replayPipeline.RunPipelineAndSubpipelines();
    replayPipeline.Pipeline.WaitAll();
    
    Console.WriteLine("Replay completed.");
    
    rdvPipeline.Dispose();
}

replayPipeline.Dispose();
```

## Integration with PsiStudio

### PsiStudio Network Streaming

ReplayPipeline can stream replayed data to PsiStudio for visualization. See [PsiStudio Modifications](PsiStudio-Modifications.md) for details on network streaming capabilities.

**Workflow**:
```
1. Configure ReplayPipeline with RealTime mode
   ↓
2. Stream data to PsiStudio Network
   ↓
3. Visualize in PsiStudio timeline
   ↓
4. Pause/seek through replayed data
```

### PsiStudio Pipeline Plugin

ReplayPipeline can be integrated as a PsiStudio pipeline plugin for in-application replay. See [PsiStudio Modifications - Part 1](PsiStudio-Modifications.md#part-1-psistudio-pipeline) for implementation details.

**Implementation**:
```csharp
public class ReplayPipelinePlugin : IPsiStudioPipeline
{
    private ReplayPipeline replayPipeline;
    
    public PipelineReplaybleMode GetReplayableMode()
    {
        return PipelineReplaybleMode.Replayable;
    }
    
    public bool RunPipeline(TimeInterval interval)
    {
        var config = new ReplayPipelineConfiguration
        {
            ReplayType = ReplayPipeline.ReplayType.IntervalRealTime,
            ReplayInterval = interval,
            DatasetPath = /* ... */,
            DatasetName = /* ... */
        };
        
        replayPipeline = new ReplayPipeline(config);
        replayPipeline.LoadDatasetAndConnectors();
        replayPipeline.RunPipelineAndSubpipelines();
        
        return true;
    }
    
    public void StopPipeline()
    {
        replayPipeline?.Stop();
    }
    
    public Dataset GetDataset()
    {
        return replayPipeline?.Dataset;
    }
    
    // Implement other IPsiStudioPipeline members...
}
```

## Advanced Scenarios

### Example 7: Replay with Custom Stream Processing

```csharp
using Microsoft.Psi;
using SAAC.PipelineServices;
using System.Linq;

var config = new ReplayPipelineConfiguration
{
    ReplayType = ReplayPipeline.ReplayType.FullSpeed,
    DatasetPath = @"D:\Recordings",
    DatasetName = "SensorData.pds",
    AutomaticPipelineRun = false
};

var replay = new ReplayPipeline(config);

if (replay.LoadDatasetAndConnectors())
{
    // Create processing subpipeline
    var processingPipeline = replay.CreateSubpipeline("EventDetection");
    
    // Access connectors
    var session = replay.Dataset.Sessions.First();
    var connectors = replay.Connectors[session.Name];
    
    // Find sensor stream
    if (connectors.TryGetValue("Temperature", out var tempConnector))
    {
        // Bridge stream to processing pipeline
        var sensorStream = tempConnector.CreateBridge<double>(
            processingPipeline
        );
        
        // Detect high-temperature events
        var events = sensorStream.Out
            .Where(value => value > 100)  // Threshold
            .Select((value, env) => new 
            { 
                Timestamp = env.OriginatingTime, 
                Value = value 
            });
        
        // Count events
        int eventCount = 0;
        events.Do(_ => 
        {
            eventCount++;
            Console.WriteLine($"Event #{eventCount} detected");
        });
        
        // Save events to new session
        var outputSession = replay.GetOrCreateSession("EventsSession");
        replay.CreateStore(
            processingPipeline,
            outputSession,
            "HighTemperatureEvents",
            "Events",
            events
        );
        
        // Run processing
        processingPipeline.RunAsync();
        replay.RunPipelineAndSubpipelines();
        replay.Pipeline.WaitAll();
        
        Console.WriteLine($"Total events detected: {eventCount}");
    }
}

replay.Dispose();
```

### Example 8: Time-Series Data Export to CSV

```csharp
using Microsoft.Psi;
using SAAC.PipelineServices;
using System.IO;
using System.Linq;

var config = new ReplayPipelineConfiguration
{
    ReplayType = ReplayPipeline.ReplayType.FullSpeed,
    DatasetPath = @"D:\Recordings",
    DatasetName = "EnvironmentalSensors.pds",
    AutomaticPipelineRun = false
};

var replay = new ReplayPipeline(config);

if (replay.LoadDatasetAndConnectors())
{
    // Create export subpipeline
    var exportPipeline = replay.CreateSubpipeline("CSVExport");
    
    // Access connectors
    var session = replay.Dataset.Sessions.First();
    var connectors = replay.Connectors[session.Name];
    
    // Bridge multiple streams
    var tempStream = connectors["Temperature"]
        .CreateBridge<double>(exportPipeline);
    var humidityStream = connectors["Humidity"]
        .CreateBridge<double>(exportPipeline);
    var pressureStream = connectors["Pressure"]
        .CreateBridge<double>(exportPipeline);
    
    // Prepare CSV export
    var csvPath = @"D:\Exports\environmental_data.csv";
    Directory.CreateDirectory(Path.GetDirectoryName(csvPath));
    
    var writer = new StreamWriter(csvPath);
    writer.WriteLine("Timestamp,Temperature,Humidity,Pressure");
    
    // Join streams and export to CSV
    tempStream.Out
        .Join(humidityStream.Out)
        .Join(pressureStream.Out)
        .Do((data, env) =>
        {
            var ((temp, humidity), pressure) = data;
            var line = $"{env.OriginatingTime:O},{temp},{humidity},{pressure}";
            writer.WriteLine(line);
        });
    
    // Handle pipeline completion
    exportPipeline.PipelineCompleted += (s, e) =>
    {
        writer.Flush();
        writer.Close();
        Console.WriteLine($"Export completed: {csvPath}");
    };
    
    // Run export
    exportPipeline.RunAsync();
    replay.RunPipelineAndSubpipelines();
    replay.Pipeline.WaitAll();
}

replay.Dispose();
```

## Unity Integration

ReplayPipeline can replay data for Unity applications. See [Psi in Unity](../PsiInUnity.md) for Unity modifications and [Unity Integration](Unity-Integration.md) for detailed integration guide.

**Use Case**: Test Unity VR applications with recorded sensor data without physical sensors.

```csharp
using Microsoft.Psi;
using SAAC.PipelineServices;

// Configure replay
var replayConfig = new ReplayPipelineConfiguration
{
    ReplayType = ReplayPipeline.ReplayType.RealTime,
    DatasetPath = @"D:\Recordings",
    DatasetName = "AzureKinectCapture.pds",
    AutomaticPipelineRun = false
};

var replayPipeline = new ReplayPipeline(replayConfig);

if (replayPipeline.LoadDatasetAndConnectors())
{
    // Configure RendezVous for Unity
    var rdvConfig = new RendezVousPipelineConfiguration
    {
        RendezVousHost = "localhost",
        AutomaticPipelineRun = false,
        CommandPort = 0
    };
    
    // Add Unity stream formats
    rdvConfig.AddTopicFormatAndTransformer(
        "Skeleton",
        typeof(List<Bodies>),
        new PsiFormatBodies()
    );
    
    rdvConfig.AddTopicFormatAndTransformer(
        "RGB",
        typeof(Shared<EncodedImage>),
        new PsiFormatEncodedImage()
    );
    
    // Create RendezVous pipeline with replay connectors
    var rdvPipeline = new RendezVousPipeline(
        replayPipeline.Pipeline,
        rdvConfig,
        "KinectReplayServer",
        null,
        null,
        replayPipeline.Connectors
    );
    
    // Generate Unity-compatible network exporters
    rdvPipeline.GenerateRemoteProcessFromConnectors(
        "ReplayedKinect",
        11411  // Unity will connect to this port range
    );
    
    // Start RendezVous
    rdvPipeline.Start();
    
    Console.WriteLine("Kinect replay server ready.");
    Console.WriteLine("Start Unity application, then press Enter to replay...");
    Console.ReadLine();
    
    // Run replay (Unity receives data in real-time)
    replayPipeline.RunPipelineAndSubpipelines();
    replayPipeline.Pipeline.WaitAll();
    
    Console.WriteLine("Replay completed.");
    
    rdvPipeline.Dispose();
}

replayPipeline.Dispose();
```

## Troubleshooting

### Dataset Not Found

**Symptom**: `LoadDatasetAndConnectors()` returns false

**Solutions**:
- Verify `DatasetPath` and `DatasetName` are correct
- Check `.pds` file exists
- Ensure read permissions on directory
- Check for file corruption

```csharp
if (!replay.LoadDatasetAndConnectors())
{
    Console.WriteLine("Failed to load dataset");
    Console.WriteLine($"Path: {config.DatasetPath}");
    Console.WriteLine($"Name: {config.DatasetName}");
    
    // Check file existence
    var pdsPath = Path.Combine(
        config.DatasetPath, 
        config.DatasetName
    );
    
    if (!File.Exists(pdsPath))
    {
        Console.WriteLine($"File not found: {pdsPath}");
    }
}
```

### Slow Replay Performance

**Symptom**: Replay slower than expected

**Solutions**:
- Use `FullSpeed` mode
- Disable diagnostics
- Reduce processing complexity
- Check disk I/O performance
- Close other applications

```csharp
var config = new ReplayPipelineConfiguration
{
    ReplayType = ReplayPipeline.ReplayType.FullSpeed,
    DatasetPath = @"D:\Recordings",
    DatasetName = "Data.pds",
    Diagnostics = DiagnosticsMode.Off  // Disable diagnostics
};
```

### Stream Not Found

**Symptom**: Stream not found in `replay.Connectors`

**Solutions**:
- Verify stream name exists in dataset
- Check session name
- List available connectors
- Verify dataset loaded successfully

```csharp
if (!replay.LoadDatasetAndConnectors())
{
    Console.WriteLine("Failed to load dataset");
    return;
}

// List all available streams from connectors
Console.WriteLine("Available sessions and streams:");
foreach (var session in replay.Connectors)
{
    Console.WriteLine($"Session: {session.Key}");
    foreach (var connector in session.Value)
    {
        Console.WriteLine($"  - {connector.Key} (Type: {connector.Value.DataType.Name})");
        Console.WriteLine($"    Store: {connector.Value.StoreName}");
    }
}

// Access specific stream
var sessionName = replay.Dataset.Sessions.First().Name;
if (replay.Connectors[sessionName].TryGetValue("Temperature", out var tempConnector))
{
    var subpipeline = replay.CreateSubpipeline("Processing");
    var stream = tempConnector.CreateBridge<double>(subpipeline);
    // Use stream.Out for processing
}
else
{
    Console.WriteLine("Temperature stream not found");
}
```

## Best Practices

### 1. Dataset Validation

Always validate datasets before processing:

```csharp
using System.Linq;

if (!replay.LoadDatasetAndConnectors())
{
    Console.WriteLine("Dataset validation failed");
    return;
}

// Check sessions exist
if (replay.Dataset.Sessions.Count == 0)
{
    Console.WriteLine("No sessions found in dataset");
    return;
}

// Verify expected streams using connectors
var session = replay.Dataset.Sessions.First();
var requiredStreams = new[] { "RGB", "Depth", "Skeleton" };

if (!replay.Connectors.ContainsKey(session.Name))
{
    Console.WriteLine($"No connectors found for session: {session.Name}");
    return;
}

var sessionConnectors = replay.Connectors[session.Name];
var availableStreams = sessionConnectors.Keys.ToHashSet();

foreach (var required in requiredStreams)
{
    if (!availableStreams.Contains(required))
    {
        Console.WriteLine($"Required stream not found: {required}");
        Console.WriteLine($"Available streams: {string.Join(", ", availableStreams)}");
        return;
    }
}

Console.WriteLine("Dataset validation successful");
```

### 2. Progress Monitoring

Implement comprehensive progress reporting:

```csharp
public class DetailedProgressReporter : IProgress<double>
{
    private DateTime startTime;
    private double lastProgress;
    
    public DetailedProgressReporter()
    {
        startTime = DateTime.Now;
        lastProgress = 0;
    }
    
    public void Report(double value)
    {
        var elapsed = DateTime.Now - startTime;
        var rate = (value - lastProgress) / elapsed.TotalSeconds;
        lastProgress = value;
        
        Console.WriteLine($"Progress: {value:P}");
        Console.WriteLine($"  Elapsed: {elapsed:hh\\:mm\\:ss}");
        Console.WriteLine($"  Rate: {rate:F4}/sec");
        
        if (value > 0 && rate > 0)
        {
            var remaining = (1 - value) / rate;
            Console.WriteLine($"  Estimated remaining: {TimeSpan.FromSeconds(remaining):hh\\:mm\\:ss}");
        }
    }
}
```

### 3. Resource Management

Properly manage resources:

```csharp
using System;
using SAAC.PipelineServices;

ReplayPipeline replay = null;
Subpipeline processingPipeline = null;

try
{
    var config = new ReplayPipelineConfiguration
    {
        ReplayType = ReplayPipeline.ReplayType.FullSpeed,
        DatasetPath = @"D:\Recordings",
        DatasetName = "Data.pds",
        AutomaticPipelineRun = false
    };
    
    replay = new ReplayPipeline(config);
    
    if (replay.LoadDatasetAndConnectors())
    {
        // Create subpipeline using replay's method
        processingPipeline = replay.CreateSubpipeline("Processing");
        
        // Access streams through connectors
        var session = replay.Dataset.Sessions.First();
        var connectors = replay.Connectors[session.Name];
        
        if (connectors.TryGetValue("Temperature", out var connector))
        {
            var stream = connector.CreateBridge<double>(processingPipeline);
            
            // Process stream
            var processed = stream.Out.Select(v => v * 1.8 + 32);  // C to F
            
            // Save using replay's store management
            var outputSession = replay.GetOrCreateSession("ProcessedSession");
            replay.CreateStore(
                processingPipeline,
                outputSession,
                "TemperatureFahrenheit",
                "ProcessedData",
                processed
            );
        }
        
        // Run processing
        processingPipeline.RunAsync();
        replay.RunPipelineAndSubpipelines();
        replay.Pipeline.WaitAll();
    }
}
catch (AggregateException ae)
{
    foreach (var ex in ae.InnerExceptions)
    {
        Console.WriteLine($"Pipeline error: {ex.Message}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    // Dispose in reverse order of creation
    processingPipeline?.Dispose();
    replay?.Dispose();
}
```

### 4. Error Handling

Implement robust error handling:

```csharp
try
{
    replay.RunPipelineAndSubpipelines();
    replay.Pipeline.WaitAll();
}
catch (AggregateException ae)
{
    foreach (var ex in ae.InnerExceptions)
    {
        Console.WriteLine($"Pipeline error: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

### 5. Logging

Implement comprehensive logging:

```csharp
var config = new ReplayPipelineConfiguration
{
    ReplayType = ReplayPipeline.ReplayType.FullSpeed,
    DatasetPath = @"D:\Recordings",
    DatasetName = "Data.pds"
};

var replay = new ReplayPipeline(
    config, 
    "ReplayInstance",
    (message) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}")
);
```

## Performance Considerations

### Replay Mode Selection

Choose appropriate replay mode:

| Scenario | Recommended Mode | Reason |
|----------|------------------|--------|
| Batch processing | FullSpeed | Maximum throughput |
| Visualization | RealTime | Synchronized playback |
| Event analysis | IntervalFullSpeed | Targeted processing |
| Live demo | IntervalRealTime | Presentation friendly |

### I/O Optimization

Optimize disk access:

- Use SSD for source and destination
- Minimize network I/O during replay
- Batch write operations
- Use appropriate buffer sizes

## See Also

- [ServerApplication](ServerApplication.md) - Central coordination with RendezVous
- [DatasetPipeline](DatasetPipeline-Guide.md) - Base pipeline class
- [PsiStudio Modifications](PsiStudio-Modifications.md) - PsiStudio integration
- [Unity Integration](Unity-Integration.md) - Unity replay scenarios
- [Architecture Overview](Architecture.md) - SAAC framework design
- [Quick Start Guide](Quick-Start.md) - Getting started with SAAC
