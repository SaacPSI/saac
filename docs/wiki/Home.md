# Welcome

The SAAC wiki contains the documentation for the System As A Collaborator framework. Please use the sidebar to navigate through the documentation. If you would like to make edits, please fork the repository and send a pull request.

# A Quick Overview

System As A Collaborator (abbreviated as SAAC) is an open, extensible framework built on top of Microsoft \\psi for modeling, capturing, and augmenting collaborative activities in Extended Reality (XR) environments. The framework enables researchers and developers to build multimodal, integrative-AI systems that operate with different types of streaming data including video, audio, body tracking, physiological sensors, and speech recognition.

SAAC addresses the challenges of building complex multimodal systems by providing a robust infrastructure, ready-to-use applications, reusable components, and comprehensive tools for data capture, streaming, and analysis. The framework specifically targets collaborative scenarios in virtual and augmented reality environments, meeting rooms, and distributed multi-site setups.

Target Framework: .NET Framework 4.8

# The Challenge

Despite significant progress in sensing, perception, and XR technologies, building multimodal systems that can capture and analyze collaborative activities remains challenging. Many challenges stem from:
- The complexity of synchronizing multiple heterogeneous data sources
- Managing distributed capture systems across multiple machines
- Processing and storing large volumes of multimodal streaming data
- Analyzing temporal relationships between different modalities
- Real-time processing requirements for feedback and augmentation

SAAC addresses these challenges through a layered architecture based on Microsoft \\psi's robust streaming infrastructure, extended with distributed coordination protocols and domain-specific components.

# Infrastructure

SAAC provides a distributed runtime infrastructure specifically tailored for multimodal collaborative capture systems:

#### RendezVous Protocol:
A distributed coordination system that enables applications to discover each other, advertise available data streams, and execute remote commands. This protocol allows multiple capture applications to work together seamlessly across network boundaries.

#### Pipeline Services: 
High-level abstractions for managing \psi pipelines, including automatic session management, store creation, diagnostic modes, and lifecycle control. The RendezVousPipeline class extends these services with distributed coordination capabilities.

### Streaming Infrastructure: 
Built on \psi's time-aware streaming model, the infrastructure is optimized for low-latency processing and provides abstractions for synchronization, data fusion, windowing, and temporal operations. All data is time-stamped with high precision, enabling perfect replay and analysis.

### Data Persistence: 
Fast, efficient storage of generic data streams in \psi stores. The framework supports both centralized and distributed storage strategies, enabling data-driven development and offline analysis.

# Applications

SAAC provides several ready-to-use applications that can be combined to build complete capture systems:

### ServerApplication: 
Central coordination hub that implements the RendezVous protocol. It manages process discovery, tracks available data streams, and distributes commands to remote applications. The server provides a graphical interface for monitoring connected processes and sending control commands.

### VideoRemoteApp: 
Screen and window capture application with support for multiple cropping areas, configurable capture intervals, JPEG encoding, network streaming, and local recording. Features a visual cropping area selection tool and support for both standalone and coordinated operation modes.

### CameraRemoteApp:
USB camera capture from simple RGB to skeleton tracking, supporting multiple cameras, encoding options, and hybrid streaming/recording modes. Can be used with Azure Kinect, Kinect and Nuitrack.

### WhisperRemoteApp: 
Speech recognition and transcription service using Whisper models. Provides real-time transcription with support for multiple languages, streaming of both audio and transcription results, and integration with other SAAC applications.

# Components

SAAC provides an ecosystem of components organized by domain and dependencies:

### Core Components:
- PipelineServices: Infrastructure for pipeline management, session control, and store operations
- GlobalHelpers: Common utilities and helper functions
- Helpers: Additional utility components and transformations

### Body Tracking:
- Bodies: Unified abstraction for multiple body tracking technologies
- BodiesRemoteServices: Network-enabled body tracking services
- Kinect Azure, Kinect, Nuitrack integrations

### Audio Processing:
- AudioRecording: Microphone capture and audio management
- Whisper: Speech recognition components
- WhisperHelpers: Utilities for speech processing

### Network & Communication:
- RemoteExporter/Importer: TCP/UDP streaming components
- WebRTC: Browser-based video/audio streaming
- LabStreamLayer: Integration with LSL sensors

### Visualization & Analysis:
- Visualizations: Custom visualization components
- AnnotationsComponents: Annotation tools for data labeling
- AttentionMeasures: Gaze and attention analysis

### Integration:
- Unity integration components
- Unreal Engine connectors
- PLUME file format support

### Hardware Integration:
- Biopac: Physiological sensors
- TeslaSuit: Haptic suit integration
- Skinetic: Haptic vest
- LabJack: Data acquisition hardware
- Optitrack: Motion capture systems

### Machine Learning:
- Ollama: Large language model integration
- OpenFace: Model for tracking an retriving information from individual.

# Tools and Visualization

-SaaCPsiStudio provides a template for running a pipeline inside PsiStudio.

-Visualizations project contains the visualisation used with PsiStudio to display specific data stream from SAAC.

# Getting Started

Installation involves setting up the \psi fork and building the SAAC solution:

1. Clone the SAAC \psi fork (PsiStudio branch)
2. Build \psi to generate NuGet packages
3. Add the \psi package directory to NuGet sources
4. Clone the SAAC repository
5. Restore NuGet packages
6. Build the SAAC solution

A typical first application combines ServerApplication with VideoRemoteApp:
1. Start ServerApplication to create the coordination hub
2. Start VideoRemoteApp in streaming mode
3. Configure cropping areas for screen capture
4. Connect VideoRemoteApp to ServerApplication
5. Use ServerApplication to send run/stop commands
6. Data is streamed over network or recorded locally

The Quick Start guide provides detailed step-by-step instructions for setting up your first SAAC application.

# Example Scenarios

### Research Lab: 
Combine multiple VideoRemoteApps, KinectAzureRemoteConsole, and WhisperRemoteApp to capture multi-perspective video, body tracking, and speech in a coordinated manner. ServerApplication orchestrates the capture, ensuring synchronized start/stop across all modalities.

### VR/AR Study: 
Integrate with Unity or Unreal to capture VR interactions alongside external cameras, body tracking, and physiological sensors. The RendezVous protocol enables seamless communication between the game engine and capture applications.

### Remote Collaboration: 
Deploy ServerApplication as a central coordinator with capture applications distributed across multiple sites. The RendezVous protocol handles discovery and communication over the network.

### Offline Analysis: 
Record all modalities to local \psi stores during an experiment, then use ReplayPipeline for synchronized replay.

### Replay & Recording:
You can create your own application that replay a dataset and at the sametime record new datastream that can be synchronized on the record time.

# Architecture Patterns

SAAC applications typically follow one of several architectural patterns:

### Local Recording: 
Single application captures data and stores it locally in \psi stores. Suitable for standalone scenarios where network streaming is not required.

### Remote Streaming: 
Multiple applications stream data over the network to a central collection point. ServerApplication coordinates the applications, while data flows directly between producers and consumers.

### Hybrid Mode: 
Applications simultaneously record locally and stream over the network. This provides both real-time monitoring and reliable local backup of captured data.

### Distributed Pipeline: 
Complex processing distributed across multiple machines with intermediate results streamed between nodes. The RendezVous protocol enables dynamic discovery and connection of pipeline stages.

### Request/Response: 
Applications expose services (like Ollama AI integration) that other components can query. The framework supports both streaming and request/response patterns.

# Development and Contribution

SAAC is designed for extensibility. Developers can:
- Create new applications by combining existing components
- Author new \psi components for domain-specific processing
- Wrap external libraries through Interop projects
- Extend the RendezVous protocol with custom commands
- Contribute to the component ecosystem

# Community and Support

SAAC is developed and maintained by researchers at IMT Atlantique, University of Glasgow, and LISN CNRS. The project is open source under the CeCILL-C license.

### For support:
- Check the documentation wiki
- Review component-specific README files  
- Open issues on GitHub
- Contact the maintainers

For research use, please cite the CASE 2025 paper describing the framework architecture and use cases.

### Roadmap

Current development focuses on:
- Enhanced security features (TLS, authentication)
- Expanded machine learning pipeline integration
- Additional sensor integrations
- Additional data processing integration 

The framework is in active production use for research projects, and new features are added based on research needs and community contributions.
