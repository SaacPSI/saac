# CASPER Analysis Application

## Overview

CASPER Analysis is a Psi-based application for analyzing pre-recorded experimental data according to the decision tree logic defined in Logigramme1.mmd. The application processes multimodal data streams (gaze, hand tracking, speech, button presses, door status) and classifies user interactions into Alpha, Beta, or Gamma learning/anticipation states.

## Purpose

This application is designed to:
1. **Load pre-recorded datasets** from ServerApplication
2. **Replay and process** multimodal data streams
3. **Implement decision logic** from the logigramme
4. **Output classification results** (Alpha/Beta/Gamma) with timestamps

## Architecture

### Components

1. **Stream Processors** (`StreamProcessors/`):
   - `HandDoorProximityDetector`: Detects hand proximity to door
   - `GazeWindowDetector`: Detects gaze events within time windows (50-150ms)
   - `SpeechComprehensionClassifier`: Classifies speech into comprehension states
   - `ButtonPressCounter`: Counts consecutive button presses

2. **LogigrammeAnalyzer**: Implements the decision tree logic (currently simplified - needs refinement)

3. **MainWindow**: UI for loading datasets, running analysis, and exporting results

## Data Requirements

The analysis requires the following streams (as defined in `experiment.json`):

### Required Streams:
- **Module status**: `ValueTuple<Int32, String>` - Module generation success status
- **Wrist positions**: `Tuple<Vector3, Vector3>` - Hand tracking (e.g., `1-LeftWrist`, `1-RightWrist`)
- **Door status**: `ValueTuple<Boolean, Vector3>` - Door opening/closing (e.g., `Porte1 ouverture`)
- **Gaze data**: `ValueTuple<Int32, Boolean, String>` - Gaze tracking (e.g., `Gaze1`, `Gaze1IndicatorDoor`)
- **Button presses**: `Boolean` - Validation buttons (e.g., `M1-Validation`)
- **Speech**: `String` - Speech transcription (from WhisperRemoteApp)

### Optional Streams:
- Visual feedback status (may need to be configured)
- Different button presses (for detecting alternative button interactions)

## Usage

### 1. Recording Data

First, record experimental data using ServerApplication:
- Configure ServerApplication with appropriate settings
- Connect all data sources (cameras, sensors, Unity, etc.)
- Record session to a dataset (.pds file)

### 2. Running Analysis

1. **Launch CASPER Analysis**
2. **Load Dataset**:
   - Click "Browse..." to select a `.pds` dataset file
   - The application will load the dataset and display available sessions
3. **Run Analysis**:
   - Click "Run Analysis" to process the recorded data
   - The application will replay streams and apply decision logic
   - Results appear in the log window
4. **Export Results**:
   - Click "Export Results" to save classifications to CSV or JSON
   - Results include timestamps, classification type, and reasoning

## Classification Types

- **AnticipationGamma** (R1): Hand touching/moving toward door before perturbation
- **GammaLearning** (R2): Gaze on indicator → gaze on door/speech → door closed
- **Gamma** (R3, R6): Various paths leading to successful door closure
- **Alpha** (R4): Button pressed 3x with speech incomprehension/annoyance
- **Beta** (R5): Different button pressed with speech incomprehension/annoyance

## Implementation Notes

### Current Status

The current implementation provides:
- ✅ Basic application structure
- ✅ Stream processor components
- ✅ Dataset loading and replay
- ✅ Simplified analysis pipeline
- ⚠️ **LogigrammeAnalyzer needs refinement** - The decision tree logic needs proper Psi stream operators (Window, Join, Fuse) implementation

### Next Steps

1. **Refine LogigrammeAnalyzer**:
   - Properly implement temporal windowing for gaze detection (50-150ms)
   - Use Psi's `Join` and `Fuse` operators for multi-stream coordination
   - Handle state machine transitions correctly

2. **Add Missing Stream Processors**:
   - Hand-Door proximity calculation (combining wrist and door positions)
   - Visual feedback status detection
   - Different button press detection

3. **Speech Integration**:
   - Connect to WhisperRemoteApp speech streams
   - Improve speech comprehension classification

4. **Testing**:
   - Test with sample recorded data
   - Validate classifications against ground truth
   - Handle edge cases and timing issues

## Technical Details

### Dependencies
- Microsoft.Psi (0.19.100.1-beta-SAAC)
- Microsoft.Psi.Data
- Microsoft.Psi.PsiStudio.PipelinePlugin
- Newtonsoft.Json

### Stream Processing

The analysis uses Psi's streaming operators:
- `Window()`: Create time windows for temporal analysis
- `Join()`: Align multiple streams temporally
- `Fuse()`: Merge streams with different rates
- `Select()`: Transform data
- `Where()`: Filter events

### Dataset Structure

Expected dataset structure (from ServerApplication):
```
Dataset.pds
└── Session/
    ├── Events/          (Module status, buttons, gaze, etc.)
    ├── PositionRotation_1/  (Wrist positions, head)
    └── ...
```

## Troubleshooting

### "Stream not found" errors
- Verify stream names match those in `experiment.json`
- Check that data was recorded in ServerApplication
- Ensure stream types match expected types

### Timing issues
- Gaze windows (50-150ms) may need adjustment based on data rate
- Consider using Psi's delivery policies for stream alignment

### Missing classifications
- Check that all required streams are present
- Verify decision tree logic matches logigramme
- Review log output for processing errors

## Related Documentation

- [Data Requirements and Pipeline Design](../../../ExpeCASPER_ForPsiAnalysis/Documentation/StageDorian/DataRequirementsAndPipeline.md)
- [Logigramme1.mmd](../../../ExpeCASPER_ForPsiAnalysis/Documentation/StageDorian/Logigrammes/Logigramme1.mmd)
- [ServerApplication README](../ServerApplication/README.md)

