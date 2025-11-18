using Google.Protobuf;
using Google.Protobuf.Reflection;
using MathNet.Spatial.Euclidean;
using PLUME.Sample;
using PLUME.Sample.Common;
using PLUME.Sample.Unity;
using PLUME.Sample.Unity.Settings;
using PLUME.Sample.Unity.UI;
using PLUME.Sample.Unity.XRITK;
using System.IO;
using System.Reflection;
using Quaternion = PLUME.Sample.Common.Quaternion;

namespace SAAC.PLUME
{
    public class PlumeFileParser
    {
        // --- Unity Settings ---
        //public delegate void ProcessRenderSettingsUpdateDelegate(RenderSettingsUpdate msg, ulong? timestamp);

        // --- XRITK ---
        //public delegate void ProcessXRBaseInteractableCreateDelegate(XRBaseInteractableCreate msg, ulong? timestamp);
        //public delegate void ProcessXRBaseInteractableUpdateDelegate(XRBaseInteractableUpdate msg, ulong? timestamp);
        //public delegate void ProcessXRBaseInteractorCreateDelegate(XRBaseInteractorCreate msg, ulong? timestamp);
        //public delegate void ProcessXRBaseInteractorUpdateDelegate(XRBaseInteractorUpdate msg, ulong? timestamp);
        //public delegate void ProcessXRITKInteractionDelegate(XRITKInteraction msg, ulong? timestamp);
        //public delegate void ProcessXRBaseInteractableDestroyDelegate(XRBaseInteractableDestroy msg, ulong? timestamp);

        // --- Unity UI ---
        //public delegate void ProcessRectTransformCreateDelegate(RectTransformCreate msg, ulong? timestamp);
        //public delegate void ProcessRectTransformUpdateDelegate(RectTransformUpdate msg, ulong? timestamp);
        //public delegate void ProcessTextCreateDelegate(TextCreate msg, ulong? timestamp);
        //public delegate void ProcessTextUpdateDelegate(TextUpdate msg, ulong? timestamp);
        //public delegate void ProcessGraphicUpdateDelegate(GraphicUpdate msg, ulong? timestamp);
        //public delegate void ProcessImageCreateDelegate(ImageCreate msg, ulong? timestamp);
        //public delegate void ProcessImageUpdateDelegate(ImageUpdate msg, ulong? timestamp);
        //public delegate void ProcessCanvasCreateDelegate(CanvasCreate msg, ulong? timestamp);
        //public delegate void ProcessCanvasUpdateDelegate(CanvasUpdate msg, ulong? timestamp);
        //public delegate void ProcessCanvasRendererCreateDelegate(CanvasRendererCreate msg, ulong? timestamp);
        //public delegate void ProcessCanvasRendererUpdateDelegate(CanvasRendererUpdate msg, ulong? timestamp);
        //public delegate void ProcessCanvasScalerCreateDelegate(CanvasScalerCreate msg, ulong? timestamp);
        //public delegate void ProcessCanvasScalerUpdateDelegate(CanvasScalerUpdate msg, ulong? timestamp);

        // --- Unity Core ---
        //public delegate void ProcessGameObjectCreateDelegate(GameObjectCreate gameObjectCreate, ulong? timestamp);
        //public delegate void ProcessGameObjectUpdateDelegate(GameObjectUpdate gameObjectUpdate, ulong? timestamp);
        //public delegate void ProcessGameObjectDestroyDelegate(GameObjectDestroy gameObjectDestroy, ulong? timestamp);
        //public delegate void ProcessTransformCreateDelegate(TransformCreate msg, ulong? timestamp);
        public delegate void ProcessTransformUpdateDelegate(string name, CoordinateSystem transform, ulong? timestamp);
        //public delegate void ProcessTransformDestroyDelegate(TransformDestroy msg, ulong? timestamp);
        //public delegate void ProcessTerrainCreateDelegate(TerrainCreate msg, ulong? timestamp);
        //public delegate void ProcessTerrainUpdateDelegate(TerrainUpdate msg, ulong? timestamp);
        //public delegate void ProcessLoadSceneDelegate(LoadScene msg, ulong? timestamp);
        //public delegate void ProcessChangeActiveSceneDelegate(ChangeActiveScene msg, ulong? timestamp);
        //public delegate void ProcessSkinnedMeshRendererCreateDelegate(SkinnedMeshRendererCreate msg, ulong? timestamp);
        //public delegate void ProcessSkinnedMeshRendererUpdateDelegate(SkinnedMeshRendererUpdate msg, ulong? timestamp);
        //public delegate void ProcessRendererUpdateDelegate(RendererUpdate msg, ulong? timestamp);
        //public delegate void ProcessMeshRendererCreateDelegate(MeshRendererCreate msg, ulong? timestamp);
        //public delegate void ProcessMeshRendererDestroyDelegate(MeshRendererDestroy msg, ulong? timestamp);
        //public delegate void ProcessLineRendererCreateDelegate(LineRendererCreate msg, ulong? timestamp);
        //public delegate void ProcessLineRendererUpdateDelegate(LineRendererUpdate msg, ulong? timestamp);
        //public delegate void ProcessReflectionProbeCreateDelegate(ReflectionProbeCreate msg, ulong? timestamp);
        //public delegate void ProcessReflectionProbeUpdateDelegate(ReflectionProbeUpdate msg, ulong? timestamp);
        //public delegate void ProcessMeshFilterCreateDelegate(MeshFilterCreate msg, ulong? timestamp);
        //public delegate void ProcessMeshFilterUpdateDelegate(MeshFilterUpdate msg, ulong? timestamp);
        //public delegate void ProcessMeshFilterDestroyDelegate(MeshFilterDestroy msg, ulong? timestamp);
        //public delegate void ProcessLightmapsUpdateDelegate(LightmapsUpdate msg, ulong? timestamp);
        //public delegate void ProcessLightCreateDelegate(LightCreate msg, ulong? timestamp);
        //public delegate void ProcessLightUpdateDelegate(LightUpdate msg, ulong? timestamp);
        //public delegate void ProcessCameraCreateDelegate(CameraCreate msg, ulong? timestamp);
        //public delegate void ProcessCameraUpdateDelegate(CameraUpdate msg, ulong? timestamp);

        // --- Unity Settings ---
        //public ProcessRenderSettingsUpdateDelegate OnRenderSettingsUpdate;

        // --- XRITK ---
        //public ProcessXRBaseInteractableCreateDelegate OnXRBaseInteractableCreate;
        //public ProcessXRBaseInteractableUpdateDelegate OnXRBaseInteractableUpdate;
        //public ProcessXRBaseInteractorCreateDelegate OnXRBaseInteractorCreate;
        //public ProcessXRBaseInteractorUpdateDelegate OnXRBaseInteractorUpdate;
        //public ProcessXRITKInteractionDelegate OnXRITKInteraction;
        //public ProcessXRBaseInteractableDestroyDelegate OnXRBaseInteractableDestroy;

        // --- Unity UI ---
        //public ProcessRectTransformCreateDelegate OnRectTransformCreate;
        //public ProcessRectTransformUpdateDelegate OnRectTransformUpdate;
        //public ProcessTextCreateDelegate OnTextCreate;
        //public ProcessTextUpdateDelegate OnTextUpdate;
        //public ProcessGraphicUpdateDelegate OnGraphicUpdate;
        //public ProcessImageCreateDelegate OnImageCreate;
        //public ProcessImageUpdateDelegate OnImageUpdate;
        //public ProcessCanvasCreateDelegate OnCanvasCreate;
        //public ProcessCanvasUpdateDelegate OnCanvasUpdate;
        //public ProcessCanvasRendererCreateDelegate OnCanvasRendererCreate;
        //public ProcessCanvasRendererUpdateDelegate OnCanvasRendererUpdate;
        //public ProcessCanvasScalerCreateDelegate OnCanvasScalerCreate;
        //public ProcessCanvasScalerUpdateDelegate OnCanvasScalerUpdate;

        // --- Unity Core ---
        //public ProcessGameObjectCreateDelegate OnGameObjectCreate;
        //public ProcessGameObjectUpdateDelegate OnGameObjectUpdate;
        //public ProcessGameObjectDestroyDelegate OnGameObjectDestroy;
        //public ProcessTransformCreateDelegate OnTransformCreate;
        public ProcessTransformUpdateDelegate OnTransformUpdate;
        //public ProcessTransformDestroyDelegate OnTransformDestroy;
        //public ProcessTerrainCreateDelegate OnTerrainCreate;
        //public ProcessTerrainUpdateDelegate OnTerrainUpdate;
        //public ProcessLoadSceneDelegate OnLoadScene;
        //public ProcessChangeActiveSceneDelegate OnChangeActiveScene;
        //public ProcessSkinnedMeshRendererCreateDelegate OnSkinnedMeshRendererCreate;
        //public ProcessSkinnedMeshRendererUpdateDelegate OnSkinnedMeshRendererUpdate;
        //public ProcessRendererUpdateDelegate OnRendererUpdate;
        //public ProcessMeshRendererCreateDelegate OnMeshRendererCreate;
        //public ProcessMeshRendererDestroyDelegate OnMeshRendererDestroy;
        //public ProcessLineRendererCreateDelegate OnLineRendererCreate;
        //public ProcessLineRendererUpdateDelegate OnLineRendererUpdate;
        //public ProcessReflectionProbeCreateDelegate OnReflectionProbeCreate;
        //public ProcessReflectionProbeUpdateDelegate OnReflectionProbeUpdate;
        //public ProcessMeshFilterCreateDelegate OnMeshFilterCreate;
        //public ProcessMeshFilterUpdateDelegate OnMeshFilterUpdate;
        //public ProcessMeshFilterDestroyDelegate OnMeshFilterDestroy;
        //public ProcessLightmapsUpdateDelegate OnLightmapsUpdate;
        //public ProcessLightCreateDelegate OnLightCreate;
        //public ProcessLightUpdateDelegate OnLightUpdate;
        //public ProcessCameraCreateDelegate OnCameraCreate;
        //public ProcessCameraUpdateDelegate OnCameraUpdate;

        public DateTime StartTime { get; private set; } = DateTime.MinValue;
        public string Name { get; private set; } = string.Empty;
        public Dictionary<string, string> GameObjectNames { get; private set; }

        // https://github.com/liris-xr/PLUME-Viewer/blob/master/Runtime/Scripts/RecordLoader.cs#L145
        private const uint LZ4MagicNumber = 0x184D2204;

        private Stream input;

        private List<string> registeryAssemblies;
        protected TypeRegistry? registry = null;

        protected LogStatus log;

        public PlumeFileParser(List<string>? assembliesToLoad = null, LogStatus? log = null)
        {
            GameObjectNames = new Dictionary<string, string>();
            registeryAssemblies = new List<string>
            {
                typeof(PackedSample).Assembly.GetName().Name
            };
            if (assembliesToLoad != null)
                registeryAssemblies.AddRange(assembliesToLoad);
            LoadTypeRegistry();
            this.log = log ?? ((log) => { Console.WriteLine(log); });
        }

        public void ParseFile(string filePath)
        {
            input = OpenFile(filePath);

            var packedMetadata = PackedSample.Parser.ParseDelimitedFrom(input);
            var metadata = packedMetadata.Payload.Unpack<RecordMetadata>();
            if (metadata == null)
                throw new Exception("Failed to load metadata from record file");
            StartTime = metadata.StartTime.ToDateTime();
            Name = metadata.Name;
            log($"Parsing {filePath}, record name : {Name}, start time : {StartTime}");

            // Unused metadata
            var packedGraphicsSettings = PackedSample.Parser.ParseDelimitedFrom(input);
            var graphicsSettings = packedGraphicsSettings.Payload.Unpack<GraphicsSettings>();

            if (graphicsSettings == null)
                throw new Exception("Failed to load graphics settings from record file");
        }

        //https://github.com/liris-xr/PLUME-Viewer/blob/master/Runtime/Scripts/TypeRegistryProviderAssembliesLookup.cs
        protected void LoadTypeRegistry()
        {
            var messageDescriptors = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => registeryAssemblies.Contains(a.GetName().Name))
                .SelectMany(assembly =>
                    assembly.GetTypes()
                        .Where(t => typeof(IMessage).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                        .Select(t =>
                        {
                            var descriptorProperty =
                                t.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static);
                            var value = descriptorProperty!.GetValue(null);
                            return (MessageDescriptor)value;
                        }));

            registry = TypeRegistry.FromMessages(messageDescriptors);
        }

        public void UnpackAll()
        {
            while (Unpack())
            {
                // Continue unpacking until no more samples are available
            }
            input.Close();
        }

        protected bool Unpack()
        {
            try
            {
                PackedSample packedSample = PackedSample.Parser.ParseDelimitedFrom(input);
                ulong? timestamp = packedSample.HasTimestamp ? packedSample.Timestamp : null;
                if (timestamp == null)
                {
                    log($"No timestamp found in the packed sample.");
                    return false; // No timestamp to process
                }

                var payload = packedSample.Payload.Unpack(registry);
                if (payload == null)
                {
                    log($"No payload found in the packed sample @{timestamp}.");
                    return true; // No payload to process
                }

                switch (payload)
                {
                    case Frame frame:
                        // Unpack frame
                        ProcessUnpackFrame(frame.Data.Select(frameData => frameData.Unpack(registry)).ToList(), (ulong)timestamp);
                        break;
                    //case Marker marker:
                    //    record.AddMarkerSample(marker);
                    //    break;
                    //case InputAction inputAction:
                    //    inputAction.Name
                    //    break;
                    //case RawSample<StreamSample> streamSample:
                    //    record.AddStreamSample(streamSample);
                    //    break;
                    //case RawSample<StreamOpen> streamOpen:
                    //    record.AddStreamOpenSample(streamOpen);
                    //    break;
                    //case RawSample<StreamClose> streamClose:
                    //    record.AddStreamCloseSample(streamClose);
                    //    break;
                    //case null:
                    //    break;
                    //default:
                    //    record.AddOtherSample(unpackedSample);
                    //    break;
                }
            }
            catch (Exception ex)
            {
                log($"Error unpacking sample: {ex.Message}");
                return false; // End of stream or invalid data
            }
            return true;
        }

        protected void ProcessUnpack(IMessage? message)
        { }

        protected void ProcessUnpackFrame(List<IMessage> message, ulong timestamp)
        {
            foreach (var msg in message)
            {
                switch (msg)
                { 
                    case GameObjectCreate gameObjectCreate:
                        ProcessGameObjectCreate(gameObjectCreate, timestamp);
                        break;
                    case GameObjectUpdate gameObjectUpdate:
                        ProcessGameObjectUpdate(gameObjectUpdate, timestamp);
                        break;
                    case GameObjectDestroy gameObjectDestroy:
                        ProcessGameObjectDestroy(gameObjectDestroy, timestamp);
                        break;
                    case RenderSettingsUpdate renderSettingsUpdate:
                        ProcessRenderSettingsUpdate(renderSettingsUpdate, timestamp);
                        break;
                    case XRBaseInteractableCreate xrBaseInteractableCreate:
                        ProcessXRBaseInteractableCreate(xrBaseInteractableCreate, timestamp);
                        break;
                    case XRBaseInteractableUpdate xrBaseInteractableUpdate:
                        ProcessXRBaseInteractableUpdate(xrBaseInteractableUpdate, timestamp);
                        break;
                    case XRBaseInteractorCreate xrBaseInteractorCreate:
                        ProcessXRBaseInteractorCreate(xrBaseInteractorCreate, timestamp);
                        break;
                    case XRBaseInteractorUpdate xrBaseInteractorUpdate:
                        ProcessXRBaseInteractorUpdate(xrBaseInteractorUpdate, timestamp);
                        break;
                    case XRITKInteraction xritkInteraction:
                        ProcessXRITKInteraction(xritkInteraction, timestamp);
                        break;
                    case XRBaseInteractableDestroy xrBaseInteractableDestroy:
                        ProcessXRBaseInteractableDestroy(xrBaseInteractableDestroy, timestamp);
                        break;
                    case RectTransformCreate rectTransformCreate:
                        ProcessRectTransformCreate(rectTransformCreate, timestamp);
                        break;
                    case RectTransformUpdate rectTransformUpdate:
                        ProcessRectTransformUpdate(rectTransformUpdate, timestamp);
                        break;
                    case TextCreate textCreate:
                        ProcessTextCreate(textCreate, timestamp);
                        break;
                    case TextUpdate textUpdate:
                        ProcessTextUpdate(textUpdate, timestamp);
                        break;
                    case GraphicUpdate graphicUpdate:
                        ProcessGraphicUpdate(graphicUpdate, timestamp);
                        break;
                    case ImageCreate imageCreate:
                        ProcessImageCreate(imageCreate, timestamp);
                        break;
                    case ImageUpdate imageUpdate:
                        ProcessImageUpdate(imageUpdate, timestamp);
                        break;
                    case CanvasCreate canvasCreate:
                        ProcessCanvasCreate(canvasCreate, timestamp);
                        break;
                    case CanvasUpdate canvasUpdate:
                        ProcessCanvasUpdate(canvasUpdate, timestamp);
                        break;
                    case CanvasRendererCreate canvasRendererCreate:
                        ProcessCanvasRendererCreate(canvasRendererCreate, timestamp);
                        break;
                    case CanvasRendererUpdate canvasRendererUpdate:
                        ProcessCanvasRendererUpdate(canvasRendererUpdate, timestamp);
                        break;
                    case CanvasScalerCreate canvasScalerCreate:
                        ProcessCanvasScalerCreate(canvasScalerCreate, timestamp);
                        break;
                    case CanvasScalerUpdate canvasScalerUpdate:
                        ProcessCanvasScalerUpdate(canvasScalerUpdate, timestamp);
                        break;
                    case TransformCreate transformCreate:
                        ProcessTransformCreate(transformCreate, timestamp);
                        break;
                    case TransformUpdate transformUpdate:
                        ProcessTransformUpdate(transformUpdate, timestamp);
                        break;
                    case TransformDestroy transformDestroy:
                        ProcessTransformDestroy(transformDestroy, timestamp);
                        break;
                    case TerrainCreate terrainCreate:
                        ProcessTerrainCreate(terrainCreate, timestamp);
                        break;
                    case TerrainUpdate terrainUpdate:
                        ProcessTerrainUpdate(terrainUpdate, timestamp);
                        break;
                    case LoadScene loadScene:
                        ProcessLoadScene(loadScene, timestamp);
                        break;
                    case ChangeActiveScene changeActiveScene:
                        ProcessChangeActiveScene(changeActiveScene, timestamp);
                        break;
                    case SkinnedMeshRendererCreate skinnedMeshRendererCreate:
                        ProcessSkinnedMeshRendererCreate(skinnedMeshRendererCreate, timestamp);
                        break;
                    case SkinnedMeshRendererUpdate skinnedMeshRendererUpdate:
                        ProcessSkinnedMeshRendererUpdate(skinnedMeshRendererUpdate, timestamp);
                        break;
                    case RendererUpdate rendererUpdate:
                        ProcessRendererUpdate(rendererUpdate, timestamp);
                        break;
                    case MeshRendererCreate meshRendererCreate:
                        ProcessMeshRendererCreate(meshRendererCreate, timestamp);
                        break;
                    case MeshRendererDestroy meshRendererDestroy:
                        ProcessMeshRendererDestroy(meshRendererDestroy, timestamp);
                        break;
                    case LineRendererCreate lineRendererCreate:
                        ProcessLineRendererCreate(lineRendererCreate, timestamp);
                        break;
                    case LineRendererUpdate lineRendererUpdate:
                        ProcessLineRendererUpdate(lineRendererUpdate, timestamp);
                        break;
                    case ReflectionProbeCreate reflectionProbeCreate:
                        ProcessReflectionProbeCreate(reflectionProbeCreate, timestamp);
                        break;
                    case ReflectionProbeUpdate reflectionProbeUpdate:
                        ProcessReflectionProbeUpdate(reflectionProbeUpdate, timestamp);
                        break;
                    case MeshFilterCreate meshFilterCreate:
                        ProcessMeshFilterCreate(meshFilterCreate, timestamp);
                        break;
                    case MeshFilterUpdate meshFilterUpdate:
                        ProcessMeshFilterUpdate(meshFilterUpdate, timestamp);
                        break;
                    case MeshFilterDestroy meshFilterDestroy:
                        ProcessMeshFilterDestroy(meshFilterDestroy, timestamp);
                        break;
                    case LightmapsUpdate lightmapsUpdate:
                        ProcessLightmapsUpdate(lightmapsUpdate, timestamp);
                        break;
                    case LightCreate lightCreate:
                        ProcessLightCreate(lightCreate, timestamp);
                        break;
                    case LightUpdate lightUpdate:
                        ProcessLightUpdate(lightUpdate, timestamp);
                        break;
                    case CameraCreate cameraCreate:
                        ProcessCameraCreate(cameraCreate, timestamp);
                        break;
                    case CameraUpdate cameraUpdate:
                        ProcessCameraUpdate(cameraUpdate, timestamp);
                        break;
                    default:
                        log($"Unknown message type: {msg?.Descriptor.FullName} at {timestamp}");
                        break;
                }
            }
        }

        // --- Unity Settings ---
        protected virtual void ProcessRenderSettingsUpdate(RenderSettingsUpdate msg, ulong? timestamp)
        {
            log($"RenderSettingsUpdate, Timestamp: {timestamp}");
        }

        // --- XRITK ---
        protected virtual void ProcessXRBaseInteractableCreate(XRBaseInteractableCreate msg, ulong? timestamp)
        {
            log($"XRBaseInteractableCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessXRBaseInteractableUpdate(XRBaseInteractableUpdate msg, ulong? timestamp)
        {
            log($"XRBaseInteractableUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessXRBaseInteractorCreate(XRBaseInteractorCreate msg, ulong? timestamp)
        {
            log($"XRBaseInteractorCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessXRBaseInteractorUpdate(XRBaseInteractorUpdate msg, ulong? timestamp)
        {
            log($"XRBaseInteractorUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessXRITKInteraction(XRITKInteraction msg, ulong? timestamp)
        {
            log($"XRITKInteraction, Timestamp: {timestamp}");
        }

        protected virtual void ProcessXRBaseInteractableDestroy(XRBaseInteractableDestroy msg, ulong? timestamp)
        {
            log($"XRBaseInteractableDestroy, Timestamp: {timestamp}");
        }

        // --- Unity UI ---
        protected virtual void ProcessRectTransformCreate(RectTransformCreate msg, ulong? timestamp)
        {
            log($"RectTransformCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessRectTransformUpdate(RectTransformUpdate msg, ulong? timestamp)
        {
            log($"RectTransformUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessTextCreate(TextCreate msg, ulong? timestamp)
        {
            log($"TextCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessTextUpdate(TextUpdate msg, ulong? timestamp)
        {
            log($"TextUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessGraphicUpdate(GraphicUpdate msg, ulong? timestamp)
        {
            log($"GraphicUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessImageCreate(ImageCreate msg, ulong? timestamp)
        {
            log($"ImageCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessImageUpdate(ImageUpdate msg, ulong? timestamp)
        {
            log($"ImageUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessCanvasCreate(CanvasCreate msg, ulong? timestamp)
        {
            log($"CanvasCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessCanvasUpdate(CanvasUpdate msg, ulong? timestamp)
        {
            log($"CanvasUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessCanvasRendererCreate(CanvasRendererCreate msg, ulong? timestamp)
        {
            log($"CanvasRendererCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessCanvasRendererUpdate(CanvasRendererUpdate msg, ulong? timestamp)
        {
            log($"CanvasRendererUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessCanvasScalerCreate(CanvasScalerCreate msg, ulong? timestamp)
        {
            log($"CanvasScalerCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessCanvasScalerUpdate(CanvasScalerUpdate msg, ulong? timestamp)
        {
            log($"CanvasScalerUpdate, Timestamp: {timestamp}");
        }

        // --- Unity Core ---
        protected virtual void ProcessGameObjectCreate(GameObjectCreate gameObjectCreate, ulong? timestamp)
        {
            log($"GameObject create: {gameObjectCreate.Id}, Timestamp: {timestamp}");
        }

        protected virtual void ProcessGameObjectUpdate(GameObjectUpdate gameObjectUpdate, ulong? timestamp)
        {
            if (gameObjectUpdate.HasName && !GameObjectNames.ContainsKey(gameObjectUpdate.Id.TransformGuid))
                GameObjectNames.Add(gameObjectUpdate.Id.TransformGuid, gameObjectUpdate.Name);
        }

        protected virtual void ProcessGameObjectDestroy(GameObjectDestroy gameObjectDestroy, ulong? timestamp)
        {
            log($"GameObject Destroy: {gameObjectDestroy.Id}, Timestamp: {timestamp}");
        }

        protected virtual void ProcessTransformCreate(TransformCreate msg, ulong? timestamp)
        {
            log($"TransformCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessTransformUpdate(TransformUpdate msg, ulong? timestamp)
        {
            if (GameObjectNames.ContainsKey(msg.Component.Guid))
            {
                Vector3 pos = msg.LocalPosition ?? new Vector3();
                Quaternion rot = msg.LocalRotation ?? new Quaternion();
                MathNet.Spatial.Euclidean.Point3D position = new Point3D(pos.X, pos.Y, pos.Z);
                MathNet.Spatial.Euclidean.Quaternion rotation = new MathNet.Spatial.Euclidean.Quaternion(rot.W, rot.X, rot.Y, rot.Z);
                CoordinateSystem system = Helpers.PositionAndQuaternionTo.CoordinateSystem(position, rotation);
                OnTransformUpdate(GameObjectNames[msg.Component.Guid], system, timestamp);
            }
        }

        protected virtual void ProcessTransformDestroy(TransformDestroy msg, ulong? timestamp)
        {
            log($"TransformDestroy, Timestamp: {timestamp}");
        }

        protected virtual void ProcessTerrainCreate(TerrainCreate msg, ulong? timestamp)
        {
            log($"TerrainCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessTerrainUpdate(TerrainUpdate msg, ulong? timestamp)
        {
            log($"TerrainUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessLoadScene(LoadScene msg, ulong? timestamp)
        {
            log($"LoadScene, Timestamp: {timestamp}");
        }

        protected virtual void ProcessChangeActiveScene(ChangeActiveScene msg, ulong? timestamp)
        {
            log($"ChangeActiveScene, Timestamp: {timestamp}");
        }

        protected virtual void ProcessSkinnedMeshRendererCreate(SkinnedMeshRendererCreate msg, ulong? timestamp)
        {
            log($"SkinnedMeshRendererCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessSkinnedMeshRendererUpdate(SkinnedMeshRendererUpdate msg, ulong? timestamp)
        {
            log($"SkinnedMeshRendererUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessRendererUpdate(RendererUpdate msg, ulong? timestamp)
        {
            log($"RendererUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessMeshRendererCreate(MeshRendererCreate msg, ulong? timestamp)
        {
            log($"MeshRendererCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessMeshRendererDestroy(MeshRendererDestroy msg, ulong? timestamp)
        {
            log($"MeshRendererDestroy, Timestamp: {timestamp}");
        }

        protected virtual void ProcessLineRendererCreate(LineRendererCreate msg, ulong? timestamp)
        {
            log($"LineRendererCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessLineRendererUpdate(LineRendererUpdate msg, ulong? timestamp)
        {
            log($"LineRendererUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessReflectionProbeCreate(ReflectionProbeCreate msg, ulong? timestamp)
        {
            log($"ReflectionProbeCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessReflectionProbeUpdate(ReflectionProbeUpdate msg, ulong? timestamp)
        {
            log($"ReflectionProbeUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessMeshFilterCreate(MeshFilterCreate msg, ulong? timestamp)
        {
            log($"MeshFilterCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessMeshFilterUpdate(MeshFilterUpdate msg, ulong? timestamp)
        {
            log($"MeshFilterUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessMeshFilterDestroy(MeshFilterDestroy msg, ulong? timestamp)
        {
            log($"MeshFilterDestroy, Timestamp: {timestamp}");
        }

        protected virtual void ProcessLightmapsUpdate(LightmapsUpdate msg, ulong? timestamp)
        {
            log($"LightmapsUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessLightCreate(LightCreate msg, ulong? timestamp)
        {
            log($"LightCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessLightUpdate(LightUpdate msg, ulong? timestamp)
        {
            log($"LightUpdate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessCameraCreate(CameraCreate msg, ulong? timestamp)
        {
            log($"CameraCreate, Timestamp: {timestamp}");
        }

        protected virtual void ProcessCameraUpdate(CameraUpdate msg, ulong? timestamp)
        {
            log($"CameraUpdate, Timestamp: {timestamp}");
        }

        private Stream OpenFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The file {filePath} does not exist.");

            var input = File.OpenRead(filePath);

            if (IsLZ4Compressed(input))
            {
                log($"Decoding {filePath}...");
                return K4os.Compression.LZ4.Streams.LZ4Stream.Decode(input);

            }
            return input;
        }

        // https://github.com/liris-xr/PLUME-Viewer/blob/master/Runtime/Scripts/RecordLoader.cs#L145
        private bool IsLZ4Compressed(Stream fileStream)
        {
            // Read magic number
            var magicNumber = new byte[4];
            _ = fileStream.Read(magicNumber, 0, 4);
            fileStream.Seek(0, SeekOrigin.Begin);
            var compressed = BitConverter.ToUInt32(magicNumber, 0) == LZ4MagicNumber;
            return compressed;
        }
    }
}
