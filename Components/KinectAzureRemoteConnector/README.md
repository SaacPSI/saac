# Kinect Azure Remote Connector

## Summary
Project including some components to handle Kinect Azure cameras. 

## Files
* [KinectAzureRemoteConnectorConfiguration](src/KinectAzureRemoteConnectorConfiguration.cs) include informations to connect to the kinect application.
* [KinectAzureRemoteConnector](src/KinectAzureRemoteConnector.cs) is a subpipline that manage the process for a RendezVous with the remote application. 
* [KinectAzureRemoteConnectorComponent](src/KinectAzureRemoteConnectorComponent.cs) is the component that integrate a RendezVous client to be used with [KinectAzureRemoteServerComponent](src/KinectAzureRemoteServerComponent.cs).
* [KinectAzureRemoteStreamsConfiguration](src/KinectAzureRemoteStreamsConfiguration.cs) include informations to connect to the kinect and configuring streams.
* [KinectAzureRemoteStreams](src/KinectAzureRemoteStreams.cs) is subpipline that manage the process for a RendezVous for the kinect.
* [KinectAzureRemoteStreamsConfiguration](src/KinectAzureRemoteStreamsConfiguration.cs) is the component that integrate a RendezVous server to be used with [KinectAzureRemoteConnectorComponent](src/KinectAzureRemoteConnectorComponent.cs).

## Package /psi
 Microsoft.Psi.Runtime nugetPackage needs to be build from our [fork](https://github.com/SaacPSI/psi) branch Pipeline is a modified version of /psi allowing to remove a subpipline from a parent pipline without stoping the whole application. See [issue](https://github.com/microsoft/psi/issues/291) for more information.

## Curent issues

## Future works
