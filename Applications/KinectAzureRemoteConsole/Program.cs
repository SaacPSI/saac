using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using RemoteConnectors;

namespace KinectAzureRemoteConsole
{
    internal class Program
    {
        static void PipelineSetup(Pipeline pipeline, KinectAzureRemoteStreamsConfiguration config, string clockAddress, int clockPort)
        {
            Console.WriteLine("Wait RemoteClockImporter connection");
            RemoteClockImporter clockImporter = new RemoteClockImporter(pipeline, clockAddress, clockPort);
            if(clockImporter.Connected.WaitOne())
            {
                Console.WriteLine("RemoteClockImporter connection failed");
                return;
            }

            Console.WriteLine("RemoteClockImporter connected!");
            new KinectAzureRemoteServerComponent(pipeline, config);
            Console.WriteLine("KinectAzureRemoteServerComponent generated!");
        }

        static void Main(string[] args)
        {
            if(args.Length < 14)
                Console.WriteLine("Missing arguments !");
            try
            {
                Pipeline p = Pipeline.Create(enableDiagnostics: false);
                KinectAzureRemoteStreamsConfiguration config = new KinectAzureRemoteStreamsConfiguration();
                string clockAddress = args[0];
                int clockPort = int.Parse(args[1]);
                config.KinectDeviceIndex = int.Parse(args[2]);
                config.StreamAudio = bool.Parse(args[3]);
                config.StreamSkeleton = bool.Parse(args[4]);
                config.StreamVideo = bool.Parse(args[5]);
                config.StreamDepth = bool.Parse(args[6]);
                config.StreamDepthCalibration = bool.Parse(args[7]);
                config.StreamIMU = bool.Parse(args[8]);
                config.EncodingVideoLevel = int.Parse(args[9]);
                float videoWidth = float.Parse(args[10]);
                float videoHeigth = float.Parse(args[11]);
                if (videoWidth == 0.0 || videoHeigth == 0)
                    config.VideoResolution = null;
                else
                    config.VideoResolution = new Tuple<float, float>(videoWidth, videoHeigth);
                config.RendezVousAddress = args[12];
                config.RendezVousPort = int.Parse(args[13]);

                PipelineSetup(p, config, clockAddress, clockPort);
                // RunAsync the pipeline in non-blocking mode.
                Console.WriteLine("RunAsync!");
                p.RunAsync();
                // Waiting for an out key
                Console.WriteLine("Press any key to stop the application.");
                Console.ReadLine();
                // Stop correctly the pipeline.
                p.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("Press any key to stop the application.");
            Console.ReadLine();
        }
    }
}