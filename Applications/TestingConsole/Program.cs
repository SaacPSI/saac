using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Rendezvous;
using WebRTC;
//using Microsoft.Psi.Imaging;
//using Microsoft.Psi.AzureKinect;
//using OpenFace;
using System.Configuration;
using Microsoft.Psi.Imaging;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.Psi.Components;

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

        static void WebRTCVideoAudio(Pipeline p)
        {
            WebRTCVideoStreamConfiguration config = new WebRTCVideoStreamConfiguration();
            config.WebsocketAddress = System.Net.IPAddress.Loopback;
            config.WebsocketPort = 80;
            config.AudioStreaming = false;
            config.PixelStreamingConnection = false;
            config.FFMPEGFullPath = "D:\\ffmpeg\\bin";
            config.Log = Microsoft.Extensions.Logging.LogLevel.Information;
            WebRTCVideoStream stream = new WebRTCVideoStream(p, config);
            var store = PsiStore.Create(p, "WebRTC", "F:\\Stores");

            store.Write(stream.OutImage.EncodeJpeg(), "Image");
            store.Write(stream.OutAudio, "Audio");
        }

        static void FullWebRTC(Pipeline p)
        {
            WebRTCVideoStreamConfiguration config = new WebRTCVideoStreamConfiguration();
            config.WebsocketAddress = System.Net.IPAddress.Parse("127.0.0.1");
            config.WebsocketPort = 80;
            config.AudioStreaming = false;
            config.PixelStreamingConnection = false;
            config.FFMPEGFullPath = "D:\\ffmpeg\\bin\\";
            config.Log = Microsoft.Extensions.Logging.LogLevel.Information;

            var emitter = new WebRTCDataChannelToEmitter<string>(p);
            var incoming = WebRTCDataReceiverToChannelFactory.Create<TimeSpan>(p, "timing");
            config.OutputChannels.Add("Events", emitter);
            config.InputChannels.Add("Timing", incoming);

            WebRTCVideoStream stream = new WebRTCVideoStream(p, config);
            var store = PsiStore.Create(p, "WebRTC", "D:\\Stores");

            store.Write(stream.OutImage.EncodeJpeg(), "Image");
            store.Write(stream.OutAudio, "Audio");
            store.Write(emitter.Out, "Events");

            var timer = Timers.Timer(p, TimeSpan.FromSeconds(1));
            timer.Out.PipeTo(incoming.In);    
        }

        static void UnityDemo(Pipeline p)
        {
            string host = "127.0.0.1";
            var server = new RendezvousServer();
            var process = new Rendezvous.Process("Console");

            RemoteClockExporter exporter = new RemoteClockExporter(11511);
            process.AddEndpoint(exporter.ToRendezvousEndpoint(host));

            RemoteExporter remoteExporter = new RemoteExporter(p, 11412, TransportKind.Tcp);
            var timer = Timers.Timer(p, TimeSpan.FromSeconds(5));
            remoteExporter.Exporter.Write(timer.Out, "PingInter");
            process.AddEndpoint(remoteExporter.ToRendezvousEndpoint(host));

            server.Rendezvous.ProcessAdded += (_, process) =>
            {
                Console.WriteLine($"Process added: {process.Name}");
                if (process.Name.Contains("Console"))
                    return;
                Subpipeline subP = new Subpipeline(p, process.Name);
                Rendezvous.Process? processF = null;
                var clone = process.Endpoints.DeepClone();
                foreach (var endpoint in clone)
                {
                    if (endpoint is Rendezvous.RemoteExporterEndpoint remoteEndpoint)
                    {
                        RemoteImporter remoteImporter = remoteEndpoint.ToRemoteImporter(subP);
                        if (remoteImporter.Connected.WaitOne() == false)
                            continue;
                        foreach (Rendezvous.Stream stream in remoteEndpoint.Streams)
                        {
                            Console.WriteLine($"Stream : {stream.StreamName}");
                            if (stream.StreamName is "Position")
                            {
                                var pos = remoteImporter.Importer.OpenStream<Vector3>("Position");

                                var emiOut = subP.CreateEmitter<Vector3>(pos, "modificator"); 
                                pos.Do((vec, env) => { Console.WriteLine("posImp : " + vec.ToString()); emiOut.Post(vec + Vector3.One, env.OriginatingTime); }) ;
                                processF = new Rendezvous.Process("ConsoleForward");
                                RemoteExporter remoteF = new RemoteExporter(p, 11420, TransportKind.Tcp);
                                
                                remoteF.Exporter.Write(emiOut, "PositionModified");
                                processF.AddEndpoint(remoteF.ToRendezvousEndpoint(host));
                            }
                        }
                    }
                }
                if(processF != null)
                    server.Rendezvous.TryAddProcess(processF);
                subP.RunAsync();
            };

            server.Rendezvous.TryAddProcess(process);
            server.Start();
        }

        static void Main(string[] args)
        {
            // Enabling diagnotstics !!!
            Pipeline p = Pipeline.Create(enableDiagnostics: true);

            //FullWebRTC(p);
            UnityDemo(p);
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
