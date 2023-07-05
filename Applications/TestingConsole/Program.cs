using Microsoft.Psi;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.AzureKinect;
using OpenFace;
using System.Configuration;

namespace TestingConsole
{
    //internal class ProgramWebRTC
    //{
    //  using WebRTC;
    //    static void WebRTC(Pipeline p)
    //    {
    //        WebRTCVideoStreamConfiguration config = new WebRTCVideoStreamConfiguration();
    //        System.Net.IPAddress empadd;
    //        System.Net.IPAddress.TryParse("10.44.293.245", out empadd);
    //        config.WebsocketAddress = empadd;
    //        config.WebsocketPort = 11411;
    //        config.AudioStreaming = true;
    //        config.PixelStreamingConnection = true;
    //        config.FFMPEGFullPath = "D:\\ffmpeg\\bin";
    //        WebRTCVideoStream stream = new WebRTCVideoStream(p, config);
    //        var store = PsiStore.Create(p, "WebRTC", "F:\\Stores");

    //        store.Write(stream.OutImage, "Image");
    //        store.Write(stream.OutAudio, "Audio");
    //    }

    //    static void Main(string[] args)
    //    {
    //        // Enabling diagnotstics !!!
    //        Pipeline p = Pipeline.Create(enableDiagnostics: true);


    //        WebRTC(p);

    //        // RunAsync the pipeline in non-blocking mode.
    //        p.RunAsync(ReplayDescriptor.ReplayAllRealTime);
    //        // Wainting for an out key
    //        Console.WriteLine("Press any key to stop the application.");
    //        Console.ReadLine();
    //        // Stop correctly the pipeline.
    //        p.Dispose();
    //    }
    //}

    internal class ProgramOpenFace
    {
        static void OpenFace(Pipeline p)
        {

            var storeIn = PsiStore.Open(p, "Video", "F:\\Store\\");
            var video = storeIn.OpenStream<Shared<EncodedImage>>("video");

            ImageDecoder decoder = new ImageDecoder(p, new ImageFromBitmapStreamDecoder());

            OpenFaceConfiguration configuration = new OpenFaceConfiguration("./");
            configuration.Face = true;
            configuration.Eyes = true;
            OpenFace.OpenFace facer = new OpenFace.OpenFace(p, configuration);
            video.PipeTo(decoder.In);
            decoder.PipeTo(facer.In);
            //sensor.ColorImage.PipeTo(facer.In);

            FaceBlurrer faceBlurrer = new FaceBlurrer(p, "Blurrer");
            facer.OutPose.PipeTo(faceBlurrer.InPose);
            decoder.Out.PipeTo(faceBlurrer.InImage);
            //sensor.ColorImage.PipeTo(faceBlurrer.InImage);
            facer.OutPose.Do((image, e) => { Console.WriteLine("Pose!"); });
            facer.OutEyes.Do((image, e) => { Console.WriteLine("Eyes!"); });

            var store = PsiStore.Create(p, "Blurrer", "F:\\Stores");

            store.Write(faceBlurrer.Out.EncodeJpeg(), "Image");
        }

        static void Main(string[] args)
        {
            // Enabling diagnotstics !!!
            Pipeline p = Pipeline.Create(enableDiagnostics: true);

            OpenFace(p);

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
