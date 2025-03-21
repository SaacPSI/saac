using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Components;
using Microsoft.Psi.Data;
using SAAC.PipelineServices;
using System.Collections.Generic;
using System.Linq;
using System;


namespace SAAC.AudioRecording
{
    internal class SetupTeam
    {

        public Pipeline p;
        public bool _isMicrophoneInit = false;
        public List<User> users = new List<User>();
        List<string> currentMicrophones = new List<string>();
        List<AudioSplitter> micAudio = new List<AudioSplitter>();
        public PsiExporter audioStore;
        public bool isImpairNumber = false;
        //public PsiStore audioStore;
        public SetupTeam(Pipeline p, Session session, string path)
        {
            this.p = p;
            audioStore = PsiStore.Create(p, "AudioData", $"{path}/{session.Name}/");
            session.AddPartitionFromPsiStoreAsync("AudioData", $"{path}/{session.Name}/");
        }
        public void AddUser(User user)
        {
            users.Add(user);
        }
        public void InitAudio(RendezVousPipeline server, bool value)
        {
            if (!server.Connectors.ContainsKey("Audio"))
                server.Connectors.Add("Audio", new Dictionary<string, ConnectorInfo>());

            int id = 0;
            foreach (User user in users)
            {
                id++;
                //if(isImpairNumber) if(id == users.Count)

                if ((int)user.microphone != -1)
                {
                    (string micString, int iduser) = Microphones[user.microphone];
                    if (!currentMicrophones.Contains(micString))
                    {
                        int nbrChannels = 2;
                        AudioCapture mic = new AudioCapture(p, new AudioCaptureConfiguration()
                        {
                            DeviceName = micString,
                            Format = WaveFormat.Create16BitPcm(16000, nbrChannels),
                            //AudioLevel = 0.1
                        });
                        //mic.Write($"Audio_" + micString, audioStore);
                        server.CreateConnectorAndStore($"Audio_" + micString, "Audio", server.GetSession("RawDataPipelineProcess.000"), p, mic.Out.Type, mic.Out, true);
                        AudioSplitter splitAudio = new AudioSplitter(p, nbrChannels);
                        mic.PipeTo(splitAudio);
                        currentMicrophones.Add(micString);
                        micAudio.Add(splitAudio);
                    }
                    int i = currentMicrophones.ToList().IndexOf(micString);
                    AudioSplitter audioSplit = micAudio.ToList()[i];
                    Emitter<AudioBuffer> audio = audioSplit.Audios[iduser - 1];

                    if (value)
                    {
                        server.Connectors["Audio"].Add("Audio_" + id, new ConnectorInfo("Audio_" + id, $"RawDataQuest{id}.000", "Audio", typeof(AudioBuffer), audio));
                        server.CreateConnectorAndStore($"{id}_Audio", "LiveVisualization", server.GetSession("RawDataPipelineProcess.000"), p, audio.Type, audio, true);
                    }
                    else
                    {
                        //server.Connectors["Audio"].Add(id + "_Audio", new ConnectorInfo(id + "_Audio", "RawDataAudio.000", "AudioData", typeof(AudioBuffer), audio));
                        server.CreateConnectorAndStore($"{id}_Audio", "Audio", server.GetSession("RawDataPipelineProcess.000"), p, audio.Type, audio, true);
                    }

                    //audio.Write("Audio" + id, audioStore);
                }
            }
            server.Log("Audio Initialized");
            server.TriggerNewProcessEvent("AudioInitialized");
            //_isMicrophoneInit = true;
        }
        public void InitAudioWithoutRDV(bool value)
        {
            int id = 0;
            foreach (User user in users)
            {
                id++;
                //if(isImpairNumber) if(id == users.Count)

                if ((int)user.microphone != -1)
                {
                    (string micString, int iduser) = Microphones[user.microphone];
                    if (!currentMicrophones.Contains(micString))
                    {
                        int nbrChannels = 2;
                        AudioCapture mic = new AudioCapture(p, new AudioCaptureConfiguration()
                        {
                            DeviceName = micString,
                            Format = WaveFormat.Create16BitPcm(16000, nbrChannels),
                            //AudioLevel = 0.1
                        });
                        mic.Write($"Audio_" + micString, audioStore);
                        AudioSplitter splitAudio = new AudioSplitter(p, nbrChannels);
                        mic.PipeTo(splitAudio);
                        currentMicrophones.Add(micString);
                        micAudio.Add(splitAudio);
                    }
                    int i = currentMicrophones.ToList().IndexOf(micString);
                    AudioSplitter audioSplit = micAudio.ToList()[i];
                    Emitter<AudioBuffer> audio = audioSplit.Audios[iduser - 1];

                    audio.Write("Audio_" + id, audioStore);
                }
            }
            //server.TriggerNewProcessEvent("FinishedInitializedAudio");
            //_isMicrophoneInit = true;++
        }

        public Dictionary<Microphone, (string, int)> Microphones = new Dictionary<Microphone, (string, int)>()
        {
            { Microphone.MicTXI1, ("Rode 1 (2- Wireless GO II RX)", 1) },
            { Microphone.MicTXI2, ("Rode 1 (2- Wireless GO II RX)", 2) },
            { Microphone.MicTXII1, ("Rode 2 (Wireless GO II RX)", 1) },
            { Microphone.MicTXII2, ("Rode 2 (Wireless GO II RX)", 2) },
            { Microphone.MicTXIII1, ("Rode 3 (3- Wireless GO II RX)", 1) },
            { Microphone.MicTXIII2, ("Rode 3 (3- Wireless GO II RX)", 2) },
            { Microphone.MicTXIV1, ("Rode 4 (4- Wireless GO II RX)", 1) },
            { Microphone.MicTXIV2, ("Rode 4 (4- Wireless GO II RX)", 2) },
        };
    }
}
