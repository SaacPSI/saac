# ServerApplication

## Summary

ServerApplication is a central server application for coordinating multiple ψ (Psi) applications in the SAAC framework. It provides a WPF-based graphical interface for managing network connections, monitoring connected devices/applications, recording data streams to datasets, and controlling remote applications through a command system.

This application implements the `IPsiStudioPipeline` interface, allowing it to integrate with PsiStudio for visualization and analysis of recorded data.

## Features

### Core Functionality
- **Network Coordination**: Uses `RendezVousPipeline` to coordinate multiple connected applications/devices over a network
- **Data Recording**: Records incoming data streams to ψ datasets for later analysis
- **Device Management**: Graphical interface to monitor and control connected devices/applications
- **Command System**: Sends commands (Run, Stop, Status, Close) to connected applications
- **Status Monitoring**: Real-time monitoring of connected applications with status indicators
- **PsiStudio Integration**: Implements `IPsiStudioPipeline` for seamless integration with PsiStudio

### Advanced Features
- **External Configuration**: Load topic formats and serializers from JSON configuration files
- **Annotations Support**: Optional WebSocket-based annotation system for data labeling
- **Flexible Storage**: Configurable store modes and session naming strategies
- **Automatic Pipeline Run**: Option to automatically start pipelines when applications connect

## Configuration

### Application Settings

The application stores configuration in `Properties/Settings.settings`. Key settings include:

#### Network Configuration
- **RendezVousHost**: Host address for RendezVous server (default: `localhost`)
- **RendezVousPort**: Port for RendezVous server (default: `13331`)
- **ClockPort**: Port for clock synchronization (default: `11510`)
- **CommandPort**: Port for command communication (default: `11610`)

#### Dataset Configuration
- **DatasetPath**: Path where datasets are stored (default: `./Stores/`)
- **DatasetName**: Name of the dataset file (default: `Dataset.pds`)
- **SessionName**: Name of the session (default: `RawData`)
- **StoreMode**: Storage mode (0 = Dictionary, etc.)
- **SessionMode**: Session naming mode (0 = default, etc.)

#### Pipeline Configuration
- **AutomaticPipelineRun**: Automatically start pipeline when applications connect (default: `True`)
- **Debug**: Enable debug logging (default: `True`)

#### Annotation Configuration
- **IsAnnotationEnabled**: Enable annotation system (default: `False`)
- **AnnotationSchemasPath**: Path to annotation schema directory
- **AnnotationHtmlPage**: Path to annotation HTML page
- **AnnotationPort**: Port for annotation WebSocket server (default: `8080`)

#### External Configuration
- **ExternalConfigurationDirectory**: Directory containing JSON configuration files for topic formats

### Configuration UI

The application provides a tabbed interface for configuration:
- **Configuration Tab**: Network, dataset, and pipeline settings
- **Annotation Tab**: Annotation system configuration
- **Overview Tab**: Connected devices and status monitoring

Settings can be saved and loaded through the UI, and are persisted between sessions.

## Usage

### Starting the Server

1. Launch the application
2. Configure settings in the Configuration tab (or use defaults)
3. Click "Start" to initialize the RendezVousPipeline server
4. The server will start listening for connections on the configured ports

### Managing Connected Applications

Once started, the server will:
- Display connected applications/devices in the "Connected devices" panel
- Show status indicators (colored dots) for each device:
  - **Orange**: Waiting/Initializing
  - **Green**: Running
  - **Red**: Error/Stopped
- Allow individual control of each device with Start/Stop buttons
- Provide "Start All" and "Stop All" buttons for bulk operations

### Status Monitoring

The server automatically monitors connected applications:
- Sends status requests every second
- Updates device status indicators in real-time
- Detects timeouts (devices not responding for >3 seconds) and marks them as errors

## External JSON Configuration

ServerApplication supports loading topic formats and serializers from external JSON configuration files. This allows you to define custom data types and their serialization formats without modifying the application code.

### Configuration File Structure

Place JSON configuration files in the directory specified by `ExternalConfigurationDirectory`. Each JSON file should contain an array of topic format definitions:

```json
[
    {
        "topic": "1-Head",
        "type": "System.Tuple`2[[System.Numerics.Vector3, System.Numerics.Vectors],[System.Numerics.Vector3, System.Numerics.Vectors]]",
        "classFormat": "PsiFormatTupleOfVector",
        "streamToStore": "Heads"
    },
    {
        "topic": "TaskEvent",
        "type": "System.String",
        "classFormat": "PsiFormatString",
        "streamToStore": "Task"
    }
]
```

### Configuration File Fields

- **topic**: The topic name (stream identifier)
- **type**: Full .NET type name for the message type
- **classFormat**: Class name of the IPsiFormat implementation for serialization
- **streamToStore**: Name of the store where this stream will be recorded

### Assembly Loading

For each JSON configuration file, the application automatically looks for a corresponding assembly (DLL) that contains the format classes. The DLL must be located in a subfolder named after the JSON file (without the `.json` extension).

**Naming Convention:**
- If your JSON file is named `myTopics.json`, the DLL should be named `myTopics.dll`
- The DLL must be in a subfolder also named `myTopics` (matching the JSON filename without extension)

**Example Directory Structure:**

```
ExternalConfigurationDirectory/
  ├── myTopics.json          ← Your JSON config file
  └── myTopics/              ← Subfolder (same name as JSON, without .json)
      └── myTopics.dll       ← DLL file (same name as JSON, but .dll extension)
```

**Another Example:**

```
C:\MyConfigs\                    ← ExternalConfigurationDirectory
  ├── sensors.json
  ├── sensors/
  │   └── sensors.dll
  ├── customTypes.json
  └── customTypes/
      └── customTypes.dll
```

**What the DLL Should Contain:**
- The format class (implementing `IPsiFormat`) specified in the `classFormat` field of your JSON
- Any custom types referenced in the `type` field (if they're not already in the .NET framework or loaded assemblies)

**How It Works:**
1. The application scans `ExternalConfigurationDirectory` for all `*.json` files
2. For each JSON file (e.g., `myTopics.json`), it looks for a DLL at: `{ExternalConfigurationDirectory}/myTopics/myTopics.dll`
3. The DLL is loaded and searched for the format classes specified in the JSON

### Example Configuration

See `Examples/test.json` for a complete example configuration file.

## Integration with Other SAAC Applications

ServerApplication is designed to work with other SAAC applications that use the RendezVous system:

- **CameraRemoteApp**: Camera streaming applications
- **WhisperRemoteApp**: Speech-to-text applications
- **VideoRemoteApp**: Desktop/application streaming
- **Unity Applications**: Using the Unity Psi integration

These applications connect to the server via the RendezVous protocol and can be controlled through the server's command system.

## PsiStudio Integration

ServerApplication implements the `IPsiStudioPipeline` interface, allowing it to:

- Load datasets recorded by the server
- Replay sessions in PsiStudio
- Visualize recorded streams
- Analyze temporal data

To use with PsiStudio:
1. Record data using ServerApplication
2. Open the dataset in PsiStudio
3. The server application will appear as a pipeline plugin

## Annotations

The annotation system allows real-time labeling of data streams through a WebSocket interface:

1. Enable annotations in the Annotation tab
2. Configure the schema directory and HTML page
3. Set the annotation port
4. The server will start an HTTP server serving the annotation interface
5. Connect annotation clients via WebSocket to label streams

When annotations are enabled, the server creates an `HTTPAnnotationsComponent` that:
- Serves the annotation web interface
- Manages WebSocket connections for annotation clients
- Integrates annotations with the recorded dataset

## Commands

The server can send commands to connected applications:

- **Status**: Request status update from application
- **Run**: Start/run the application pipeline
- **Stop**: Stop the application pipeline
- **Close**: Close/disconnect the application
- **Reset**: Reset the application state

Commands can be sent to:
- Individual applications (by name)
- All applications (using `"*"` as the target)

## Related Documentation

- [PipelineServices](../Components/PipelineServices/PipelineServices.md): Documentation for `RendezVousPipeline` and related components
- [Applications README](../README.md): Overview of other SAAC applications
- [SAAC Main README](../../README.md): Project overview and installation instructions

## Technical Details

### Dependencies

- Microsoft.Psi framework (from [SaacPSI/psi fork](https://github.com/SaacPSI/psi), PsiStudio branch)
- WPF for the user interface
- Newtonsoft.Json for JSON configuration parsing

### Ports Used

- **RendezVousPort**: 13331 (default) - RendezVous server
- **CommandPort**: 11610 (default) - Command communication
- **ClockPort**: 11621 (default) - Clock synchronization
- **AnnotationPort**: 8080 (default) - Annotation WebSocket server

### Data Flow

1. Remote applications connect via RendezVous
2. Server creates connectors for incoming streams
3. Streams are recorded to the dataset (if configured)
4. Commands can be sent to control remote applications
5. Status updates are received and displayed in the UI

## Future Works

Potential improvements and features:
- NamedPipes protocol support
- Enhanced error handling and recovery
- More granular control over stream recording
- Improved annotation workflow
- Configuration templates and presets

