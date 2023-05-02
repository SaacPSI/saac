using Microsoft.Psi;
using WebRTC;

namespace TestingConsole
{
    internal class Program
    {   
        static void WebRTC(Pipeline p)
        {
            WebRTCVideoStreamConfiguration config = new WebRTCVideoStreamConfiguration();
            config.WebsocketAddress = System.Net.IPAddress.Loopback;
            config.WebsocketPort = 80;
            config.AudioStreaming = true;
            config.PixelStreamingConnection = true;
            config.FFMPEGFullPath = "D:\\ffmpeg\\bin";
            WebRTCVideoStream stream = new WebRTCVideoStream(p, config);
            var store = PsiStore.Create(p, "WebRTC", "F:\\Stores");

            store.Write(stream.OutImage, "Image");
            store.Write(stream.OutAudio, "Audio");
        }

        static void Main(string[] args)
        {
            // Enabling diagnotstics !!!
            Pipeline p = Pipeline.Create(enableDiagnostics: true);


            WebRTC(p);

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
