using Microsoft.Psi;
using Microsoft.Psi.Data;
using SAAC.PipelineServices;
using System.Collections.Generic;

namespace SAAC.AudioRecording
{
    public class SetupAudioRecording
    {
        public void SetupAudio(Pipeline p, RendezVousPipeline server, bool value, string path, int userNumber)
        {
            SetupTeam setupTeam = new SetupTeam(p, server.GetSession("RawDataAudio.000"), path);

            // Adaptative way to add User to Team and Initialize Microphones
            for (int i = 1; i < userNumber+1; i++)
            {
                User user = new User(i, CheckMicrophoneReference(i));
                setupTeam.AddUser(user);
            }
            //setupTeam.InitAudioWithoutRDV(value);
            setupTeam.InitAudio(server, value);
        }

        private static Microphone CheckMicrophoneReference(int userid)
        {
            Microphone microphone = Microphone.None;
            switch (userid)
            {
                case 1:
                    microphone = Microphone.MicTXI1; // yellow
                    break;
                case 2:
                    microphone = Microphone.MicTXI2; // green
                    break;
                case 3:
                    microphone = Microphone.MicTXII1; // purple
                    break;
                case 4:
                    microphone = Microphone.MicTXII2;
                    break;
                case 5:
                    microphone = Microphone.MicTXIII1;
                    break;
                case 6:
                    microphone = Microphone.MicTXIII2;
                    break;
                case 7:
                    microphone = Microphone.MicTXIV2;
                    break;
                case 8:
                    microphone = Microphone.MicTXIV1;
                    break;
                default:
                    break;
            }
            return microphone;
        }

        public void SetupAudioWithoutRDV(Pipeline p, Session session, bool value/*, Dataset dataset*/, string path)
        {
            //var session = server.Dataset.AddEmptySession("Audio");

            // Add User to Team and Initialize Microphones
            SetupTeam setupTeam = new SetupTeam(p, /*session*/ session, path);
            User user1 = new User(1, Microphone.MicTXI1);
            User user2 = new User(2, Microphone.MicTXI2);
            /*User user3 = new User(3, Microphone.MicTXII1);
            User user4 = new User(4, Microphone.MicTXII2);
            User user5 = new User(5, Microphone.MicTXIII1);
            User user6 = new User(6, Microphone.MicTXIII2);
            User user7 = new User(7, Microphone.MicTXIV2);*/
            //User user8 = new User(8, Microphone.MicTXIV2);

            setupTeam.AddUser(user1);
            setupTeam.AddUser(user2);
          /*  setupTeam.AddUser(user3);
            setupTeam.AddUser(user4);
            setupTeam.AddUser(user5);
            setupTeam.AddUser(user6);
            setupTeam.AddUser(user7);*/
            //setupTeam.AddUser(user8);

            setupTeam.InitAudioWithoutRDV(value);
            //dataset.Save();
        }
    }
}
