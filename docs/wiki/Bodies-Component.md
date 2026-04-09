# Bodies Component

## Overview

**Bodies** provides body tracking and skeleton processing components for the SAAC framework. It converts and processes skeletal data from various sources (Azure Kinect, Kinect v2, Nuitrack) into a unified format, and provides analysis tools for body postures, gestures, and interactions.

**Key Features**:
- ? Unified skeleton representation across multiple sensors
- ? Body converter for Azure Kinect, Kinect v2 formats
- ? Posture detection (standing, sitting, lying, arms raised, etc.)
- ? Hand proximity detection (clapping, hand-to-hand distance)
- ? Body selection and identification
- ? Statistical analysis of body movements
- ? Multi-person tracking support

## Components

### 1. BodiesConverter

Converts sensor-specific body formats to unified SAAC format.

**Supported Formats**:
- Azure Kinect `List<AzureKinectBody>` ?  `List<Body>`
- Kinect v2 `List<KinectBody>` ? `List<Body>`
- Nuitrack `List<NuitrackBody>` ? `List<Body>`

**Example**:
```csharp
using Microsoft.Psi;
using SAAC.Bodies;

var pipeline = Pipeline.Create();

// Azure Kinect sensor
var azureKinect = new AzureKinectSensor(pipeline);

// Convert bodies
var converter = new BodiesConverter(pipeline);
azureKinect.Bodies.PipeTo(converter.InBodiesAzure);

// Use unified format
converter.Out.Do((bodies, env) =>
{
    foreach (var body in bodies)
    {
        Console.WriteLine($"Body {body.TrackingId}: {body.Joints.Count} joints");
    }
});

pipeline.Run();
```

### 2. BodyPosturesDetector

Detects predefined body postures in real-time.

**Detected Postures**:
- **Standing**: Upright position
- **Sitting**: Seated position
- **Lying**: Horizontal position
- **ArmsRaised**: Both arms above shoulders
- **TShapePose**: Arms extended horizontally
- **Crouching**: Low center of mass
- **Leaning**: Body tilted forward/backward

**Configuration**:
```csharp
public class BodyPosturesDetectorConfiguration
{
    public double StandingThreshold { get; set; } = 0.8;
    public double SittingThreshold { get; set; } = 0.5;
    public double ArmsRaisedAngle { get; set; } = 45.0;  // degrees
    public bool DetectAllPostures { get; set; } = true;
}
```

**Example**:
```csharp
var config = new BodyPosturesDetectorConfiguration
{
    DetectAllPostures = true
};

var detector = new BodyPosturesDetector(pipeline, config);

converter.Out.PipeTo(detector.In);

detector.Out.Do((postures, env) =>
{
    foreach (var bodyPostures in postures)
    {
        Console.WriteLine($"Body {bodyPostures.Key}:");
        foreach (var posture in bodyPostures.Value)
        {
            Console.WriteLine($"  - {posture}");
        }
    }
});
```

### 3. HandsProximityDetector

Detects hand proximity and clapping gestures.

**Features**:
- Distance between hands
- Clapping detection
- Hand-to-hand interaction
- Configurable thresholds

**Configuration**:
```csharp
public class HandsProximityDetectorConfiguration
{
    public double ClappingThreshold { get; set; } = 0.15;  // meters
    public double ProximityThreshold { get; set; } = 0.3;  // meters
    public bool IsPairToCheckGiven { get; set; } = false;
}
```

**Example**:
```csharp
var proximityConfig = new HandsProximityDetectorConfiguration
{
    ClappingThreshold = 0.15,  // 15 cm for clapping
    IsPairToCheckGiven = false  // Check all bodies
};

var proximityDetector = new HandsProximityDetector(pipeline, proximityConfig);

converter.Out.PipeTo(proximityDetector.In);

proximityDetector.Out.Do((proximities, env) =>
{
    foreach (var data in proximities)
    {
        foreach (var interaction in data.Value)
        {
            Console.WriteLine($"{data.Key} - {interaction}");
        }
    }
});
```

### 4. BodiesSelection

Selects specific bodies based on criteria.

**Selection Modes**:
- By tracking ID
- By position (closest to point)
- By posture
- By joint confidence

**Example**:
```csharp
var config = new BodiesSelectionConfiguration
{
    SelectionMode = SelectionMode.ClosestToPoint,
    TargetPoint = new Point3D(0, 0, 2.0),  // 2 meters in front
    MaxBodies = 1
};

var selector = new BodiesSelection(pipeline, config);
converter.Out.PipeTo(selector.In);

// Only selected bodies
selector.Out.Do((selectedBodies, env) =>
{
    Console.WriteLine($"Selected {selectedBodies.Count} bodies");
});
```

### 5. BodiesIdentification

Identifies and tracks bodies across frames.

**Features**:
- Persistent ID assignment
- Re-identification after occlusion
- Multi-person tracking
- Trajectory smoothing

**Example**:
```csharp
var idConfig = new BodiesIdentificationConfiguration
{
    MaxTrackingLossFrames = 30,  // 1 second at 30 FPS
    SimilarityThreshold = 0.8
};

var identifier = new BodiesIdentification(pipeline, idConfig);
converter.Out.PipeTo(identifier.In);

identifier.Out.Do((identifiedBodies, env) =>
{
    foreach (var body in identifiedBodies)
    {
        Console.WriteLine($"Person {body.PersistentId}: Tracking ID {body.TrackingId}");
    }
});
```

### 6. BodiesStatistics

Computes statistical measures of body movements.

**Statistics**:
- Joint velocities
- Joint accelerations
- Body center of mass
- Bounding box
- Movement energy
- Posture stability

**Example**:
```csharp
var statistics = new BodiesStatistics(pipeline);
converter.Out.PipeTo(statistics.In);

statistics.Out.Do((stats, env) =>
{
    foreach (var bodyStat in stats)
    {
        Console.WriteLine($"Body {bodyStat.Key}:");
        Console.WriteLine($"  Velocity: {bodyStat.Value.AverageVelocity} m/s");
        Console.WriteLine($"  Energy: {bodyStat.Value.MovementEnergy}");
    }
});
```

## Unified Body Format

### Body Structure

```csharp
public class Body
{
    public uint TrackingId { get; set; }
    public uint PersistentId { get; set; }  // Cross-frame tracking
    
    public Dictionary<JointType, Joint> Joints { get; set; }
    
    public Point3D CenterOfMass { get; set; }
    public BoundingBox3D BoundingBox { get; set; }
    
    public BodyTrackingState TrackingState { get; set; }
    public double OverallConfidence { get; set; }
}

public class Joint
{
    public JointType Type { get; set; }
    public Point3D Position { get; set; }
    public Quaternion Orientation { get; set; }
    public JointTrackingState TrackingState { get; set; }
    public double Confidence { get; set; }
}

public enum JointType
{
    Head, Neck,
    SpineShoulder, SpineMid, SpineBase,
    ShoulderLeft, ShoulderRight,
    ElbowLeft, ElbowRight,
    WristLeft, WristRight,
    HandLeft, HandRight,
    HipLeft, HipRight,
    KneeLeft, KneeRight,
    AnkleLeft, AnkleRight,
    FootLeft, FootRight
}
```

## Integration Examples

### Example 1: Full Body Analysis Pipeline

```csharp
using Microsoft.Psi;
using SAAC.Bodies;

var pipeline = Pipeline.Create();

// Sensor input
var azureKinect = new AzureKinectSensor(pipeline);

// Convert to unified format
var converter = new BodiesConverter(pipeline);
azureKinect.Bodies.PipeTo(converter.InBodiesAzure);

// Detect postures
var postureDetector = new BodyPosturesDetector(pipeline, new BodyPosturesDetectorConfiguration());
converter.Out.PipeTo(postureDetector.In);

// Detect hand interactions
var handDetector = new HandsProximityDetector(pipeline, new HandsProximityDetectorConfiguration());
converter.Out.PipeTo(handDetector.In);

// Compute statistics
var statistics = new BodiesStatistics(pipeline);
converter.Out.PipeTo(statistics.In);

// Store all data
var store = PsiStore.Create(pipeline, "BodyAnalysis", @"D:\Data");
store.Write(converter.Out, "Bodies");
store.Write(postureDetector.Out, "Postures");
store.Write(handDetector.Out, "HandInteractions");
store.Write(statistics.Out, "Statistics");

pipeline.Run();
```

### Example 2: Multi-Person Interaction Detection

```csharp
var pipeline = Pipeline.Create();
var azureKinect = new AzureKinectSensor(pipeline);

// Convert bodies
var converter = new BodiesConverter(pipeline);
azureKinect.Bodies.PipeTo(converter.InBodiesAzure);

// Identify individuals
var identifier = new BodiesIdentification(pipeline, new BodiesIdentificationConfiguration());
converter.Out.PipeTo(identifier.In);

// Detect interactions between specific people
var proximityConfig = new HandsProximityDetectorConfiguration
{
    IsPairToCheckGiven = true  // Will provide specific pairs
};

var proximityDetector = new HandsProximityDetector(pipeline, proximityConfig);

// Create connector for pairs to check
var pairConnector = pipeline.CreateConnector<List<(uint, uint)>>("pairs");
pairConnector.Out.PipeTo(proximityDetector.InPair);

// Define pairs to check (e.g., Person 1 and Person 2)
identifier.Out.Do((bodies, env) =>
{
    if (bodies.Count >= 2)
    {
        var person1 = bodies[0].PersistentId;
        var person2 = bodies[1].PersistentId;
        
        var pairs = new List<(uint, uint)> { (person1, person2) };
        pairConnector.In.Post(pairs, env.OriginatingTime);
    }
});

identifier.Out.PipeTo(proximityDetector.In);

proximityDetector.Out.Do((interactions, env) =>
{
    foreach (var interaction in interactions)
    {
        Console.WriteLine($"Interaction between {interaction.Key}: {string.Join(", ", interaction.Value)}");
    }
});

pipeline.Run();
```

## See Also

- [Components Overview](Components-Overview.md) - All SAAC components
- [BodiesRemoteServices Component](BodiesRemoteServices-Component.md) - Remote body streaming
- [Groups Component](Groups-Component.md) - Group dynamics analysis
- [Gestures Component](Gestures-Component.md) - Gesture recognition
- [Architecture Overview](Architecture.md) - SAAC framework architecture
