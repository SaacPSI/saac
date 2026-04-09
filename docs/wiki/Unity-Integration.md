# Psi in Unity - Integration Guide

## Overview

This document describes the modifications made to Microsoft Psi to enable full compatibility with Unity game engine, including support for Android platforms. The modifications address Unity's specific runtime limitations and enable seamless integration of Psi's streaming data capabilities within Unity applications.

**Modified Repository**: [SAAC Psi Fork](https://github.com/SaacPSI/psi) (UnityAndroid branch)

**Pull Request**: [microsoft/psi#333 - Compatibility with Unity & Android](https://github.com/microsoft/psi/pull/333)

### Key Enhancements

The Unity integration provides:

1. **Unity Runtime Compatibility** - Modifications to make Psi DLLs work within Unity's runtime environment
2. **Android Platform Support** - Additional changes for Android builds including handling of dynamic types
3. **Unity Components** - Ready-to-use Unity scripts for importing and exporting Psi streams
4. **RendezVous Integration** - Full integration with SAAC's distributed coordination system
5. **Multi-Client Streaming** - Enhanced RemoteExporter supporting multiple concurrent connections

### Supported Unity Versions

- Unity 2022.1.23f and newer recommended
- Android builds require Unity 2021.3 LTS or newer
- Tested on Windows and Android, not iOS platforms

## Architecture Overview

```
┌────────────────────────────────────────────────────────────────┐
│                    Unity Application                           │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │           PsiPipelineManager (Core)                      │  │
│  │  - Pipeline lifecycle management                         │  │
│  │  - RendezVous client integration                         │  │
│  │  - Serializer registration                               │  │
│  │  - Command handling                                      │  │
│  └────────────┬────────────────────────┬────────────────────┘  │
│               │                        │                       │
│  ┌────────────▼──────────┐  ┌─────────▼───────────┐            │
│  │  PsiExporter<T>       │  │  PsiImporter<T>     │            │
│  │  (Base Classes)       │  │  (Base Classes)     │            │
│  └────────┬──────────────┘  └──────────┬──────────┘            │
│           │                            │                       │
│  ┌────────▼──────────────────┐  ┌──────▼───────────────┐       │
│  │ Concrete Exporters        │  │ Concrete Importers   │       │
│  │ - Position                │  │ - Position           │       │
│  │ - Image                   │  │ - Commands           │       │
│  │ - Matrix4x4               │  │ - Custom data        │       │
│  └───────────────────────────┘  └──────────────────────┘       │
│                                                                │
└────────────────┬───────────────────────────────────────────────┘
                 │
          [Network Stream]
                 │
┌────────────────▼───────────────────────────────────────────────┐
│              External Psi Application                          │
│          (SAAC components, PsiStudio, etc.)                    │
└────────────────────────────────────────────────────────────────┘
```

## Core Modifications to Psi

### 1. TypeResolutionHelper Modification

**Location**: `Sources/Runtime/Microsoft.Psi/Common/TypeResolutionHelper.cs`

**Issue**: Unity's type resolution system differs from standard .NET, causing failures when loading types dynamically.

**Original Code**:
```csharp
type = Type.GetType(typeName, AssemblyResolver, null);
```

**Modified Code**:
```csharp
type = Type.GetType(typeName);
```

**Reason**: Unity cannot use custom assembly resolvers in the same way as .NET Framework. The simplified type loading works within Unity's managed environment.

**Impact**:
- ✅ Enables Psi DLLs to load correctly in Unity
- ✅ Type serialization works properly
- ⚠️ May require explicit type registration for complex scenarios

### 2. Android Dynamic Type Handling

**Affected Files**: Multiple files using `dynamic` keyword

**Issue**: Android's IL2CPP runtime does not support the `dynamic` keyword with generic types.

**Solution**: Replace all instances of `dynamic` with explicit generic type parameters.

**Example Modification**:

**Before**:
```csharp
public void Process(dynamic data)
{
    // Process dynamic data
}
```

**After**:
```csharp
public void Process<T>(T data)
{
    // Process typed data
}
```

**Affected Areas**:
- RecyclingPool (RecyclingPool.cs:24)
- Serialization code generation
- Type registration systems

### 3. Serialization Shortcut Generation

**Issue**: Unity's runtime cannot generate serialization code dynamically using `Emit.Reflection`.

**Solution**: Pre-register known serializers explicitly.

**Implementation**:
```csharp
protected void InitializeSerializer(KnownSerializers serializers)
{
    // Register primitive types
    serializers.Register<bool, BoolSerializer>();
    serializers.Register<char, CharSerializer>();
    serializers.Register<int, IntSerializer>();
    serializers.Register<float, FloatSerializer>();
    
    // Register Unity types
    serializers.Register<Vector3, Vector3Serializer>();
    serializers.Register<Quaternion, QuaternionSerializer>();
    serializers.Register<Matrix4x4, Matrix4x4Serializer>();
    
    // Register tuples
    serializers.Register<Tuple<Vector3, Vector3>, TupleOfVector3Serializer>();
    
    // Register custom types
    // Add more as needed...
}
```

**Location**: `PsiPipelineManager.cs` and `PsiSerializerReflexion.cs`

## Unity Package Contents

### Package Structure

```
PsiImporterExporter.unitypackage
├── Base/
│   ├── PsiPipelineManager.cs        # Core manager component
│   ├── PsiExporter.cs                # Base exporter class
│   ├── PsiImporter.cs                # Base importer class
│   ├── PsiSerializerReflexion.cs    # Known serializers
│   ├── TcpWriterMulti.cs            # Multi-client TCP writer
│   └── PipeToMessage.cs             # Helper utilities
│
├── Exporters/
│   ├── PsiExporterPosition.cs       # GameObject position
│   ├── PsiExporterPositionOrientation.cs  # Position + rotation
│   ├── PsiExporterMatrix4x4.cs      # Transformation matrix
│   ├── PsiExporterImage.cs          # Camera image (compressed)
│   ├── PsiExporterImageAsSteam.cs   # Camera image (raw bytes)
│   ├── PsiExporterString.cs         # String data
│   ├── PsiExporterInteger.cs        # Integer data
│   └── PsiExporterDateTime.cs       # Timestamp data
│
├── Importers/
│   ├── PsiImporterPosition.cs       # Receive position data
│   └── PsiImporterPing.cs           # Handle ping/heartbeat
│
├── Formats/
│   └── PsiFormatImage.cs            # Image serialization
│
└── Dependencies/
    ├── Microsoft.Psi.dll
    ├── Microsoft.Psi.Interop.dll
    └── Other required DLLs...
```

### Required DLLs

The package includes modified Psi DLLs built from commit `d08fdd34f6957a92a6343d6e7978c2b8efc5f83a` (June 24, 2023):

- Microsoft.Psi.dll
- Microsoft.Psi.Interop.dll
- Microsoft.Psi.Remoting.dll
- System.Buffers.dll
- System.Memory.dll
- Additional dependencies...

## Setup and Configuration

### Installation

1. **Import Unity Package**:
   ```
   Assets → Import Package → Custom Package
   Select PsiImporterExporter.unitypackage
   ```

2. **Configure Microsoft.BCL.Async** (Important):
   
   [[/images/bcl_configuration.jpg]]
   
   Set the following in Unity:
   - Validate References: ✅
   - CPU: Any CPU
   - OS: Any OS
   - Don't process: ✅

   This prevents conflicts with Unity's built-in async support.

3. **For Android Builds**:
   
   Add to **Scripting Define Symbols** in **Player Settings**:
   ```
   PSI_TCP_STREAMS
   ```
   
   This enables TCP-only mode, which is required for Android.

### PsiPipelineManager Component

The `PsiPipelineManager` is the core component that must be added to a GameObject in your scene.

   [[/images/pipeline_manager.jpg]]

#### Configuration Properties

**Start Mode**:
- `Manual`: All initialization steps must be triggered by script
- `Connection`: Automates connection to server, waits for start signal
- `Automatic`: Fully automated startup sequence

**Exporter Number Expected At Start**:
- Number of exporters to wait for before initializing
- Allows dynamic spawning/loading of GameObjects with exporters
- Set to 0 if all exporters are in the initial scene

**RendezVous Server Address**:
- IP address of the SAAC ServerApplication
- Use "localhost" or "127.0.0.1" for local testing
- Use network IP for distributed scenarios

**RendezVous Server Port**:
- Port number (default: 13331)
- Must match ServerApplication configuration

**Used Process Name**:
- Unique identifier for this Unity application instance
- Appears in ServerApplication's process list
- Example: "UnityVR_Room1"

**Used Address**:
- IP address this Unity application uses for streams
- Auto-detected by default
- Override for multi-NIC machines

**Waited Process**:
- List of process names to wait for before initializing
- Ensures dependencies are available
- Example: ["VideoCapture1", "AudioCapture1"]

**Accepted Process**:
- Whitelist of allowed incoming processes
- Empty list accepts none, except those in Waited Process

**Exporters Max Low Frequency Streams**:
- Reserved for future use (RemoteExporter limitation)
- Currently not implemented

**Exporters Starting Port**:
- First port number for exporters
- Each exporter increments this value
- Must match free port range in external Psi applications
- Example: 11411

**Text Log Object**:
- Optional TextMeshPro component for in-scene logging
- If null, logs go to Unity Console
- Useful for debugging in VR headsets

**Command Emitter Port**:
- Optional port for sending commands to other applications
- Used for bidirectional communication
- Example: 11511

#### State Machine

```
Instantiated
    ↓
Connecting (if StartMode != Manual)
    ↓
Connected (RendezVous connection established)
    ↓
Served (Waited processes discovered)
    ↓
Initializing (Exporters/Importers setup)
    ↓
Initialized (Ready to run)
    ↓
Running (Pipeline active)
    ↓
Stopped (Pipeline stopped)
```

## Creating Custom Exporters

### Base Class: PsiExporter<T>

All exporters inherit from `PsiExporter<T>`:

```csharp
public abstract class PsiExporter<T> : MonoBehaviour, IProducer<T>
{
    public string TopicName = "Topic";
    public float DataPerSecond = 0.0f;
    public PsiPipelineManager.ExportType ExportType;
    
    protected PsiPipelineManager PsiManager;
    public Emitter<T> Out { get; private set; }
    public bool IsInitialized { get; private set; }
    
    // Abstract method for Android builds
#if PSI_TCP_STREAMS
    protected abstract IFormatSerializer<T> GetSerializer();
#endif
}
```

### Example 1: Position Exporter

```csharp
using UnityEngine;
using Microsoft.Psi;

public class PsiExporterPosition : PsiExporter<System.Numerics.Vector3>
{
    void Update()
    {
        if (CanSend())
        {
            // Convert Unity Vector3 to System.Numerics.Vector3
            var position = new System.Numerics.Vector3(
                transform.position.x,
                transform.position.y,
                transform.position.z
            );
            
            Out.Post(position, GetCurrentTime());
        }
    }
    
#if PSI_TCP_STREAMS
    protected override IFormatSerializer<System.Numerics.Vector3> GetSerializer()
    {
        return new PsiFormatVector3();
    }
#endif
}
```

**Usage**:
1. Attach `PsiExporterPosition` to any GameObject
2. Set `TopicName` to unique identifier (e.g., "PlayerPosition")
3. Set `DataPerSecond` (0 = every frame, 10 = 10 times per second)
4. Select `ExportType` (TCPWriter for Android, HighFrequency otherwise)

### Example 2: Matrix4x4 Exporter

```csharp
using UnityEngine;
using Microsoft.Psi;

public class PsiExporterMatrix4x4 : PsiExporter<System.Numerics.Matrix4x4>
{
    void Update()
    {
        if (CanSend())
        {
            // Convert Unity Matrix4x4 to System.Numerics.Matrix4x4
            var unityMatrix = transform.localToWorldMatrix;
            var matrix = new System.Numerics.Matrix4x4(
                unityMatrix.m00, unityMatrix.m01, unityMatrix.m02, unityMatrix.m03,
                unityMatrix.m10, unityMatrix.m11, unityMatrix.m12, unityMatrix.m13,
                unityMatrix.m20, unityMatrix.m21, unityMatrix.m22, unityMatrix.m23,
                unityMatrix.m30, unityMatrix.m31, unityMatrix.m32, unityMatrix.m33
            );
            
            Out.Post(matrix, GetCurrentTime());
        }
    }
    
#if PSI_TCP_STREAMS
    protected override IFormatSerializer<System.Numerics.Matrix4x4> GetSerializer()
    {
        return new PsiFormatMatrix4x4();
    }
#endif
}
```

## Creating Custom Importers

### Base Class: PsiImporter<T>

```csharp
public abstract class PsiImporter<T> : IPsiImporter, IDisposable
{
    public string TopicName = "Topic";
    protected bool IsInitialized = false;
    protected PsiPipelineManager PsiManager;
    
    protected abstract void Process(T message, Envelope envelope);
    
#if PSI_TCP_STREAMS
    protected abstract IFormatDeserializer<T> GetDeserializer();
#endif
}
```

### Example 1: Position Importer

```csharp
using UnityEngine;
using Microsoft.Psi;

public class PsiImporterPosition : PsiImporter<System.Numerics.Vector3>
{
    protected override void Process(System.Numerics.Vector3 message, Envelope envelope)
    {
        // Convert to Unity coordinates
        var position = new Vector3(message.X, message.Y, message.Z);
        
        // Do something with the position
        // For example, move a GameObject on the Update method
    }
    
#if PSI_TCP_STREAMS
    protected override IFormatDeserializer<System.Numerics.Vector3> GetDeserializer()
    {
        return new PsiFormatVector3();
    }
#endif
}
```

### Example 2: Command Importer

```csharp
using UnityEngine;
using Microsoft.Psi;

public class PsiImporterPing : PsiImporter<TimeSpan>
{
    public UnityEvent OnPingReceived;
    
    protected override void Process(TimeSpan message, Envelope envelope)
    {
        Debug.Log($"Ping received: {message.TotalMilliseconds}ms");

        OnPingReceived?.Invoke();
    }
    
#if PSI_TCP_STREAMS
    protected override IFormatDeserializer<TimeSpan> GetDeserializer()
    {
        return new PsiFormatTimeSpan();
    }
#endif
}
```

## Serialization Formats

### Creating Custom Formats

For custom data types, create a format class:

```csharp
using Microsoft.Psi.Interop.Serialization;
using System.IO;

public class PsiFormatMyCustomType : IPsiFormat
{
    public static Format<MyCustomType> GetFormat()
    {
        return new Format<MyCustomType>(
            "MyCustomType",
            Write,
            Read
        );
    }
    
    private static void Write(MyCustomType obj, BinaryWriter writer)
    {
        writer.Write(obj.Property1);
        writer.Write(obj.Property2);
        // ... write all properties
    }
    
    private static MyCustomType Read(BinaryReader reader)
    {
        return new MyCustomType
        {
            Property1 = reader.ReadInt32(),
            Property2 = reader.ReadString()
            // ... read all properties
        };
    }
}
```

### Built-in Formats

Available in the Unity package:

- `PsiFormatBoolean` - bool
- `PsiFormatChar` - char
- `PsiFormatInt32` - int
- `PsiFormatFloat` - float
- `PsiFormatString` - string
- `PsiFormatVector3` - System.Numerics.Vector3
- `PsiFormatQuaternion` - System.Numerics.Quaternion
- `PsiFormatMatrix4x4` - System.Numerics.Matrix4x4
- `PsiFormatImage` - Shared<Image>
- `PsiFormatTupleOfVector` - Tuple<Vector3, Vector3>

## Integration with SAAC

### Server-Side Configuration

In your SAAC application (e.g., TestingConsole or custom server):

```csharp
// Configure RendezVous pipeline
var configuration = new RendezVousPipelineConfiguration();
configuration.RendezVousHost = "192.168.1.100";
configuration.RendezVousPort = 13331;
configuration.CommandPort = 11511;

// Register Unity stream formats
configuration.AddTopicFormatAndTransformer(
    "PlayerPosition",
    typeof(System.Numerics.Vector3),
    new PsiFormatVector3()
);

configuration.AddTopicFormatAndTransformer(
    "PlayerHead",
    typeof(System.Numerics.Matrix4x4),
    new PsiFormatMatrix4x4(),
    typeof(MatrixToCoordinateSystem)  // Optional transformer
);

configuration.AddTopicFormatAndTransformer(
    "LeftHand",
    typeof(System.Numerics.Matrix4x4),
    new PsiFormatMatrix4x4(),
    typeof(MatrixToCoordinateSystem)
);

// Create pipeline
var pipeline = new RendezVousPipeline(configuration, "Server");
pipeline.Start();
```

### Bidirectional Communication

**Unity → SAAC**:
```csharp
// In Unity (exporter)
public class VRHandExporter : PsiExporter<Matrix4x4>
{
    void Update()
    {
        if (CanSend())
        {
            Out.Post(GetHandMatrix(), GetCurrentTime());
        }
    }
}
```

**SAAC → Unity**:
```csharp
// In SAAC server
var lightToggle = pipeline.CreateEmitter<bool>(pipeline, "LightToggle");

// Export to Unity
var writer = new TcpWriter<bool>(pipeline, 11511, PsiFormatBoolean.GetFormat());
lightToggle.PipeTo(writer);

...
// Somewhere after all initialization
pipeline.RunAsynch();
...

// Send commands to Unity
lightToggle.Post(true, pipeline.GetCurrentTime());

```

**Unity receives**:
```csharp
// In Unity (importer)
public class LightController : PsiImporter<bool>
{
    public Light targetLight;
    
    protected override void Process(bool isOn, Envelope envelope)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            targetLight.enabled = isOn;
        });
    }
}
```

## Android-Specific Considerations

### 1. Scripting Define Symbol

Always add `PSI_TCP_STREAMS` to build settings:

```
Edit → Project Settings → Player → Other Settings
→ Scripting Define Symbols → Add "PSI_TCP_STREAMS"
```

### 2. TCP-Only Mode

Android builds must use TCP writers explicitly:

```csharp
// Set ExportType to TCPWriter
public PsiPipelineManager.ExportType ExportType = PsiPipelineManager.ExportType.TCPWriter;
```

### 3. Explicit Serializer Registration

In `PsiPipelineManager`, register all serializers upfront:

```csharp
protected void InitializeSerializer(KnownSerializers serializers)
{
    // All types must be registered before use
    serializers.Register<bool, BoolSerializer>();
    serializers.Register<int, IntSerializer>();
    serializers.Register<Vector3, Vector3Serializer>();
    // ... etc
}
```

### 4. IL2CPP Considerations

- Avoid `dynamic` types completely
- Pre-register all serializers
- Test thoroughly with IL2CPP backend
- Use explicit generic types everywhere

### 5. Network Permissions

Add to `AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

## Complete Example Scenarios

### Scenario 1: VR Hand Tracking

**Unity Setup**:
```csharp
// Attach to VR hand GameObjects
public class VRHandTracker : PsiExporterMatrix4x4
{
    // Configuration in Inspector:
    // TopicName: "LeftHand" or "RightHand"
    // ExportType: HighFrequency
    // DataPerSecond: 90 (for 90Hz tracking)
}
```

**SAAC Server**:
```csharp
// Configure pipeline
configuration.AddTopicFormatAndTransformer(
    "LeftHand",
    typeof(Matrix4x4),
    new PsiFormatMatrix4x4(),
    typeof(MatrixToCoordinateSystem)
);

configuration.AddTopicFormatAndTransformer(
    "RightHand",
    typeof(Matrix4x4),
    new PsiFormatMatrix4x4(),
    typeof(MatrixToCoordinateSystem)
);

// Create pipeline
var pipeline = new RendezVousPipeline(configuration, "VRAnalysis");
pipeline.Start();

// Streams automatically available for processing/visualization
```

### Scenario 2: Multi-User Collaboration

**Unity (Multiple Instances)**:
```csharp
// User1's Unity
PsiPipelineManager settings:
- UsedProcessName: "UnityUser1"
- Exporters: Position, Head, Hands

// User2's Unity
PsiPipelineManager settings:
- UsedProcessName: "UnityUser2"
- Exporters: Position, Head, Hands

// Each Unity also imports others' positions
WaitedProcess:
- "UnityUser1"
- "UnityUser2
- "UnityServer"
```

**Unity Server**:
```csharp
// Manage all Unity application data exchange through netcode
// Connect to SAAC Server for register events and experiment data
```

**SAAC Server**:
```csharp
// Server can coordinates all clients
// Each Unity instance automatically discovers others
```

## Troubleshooting

### Unity Issues

**DLLs Won't Load**:
- Verify Microsoft.BCL.Async is configured correctly
- Check all DLL import settings (CPU: Any, OS: Any, Don't process: ✅)
- Ensure .NET 4.x runtime is selected in Player Settings
- You can build them from source if needed (fork UnityAndroid branch)

**Type Not Found Errors**:
- Register type in `InitializeSerializer()`
- Create custom Format class if needed
- Verify namespace imports

**Android Build Fails**:
- Add `PSI_TCP_STREAMS` define symbol
- Check all exporters use `ExportType.TCPWriter`
- Test with IL2CPP backend

**Network Connection Fails**:
- Verify ServerApplication is running
- Check IP addresses and ports match
- Test firewall rules
- Use `ping` and `telnet` to verify connectivity

### Performance Issues

**Frame Rate Drops**:
- Reduce `DataPerSecond` on exporters
- Use `ExportType.LowFrequency` for non-critical data
- Optimize image resolution
- Profile with Unity Profiler

**Memory Leaks**:
- Properly dispose Shared<> objects
- Implement `OnDestroy()` in components
- Monitor with Memory Profiler

**Network Bandwidth**:
- Compress images before sending
- Reduce stream frequency
- Use delta encoding where possible

## Best Practices

### Performance

1. **Data Rate**: Set appropriate `DataPerSecond` values
   - 90Hz for VR tracking
   - 30Hz for video
   - 10Hz for UI updates
   - 1Hz for status data

2. **Export Type Selection**:
   - `HighFrequency`: Critical real-time data (tracking, input)
   - `LowFrequency`: Non-critical data (analytics, state)
   - `TCPWriter`: Android builds only

3. **Memory Management**:
   - Use `Shared<>` for large objects
   - Dispose resources in `OnDestroy()`
   - Pool frequently allocated objects

### Development Workflow

1. **Test Locally First**: Use "localhost" for initial development
2. **Log Everything**: Enable TextLogObject during development
3. **Monitor State**: Watch PsiPipelineManager.State
4. **Incremental Testing**: Test each exporter/importer individually

### Production Deployment

1. **Use Specific IPs**: Don't use auto-detection in production
2. **Whitelist Processes**: Use AcceptedProcess list
3. **Handle Disconnections**: Implement reconnection logic
4. **Monitor Health**: Use Status commands
5. **Error Handling**: Wrap all callbacks in try-catch


## References

- [Microsoft Psi](https://github.com/microsoft/psi)
- [SAAC Framework](https://github.com/SaacPSI/saac)
- [Unity Compatibility PR](https://github.com/microsoft/psi/pull/333)
- [Psi Issue #263](https://github.com/microsoft/psi/issues/263)
- [Unity Scripting Manual](https://docs.unity3d.com/Manual/ScriptingSection.html)

## License

Modified Psi components maintain the original Microsoft Psi license (MIT).  
Unity integration components are licensed under CeCILL-C.