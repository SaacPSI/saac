# Psi Studio UI Taxonomy

## Overview

Psi Studio is a visualization and analysis tool for the Microsoft Psi framework. The UI is organized into a hierarchical structure of containers, panels, and visualization objects that work together to display and interact with temporal data streams.

---

## 1. Top-Level Architecture

### 1.1 Main Window Structure
```
MainWindow (WPF Window)
├── Menu Bar
│   ├── File Menu
│   ├── Pipeline Plugin Menu
│   ├── Network Menu
│   └── Settings Menu
├── Toolbar
│   ├── Playback Controls
│   ├── Dataset Management
│   └── Plugin Controls
└── Visualization Container
    └── [Grid-based Layout System]
```

### 1.2 Core Components Hierarchy
```
MainWindowViewModel
├── VisualizationContainer
│   ├── Navigator (Time Control)
│   └── Visualization Panels (Grid Layout)
├── Dataset Management
├── Pipeline Plugin Handler (Optional)
└── Network Streams Manager (Optional)
```

---

## 2. Visualization System

### 2.1 Visualization Container
**Purpose**: Main container that holds all visualization panels in a grid-based layout.

**Key Features**:
- Grid-based layout system for organizing panels
- Supports dynamic panel addition/removal
- Manages panel resizing and positioning
- Coordinates time synchronization across all panels

**Types of Containers**:
- **Instant Visualization Container**: For real-time/live data streams
- **Timeline Visualization Container**: For recorded/replay data

### 2.2 Navigator
**Purpose**: Central time control system for playback and navigation.

**Key Responsibilities**:
- Time cursor management
- Playback control (play, pause, stop)
- Playback speed control
- Time interval selection
- Synchronization with all visualization panels

**Properties**:
- `IsCursorModePlayback`: Indicates if in playback mode
- Current time position
- Playback speed
- Time interval bounds

**Integration Points**:
- Synchronizes with pipeline plugins
- Coordinates network stream timing
- Updates all visualization objects

### 2.3 Visualization Panels
**Purpose**: Containers that hold one or more visualization objects.

**Panel Hierarchy**:
```
VisualizationPanel (Base Class)
├── InstantVisualizationPlaceholderPanel
│   └── Placeholder for adding new visualizations
├── ExternalApplicationVisualizationPanel
│   └── Hosts external viewer applications
└── [Type-Specific Panels]
    ├── XY Panel
    ├── 3D Panel
    ├── Timeline Panel
    └── Audio Panel
```

**Panel Properties**:
- `Name`: Panel identifier
- `Visible`: Visibility state
- `Height`: Panel height
- `BackgroundColor`: Background color
- `RelativeWidth`: Width relative to container
- `VisualizationObjects`: Collection of objects in panel

### 2.4 Visualization Objects
**Purpose**: Render specific data types in panels.

**Object Types**:
- **Stream Value Visualization Objects**: Display single stream values
- **Timeline Visualization Objects**: Display temporal data
- **3D Visualization Objects**: Display spatial/3D data
- **Networked Visualization Objects**: Display network-streamed data

**Common Properties**:
- `StreamSource`: Source stream binding
- `CursorEpsilon`: Time tolerance for data retrieval
- `Visible`: Visibility state
- `Name`: Object identifier

**Example Objects**:
- `AugmentedBodyVisualizationObject`
- `PositionOrientationVisualizationObject`
- `LabeledPoint2DVisualizationObject`
- `NetworkedStreamValueVisualisationObject<TData>`

---

## 3. Feature-Specific UI Components

### 3.1 Pipeline Plugin System

#### 3.1.1 Pipeline Plugin Browser Window
**File**: `PiplinePluginsWindow.xaml`

**Purpose**: User interface for selecting and loading pipeline plugins.

**UI Elements**:
- Plugin list/selection
- Assembly path browser
- Load/Unload buttons
- Plugin metadata display

**Integration**:
- Loads assemblies implementing `IPsiStudioPipeline`
- Manages plugin lifecycle
- Provides feedback on loading status

#### 3.1.2 Pipeline Plugin Interface
**Interface**: `IPsiStudioPipeline`

**Key Methods**:
```
IPsiStudioPipeline
├── GetReplayableMode(): PipelineReplaybleMode
├── RunPipeline(TimeInterval): bool
├── StopPipeline(): void
├── GetDataset(): Dataset
├── GetStartTime(): DateTime
├── GetLayout(): string
├── GetAnnotation(): string
├── ShowWindow(): void
└── CloseWindow(): void
```

**Properties**:
- `IsRunning`: Pipeline execution state
- `Name`: Plugin name
- `OnDatasetLoaded`: Event for dataset updates

#### 3.1.3 Pipeline Plugin Handler
**Class**: `PsiStudioPipelineAssemblyHandler`

**Responsibilities**:
- Dynamic assembly loading
- Component instantiation
- Lifecycle management
- Data access coordination

**Lifecycle Flow**:
```
User Selects Plugin
    ↓
[PiplinePluginsWindow loads assembly]
    ↓
[PsiStudioPipelineAssemblyHandler instantiated]
    ↓
[Plugin implements IPsiStudioPipeline]
    ↓
[Plugin state available in MainWindowViewModel]
    ↓
[Dataset/Layout/Annotations provided to VisualizationContainer]
```

### 3.2 Network Streaming System

#### 3.2.1 Network Configuration Window
**File**: `NetworkConfigurationWindow.xaml`

**Purpose**: Configure network streaming parameters.

**UI Elements**:
- Server address input (IP address)
- Server port input
- Available streams list
- Selected streams list
- Connection status indicator
- Stream selection checkboxes

**Configuration Properties**:
- `ServerAddress`: Remote server IP
- `ServerPort`: TCP port
- `AvailableStreams`: Discovered streams
- `SelectedStreams`: Streams to visualize

#### 3.2.2 Network Streams Manager
**Class**: `NetworkStreamsManager`

**Responsibilities**:
- Establish TCP/IP connections
- Manage active network streams
- Handle disconnections/errors
- Coordinate with Navigator for timing

**Data Flow**:
```
Remote Psi Pipeline
    ↓
[TCP/IP Stream]
    ↓
[NetworkStreamsManager]
    ↓
[Navigator synchronization]
    ↓
[NetworkedStreamValueVisualisationObject<TData>]
    ↓
[Visualization Panel]
```

#### 3.2.3 Networked Visualization Objects
**Class**: `NetworkedStreamValueVisualisationObject<TData>`

**Purpose**: Specialized visualization objects for network streams.

**Features**:
- Receives data from network streams
- Updates based on Navigator time cursor
- Type-specific rendering logic
- Handles network latency/buffering

### 3.3 External Viewer System

#### 3.3.1 External Viewer Selection Window
**File**: `SelectExternalViewerWindow.xaml`

**Purpose**: Dialog for selecting external viewer applications.

**UI Elements**:
- File browser for executables
- Viewer metadata display
- Supported stream types
- Selection confirmation

#### 3.3.2 External Application Panel
**Class**: `ExternalApplicationVisualizationPanel`

**Purpose**: Panel that hosts external viewer processes.

**Features**:
- Launches external application process
- Manages process lifecycle
- Handles window integration
- Communicates via IPC

**Properties**:
- `ExternalViewerPath`: Path to executable
- `ExternalViewerProcess`: Process reference
- `EnableWindowSizing`: Allow window resizing
- `ClipToViewport`: Clip to panel bounds

**Integration Flow**:
```
Visualization Container
    ↓
[InstantVisualizationPlaceholderPanel]
    ↓
[User selects "Load External Viewer"]
    ↓
[SelectExternalViewerWindow]
    ↓
[ExternalApplicationVisualizationPanel]
    ↓
[ExternalApplicationPanelView]
    ↓
[External Viewer Process]
```

---

## 4. Data Flow Architecture

### 4.1 Standard Replay Flow
```
Dataset (Psi Store)
    ↓
[MainWindowViewModel.OpenDataset()]
    ↓
[VisualizationContainer loads streams]
    ↓
[Navigator controls time cursor]
    ↓
[Visualization Objects query data at cursor time]
    ↓
[Panels render visualization objects]
```

### 4.2 Pipeline Plugin Flow
```
[User loads pipeline plugin]
    ↓
[IPsiStudioPipeline.RunPipeline()]
    ↓
[Plugin generates data streams]
    ↓
[Plugin provides Dataset via GetDataset()]
    ↓
[VisualizationContainer loads plugin dataset]
    ↓
[Navigator synchronizes with plugin execution]
    ↓
[Visualization panels display plugin data]
```

### 4.3 Network Streaming Flow
```
[Remote Psi Pipeline]
    ↓
[NetworkStreamsManager connects via TCP/IP]
    ↓
[Streams discovered and selected]
    ↓
[NetworkedStreamValueVisualisationObject created]
    ↓
[Navigator controls playback]
    ↓
[Network manager requests data at cursor time]
    ↓
[Remote pipeline sends data]
    ↓
[Visualization objects render network data]
```

### 4.4 External Viewer Flow
```
[Stream data in Visualization Container]
    ↓
[User selects "Load External Viewer" on placeholder]
    ↓
[SelectExternalViewerWindow opens]
    ↓
[User selects external application]
    ↓
[ExternalApplicationVisualizationPanel created]
    ↓
[External process launched]
    ↓
[Data sent to external process via IPC]
    ↓
[External application displays data]
```

---

## 5. UI Component Taxonomy

### 5.1 Container Hierarchy
```
MainWindow
└── VisualizationContainer
    ├── Navigator (Time Control)
    └── Panel Grid
        ├── Panel 1
        │   └── VisualizationObject 1
        ├── Panel 2
        │   ├── VisualizationObject 2
        │   └── VisualizationObject 3
        └── Panel N
            └── VisualizationObject N
```

### 5.2 Panel Types
1. **Placeholder Panels**: Empty panels for adding visualizations
2. **Standard Panels**: Panels with built-in visualization objects
3. **External Panels**: Panels hosting external applications
4. **Network Panels**: Panels displaying network streams

### 5.3 Visualization Object Types
1. **Value Objects**: Display single values (e.g., numbers, strings)
2. **Timeline Objects**: Display temporal sequences
3. **Spatial Objects**: Display 2D/3D spatial data
4. **Composite Objects**: Display collections (lists, dictionaries)
5. **Network Objects**: Display network-streamed data

### 5.4 Control Elements
1. **Navigator Controls**:
   - Play/Pause button
   - Stop button
   - Speed control
   - Time cursor slider
   - Time interval selection

2. **Dataset Controls**:
   - Open dataset button
   - Session selection
   - Stream selection

3. **Plugin Controls**:
   - Load plugin button
   - Plugin settings button
   - Update dataset button

4. **Network Controls**:
   - Network configuration button
   - Stream selection
   - Connection status

---

## 6. Settings and Configuration

### 6.1 PsiStudioSettings
**Purpose**: Persistent application settings.

**Key Settings**:
- `AdditionalPlugins`: List of pipeline plugin assemblies
- `AdditionalAssemblies`: List of assemblies for serialization
- Network configuration
- Layout preferences
- Annotation schemas

### 6.2 Settings UI
**Window**: Settings dialog

**Sections**:
- Pipeline Plugins configuration
- Additional Assemblies configuration
- Network settings
- Visualization preferences
- Layout management

---

## 7. Interaction Patterns

### 7.1 Adding a Visualization
1. Right-click on placeholder panel
2. Select "Add Visualization"
3. Choose stream from dataset
4. Select visualization object type
5. Panel updates with new visualization

### 7.2 Loading a Pipeline Plugin
1. Menu: Pipeline → Load Plugin
2. Browse for plugin assembly
3. Plugin window opens (if `ShowWindow()` implemented)
4. Dataset automatically loads (if `AutoRefreshDatasetOnChangeFromPlugin` enabled)
5. Layout and annotations applied

### 7.3 Configuring Network Streaming
1. Menu: Network → Configure
2. Enter server address and port
3. Discover available streams
4. Select streams to visualize
5. Streams appear in visualization panels

### 7.4 Using External Viewer
1. Right-click on placeholder panel
2. Select "Load External Viewer"
3. Browse for external application
4. External application launches
5. Data streams to external application

---

## 8. Key UI Patterns

### 8.1 Time Synchronization
- All visualization objects synchronize to Navigator's time cursor
- Network streams align with playback timing
- Pipeline plugins can control Navigator timing

### 8.2 Layout Management
- Panels arranged in grid layout
- Layouts can be saved/loaded
- Panels can be resized and repositioned
- Layouts provided by pipeline plugins

### 8.3 Annotation System
- Annotations can be displayed on timeline
- Annotation schemas provided by plugins
- Annotations synchronized with time cursor

### 8.4 Context Menus
- Right-click on panels for context actions
- Add/remove visualizations
- Configure panel settings
- Load external viewers
- Panel-specific actions

---

## 9. Component Relationships

```
MainWindowViewModel
    ├── Manages → VisualizationContainer
    │               ├── Contains → Navigator
    │               └── Contains → Visualization Panels
    │
    ├── Manages → PsiStudioPipelineAssemblyHandler (if plugin loaded)
    │               └── Wraps → IPsiStudioPipeline implementation
    │
    ├── Manages → NetworkStreamsManager (if network active)
    │               └── Creates → NetworkedStreamValueVisualisationObject
    │
    └── Manages → PsiStudioSettings
                    └── Stores → Configuration data
```

---

## 10. Extension Points

### 10.1 Custom Visualization Objects
- Inherit from base visualization object classes
- Implement type-specific rendering
- Register in Additional Assemblies

### 10.2 Custom Pipeline Plugins
- Implement `IPsiStudioPipeline` interface
- Provide dataset, layout, and annotations
- Register in Additional Plugins settings

### 10.3 Custom Serialization Formats
- Implement `GetFormat()`, `Write()`, `Read()` methods
- Register in Additional Assemblies
- Used for network streaming

---

## Summary

The Psi Studio UI follows a hierarchical container-panel-object architecture:

1. **MainWindow** contains the overall application structure
2. **VisualizationContainer** manages the grid layout and time synchronization
3. **Navigator** provides centralized time control
4. **Visualization Panels** organize visualization objects
5. **Visualization Objects** render specific data types

Three major features extend this base:
- **Pipeline Plugins**: Execute custom pipelines within PsiStudio
- **Network Streaming**: Visualize data from remote Psi applications
- **External Viewers**: Integrate specialized external visualization tools

All components work together through the Navigator's time cursor to provide synchronized, multi-stream visualization of temporal data.



