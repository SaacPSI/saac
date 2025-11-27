using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Rendezvous;
//using WebRTC;
using Microsoft.Psi.Imaging;
//using Microsoft.Psi.AzureKinect;
//using Bodies;
//using OpenFace;
using System.Configuration;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.Psi.Components;
using SAAC;
//using SAAC.Bodies;
using System.Text;
//using SAAC.Groups;
using Microsoft.Psi.Interop.Serialization;
using Microsoft.Psi.Interop.Transport;
using static Microsoft.Psi.Interop.Rendezvous.Rendezvous;
using System.IO;
using SAAC.PipelineServices;
using SAAC.Helpers;
using static SAAC.PipelineServices.RendezVousPipeline;
using System.Windows;
//using PLUME;
//using SAAC.Ollama;
using SAAC.LabStreamLayer;
using static LSL.liblsl;

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

        //static void WebRTCVideoAudio(Pipeline p)
        //{
        //    WebRTCVideoStreamConfiguration config = new WebRTCVideoStreamConfiguration();
        //    config.WebsocketAddress = System.Net.IPAddress.Loopback;
        //    config.WebsocketPort = 80;
        //    config.AudioStreaming = false;
        //    config.PixelStreamingConnection = false;
        //    config.FFMPEGFullPath = "D:\\ffmpeg\\bin";
        //    config.Log = Microsoft.Extensions.Logging.LogLevel.Information;
        //    WebRTCVideoStream stream = new WebRTCVideoStream(p, config);
        //    var store = PsiStore.Create(p, "WebRTC", "F:\\Stores");

        //    store.Write(stream.OutImage.EncodeJpeg(), "Image");
        //    store.Write(stream.OutAudio, "Audio");
        //}

        //static void FullWebRTC(Pipeline p)
        //{
        //    WebRTCVideoStreamConfiguration config = new WebRTCVideoStreamConfiguration();
        //    config.WebsocketAddress = System.Net.IPAddress.Parse("127.0.0.1");
        //    config.WebsocketPort = 80;
        //    config.AudioStreaming = false;
        //    config.PixelStreamingConnection = false;
        //    config.FFMPEGFullPath = "D:\\ffmpeg\\bin\\";
        //    config.Log = Microsoft.Extensions.Logging.LogLevel.Information;

        //    var emitter = new WebRTCDataChannelToEmitter<string>(p);
        //    var incoming = WebRTCDataReceiverToChannelFactory.Create<TimeSpan>(p, "timing");
        //    config.OutputChannels.Add("Events", emitter);
        //    config.InputChannels.Add("Timing", incoming);

        //    WebRTCVideoStream stream = new WebRTCVideoStream(p, config);
        //    var store = PsiStore.Create(p, "WebRTC", "D:\\Stores");

        //    store.Write(stream.OutImage.EncodeJpeg(), "Image");
        //    store.Write(stream.OutAudio, "Audio");
        //    store.Write(emitter.Out, "Events");

        //    var timer = Timers.Timer(p, TimeSpan.FromSeconds(1));
        //    timer.Out.PipeTo(incoming.In);    
        //}

        //static void UnityDemo(Pipeline p)
        //{
        //    string host = "127.0.0.1";
        //    var server = new RendezvousServer();
        //    var process = new Rendezvous.Process("Console");

        //    RemoteClockExporter exporter = new RemoteClockExporter(11511);
        //    process.AddEndpoint(exporter.ToRendezvousEndpoint(host));

        //    RemoteExporter remoteExporter = new RemoteExporter(p, 11412, TransportKind.Tcp);
        //    var timer = Timers.Timer(p, TimeSpan.FromSeconds(5));
        //    remoteExporter.Exporter.Write(timer.Out, "PingInter");
        //    process.AddEndpoint(remoteExporter.ToRendezvousEndpoint(host));

        //    server.Rendezvous.ProcessAdded += (_, process) =>
        //    {
        //        Console.WriteLine($"Process added: {process.Name}");
        //        if (process.Name.Contains("Console"))
        //            return;
        //        Subpipeline subP = new Subpipeline(p, process.Name);
        //        Rendezvous.Process? processF = null;
        //        var clone = process.Endpoints.DeepClone();
        //        foreach (var endpoint in clone)
        //        {
        //            if (endpoint is Rendezvous.RemoteExporterEndpoint remoteEndpoint)
        //            {
        //                RemoteImporter remoteImporter = remoteEndpoint.ToRemoteImporter(subP);
        //                if (remoteImporter.Connected.WaitOne() == false)
        //                    continue;
        //                foreach (Rendezvous.Stream stream in remoteEndpoint.Streams)
        //                {
        //                    Console.WriteLine($"Stream : {stream.StreamName}");
        //                    if (stream.StreamName is "Position")
        //                    {
        //                        var pos = remoteImporter.Importer.OpenStream<Vector3>("Position");

        //                        var emiOut = subP.CreateEmitter<Vector3>(pos, "modificator"); 
        //                        pos.Do((vec, env) => { Console.WriteLine("posImp : " + vec.ToString()); emiOut.Post(vec + Vector3.One, env.OriginatingTime); }) ;
        //                        processF = new Rendezvous.Process("ConsoleForward");
        //                        RemoteExporter remoteF = new RemoteExporter(p, 11420, TransportKind.Tcp);

        //                        remoteF.Exporter.Write(emiOut, "PositionModified");
        //                        processF.AddEndpoint(remoteF.ToRendezvousEndpoint(host));
        //                    }
        //                }
        //            }
        //        }
        //        if(processF != null)
        //            server.Rendezvous.TryAddProcess(processF);
        //        subP.RunAsync();
        //    };

        //    server.Rendezvous.TryAddProcess(process);
        //    server.Start();
        //}

        //static void testBodies(Pipeline p)
        //{
        //    AzureKinectSensorConfiguration configKinect = new AzureKinectSensorConfiguration();
        //    configKinect.DeviceIndex = 0;
        //    configKinect.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
        //    AzureKinectSensor sensor = new AzureKinectSensor(p, configKinect);

        //    /*** BODIES CONVERTERS ***/
        //    Bodies.BodiesConverter bodiesConverter = new Bodies.BodiesConverter(p);

        //    Bodies.HandsProximityDetectorConfiguration configHands = new Bodies.HandsProximityDetectorConfiguration();
        //    configHands.IsPairToCheckGiven = false;
        //    Bodies.HandsProximityDetector detector = new Bodies.HandsProximityDetector(p, configHands);


        //    Bodies.BodyPosturesDetectorConfiguration configPostures = new Bodies.BodyPosturesDetectorConfiguration();
        //    Bodies.BodyPosturesDetector postures = new Bodies.BodyPosturesDetector(p, configPostures);

        //    sensor.Bodies.PipeTo(bodiesConverter.InBodiesAzure);

        //    //Connector<List<(uint, uint)>> connector = p.CreateConnector<List<(uint, uint)>>("random");
        //    //connector.Out.PipeTo(detector.InPair);
        //    //sensor.Bodies.Do((m, e) =>
        //    //{
        //    //    if (m.Count > 0)
        //    //    {
        //    //        uint id = m.First().TrackingId;
        //    //        List<(uint, uint)> list = new List<(uint, uint)> { (id, id) };
        //    //        connector.Out.Post(list, e.OriginatingTime);
        //    //    }
        //    //});
        //    bodiesConverter.Out.PipeTo(detector.In);
        //    bodiesConverter.Out.PipeTo(postures.In);

        //    detector.Out.Do((m, e) => { 
        //        foreach (var data in m)
        //        {
        //            foreach(var item in data.Value)
        //                Console.WriteLine($"{data.Key} - {item}");
        //        } });

        //    postures.Out.Do((m, e) => {
        //        foreach (var data in m)
        //        {
        //            foreach (var item in data.Value)
        //                Console.WriteLine($"{data.Key} - {item}");
        //        }
        //    });
        //}

        //static void testOllama(Pipeline p)
        //{
        //    OllamaConectorConfiguration config = new OllamaConectorConfiguration();
        //    OllamaConnector ollama = new OllamaConnector(p, config);
        //    KeyboardReader.KeyboardReader reader = new KeyboardReader.KeyboardReader(p);

        //    reader.Out.PipeTo(ollama.In);
        //    ollama.Out.Do((m, e) => { Console.WriteLine($"{(e.CreationTime - e.OriginatingTime).TotalSeconds} \n {m}"); });
        //}

        //static void testGroups(Pipeline p)
        //{
        //    var azureConfig = new AzureKinectSensorConfiguration();
        //    azureConfig.BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration();
        //    azureConfig.BodyTrackerConfiguration.CpuOnlyMode = false;
        //    var azureKinect = new AzureKinectSensor(p, azureConfig);



        //    // Create the store component
        //    var store = PsiStore.Create(p, "Azure", "D:\\Stores");

        //    // Write incoming data in the store
        //    //store.Write(azureKinect.ColorImage, "Color");
        //    //store.Write(azureKinect.DepthImage, "Depth");
        //    store.Write(azureKinect.Bodies, "Bodies");
        //    store.Write(azureKinect.Imu, "Imu");

        //    BodiesConverter converter = new BodiesConverter(p);
        //    azureKinect.Bodies.PipeTo(converter.InBodiesAzure);
        //    store.Write(converter.Out, "BodiesS");
        //    converter.Out.Do((d, e) => { Console.WriteLine("c"); });

        //    SimpleBodiesPositionExtraction extractor = new SimpleBodiesPositionExtraction(p);
        //    azureKinect.Bodies.PipeTo(extractor.InBodiesAzure);

        //    SimplifiedFlockGroupsDetector groupsDetector = new SimplifiedFlockGroupsDetector(p);
        //    extractor.Out.PipeTo(groupsDetector.In);

        //    store.Write(groupsDetector.Out, "Groups");
        //    groupsDetector.Out.Do((d,e) => { Console.WriteLine("g"); });
        //}

        //static private void Connection<T>(string name, TcpSourceEndpoint? source, Pipeline p, Format<T> deserializer)
        //{
        //    source?.ToTcpSource<T>(p, deserializer, null, true, name).Do((d, e) => { Console.WriteLine($"Recieve {name} data @{e} : {d}"); });
        //}

        //static void Quest2Demo(Pipeline p)
        //{
        //    //var host = "192.168.1.191";
        //    var host = "10.44.192.131";
        //    var remoteClock = new RemoteClockExporter(port: 11510);

        //    //Light
        //    Emitter<bool> lightEmitter = p.CreateEmitter<bool>(p, "lightEmitter");
        //    var timer = Timers.Timer(p, TimeSpan.FromSeconds(1));
        //    bool alternate = false;
        //    timer.Out.Do(t =>
        //    {
        //        Console.WriteLine($"Send {alternate}");
        //        lightEmitter.Post(alternate, p.GetCurrentTime());
        //        alternate = !alternate;
        //    });
        //    TcpWriter<bool> tcpWiter = new TcpWriter<bool>(p, 11511, PsiFormatBoolean.GetFormat(), "Light");
        //    lightEmitter.PipeTo(tcpWiter);

        //    bool canStart = false;
        //    var process = new Rendezvous.Process("Server", new[] { remoteClock.ToRendezvousEndpoint(host), tcpWiter.ToRendezvousEndpoint<bool>(host, "Light") });
        //    var server = new RendezvousServer();
        //    server.Rendezvous.TryAddProcess(process);
        //    server.Rendezvous.ProcessAdded += (_, pr) =>
        //    {
        //        Console.WriteLine($"Process {pr.Name}");
        //        if (pr.Name == "Unity")
        //        {
        //            foreach (var endpoint in pr.Endpoints)
        //            {
        //                if (endpoint is Rendezvous.TcpSourceEndpoint)
        //                {
        //                    TcpSourceEndpoint? source = endpoint as TcpSourceEndpoint;
        //                    foreach (var stream in endpoint.Streams)
        //                    {
        //                        Console.WriteLine($"\tStream {stream.StreamName}");
        //                        switch (stream.StreamName)
        //                        {
        //                            case "PositionLeft":
        //                            case "PositionRight":
        //                            case "Left":
        //                            case "Right":
        //                            case "Player":
        //                                Connection<Tuple<Vector3, Vector3>>(stream.StreamName, source, p, PsiFormatTupleOfVector.GetFormat());
        //                                break;
        //                            case "OutDigiCode":
        //                                Connection<char>(stream.StreamName, source, p, PsiFormaChar.GetFormat());
        //                                break;
        //                            case "IsSuccess":
        //                                Connection<bool>(stream.StreamName, source, p, PsiFormatBoolean.GetFormat());
        //                                break;
        //                            case "Area":
        //                                Connection<string>(stream.StreamName, source, p, PsiFormatString.GetFormat());
        //                                break;
        //                            case "Camera":
        //                                Connection<byte[]>(stream.StreamName, source, p, PsiFormatBytes.GetFormat());
        //                                //Connection<Image>(stream.StreamName, source, p, PsiFormatImage.GetFormat());
        //                                break;
        //                        }
        //                    }
        //                }
        //                canStart = true;
        //            }
        //        }
        //    };
        //    server.Error += (s, e) => { Console.WriteLine(e.Message); Console.WriteLine(e.HResult); };
        //    server.Start();
        //    while (!canStart) Thread.Sleep(500);
        //    Thread.Sleep(500);
        //}

        //static bool alternate = true;
        //static void testKinectRemote(Pipeline p)
        //{
        //    RendezvousServer server = new RendezvousServer(11411);
        //    KinectAzureRemoteConnector receiver = new KinectAzureRemoteConnector(p);
        //    server.Rendezvous.ProcessAdded += receiver.GenerateProcess();

        //    var emitter = p.CreateEmitter<KinectAzureRemoteStreamsConfiguration?>(p, "config");
        //    var timer = Timers.Timer(p, TimeSpan.FromSeconds(15));
        //    RemoteExporter exporter = new RemoteExporter(p, 11511, TransportKind.Tcp);
        //    KinectAzureRemoteStreamsConfiguration cfg = new KinectAzureRemoteStreamsConfiguration();
        //    cfg.StreamVideo = cfg.StreamSkeleton = false;
        //    timer.Out.Do(t =>
        //    {
        //        if (alternate)
        //            emitter.Post(cfg, p.GetCurrentTime());
        //        else
        //            emitter.Post(null, p.GetCurrentTime());
        //        alternate = !alternate;
        //        Console.WriteLine("Post");
        //    });
        //    exporter.Exporter.Write(emitter, "Configuration");
        //    if (!server.Rendezvous.TryAddProcess(new Rendezvous.Process("KinectStreaming_Configuration", new List<Rendezvous.Endpoint> { exporter.ToRendezvousEndpoint("10.44.192.131") })))
        //        Console.WriteLine("failed");
        //    server.Start();
        //    Console.WriteLine("start");
        //    //var store = PsiStore.Create(p, "testKinectRemote", "D:\\Stores");

        //}

        //private static void OnNewProcess(object sender, (string, Dictionary<string, Dictionary<string, ConnectorInfo>>) e)
        //{
        //    RendezVousPipeline? parent = sender as RendezVousPipeline;
        //    if (parent == null)
        //        return;
        //    var sessionp = parent.GetSession("Unity.");

        //    var newStreams = e.Item2[e.Item1];
        //    foreach (var stream in newStreams)
        //    {
        //        if (stream.Key == "Image")
        //        {
        //            var subP = parent.CreateSubpipeline($"{e.Item1}-ImageProcessing");
        //            var producer = stream.Value.CreateBridge<byte[]>(subP);
        //            BytesStreamToImage processor = new BytesStreamToImage(subP);
        //            Microsoft.Psi.Operators.PipeTo(producer.Out, processor.In);
        //            Microsoft.Psi.Data.Session? session = parent.GetSession(stream.Value.SessionName);
        //            if (session != null)
        //                parent.CreateStore(subP, session, "Image", "WebRTC", processor);
        //            subP.RunAsync();
        //            parent.Dataset.Save();
        //            return;
        //        }
        //    }
        //}

        //static void CommandDel(string source, Message<(RendezVousPipeline.Command, string)> message)
        //{
        //    Console.WriteLine($"Command by {source}: {message.Data.Item1} with args {message.Data.Item2} @{message.OriginatingTime}");
        //}

        //static void Main(string[] args)
        //{
        //    RendezVousPipelineConfiguration configuration = new RendezVousPipelineConfiguration();
        //    configuration.AutomaticPipelineRun = true;
        //    configuration.Debug = true;
        //    //configuration.Diagnostics = DiagnosticsMode.Store;
        //    configuration.DatasetPath = "F:\\Stores\\RendezVousPipeline\\";
        //    configuration.DatasetName = "RendezVousPipeline.pds";
        //    configuration.RendezVousHost = "127.0.0.1";

        //    configuration.AddTopicFormatAndTransformer("Cube", typeof(System.Numerics.Matrix4x4),new PsiFormatMatrix4x4(), typeof(MatrixToCoordinateSystem));
           
        //    RendezVousPipeline pipeline = new RendezVousPipeline(configuration);

        //    pipeline.Start();

        //    // Waiting for an out key
        //    Console.WriteLine("Press any key to stop the application.");
        //    Console.ReadLine();
        //    pipeline.Stop();
        //}


        static void CommandDel(string source, Message<(RendezVousPipeline.Command, string)> message)
        {
            Console.WriteLine($"Command by {source}: {message.Data.Item1} with args {message.Data.Item2} @{message.OriginatingTime}");
        }

        public class Reporter : System.IProgress<double>
        {
            public void Report(double value)
            {
               Console.WriteLine($"Progress @ {value}");
            }
        }

        //static void PlumeSample()
        //{
        //    DatasetPipelineConfiguration config = new DatasetPipelineConfiguration();
        //    config.AutomaticPipelineRun = false;
        //    config.Diagnostics = DatasetPipeline.DiagnosticsMode.Off;
        //    config.DatasetPath = @"E:\SAAC\Stores\";
        //    config.DatasetName = "SAAC.pds";

        //    PlumeDatasetPipeline pipeline = new PlumeDatasetPipeline(config, "exampleParser");
        //    pipeline.LoadPlumeFile(@"E:\SAAC\record.plm", new Dictionary<string, Type>() { { "Main Camera", typeof(MathNet.Spatial.Euclidean.CoordinateSystem) } });
        //    pipeline.Dispose();
        //}
        static void Main(string[] args)
        {
            Pipeline pw = Pipeline.Create();
            Microsoft.Psi.Interop.Transport.WebSocketsManager websocketManager = new Microsoft.Psi.Interop.Transport.WebSocketsManager(true, true, "https://localhost:8080/ws/");
            websocketManager.OnNewWebSocketConnectedHandler += (s, e) => 
            {
                Console.WriteLine($"New WebSocket connected: {e.Item1}:{e.Item2}");
                Emitter<string> emitter = pw.CreateEmitter<string>(pw, "emitter");
                WebSocketWriter<string>? writer = websocketManager.CreateWebsocketWriter<string>(pw, SAAC.PsiFormats.PsiFormatString.GetFormat(), e.Item1, e.Item2, "testw");
                emitter.PipeTo(writer);
                pw.RunAsync();
                emitter.Post("hello", pw.GetCurrentTime());
            };
            websocketManager.Start((e) => { });
            Console.ReadLine();
            return;
            Random rnd = new Random();
            // create stream info and outlet
            StreamInfo info = new StreamInfo("TestCSharp", "EEG", 8, 200, channel_format_t.cf_float32, "sddsfsdf");
            StreamOutlet outlet = new StreamOutlet(info);
            float[] data = new float[8];
            Thread thread = new Thread(() =>
            {
                Console.WriteLine("LSL Outlet started...");
                while (true)
                {
                    Thread.Sleep(1000);
                    // generate random data and send it
                    for (int k = 0; k < data.Length; k++)
                        data[k] = rnd.Next(-100, 100);
                    outlet.push_sample(data);
                    Console.WriteLine($"PTime : {DateTime.UtcNow}");
                }
            });

            Pipeline p = Pipeline.Create();
            LabStreamLayerManager manager = new LabStreamLayerManager(p, (log) => Console.WriteLine($"{log}\n"),500,100);
            manager.Start();
            Console.ReadLine();
            LabStreamLayerComponent<float>? TEST = manager.LabStreamComponents.First().Value as LabStreamLayerComponent<float>;
            
            TEST?.Out.Do((m, e) => { Console.WriteLine($"Data @ {e.OriginatingTime}"); });

            p.RunAsync();
            thread.Start();
            Console.ReadLine();
            return;
            //PlumeSample();
            //return;

            //ReplayPipelineConfiguration replayConfig = new ReplayPipelineConfiguration();
            //replayConfig.AutomaticPipelineRun = false;
            //replayConfig.DatasetBackup = true;
            //replayConfig.DatasetPath = @"D:\Stores\SAAC\";
            //replayConfig.DatasetName = "SAAC.pds";
            //replayConfig.ProgressReport = new Reporter();

            //ReplayPipeline replayPipeline = new ReplayPipeline(replayConfig);
            //replayPipeline.LoadDatasetAndConnectors();

            RendezVousPipelineConfiguration configuration = new RendezVousPipelineConfiguration();
            configuration.AutomaticPipelineRun = true;
            configuration.Debug = true;
            configuration.DatasetPath = @"D:\Stores\RendezVousPipeline\"; // change if needed !
            configuration.DatasetName = "RendezVousPipeline.pds";
            configuration.RendezVousHost = "localhost";
            configuration.Diagnostics = DatasetPipeline.DiagnosticsMode.Off;
            configuration.StoreMode = DatasetPipeline.StoreMode.Process;

            // Topics to receive from Unity
            // do a all-in management of streams
            configuration.AddTopicFormatAndTransformer("Champignon", typeof(System.Numerics.Vector3), new PsiFormatVector3());
            configuration.AddTopicFormatAndTransformer("Boletus", typeof(System.Numerics.Vector3), new PsiFormatVector3());
            configuration.AddTopicFormatAndTransformer("Amanita", typeof(System.Numerics.Vector3), new PsiFormatVector3());
            

            // Instantiate the class that manage the RendezVous system and the pipeline execution?
            RendezVousPipeline rdvPipeline = new RendezVousPipeline(/*replayPipeline.Pipeline,*/ configuration, "Server");

            // Register an action when receive the incoming connection from Unity
            //rdvPipeline.AddNewProcessEvent(OnNewProcess);

            // Start the rendezVous and the pipeline
            rdvPipeline.Start();

            Console.WriteLine("Press any key to send RUN command to Unity.");
            Console.ReadLine();
            rdvPipeline.SendCommand(RendezVousPipeline.Command.Run, "UnityB", "");
            //replayPipeline.RunPipelineAndSubpipelines();

            Console.WriteLine("Press any key to send STOP command to Unity.");
            Console.ReadLine();
            rdvPipeline.SendCommand(RendezVousPipeline.Command.Stop, "UnityB", "");

            // Waiting for an out key to Stop
            Console.WriteLine("Press any key to stop the application.");
            Console.ReadLine();
            rdvPipeline.Stop();
            //replayPipeline.Stop();
        }
    }
}
