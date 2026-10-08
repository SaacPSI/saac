// <copyright file="SessionStreams.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

namespace Expe2Reprocessing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Numerics;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Speech;
    using SAAC.CollaborationIndices;
    using SAAC.PsiFormats;

    /// <summary>The raw streams of one participant.</summary>
    internal sealed class ParticipantStreams
    {
        /// <summary>Head on the server: position, and Euler angles of the head transform.</summary>
        public IProducer<Tuple<Vector3, Vector3>> Head { get; set; }

        public IProducer<Tuple<Vector3, Vector3>> LeftWrist { get; set; }

        public IProducer<Tuple<Vector3, Vector3>> RightWrist { get; set; }

        /// <summary>Enter and leave events of the areas ("User1;In;In;CentraleTableArea").</summary>
        public IProducer<string> Area { get; set; }

        public IProducer<PieceStatus> Pieces { get; set; }

        /// <summary>One stream per generator.</summary>
        public List<IProducer<GeneratorInteraction>> Generators { get; } = new List<IProducer<GeneratorInteraction>>();

        /// <summary>Gaze events of the headset, one stream per eye; a headset publishes on either.</summary>
        public List<IProducer<ObjectGazeEvent>> EyeGaze { get; } = new List<IProducer<ObjectGazeEvent>>();

        /// <summary>Gaze events on the avatars, recomputed by the replay of the scene; peers are numbered from 0.</summary>
        public IProducer<ObjectGazeEvent> PeerGaze { get; set; }

        public IProducer<IStreamingSpeechRecognitionResult> Transcription { get; set; }
    }

    /// <summary>
    /// The raw streams of one session of the puzzle task (Expe2 and Expe3 datasets), opened
    /// from its stores. This class is the only place that knows where each stream is.
    ///
    /// The stores are opened for reading: nothing is written in the dataset. They were
    /// recorded with the types of the SerializableClass assembly and are read with the SAAC
    /// types (LegacyStoreTypes).
    /// </summary>
    internal sealed class SessionStreams
    {
        /// <summary>
        /// Local axis of the head transform recorded by the server that points where the
        /// participant looks, for DetectorSettings.HeadForwardAxis. Measured on a session:
        /// when a participant looks at a peer, this axis is at 9 degrees (median) of the
        /// direction of the peer, the others at 90.
        /// </summary>
        public const string HeadForwardAxis = "+Y";

        private readonly Pipeline pipeline;
        private readonly string datasetPath;

        /// <summary>Folder of the session.</summary>
        public string DatasetPath => this.datasetPath;

        private SessionStreams(Pipeline pipeline, string datasetPath)
        {
            this.pipeline = pipeline;
            this.datasetPath = datasetPath;
        }

        public IProducer<string> TaskEvents { get; private set; }

        public IProducer<PuzzleStatus> PuzzleStatus { get; private set; }

        public Dictionary<uint, ParticipantStreams> Participants { get; } = new Dictionary<uint, ParticipantStreams>();

        /// <summary>Opens the streams of a session. Participant 0 is headset 1, and so on.</summary>
        public static SessionStreams Open(Pipeline pipeline, string datasetPath, IReadOnlyList<uint> participants)
        {
            var session = new SessionStreams(pipeline, datasetPath);

            Importer task = session.OpenStore("Task", @"RawDataUnityServer.000\Task.0000");
            session.TaskEvents = task.OpenStream<string>("UnityServer-TaskEvent");
            session.PuzzleStatus = task.OpenStream<PuzzleStatus>("UnityServer-PuzzleStatus");

            Importer heads = session.OpenStore("Heads", @"RawDataUnityServer.000\Heads.0000");
            Importer lefts = session.OpenStore("Positions Left", @"RawDataUnityServer.000\Positions Left.0000");
            Importer rights = session.OpenStore("Positions Right", @"RawDataUnityServer.000\Positions Right.0000");
            Importer audio = session.OpenStore("AudioData", @"AudioData.0000");
            Importer replay = session.OpenStore("UnityReplay", @"UnityUnityB.000\UnityReplay.0000");

            foreach (uint id in participants)
            {
                uint n = id + 1;
                Importer spatial = session.OpenStore("Individual Spatial Event", $@"RawDataQuest{n}.000\Individual Spatial Event.0000");
                Importer logs = session.OpenStore("Individual Task Logs", $@"RawDataQuest{n}.000\Individual Task Logs.0000");
                Importer gaze = session.OpenStore("Individual GazeEvent", $@"RawDataQuest{n}.000\Individual GazeEvent.0000");

                var streams = new ParticipantStreams
                {
                    Head = heads.OpenStream<Tuple<Vector3, Vector3>>($"UnityServer-{n}-Head"),
                    LeftWrist = lefts.OpenStream<Tuple<Vector3, Vector3>>($"UnityServer-{n}-LeftWrist"),
                    RightWrist = rights.OpenStream<Tuple<Vector3, Vector3>>($"UnityServer-{n}-RightWrist"),
                    Area = spatial.OpenStream<string>($"Quest{n}-Area"),
                    Pieces = logs.OpenStream<PieceStatus>($"Quest{n}-PieceState"),
                    PeerGaze = replay.OpenStream<ObjectGazeEvent>($"UnityB-H{n}-GazeEvent"),
                    Transcription = audio.OpenStream<IStreamingSpeechRecognitionResult>($"STT_{n}"),
                };
                streams.Generators.Add(logs.OpenStream<GeneratorInteraction>($"Quest{n}-Generator1"));
                streams.Generators.Add(logs.OpenStream<GeneratorInteraction>($"Quest{n}-Generator2"));
                streams.EyeGaze.Add(gaze.OpenStream<ObjectGazeEvent>($"Quest{n}-LeftEyeGaze"));
                streams.EyeGaze.Add(gaze.OpenStream<ObjectGazeEvent>($"Quest{n}-RightEyeGaze"));
                session.Participants[id] = streams;
            }

            return session;
        }

        private Importer OpenStore(string name, string relativePath)
        {
            string path = Path.Combine(this.datasetPath, relativePath);
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Store '{name}' not found: {path}");
            }

            return PsiStore.Open(this.pipeline, name, path).WithLegacyTypes();
        }
    }
}
