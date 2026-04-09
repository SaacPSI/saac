# SAAC Wiki Documentation Index

Welcome to the SAAC (Situated Analytics with Augmented Cognition) framework documentation. This index provides quick access to all available documentation.

## ?? Main Documentation

### Getting Started
- [Quick Start Guide](Quick-Start.md) - Get up and running with SAAC
- [Architecture Overview](Architecture.md) - Understand the framework design
- [Components Overview](Components-Overview.md) - Browse all available components

### Applications
- [ServerApplication](ServerApplication.md) - Central coordination application for distributed data collection
- [VideoRemoteApp](VideoRemoteApp.md) - Screen and window capture application
- [WhisperRemoteApp](WhisperRemoteApp.md) - Speech-to-text transcription application
- [CameraRemoteApp](CameraRemoteApp.md) - Multi-sensor depth camera capture application

### Core Components
- [PipelineServices Component](PipelineServices-Component.md) - Pipeline management (RendezVous, Replay, Dataset)
- [ReplayPipeline Guide](ReplayPipeline-Guide.md) - Dataset replay with processing
- [AnnotationsComponents Component](AnnotationsComponents-Component.md) - Web-based annotation interface
- [WebRTC Component](WebRTC-Component.md) - Unity/Unreal Engine streaming
- [Bodies Component](Bodies-Component.md) - Body tracking and analysis

### Additional Components
- [Additional Components](Additional-Components.md) - Documentation for all other SAAC components (Audio, Sensors, AI, Utilities)

### Advanced Topics
- [PsiStudio Modifications](PsiStudio-Modifications.md) - PsiStudio enhancements (Pipeline plugins, Network streaming, External viewers)
- [Psi in Unity](../PsiInUnity.md) - Unity integration modifications

## ??? Documentation by Category

### ?? Data Capture & Sensors
| Component | Description | Documentation |
|-----------|-------------|---------------|
| AudioRecording | Multi-source audio capture | [Additional Components](Additional-Components.md#audiorecording-component) |
| Biopac | Physiological signal acquisition | [Additional Components](Additional-Components.md#biopac-component) |
| LabJack | Data acquisition hardware | [Additional Components](Additional-Components.md#labjack-component) |
| LabStreamLayer | LSL protocol integration | [Additional Components](Additional-Components.md#labstreamlayer-component) |
| Optitrack | Motion capture system | [Additional Components](Additional-Components.md#optitrack-component) |
| TeslaSuit | Haptic suit integration | [Additional Components](Additional-Components.md#teslasuit-component) |
| **CameraRemoteApp** | Multi-sensor depth cameras | [CameraRemoteApp](CameraRemoteApp.md) |
| **VideoRemoteApp** | Screen/window capture | [VideoRemoteApp](VideoRemoteApp.md) |

### ?? Body Tracking & Analysis
| Component | Description | Documentation |
|-----------|-------------|---------------|
| **Bodies** | Unified body tracking | [Bodies Component](Bodies-Component.md) |
| BodiesRemoteServices | Remote body streaming | [Additional Components](Additional-Components.md#kinect-components) |
| KinectAzureRemoteServices | Azure Kinect streaming | [CameraRemoteApp](CameraRemoteApp.md#azure-kinect-configuration) |
| KinectRemoteServices | Kinect v2 streaming | [CameraRemoteApp](CameraRemoteApp.md#kinect-configuration) |
| NuitrackRemoteServices | Nuitrack streaming | [CameraRemoteApp](CameraRemoteApp.md#nuitrack-configuration) |
| OpenFace | Facial analysis | [Additional Components](Additional-Components.md#openface-component) |

### ??? Speech & Audio Processing
| Component | Description | Documentation |
|-----------|-------------|---------------|
| **Whisper** | Speech-to-text | [WhisperRemoteApp](WhisperRemoteApp.md) |
| **WhisperRemoteServices** | Remote transcription | [WhisperRemoteApp](WhisperRemoteApp.md#network-streaming) |
| OpenSmile | Audio feature extraction | [Additional Components](Additional-Components.md#opensmile-component) |

### ?? Virtual Reality & Game Engines
| Component | Description | Documentation |
|-----------|-------------|---------------|
| **WebRTC** | Unity/Unreal streaming | [WebRTC Component](WebRTC-Component.md) |
| Unity | Unity integration helpers | [Additional Components](Additional-Components.md#unity-component) |
| UnrealRemoteConnector | Unreal HTTP integration | [Additional Components](Additional-Components.md#unrealremoteconnector-component) |

### ?? Annotations & Visualization
| Component | Description | Documentation |
|-----------|-------------|---------------|
| **AnnotationsComponents** | Web annotation interface | [AnnotationsComponents Component](AnnotationsComponents-Component.md) |
| Visualizations | PsiStudio visualizations | [Additional Components](Additional-Components.md#visualizations-component) |

### ?? Utilities & Services
| Component | Description | Documentation |
|-----------|-------------|---------------|
| **PipelineServices** | Pipeline management | [PipelineServices Component](PipelineServices-Component.md) |
| **ReplayPipeline** | Dataset replay | [ReplayPipeline Guide](ReplayPipeline-Guide.md) |
| PsiFormats | Custom serialization | [Additional Components](Additional-Components.md#psiformats-component) |
| InteropExtension | WebSocket/TCP extensions | [Additional Components](Additional-Components.md#interopextension-component) |

## ?? Quick Reference

### Common Tasks

**Start a new experiment**:
1. Read [Quick Start Guide](Quick-Start.md)
2. Configure [ServerApplication](ServerApplication.md)
3. Set up remote applications ([VideoRemoteApp](VideoRemoteApp.md), [CameraRemoteApp](CameraRemoteApp.md), etc.)

**Replay recorded data**:
1. See [ReplayPipeline Guide](ReplayPipeline-Guide.md)
2. Learn about [PipelineServices](PipelineServices-Component.md)

**Add annotations**:
1. Use [AnnotationsComponents](AnnotationsComponents-Component.md)

**Stream to Unity/Unreal**:
1. Set up [WebRTC Component](WebRTC-Component.md)

**Analyze body movements**:
1. Capture with [CameraRemoteApp](CameraRemoteApp.md)
2. Process with [Bodies Component](Bodies-Component.md)

## ?? Support

For questions or issues:
1. Check existing documentation
2. Search [GitHub Issues](https://github.com/SaacPSI/saac/issues)
3. Create new issue

---

**SAAC Framework Documentation**
