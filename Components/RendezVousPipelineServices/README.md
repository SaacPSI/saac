# RendezVousPipelineServices

## Summary
Project handling network connections with automatic storing data stream to store. This project works with the Psi.Runtime package of Pipeline branch tant can be found in our [forck](https://github.com/SaacPSI/psi).

## Files
* [Bytes Stream To Image](src/BytesStreamToImage.cs) component to use in conjonction of Unity component to get capture of the rendering.
* [Connector Info](src/ConnectorInfo.cs) class that store information form a datastream and allow bridge between pipelines.
* [Formats](src/Formats.cs) file that store the (de)serializer for TCP connections.
* [I Rebooting Component](src/IRebootingComponent.cs) interface for rebbotable components.
* [Rebootable Sub Pipeline](src/RebootableSubPipeline.cs) class from SubPipeline that can (re)store data from rebootable components. 
* [Rebooter Rendez Vous Pipeline](src/RebooterRendezVousPipeline.cs) class that handle RendezVous process, stores and rebootable subpipelines.
* [Rendez Vous Pipeline](src/RendezVousPipeline.cs)  class that handle RendezVous process, stores and subpipelines.
* [Rendez Vous Pipeline Configuration](src/RendezVousPipelineConfiguration.cs) configuration class for RendezVousPipeline.

## Curent issues
* [Bytes Stream To Image](src/BytesStreamToImage.cs) (or the conterpart component) doesn't seems to work well on Quest2

## Future works