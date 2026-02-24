# SAAC Components Overview

This page provides an overview of all components available in the SAAC framework. Each component extends the Microsoft Platform for Situated Intelligence (\\psi) with specialized functionality for multimodal data capture, processing, and analysis.

## Component Categories

### 📊 Data Capture & Sensors
- [AudioRecording](AudioRecording-Component.md) - Audio capture from microphones, files, and datasets
- [Biopac](Biopac-Component.md) - Biopac hardware interface for physiological signals
- [LabJack](LabJack-Component.md) - LabJack data acquisition hardware integration
- [LabStreamLayer](LabStreamLayer-Component.md) - Lab Streaming Layer (LSL) protocol integration
- [Optitrack](Optitrack-Component.md) - Optitrack motion capture system integration
- [Skinectic](Skinectic-Component.md) - Skinectic sensor integration
- [TeslaSuit](TeslaSuit-Component.md) - TeslaSuit haptic suit integration

### 👤 Body Tracking & Analysis
- [Bodies](Bodies-Component.md) - Body tracking and skeleton processing
- [BodiesRemoteServices](BodiesRemoteServices-Component.md) - Remote body tracking services
- [KinectAzureRemoteServices](KinectAzureRemoteServices-Component.md) - Azure Kinect DK remote streaming
- [KinectRemoteServices](KinectRemoteServices-Component.md) - Kinect v2 remote streaming
- [NuitrackRemoteServices](NuitrackRemoteServices-Component.md) - Nuitrack SDK remote streaming
- [OpenFace](OpenFace-Component.md) - Facial analysis and recognition

### 🎭 Gesture & Interaction
- [Gestures](Gestures-Component.md) - Gesture recognition and classification
- [AttentionMeasures](AttentionMeasures-Component.md) - Attention and gaze analysis

### 👥 Social & Group Analysis
- [Groups](Groups-Component.md) - Group dynamics and flock behavior detection

### 🗣️ Speech & Audio Processing
- [Whisper](Whisper-Component.md) - OpenAI Whisper speech-to-text
- [WhisperRemoteServices](WhisperRemoteServices-Component.md) - Remote Whisper transcription
- [OpenSmile](OpenSmile-Component.md) - Audio feature extraction

### 🤖 AI & Machine Learning
- [Ollama](Ollama-Component.md) - Local LLM integration

### 🎮 Virtual Reality & Game Engines
- [Unity](Unity-Component.md) - Unity engine integration
- [UnrealRemoteConnector](UnrealRemoteConnector-Component.md) - Unreal Engine integration
- [WebRTC](WebRTC-Component.md) - WebRTC streaming for Unity/Unreal

### 📝 Annotations & Visualization
- [AnnotationsComponents](AnnotationsComponents-Component.md) - Web-based annotation interface
- [Visualizations](Visualizations-Component.md) - PsiStudio visualization objects

### 🔧 Utilities & Services
- [PipelineServices](PipelineServices-Component.md) - Pipeline management (RendezVous, Replay, Dataset)
- [GlobalHelpers](GlobalHelpers-Component.md) - Global helper utilities
- [Helpers](Helpers-Component.md) - General helper components
- [PsiFormats](PsiFormats-Component.md) - Custom \\psi serialization formats
- [InteropExtension](InteropExtension-Component.md) - Interop extensions (WebSockets, TCP)
- [PLUME](PLUME-Component.md) - PLUME file format integration

### 🔌 Remote Connectors
- [KinectAzureRemoteConnector](KinectAzureRemoteConnector-Component.md) - Azure Kinect client connector
- [NuitrackRemoteConnector](NuitrackRemoteConnector-Component.md) - Nuitrack client connector
- [WhisperRemoteConnector](WhisperRemoteConnector-Component.md) - Whisper client connector

### 🎬 Replay & Studio
- [PsiStudioReplayExtension](PsiStudioReplayExtension-Component.md) - PsiStudio replay extensions

## Getting Started

### Basic Component Usage

```csharp
using Microsoft.Psi;
using SAAC.ComponentName;

// Create pipeline
var pipeline = Pipeline.Create();

// Create component with configuration
var config = new ComponentConfiguration
{
    // Set configuration options
};

var component = new ComponentClass(pipeline, config);

// Connect to other components
component.Out.PipeTo(otherComponent.In);

// Run pipeline
pipeline.RunAsync();
```

### Integration with RendezVous

Many components support remote streaming via RendezVous protocol:

```csharp
using SAAC.PipelineServices;

// Configure RendezVous pipeline
var rdvConfig = new RendezVousPipelineConfiguration
{
    RendezVousHost = "localhost",
    DatasetPath = @"D:\Data",
    DatasetName = "Experiment.pds"
};

var rdvPipeline = new RendezVousPipeline(rdvConfig, "Server");

// Components with RemoteServices automatically stream data
// See component-specific documentation for details

rdvPipeline.Start();
```

## Component Dependencies

### Core Dependencies

All components depend on:
- Microsoft.Psi (Platform for Situated Intelligence)
- .NET Framework 4.8 or .NET Standard 2.0

### Hardware Dependencies

Some components require specific hardware:
- **Biopac**: Biopac MP150/160 hardware
- **LabJack**: LabJack U3/U6/T4/T7 devices
- **Optitrack**: Optitrack motion capture system
- **Azure Kinect**: Azure Kinect DK sensor
- **Kinect v2**: Kinect v2 sensor + USB 3.0 adapter
- **TeslaSuit**: TeslaSuit haptic suit

### Software Dependencies

Some components require additional software:
- **OpenFace**: OpenFace SDK
- **Nuitrack**: Nuitrack SDK + license
- **Whisper**: Whisper.net package
- **Ollama**: Ollama local LLM server
- **FFmpeg**: Required for WebRTC video encoding

## Development Guidelines

### Creating a New Component

1. **Inherit from appropriate base class**:
   - `IConsumerProducer<TIn, TOut>` for transformation components
   - `ISourceComponent` for source components
   - `IConsumer<T>` for sink components

2. **Follow naming conventions**:
   - Component class: `ComponentName.cs`
   - Configuration class: `ComponentNameConfiguration.cs`
   - Remote service: `ComponentNameRemoteService.cs`

3. **Implement proper disposal**:
   ```csharp
   public void Dispose()
   {
       // Cleanup resources
       source?.Dispose();
   }
   ```

4. **Add configuration validation**:
   ```csharp
   public ComponentNameConfiguration()
   {
       // Set defaults
       // Validate settings
   }
   ```

5. **Document your component**:
   - Create wiki page in `docs/wiki/ComponentName-Component.md`
   - Include usage examples
   - Document configuration options
   - List dependencies

### Testing Components

```csharp
[TestClass]
public class ComponentNameTests
{
    [TestMethod]
    public void TestBasicFunctionality()
    {
        using (var pipeline = Pipeline.Create())
        {
            var component = new ComponentName(pipeline);
            // Test logic
            pipeline.Run();
        }
    }
}
```

## Common Patterns

### Configuration Pattern

```csharp
public class MyComponentConfiguration
{
    public string Parameter1 { get; set; } = "default";
    public int Parameter2 { get; set; } = 42;
    
    public void Validate()
    {
        if (string.IsNullOrEmpty(Parameter1))
            throw new ArgumentException(nameof(Parameter1));
    }
}

public class MyComponent : ISourceComponent
{
    private MyComponentConfiguration config;
    
    public MyComponent(Pipeline pipeline, MyComponentConfiguration config = null)
    {
        this.config = config ?? new MyComponentConfiguration();
        this.config.Validate();
    }
}
```

### Remote Service Pattern

```csharp
public class MyComponentRemoteService
{
    private Pipeline pipeline;
    private RemoteExporter exporter;
    
    public MyComponentRemoteService(
        Pipeline pipeline, 
        int port, 
        string host = "localhost")
    {
        this.pipeline = pipeline;
        this.exporter = new RemoteExporter(pipeline, port);
        
        // Create and export streams
        var component = new MyComponent(pipeline);
        exporter.Exporter.Write(component.Out, "StreamName");
    }
}
```

### Event-Driven Pattern

```csharp
public class EventDrivenComponent
{
    public event EventHandler<DataEventArgs> DataReceived;
    
    public void ProcessData(TData data, Envelope envelope)
    {
        // Process data
        
        // Raise event
        DataReceived?.Invoke(this, new DataEventArgs 
        { 
            Data = processed, 
            Time = envelope.OriginatingTime 
        });
    }
}
```

## Troubleshooting

### Common Issues

1. **Component not found**:
   - Ensure NuGet packages are restored
   - Check project references
   - Verify assembly is in output directory

2. **Configuration errors**:
   - Validate configuration before use
   - Check for null values
   - Verify paths exist

3. **Pipeline errors**:
   - Check component connections
   - Ensure proper disposal
   - Monitor memory usage

4. **Performance issues**:
   - Reduce processing frequency
   - Use appropriate buffer sizes
   - Profile with PsiStudio diagnostics
