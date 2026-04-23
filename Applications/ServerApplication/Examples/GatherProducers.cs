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
        

        public List<IProducer<bool>> GetVadProducers(DatasetPipeline server, Pipeline subP, string store, string type, int numberOfQuests, bool value)
        {
            var producers = new List<IProducer<bool>>();
            for (int i = 1; i < numberOfQuests + 1; i++)
            {
                var connectorKey = $"{type}{i}";

                if (value)
                {
                    producers.Add(server.Connectors[$"{store}"][connectorKey].CreateBridge<bool>(subP));
                }
                else
                {
                    producers.Add(server.Connectors[$"{store}{i}"][connectorKey].CreateBridge<bool>(subP));
                }
            }

            return producers;
        }

        public List<IProducer<IStreamingSpeechRecognitionResult>> GetSTTProducers(DatasetPipeline server, Pipeline subP, string store, string type, int numberOfQuests, bool value)
        {
            var producers = new List<IProducer<IStreamingSpeechRecognitionResult>>();
            for (int i = 1; i < numberOfQuests + 1; i++)
            {
                var connectorKey = $"{type}{i}";
                if (value)
                {
                    producers.Add(server.Connectors[$"{store}"][connectorKey].CreateBridge<IStreamingSpeechRecognitionResult>(subP));
                }
                else if (!value)
                {
                    producers.Add(server.Connectors[$"{store}{i}"][connectorKey].CreateBridge<IStreamingSpeechRecognitionResult>(subP));
                }
            }

            return producers;
        }

        public List<IProducer<AudioBuffer>>? GetAudioProducers(DatasetPipeline server, Pipeline subP, string store, string type, int numberOfQuests)
        {
            var producers = new List<IProducer<AudioBuffer>>();
            for (int i = 1; i < numberOfQuests + 1; i++)
            {
                var connectorKey = $"{type}{i}";
                if (server.Connectors.ContainsKey($"{store}{i}"))
                {
                    producers.Add(server.Connectors[$"{store}{i}"][connectorKey].CreateBridge<AudioBuffer>(subP));
                }
                else
                {
                    return null;
                }
            }

            return producers;
        }

        // Create Producers
        public List<IProducer<Tuple<Vector3, Vector3>>> CreateTupleVector3Producers(DatasetPipeline server, Pipeline subP, string store, string category, string type, int numberOfQuests)
        {
            var producers = new List<IProducer<Tuple<Vector3, Vector3>>>();
            for (int i = 1; i <= numberOfQuests; i++)
            {
                var connectorKey = $"{type}{i}-{category}";
                if (server.Connectors.ContainsKey(store))
                {
                    producers.Add(server.Connectors[store][connectorKey].CreateBridge<Tuple<Vector3, Vector3>>(subP));
                }
            }

            return producers;
        }

        public List<IProducer<TimeIntervalAnnotationSet>> CreateTimeIntervalAnnotationProducers(DatasetPipeline server, Pipeline subP, string store, string category, int numberOfQuests)
        {
            var producers = new List<IProducer<TimeIntervalAnnotationSet>>();
            for (int i = 1; i <= numberOfQuests; i++)
            {
                var connectorKey = $"{category}_{i}";
                if (server.Connectors.ContainsKey(store))
                {
                    producers.Add(server.Connectors[store][connectorKey].CreateBridge<TimeIntervalAnnotationSet>(subP));
                }
            }

            return producers;
        }

        public List<IProducer<PieceStatus>> CreatePieceInteractionProducers(DatasetPipeline server, Pipeline subP, string store, string category, int numberOfQuests)
        {
            var producers = new List<IProducer<PieceStatus>>();
            for (int i = 1; i <= numberOfQuests; i++)
            {
                var connectorKey = $"UnityServer-{i}-{category}";
                if (server.Connectors.ContainsKey(store))
                {
                    producers.Add(server.Connectors[store][connectorKey].CreateBridge<PieceStatus>(subP));
                }
            }

            return producers;
        }

        public List<IProducer<ObjectGazeEvent>> CreateGazeProducers(DatasetPipeline server, Pipeline subP, string store, string category, int numberOfQuests)
        {
            var producers = new List<IProducer<ObjectGazeEvent>>();
            for (int i = 1; i <= numberOfQuests; i++)
            {
                var connectorKey = $"UnityServer-{i}-{category}";
                if (server.Connectors.ContainsKey(store))
                {
                    producers.Add(server.Connectors[store][connectorKey].CreateBridge<ObjectGazeEvent>(subP));
                }
            }

            return producers;
        }

        public List<IProducer<string>> CreateStringProducers(DatasetPipeline server, Pipeline subP, string store, string category, int numberOfQuests)
        {
            var producers = new List<IProducer<string>>();
            for (int i = 1; i <= numberOfQuests; i++)
            {
                var connectorKey = $"UnityServer-{i}-{category}";
                if (server.Connectors.ContainsKey(store))
                {
                    Console.WriteLine($"{store}_{connectorKey}");
                    producers.Add(server.Connectors[store][connectorKey].CreateBridge<string>(subP));
                }
            }

            return producers;
        }
        #endregion
    }
}
