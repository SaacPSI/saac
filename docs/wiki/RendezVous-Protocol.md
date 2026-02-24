# RendezVous Protocol

The RendezVous protocol is a distributed coordination system that enables SAAC applications to discover, connect, and communicate with each other across a network.

## Overview

The RendezVous protocol provides:

- **Service Discovery**: Applications announce their capabilities
- **Dynamic Connection**: Clients discover and connect to services
- **Lifecycle Management**: Start, stop, and monitor remote processes
- **Command & Control**: Send commands to remote applications
- **Stream Advertisement**: Publish available data streams

## Architecture

```
????????????????????????????????????????????????????????????????
?                    RendezVous Server                         ?
?                  (ServerApplication)                         ?
?                                                              ?
?  ??????????????????????????????????????????????????????    ?
?  ?           Process Registry                          ?    ?
?  ?  { Name, Endpoints, Version, Status, ... }        ?    ?
?  ??????????????????????????????????????????????????????    ?
?                                                              ?
?  ??????????????????????  ??????????????????????           ?
?  ?  TCP Command Port  ?  ?  RendezVous Port   ?           ?
?  ?   (e.g., 13331)    ?  ?   (e.g., 13330)    ?           ?
?  ??????????????????????  ??????????????????????           ?
???????????????????????????????????????????????????????????????
               ?                     ?
        ???????????????       ??????????????
        ?             ?       ?            ?
??????????????????  ???????????????????  ????????????????????
? VideoRemoteApp ?  ? CameraRemoteApp ?  ? WhisperRemoteApp ?
?                ?  ?                 ?  ?                  ?
? Registers:     ?  ? Registers:      ?  ? Registers:       ?
? - Process      ?  ? - Process       ?  ? - Process        ?
? - Endpoints    ?  ? - Endpoints     ?  ? - Endpoints      ?
??????????????????  ???????????????????  ????????????????????
```

## Core Concepts

### Process

A **Process** represents a running application instance:

```csharp
public class Process
{
    public string Name { get; set; }
    public string Version { get; set; }
    public List<Endpoint> Endpoints { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

**Properties:**
- `Name`: Unique identifier for the application
- `Version`: Application version string
- `Endpoints`: List of available data streams
- `Metadata`: Additional custom information

### Endpoint

An **Endpoint** represents a data stream available for connection:

```csharp
public class Endpoint
{
    public string Name { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public TransportKind Transport { get; set; }  // TCP or UDP
    public string StreamName { get; set; }
    public Type StreamType { get; set; }
}
```

**Example:**
```csharp
new Endpoint
{
    Name = "VideoStream",
    Host = "192.168.1.100",
    Port = 50000,
    Transport = TransportKind.Tcp,
    StreamName = "Video/FullScreen",
    StreamType = typeof(Shared<EncodedImage>)
}
```

### Commands

Commands are sent between applications for control:

```csharp
public enum Command
{
    Initialize,     // Configure remote application
    Run,           // Start execution
    Stop,          // Stop execution
    Close,         // Terminate application
    Status         // Query status
}
```

## Protocol Flow

### 1. Application Startup

```
Application                    RendezVous Server
    ?                                ?
    ?  1. Connect to server          ?
    ??????????????????????????????????
    ?                                ?
    ?  2. Register Process           ?
    ??????????????????????????????????
    ?     (Name, Version)            ?
    ?                                ?
    ?  3. Registration Confirmed     ?
    ??????????????????????????????????
    ?                                ?
    ?  4. Add Endpoints              ?
    ??????????????????????????????????
    ?     (Stream info, ports)       ?
    ?                                ?
```

**Code Example:**

```csharp
// Create RendezVous pipeline
var config = new RendezVousPipelineConfiguration
{
    RendezVousHost = "192.168.1.100",
    RendezVousPort = 13330,
    CommandPort = 13331
};

var pipeline = new RendezVousPipeline(
    config,
    applicationName: "VideoCapture1",
    rendezVousServerAddress: "192.168.1.100",
    log: LogMessage
);

// Create process description
var process = new Rendezvous.Process("VideoCapture1")
{
    Version = "1.0.0"
};

// Add endpoints
var endpoint = new Rendezvous.Endpoint
{
    Name = "VideoStream",
    Host = config.RendezVousHost,
    Port = 50000,
    Transport = TransportKind.Tcp
};
process.AddEndpoint(endpoint);

// Add to pipeline
pipeline.AddProcess(process);

// Start and connect
pipeline.Start();
```

### 2. Service Discovery

```
Client Application             RendezVous Server
    ?                                ?
    ?  1. Query Processes            ?
    ??????????????????????????????????
    ?                                ?
    ?  2. Return Process List        ?
    ??????????????????????????????????
    ?     (Names, Endpoints)         ?
    ?                                ?
    ?  3. Select and Connect         ?
    ?     to Endpoint                ?
    ??????????????????               ?
                     ?               ?
            ?????????????????        ?
            ? RemoteImporter ?        ?
            ?   (connects)   ?        ?
            ??????????????????        ?
```

**Code Example:**

```csharp
// Discover processes
var server = new Rendezvous.Server("192.168.1.100", 13330);
var processes = await server.GetProcessesAsync();

// Find specific process
var videoProcess = processes
    .FirstOrDefault(p => p.Name == "VideoCapture1");

if (videoProcess != null)
{
    // Connect to endpoint
    var endpoint = videoProcess.Endpoints
        .FirstOrDefault(e => e.Name == "VideoStream");
    
    if (endpoint != null)
    {
        var importer = new RemoteImporter(
            pipeline,
            endpoint.Host,
            endpoint.Port,
            endpoint.Transport
        );
        
        var stream = importer.Importer
            .OpenStream<Shared<EncodedImage>>(endpoint.StreamName);
    }
}
```

### 3. Command Execution

```
Controller                     Target Application
    ?                                ?
    ?  1. Send Command               ?
    ??????????????????????????????????
    ?     (Command, Args)            ?
    ?                                ?
    ?  2. Process Command            ?
    ?                     ????????????
    ?                     ? Execute  ?
    ?                     ????????????
    ?                                ?
    ?  3. Status Response (optional) ?
    ??????????????????????????????????
    ?                                ?
```

**Code Example:**

```csharp
// Send command from controller
pipeline.SendCommand(
    RendezVousPipeline.Command.Run,
    targetSource: "Server",  // Command destination
    args: "VideoCapture1"    // Target application
);

// Or broadcast to all
pipeline.SendCommand(
    RendezVousPipeline.Command.Stop,
    targetSource: "Server",
    args: "*"  // Wildcard for all applications
);
```

**Receive command in target application:**

```csharp
config.CommandDelegate = CommandReceived;

void CommandReceived(string source, Message<(Command, string)> message)
{
    var (command, args) = message.Data;
    var argArray = args.Split(';');
    
    // Check if command is for this application
    if (argArray[0] != applicationName && argArray[0] != "*")
        return;
    
    switch (command)
    {
        case Command.Run:
            Start();
            break;
            
        case Command.Stop:
            Stop();
            break;
            
        case Command.Status:
            SendStatus();
            break;
    }
}
```

### 4. Stream Data Flow

```
Producer Application          Consumer Application
    ?                                ?
    ?  Continuous Data Stream        ?
    ??????????????????????????????????
    ?  (TCP/UDP packets)             ?
    ?                                ?
    ?  ????????????????    ???????????????????
    ?  ?RemoteExporter?????? RemoteImporter  ?
    ?  ????????????????    ???????????????????
    ?                                ?
```

## Configuration

### RendezVousPipelineConfiguration

```csharp
public class RendezVousPipelineConfiguration : DatasetPipelineConfiguration
{
    // RendezVous server connection
    public string RendezVousHost { get; set; }
    public int RendezVousPort { get; set; } = 13330;
    
    // Command channel
    public int CommandPort { get; set; } = 13331;
    public CommandDelegate CommandDelegate { get; set; }
    
    // Clock synchronization (optional)
    public int ClockPort { get; set; } = 0;  // 0 = disabled
    
    // Pipeline behavior
    public bool AutomaticPipelineRun { get; set; } = true;
    
    // Inherited from DatasetPipelineConfiguration
    public string DatasetPath { get; set; }
    public string DatasetName { get; set; }
    public bool RecordIncomingProcess { get; set; }
}
```

### Application Configuration

**VideoRemoteApp Example:**

```csharp
// Network Tab Settings
RendezVousServerIp = "192.168.1.100"
RendezVousPort = 13330
CommandPort = 13331
CommandSource = "Server"
IpToUse = "192.168.1.101"  // This application's IP
ApplicationName = "VideoCapture1"
StreamingPortRangeStart = 50000
```

## Network Ports

### Standard Port Assignments

| Port Range | Purpose | Transport |
|------------|---------|-----------|
| 13330 | RendezVous Server | TCP |
| 13331 | Command Channel | TCP |
| 13332 | Clock Sync (optional) | UDP |
| 50000+ | Data Streams | TCP/UDP |

**Port Allocation Example:**

```
ServerApplication:
  - 13330 (RendezVous)
  - 13331 (Commands)

VideoRemoteApp:
  - 50000 (Video Stream 1)
  - 50001 (Video Stream 2)
  - ...

CameraRemoteApp:
  - 51000 (Camera 1)
  - 51001 (Camera 2)
  - ...

WhisperRemoteApp:
  - 52000 (Transcription)
  - 52001 (Audio)
```

## Data Serialization

### Stream Types

Common stream types in RendezVous:

```csharp
// Video
Shared<EncodedImage>          // Encoded (JPEG, PNG, etc.)
Shared<Image>                 // Raw image data

// Audio
AudioBuffer                   // Raw audio samples

// Body Tracking
List<Body>                    // Skeleton data
List<Face>                    // Facial landmarks

// Text
string                        // Transcriptions, annotations
```

### Encoding Strategies

**Image Compression:**
```csharp
// JPEG encoding for video
producer
    .EncodeJpeg(quality: 90, DeliveryPolicy.LatestMessage)
    .PipeTo(remoteExporter);
```

**Audio Compression:**
```csharp
// Wave encoding for audio
audioProducer
    .Encode(AudioCompressionMode.Wave)
    .PipeTo(remoteExporter);
```

## Performance Tuning

### Network Bandwidth

**Reduce Bandwidth:**
- Lower video resolution
- Reduce JPEG quality
- Increase capture interval
- Use UDP for real-time data

**Example:**
```csharp
// High quality (more bandwidth)
Interval = TimeSpan.FromMilliseconds(33)  // 30 fps
EncodingLevel = 95

// Low bandwidth
Interval = TimeSpan.FromMilliseconds(100)  // 10 fps
EncodingLevel = 75
```

### Latency Optimization

**UDP for Real-Time:**
```csharp
var exporter = new RemoteExporter(
    pipeline,
    port: 50000,
    TransportKind.Udp  // Lower latency, possible packet loss
);
```

**TCP for Reliability:**
```csharp
var exporter = new RemoteExporter(
    pipeline,
    port: 50000,
    TransportKind.Tcp  // Higher latency, guaranteed delivery
);
```

### Delivery Policies

Control message queueing behavior:

```csharp
// Latest message only (real-time)
producer.PipeTo(consumer, DeliveryPolicy.LatestMessage);

// All messages (archival)
producer.PipeTo(consumer, DeliveryPolicy.Unlimited);

// Throttle messages
producer.PipeTo(consumer, DeliveryPolicy.Throttle);
```

## Security Considerations

### Network Security

?? **The RendezVous protocol does not include built-in encryption or authentication.**

**Recommendations:**
- Deploy on trusted networks only
- Use VPN for remote access
- Implement firewall rules
- Consider adding TLS wrapper for sensitive data

### Access Control

No built-in authentication. Consider:
- IP whitelisting at firewall level
- Network segmentation
- Application-level authentication (custom implementation)

## Troubleshooting

### Connection Issues

**Problem:** Cannot connect to RendezVous server

**Check:**
1. Server is running
2. Network connectivity (`ping`)
3. Firewall rules allow ports
4. Correct IP address and port
5. Server logs for errors

**Debug:**
```csharp
// Enable debug logging
config.Debug = true;

// Check server status
var server = new Rendezvous.Server(serverIp, port);
bool isConnected = await server.TryConnectAsync();
```

### Discovery Issues

**Problem:** Process not appearing in registry

**Check:**
1. Process successfully registered
2. Endpoints added before query
3. Application name is unique
4. No network issues during registration

**Debug:**
```csharp
// Manually query processes
var processes = await server.GetProcessesAsync();
foreach (var proc in processes)
{
    Console.WriteLine($"Process: {proc.Name}");
    foreach (var ep in proc.Endpoints)
    {
        Console.WriteLine($"  Endpoint: {ep.Name} at {ep.Host}:{ep.Port}");
    }
}
```

### Stream Connection Issues

**Problem:** RemoteImporter not receiving data

**Check:**
1. Endpoint port is correct
2. RemoteExporter is writing to correct stream name
3. Importer is reading correct stream name (case-sensitive)
4. Network path between hosts
5. Delivery policy compatibility

**Debug:**
```csharp
// Log stream activity
stream.Do((data, env) => {
    Console.WriteLine($"Received at {env.OriginatingTime}");
});
```

### Command Not Received

**Problem:** Commands sent but not processed

**Check:**
1. Command port matches between sender and receiver
2. CommandDelegate is set in configuration
3. Application name matches or uses wildcard
4. Command source matches expected source

**Debug:**
```csharp
void CommandReceived(string source, Message<(Command, string)> message)
{
    // Log all received commands
    Console.WriteLine($"Command: {message.Data.Item1}");
    Console.WriteLine($"Source: {source}");
    Console.WriteLine($"Args: {message.Data.Item2}");
    
    // Process as normal
}
```

## Advanced Topics

### Clock Synchronization

Synchronize time across distributed nodes:

```csharp
config.ClockPort = 13332;  // Enable clock sync

// Applications will synchronize their clocks
// Improves temporal alignment of streams
```

### Custom Metadata

Add custom information to process registry:

```csharp
var process = new Rendezvous.Process("MyApp")
{
    Metadata = new Dictionary<string, object>
    {
        ["Location"] = "Lab A",
        ["Capabilities"] = new[] { "Video", "Audio" },
        ["Status"] = "Ready"
    }
};
```

### Dynamic Endpoint Management

Add/remove endpoints at runtime:

```csharp
// Add endpoint during execution
var newEndpoint = CreateEndpoint("NewStream", 50005);
pipeline.AddEndpoint(newEndpoint);

// Query updated process info
var updatedProcess = await server.GetProcessAsync("MyApp");
```

### Multi-Server Deployment

Deploy multiple RendezVous servers for different subsystems:

```
Server A (Video subsystem)
  ?? VideoCapture1
  ?? VideoCapture2
  ?? VideoCapture3

Server B (Audio subsystem)
  ?? AudioCapture1
  ?? AudioCapture2

Server C (Coordination)
  ?? Connects to Server A
  ?? Connects to Server B
  ?? Orchestrates workflow
```

## Best Practices

### Naming Conventions

- **Process Names:** Descriptive and unique (e.g., `VideoCapture_RoomA_Station1`)
- **Endpoint Names:** Function-based (e.g., `Video/FrontCamera`, `Audio/Microphone1`)
- **Stream Names:** Hierarchical paths (e.g., `Video/Cameras/Camera1/Color`)

### Error Handling

```csharp
try
{
    pipeline.Start();
}
catch (RendezVousConnectionException ex)
{
    Log($"Failed to connect to RendezVous server: {ex.Message}");
    // Fall back to local-only mode
}
```

### Resource Cleanup

```csharp
// Proper disposal
try
{
    pipeline.RunAsync();
    // ... application logic ...
}
finally
{
    pipeline?.Dispose();
    // Automatically unregisters from RendezVous
}
```

### Configuration Management

- Store network settings in user settings
- Validate configuration before starting
- Provide UI for configuration editing
- Support configuration import/export

## Examples

### Complete VideoRemoteApp Setup

See [VideoRemoteApp Documentation](VideoRemoteApp.md) for a complete working example.

### Simple Producer-Consumer

Producer:
```csharp
var config = new RendezVousPipelineConfiguration
{
    RendezVousHost = "192.168.1.100",
    RendezVousPort = 13330
};

var pipeline = new RendezVousPipeline(config, "Producer", "192.168.1.100");

var generator = Generators.Sequence(pipeline, 0, i => i + 1, TimeSpan.FromSeconds(1));

var exporter = new RemoteExporter(pipeline, 50000, TransportKind.Tcp);
exporter.Exporter.Write(generator, "Counter");

var process = new Rendezvous.Process("Producer");
process.AddEndpoint(exporter.ToRendezvousEndpoint(config.RendezVousHost));
pipeline.AddProcess(process);

pipeline.RunAsync();
```

Consumer:
```csharp
var server = new Rendezvous.Server("192.168.1.100", 13330);
var processes = await server.GetProcessesAsync();
var producer = processes.FirstOrDefault(p => p.Name == "Producer");

using var pipeline = Pipeline.Create("Consumer");

var endpoint = producer.Endpoints.First();
var importer = new RemoteImporter(pipeline, endpoint.Host, endpoint.Port);
var stream = importer.Importer.OpenStream<int>("Counter");

stream.Do(i => Console.WriteLine($"Received: {i}"));

await pipeline.RunAsync();
```

## See Also

- [Architecture Overview](Architecture.md)
- [ServerApplication](ServerApplication.md)
- [PipelineServices](PipelineServices.md)
- [VideoRemoteApp](VideoRemoteApp.md)
