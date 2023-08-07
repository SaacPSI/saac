using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using WebRTC;

namespace TestingConsole
{
    internal class Program
    {   
        static void WebRTC(Pipeline p)
        {
            RemoteClockExporter exporter = new RemoteClockExporter(11511);
            WebRTCVideoStreamConfiguration config = new WebRTCVideoStreamConfiguration();
            config.WebsocketAddress = System.Net.IPAddress.Loopback;
            config.WebsocketPort = 80;
            config.AudioStreaming = true;
            config.PixelStreamingConnection = false;
            config.FFMPEGFullPath = "D:\\ffmpeg\\bin";
            config.Log = Microsoft.Extensions.Logging.LogLevel.Information;
            WebRTCVideoStream stream = new WebRTCVideoStream(p, config);
            var store = PsiStore.Create(p, "WebRTC", "F:\\Stores");

            //store.Write(stream.OutImage, "Image");
            store.Write(stream.OutAudio, "Audio");

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

        static void Main(string[] args)
        {
            // Enabling diagnotstics !!!
            Pipeline p = Pipeline.Create(enableDiagnostics: true);


            WebRTC(p);
            //testUnity(p);

            // RunAsync the pipeline in non-blocking mode.
            p.RunAsync(ReplayDescriptor.ReplayAllRealTime);
            // Wainting for an out key
            Console.WriteLine("Press any key to stop the application.");
            Console.ReadLine();
            // Stop correctly the pipeline.
            p.Dispose();
        }
    }
}
