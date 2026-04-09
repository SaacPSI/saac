# AnnotationsComponents

## Overview

**AnnotationsComponents** provides HTTP/WebSocket-based annotation components for integrating web-based annotation interfaces with \\psi pipelines. It allows users to annotate data streams in real-time through a browser interface while maintaining perfect synchronization with the pipeline timeline.

**Key Features**:
- ? Web-based annotation interface via HTTP/WebSocket
- ? Real-time annotation synchronized with \\psi pipeline
- ? Compatible with PsiStudio annotation schemas
- ? Multiple client support (collaborative annotation)
- ? Time-interval based annotations
- ? JSON schema validation
- ? Custom HTML interface support

## Architecture

```
???????????????????????????????????????????????????????????
?              AnnotationsComponents                      ?
???????????????????????????????????????????????????????????
?                                                         ?
?  ????????????????????????????????????????????????????  ?
?  ?     HTTPAnnotationsComponent                     ?  ?
?  ?  • WebSocket server                              ?  ?
?  ?  • HTTP server for HTML interface                ?  ?
?  ?  • Client connection management                  ?  ?
?  ????????????????????????????????????????????????????  ?
?                   ?                                    ?
?  ????????????????????????????????????????????????????  ?
?  ?     AnnotationProcessor                          ?  ?
?  ?  • Parse annotation messages                     ?  ?
?  ?  • Convert to TimeIntervalAnnotationSet          ?  ?
?  ?  • Validate against schema                       ?  ?
?  ????????????????????????????????????????????????????  ?
?                   ?                                    ?
?                   ?                                    ?
?         \\psi Annotation Stream                         ?
?   (TimeIntervalAnnotationSet<T>)                       ?
?                                                         ?
???????????????????????????????????????????????????????????
        ?
        ? WebSocket
        ?
???????????????????????????????????????
?      Web Browser Clients            ?
?  • annotation.html interface        ?
?  • Real-time annotation UI          ?
?  • Multiple annotators              ?
???????????????????????????????????????
```

## Components

### 1. HTTPAnnotationsComponent

Main component handling WebSocket connections and serving HTML annotation pages.

**Inherits From**: `WebSocketsManager`

**Responsibilities**:
- Start HTTP server for serving annotation interface
- Manage WebSocket connections from browser clients
- Route annotation messages to `AnnotationProcessor`
- Handle multiple simultaneous annotators

**Configuration**:
```csharp
public class HTTPAnnotationsComponent
{
    public HTTPAnnotationsComponent(
        Pipeline pipeline,
        List<string> addresses,
        string schemaPath,
        string htmlPath);
}
```

**Parameters**:
- `pipeline`: Parent \\psi pipeline
- `addresses`: List of URLs for WebSocket and HTTP servers
  - Example: `["http://localhost:8080/ws/", "http://localhost:8080/"]`
- `schemaPath`: Path to annotation schema JSON file
- `htmlPath`: Path to HTML annotation interface

### 2. AnnotationProcessor

Processes annotation messages received from WebSocket connections.

**Responsibilities**:
- Parse JSON annotation messages
- Convert to `TimeIntervalAnnotationSet` objects
- Validate annotations against schema
- Output annotations to \\psi stream

**Output Stream**:
```csharp
IProducer<TimeIntervalAnnotationSet<T>> Out { get; }
```

## Annotation Files

### annotation.html

HTML/JavaScript interface for the annotation web application.

**Features**:
- Timeline visualization
- Annotation track creation
- Real-time annotation editing
- Keyboard shortcuts
- Multi-track support
- Export functionality

**Location**: `Components/AnnotationsComponents/AnnotationFiles/annotation.html`

### annotation.schema.json

Schema definition file for annotation validation, compatible with [PsiStudio Time-Interval Annotations](https://github.com/microsoft/psi/wiki/Time-Interval-Annotations).

**Schema Structure**:
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "name": { "type": "string" },
    "tracks": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "name": { "type": "string" },
          "values": {
            "type": "array",
            "items": { "type": "string" }
          }
        }
      }
    }
  }
}
```

**Location**: `Components/AnnotationsComponents/AnnotationFiles/annotation.schema.json`

## Basic Usage

### Example 1: Simple Annotation System

```csharp
using Microsoft.Psi;
using SAAC.PipelineServices;
using SAAC.AnnotationsComponents;

// Create RendezVous pipeline
var rdvConfig = new RendezVousPipelineConfiguration
{
    DatasetPath = @"D:\Experiments",
    DatasetName = "Study2024.pds",
    RendezVousHost = "localhost"
};

var rdvPipeline = new RendezVousPipeline(rdvConfig, "Server");

// Create annotation session
rdvPipeline.CreateOrGetSession("AnnotationSession");

// Configure annotation component
List<string> addresses = new List<string>
{
    "http://localhost:8080/ws/",   // WebSocket endpoint
    "http://localhost:8080/"        // HTTP endpoint
};

string schemaPath = @"C:\Users\User\Documents\PsiStudio\AnnotationSchemas\annotation.schema.json";
string htmlPath = @"D:\saac\Components\AnnotationsComponents\AnnotationFiles\annotation.html";

var annotationManager = new HTTPAnnotationsComponent(
    rdvPipeline,
    addresses,
    schemaPath,
    htmlPath
);

// Start annotation server
rdvPipeline.Start();
annotationManager.Start((error) => 
{
    if (error != null)
        Console.WriteLine($"Annotation error: {error}");
});

Console.WriteLine("Annotation interface available at: http://localhost:8080/");
Console.WriteLine("Press Enter to stop...");
Console.ReadLine();

// Stop annotation component
annotationManager.Stop();
rdvPipeline.Dispose();
```

### Example 2: Annotation with Data Streams

```csharp
using Microsoft.Psi;
using SAAC.AnnotationsComponents;
using SAAC.PipelineServices;

var rdvPipeline = new RendezVousPipeline(config, "AnnotationServer");

// Setup annotation component
var annotationComponent = new HTTPAnnotationsComponent(
    rdvPipeline,
    new List<string> { "http://localhost:8080/ws/", "http://localhost:8080/" },
    schemaPath,
    htmlPath
);

// Connect other data sources
// (e.g., video, audio, sensors)

// Process annotations
annotationComponent.Out.Do((annotations, envelope) =>
{
    Console.WriteLine($"New annotation at {envelope.OriginatingTime}:");
    foreach (var track in annotations.Tracks)
    {
        Console.WriteLine($"  Track: {track.Name}");
        foreach (var interval in track.Intervals)
        {
            Console.WriteLine($"    {interval.StartTime} - {interval.EndTime}: {interval.Value}");
        }
    }
});

// Store annotations
var session = rdvPipeline.CreateOrGetSession("Study_Session_001");
rdvPipeline.CreateStore(
    rdvPipeline.Pipeline,
    session,
    "Annotations",
    "AnnotationData",
    annotationComponent.Out
);

rdvPipeline.Start();
annotationComponent.Start((e) => { });

// Run pipeline
Console.WriteLine("Annotating at http://localhost:8080/");
Console.ReadLine();

annotationComponent.Stop();
rdvPipeline.Dispose();
```

### Example 3: Multi-Annotator Setup

```csharp
using SAAC.AnnotationsComponents;
using System.Collections.Generic;

// Configure for multiple annotators
var addresses = new List<string>
{
    "http://0.0.0.0:8080/ws/",    // Listen on all interfaces
    "http://0.0.0.0:8080/"
};

var annotationManager = new HTTPAnnotationsComponent(
    rdvPipeline,
    addresses,
    schemaPath,
    htmlPath
);

// Track connected clients
int clientCount = 0;
annotationManager.OnNewWebSocketConnectedHandler += (sender, e) =>
{
    clientCount++;
    Console.WriteLine($"Annotator {clientCount} connected: {e.Item1}:{e.Item2}");
};

annotationManager.Start((e) => { });

Console.WriteLine("Multi-annotator server running.");
Console.WriteLine("Annotators can connect from:");
Console.WriteLine("  - http://localhost:8080/");
Console.WriteLine("  - http://192.168.1.100:8080/ (from network)");

// Wait for annotations
Console.ReadLine();

annotationManager.Stop();
```

## Annotation Workflow

### Typical Annotation Process

```
1. Start HTTPAnnotationsComponent
   ?
2. Open http://localhost:8080/ in browser
   ?
3. Load annotation schema
   ?
4. Create annotation tracks
   ?
5. Play/pause data stream
   ?
6. Click timeline to create annotations
   ?
7. Edit annotation values
   ?
8. Annotations sent via WebSocket to pipeline
   ?
9. AnnotationProcessor validates and converts
   ?
10. Annotations stored in \\psi dataset
```

### Annotation Interface Features

**Timeline Controls**:
- Play/Pause playback
- Seek to specific time
- Zoom in/out
- Pan timeline

**Track Management**:
- Add new annotation tracks
- Delete tracks
- Rename tracks
- Reorder tracks

**Annotation Creation**:
- Click to create annotation
- Drag to adjust start/end times
- Select from predefined values (from schema)
- Add custom labels
- Delete annotations

**Keyboard Shortcuts**:
- `Space`: Play/Pause
- `Left/Right Arrow`: Seek backward/forward
- `+/-`: Zoom in/out
- `N`: New annotation at current time
- `Delete`: Delete selected annotation

## Schema Configuration

### Creating Custom Annotation Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "Behavior Annotation Schema",
  "type": "object",
  "properties": {
    "name": {
      "type": "string",
      "description": "Behavior coding session"
    },
    "tracks": {
      "type": "array",
      "items": [
        {
          "name": "Activity",
          "values": [
            "Walking",
            "Sitting",
            "Standing",
            "Running",
            "Other"
          ]
        },
        {
          "name": "Emotion",
          "values": [
            "Happy",
            "Sad",
            "Angry",
            "Neutral",
            "Surprised"
          ]
        },
        {
          "name": "Social Interaction",
          "values": [
            "Solo",
            "Dyad",
            "Group",
            "None"
          ]
        }
      ]
    }
  },
  "required": ["name", "tracks"]
}
```

### Schema Best Practices

1. **Define Clear Track Names**: Use descriptive track names that match your coding scheme

2. **Enumerate Values**: Provide all possible annotation values in the schema

3. **Organize Tracks**: Group related annotations together

4. **Validation**: Schema ensures only valid annotations are created

5. **Documentation**: Include descriptions for each track and value

## Integration with PsiStudio

Annotations created with AnnotationsComponents are fully compatible with PsiStudio:

1. **View Annotations**:
   - Open dataset in PsiStudio
   - Navigate to annotation store
   - Visualize annotation tracks on timeline

2. **Export Annotations**:
   - Export to CSV for statistical analysis
   - Export to JSON for further processing

3. **Edit in PsiStudio**:
   - Continue annotation in PsiStudio
   - Merge with web-based annotations
   - Apply additional coding

## Advanced Features

### Custom HTML Interface

Create custom annotation interface:

```html
<!DOCTYPE html>
<html>
<head>
    <title>Custom Annotation Interface</title>
    <script>
        const ws = new WebSocket('ws://localhost:8080/ws/');
        
        ws.onopen = () => {
            console.log('Connected to annotation server');
        };
        
        ws.onmessage = (event) => {
            const data = JSON.parse(event.data);
            // Handle incoming data
        };
        
        function sendAnnotation(startTime, endTime, value) {
            const annotation = {
                track: 'Behavior',
                start: startTime,
                end: endTime,
                value: value,
                timestamp: Date.now()
            };
            ws.send(JSON.stringify(annotation));
        }
    </script>
</head>
<body>
    <!-- Custom annotation UI -->
</body>
</html>
```

### Programmatic Annotation

Create annotations programmatically:

```csharp
using Microsoft.Psi;
using Microsoft.Psi.Data.Annotations;

var annotationSet = new TimeIntervalAnnotationSet<string>("Behavior");

// Add track
var track = annotationSet.AddTrack("Activity");

// Add annotations
track.AddAnnotation(
    new TimeInterval(
        DateTime.Parse("2024-01-15 10:00:00"),
        DateTime.Parse("2024-01-15 10:02:30")
    ),
    "Walking"
);

track.AddAnnotation(
    new TimeInterval(
        DateTime.Parse("2024-01-15 10:02:30"),
        DateTime.Parse("2024-01-15 10:05:00")
    ),
    "Sitting"
);

// Post to pipeline
emitter.Post(annotationSet, pipeline.GetCurrentTime());
```

## Troubleshooting

### Server Won't Start

**Symptoms**:
- Exception when calling `Start()`
- "Address already in use" error

**Solutions**:
- Check if port 8080 is already in use
- Change port in addresses configuration
- Ensure Windows Firewall allows the port
- Run as administrator if needed

**Check port availability**:
```powershell
netstat -ano | findstr :8080
```

### Browser Can't Connect

**Symptoms**:
- "Connection refused" in browser
- WebSocket connection fails

**Solutions**:
- Verify HTTP server is running
- Check firewall rules
- Test with `http://localhost:8080/` first
- Ensure correct URL format (http:// not https://)

### Annotations Not Appearing

**Symptoms**:
- Annotations created in browser but not in pipeline
- No output from `AnnotationProcessor`

**Solutions**:
- Check WebSocket connection status
- Verify schema validation
- Monitor console for errors
- Ensure correct data format

### Schema Validation Errors

**Symptoms**:
- "Invalid annotation" errors
- Annotations rejected

**Solutions**:
- Validate schema JSON syntax
- Ensure track names match schema
- Check value enumerations
- Review annotation message format

## Performance Considerations

### Annotation Frequency

- **Low-frequency annotations**: < 1 annotation/second (typical)
- **Medium-frequency**: 1-10 annotations/second
- **High-frequency**: > 10 annotations/second (may impact performance)

### Multiple Annotators

- Supports 5-10 simultaneous annotators without performance impact
- For >10 annotators, consider load balancing

### Data Storage

- Annotations are lightweight (< 1 KB per annotation)
- Typical session: 100-1000 annotations = ~100 KB

## See Also

- [Components Overview](Components-Overview.md) - All SAAC components
- [PipelineServices Component](PipelineServices-Component.md) - Pipeline management
- [InteropExtension Component](InteropExtension-Component.md) - WebSocket implementation
- [PsiStudio Time-Interval Annotations](https://github.com/microsoft/psi/wiki/Time-Interval-Annotations) - Microsoft \\psi annotation documentation
- [Architecture Overview](Architecture.md) - SAAC framework architecture
