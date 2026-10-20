using System;
using Microsoft.Psi;

namespace SAAC.CollaborationIndices
{
    public class TaskPhaseDetectorConfiguration
    {
        /// <summary>First field of the task event that starts the task.</summary>
        public string StartKeyword { get; set; } = "start";

        /// <summary>The task event that ends the task.</summary>
        public string EndKeyword { get; set; } = "end";

        /// <summary>Separator of the fields of a task event.</summary>
        public char Separator { get; set; } = ';';

        public bool LogToConsole { get; set; } = false;
    }

    /// <summary>
    /// Phases of a session made of successive puzzles, from the streams of the task server.
    ///
    ///  - the task, and the first puzzle with it, starts on the first task event whose first
    ///    field is "start";
    ///  - a puzzle ends when the server publishes its status with the pieces it was solved
    ///    with (currentPiecesNumber greater than 0);
    ///  - the next puzzle starts when the server publishes the status of a puzzle not started
    ///    yet (no piece placed, and an optimal number of pieces to reach). The last status of
    ///    a session announces no puzzle (optimal number 0) and starts nothing.
    ///
    /// PhaseStartOut and PhaseEndOut go to the phase gate of the indices
    /// (indices.Gate.PhaseStartIn / PhaseEndIn), which then restricts the indices to the
    /// puzzles and restarts the warm-up of the window at each of them.
    ///
    /// The legacy StepTaskEvent read the puzzle boundaries from the task events alone, with
    /// the values of their third field hard coded (17000 for the start, at most 16000 for
    /// the following ones) and the next puzzle assumed to start 5 s after the previous one.
    /// The puzzle status carries the same instants without those constants.
    /// </summary>
    public class TaskPhaseDetector
    {
        private readonly TaskPhaseDetectorConfiguration configuration;
        private readonly string name;
        private bool taskStarted;
        private bool taskEnded;
        private bool phaseRunning;

        public TaskPhaseDetector(Pipeline pipeline, TaskPhaseDetectorConfiguration configuration = null, string name = nameof(TaskPhaseDetector))
        {
            this.configuration = configuration ?? new TaskPhaseDetectorConfiguration();
            this.name = name;

            this.TaskEventIn = pipeline.CreateReceiver<string>(this, this.ReceiveTaskEvent, $"{name}-TaskEvent");
            this.PuzzleStatusIn = pipeline.CreateReceiver<PuzzleStatus>(this, this.ReceivePuzzleStatus, $"{name}-PuzzleStatus");

            this.TaskStartOut = pipeline.CreateEmitter<DateTime>(this, $"{name}-TaskStart");
            this.TaskEndOut = pipeline.CreateEmitter<DateTime>(this, $"{name}-TaskEnd");
            this.PhaseStartOut = pipeline.CreateEmitter<bool>(this, $"{name}-PhaseStart");
            this.PhaseEndOut = pipeline.CreateEmitter<bool>(this, $"{name}-PhaseEnd");
            this.PuzzleIdOut = pipeline.CreateEmitter<int>(this, $"{name}-PuzzleId");
        }

        /// <summary>Task events of the server ("start;True;17000;0;0", "end").</summary>
        public Receiver<string> TaskEventIn { get; }

        /// <summary>Puzzle status of the server.</summary>
        public Receiver<PuzzleStatus> PuzzleStatusIn { get; }

        /// <summary>Time of the start of the task, once.</summary>
        public Emitter<DateTime> TaskStartOut { get; }

        /// <summary>Time of the end of the task, once.</summary>
        public Emitter<DateTime> TaskEndOut { get; }

        /// <summary>One message at the start of each puzzle; its originating time is the start.</summary>
        public Emitter<bool> PhaseStartOut { get; }

        /// <summary>One message at the end of each puzzle; its originating time is the end.</summary>
        public Emitter<bool> PhaseEndOut { get; }

        /// <summary>Identifier of the puzzle that starts, as numbered by the server (0 for the first).</summary>
        public Emitter<int> PuzzleIdOut { get; }

        private void ReceiveTaskEvent(string taskEvent, Envelope envelope)
        {
            if (string.IsNullOrEmpty(taskEvent) || this.taskEnded)
            {
                return;
            }

            string keyword = taskEvent.Split(this.configuration.Separator)[0];
            if (!this.taskStarted && keyword == this.configuration.StartKeyword)
            {
                this.taskStarted = true;
                this.TaskStartOut.Post(envelope.OriginatingTime, envelope.OriginatingTime);
                this.StartPhase(0, envelope.OriginatingTime);
            }
            else if (this.taskStarted && taskEvent == this.configuration.EndKeyword)
            {
                this.EndPhase(envelope.OriginatingTime);
                this.EndTask(envelope.OriginatingTime);
            }
        }

        private void ReceivePuzzleStatus(PuzzleStatus status, Envelope envelope)
        {
            if (status == null || !this.taskStarted || this.taskEnded)
            {
                return;
            }

            if (status.currentPiecesNumber > 0)
            {
                this.EndPhase(envelope.OriginatingTime);
            }
            else if (status.optimalPiecesNumber > 0)
            {
                this.StartPhase(status.puzzleID, envelope.OriginatingTime);
            }
            else
            {
                // No piece placed and nothing to reach: the server has no puzzle left.
                this.EndPhase(envelope.OriginatingTime);
                this.EndTask(envelope.OriginatingTime);
            }
        }

        private void StartPhase(int puzzleId, DateTime time)
        {
            if (this.phaseRunning)
            {
                return;
            }

            this.phaseRunning = true;
            this.PhaseStartOut.Post(true, time);
            this.PuzzleIdOut.Post(puzzleId, time);
            this.Log($"puzzle {puzzleId} starts at {time:HH:mm:ss.fff}");
        }

        private void EndPhase(DateTime time)
        {
            if (!this.phaseRunning)
            {
                return;
            }

            this.phaseRunning = false;
            this.PhaseEndOut.Post(true, time);
            this.Log($"puzzle ends at {time:HH:mm:ss.fff}");
        }

        private void EndTask(DateTime time)
        {
            this.taskEnded = true;
            this.TaskEndOut.Post(time, time);
        }

        private void Log(string message)
        {
            if (this.configuration.LogToConsole)
            {
                Console.WriteLine($"[{this.name}] {message}");
            }
        }
    }
}
