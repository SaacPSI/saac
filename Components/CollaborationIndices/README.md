# CollaborationIndices

## Summary

Components computing collaboration indices (movement, synchrony, verbal participation, joint
visual attention, ...) on a sliding window, for a live pipeline as well as for the replay of
recorded stores.

Which indices are computed is declared when the set is instantiated, either in code with
`CollaborationIndicesBuilder` or as data with `CollaborationIndicesConfiguration`. Nothing has
to be edited in the library to select, configure or add an indicator.

A complete program that runs as it is: `Applications/CollaborationIndicesExample`.

Detailed documentation (time window, computation of each indicator, collaboration score, conformity
with the published method): `docs/wiki/CollaborationIndices-Component.md`.

## Instantiation with the builder

```csharp
var indices = new CollaborationIndicesBuilder(pipeline)
    .WithParticipants(0, 1, 2)
    .WithWindow(TimeSpan.FromSeconds(30))
    .WithDataClock(headPositions[0])                 // one tick per second of data
    .AddIndicator(Indicators.Movement)
    .AddIndicator(Indicators.Synchrony, o => o.SubsetSize = 3)
    .AddIndicator(Indicators.VerbalParticipation)
    .AddIndicator(Indicators.SpeechEquality)
    .WithCollaborationScore()                        // optional
    .WithCsvExport("indices.csv")                    // optional
    .Build();

// Connect the source streams to the inputs of the indicators.
headPositions[0].PipeTo(indices.Get(Indicators.Movement).Component.GetPositionInput(0, BodyPartNames.Head));
speech[0].PipeTo(indices.Get(Indicators.VerbalParticipation).Component.GetIntervalInput(0));
```

`Build()` checks the whole declaration before creating anything and reports every problem at
once in a `CollaborationIndicesConfigurationException`: window or interval not positive, no
participant or a duplicated one, no indicator, the same indicator twice, a missing dependency,
an unknown indicator or option, a constraint of an indicator (synchrony needs two participants).
`Validate()` returns the same messages without throwing.

## Instantiation with a configuration

```csharp
var configuration = CollaborationIndicesConfiguration.Load("indices.json");
var indices = CollaborationIndicesBuilder.FromConfiguration(pipeline, configuration)
    .WithDataClock(headPositions[0])                 // streams are not data: given in code
    .Build();
```

```json
{
  "ParticipantIds": [ 0, 1, 2 ],
  "WindowDuration": "00:00:30",
  "Indicators": [
    { "Type": "Movement", "Options": { "BodyParts": [ "Head" ] } },
    { "Type": "Synchrony", "Options": { "SubsetSize": 3 } },
    { "Type": "VerbalParticipation" },
    { "Type": "SpeechEquality" }
  ],
  "ComputeCollaborationScores": true,
  "ExportPath": "indices.csv"
}
```

`Type` is an entry of the catalogue below. `Options` are the properties of the options class of
the indicator; `Name` and `WindowDuration` exist for every indicator. A misspelled property is an
error, not silently ignored. `builder.ToConfiguration().Save(path)` writes the declaration of a
builder, for instance next to the results it produced.

## Catalogue

| Entry of `Indicators` | Index | Needs | Connect to `Component` |
|---|---|---|---|
| `Movement` | Activity level per participant and for the group | | `GetPositionInput(id, bodyPart)` or `GetBodyInput(id)` |
| `Synchrony` | Correlation of the movements per pair and for the group | 2 participants | `GetPositionInput(id, bodyPart)` or `GetBodyInput(id)` |
| `VerbalParticipation` | Speaking time per participant and for the group | | `GetIntervalInput(id)`, `IntervalsIn` |
| `SpeechEquality` | Gini index of the speaking times | `VerbalParticipation` | |
| `TalkingMost` | Who speaks the most | `VerbalParticipation` | |
| `TurnTaking` | Turn takings with / without overlap, overlaps | 2 participants | `EventsIn` |
| `Silence` | Cumulated time during which nobody speaks | | `GetIntervalInput(id)`, `IntervalsIn` (the speech intervals) |
| `CrossTalk` | Cumulated time during which at least two participants speak, for the group and per pair | 2 participants | `GetIntervalInput(id)`, `IntervalsIn` (the speech intervals) |
| `JointVisualAttention` | Episodes of joint visual attention | 2 participants | `EventsIn` |
| `GazeOnPeers` | Gazes of each participant on each peer | 2 participants | `EventsIn` |
| `MutualGaze` | Times two participants look at each other, for the group and per pair | 2 participants | `EventIn`, `EventsIn` (from `MutualGazeDetector`) |
| `AttentionLevel` | Attention accumulator, on its own fast clock | | `GetAttentiveInput(id)`, `ClockIn` of the indicator |
| `TaskParticipation` | Productive task actions | | `EventsIn` |
| `TaskEquality` | Gini index of the task participations | `TaskParticipation` | |
| `TaskingMost` | Who acts the most on the task | `TaskParticipation` | |
| `TimeInArea` | Time per participant in each area | | `GetIntervalInput(id)`, `IntervalsIn` |
| `Formation` | F-formations | 2 participants | `EventIn`, `EventsIn` |
| `Proximity` | Interpersonal distance per pair | 2 participants | `GetDistanceInput(a, b)` |
| `Equality`, `Dominance` | The two above for any distribution: set `Source` and `Name` | the source | |

The adapters of `IndexAdapters` and `SerializableClassAdapters` turn the existing SAAC stream
types into the events, intervals and positions expected by these inputs.

The speech indicators start from the transcription of each participant. For a session that
only recorded the audio, `SpeechFromAudio` (folder `Verbal`) computes it: `VoiceActivity` from
the audio (16 kHz, one channel, 16 bit PCM), then `Transcription` by Whisper, with the model
set in `SpeechFromAudioConfiguration`. Such a pipeline runs at the pace of the session.
`AudioStreamAssignment` gives each participant an audio stream whatever the streams are named:
it numbers them in the natural order of their names, and takes them in that order or as an
instruction says, by id or by name.

The name of an entry is at once the `Type` in a configuration, the default name of the outputs
(`indices.Outputs.Group("Synchrony")`), the prefix of the CSV columns and the key of the
reference value in `IndexCalibration`.

An indicator can be declared three ways: by catalogue entry (typed options), by name
(`AddIndicator("Synchrony", options)`, what a configuration does) or by class
(`AddIndicator<MyIndicator>(i => i.Options...)`).

## Windows

- The window of the builder applies to every indicator. One indicator can have its own:
  `AddIndicator(Indicators.Synchrony, o => o.WindowDuration = TimeSpan.FromSeconds(5))`.
- Each indicator keeps its own buffer, since they hold different data; the clock and the phase
  gate are shared, so all the indices are published at the same instants.
- The gate stays closed for one window after the start (or after each phase start), until the
  window is filled.
- Several windows side by side: `builder.BuildSet(windows)` gives one instance per window, each
  with the calibration measured for its window, all on the same clock.

## Real time and replay

Every window is `[T - window, T]`, where `T` is the originating time of a tick of the clock, and
contains the messages whose originating time falls inside. Nothing reads the clock of the machine,
so a store replayed faster than real time gives the indices of the recorded session.

The clock is chosen on the builder:

| | |
|---|---|
| nothing | connect a tick stream to `indices.ClockIn` |
| `WithClock(ticks)` | any `IProducer<bool>` |
| `WithDataClock(stream)` | ticks taken from a dense data stream, on the data timeline by construction; recommended for a replay |
| `WithInternalClock()` | a generator on the clock of the pipeline; live pipelines only |

Limit to know: \psi does not synchronise a tick with the data. A message stamped before `T` but
delivered after the tick `T` only enters the next window. Live, this concerns at most the very
last samples. In a replay that is not paced by the clock, with several threads, it can concern
more, and two runs of the same store then differ slightly. For an offline replay that must be exactly
reproducible, run the indices in a single threaded pipeline
(`Pipeline.Create(threadCount: 1)`), as the example and the tests do.

## Score, graph, export

`WithCollaborationScore`, `WithInteractionGraph` and `WithCsvExport` are wired from what the
indicators declare. All three read the indices of a tick from one `IndexSnapshot`, built once
every index of that tick is known, so a score or a row never mixes two ticks.
`indices.SnapshotOut` gives those snapshots directly and is the simplest stream to consume
downstream.

`options.IncludeInScore = false` keeps an indicator out of the score while still computing and
exporting it.

## Adding an indicator

Two classes, in any project; the library is not modified.

1. The component computes. Deriving from `IndexComponentBase` brings the clock input, the
   throttling and the completion stream used to align the indices.

```csharp
public class HeadHeightConfiguration : IndexComponentConfiguration { }

public class HeadHeightComponent : IndexComponentBase<HeadHeightConfiguration>
{
    public HeadHeightComponent(Pipeline pipeline, HeadHeightConfiguration configuration, string name)
        : base(pipeline, configuration, name)
    {
        this.GroupOut = pipeline.CreateEmitter<double>(this, $"{name}-Group");
        // ... receivers storing the samples with their originating time
    }

    public Emitter<double> GroupOut { get; }

    protected override void Compute(DateTime originatingTime)
    {
        // ... mean over [this.WindowStart(originatingTime), originatingTime]
        this.GroupOut.Post(mean, originatingTime);
    }
}
```

2. The indicator tells the builder what the component needs and offers.

```csharp
public class HeadHeightOptions : IndicatorOptions { }

public class HeadHeightIndicator : ComponentIndicator<HeadHeightOptions, HeadHeightComponent>
{
    public override string DefaultName => "HeadHeight";

    public override void Build(IndicatorBuildContext context)
    {
        var configuration = new HeadHeightConfiguration
        {
            ParticipantIds = context.CopyParticipantIds(),
            WindowDuration = context.WindowDuration,
            ComputationInterval = TimeSpan.Zero,          // paced by the shared clock
        };

        this.Component = new HeadHeightComponent(context.Pipeline, configuration, context.ComponentName(this.Name));

        context.DriveByClock(this.Component.TickIn, this.Component.TickProcessedOut);
        context.PublishGroup(this.Name, this.Component.GroupOut);        // snapshot, CSV column
        context.PublishScoreInput(this.Name, this.Component.GroupOut);   // usable in a score dimension
    }
}
```

3. Use it: `builder.AddIndicator<HeadHeightIndicator>()`. To also use it from a configuration
   file and with typed options, register it once:

```csharp
public static readonly IndicatorType<HeadHeightIndicator, HeadHeightOptions> HeadHeight
    = IndicatorRegistry.Register<HeadHeightIndicator, HeadHeightOptions>("HeadHeight");
```

Rules of an indicator: declare only outputs that the component posts on every computation;
override `Validate` to check the options; list in `Dependencies` the indicators it reads from
and use `context.FollowPulseOf(name)` instead of `DriveByClock` when it is computed from the
output of another one. A complete, tested version of this example is in
`UnitTests/CollaborationIndices.Tests/CustomIndicatorTests.cs`.

## Migration from `SlidingAverageConfiguration`

`new SlidingAverageComputation(pipeline, server, configuration)` and
`new SlidingAverageComputationSet(...)` still work and are marked obsolete. They are translated
into builder calls by `Legacy/LegacyIndices.cs`:

| Flag | Builder |
|---|---|
| `UsePhysicalIndices` | `Movement` (with a 5 s additional window), `Synchrony` |
| `UseVerbalIndices` | `VerbalParticipation`, `SpeechEquality` (not in the score), `TalkingMost` |
| `UseVisualIndices` | `JointVisualAttention`, `GazeOnPeers` (both not in the score) |
| `UseTaskIndices` | `TaskParticipation`, `TaskEquality`, `TaskingMost` |
| `UseSpatialIndices` | `TimeInArea`, `Formation`, `Proximity` |
| always | `AttentionLevel` |
| `ComputeCollaborationScores`, `GenerateGraph`, `IndicesWriter` | `WithCollaborationScore()`, `WithInteractionGraph()`, `WithCsvExport(writer, columns)` |
| `server` | `WithStore(server)` |

`sa.ActivityLevel`, `sa.Synchrony`, `sa.VerbalParticipation`, ... are kept and return `null` when
the indicator is not part of the instance. Differences with the previous constructor:

- a family switched off is not instantiated any more (`UsePhysicalIndices = false` used to create
  the physical components without pacing them);
- combinations of flags that threw a `NullReferenceException` now work;
- the score and the CSV rows hold the indices of their own tick (they used to hold a mix of two
  consecutive ticks), so their values differ from files produced before;
- the builder defaults to `RequirePhase = false`; the legacy configuration keeps its `true`.

## Tests

`UnitTests/CollaborationIndices.Tests` (MSTest): construction by builder and by configuration,
validation, indices on windows worked out by hand, replay from a store, alignment of the score,
and equivalence with the outputs recorded before the builder existed (`Golden/`).
