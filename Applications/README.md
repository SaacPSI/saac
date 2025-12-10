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

## CameraRemoteApp
### Summary
This application allow to broadcast camera streams through a (local) network easily and/or record a dataset locally. It store its own configuration and can allow multiple clients with our **RemoteExporter** modification (see [Remote Exporter Modification](../RemoteExporterModification.md)). 
To be used with [Kinect Azure Remote Services](../Components/KinectAzureRemoteServices/README.md), [Kinect Remote Services](../Components/KinectRemoteServices/README.md) &  [Nuitrack Remote Services](../Components/NuitrackRemoteServices/README.md).
It use RendezVous system allowing to start (and stop) camera streams from the remote server with the given configuration (only for KinectAzure atm).
### Future works
* Allowing **NamedPipes** protocol
* Fully implement configuration over network.

## WhipserRemoteApp
### Summary
This application allow the use microphones system in conjuction of Whisper to provide a speech to text application that can stream and record audio and do a transcription locally.
It can transcrpit from wav files or dataset in local use.
It use RendezVous system allowing to start (and stop) the application from the remote server with the given configuration.
### Future works
* Allowing **NamedPipes** protocol
* Friendly tooltips
* Input source configuration saving/loading and networked.

## VideoRemoteApp
### Summary
This application stream screenshot of desktop or application throught network. This application allow, for example the streaming of a Unity server application without impacting the performance of the app.
### Future works
* Allowing **NamedPipes** protocol

## SaaCPsiStudio
### Summary
This is a template to execute \psi pipeline in PsiStudio from our [fork](https://github.com/SaacPSI/psi), branch PsiStudio. It use [Rendez Vous Pipeline](../Components/RendezVousPipelineServices/src/RendezVousPipeline.cs) as main context for executing the pipeline.


## TestingConsole
### Summary
This is our testing application, it might have some working example code to test (in the master branch)!