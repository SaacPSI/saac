# CollaborationIndices Component

## Overview

**CollaborationIndices** computes behavioural indicators of collaboration for a group of any size
(movement, synchrony, verbal participation, gaze, task actions, spatial formations...) on a
sliding time window, and merges them into dimension scores and a collaboration score. It runs
identically on live streams and on the replay of recorded \psi stores.

The method comes from two studies, referred to as **[A]** and **[B]** in this page:

- **[A]** Léchappé, Fleury, Chollet and Dumas (2025). *How to Categorize Collaboration during a
  Collaborative Puzzle-solving Task? Validation of Collaboration Profiles using Multimodal Data in
  Virtual Reality Context.* Proc. ACM Hum.-Comput. Interact. 9(2), CSCW098.
  https://doi.org/10.1145/3710996
- **[B]** Léchappé, Milliat, Fleury, Chollet and Dumas. *Assessing Triadic Collaboration in
  Virtual Reality Using a Multimodal Computational Approach.* Manuscript under revision.

[A] defines the indicators, the aspects of collaboration they belong to and the 20 s window.
[B] extends them to triads and defines the normalisation, the five dimensions and the
dimension-based collaboration score.

**Contents**

1. [Architecture](#1-architecture)
2. [The builder](#2-the-builder)
3. [The time window](#3-the-time-window)
4. [Normalisation and calibration](#4-normalisation-and-calibration)
5. [Indicators](#5-indicators)
6. [The collaboration score](#6-the-collaboration-score)
7. [Outputs: snapshot, graph, CSV](#7-outputs-snapshot-graph-csv)
8. [Conformity with the published method](#8-conformity-with-the-published-method)
9. [Extending, testing, files](#9-extending-testing-files)

A shorter introduction is in `Components/CollaborationIndices/README.md`, and a program that
runs as it is in `Applications/CollaborationIndicesExample`.

---

## 1. Architecture

```
 source streams                 indicators                          fusion
 ──────────────                 ──────────                          ──────
 positions  ───────────────►  Movement ─────────┐
 positions  ───────────────►  Synchrony ────────┤
 speech intervals ─────────►  VerbalParticipation ─► SpeechEquality, TalkingMost
 speech intervals ─────────►  Silence, CrossTalk
 turn-taking events ───────►  TurnTaking ───────┤
 JVA / gaze events ────────►  JointVisualAttention, GazeOnPeers, MutualGaze
 task events ──────────────►  TaskParticipation ──► TaskEquality, TaskingMost
 presence intervals ───────►  TimeInArea ───────┤
 formation events ─────────►  Formation ────────┤            ┌► CollaborationScore
 distances ────────────────►  Proximity ────────┤            │        │
                                                ├► Snapshot ─┤        ▼
 clock ──► PhaseGate ──► tick (to every         │  assembler ├► InteractionGraph
            ▲             indicator and to the ─┘            └► CSV export
 phase start / end        snapshot assembler)
```

Three layers:

- **Components** do the computing. Each one is a \psi component with typed inputs, a tick input
  and outputs. They exist independently of the builder and can be used alone
  (`PhysicalSynchronyComponent`, `VerbalParticipationComponent`...).
- **Indicators** (`ICollaborationIndicator`) are thin descriptions of a component for the
  builder: which options it has, which other indicators it reads from, which clock paces it and
  which named outputs it offers.
- **`SlidingAverageComputation`** is the instance returned by the builder. It knows no indicator
  in particular: it creates the phase gate, asks each declared indicator to build itself, then
  wires the snapshot assembler, the score, the graph and the export from what the indicators
  declared.

Every indicator receives two kinds of input: its **data** (events, intervals or positions,
stamped with their originating time) and the **tick** of the shared clock. Data is only stored
when it arrives; all the computing happens on a tick.

### Input types

| Type | Meaning | Used by |
|---|---|---|
| `Vector3` | position of one body part of one participant | Movement, Synchrony |
| `InteractionInterval` | a state held from `StartTime` to `EndTime` by `ParticipantId` (speaking, being in an area). `EndTime = DateTime.MaxValue` means still ongoing | VerbalParticipation, TimeInArea |
| `InteractionEvent` | a point event at `OriginatingTime`, produced by `ParticipantId`, optionally towards `TargetId`, with an `Intensity` (1 by default) and a `Label` | TurnTaking, JointVisualAttention, GazeOnPeers, TaskParticipation, Formation |
| `double` | a distance between two participants | Proximity |
| `bool` | an attention state | AttentionLevel |

`IndexAdapters` and `SerializableClassAdapters` convert the SAAC stream types (`PieceStatus`,
`TTData`, `NewJVAData`, `SpeakingTimeIDData`, `TimeData`...) into these, including the
participant numbering of each source (`ParticipantIdMap`).

### Detectors

The indicators count events and intervals; they do not find them in the raw streams. The
detectors do, and publish directly the types above, so that their outputs are piped to the
inputs of the indicators without adapter. They work for any number of participants and only
read the time of the data.

| Detector | Reads | Publishes | Goes to |
|---|---|---|---|
| `TaskPhaseDetector` | task events and puzzle status of the task server | start and end of each puzzle | `indices.Gate.PhaseStartIn` / `PhaseEndIn` |
| `AreaPresenceDetector` | enter and leave events of the areas | presence intervals (open, then closed), current area | `TimeInArea` |
| `FFormationDetector` | position and forward direction of the heads | distance of each pair; formations (face to face, L shape, side by side) with 1 s of hysteresis | `Proximity`, `Formation` |
| `VerbalizationDetectorComponent` | transcriptions | speech intervals | `VerbalParticipation`, `Silence`, `CrossTalk` |
| `TurnTakingDetector` | speech intervals | turn takings with and without overlap, overlaps | `TurnTaking` |
| `GazeEpisodeDetector` | gaze events of the eye trackers | looks at objects, looks at peers (at least 200 ms) | `GazeOnPeers`, `AttentionLevel`, and the next two |
| `JointVisualAttentionDetector` | looks at objects, optionally the head poses | joint looks at the same object within 2 s, initiator and responder | `JointVisualAttention` |
| `MutualGazeDetector` | looks at peers, as intervals | mutual gazes: a look of A at B and a look of B at A that overlap | `MutualGaze` |
| `TaskActionDetector` | piece statuses, generator interactions, current area | task actions labelled "object@place" | `TaskParticipation` |
| `ObjectHandoverDetector` | task actions | handovers (release then grab by another within 2 s) | no indicator yet |

`Applications/Expe2Reprocessing` is a complete program that wires them on a recorded session
of the puzzle task: `SessionStreams` opens the stores, `CollaborationPipeline` declares the
detectors and the indices and connects them.

A session recorded with its audio only has no transcription for `VerbalizationDetectorComponent`
to read. `SpeechFromAudio` (folder `Verbal`) computes it, with the components the reprocessing
of the studies uses (`VadWhisper`): `VoiceActivity` runs the system voice activity detector on
the audio of a participant, `Transcription` runs Whisper on what that voice activity marks as
spoken and gives one final result per utterance. The audio must be 16 kHz, one channel, 16 bit
PCM (`AudioResampler` of `Microsoft.Psi.Audio.Windows` converts it). The model is set by
`SpeechFromAudioConfiguration`: folder of the models, model (`TinyEn` by default) and language
(`English`); Whisper loads it when the pipeline starts and downloads it into the folder when
it is not there.

```csharp
IProducer<bool> voiceActivity = SpeechFromAudio.VoiceActivity(pipeline, server, audio, 0);
var transcription = SpeechFromAudio.Transcription(pipeline, server, audio, voiceActivity, 0, whisperSettings);
transcription.PipeTo(verbalization.GetSttInput(0));
```

Whisper transcribes an utterance once it has ended and takes from a fraction of a second to a
few seconds: such a pipeline is to be run at the pace of the session, live or replayed in real
time. Two runs do not give exactly the same utterances.

`AudioStreamAssignment.Resolve(availableStreams, requested, participants)` says which audio
stream is the one of which participant without reading anything into the names: the streams
are numbered from 1 in the natural order of their names (`Audio_2` before `Audio_10`), and an
assignment is given by id or by name. It does not guess: with more or fewer streams than
participants and no instruction, it is not resolved and `Problem` says why.

The detectors replace components of the *CollaborationAnalysisFramework* repository. They were
checked on a recorded session against what those components had stored: same task actions and
same looks at peers, event for event; time in area, gaze on peers and verbal participation
indices close to the stored ones (correlation 0.98 to 1.00, one gaze series at 0.88). Three of
them deliberately do not reproduce the original, whose result depended on a defect; each class
comment says how:

- **Joint visual attention.** The original required the two participants to look at the same
  zone, but computed it from the components of the head quaternion read as angles: the test
  kept about one candidate in six without measuring where the heads pointed. It also named
  initiator the participant with the lowest index. The detector finds the stored episodes and
  about six times more without head test, about four and a half times more with it
  (`RequireConvergingHeads`). **The reference values of the calibration (section 4.2) were
  measured with the original and do not apply to these counts.**
- **Turn taking.** The original rules could not be reproduced. The detector finds every stored
  turn taking with overlap and overlap, and about twice as many turn takings without overlap.
  It was checked on simulated conversations whose turns are known by construction (smooth
  switches, overlapping switches, backchannels, two to four participants, about 1 600 events
  per group size): every event is found and none is invented
  (`TurnTakingScenarioTests`). A speech interval is received when it ends, so a backchannel
  is received before the longer utterance it was said over and looks, alone, like a turn
  taking; the detector therefore holds a turn taking back until the longer utterance can no
  longer come (`ConfirmationDelay`, 5 s). Without that delay three backchannels in four are
  also counted as turn takings (15 of the 100 turn takings without overlap of the recorded
  session). The delay must cover the longest speech intervals (5.9 s in that session): the
  events are published that much later, with the time they happened.
- **Distances and formations.** The original refreshed the position of one participant per
  second: its distances differ from the true ones by 0.3 m (median) and up to 2.9 m.

A store recorded with the `SerializableClass` assembly is read with the SAAC types after
`PsiStore.Open(...).WithLegacyTypes()` (`LegacyStoreTypes`). The replay of the server does the
same for the gaze events and the piece statuses, whose types are in `PsiFormats`
(`DatasetLoader`, in `PipelineServices`); the generator interactions and the puzzle statuses,
whose SAAC types are in this component, are not read there.

### Collaboration profiles

`CollaborationProfilesComponent` (folder `Profiles/`) reads the snapshot of the indices and
publishes, at each tick, the profile of each pair and of the group:

```csharp
var profiles = new CollaborationProfilesComponent(pipeline, new CollaborationProfilesConfiguration { ParticipantIds = ids });
indices.SnapshotOut.PipeTo(profiles.SnapshotIn);
indices.Get(Indicators.JointVisualAttention).Component.LeadVisualAttentionByPairOut.PipeTo(profiles.LeadVisualAttentionByPairIn);
```

For a pair, seven criteria are placed in ranges (speech equality, turn takings with overlap,
joint visual attention, task equality, verbal participation, formations, synchrony). Each of
the seven profiles (everything/nothing, independent solitary, independent sociable,
leader/follower, teacher/student, turn takers accurate and non accurate) expects a range for
each criterion and has a condition; its confidence is the share of criteria in their range,
with bonuses, and the pair takes the profile of highest confidence when it reaches 0.5
(`CollaborationProfileRules.Evaluate`). The group is undetermined, individual, hierarchical,
balanced or mixed according to the families of its pairs (`CollaborationProfileRules.Merge`).
The component can write three files: the confidences, the profiles of each tick, and the
duration of each group profile.

The rules are those of `UpdateConfidenceOnCollaborationProfiles_MultipleUsers` and
`CollaborationProfileMerging`, with two defects of the original not kept
(`ReproduceLegacyDefects` brings them back): the two turn takers profiles were never evaluated
(their confidence was always 0), and the profile published for a pair was the one *before*
the best in the enumeration (leader/follower came out as independent sociable,
everything/nothing as none).

---

## 2. The builder

### 2.1 Life cycle

```csharp
// 1. Declare
var builder = new CollaborationIndicesBuilder(pipeline)
    .WithParticipants(0, 1, 2)
    .WithWindow(TimeSpan.FromSeconds(20))
    .WithDataClock(headPositions[0])
    .AddIndicator(Indicators.Movement)
    .AddIndicator(Indicators.Synchrony, o => o.SubsetSize = 3)
    .AddIndicator(Indicators.VerbalParticipation)
    .AddIndicator(Indicators.SpeechEquality)
    .WithCollaborationScore()
    .WithCsvExport("indices.csv");

// 2. Build: validates everything, then creates the components
SlidingAverageComputation indices = builder.Build();

// 3. Connect the source streams to the inputs of the indicators
foreach (uint id in indices.ParticipantIds)
{
    headPositions[id].PipeTo(indices.Get(Indicators.Movement).Component.GetPositionInput(id, BodyPartNames.Head));
    speech[id].PipeTo(indices.Get(Indicators.VerbalParticipation).Component.GetIntervalInput(id));
}

// 4. Run the pipeline
pipeline.RunAsync();
```

Nothing is added to the pipeline before `Build()`. A builder can be built several times and
cloned (`Clone()`), which is how several windows are computed side by side.

### 2.2 Builder methods

| Method | Effect | Default |
|---|---|---|
| `WithName(name)` | prefix of every component and stream name | `CollaborationIndices` |
| `WithParticipants(ids)` | participants of the session, any number, contiguous or not | none (required) |
| `WithWindow(duration)` | sliding window of every indicator | 30 s |
| `WithComputationInterval(interval)` | period of the clock, i.e. publication period | 1 s |
| `WithCalibration(calibration)` | reference values of the normalisation | `IndexCalibration.ForWindow(window)` |
| `WithPhaseGate(requirePhase, log)` | whether the indices only run inside a phase | `false` |
| `WithClock(ticks)` | drive the indices from a tick stream | connect `ClockIn` by hand |
| `WithDataClock(stream)` | ticks derived from a dense data stream | |
| `WithInternalClock()` | a generator on the pipeline clock (live only) | |
| `WithStore(server, session, store)` | store the streams the components have always stored | nothing stored |
| `AddIndicator(...)` | declare an indicator, see below | none (at least one required) |
| `WithCollaborationScore(dimensions, configure)` | add the score | not computed |
| `WithInteractionGraph(configure)` | add the graph | not computed |
| `WithCsvExport(writer or path, columns, configure)` | add the export | not written |
| `WithMaximumPendingTicks(n)` | safety limit of the snapshot assembler | 600 |
| `Validate()` | the list of problems, without throwing | |
| `Build()` | one instance | |
| `BuildSet(windows)` | one instance per window, on shared clocks | |
| `ToConfiguration()` | the declaration as a serializable object | |

### 2.3 Declaring an indicator

Three equivalent forms:

```csharp
// By catalogue entry: typed options, no class name to know.
builder.AddIndicator(Indicators.Synchrony, o => o.SubsetSize = 3);

// By name: what a configuration file does.
builder.AddIndicator("Synchrony", new Dictionary<string, object> { { "SubsetSize", 3 } });

// By class: for an indicator that is not registered in a catalogue.
builder.AddIndicator<MyIndicator>(i => i.Options.Threshold = 0.5);
```

Options common to every indicator (`IndicatorOptions`):

| Option | Meaning |
|---|---|
| `Name` | name of the indicator and of its outputs; set it to declare the same kind twice |
| `WindowDuration` | a window for this indicator only; null uses the window of the builder |
| `IncludeInScore` | false keeps the indicator out of the score, while still computing and exporting it |
| `Configure` | (code only) an action on the full component configuration, for the settings that are not exposed as options: normalizers, synchrony measure, aggregators... |

The name of a catalogue entry is used everywhere: it is the `Type` in a configuration, the
default name of the indicator, the name of its outputs in `indices.Outputs` and in a snapshot,
the prefix of its CSV columns, and the key of its reference value in the calibration.

### 2.4 Configuration object

```csharp
CollaborationIndicesConfiguration configuration = CollaborationIndicesConfiguration.Load("indices.json");
SlidingAverageComputation indices = CollaborationIndicesBuilder
    .FromConfiguration(pipeline, configuration)
    .WithDataClock(headPositions[0])      // streams, stores and writers are code, not data
    .Build();
```

```json
{
  "Name": "Indices",
  "ParticipantIds": [ 0, 1, 2 ],
  "WindowDuration": "00:00:20",
  "ComputationInterval": "00:00:01",
  "RequirePhase": false,
  "Indicators": [
    { "Type": "Movement", "Options": { "BodyParts": [ "Head", "LeftHand", "RightHand" ] } },
    { "Type": "Synchrony", "Options": { "SubsetSize": 3 } },
    { "Type": "VerbalParticipation" },
    { "Type": "SpeechEquality" },
    { "Type": "Equality", "Options": { "Name": "MovementEquality", "Source": "Movement" } }
  ],
  "ComputeCollaborationScores": true,
  "GenerateGraph": false,
  "ExportPath": "indices.csv"
}
```

| Property | Type | Meaning |
|---|---|---|
| `Name`, `ParticipantIds`, `WindowDuration`, `ComputationInterval` | | as on the builder |
| `UseInternalClock`, `RequirePhase`, `LogGateTransitions` | bool | as on the builder |
| `Calibration` | `{ ReferenceValues, ReferenceScore }` | null uses the calibration of the window |
| `Indicators` | list of `{ Type, Options }` | `Options` are the properties of the options class |
| `ComputeCollaborationScores`, `Dimensions` | bool, list | null `Dimensions` uses the default model |
| `GenerateGraph` | bool | |
| `ExportPath`, `ExportColumns` | string, list | null columns exports every declared output |

Durations are written `"hh:mm:ss"`, enumerations by name. A list in the file replaces the default
list. A misspelled property is an error. `builder.ToConfiguration().Save(path)` writes the
declaration of a builder, which is a convenient trace of what produced a result file.

### 2.5 Validation

`Build()` checks the whole declaration first and throws one
`CollaborationIndicesConfigurationException` listing every problem (`Errors`):

- window or computation interval not strictly positive, including the window of one indicator;
- no participant, or a participant declared twice;
- no indicator; the same indicator declared twice without distinct names;
- an indicator whose dependency is not declared (`SpeechEquality` without
  `VerbalParticipation`), is of the wrong kind, or a cycle of dependencies;
- an unknown indicator type, an unknown or ill-typed option (the valid ones are listed);
- a constraint of an indicator (two participants for a relational index, at least one body part,
  a sub-group size of 0 or at least 3);
- a score without dimension or with two dimensions of the same name;
- an export without destination; several clocks declared.

### 2.6 The built instance

| Member | Content |
|---|---|
| `Get(Indicators.X)`, `Get<T>(name)`, `Find<T>(name)`, `Contains(name)` | the indicators; `.Component` gives the inputs |
| `Indicators` | the indicators, each one after those it depends on |
| `Outputs` | every declared output by level: `Group(name)`, `Individual(name)`, `Pair(name)`, `DirectedPair(name)`, `ScoreInput(name)` |
| `Gate`, `ClockIn` | phase gate (`PhaseStartIn`, `PhaseEndIn`) and clock input |
| `SnapshotOut` | every index of each tick in one message |
| `CollaborationScore`, `Graph`, `Export`, `Out` | the fusion components, null when not asked for |

### 2.7 Several windows

```csharp
SlidingAverageComputationSet set = builder.WithName("Indices").BuildSet(
    TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(45));

set.ForEach(indices => /* connect the shared source streams */);
set.OfSeconds(20).SnapshotOut.Do(...);
```

Each instance is named after its window (`Indices_20s`), gets the calibration measured for that
window, and shares the clocks with the others so that their rows line up. An overload takes one
`TextWriter` per window for the CSV exports.

### 2.8 Previous API

`new SlidingAverageComputation(pipeline, server, SlidingAverageConfiguration)` and the matching
`SlidingAverageComputationSet` constructor still work and are marked obsolete. The `Use...Indices`
flags are translated into builder calls by `Legacy/LegacyIndices.cs`; the table is in the README.
This translation reproduces the selection that was hard coded before the builder: no turn
taking, and joint visual attention, gaze on peers and speech equality computed but kept out of
the score. It is therefore **not** the five-dimension score of section 6; declare the indicators
with the builder to obtain it.

### 2.9 Session configuration: the template of an application

`CollaborationSessionConfiguration` (`Builder/CollaborationSessionConfiguration.cs`) is what an
application computes for one session: for whom, on which windows, which indicators, with or
without scores and profiles. A console application reads it from a JSON file; the server fills
it from its interface. Both then build the same way.

```json
{
  "DatasetPath": "C:\\path\\to\\session",
  "OutputFolder": "C:\\path\\to\\results",
  "RealTime": false,
  "Name": "Indices",
  "ParticipantIds": [ 0, 1, 2 ],
  "Windows": [ "00:00:20", "00:00:30", "00:00:45" ],
  "ComputationInterval": "00:00:01",
  "RequirePhase": true,
  "Indicators": [
    { "Type": "Movement" },
    { "Type": "VerbalParticipation" },
    { "Type": "SpeechEquality" },
    { "Type": "TurnTaking", "Options": { "Categories": [ "TurnTakingWithoutOverlap" ] } }
  ],
  "ComputeCollaborationScores": true,
  "ComputeProfiles": true,
  "Detectors": {
    "Frame": "Unity",
    "HeadForwardAxis": "+Y",
    "TurnTakingConfirmationDelay": "00:00:05",
    "MinimumGazeDuration": "00:00:00.2000000",
    "RequireConvergingHeads": true,
    "FormationTransitionDuration": "00:00:01"
  }
}
```

| Property | Meaning |
|---|---|
| `DatasetPath`, `OutputFolder`, `RealTime` | where a recorded session is, where the result files go, replay at the original pace or as fast as possible |
| `Name`, `ParticipantIds`, `ComputationInterval`, `RequirePhase` | as on the builder |
| `Windows` | one instance of the indices per window (section 2.7) |
| `Indicators` | list of `{ Type, Options }`, as in section 2.4 |
| `ComputeCollaborationScores` | dimension scores and collaboration score (section 6) |
| `ComputeProfiles` | profiles of the pairs and of the group, one component per window |
| `Detectors` | the detector settings an application may want to change; the others keep their defaults |

Two settings of `Detectors` describe the positions and the head rotations of the session:

| Setting | Values | Effect |
|---|---|---|
| `Frame` | `Unity`: left-handed, Y up, what the Unity server streams. `Standard`: right-handed, Z up, the Unity frame with Y and Z swapped, what `PositionOrientationPreProcessing` publishes | the vertical axis used by the formations (`UpAxis()`), and the frame the direction of a head is expressed in (`ToHeadPose`) |
| `HeadForwardAxis` | `+X` ... `-Z` | the local axis of the head transform that points where the participant looks: `+Z` for a Unity camera, `+Y` for the head of the avatars of the Unity server |

The rotation of a head is its Unity Euler angles in both frames (the standardised streams keep
them and only swap the position). With `+Y`, the direction is the one of
`PositionOrientationPreProcessing.Convert`: the local +Z axis of its reframed rotation is the
local +Y axis of the Unity transform, with Y and Z swapped. A session gives the same formations
in either frame, provided the frame declared is the one of the data; declared in the wrong one,
the height is taken for a horizontal axis. Distances, movement and synchrony do not depend on
the frame.

| Member | Use |
|---|---|
| `Template(participantCount)` | every indicator that has a detector, three windows, scores and profiles: a file to start from |
| `Load(path)`, `Save(path)`, `FromJson`, `ToJson` | a misspelled property is an error |
| `Add(name, options)`, `Has(name)` | declares an indicator once, with the indicator it is computed from (`SpeechEquality` brings `VerbalParticipation`) |
| `Validate()` | the problems that do not need a pipeline: no participant, no window, a window declared twice, no indicator, profiles for fewer than two participants |
| `MissingProfileIndicators()` | the indicators the profiles read that are not declared; they count as 0 |
| `CreateBuilder(pipeline)` | a builder with the participants, the period, the phases, the indicators and the score |
| `CreateProfiles(pipeline, window, indices, writers)` | the profile component of one window, connected to the indices of that window |

What is specific to an application stays in the application: where its streams are and how
they are connected. The skeleton is the same for all of them:

```csharp
CollaborationSessionConfiguration configuration = CollaborationSessionConfiguration.Load(path);
configuration.Save(Path.Combine(configuration.OutputFolder, "configuration.json"));   // what produced the files

// 1. Detectors, from the raw streams of the application.
var speech = new VerbalizationDetectorComponent(pipeline, new VerbalizationDetectorConfiguration { ParticipantIds = configuration.ParticipantIds });
var turnTakings = new TurnTakingDetector(pipeline, new TurnTakingDetectorConfiguration
{
    ParticipantIds = configuration.ParticipantIds,
    ConfirmationDelay = configuration.Detectors.TurnTakingConfirmationDelay,
});

// 2. Indices: everything declared comes from the configuration; the clock and the writers from the application.
SlidingAverageComputationSet indices = configuration.CreateBuilder(pipeline)
    .WithDataClock(headPositions[0])
    .BuildSet(writerOfEachWindow);

// 3. An indicator that is not declared is not computed: connect an input only when it is there.
indices.ForEach(window =>
{
    if (window.Contains(IndexNames.TurnTaking))
    {
        turnTakings.Out.PipeTo(window.Get(Indicators.TurnTaking).Component.EventIn);
    }
});

// 4. Profiles, one component per window.
if (configuration.ComputeProfiles)
{
    indices.ForEach((window, instance) => configuration.CreateProfiles(pipeline, window, instance, writersOf(window)));
}
```

Two applications follow it:

| | `Applications/Expe2Reprocessing` | `Applications/ServerApplication` |
|---|---|---|
| Configuration | `expe2.json`, or `--config file.json`; `--template file.json` writes one to edit | built from the "Process" tab (`MainWindow.BuildCollaborationConfiguration`) |
| Streams | `SessionStreams`: the stores of a recorded session of the puzzle task | `GatherProducers.FindProducers`: the connectors of the live server or of a replayed dataset, looked up by stream name |
| Pipeline | `CollaborationPipeline` | `Examples/CollaborationIndicesProcess` |
| Missing stream | an error: the dataset layout is known | the indicators that need it are left out and reported in the log |
| Results | the output folder | `<dataset path>/CollaborationIndices/<session id>_<date>_<time>/`, with `configuration.json` |

In the server, the "Process" tab reads from left to right: 1. the metrics to compute,
2. the computation settings, 3. the live session and 4. the recorded dataset, each with its
start and stop buttons, then the log of what was done. It gives the configuration as follows.
The same selection applies to "Start Process" (live session) and to "Start Post Processing"
(replay of the dataset).

| Interface | Configuration |
|---|---|
| Turn-taking with overlap, without overlap | `TurnTaking`, with the checked categories |
| Speech participation, Speech equality | `VerbalParticipation`, `SpeechEquality` |
| Silence, Cross-talk | `Silence`, `CrossTalk` |
| Joint Visual attention, Gaze on peers, Mutual gaze | `JointVisualAttention`, `GazeOnPeers`, `MutualGaze` |
| Task participation, Task equality | `TaskParticipation`, `TaskEquality` |
| Physical activity level, Physical synchrony score | `Movement`, `Synchrony` |
| Physical Proximity, Facing-Formation | `Proximity`, `Formation` |
| Participants | `ParticipantIds`, numbered from 0 |
| Data frame | `Detectors.Frame`: "Unity (Y up, left-handed)" or "Standard (Z up, right-handed)"; `Detectors.HeadForwardAxis` is `+Y`, the head of the avatars |
| Audio streams, Whisper models | not in the configuration: how the speech is computed from the audio when the session did not record it (below) |
| Sliding Windows Computation and its list | `Windows`. Unchecked, or with an empty list, there is no window: no index, score or profile is computed, and the log says so |
| Collaboration Profile Computation | `ComputeProfiles`, and the indicators the profiles read are declared with their default options |
| Collaboration Score Computation | `ComputeCollaborationScores`: the score of the selected metrics (section 6.4); the log names the indices of the score that are not selected |

The log of the server follows each step: what was selected, the streams found and the store
each one is read from, the streams left out, the detectors created and their settings, what
paces the indices, the result files, then the first message of each stream and of each
detection as it arrives. When the process stops it reports the number of messages received per
participant for each stream, the streams that stayed silent, and what was produced: detections,
rows of indices and of profiles. For a replay it also says how many streams of the dataset are
read, which ones with a type of the application, and which ones are left out.

**Speech of the session, or computed from its audio.** The conversational metrics read the
transcription of each participant (streams `STT_1`, `STT_2`...), and the voice activity
(`VAD_1`, `VAD_2`...) lets a turn taking be confirmed while nobody speaks. The process
(`RealTimeProcessingUseCase.GatherSpeech`) first looks for these streams, in a live session and
in a replayed dataset alike, and says in the log which ones it found:

| The session has | A conversational metric is selected | What the process does |
|---|---|---|
| voice activity and transcription | yes or no | uses them as recorded; nothing is computed from the audio |
| one of them, or none | no | nothing: what is missing is not computed |
| one of them, or none | yes | computes what is missing from the audio of each participant, with `SpeechFromAudio`, and stores it in the dataset; what is recorded is used as it is |

The audio streams are found by their type, whatever they are named: by number (`Audio_1`), by
colour (`Audio_User_red`) or otherwise. `AudioStreamAssignment` (folder `Verbal` of the
component) numbers them from 1 in the natural order of their names, a stream that several
stores hold counting once. That number is the id of the stream, and the log lists the ids with
the number of buffers recorded:

```
Speech: audio streams found, numbered in the order of their names: 1 = Audio_User_red (82144 buffers), 2 = Audio_User_yellow (82143 buffers).
Speech: audio of the participants, one stream each in the order of the ids: participant 1 = Audio_User_red (audio 1), participant 2 = Audio_User_yellow (audio 2).
```

| "Audio streams" | Audio of the participants |
|---|---|
| empty, one audio stream per participant | audio 1 is participant 1, audio 2 is participant 2... |
| empty, another number of audio streams | not guessed: nothing is computed, the log lists the ids to choose from |
| `2;1` | by id, in the order of the participants: audio 2 is participant 1, audio 1 is participant 2 |
| `Audio_User_yellow;Audio_User_red` | the same by name; ids and names can be mixed |

Nothing in a session says which audio stream belongs to which participant of the other streams
(`1-Head`, `1-Interactions`...): the order of the ids is a convention. It does not change the
indices of the group (silence, cross-talk, turn takings, speech equality); it decides which
participant a speaking time is attributed to, hence the values per participant and the
profiles, so it is to be checked once per recording setup. "Whisper models" is the folder of the
models. Both fields are saved with the configuration of the application. When the audio cannot
be assigned or when the folder of the models does not exist, nothing is computed from the
audio, the conversational metrics are left out and the log gives the reason.

**What is computed is stored in the dataset**, so that it is computed once. The voice activity
and the transcription computed from the audio are written under the names a session records
them, `VAD_1`, `STT_1`..., in a store `SpeechFromAudio` of the session
`RawDataPipelineProcess.000` (folder `<dataset>/RawDataPipelineProcess.000/SpeechFromAudio.0000`),
and the dataset file lists that store. The next replay finds the streams by their names, reads
them, and neither the detector nor Whisper runs. Next to the store, `SpeechFromAudio.json` says
how it was obtained:

```json
{
  "Store": "SpeechFromAudio",
  "ComputedOn": "2026-10-08 18:44",
  "Streams": [ "VAD_1", "VAD_2", "STT_1", "STT_2" ],
  "AudioOfParticipants": [ "Audio_User_red", "Audio_User_yellow" ],
  "VoiceActivity": "system voice activity detector",
  "Transcription": "Whisper TinyEn, English",
  "Complete": true
}
```

| Situation | What happens |
|---|---|
| the replay reads the dataset to its end | the store is marked `Complete`; the window disposes the pipeline, which closes the store; the next replays read it, and the log recalls where it comes from |
| the replay is stopped before the end ("Stop", or the window is closed) | the store only holds the beginning of the session: it stays marked `"Complete": false`, the next replays do not read it and compute the speech again, in a store `SpeechFromAudio_2`; its folder can be deleted |
| the speech must be computed again (another assignment of the audio, another model) | delete the folder `SpeechFromAudio.0000`: its streams are no longer found, and the next replay computes and stores them again |
| a live session | the store is written in the session of the process, and is complete when the process stops |

`RealTimeProcessingUseCase.StoreSpeechFromAudio = false` computes without storing. The stored
transcription holds what the participants said, like the `STT` streams a session records.
A replay that reads the stored streams gives the speech indices of the run that computed them
(checked row by row on a recorded session), and no longer depends on Whisper: it also fixes
the utterances, which two computations from the audio do not find exactly alike.

A metric of a category whose box is unchecked is not computed. The saved `configuration.json`
lists what was actually computed, after the indicators without streams were left out; it is in
the format the console applications read.

---

## 3. The time window

### 3.1 Definition

Every index describes the last `W` seconds of data, where `W` is the window duration. A
computation is triggered by a **tick** of the clock. If `T` is the originating time of the tick,
the window is the closed interval

```
[T - W, T]
```

and the index is published with the originating time `T`. Nothing reads the clock of the
machine: a store replayed in a few seconds produces the indices of the recorded session, with
the timestamps of the recorded session.

What "inside the window" means depends on the kind of data:

| Data | Rule |
|---|---|
| Event | counted when `T - W <= event.OriginatingTime <= T`, with its `Intensity` as weight |
| Interval | contributes the duration of its intersection with the window, `max(0, min(End, T) - max(Start, T - W))`. An interval that is still open is cut at `T` |
| Position | the samples whose timestamp is in the window; the movement is measured between the first and the last of them |

The time used for an event or an interval is the one it carries (`OriginatingTime`,
`StartTime`, `EndTime`), not the time of the message that delivered it. A source may therefore
republish its whole history at each tick, as the legacy queues do: the stores recognise what
they already hold (an event by participant, category, time, target and label; an interval by its
start and target, so an interval received open and later closed is updated in place).

An interval or an event only counts once it has been **received**. A speech interval produced by
a recogniser at the end of an utterance does not exist for the ticks that happen during the
utterance. This is inherent to the sources and identical live and in replay.

### 3.2 Storage and pruning

Each indicator keeps its own buffer, because they hold different data fed by different streams.
Before each computation the data older than `T - (W + RetentionMargin)` is dropped
(`RetentionMargin` is 2 s for events and intervals, 1 s for positions), so memory is bounded by
the window, whatever the length of the session. Open intervals are never dropped.

Positions use a cumulated travelled distance maintained when the samples arrive: the distance
over any window costs two binary searches, independently of the window length and of the sensor
rate. Late samples are inserted at their place.

### 3.3 The clock

All the indicators of an instance are paced by the same tick stream, so they are published at
the same instants and can be compared and merged tick by tick.

| Source | Use |
|---|---|
| `WithDataClock(stream)` | one tick each time `ComputationInterval` of *data time* has elapsed on a dense stream (head positions). The ticks are on the timeline of the data by construction. Recommended, in particular for replays |
| `WithClock(ticks)` or `indices.ClockIn` | any tick stream |
| `WithInternalClock()` | `Generators.Repeat` on the clock of the pipeline, for live pipelines |

`DataClock.FromStream` starts its grid on the first message and emits a tick on the first message
that is at least one interval later than the previous tick. The period is therefore the interval
rounded up to the next sample.

### 3.4 Phase gate and warm-up

The clock goes through `PhaseGateComponent` before reaching the indicators:

- after the start, the gate stays closed for one window (the longest one when indicators have
  their own): an index computed on a window that is not filled yet would be meaningless;
- with `RequirePhase`, the gate only opens inside a phase, i.e. after a message on
  `Gate.PhaseStartIn`, again after one window of warm-up, and closes on `Gate.PhaseEndIn`. This
  is how the indices are restricted to the puzzles or structures of a session, and why no index
  mixes the data of two phases;
- without `RequirePhase` (default of the builder), the first tick starts the session.

`Gate.Out` is the gated tick, `Gate.EnabledOut` the state of the gate, `Gate.PhaseIdOut` the
index of the current phase.

### 3.5 Per-indicator and additional windows

- `options.WindowDuration` gives one indicator its own window; the others keep the window of
  the builder. The warm-up uses the longest.
- `Movement` can compute further windows on the same buffer (`AdditionalWindows`, for instance
  5 s next to 20 s), each one on its own stream
  (`Component.GetWindowedActivityLevelsEmitter(window)`).
- `BuildSet` computes complete instances for several windows (section 2.7).

### 3.6 Ticks, data and reproducibility

\psi delivers each stream in order but does not synchronise two streams. Two consequences:

1. **Late data.** A message stamped before `T` but delivered after the tick `T` is not in the
   window of `T`; it enters the next one. Live, this concerns at most the very last samples. In
   a replay that is not paced by the clock and runs on several threads it can concern more, and
   two runs of the same store can differ slightly. A message stamped *exactly* `T` is in or out
   depending on delivery order. For an offline analysis that must be exactly reproducible, run
   the indices in a single threaded pipeline (`Pipeline.Create(threadCount: 1)`); the tests and
   the example do, and obtain identical files run after run.
2. **Merging indices.** The values of the different indicators for one tick reach a consumer as
   separate messages in no guaranteed order. The score, the graph and the export therefore do
   not read the indicators directly: see the snapshot in section 7.1.

---

## 4. Normalisation and calibration

### 4.1 Function

Count-like indices are mapped to `[0, 1)` by a saturating exponential ([B], Table 4):

```
alpha = -ln(1 - s_ref) / x_ref
s(x)  = 1 - exp(-alpha * x)
```

`x_ref` is a reference value of the raw index, the 95th percentile observed on a corpus, and
`s_ref` the score it must map to, 0.95. The function rises quickly for the frequent low values
and saturates for the rare high ones, so an outlier cannot dominate; it makes no assumption on
the distribution, and scores become comparable across indicators, windows and sessions.

Example, 20 s window, joint visual attention (`x_ref` = 8): `alpha = -ln(0.05) / 8 = 0.374`;
three episodes in the window give `1 - exp(-1.12) = 0.67`, eight give 0.95.

In code: `ExponentialSaturationNormalizer.FromReference(x_ref, s_ref)`. Other normalizers exist
(`IdentityNormalizer`, `RatioNormalizer`, `EqualityNormalizer`) and any `IIndexNormalizer` can be
injected through the `Configure` hook of an indicator.

### 4.2 Reference values

`IndexCalibration` holds `x_ref` by index name and `s_ref` (0.95). The values shipped with the
library are the 95th percentiles measured on a corpus of 27 sessions, zeros excluded:

| Index | 20 s | 30 s | 45 s |
|---|---|---|---|
| `JointVisualAttention` (group) | 8 | 11 | 18 |
| `JointVisualAttentionPair` | 5 | 8 | 11 |
| `GazeOnPeers` (group) | 7 | 10 | 14 |
| `TaskParticipation` (group) | 18 | 27 | 32 |
| `Formation` (group and pair) | 4 | 5 | 8 |
| `TurnTakingWithOverlap` | 2 | 2 | 3 |
| `TurnTakingWithoutOverlap` (group) | 4 | 5 | 5 |
| `TurnTakingWithoutOverlapPair` | 3 | 4 | 3 |
| `Movement` | 0.041 | 0.041 | 0.037 |
| `VerbalParticipation` | 20 | 30 | 45 |

`IndexCalibration.ForWindow(window)` returns the table of the window, or of the closest one
(below 25 s the 20 s table, below 35 s the 30 s table, otherwise the 45 s table). **A reference
value is only meaningful for the window, the task and the sampling it was measured on**: for
another window or another study, measure the percentiles on your data and pass them with
`WithCalibration(new IndexCalibration { ReferenceValues = ... })`. An index without reference
value is left as it is (identity).

The last two rows are not applied by the current code, see section 8.

---

## 5. Indicators

Notation: `P` is the set of participants, `N` their number, `W` the window in seconds, `T` the
tick. "Group", "individual", "pair" and "directed pair" are the levels at which an output is
declared; they are the keys `Group`, `Individual`, `Pair` and `DirectedPair` of a snapshot.
"Score input" is the value handed to the collaboration score under the name of the index.

Sources of the indicators in the literature are those listed in Table 1 of [A] and Table 2 of
[B]; see the papers for the full references.

### 5.1 Movement (physical activity level)

*Catalogue entry `Indicators.Movement`, class `PhysicalActivityIndicator`, component
`PhysicalActivityLevelComponent`. Behavioural engagement. After Won et al. (2014).*

**Input.** Positions of the tracked body parts of each participant
(`GetPositionInput(id, bodyPart)` or `GetBodyInput(id)`), by default head, left hand, right hand.

**Computation.** For a participant `p` and a body part `b`, let `s_1 ... s_n` be the samples
whose timestamp is in the window.

```
distance(p, b) = sum over i of || s_i - s_(i-1) ||            (travelled path)
movement(p, b) = distance / (t_n - t_1)        unit DisplacementPerSecond (default), in m/s
               = distance / (n - 1)            unit DisplacementPerSample, in m per sample
activity(p)    = sum over b of w_b * movement(p, b) / sum over b of w_b
```

with the weights `w` = 0.4 for the head and 0.3 for each hand. A body part with fewer than
`MinimumSampleCount` (2) samples contributes 0. The speed is measured over the time actually
covered by the samples, so a participant tracked for part of the window gets the speed observed
while tracked.

**Outputs.**

| Level | Name | Value |
|---|---|---|
| individual | `Movement` | `activity(p)` |
| group | `Movement` | mean of `activity(p)` over the participants (`GroupAggregator`) |
| score input | `Movement` | the group value, **not normalised** (section 8, item 1) |

Component streams: `Out`, `GroupActivityLevelOut`, one emitter per participant, one per
additional window.

**Options.** `BodyParts`, `BodyPartWeights`, `AdditionalWindows`, `Unit`.

### 5.2 Synchrony (physical synchrony)

*`Indicators.Synchrony`, `PhysicalSynchronyIndicator`, `PhysicalSynchronyComponent`. Embodied
coordination. After Won et al. (2014) and Miller et al. (2021).*

**Input.** Positions, by default of the head only.

**Computation.**

1. *Resampling.* Every participant is resampled on a common grid of 50 ms
   (`SamplingInterval`), anchored on absolute time. A grid point takes the nearest sample within
   30 ms (`MaxDelta`); without one the point is missing for that participant. Points are
   finalised once they are 30 ms old, so that a slightly late sample is still used.
2. *Movement series.* For each participant, the displacement between two consecutive grid
   points, divided by the time between them (weighted over the body parts when several are
   configured).
3. *Common support.* By default (`RequireAllParticipants`) only the grid points where every
   participant has a value are kept, so the series are aligned.
4. *Pairs.* For each pair, the Pearson correlation `r` of the two series, 0 with fewer than
   `MinimumSampleCount` (10) points or a constant series.
5. *Normalisation.* `(r + 1) / 2` (`ZeroToOne`): 0 in opposition, 0.5 unrelated, 1 in phase.
   `None`, `Absolute` and `PositiveOnly` are the other choices.
6. *Group.* Mean of the normalised pair scores, which equals `(mean(r) + 1) / 2`. Sub-groups of
   `SubsetSize` participants get the mean of their pairs.

Nothing is published while a participant has fewer than two samples or has not been seen for
2 s (`MaximumSampleAge`): the index would be built on stale data.

**Outputs.**

| Level | Name | Value |
|---|---|---|
| pair | `Synchrony` | normalised score of each pair |
| group | `Synchrony` | mean of the pairs |
| score input | `Synchrony` | the group value |

Component streams: `Out`, `PairCorrelationsOut` (raw `r`), `SubsetSynchronyOut`,
`GroupSynchronyOut`, one emitter per pair.

**Options.** `BodyParts`, `SubsetSize`, `Normalization`, `RequireAllParticipants`; through
`Configure`: `Measure` (`PearsonCorrelationMeasure`, `LaggedCrossCorrelationMeasure`),
`Aggregator` (mean, median, minimum), `SamplingInterval`, `MaxDelta`.

### 5.3 Verbal participation

*`Indicators.VerbalParticipation`, `VerbalParticipationIndicator`,
`VerbalParticipationComponent`. Conversational dynamics.*

**Input.** Speech intervals of each participant, category `Speaking`
(`GetIntervalInput(id)` or `IntervalsIn`).

**Computation.**

```
speaking(p)   = total duration of the intervals of p inside the window, in seconds
ratio(p)      = speaking(p) / W
pair(a, b)    = (speaking(a) + speaking(b)) / (2 W)
group         = sum over p of speaking(p) / (N W)
usable        = group > MinimumRatioForEquality                       (0.15)
```

**Outputs.**

| Level | Name | Value |
|---|---|---|
| individual | `VerbalParticipation` | `ratio(p)` |
| group | `VerbalParticipation` | `group` |
| score input | `VerbalParticipation` | `group`, **a ratio, not the normalised speaking time** (section 8, item 2) |

Component streams: `Out`, `SpeakingTimesOut` (seconds, the distribution read by the equality
and dominance indicators), `PairOut`, `GroupOut`, `EqualityUsableOut`.

**Options.** `AsRatioOfWindow`, `MinimumRatioForEquality`.

### 5.4 Speech equality and task equality

*`Indicators.SpeechEquality`, `Indicators.TaskEquality`, generic `Indicators.Equality`; class
`EqualityIndicator`, component `EqualityIndexComponent`. Dominance. Speaking-time distribution
and symmetry of the contribution to the task.*

**Input.** The per-participant values of another indicator: the speaking times for speech
equality, the task participation counts for task equality, any distribution through
`options.Source`.

**Computation.** The Gini index of the values `x_1 <= ... <= x_n`, normalised by its maximum so
that it is comparable between groups of different sizes:

```
G      = 2 * (sum over i of i * x_i) / (n * sum of x) - (n + 1) / n
G_norm = G / ((n - 1) / n)             clamped to [0, 1]
```

0 means perfectly equal, 1 means one participant does everything. The same formula is applied
to each pair and to each sub-group of `SubsetSize`. When the total is not greater than
`MinimumTotal` (0), the index is undefined and published as -1.

**Outputs.**

| Level | Name | Value |
|---|---|---|
| group | `SpeechEquality` / `TaskEquality` | `G_norm`, the **inequality**, or -1 |
| pair | same | `G_norm` of the pair (graph) |
| score input | same | the **equality** `1 - G_norm`; 0 when undefined |
| validity | same | false when the index is undefined, or when the source reports that it is not usable (verbal participation: `usable` above) |

The published stream and the CSV column hold the inequality, as they always did; the score
uses the equality.

**Options.** `Source`, `SubsetSize`, `MinimumTotal`; through `Configure`: `UndefinedValue`,
`PublishAsEquality`.

### 5.5 Talking most and tasking most

*`Indicators.TalkingMost`, `Indicators.TaskingMost`, generic `Indicators.Dominance`; class
`DominanceIndicator`, component `DominanceIdentityComponent`.*

Identity of the participant with the highest value of the source distribution, published as
`id + 1`, and 0 when the maximum is shared. Computed for the group and inside each pair. It is an
identity, not a quantity: it is exported but does not enter the score.

### 5.6 Turn taking

*`Indicators.TurnTaking`, `TurnTakingIndicator`, `TurnTakingComponent`. Conversational
dynamics. After Gravano and Hirschberg (2011).*

**Input.** Events of the categories `TurnTakingWithOverlap`, `TurnTakingWithoutOverlap` and
`Overlap` (`EventsIn`), the new speaker in `ParticipantId` and the previous speaker in
`TargetId`; optionally `Silence` events whose `Intensity` is a duration in seconds. The detection
of the turn takings from the voice activity is done upstream.

**Computation**, for each category `c`:

```
count(c, p)    = number of events of c of participant p in the window
group(c)       = s_c( sum over p of count(c, p) )
pair(c, a, b)  = s_c,pair( events of c from a towards b, plus from b towards a )
silence        = sum of the durations of the Silence events in the window
```

`s_c` is the normalizer of the category: the calibration for the turn takings with and without
overlap, the identity for the overlaps. The pair level of the turn takings without overlap has
its own reference value.

**Outputs.** Each category is an index of its own, named after the category:

| Level | Name | Value |
|---|---|---|
| group | `TurnTakingWithOverlap`, `TurnTakingWithoutOverlap`, `Overlap` | `group(c)` |
| pair | same | `pair(c, a, b)` |
| group | `Silence` | `silence` |
| score input | same three names | `group(c)`; the default model uses `TurnTakingWithoutOverlap` |

**Options.** `Categories`.

### 5.7 Joint visual attention

*`Indicators.JointVisualAttention`, `JointVisualAttentionIndicator`,
`JointVisualAttentionComponent`. Joint attention. After Richardson and Dale (2005).*

**Input.** One event per episode (`EventsIn`), the initiator in `ParticipantId` and the
responder in `TargetId`. An episode is detected upstream when two participants look at the same
object within plus or minus two seconds of each other ([A], Table 1).

**Computation.**

```
initiated(p) = number of episodes initiated by p in the window
group        = s_JVA( total number of episodes in the window )
pair(a, b)   = s_JVA,pair( episodes between a and b, whoever initiated )
lead         = argmax over p of initiated(p), published as id + 1, 0 on a tie
```

**Outputs.**

| Level | Name | Value |
|---|---|---|
| group | `JointVisualAttention` | normalised number of episodes |
| pair | same | normalised number of episodes of the pair |
| score input | same | the group value |

Component streams also give `InitiatorCountsOut`, `LeadVisualAttentionOut` (who leads visual
attention, after Cheng et al. 2022) and `LeadVisualAttentionByPairOut`.

### 5.8 Gaze on peers

*`Indicators.GazeOnPeers`, `GazeOnPeersIndicator`, `GazeOnPeersComponent`. Joint attention.
After Jayagopi et al. (2012).*

**Input.** One event per gaze episode (`EventsIn`), the gazer in `ParticipantId` and the gazed
peer in `TargetId`, dated at the end of the episode.

**Computation.**

```
gaze(a -> b)  = number of gazes of a on b in the window
group         = s_GoP( sum over all ordered pairs of gaze(a -> b) )
watched(b)    = sum over a of gaze(a -> b)
most watched  = argmax over b of watched(b), published as id + 1, 0 on a tie
```

**Outputs.**

| Level | Name | Value |
|---|---|---|
| directed pair | `GazeOnPeers` | `gaze(a -> b)`, a raw count; `1 -> 2` and `2 -> 1` are distinct |
| group | same | normalised total |
| score input | same | the group value |

Component streams also give `WatchedOut`, `MostWatchedOut` and `MostWatchedByPairOut`.

### 5.9 Attention level

*`Indicators.AttentionLevel`, `AttentionLevelIndicator`, `AttentionLevelComponent`.*

The only indicator that is not a window statistic: an accumulator per participant, updated at
each tick of its **own, faster clock** (`ClockIn` of the indicator):

```
level(p) = clamp( level(p) + Step   if p is attentive
                  level(p) - Step   otherwise,          Floor = 0, Ceiling = 1932 ms )
```

`Step` should be the period of that clock. The attention states come from
`Component.GetAttentiveInput(id)`. The level is published as milliseconds, or as a ratio of the
ceiling. It feeds the graph with its latest value and does not enter the score.

### 5.10 Task participation

*`Indicators.TaskParticipation`, `TaskParticipationIndicator`, `TaskParticipationComponent`.
Behavioural engagement. After Poggi (2007) and Rodrigo et al. (2013).*

**Input.** Task events (`EventsIn`), one per action: categories `Grab`, `Ungrab`, `Place`,
`Color`, `Uncolor`, `GeneratorInteraction`, and `Unplace`. `ToTaskEvents` builds them from
`PieceStatus`, with the object and its location as label.

**Computation.**

```
productive(p)  = number of Grab, Ungrab, Place, Color, Uncolor and GeneratorInteraction of p
inefficient(p) = number of times a Grab of p is directly followed by an Ungrab of p
                 with the same label (same object at the same place)
task(p)        = max(0, productive(p) - 2 * inefficient(p))
group          = s_task( sum over p of task(p) )
interfering(p) = inefficient(p) + number of Unplace of p
```

A grab and release that changes nothing costs the two actions it counted for. Unplacing is not
counted as participation.

**Outputs.**

| Level | Name | Value |
|---|---|---|
| individual | `TaskParticipation` | `task(p)`, a raw count |
| group | same | normalised total |
| score input | same | the group value |

Component streams also give `RawIndividualOut` (the distribution read by task equality and
tasking most), `InefficientActionsOut` and `InterferingParticipationOut`.

**Options.** `InefficientActionPenalty` (2), `ClampToZero`; through `Configure`: `Categories`,
`InefficientPatterns`, `InterferingCategories`.

### 5.11 Time in area

*`Indicators.TimeInArea`, `TimeInAreaIndicator`, `TimeInAreaComponent`.*

**Input.** Presence intervals of each participant (`GetIntervalInput(id)`), category `InArea`,
the area in the label. An interval stays open while the participant is inside.

**Computation.**

```
time(p, area) = min(W, total duration of the presence of p in the area inside the window)
total(area)   = sum over p of time(p, area)
group         = sum over the planning areas of total(area) / (N W)
```

**Outputs.** One individual index per area, `TimeInArea_<area>`, in seconds (or as a share of
the window); the group index `TimeInArea` when planning areas are declared. Not part of the
score.

**Options.** `Areas`, `PlanningAreas`, `AsRatioOfWindow`.

### 5.12 Formation (F-formations)

*`Indicators.Formation`, `FFormationIndicator`, `FFormationComponent`. Embodied coordination.
After Kendon (1976).*

**Input.** One event each time a formation between two participants ends (`EventIn`), category
`FormationEnd`, the two participants in `ParticipantId` and `TargetId`. The formations
themselves (face to face, side by side, L shape, from distance and head orientation) are
detected upstream ([B], section 3.3.3).

**Computation.**

```
pair(a, b) = s_F( number of formations between a and b that ended in the window )
group      = s_F( total number of formations that ended in the window )
```

Sub-groups of `SubsetSize` get the normalised sum of their pairs.

**Outputs.** Pair and group `Formation`; the group value is the score input.

### 5.13 Proximity

*`Indicators.Proximity`, `ProximityIndicator`, `ProximityComponent`. After Hall (1969).*

The latest distance `d` of each pair (`GetDistanceInput(a, b)`), republished on the clock of the
indices as `1 - min(1, d / MaximumDistance)` with `MaximumDistance` = 3 m, or as the raw
distance. It is a sample, not a window statistic. Pair level only; not part of the score.

### 5.14 Silence and cross-talk

*`Indicators.Silence`, `Indicators.CrossTalk`, `SilenceIndicator`, `CrossTalkIndicator`,
`SimultaneousSpeechComponent`.*

Both read the speech intervals of the participants, the same ones as the verbal participation
(`GetIntervalInput(id)`), and measure a time inside the window `[T - W, T]`:

```
n(t)      = number of participants speaking at the instant t
Silence   = time during which n(t) = 0
CrossTalk = time during which n(t) >= 2
```

in seconds, or as a share of the window with `AsRatioOfWindow`. The intervals are cut on the
window edges. An instant counts once, however many participants speak, so three participants
speaking together for 2 s are 2 s of cross-talk; two intervals of one participant that overlap
are one speech. Only the time spoken together counts, not the whole utterances.

`CrossTalk` also has a pair level: the time during which the two participants of the pair both
speak. `Silence` is a group index. Neither is part of the score.

Example, window of 10 s ending at 25.5 s, with A speaking from 12 to 21, B from 14 to 16 and
from 18 to 22, C from 15 to 19: three speak until 16, two until 18, three until 19, two until
21, one until 22, nobody afterwards. `Silence` = 3.5 s, `CrossTalk` = 5.5 s, and for the pairs
A-B 3.5 s, A-C 3.5 s, B-C 1.5 s.

As for every interval (section 3.1), a speech exists once its interval has been received: the
time of an utterance in progress is silence until then, and enters the following windows.

The turn taking indicator has a silence output of its own, the sum of the durations carried by
events of the category `Silence`, which stays at 0 unless such events are fed to it. When the
`Silence` indicator is declared it publishes the index of that name, and the output of the turn
taking indicator is left out.

### 5.15 Mutual gaze

*`Indicators.MutualGaze`, `MutualGazeIndicator`, `EventCountIndexComponent`; events from
`MutualGazeDetector`.*

A mutual gaze is the time during which two participants look at each other. The detector finds
it in the looks at peers (`GazeEpisodeDetector.PeerGazeIntervalOut`): a look of A at B over
`[s1, e1]` and a look of B at A over `[s2, e2]` are a mutual gaze when

```
min(e1, e2) - max(s1, s2) > 0        (and at least MinimumDuration, zero by default)
```

which lasts from `max(s1, s2)` to `min(e1, e2)`. The event carries the two participants and the
end of the mutual gaze. A look is known when it ends, so the event is published when the second
of the two looks ends; one long look met by three short ones gives three mutual gazes.

The index is the number of mutual gazes in the window, for the group and for each pair. It is
a raw count (no reference value has been measured), and not part of the score. It inherits what
the looks at peers are: at least 200 ms long, and a look that ends less than 500 ms after the
previous look at the same peer is not a new look.

### 5.16 Summary

| Indicator | Window statistic | Normalised by | Score input |
|---|---|---|---|
| Movement | weighted mean speed | nothing | group, raw |
| Synchrony | Pearson correlation of movement | `(r + 1) / 2` | group |
| VerbalParticipation | speaking time | division by the window | group ratio |
| SpeechEquality, TaskEquality | Gini index | its maximum | `1 - G`, with validity |
| TalkingMost, TaskingMost | arg max | | no |
| TurnTaking | event count per category | calibration | group of each category |
| JointVisualAttention | episode count | calibration | group |
| GazeOnPeers | gaze count | calibration (group) | group |
| AttentionLevel | accumulator, own clock | | no |
| TaskParticipation | action count with penalty | calibration (group) | group |
| TimeInArea | presence duration | | no |
| Formation | formation count | calibration | group |
| Proximity | latest distance | `1 - d / 3 m` | no |
| Silence | time nobody speaks | nothing (seconds), or division by the window | no |
| CrossTalk | time at least two speak | nothing (seconds), or division by the window | no |
| MutualGaze | mutual gaze count | nothing | no |

---

## 6. The collaboration score

*`WithCollaborationScore(dimensions)`, component `CollaborationScoreComponent`.*

### 6.1 Dimensions

A dimension is a named set of indices. The default model
(`SlidingAverageComputation.DefaultDimensions()`) is the one of [A] and [B]:

| Dimension (name in code) | Name in [B] | Indices | Conditional |
|---|---|---|---|
| `Dominance` | Dominance | `SpeechEquality`, `TaskEquality` | both |
| `JointAttention` | Joint attention | `JointVisualAttention`, `GazeOnPeers` | |
| `CommunicationProcessManagement` | Conversational dynamics | `VerbalParticipation`, `TurnTakingWithoutOverlap` | |
| `SpatialBehaviour` | Embodied coordination | `Formation`, `Synchrony` | |
| `Engagement` | Behavioural engagement | `Movement`, `TaskParticipation` | |

Pass other `ScoreDimension` objects to test another decomposition, with weights per index if
needed; nothing else changes.

### 6.2 Computation

At each tick the component receives the score inputs `v_i` and their validity flags, all of the
same tick. For a dimension `d` with indices `I_d` and weights `w_i` (1 by default):

```
usable(i) = the index i has been received at least once
            and (i is not conditional in d, or its validity flag is true)

D_d = sum over usable i of  w_i * f(v_i)  /  sum over usable i of w_i
      with f(v) = ln(1 + v)   when UseLogCompression (default)
           f(v) = v           otherwise
```

A dimension without any usable index is left out. The global score is

```
score = mean of D_d over the dimensions that were computed
```

which is the dimension-based collaboration score of [B] (Table 6): each dimension counts the
same, and an invalid dimension is excluded rather than counted as zero. With
`GlobalFromDimensions = false` the global score is instead the plain mean of the indices listed
in `GlobalIndexNames`.

Example with the default model, movement 0.24 and normalised task participation 0.60:
`Engagement = (ln(1.24) + ln(1.60)) / 2 = (0.215 + 0.470) / 2 = 0.34`. With the logarithm a
dimension cannot exceed `ln 2 = 0.69` when its indices are in `[0, 1]`.

### 6.3 Validity of the equality indices

An equality index computed on almost no data is meaningless: two people who each said one word
are "perfectly equal". Both equality indices are therefore **conditional**: they only count
while their validity flag is true.

- Speech equality is valid when the index is defined and the group verbal participation ratio
  exceeds `MinimumRatioForEquality` (0.15).
- Task equality is valid when the index is defined, i.e. when at least one task action was
  counted in the window.

When neither is valid, `Dominance` is absent and the score is the mean of the other dimensions.

### 6.4 Missing and silent indices

- An indicator that is not declared never provides its index: the dimension is computed on the
  indices it has, and disappears if it has none. Declaring two indicators therefore gives a
  score over the dimensions those two indicators cover.
- An indicator that skips a tick (synchrony while a participant is not tracked) keeps its last
  value in the score, the graph and the export.

### 6.5 Outputs

`Out` (global score), `DimensionsOut` and one stream per dimension, `NormalizedIndicesOut` (the
inputs used, for inspection). In a snapshot and in the CSV the global score is
`CollaborationScore` and each dimension appears under its name.

---

## 7. Outputs: snapshot, graph, CSV

### 7.1 Snapshot

`indices.SnapshotOut` posts one `IndexSnapshot` per tick:

| Field | Content |
|---|---|
| `OriginatingTime` | the tick |
| `Group[name]` | group values, plus `CollaborationScore` and the dimension scores when the score is computed |
| `Individual[name][participant]` | values per participant |
| `Pair[name][pair]`, `DirectedPair[name][pair]` | values per pair and per ordered pair |
| `ScoreInputs[name]`, `Validity[name]` | what entered the score |

It is built by `IndexSnapshotAssembler`. Every indicator paced by the clock posts, after
handling a tick, whether it published for that tick (`TickProcessedOut` of the component). For
each tick the assembler therefore knows exactly which values are still to come, waits for those
and only those, and emits the snapshot. The result does not depend on the order in which \psi
delivers the messages, at any replay speed and with any number of threads. An indicator computed
from another one (equality from participation) follows the pulse of its source; an indicator on
its own clock (attention level) is read at its latest value.

The score, the graph and the export all read the same snapshot, completed by the score on its
way, so a score, a graph and a CSV row of one tick never mix the values of two ticks.

### 7.2 Interaction graph

`WithInteractionGraph()` posts one `InteractionGraph` per tick on `indices.Out`: a node per
participant with its individual metrics, an edge per pair with its pair metrics and, in
`ForwardMetrics` / `BackwardMetrics`, the directed ones, and the group metrics
(`CollaborationScore`). Which outputs become graph metrics is declared by the indicators
(`IndexUsage.Graph`).

### 7.3 CSV export

`WithCsvExport(writer or path)` writes one row per tick: `Timestamp` (Unix milliseconds of the
tick), then the columns. By default the columns are every output declared for export, in the
order of the indicators: `Name` for a group value, `Name_<participant>`, `Name_<a>-<b>` for a
pair, `Name_<a>-><b>` for an ordered pair, then the dimension scores and `CollaborationScore`.
Separator `;`, invariant culture, `NA` until an index has been published. Pass `columns` to fix
the list and the order.

### 7.4 Stores

With `WithStore(server)`, the components write the streams they have always written into the
`LiveVisualization` store of the session: group and individual activity levels, group and pair
synchrony, speaking times and group verbal participation, group equality.

---

## 8. Conformity with the published method

This section compares three things: what [A] and [B] describe, what the implementation used for
those studies did (`SlidingAverageComputation.cs` of the *CollaborationAnalysisFramework*
repository, called "original" below), and what this component does. It was established by
reading the three; where they agree nothing is listed.

### 8.1 What matches

- The window `[T - W, T]`, one computation per second, the warm-up of one window after the start
  of a phase.
- The normalisation function and its `alpha`, and the group level reference values of the 20 s
  window.
- The Gini index normalised by its maximum, for the group, the pairs and the triads.
- Synchrony: 50 ms grid, 30 ms tolerance, Pearson correlation of the head displacement series,
  mean of the pairs, `(r + 1) / 2`.
- Movement: path length of head and hands, weights 0.4 / 0.3 / 0.3 (in the per-sample unit).
- Counts of joint visual attention episodes (group and pairs), gazes on peers, formations, turn
  takings, and task actions with the penalty of 2 per inefficient action.
- The five dimensions and their indices; the score as the mean of the valid dimensions; equality
  entering the score as `1 - Gini`.

### 8.2 What differs

| # | Subject | [A] / [B] | Original implementation | This component |
|---|---|---|---|---|
| 1 | Movement in the score | every indicator is normalised with its 95th percentile | `1 - exp(-alpha * x)`, `x_ref` = 0.041 | the raw group value enters the score; the reference value is in the calibration but unused |
| 1b | Unit of movement | mean distance between consecutive samples | same | metres per second by default (`Unit`); the per-sample unit is an option. The reference 0.041 was measured per sample |
| 2 | Verbal participation in the score | normalised like the others | `1 - exp(-alpha * x)` with `x` = sum of the speaking times in seconds and `x_ref` = the window length | the ratio `sum / (N W)` enters the score; the reference value is unused |
| 3 | Validity of speech equality | invalid when verbal participation is below 0.125 | invalid when the *normalised* verbal participation is at most 0.15 | invalid when the *ratio* `sum / (N W)` is at most 0.15, which is far stricter (9 s of cumulated speech in 20 s for a triad, against about 1 s) |
| 4 | Validity of task equality | same rule with task participation | invalid when the normalised task participation is at most 0.15 | only invalid when no action at all was counted |
| 5 | Dimension formula | arithmetic mean of the two indicators ([B], Table 5) | mean of `ln(1 + s)` | mean of `ln(1 + s)` by default; `UseLogCompression = false` gives the arithmetic mean |
| 6 | Reference score of turn taking without overlap | 0.95 for every indicator | 0.90 | 0.95 |
| 7 | Counts entering the score | counts over the window | recency-weighted counts `sum of exp(-lambda * age)` for joint visual attention, gaze, task participation and formation | plain counts. `TimeDecayIndexComponent` implements the weighted variant but is not in the catalogue |
| 8 | Default window | 20 s | set per instance | 30 s for the builder: set `WithWindow(TimeSpan.FromSeconds(20))` |
| 9 | Undefined equality | not specified | group Gini 0 when nobody spoke or acted | -1, and flagged invalid |
| 10 | Inefficient action | not specified | grab then release of the same object, both in the central table zone, consecutive among grabs, releases and placements | grab then release with the same label (same object, same place, any zone), consecutive among grabs and releases |
| 11 | Reference values | not given | some applied values differ from those documented next to them: for 45 s the 20 s values of most indicators, 8 instead of 5 for the joint visual attention pair at 20 s, 5 instead of 4 for the turn taking pair at 30 s | the documented values (section 4.2) |
| 12 | An indicator absent from a dimension | every indicator is present | counted with its value, possibly zero | skipped; the dimension is the mean of the indices it has |

Items 1 to 4 make the score of this component numerically different from the one described in
[B], even with the five dimensions declared: `Engagement` and `CommunicationProcessManagement`
receive un-normalised values, and `Dominance` is valid less often for speech and more often for
the task. Item 5 is a difference between the manuscript and both implementations. Items 6 to 12
are choices to be aware of when comparing with earlier results.

Item 1b needs a decision before item 1 can be applied: with the default unit a walking
participant moves at a few tenths of a metre per second, far above 0.041, so the normalised
value would always be close to 1. Either the indicator is used per sample at the sampling rate
of the study, or the reference value is measured again in metres per second.

### 8.3 Not specified by the papers

The following are choices of the implementation: the retention margins, the stale-data guards of
the synchrony, the handling of late data, the keeping of the last value of a silent indicator,
the attention accumulator and its constants, the proximity scale of 3 m.

---

## 9. Extending, testing, files

**Adding an indicator** takes two classes, a component and an indicator, and no change in the
library: see "Adding an indicator" in the README and the complete tested example in
`UnitTests/CollaborationIndices.Tests/CustomIndicatorTests.cs`.

**Tests** (`UnitTests/CollaborationIndices.Tests`, MSTest):

| File | What it checks |
|---|---|
| `BuilderTests`, `ValidationTests` | construction by builder and by configuration, JSON, every validation rule |
| `IndicatorComputationTests`, `AllIndicatorsTests` | each indicator on windows worked out by hand |
| `ReplayTests` | replay faster than real time, from a store, at another date, with a data clock |
| `FusionAlignmentTests` | the score, the graph and the rows hold the values of their own tick, with several threads |
| `EquivalenceTests` | same outputs as recorded before the builder existed (`Golden/`) |
| `RegressionTests`, `CustomIndicatorTests` | the defects fixed, and an indicator defined outside the library |

**Files** (`Components/CollaborationIndices`):

| Folder | Content |
|---|---|
| `Builder/` | builder, configuration, validation |
| `Core/` | indicator abstraction, build context, outputs, registry, base components, stores, normalizers, calibration |
| `Physical/`, `Verbal/`, `Gaze/`, `Task/`, `Spatial/` | components and their indicators, the detectors (`*Detector.cs`, section 1), and the pre-processing of the source streams: the speech chain in `Verbal/`, `Spatial/PositionOrientationPreProcessing` (head and hands of one participant), `Task/InteractionFilter` (piece interactions) |
| `Fusion/` | phase gate, equality, dominance, score, snapshot, graph, export, data clock |
| `Adapters/` | conversion of the SAAC stream types |
| `Legacy/` | the configuration by families of indices and its translation |
| `Indicators.cs` | the catalogue |
| `SlidingAverageComputation.cs`, `SlidingAverageComputationSet.cs` | the built instance and the set of windows |
