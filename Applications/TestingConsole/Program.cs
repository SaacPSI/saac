using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using WebRTC;
//using Microsoft.Psi.Imaging;
//using Microsoft.Psi.AzureKinect;
//using OpenFace;
using System.Configuration;
using Microsoft.Psi.Imaging;
using RemoteConnectors;
using Microsoft.Psi.Interop.Rendezvous;
using static Microsoft.Psi.DeviceManagement.CameraDeviceInfo;

namespace TestingConsole
{
    internal class Program
    {
        //*****Uncomment OpenFace, Microsoft.Psi.Imaging and Microsoft.Psi.AzureKinect
        //***** Add OpenFace deps
        //   static void OpenFace(Pipeline p)
        //    {

        //        //Microsoft.Psi.Media.MediaCaptureConfiguration camConfig = new Microsoft.Psi.Media.MediaCaptureConfiguration();
        //        //Microsoft.Psi.Media.MediaCapture webcam = new Microsoft.Psi.Media.MediaCapture(p, camConfig);

        //        AzureKinectSensor webcam = new AzureKinectSensor(p);

        //        OpenFaceConfiguration configuration = new OpenFaceConfiguration("./");
        //        configuration.Face = false;
        //        configuration.Eyes = false;
        //        configuration.Pose = false;
        //        OpenFace.OpenFace facer = new OpenFace.OpenFace(p, configuration);
        //        webcam.ColorImage.PipeTo(facer.In);
        //        //sensor.ColorImage.PipeTo(facer.In);

        //        FaceBlurrer faceBlurrer = new FaceBlurrer(p, "Blurrer");
        //        facer.OutBoundingBoxes.PipeTo(faceBlurrer.InBBoxes);
        //        webcam.ColorImage.PipeTo(faceBlurrer.InImage);
        //        //sensor.ColorImage.PipeTo(faceBlurrer.InImage);

        //        var store = PsiStore.Create(p, "Blurrer", "D:\\Stores");
        //    }

        static void WebRTC(Pipeline p)
        {
            WebRTCVideoStreamConfiguration config = new WebRTCVideoStreamConfiguration();
            config.WebsocketAddress = System.Net.IPAddress.Loopback;
            config.WebsocketPort = 80;
            config.AudioStreaming = false;
            config.PixelStreamingConnection = false;
            config.FFMPEGFullPath = "D:\\ffmpeg\\bin";
            config.Log = Microsoft.Extensions.Logging.LogLevel.Information;
            WebRTCVideoStream stream = new WebRTCVideoStream(p, config);
            //var store = PsiStore.Create(p, "WebRTC", "F:\\Stores");

            //store.Write(stream.OutImage.EncodeJpeg(), "Image");
            //store.Write(stream.OutAudio, "Audio");

            //var emitter = new WebRTCDataChannelToEmitter<string>(p);

            //WebRTCDataConnectorConfiguration configuration = new WebRTCDataConnectorConfiguration();
            //configuration.WebsocketAddress = System.Net.IPAddress.Loopback;
            //configuration.WebsocketPort = 80;
            //configuration.PixelStreamingConnection = false;

            //configuration.OutputChannels.Add("test", emitter);
            //var incoming = WebRTCDataReceiverToChannelFactory.Create<TimeSpan>(p, "timing");
            //configuration.InputChannels.Add("timing", incoming);
            //WebRTCDataConnector connector = new WebRTCDataConnector(p, configuration);
            //var timer = Timers.Timer(p, TimeSpan.FromSeconds(1));
            //timer.Out.PipeTo(incoming.In);

            //store.Write(emitter.Out, "string");

        }

        static void testUnity(Pipeline p)
        {
            RemoteClockExporter exporter = new RemoteClockExporter(11511);
            
            RemoteImporter posImp = new RemoteImporter(p, "localhost", 11411);
            if (!posImp.Connected.WaitOne(-1))
            {
                throw new Exception("could not connect to server");
            }
            while (posImp.Importer.AvailableStreams.Count() == 0)
                Thread.Sleep(500);
            var pos = posImp.Importer.OpenStream<System.Numerics.Vector3>("Position");
            pos.Do(vec => Console.WriteLine("posImp : " + vec.ToString()));

            RemoteExporter remoteExporter = new RemoteExporter(p, 11412, TransportKind.Tcp);
            remoteExporter.Exporter.Write(pos, "Position2");
        }

        static bool alternate = true; 
        static void testKinectRemote(Pipeline p)
        {
            RendezvousServer server = new RendezvousServer(11411);
            KinectAzureRemoteConnector receiver = new KinectAzureRemoteConnector(p);
            server.Rendezvous.ProcessAdded += receiver.GenerateProcess();

            var emitter = p.CreateEmitter<KinectAzureRemoteStreamsConfiguration?>(p, "config");
            var timer = Timers.Timer(p, TimeSpan.FromSeconds(15));
            RemoteExporter exporter = new RemoteExporter(p, 11511, TransportKind.Tcp);
            KinectAzureRemoteStreamsConfiguration cfg = new KinectAzureRemoteStreamsConfiguration();
            cfg.StreamVideo = cfg.StreamSkeleton = false;
            timer.Out.Do(t => 
            {
                if (alternate)
                    emitter.Post(cfg, p.GetCurrentTime());
                else
                    emitter.Post(null, p.GetCurrentTime());
                alternate = !alternate;
                Console.WriteLine("Post");
            });
            exporter.Exporter.Write(emitter, "Configuration");
            if(!server.Rendezvous.TryAddProcess(new Rendezvous.Process("KinectStreaming_Configuration", new List<Rendezvous.Endpoint> { exporter.ToRendezvousEndpoint("10.44.192.131") })))
                Console.WriteLine("failed");
            server.Start();
            Console.WriteLine("start");
            //var store = PsiStore.Create(p, "testKinectRemote", "D:\\Stores");

        }


        static void Main(string[] args)
        {
            // Enabling diagnotstics !!!
            Pipeline p = Pipeline.Create(enableDiagnostics: true);

            testKinectRemote(p);
            //WebRTC(p);
            //testUnity(p);
            //OpenFace(p);

            try { 
            // RunAsync the pipeline in non-blocking mode.
            p.RunAsync(ReplayDescriptor.ReplayAll);
           
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            // Waiting for an out key
           Console.WriteLine("Press any key to stop the application.");
           Console.ReadLine();
           // Stop correctly the pipeline.
           p.Dispose();
        }
    }
}
