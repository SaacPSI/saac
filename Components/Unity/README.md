# Platform for Situated Intelligence in Android with Unity
Dlls from build will not work as it, some modifications are needed for get it work in Unity (might work with versions newer than 2022.1.23f). A part of solution comes from this issue:
https://github.com/microsoft/psi/issues/263

In order to have shortcut code generation (from Emit.Reflexion) for serialization to happen, the default serializer need to be accessible and modifiable. Even if only TcpSource and TcpWriter are used, due to a test of ImmutableTypes in RecyclingPool (RecyclingPool.cs:24). 
Android does not handle the use of **dynamic** type with generic, all methods using **dynamic** need to be modified to a generic type.

All thoses modifications can be found in https://github.com/SaacPSI/psi branch UnityAndroid.

The unitypackage has been made with a fork from the \psi repository with the last commit from 24/06/2023, commit ID : d08fdd34f6957a92a6343d6e7978c2b8efc5f83a
It includes modifications of RemoteExorter to include multi clients streaming (see [RemoteExporterModification.md](../../RemoteExporterModification.md))

A Pull Request as been made in order for \psi to officially and fully supoort Unity & Android: [Compatibility with Unity & Android](https://github.com/microsoft/psi/pull/333)

## Unity Package
The package contains dlls and scripts in 3 folders:
In **Base** folder:
* PsiExporter : base script to inherit from for exporter scripts.
* PsiImporter : base script to inherit from for importer scripts.
* PsiPiplineManager : required component for scripts to work, can synchronise with other \\psi piplines through a **RemoteClockImporter**.
* PsiSerializerReflexion : file with serializers for KnownSerializer.

In **Exporters** folder:
* PsiExporterPosition : example of exporter to send position of the gameobject.
* PsiExporterPositionOrientation : example of exporter to send position \& rotation of the gameobject.
* PsiExporterImage : exporter to send image of the camera.
* PsiExporterImageAsSteam : exporter to send image of the camera as bytes stream.
* PsiExporterMatrix4x4  : exporter to the localToWorld matrix of the gameobject.

In **Formats** folder:
* PsiFormatImage : (De)Serialization for image. 
* Basics formats can be found on in the PsiFormats project, the dll can be add inside Unity.
* Others formats can be found in projects under the folders Formats/Unity/.

In **Importers** folder:
* PsiImportePosition : example of importer to set position of the gameobject.
* PsiImporterPing : example of importer to handle ping from server.

## Error CS0433 correction
In Unity Microsoft.BCL.Async should be configured as :

![Dll configuration](./docs/bcl_configuration.jpg) 

## How it works
Add the PsiPiplineManager in a GameObject and configure it 

![Pipeline manager](./docs/pipeline_manager.jpg) 

**Start Mode** have 3 modes available to exectue the pipeline manager:
* Manual : nothing is automated, other(s) scripts call all initialisation steps.
* Connection : the connection to the server is automated, then the manager is waiting for the signal to start.       
* Automatic : everything steps is automatically called dependeding of the configuration

**Exporter Number Expected At Start** is the number of exporter to wait before trigger the inialisation process of the pipeline manager. It allow to wait that everything is loaded/spawned in the scene.

**Rendez Vous Server Address** The address of the \psi server.

**Rendez Vous Server Port** The port of the \psi server.

**Used Process Name** The name the process used by the pipeline manager that will contains all exporters.

**Used Address** The ip address used by exporters in the application.

**Waited Process** List of process waited by the pipeline manager in order to continue it's initialisation (waiting for incoming streams).

**Accepted Process** Whitelist of process that can be used in the application. 

**Exporters Max Low Frequency Streams** Not used for the moment, will be used with [RemoteExporter](https://github.com/microsoft/psi/blob/master/Sources/Runtime/Microsoft.Psi/Remoting/RemoteExporter.cs).

**Exporters Starting Port** Starting port for exporter each new exporter will increment this number.

**Text Log Object** Optionnal object to log in scene the \psi part of the application. If not set log will be displayed in the console.

**Command Emitter Port** Optionnal port to send command and informations to other application over the network.

## *Android version*
First add **PSI_TCP_STREAMS** in **Scripting Define Symbols** in **Player Settings**. 

Then add exporters \& importers, for new type of serialization, you may need to add :
* A serializer in PsiAddedSerializer, and then in the PsiPiplineManager :
    
        protected void InitializeSerializer(KnownSerializers serializers)
        {
            serializers.Register<bool, BoolSerializer>();
            serializers.Register<char, CharSerializer>();
            serializers.Register<System.Numerics.Vector3, Vector3Serializer>();
            serializers.Register<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>, TupleOfVector3Serializer>();
        }

* A need PsiFormat of the type.
* Create the component(s) of using the base class PsiExporter/PsiImporter.
* Use your component(s)!
* On the server side you should use the same format classes and extend as in PipelineServices Formats Folder (PipelineServices/src/Formats):

        configuration.AddTopicFormatAndTransformer("Position1", typeof(System.Numerics.Vector3), new PsiFormatVector3());
        configuration.AddTopicFormatAndTransformer("Position2", typeof(System.Numerics.Vector3), new PsiFormatVector3());
        configuration.AddTopicFormatAndTransformer("State", typeof(bool), new PsiFormatBoolean());


