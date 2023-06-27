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
            Microsoft.Psi.Media.MediaCaptureConfiguration camConfig = new Microsoft.Psi.Media.MediaCaptureConfiguration();
            Microsoft.Psi.Media.MediaCapture webcam = new Microsoft.Psi.Media.MediaCapture(p, camConfig);

            //AzureKinectSensorConfiguration configKinect = new AzureKinectSensorConfiguration();
            //  AzureKinectSensor sensor = new AzureKinectSensor(p, configKinect);

            OpenFaceConfiguration configuration = new OpenFaceConfiguration("./");
            configuration.Face = true;
            configuration.Eyes = false;
            OpenFace.OpenFace facer = new OpenFace.OpenFace(p, configuration);
            webcam.Out.PipeTo(facer.In);
            //sensor.ColorImage.PipeTo(facer.In);

            FaceBlurrer faceBlurrer = new FaceBlurrer(p, "Blurrer");
            facer.PoseOut.PipeTo(faceBlurrer.InPose);
            webcam.Out.PipeTo(faceBlurrer.InImage);
            //sensor.ColorImage.PipeTo(faceBlurrer.InImage);

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
            p.RunAsync(ReplayDescriptor.ReplayAllRealTime);
           
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
