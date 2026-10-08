// <copyright file="GatherProducers.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Data;
using Microsoft.Psi.Data.Annotations;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Speech;
using SAAC.PipelineServices;
using SAAC.PsiFormats;
using ServerApplication.Examples.ComponentsClass.Enums;
using ServerApplication.Examples.ComponentsClass.Structures;
using static SAAC.PipelineServices.DatasetPipeline;

namespace ServerApplication.Examples
{
    public class GatherProducers
    {
        #region Lists, Iproducer, and values initialization

        #region Main Data
        // Audio
        public List<IProducer<AudioBuffer>> Audios = new List<IProducer<AudioBuffer>>();
        public List<IProducer<bool>> Vads = new List<IProducer<bool>>();
        public List<IProducer<IStreamingSpeechRecognitionResult>> Stts = new List<IProducer<IStreamingSpeechRecognitionResult>>();

        // Spatial
        public List<IProducer<Tuple<Vector3, Vector3>>> HeadPositionOrientationsUnity = new List<IProducer<Tuple<Vector3, Vector3>>>();
        public List<IProducer<Tuple<Vector3, Vector3>>> LeftsHandPositionOrientationsUnity = new List<IProducer<Tuple<Vector3, Vector3>>>();
        public List<IProducer<Tuple<Vector3, Vector3>>> RightsHandPositionOrientationsUnity = new List<IProducer<Tuple<Vector3, Vector3>>>();

        // Visual
        public List<IProducer<ObjectGazeEvent>> LeftGazeEvents = new List<IProducer<ObjectGazeEvent>>();
        public List<IProducer<ObjectGazeEvent>> RightGazeEvents = new List<IProducer<ObjectGazeEvent>>();
        public List<IProducer<ObjectGazeEvent>> AvatarGazeEvents = new List<IProducer<ObjectGazeEvent>>();
        public List<IProducer<string>> LeftGazeEventsStrings = new List<IProducer<string>>();

        // Physical
        public List<IProducer<PieceStatus>> TaskLogs = new List<IProducer<PieceStatus>>();

        // Task Events
        public IProducer<string> TaskEvent;

        // Video
        public IProducer<Shared<EncodedImage>> ServerVideo;
        #endregion

        #region Processed Data

        // Spatial
        public List<IProducer<Tuple<int, Vector3>>> Individuals_HeadPositions = new List<IProducer<Tuple<int, Vector3>>>();
        public List<IProducer<Tuple<int, Vector3>>> Individuals_HeadOrientations = new List<IProducer<Tuple<int, Vector3>>>();
        public List<IProducer<Tuple<int, Vector3>>> Individuals_LeftHandPositions = new List<IProducer<Tuple<int, Vector3>>>();
        public List<IProducer<Tuple<int, Vector3>>> Individuals_RightHandPositions = new List<IProducer<Tuple<int, Vector3>>>();

        // public List<IProducer<Tuple<Vector3, Quaternion>>> HeadPositionQuaternionsStandardised = new List<IProducer<Tuple<Vector3, Quaternion>>>();
        // public List<IProducer<Tuple<Vector3, Vector3>>> HeadPositionOrientationsStandardised = new List<IProducer<Tuple<Vector3, Vector3>>>();
        // public List<IProducer<Tuple<Vector3, Vector3>>> LeftsHandPositionOrientationsStandardised = new List<IProducer<Tuple<Vector3, Vector3>>>();
        // public List<IProducer<Tuple<Vector3, Vector3>>> RightsHandPositionOrientationsStandardised = new List<IProducer<Tuple<Vector3, Vector3>>>();

        // Visual
        public List<IProducer<Tuple<int, Queue<ObjectGazeEvent>>>> Individuals_Gazes = new List<IProducer<Tuple<int, Queue<ObjectGazeEvent>>>>();
        public List<IProducer<Tuple<int, Queue<ObjectGazeEvent>>>> Individuals_Ungazes = new List<IProducer<Tuple<int, Queue<ObjectGazeEvent>>>>();

        // public List<IProducer<Tuple<int, TimeData>>> Individuals_ObjectGazesFiltered = new List<IProducer<Tuple<int, TimeData>>>();
        // public List<IProducer<Tuple<int, TimeData>>> Individuals_AvatarGazesFiltered = new List<IProducer<Tuple<int, TimeData>>>();
        // public List<IProducer<Dictionary<string, Queue<TimeData>>>> GazesOnPeers = new List<IProducer<Dictionary<string, Queue<TimeData>>>>();
        // public List<IProducer<Dictionary<DuoType, Queue<TimeData>>>> GazesOnPeersDuo = new List<IProducer<Dictionary<DuoType, Queue<TimeData>>>>();

        // public List<IProducer<Tuple<Vector3, Vector3>>> QuestPosRot = new List<IProducer<Tuple<Vector3, Vector3>>>();
        // public List<IProducer<Tuple<Vector3, Vector3>>> LeftEyes = new List<IProducer<Tuple<Vector3, Vector3>>>();
        // public List<IProducer<Tuple<Vector3, Vector3>>> RightEyes = new List<IProducer<Tuple<Vector3, Vector3>>>();
        // public List<IProducer<ObjectGazeEvent>> LeftAvatarGazeEvents = new List<IProducer<ObjectGazeEvent>>();
        // public List<IProducer<ObjectGazeEvent>> LeftUnityGazeEvents = new List<IProducer<ObjectGazeEvent>>();
        // public List<IProducer<ObjectGazeEvent>> RightGazeEvents = new List<IProducer<ObjectGazeEvent>>();

        // Interactions
        public List<IProducer<Tuple<int, Queue<PieceStatus>>>> Grab = new List<IProducer<Tuple<int, Queue<PieceStatus>>>>();
        public List<IProducer<Tuple<int, Queue<PieceStatus>>>> Ungrab = new List<IProducer<Tuple<int, Queue<PieceStatus>>>>();
        public List<IProducer<Tuple<int, Queue<PieceStatus>>>> Placed = new List<IProducer<Tuple<int, Queue<PieceStatus>>>>();
        public List<IProducer<Tuple<int, Queue<PieceStatus>>>> Unplaced = new List<IProducer<Tuple<int, Queue<PieceStatus>>>>();

        // General
        public List<IProducer<string>> DeviceId = new List<IProducer<string>>();

        public List<string> Colorid = new List<string>() { "yellow", "green", "purple" };
        #endregion

        #endregion

        public GatherProducers()
        {
        }

        #region Get Producers

        /// <summary>
        /// The stream of each participant, looked up by its name in every store: a live
        /// session and a replayed dataset do not put a stream in the same store, but give it
        /// the same name. The list is empty unless every participant has the stream, so that
        /// the index in the list is always the participant.
        /// </summary>
        /// <typeparam name="T">Type of the stream.</typeparam>
        /// <param name="server">The pipeline owning the connectors.</param>
        /// <param name="subP">The pipeline the streams are bridged to.</param>
        /// <param name="numberOfParticipants">Number of participants, numbered from 1 in the names.</param>
        /// <param name="streamNames">The names the stream of a participant may have, by order of preference.</param>
        /// <param name="log">Where to report what was found.</param>
        /// <param name="reportMissing">False for a stream that only some sessions have: only finding it is reported.</param>
        /// <param name="accept">Says whether the stream of a store may be read; null to read any.</param>
        /// <returns>One stream per participant, or none.</returns>
        public List<IProducer<T>> FindProducers<T>(DatasetPipeline server, Pipeline subP, int numberOfParticipants, Func<int, string[]> streamNames, Action<string>? log = null, bool reportMissing = true, Func<ConnectorInfo, bool>? accept = null)
        {
            var found = new List<ConnectorInfo>();
            for (int i = 1; i <= numberOfParticipants; i++)
            {
                string[] names = streamNames(i);
                ConnectorInfo? connector = names
                    .Select(name => this.FindConnector<T>(server, name, accept))
                    .FirstOrDefault(candidate => candidate != null);
                if (connector == null)
                {
                    if (reportMissing)
                    {
                        log?.Invoke($"No stream {string.Join(" or ", names)} of type {typeof(T).Name}: the streams of this kind are left out.");
                    }

                    return new List<IProducer<T>>();
                }

                found.Add(connector);
            }

            var producers = new List<IProducer<T>>();
            foreach (ConnectorInfo connector in found)
            {
                producers.Add(connector.CreateBridge<T>(subP));
            }

            if (found.Count > 0)
            {
                // Session and store: the same name may be in several stores of a dataset.
                log?.Invoke($"Streams {string.Join(", ", found.Select(connector => connector.SourceName))} read from {string.Join(", ", found.Select(connector => $"{connector.SessionName}/{connector.StoreName}").Distinct())}.");
            }

            return producers;
        }

        /// <summary>
        /// The connector of a stream, looked up by its name in every store.
        /// </summary>
        /// <typeparam name="T">Type of the stream.</typeparam>
        /// <param name="server">The pipeline owning the connectors.</param>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="accept">Says whether the stream of a store may be read; null to read any.</param>
        /// <returns>The connector of the first store that holds the stream; null when none does.</returns>
        public ConnectorInfo? FindConnector<T>(DatasetPipeline server, string streamName, Func<ConnectorInfo, bool>? accept = null)
        {
            return server.Connectors.Values
                .Where(store => store.ContainsKey(streamName))
                .Select(store => store[streamName])
                .FirstOrDefault(candidate => candidate.DataType == typeof(T) && (accept == null || accept(candidate)));
        }

        /// <summary>
        /// The names of the streams of a type, in the order the stores list them, each one once.
        /// </summary>
        /// <typeparam name="T">Type of the streams.</typeparam>
        /// <param name="server">The pipeline owning the connectors.</param>
        /// <returns>The names.</returns>
        public List<string> FindStreamNames<T>(DatasetPipeline server)
        {
            return server.Connectors.Values
                .SelectMany(store => store.Where(stream => stream.Value.DataType == typeof(T)).Select(stream => stream.Key))
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// The number of messages the dataset records for a stream, looked up by its name.
        /// </summary>
        /// <param name="server">The pipeline owning the dataset.</param>
        /// <param name="streamName">Name of the stream.</param>
        /// <returns>The number of messages; 0 when it is not known, as in a live session.</returns>
        public long RecordedMessageCount(DatasetPipeline server, string streamName)
        {
            try
            {
                return server.Dataset?.Sessions
                    .SelectMany(session => session.Partitions)
                    .SelectMany(partition => partition.AvailableStreams)
                    .Where(stream => stream.Name == streamName)
                    .Select(stream => stream.MessageCount)
                    .DefaultIfEmpty(0)
                    .Max() ?? 0;
            }
            catch (Exception)
            {
                // The catalogue of a store that is being written may not be readable.
                return 0;
            }
        }

        public List<IProducer<bool>> GetVadProducers(DatasetPipeline server, Pipeline subP, string store, string type, int numberOfQuests, RendezVousPipeline.StoreMode storeMode, bool value)
        {
            var producers = new List<IProducer<bool>>();
            for (int i = 1; i < numberOfQuests + 1; i++)
            {
                var connectorKey = $"{type}{i}";

                switch (storeMode)
                {
                    case RendezVousPipeline.StoreMode.Independant:
                        if (value)
                        {
                            producers.Add(server.Connectors[$"{store}-{connectorKey}"][connectorKey].CreateBridge<bool>(subP));
                        }
                        else
                        {
                            producers.Add(server.Connectors[$"{store}{i}"][connectorKey].CreateBridge<bool>(subP));
                        }

                        break;
                    case RendezVousPipeline.StoreMode.Dictionnary:
                        if (value)
                        {
                            producers.Add(server.Connectors[$"{store}-{connectorKey}"][connectorKey].CreateBridge<bool>(subP));
                        }
                        else
                        {
                            producers.Add(server.Connectors[$"{store}{i}"][connectorKey].CreateBridge<bool>(subP));
                        }

                        break;
                }
            }

            return producers;
        }

        public List<IProducer<IStreamingSpeechRecognitionResult>> GetSTTProducers(DatasetPipeline server, Pipeline subP, string store, string type, int numberOfQuests, RendezVousPipeline.StoreMode storeMode, bool value)
        {
            var producers = new List<IProducer<IStreamingSpeechRecognitionResult>>();
            for (int i = 1; i < numberOfQuests + 1; i++)
            {
                var connectorKey = $"{type}{i}";

                switch (storeMode)
                {
                    case RendezVousPipeline.StoreMode.Independant:
                        if (value)
                        {
                            producers.Add(server.Connectors[$"{store}-{connectorKey}"][connectorKey].CreateBridge<IStreamingSpeechRecognitionResult>(subP));
                        }
                        else if (!value)
                        {
                            producers.Add(server.Connectors[$"{store}{i}"][connectorKey].CreateBridge<IStreamingSpeechRecognitionResult>(subP));
                        }

                        break;
                    case RendezVousPipeline.StoreMode.Dictionnary:
                        if (value)
                        {
                            producers.Add(server.Connectors[$"{store}-{connectorKey}"][connectorKey].CreateBridge<IStreamingSpeechRecognitionResult>(subP));
                        }
                        else if (!value)
                        {
                            producers.Add(server.Connectors[$"{store}{i}"][connectorKey].CreateBridge<IStreamingSpeechRecognitionResult>(subP));
                        }

                        break;
                }
            }

            return producers;
        }

        public List<IProducer<AudioBuffer>> GetAudioProducers(DatasetPipeline server, Pipeline subP, string store, string type, int numberOfQuests, RendezVousPipeline.StoreMode storeMode)
        {
            var producers = new List<IProducer<AudioBuffer>>();
            for (int i = 1; i < numberOfQuests + 1; i++)
            {
                var connectorKey = $"{type}{i}";
                switch (storeMode)
                {
                    case RendezVousPipeline.StoreMode.Independant:
                        if (server.Connectors.ContainsKey($"{store}-{connectorKey}"))
                        {
                            producers.Add(server.Connectors[$"{store}-{connectorKey}"][connectorKey].CreateBridge<AudioBuffer>(subP));
                        }
                        else
                        {
                            return null;
                        }

                        break;
                    case RendezVousPipeline.StoreMode.Dictionnary:
                        if (server.Connectors.ContainsKey($"{store}-{connectorKey}"))
                        {
                            producers.Add(server.Connectors[$"{store}-{connectorKey}"][connectorKey].CreateBridge<AudioBuffer>(subP));
                        }
                        else
                        {
                            return null;
                        }

                        break;
                }
            }

            return producers;
        }

        // Create Producers
        public List<IProducer<Tuple<Vector3, Vector3>>> CreateTupleVector3Producers(DatasetPipeline server, Pipeline subP, string store, string category, string type, int numberOfQuests, RendezVousPipeline.StoreMode storeMode)
        {
            var producers = new List<IProducer<Tuple<Vector3, Vector3>>>();
            for (int i = 1; i <= numberOfQuests; i++)
            {
                var connectorKey = $"{type}{i}-{category}";
                switch (storeMode)
                {
                    case RendezVousPipeline.StoreMode.Independant:
                        var id = $"{i}-{category}";
                        if (server.Connectors.ContainsKey($"{connectorKey}"))
                        {
                            producers.Add(server.Connectors[connectorKey][id].CreateBridge<Tuple<Vector3, Vector3>>(subP));
                        }

                        break;
                    case RendezVousPipeline.StoreMode.Dictionnary:
                        if (server.Connectors.ContainsKey(store))
                        {
                            producers.Add(server.Connectors[store][connectorKey].CreateBridge<Tuple<Vector3, Vector3>>(subP));
                        }

                        break;
                }
            }

            return producers;
        }

        public List<IProducer<TimeIntervalAnnotationSet>> CreateTimeIntervalAnnotationProducers(DatasetPipeline server, Pipeline subP, string store, string category, int numberOfQuests, RendezVousPipeline.StoreMode storeMode)
        {
            var producers = new List<IProducer<TimeIntervalAnnotationSet>>();
            for (int i = 1; i <= numberOfQuests; i++)
            {
                switch (storeMode)
                {
                    case RendezVousPipeline.StoreMode.Independant:
                        var connectorKey = $"{category}_{i}";
                        if (server.Connectors.ContainsKey(store))
                        {
                            producers.Add(server.Connectors[store][connectorKey].CreateBridge<TimeIntervalAnnotationSet>(subP));
                        }

                        break;
                    case RendezVousPipeline.StoreMode.Dictionnary:

                        break;
                }
            }

            return producers;
        }

        public List<IProducer<PieceStatus>> CreatePieceInteractionProducers(DatasetPipeline server, Pipeline subP, string store, string category, int numberOfQuests, RendezVousPipeline.StoreMode storeMode)
        {
            var producers = new List<IProducer<PieceStatus>>();
            for (int i = 1; i <= numberOfQuests; i++)
            {
                var connectorKey = $"UnityServer-{i}-{category}";

                switch (storeMode)
                {
                    case RendezVousPipeline.StoreMode.Independant:
                        var id = $"{i}-{category}";
                        if (server.Connectors.ContainsKey(connectorKey))
                        {
                            producers.Add(server.Connectors[connectorKey][id].CreateBridge<PieceStatus>(subP));
                        }

                        break;
                    case RendezVousPipeline.StoreMode.Dictionnary:
                        if (server.Connectors.ContainsKey(store))
                        {
                            producers.Add(server.Connectors[store][connectorKey].CreateBridge<PieceStatus>(subP));
                        }

                        break;
                }
            }

            return producers;
        }

        public List<IProducer<ObjectGazeEvent>> CreateGazeProducers(DatasetPipeline server, Pipeline subP, string store, string category, int numberOfQuests, RendezVousPipeline.StoreMode storeMode)
        {
            var producers = new List<IProducer<ObjectGazeEvent>>();
            for (int i = 1; i <= numberOfQuests; i++)
            {
                var connectorKey = $"UnityServer-{i}-{category}";

                switch (storeMode)
                {
                    case RendezVousPipeline.StoreMode.Independant:
                        var id = $"{i}-{category}";
                        if (server.Connectors.ContainsKey(connectorKey))
                        {
                            producers.Add(server.Connectors[connectorKey][id].CreateBridge<ObjectGazeEvent>(subP));
                        }

                        break;
                    case RendezVousPipeline.StoreMode.Dictionnary:
                        if (server.Connectors.ContainsKey(store))
                        {
                            producers.Add(server.Connectors[store][connectorKey].CreateBridge<ObjectGazeEvent>(subP));
                        }

                        break;
                }
            }

            return producers;
        }

        public List<IProducer<string>> CreateStringProducers(DatasetPipeline server, Pipeline subP, string store, string category, int numberOfQuests, RendezVousPipeline.StoreMode storeMode)
        {
            var producers = new List<IProducer<string>>();
            for (int i = 1; i <= numberOfQuests; i++)
            {
                switch (storeMode)
                {
                    case RendezVousPipeline.StoreMode.Independant:
                        var id = $"{i}-{category}";
                        var connectorKey = $"UnityServer-{i}-{category}";
                        if (server.Connectors.ContainsKey(connectorKey))
                        {
                            Console.WriteLine($"{store}_{connectorKey}");
                            producers.Add(server.Connectors[connectorKey][id].CreateBridge<string>(subP));
                        }

                        break;
                    case RendezVousPipeline.StoreMode.Dictionnary:

                        break;
                }
            }

            return producers;
        }
        #endregion
    }
}
