// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

using System.IO;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using MathNet.Spatial.Euclidean;
using PLUME.Sample;
using PLUME.Sample.Common;
using PLUME.Sample.Unity;
using PLUME.Sample.Unity.Settings;
using PLUME.Sample.Unity.UI;
using PLUME.Sample.Unity.XRITK;
using Quaternion = PLUME.Sample.Common.Quaternion;

namespace SAAC.PLUME
{
    /// <summary>
    /// Parses PLUME (Platform for Lightweight Unity Multimodal Experiences) files and processes Unity scene data.
    /// </summary>
    public class PlumeFileParser
    {
        // --- Unity Settings ---

        /// <summary>
        /// Event handler for render settings update messages.
        /// </summary>
        public ProcessRenderSettingsUpdateDelegate OnRenderSettingsUpdate;

        // --- XRITK ---

        /// <summary>
        /// Event handler for XR base interactable create messages.
        /// </summary>
        public ProcessXRBaseInteractableCreateDelegate OnXRBaseInteractableCreate;

        /// <summary>
        /// Event handler for XR base interactable update messages.
        /// </summary>
        public ProcessXRBaseInteractableUpdateDelegate OnXRBaseInteractableUpdate;

        /// <summary>
        /// Event handler for XR base interactor create messages.
        /// </summary>
        public ProcessXRBaseInteractorCreateDelegate OnXRBaseInteractorCreate;

        /// <summary>
        /// Event handler for XR base interactor update messages.
        /// </summary>
        public ProcessXRBaseInteractorUpdateDelegate OnXRBaseInteractorUpdate;

        /// <summary>
        /// Event handler for XR ITK interaction messages.
        /// </summary>
        public ProcessXRITKInteractionDelegate OnXRITKInteraction;

        /// <summary>
        /// Event handler for XR base interactable destroy messages.
        /// </summary>
        public ProcessXRBaseInteractableDestroyDelegate OnXRBaseInteractableDestroy;

        // --- Unity UI ---

        /// <summary>
        /// Event handler for rect transform create messages.
        /// </summary>
        public ProcessRectTransformCreateDelegate OnRectTransformCreate;

        /// <summary>
        /// Event handler for rect transform update messages.
        /// </summary>
        public ProcessRectTransformUpdateDelegate OnRectTransformUpdate;

        /// <summary>
        /// Event handler for text create messages.
        /// </summary>
        public ProcessTextCreateDelegate OnTextCreate;

        /// <summary>
        /// Event handler for text update messages.
        /// </summary>
        public ProcessTextUpdateDelegate OnTextUpdate;

        /// <summary>
        /// Event handler for graphic update messages.
        /// </summary>
        public ProcessGraphicUpdateDelegate OnGraphicUpdate;

        /// <summary>
        /// Event handler for image create messages.
        /// </summary>
        public ProcessImageCreateDelegate OnImageCreate;

        /// <summary>
        /// Event handler for image update messages.
        /// </summary>
        public ProcessImageUpdateDelegate OnImageUpdate;

        /// <summary>
        /// Event handler for canvas create messages.
        /// </summary>
        public ProcessCanvasCreateDelegate OnCanvasCreate;

        /// <summary>
        /// Event handler for canvas update messages.
        /// </summary>
        public ProcessCanvasUpdateDelegate OnCanvasUpdate;

        /// <summary>
        /// Event handler for canvas renderer create messages.
        /// </summary>
        public ProcessCanvasRendererCreateDelegate OnCanvasRendererCreate;

        /// <summary>
        /// Event handler for canvas renderer update messages.
        /// </summary>
        public ProcessCanvasRendererUpdateDelegate OnCanvasRendererUpdate;

        /// <summary>
        /// Event handler for canvas scaler create messages.
        /// </summary>
        public ProcessCanvasScalerCreateDelegate OnCanvasScalerCreate;

        /// <summary>
        /// Event handler for canvas scaler update messages.
        /// </summary>
        public ProcessCanvasScalerUpdateDelegate OnCanvasScalerUpdate;

        // --- Unity Core ---

        /// <summary>
        /// Event handler for GameObject create messages.
        /// </summary>
        public ProcessGameObjectCreateDelegate OnGameObjectCreate;

        /// <summary>
        /// Event handler for GameObject update messages.
        /// </summary>
        public ProcessGameObjectUpdateDelegate OnGameObjectUpdate;

        /// <summary>
        /// Event handler for GameObject destroy messages.
        /// </summary>
        public ProcessGameObjectDestroyDelegate OnGameObjectDestroy;

        /// <summary>
        /// Event handler for Transform create messages.
        /// </summary>
        public ProcessTransformCreateDelegate OnTransformCreate;

        /// <summary>
        /// Event handler for Transform update messages.
        /// </summary>
        public ProcessTransformUpdateDelegate OnTransformUpdate;

        /// <summary>
        /// Event handler for Transform destroy messages.
        /// </summary>
        public ProcessTransformDestroyDelegate OnTransformDestroy;

        /// <summary>
        /// Event handler for Terrain create messages.
        /// </summary>
        public ProcessTerrainCreateDelegate OnTerrainCreate;

        /// <summary>
        /// Event handler for Terrain update messages.
        /// </summary>
        public ProcessTerrainUpdateDelegate OnTerrainUpdate;

        /// <summary>
        /// Event handler for load scene messages.
        /// </summary>
        public ProcessLoadSceneDelegate OnLoadScene;

        /// <summary>
        /// Event handler for change active scene messages.
        /// </summary>
        public ProcessChangeActiveSceneDelegate OnChangeActiveScene;

        /// <summary>
        /// Event handler for skinned mesh renderer create messages.
        /// </summary>
        public ProcessSkinnedMeshRendererCreateDelegate OnSkinnedMeshRendererCreate;

        /// <summary>
        /// Event handler for skinned mesh renderer update messages.
        /// </summary>
        public ProcessSkinnedMeshRendererUpdateDelegate OnSkinnedMeshRendererUpdate;

        /// <summary>
        /// Event handler for renderer update messages.
        /// </summary>
        public ProcessRendererUpdateDelegate OnRendererUpdate;

        /// <summary>
        /// Event handler for mesh renderer create messages.
        /// </summary>
        public ProcessMeshRendererCreateDelegate OnMeshRendererCreate;

        /// <summary>
        /// Event handler for mesh renderer destroy messages.
        /// </summary>
        public ProcessMeshRendererDestroyDelegate OnMeshRendererDestroy;

        /// <summary>
        /// Event handler for line renderer create messages.
        /// </summary>
        public ProcessLineRendererCreateDelegate OnLineRendererCreate;

        /// <summary>
        /// Event handler for line renderer update messages.
        /// </summary>
        public ProcessLineRendererUpdateDelegate OnLineRendererUpdate;

        /// <summary>
        /// Event handler for reflection probe create messages.
        /// </summary>
        public ProcessReflectionProbeCreateDelegate OnReflectionProbeCreate;

        /// <summary>
        /// Event handler for reflection probe update messages.
        /// </summary>
        public ProcessReflectionProbeUpdateDelegate OnReflectionProbeUpdate;

        /// <summary>
        /// Event handler for mesh filter create messages.
        /// </summary>
        public ProcessMeshFilterCreateDelegate OnMeshFilterCreate;

        /// <summary>
        /// Event handler for mesh filter update messages.
        /// </summary>
        public ProcessMeshFilterUpdateDelegate OnMeshFilterUpdate;

        /// <summary>
        /// Event handler for mesh filter destroy messages.
        /// </summary>
        public ProcessMeshFilterDestroyDelegate OnMeshFilterDestroy;

        /// <summary>
        /// Event handler for lightmaps update messages.
        /// </summary>
        public ProcessLightmapsUpdateDelegate OnLightmapsUpdate;

        /// <summary>
        /// Event handler for light create messages.
        /// </summary>
        public ProcessLightCreateDelegate OnLightCreate;

        /// <summary>
        /// Event handler for light update messages.
        /// </summary>
        public ProcessLightUpdateDelegate OnLightUpdate;

        /// <summary>
        /// Event handler for camera create messages.
        /// </summary>
        public ProcessCameraCreateDelegate OnCameraCreate;

        /// <summary>
        /// Event handler for camera update messages.
        /// </summary>
        public ProcessCameraUpdateDelegate OnCameraUpdate;

        /// <summary>
        /// Gets the start time of the PLUME recording.
        /// </summary>
        public DateTime StartTime { get; private set; } = DateTime.MinValue;

        /// <summary>
        /// Gets the name of the PLUME recording.
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the dictionary mapping GameObject GUIDs to their names.
        /// </summary>
        public Dictionary<string, string> GameObjectNames { get; private set; }

        private const uint LZ4MagicNumber = 0x184D2204; // https://github.com/liris-xr/PLUME-Viewer/blob/master/Runtime/Scripts/RecordLoader.cs#L145
        private Stream input;
        private List<string> registeryAssemblies;

        /// <summary>
        /// Registery of types to parse.
        /// </summary>
        protected TypeRegistry? registry = null;

        /// <summary>
        /// Logging instance.
        /// </summary>
        protected LogStatus log;

        // --- Unity Settings ---

        /// <summary>
        /// Delegate for processing render settings update messages.
        /// </summary>
        /// <param name="msg">The render settings update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessRenderSettingsUpdateDelegate(RenderSettingsUpdate msg, ulong? timestamp);

        // --- XRITK ---

        /// <summary>
        /// Delegate for processing XR base interactable create messages.
        /// </summary>
        /// <param name="msg">The XR base interactable create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessXRBaseInteractableCreateDelegate(XRBaseInteractableCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing XR base interactable update messages.
        /// </summary>
        /// <param name="msg">The XR base interactable update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessXRBaseInteractableUpdateDelegate(XRBaseInteractableUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing XR base interactor create messages.
        /// </summary>
        /// <param name="msg">The XR base interactor create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessXRBaseInteractorCreateDelegate(XRBaseInteractorCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing XR base interactor update messages.
        /// </summary>
        /// <param name="msg">The XR base interactor update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessXRBaseInteractorUpdateDelegate(XRBaseInteractorUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing XR ITK interaction messages.
        /// </summary>
        /// <param name="msg">The XR ITK interaction message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessXRITKInteractionDelegate(XRITKInteraction msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing XR base interactable destroy messages.
        /// </summary>
        /// <param name="msg">The XR base interactable destroy message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessXRBaseInteractableDestroyDelegate(XRBaseInteractableDestroy msg, ulong? timestamp);

        // --- Unity UI ---

        /// <summary>
        /// Delegate for processing rect transform create messages.
        /// </summary>
        /// <param name="msg">The rect transform create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessRectTransformCreateDelegate(RectTransformCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing rect transform update messages.
        /// </summary>
        /// <param name="msg">The rect transform update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessRectTransformUpdateDelegate(RectTransformUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing text create messages.
        /// </summary>
        /// <param name="msg">The text create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessTextCreateDelegate(TextCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing text update messages.
        /// </summary>
        /// <param name="msg">The text update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessTextUpdateDelegate(TextUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing graphic update messages.
        /// </summary>
        /// <param name="msg">The graphic update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessGraphicUpdateDelegate(GraphicUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing image create messages.
        /// </summary>
        /// <param name="msg">The image create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessImageCreateDelegate(ImageCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing image update messages.
        /// </summary>
        /// <param name="msg">The image update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessImageUpdateDelegate(ImageUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing canvas create messages.
        /// </summary>
        /// <param name="msg">The canvas create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessCanvasCreateDelegate(CanvasCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing canvas update messages.
        /// </summary>
        /// <param name="msg">The canvas update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessCanvasUpdateDelegate(CanvasUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing canvas renderer create messages.
        /// </summary>
        /// <param name="msg">The canvas renderer create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessCanvasRendererCreateDelegate(CanvasRendererCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing canvas renderer update messages.
        /// </summary>
        /// <param name="msg">The canvas renderer update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessCanvasRendererUpdateDelegate(CanvasRendererUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing canvas scaler create messages.
        /// </summary>
        /// <param name="msg">The canvas scaler create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessCanvasScalerCreateDelegate(CanvasScalerCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing canvas scaler update messages.
        /// </summary>
        /// <param name="msg">The canvas scaler update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessCanvasScalerUpdateDelegate(CanvasScalerUpdate msg, ulong? timestamp);

        // --- Unity Core ---

        /// <summary>
        /// Delegate for processing GameObject create messages.
        /// </summary>
        /// <param name="gameObjectCreate">The GameObject create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessGameObjectCreateDelegate(GameObjectCreate gameObjectCreate, ulong? timestamp);

        /// <summary>
        /// Delegate for processing GameObject update messages.
        /// </summary>
        /// <param name="gameObjectUpdate">The GameObject update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessGameObjectUpdateDelegate(GameObjectUpdate gameObjectUpdate, ulong? timestamp);

        /// <summary>
        /// Delegate for processing GameObject destroy messages.
        /// </summary>
        /// <param name="gameObjectDestroy">The GameObject destroy message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessGameObjectDestroyDelegate(GameObjectDestroy gameObjectDestroy, ulong? timestamp);

        /// <summary>
        /// Delegate for processing Transform create messages.
        /// </summary>
        /// <param name="msg">The Transform create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessTransformCreateDelegate(TransformCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing Transform update messages with coordinate system.
        /// </summary>
        /// <param name="name">The name of the GameObject.</param>
        /// <param name="transform">The coordinate system.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessTransformUpdateDelegate(string name, CoordinateSystem transform, ulong? timestamp);

        /// <summary>
        /// Delegate for processing Transform destroy messages.
        /// </summary>
        /// <param name="msg">The Transform destroy message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessTransformDestroyDelegate(TransformDestroy msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing Terrain create messages.
        /// </summary>
        /// <param name="msg">The Terrain create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessTerrainCreateDelegate(TerrainCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing Terrain update messages.
        /// </summary>
        /// <param name="msg">The Terrain update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessTerrainUpdateDelegate(TerrainUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing load scene messages.
        /// </summary>
        /// <param name="msg">The load scene message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessLoadSceneDelegate(LoadScene msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing change active scene messages.
        /// </summary>
        /// <param name="msg">The change active scene message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessChangeActiveSceneDelegate(ChangeActiveScene msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing skinned mesh renderer create messages.
        /// </summary>
        /// <param name="msg">The skinned mesh renderer create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessSkinnedMeshRendererCreateDelegate(SkinnedMeshRendererCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing skinned mesh renderer update messages.
        /// </summary>
        /// <param name="msg">The skinned mesh renderer update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessSkinnedMeshRendererUpdateDelegate(SkinnedMeshRendererUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing renderer update messages.
        /// </summary>
        /// <param name="msg">The renderer update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessRendererUpdateDelegate(RendererUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing mesh renderer create messages.
        /// </summary>
        /// <param name="msg">The mesh renderer create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessMeshRendererCreateDelegate(MeshRendererCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing mesh renderer destroy messages.
        /// </summary>
        /// <param name="msg">The mesh renderer destroy message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessMeshRendererDestroyDelegate(MeshRendererDestroy msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing line renderer create messages.
        /// </summary>
        /// <param name="msg">The line renderer create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessLineRendererCreateDelegate(LineRendererCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing line renderer update messages.
        /// </summary>
        /// <param name="msg">The line renderer update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessLineRendererUpdateDelegate(LineRendererUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing reflection probe create messages.
        /// </summary>
        /// <param name="msg">The reflection probe create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessReflectionProbeCreateDelegate(ReflectionProbeCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing reflection probe update messages.
        /// </summary>
        /// <param name="msg">The reflection probe update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessReflectionProbeUpdateDelegate(ReflectionProbeUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing mesh filter create messages.
        /// </summary>
        /// <param name="msg">The mesh filter create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessMeshFilterCreateDelegate(MeshFilterCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing mesh filter update messages.
        /// </summary>
        /// <param name="msg">The mesh filter update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessMeshFilterUpdateDelegate(MeshFilterUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing mesh filter destroy messages.
        /// </summary>
        /// <param name="msg">The mesh filter destroy message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessMeshFilterDestroyDelegate(MeshFilterDestroy msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing lightmaps update messages.
        /// </summary>
        /// <param name="msg">The lightmaps update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessLightmapsUpdateDelegate(LightmapsUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing light create messages.
        /// </summary>
        /// <param name="msg">The light create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessLightCreateDelegate(LightCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing light update messages.
        /// </summary>
        /// <param name="msg">The light update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessLightUpdateDelegate(LightUpdate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing camera create messages.
        /// </summary>
        /// <param name="msg">The camera create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessCameraCreateDelegate(CameraCreate msg, ulong? timestamp);

        /// <summary>
        /// Delegate for processing camera update messages.
        /// </summary>
        /// <param name="msg">The camera update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        public delegate void ProcessCameraUpdateDelegate(CameraUpdate msg, ulong? timestamp);

        /// <summary>
        /// Initializes a new instance of the <see cref="PlumeFileParser"/> class.
        /// </summary>
        /// <param name="assembliesToLoad">Optional list of assembly names to load for type registry.</param>
        /// <param name="log">Optional logging delegate.</param>
        public PlumeFileParser(List<string>? assembliesToLoad = null, LogStatus? log = null)
        {
            this.GameObjectNames = new Dictionary<string, string>();
            this.registeryAssemblies = new List<string>
            {
                typeof(PackedSample).Assembly.GetName().Name
            };
            if (assembliesToLoad != null)
            {
                this.registeryAssemblies.AddRange(assembliesToLoad);
            }

            this.LoadTypeRegistry();
            this.log = log ?? ((log) => { Console.WriteLine(log); });
        }

        /// <summary>
        /// Parses a PLUME file and loads its metadata.
        /// </summary>
        /// <param name="filePath">The path to the PLUME file to parse.</param>
        public void ParseFile(string filePath)
        {
            this.input = this.OpenFile(filePath);

            var packedMetadata = PackedSample.Parser.ParseDelimitedFrom(this.input);
            var metadata = packedMetadata.Payload.Unpack<RecordMetadata>();
            if (metadata == null)
            {
                throw new Exception("Failed to load metadata from record file");
            }

            this.StartTime = metadata.StartTime.ToDateTime();
            this.Name = metadata.Name;
            this.log($"Parsing {filePath}, record name : {this.Name}, start time : {this.StartTime}");

            // Unused metadata
            var packedGraphicsSettings = PackedSample.Parser.ParseDelimitedFrom(this.input);
            var graphicsSettings = packedGraphicsSettings.Payload.Unpack<GraphicsSettings>();

            if (graphicsSettings == null)
            {
                throw new Exception("Failed to load graphics settings from record file");
            }
        }

        /// <summary>
        /// Loads the type registry from the specified assemblies for Protocol Buffers type resolution.
        /// </summary>
        protected void LoadTypeRegistry() // https://github.com/liris-xr/PLUME-Viewer/blob/master/Runtime/Scripts/TypeRegistryProviderAssembliesLookup.cs
        {
            var messageDescriptors = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => this.registeryAssemblies.Contains(a.GetName().Name))
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

            this.registry = TypeRegistry.FromMessages(messageDescriptors);
        }

        /// <summary>
        /// Unpacks all samples from the PLUME file until the end of the stream.
        /// </summary>
        public void UnpackAll()
        {
            while (this.Unpack())
            {
                // Continue unpacking until no more samples are available
            }

            this.input.Close();
        }

        /// <summary>
        /// Unpacks a single sample from the PLUME file.
        /// </summary>
        /// <returns>True if a sample was successfully unpacked; false if end of stream or error.</returns>
        protected bool Unpack()
        {
            try
            {
                PackedSample packedSample = PackedSample.Parser.ParseDelimitedFrom(this.input);
                ulong? timestamp = packedSample.HasTimestamp ? packedSample.Timestamp : null;
                if (timestamp == null)
                {
                    this.log($"No timestamp found in the packed sample.");
                    return false; // No timestamp to process
                }

                var payload = packedSample.Payload.Unpack(this.registry);
                if (payload == null)
                {
                    this.log($"No payload found in the packed sample @{timestamp}.");
                    return true; // No payload to process
                }

                switch (payload)
                {
                    case Frame frame:
                        // Unpack frame
                        this.ProcessUnpackFrame(frame.Data.Select(frameData => frameData.Unpack(this.registry)).ToList(), (ulong)timestamp);
                        break;

                        // case Marker marker:
                        //    record.AddMarkerSample(marker);
                        //    break;
                        // case InputAction inputAction:
                        //    inputAction.Name
                        //    break;
                        // case RawSample<StreamSample> streamSample:
                        //    record.AddStreamSample(streamSample);
                        //    break;
                        // case RawSample<StreamOpen> streamOpen:
                        //    record.AddStreamOpenSample(streamOpen);
                        //    break;
                        // case RawSample<StreamClose> streamClose:
                        //    record.AddStreamCloseSample(streamClose);
                        //    break;
                        // case null:
                        //    break;
                        // default:
                        //    record.AddOtherSample(unpackedSample);
                        //    break;
                }
            }
            catch (Exception ex)
            {
                this.log($"Error unpacking sample: {ex.Message}");
                return false; // End of stream or invalid data
            }

            return true;
        }

        /// <summary>
        /// Processes an unpacked message (currently not implemented).
        /// </summary>
        /// <param name="message">The message to process.</param>
        protected void ProcessUnpack(IMessage? message)
        {
        }

        /// <summary>
        /// Processes a frame containing multiple messages.
        /// </summary>
        /// <param name="message">List of messages to process.</param>
        /// <param name="timestamp">The timestamp of the frame.</param>
        protected void ProcessUnpackFrame(List<IMessage> message, ulong timestamp)
        {
            foreach (var msg in message)
            {
                switch (msg)
                {
                    case GameObjectCreate gameObjectCreate:
                        this.ProcessGameObjectCreate(gameObjectCreate, timestamp);
                        break;
                    case GameObjectUpdate gameObjectUpdate:
                        this.ProcessGameObjectUpdate(gameObjectUpdate, timestamp);
                        break;
                    case GameObjectDestroy gameObjectDestroy:
                        this.ProcessGameObjectDestroy(gameObjectDestroy, timestamp);
                        break;
                    case RenderSettingsUpdate renderSettingsUpdate:
                        this.ProcessRenderSettingsUpdate(renderSettingsUpdate, timestamp);
                        break;
                    case XRBaseInteractableCreate xrBaseInteractableCreate:
                        this.ProcessXRBaseInteractableCreate(xrBaseInteractableCreate, timestamp);
                        break;
                    case XRBaseInteractableUpdate xrBaseInteractableUpdate:
                        this.ProcessXRBaseInteractableUpdate(xrBaseInteractableUpdate, timestamp);
                        break;
                    case XRBaseInteractorCreate xrBaseInteractorCreate:
                        this.ProcessXRBaseInteractorCreate(xrBaseInteractorCreate, timestamp);
                        break;
                    case XRBaseInteractorUpdate xrBaseInteractorUpdate:
                        this.ProcessXRBaseInteractorUpdate(xrBaseInteractorUpdate, timestamp);
                        break;
                    case XRITKInteraction xritkInteraction:
                        this.ProcessXRITKInteraction(xritkInteraction, timestamp);
                        break;
                    case XRBaseInteractableDestroy xrBaseInteractableDestroy:
                        this.ProcessXRBaseInteractableDestroy(xrBaseInteractableDestroy, timestamp);
                        break;
                    case RectTransformCreate rectTransformCreate:
                        this.ProcessRectTransformCreate(rectTransformCreate, timestamp);
                        break;
                    case RectTransformUpdate rectTransformUpdate:
                        this.ProcessRectTransformUpdate(rectTransformUpdate, timestamp);
                        break;
                    case TextCreate textCreate:
                        this.ProcessTextCreate(textCreate, timestamp);
                        break;
                    case TextUpdate textUpdate:
                        this.ProcessTextUpdate(textUpdate, timestamp);
                        break;
                    case GraphicUpdate graphicUpdate:
                        this.ProcessGraphicUpdate(graphicUpdate, timestamp);
                        break;
                    case ImageCreate imageCreate:
                        this.ProcessImageCreate(imageCreate, timestamp);
                        break;
                    case ImageUpdate imageUpdate:
                        this.ProcessImageUpdate(imageUpdate, timestamp);
                        break;
                    case CanvasCreate canvasCreate:
                        this.ProcessCanvasCreate(canvasCreate, timestamp);
                        break;
                    case CanvasUpdate canvasUpdate:
                        this.ProcessCanvasUpdate(canvasUpdate, timestamp);
                        break;
                    case CanvasRendererCreate canvasRendererCreate:
                        this.ProcessCanvasRendererCreate(canvasRendererCreate, timestamp);
                        break;
                    case CanvasRendererUpdate canvasRendererUpdate:
                        this.ProcessCanvasRendererUpdate(canvasRendererUpdate, timestamp);
                        break;
                    case CanvasScalerCreate canvasScalerCreate:
                        this.ProcessCanvasScalerCreate(canvasScalerCreate, timestamp);
                        break;
                    case CanvasScalerUpdate canvasScalerUpdate:
                        this.ProcessCanvasScalerUpdate(canvasScalerUpdate, timestamp);
                        break;
                    case TransformCreate transformCreate:
                        this.ProcessTransformCreate(transformCreate, timestamp);
                        break;
                    case TransformUpdate transformUpdate:
                        this.ProcessTransformUpdate(transformUpdate, timestamp);
                        break;
                    case TransformDestroy transformDestroy:
                        this.ProcessTransformDestroy(transformDestroy, timestamp);
                        break;
                    case TerrainCreate terrainCreate:
                        this.ProcessTerrainCreate(terrainCreate, timestamp);
                        break;
                    case TerrainUpdate terrainUpdate:
                        this.ProcessTerrainUpdate(terrainUpdate, timestamp);
                        break;
                    case LoadScene loadScene:
                        this.ProcessLoadScene(loadScene, timestamp);
                        break;
                    case ChangeActiveScene changeActiveScene:
                        this.ProcessChangeActiveScene(changeActiveScene, timestamp);
                        break;
                    case SkinnedMeshRendererCreate skinnedMeshRendererCreate:
                        this.ProcessSkinnedMeshRendererCreate(skinnedMeshRendererCreate, timestamp);
                        break;
                    case SkinnedMeshRendererUpdate skinnedMeshRendererUpdate:
                        this.ProcessSkinnedMeshRendererUpdate(skinnedMeshRendererUpdate, timestamp);
                        break;
                    case RendererUpdate rendererUpdate:
                        this.ProcessRendererUpdate(rendererUpdate, timestamp);
                        break;
                    case MeshRendererCreate meshRendererCreate:
                        this.ProcessMeshRendererCreate(meshRendererCreate, timestamp);
                        break;
                    case MeshRendererDestroy meshRendererDestroy:
                        this.ProcessMeshRendererDestroy(meshRendererDestroy, timestamp);
                        break;
                    case LineRendererCreate lineRendererCreate:
                        this.ProcessLineRendererCreate(lineRendererCreate, timestamp);
                        break;
                    case LineRendererUpdate lineRendererUpdate:
                        this.ProcessLineRendererUpdate(lineRendererUpdate, timestamp);
                        break;
                    case ReflectionProbeCreate reflectionProbeCreate:
                        this.ProcessReflectionProbeCreate(reflectionProbeCreate, timestamp);
                        break;
                    case ReflectionProbeUpdate reflectionProbeUpdate:
                        this.ProcessReflectionProbeUpdate(reflectionProbeUpdate, timestamp);
                        break;
                    case MeshFilterCreate meshFilterCreate:
                        this.ProcessMeshFilterCreate(meshFilterCreate, timestamp);
                        break;
                    case MeshFilterUpdate meshFilterUpdate:
                        this.ProcessMeshFilterUpdate(meshFilterUpdate, timestamp);
                        break;
                    case MeshFilterDestroy meshFilterDestroy:
                        this.ProcessMeshFilterDestroy(meshFilterDestroy, timestamp);
                        break;
                    case LightmapsUpdate lightmapsUpdate:
                        this.ProcessLightmapsUpdate(lightmapsUpdate, timestamp);
                        break;
                    case LightCreate lightCreate:
                        this.ProcessLightCreate(lightCreate, timestamp);
                        break;
                    case LightUpdate lightUpdate:
                        this.ProcessLightUpdate(lightUpdate, timestamp);
                        break;
                    case CameraCreate cameraCreate:
                        this.ProcessCameraCreate(cameraCreate, timestamp);
                        break;
                    case CameraUpdate cameraUpdate:
                        this.ProcessCameraUpdate(cameraUpdate, timestamp);
                        break;
                    default:
                        this.log($"Unknown message type: {msg?.Descriptor.FullName} at {timestamp}");
                        break;
                }
            }
        }

        /// <summary>
        /// Processes a render settings update message.
        /// </summary>
        /// <param name="msg">The render settings update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessRenderSettingsUpdate(RenderSettingsUpdate msg, ulong? timestamp)
        {
            this.log($"RenderSettingsUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes an XR base interactable create message.
        /// </summary>
        /// <param name="msg">The XR base interactable create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessXRBaseInteractableCreate(XRBaseInteractableCreate msg, ulong? timestamp)
        {
            this.log($"XRBaseInteractableCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes an XR base interactable update message.
        /// </summary>
        /// <param name="msg">The XR base interactable update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessXRBaseInteractableUpdate(XRBaseInteractableUpdate msg, ulong? timestamp)
        {
            this.log($"XRBaseInteractableUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes an XR base interactor create message.
        /// </summary>
        /// <param name="msg">The XR base interactor create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessXRBaseInteractorCreate(XRBaseInteractorCreate msg, ulong? timestamp)
        {
            this.log($"XRBaseInteractorCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes an XR base interactor update message.
        /// </summary>
        /// <param name="msg">The XR base interactor update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessXRBaseInteractorUpdate(XRBaseInteractorUpdate msg, ulong? timestamp)
        {
            this.log($"XRBaseInteractorUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes an XR ITK interaction message.
        /// </summary>
        /// <param name="msg">The XR ITK interaction message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessXRITKInteraction(XRITKInteraction msg, ulong? timestamp)
        {
            this.log($"XRITKInteraction, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes an XR base interactable destroy message.
        /// </summary>
        /// <param name="msg">The XR base interactable destroy message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessXRBaseInteractableDestroy(XRBaseInteractableDestroy msg, ulong? timestamp)
        {
            this.log($"XRBaseInteractableDestroy, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a rect transform create message.
        /// </summary>
        /// <param name="msg">The rect transform create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessRectTransformCreate(RectTransformCreate msg, ulong? timestamp)
        {
            this.log($"RectTransformCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a rect transform update message.
        /// </summary>
        /// <param name="msg">The rect transform update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessRectTransformUpdate(RectTransformUpdate msg, ulong? timestamp)
        {
            this.log($"RectTransformUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a text create message.
        /// </summary>
        /// <param name="msg">The text create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessTextCreate(TextCreate msg, ulong? timestamp)
        {
            this.log($"TextCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a text update message.
        /// </summary>
        /// <param name="msg">The text update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessTextUpdate(TextUpdate msg, ulong? timestamp)
        {
            this.log($"TextUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a graphic update message.
        /// </summary>
        /// <param name="msg">The graphic update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessGraphicUpdate(GraphicUpdate msg, ulong? timestamp)
        {
            this.log($"GraphicUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes an image create message.
        /// </summary>
        /// <param name="msg">The image create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessImageCreate(ImageCreate msg, ulong? timestamp)
        {
            this.log($"ImageCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes an image update message.
        /// </summary>
        /// <param name="msg">The image update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessImageUpdate(ImageUpdate msg, ulong? timestamp)
        {
            this.log($"ImageUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a canvas create message.
        /// </summary>
        /// <param name="msg">The canvas create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessCanvasCreate(CanvasCreate msg, ulong? timestamp)
        {
            this.log($"CanvasCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a canvas update message.
        /// </summary>
        /// <param name="msg">The canvas update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessCanvasUpdate(CanvasUpdate msg, ulong? timestamp)
        {
            this.log($"CanvasUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a canvas renderer create message.
        /// </summary>
        /// <param name="msg">The canvas renderer create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessCanvasRendererCreate(CanvasRendererCreate msg, ulong? timestamp)
        {
            this.log($"CanvasRendererCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a canvas renderer update message.
        /// </summary>
        /// <param name="msg">The canvas renderer update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessCanvasRendererUpdate(CanvasRendererUpdate msg, ulong? timestamp)
        {
            this.log($"CanvasRendererUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a canvas scaler create message.
        /// </summary>
        /// <param name="msg">The canvas scaler create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessCanvasScalerCreate(CanvasScalerCreate msg, ulong? timestamp)
        {
            this.log($"CanvasScalerCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a canvas scaler update message.
        /// </summary>
        /// <param name="msg">The canvas scaler update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessCanvasScalerUpdate(CanvasScalerUpdate msg, ulong? timestamp)
        {
            this.log($"CanvasScalerUpdate, Timestamp: {timestamp}");
        }

        /// --- Unity Core ---
        /// <summary>
        /// Processes a GameObject create message.
        /// </summary>
        /// <param name="gameObjectCreate">The GameObject create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessGameObjectCreate(GameObjectCreate gameObjectCreate, ulong? timestamp)
        {
            this.log($"GameObject create: {gameObjectCreate.Id}, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a GameObject update message and stores GameObject names.
        /// </summary>
        /// <param name="gameObjectUpdate">The GameObject update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessGameObjectUpdate(GameObjectUpdate gameObjectUpdate, ulong? timestamp)
        {
            if (gameObjectUpdate.HasName && !this.GameObjectNames.ContainsKey(gameObjectUpdate.Id.TransformGuid))
            {
                this.GameObjectNames.Add(gameObjectUpdate.Id.TransformGuid, gameObjectUpdate.Name);
            }
        }

        /// <summary>
        /// Processes a GameObject destroy message.
        /// </summary>
        /// <param name="gameObjectDestroy">The GameObject destroy message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessGameObjectDestroy(GameObjectDestroy gameObjectDestroy, ulong? timestamp)
        {
            this.log($"GameObject Destroy: {gameObjectDestroy.Id}, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a Transform create message.
        /// </summary>
        /// <param name="msg">The Transform create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessTransformCreate(TransformCreate msg, ulong? timestamp)
        {
            this.log($"TransformCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a Transform update message and invokes the OnTransformUpdate delegate.
        /// </summary>
        /// <param name="msg">The Transform update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessTransformUpdate(TransformUpdate msg, ulong? timestamp)
        {
            if (this.GameObjectNames.ContainsKey(msg.Component.Guid))
            {
                Vector3 pos = msg.LocalPosition ?? new Vector3();
                Quaternion rot = msg.LocalRotation ?? new Quaternion();
                MathNet.Spatial.Euclidean.Point3D position = new Point3D(pos.X, pos.Y, pos.Z);
                MathNet.Spatial.Euclidean.Quaternion rotation = new MathNet.Spatial.Euclidean.Quaternion(rot.W, rot.X, rot.Y, rot.Z);
                CoordinateSystem system = Helpers.PositionAndQuaternionTo.CoordinateSystem(position, rotation);
                this.OnTransformUpdate(this.GameObjectNames[msg.Component.Guid], system, timestamp);
            }
        }

        /// <summary>
        /// Processes a Transform destroy message.
        /// </summary>
        /// <param name="msg">The Transform destroy message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessTransformDestroy(TransformDestroy msg, ulong? timestamp)
        {
            this.log($"TransformDestroy, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a Terrain create message.
        /// </summary>
        /// <param name="msg">The Terrain create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessTerrainCreate(TerrainCreate msg, ulong? timestamp)
        {
            this.log($"TerrainCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a Terrain update message.
        /// </summary>
        /// <param name="msg">The Terrain update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessTerrainUpdate(TerrainUpdate msg, ulong? timestamp)
        {
            this.log($"TerrainUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a load scene message.
        /// </summary>
        /// <param name="msg">The load scene message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessLoadScene(LoadScene msg, ulong? timestamp)
        {
            this.log($"LoadScene, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a change active scene message.
        /// </summary>
        /// <param name="msg">The change active scene message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessChangeActiveScene(ChangeActiveScene msg, ulong? timestamp)
        {
            this.log($"ChangeActiveScene, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a skinned mesh renderer create message.
        /// </summary>
        /// <param name="msg">The skinned mesh renderer create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessSkinnedMeshRendererCreate(SkinnedMeshRendererCreate msg, ulong? timestamp)
        {
            this.log($"SkinnedMeshRendererCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a skinned mesh renderer update message.
        /// </summary>
        /// <param name="msg">The skinned mesh renderer update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessSkinnedMeshRendererUpdate(SkinnedMeshRendererUpdate msg, ulong? timestamp)
        {
            this.log($"SkinnedMeshRendererUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a renderer update message.
        /// </summary>
        /// <param name="msg">The renderer update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessRendererUpdate(RendererUpdate msg, ulong? timestamp)
        {
            this.log($"RendererUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a mesh renderer create message.
        /// </summary>
        /// <param name="msg">The mesh renderer create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessMeshRendererCreate(MeshRendererCreate msg, ulong? timestamp)
        {
            this.log($"MeshRendererCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a mesh renderer destroy message.
        /// </summary>
        /// <param name="msg">The mesh renderer destroy message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessMeshRendererDestroy(MeshRendererDestroy msg, ulong? timestamp)
        {
            this.log($"MeshRendererDestroy, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a line renderer create message.
        /// </summary>
        /// <param name="msg">The line renderer create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessLineRendererCreate(LineRendererCreate msg, ulong? timestamp)
        {
            this.log($"LineRendererCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a line renderer update message.
        /// </summary>
        /// <param name="msg">The line renderer update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessLineRendererUpdate(LineRendererUpdate msg, ulong? timestamp)
        {
            this.log($"LineRendererUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a reflection probe create message.
        /// </summary>
        /// <param name="msg">The reflection probe create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessReflectionProbeCreate(ReflectionProbeCreate msg, ulong? timestamp)
        {
            this.log($"ReflectionProbeCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a reflection probe update message.
        /// </summary>
        /// <param name="msg">The reflection probe update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessReflectionProbeUpdate(ReflectionProbeUpdate msg, ulong? timestamp)
        {
            this.log($"ReflectionProbeUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a mesh filter create message.
        /// </summary>
        /// <param name="msg">The mesh filter create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessMeshFilterCreate(MeshFilterCreate msg, ulong? timestamp)
        {
            this.log($"MeshFilterCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a mesh filter update message.
        /// </summary>
        /// <param name="msg">The mesh filter update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessMeshFilterUpdate(MeshFilterUpdate msg, ulong? timestamp)
        {
            this.log($"MeshFilterUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a mesh filter destroy message.
        /// </summary>
        /// <param name="msg">The mesh filter destroy message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessMeshFilterDestroy(MeshFilterDestroy msg, ulong? timestamp)
        {
            this.log($"MeshFilterDestroy, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a lightmaps update message.
        /// </summary>
        /// <param name="msg">The lightmaps update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessLightmapsUpdate(LightmapsUpdate msg, ulong? timestamp)
        {
            this.log($"LightmapsUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a light create message.
        /// </summary>
        /// <param name="msg">The light create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessLightCreate(LightCreate msg, ulong? timestamp)
        {
            this.log($"LightCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a light update message.
        /// </summary>
        /// <param name="msg">The light update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessLightUpdate(LightUpdate msg, ulong? timestamp)
        {
            this.log($"LightUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a camera create message.
        /// </summary>
        /// <param name="msg">The camera create message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessCameraCreate(CameraCreate msg, ulong? timestamp)
        {
            this.log($"CameraCreate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Processes a camera update message.
        /// </summary>
        /// <param name="msg">The camera update message.</param>
        /// <param name="timestamp">The timestamp of the message.</param>
        protected virtual void ProcessCameraUpdate(CameraUpdate msg, ulong? timestamp)
        {
            this.log($"CameraUpdate, Timestamp: {timestamp}");
        }

        /// <summary>
        /// Opens a PLUME file and automatically handles LZ4 decompression if needed.
        /// </summary>
        /// <param name="filePath">The path to the PLUME file.</param>
        /// <returns>A stream for reading the file contents.</returns>
        private Stream OpenFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file {filePath} does not exist.");
            }

            var input = File.OpenRead(filePath);

            if (this.IsLZ4Compressed(input))
            {
                this.log($"Decoding {filePath}...");
                return K4os.Compression.LZ4.Streams.LZ4Stream.Decode(input);
            }

            return input;
        }

        /// <summary>
        /// Determines whether a file stream is LZ4 compressed by checking its magic number.
        /// </summary>
        /// <param name="fileStream">The file stream to check.</param>
        /// <returns>True if the file is LZ4 compressed; otherwise false.</returns>
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
