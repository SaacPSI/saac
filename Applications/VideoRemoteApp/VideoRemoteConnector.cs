using Microsoft.Psi;
using SAAC.RendezVousPipelineServices;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Media;
using System.Text.RegularExpressions;

namespace VideoRemoteApp
{
    public class VideoRemoteConnectorConfiguration
    {
        public string RendezVousAddress { get; set; } = "10.144.210.100";
        public int RendezVousPort { get; set; } = 13331;
        public int ExportPort { get; set; } = 11550;
        public TransportKind ConnectionType { get; set; } = TransportKind.Tcp;
        public int EncodingVideoLevel { get; set; } = 75;
        public Tuple<float, float>? VideoResolution { get; set; } = new Tuple<float, float>(1920.0f, 1080.0f);
        public string RendezVousApplicationName { get; set; } = "VideoServer";
        public string AppName { get; set; } = "SpacePipeline_Main";
    }
    public class VideoRemoteConnector : Subpipeline
    {
        public VideoRemoteConnectorConfiguration Configuration { get; private set; }
        public Shared<Image> Image { get; private set; }
        protected Pipeline p;
        public VideoRemoteConnector(Pipeline pipeline, VideoRemoteConnectorConfiguration? configuration = null, string name = nameof(VideoRemoteConnector)) : base(pipeline, name)
        {
            p = pipeline;
            Configuration = configuration ?? new VideoRemoteConnectorConfiguration();
        }
        public Rendezvous.Process GenerateProcess()
        {
            List<Rendezvous.Endpoint> exporters = new List<Rendezvous.Endpoint>();

            // Test webcam stream
            /*var webcam = new MediaCapture(p, 1280, 720, 30);
            var encodedCapture = webcam.Out.EncodeJpeg(Configuration.EncodingVideoLevel, DeliveryPolicy.LatestMessage);*/

            // Application windows capture
            
            WindowCaptureConfiguration cfg = new WindowCaptureConfiguration() { Interval = TimeSpan.FromMilliseconds(50) };
            //cfg.WindowHandle = Process.GetProcessesByName(Configuration.AppName)[0].MainWindowHandle;
            WindowCapture capture = new WindowCapture(p, cfg);
            var encodedCapture = capture.Out.EncodeJpeg(Configuration.EncodingVideoLevel, DeliveryPolicy.LatestMessage);
            //encodedCapture.Sample(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(5));

            RemoteExporter imageExporter = new RemoteExporter(this, Configuration.ExportPort, Configuration.ConnectionType);
            imageExporter.Exporter.Write(encodedCapture.Out, "VideoStreaming");
            exporters.Add(imageExporter.ToRendezvousEndpoint(Configuration.RendezVousAddress));

            return new Rendezvous.Process(Configuration.RendezVousApplicationName, exporters, "Version1.0");
        }
    }
}
