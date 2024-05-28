# Applications

## ffmpegToStore
### Summary
Tools to transfert a video file into a \\psi store including time synchronisation. This tool encode image into Jpeg format to reduce the size of the store and the audio transfert is optionnal.
### Arguments
* FFMPEG path
* Video file
* Time of the beginning of the video
* Name of the video store
* Path of the store
* The encoding level
* Name of the audio store (optionnal)
### Future works
* Add proper command line support (such as -help).
* Make optional the jpeg encoding.
* Make only audio transfert.

## KinectAzureRemoteApp
### Summary
This application allow to broadcast Azure Kinect streams through a (local) network easily. It store its own configuration and can allow multiple clients with our **RemoteExporter** modification (see [Remote Exporter Modification](../RemoteExporterModification.md)). 
To be used with [Kinect Azure Remote Connector](../Components/KinectAzureRemoteConnector/KinectAzureRemoteConnector.md).
### Future works
* Cleaning the UI
* Allowing **NamedPipes** protocol
* Start/Stop streaming from network commands

## SaaCPsiStudio
### Summary
This is a template to execute \psi pipeline in PsiStudio from our [fork](https://github.com/SaacPSI/psi), branch PsiStudio. It use [Rendez Vous Pipeline](../Components/RendezVousPipelineServices/src/RendezVousPipeline.cs) as main context for executing the pipeline.


## TestingConsole
### Summary
This is our testing application, it might have some working example code to test (in the master branch)!