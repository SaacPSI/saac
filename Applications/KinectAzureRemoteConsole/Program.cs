// <copyright file="Program.cs" company="SAAC">
// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
// </copyright>

using SAAC.PipelineServices;
using SAAC.RemoteConnectors;

namespace KinectAzureRemoteConsole
{
    internal class KinectAzureRemote
    {
        private RendezVousPipelineConfiguration configRdv = new RendezVousPipelineConfiguration(false);
        private KinectAzureRemoteStreamsConfiguration configKinect = new KinectAzureRemoteStreamsConfiguration();
        private RendezVousPipeline client;
        private KinectAzureRemoteStreams? kinect = null;
        private string commandServer;

        public KinectAzureRemote(string[] args)
        {
            configRdv.ClockPort = configRdv.CommandPort = 0;
            configRdv.RecordIncomingProcess = false;
            string server = args[0];
            configRdv.RendezVousPort = int.Parse(args[1]);
            commandServer = args[2];
            configRdv.CommandDelegate += CommandRecieved;
            configRdv.AutomaticPipelineRun = true;
            string applicationName = configKinect.RendezVousApplicationName = args[3];
            client = new RendezVousPipeline(configRdv, applicationName, server);
            UpdateConfigurationFromArgs(args.Skip(3).ToArray());
            client.Start();
        }

        private bool UpdateConfigurationFromArgs(string[] args)
        {
            if (args.Length < 12)
            {
                client.Log($"UpdateConfigurationFromArgs failed only {args.Length} needed 13");
                return false;
            }
            try
            {
                configKinect.KinectDeviceIndex = int.Parse(args[1]);
                configKinect.OutputAudio = bool.Parse(args[2]);
                configKinect.OutputBodies = bool.Parse(args[3]);
                configKinect.OutputColor = bool.Parse(args[4]);
                configKinect.OutputDepth = bool.Parse(args[5]);
                configKinect.OutputCalibration = bool.Parse(args[6]);
                configKinect.OutputImu = bool.Parse(args[7]);
                configKinect.EncodingVideoLevel = int.Parse(args[8]);
                configKinect.ColorResolution = (Microsoft.Azure.Kinect.Sensor.ColorResolution)int.Parse(args[9]);
                configKinect.CameraFPS = (Microsoft.Azure.Kinect.Sensor.FPS)int.Parse(args[10]);
                configKinect.IpToUse = args[11];
                configKinect.StartingPort = int.Parse(args[12]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            client.Log($"UpdateConfigurationFromArgs done.");
            return true;
        }

        void SetupKinect()
        {
            if (kinect != null)
                return;
            kinect = new KinectAzureRemoteStreams(client.GetOrCreateSubpipeline("azure"), configKinect, configKinect.RendezVousApplicationName);
            client.AddProcess(kinect.GenerateProcess());
            kinect.RunAsync();
            client.Log($"SetupKinect done.");
        }

        void StopKinect()
        {
            if (kinect == null)
                return;
            client.RemoveProcess(configKinect.RendezVousApplicationName);
            kinect.Dispose();
            kinect = null;
            client.Log($"StopKinect done.");
        }

        void CommandRecieved(string source, Microsoft.Psi.Message<(RendezVousPipeline.Command, string)> message)
        {
            if ($"{commandServer}-Command" != source)
                return;
            var args = message.Data.Item2.Split([';']);

            if (args[0] != configKinect.RendezVousApplicationName)
                return;

            client.Log($"CommandRecieved with {message.Data.Item1} command, args: {message.Data.Item2}.");
            switch (message.Data.Item1)
            {
                case RendezVousPipeline.Command.Initialize:
                    UpdateConfigurationFromArgs(args);
                    break;
                case RendezVousPipeline.Command.Run:
                    SetupKinect();
                    break;
                case RendezVousPipeline.Command.Stop:
                    StopKinect();
                    break;
                case RendezVousPipeline.Command.Close:
                    StopKinect();
                    client.Stop();
                    throw new Exception("Ugly way to close");
                case RendezVousPipeline.Command.Reset:
                    if (UpdateConfigurationFromArgs(args))
                    {
                        StopKinect();
                        SetupKinect();
                    }
                    break;
            }
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 4)
                Console.WriteLine("Missing arguments !");
            try
            {
                new KinectAzureRemote(args);
                // Waiting for an out key
                Console.WriteLine("Press any key to stop the application.");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }
    }
}
