# RendezVousPipelineServices

## Summary
Project handling network connections with automatic storing data stream to store. This project works with the Psi.Runtime package of Pipeline branch tant can be found in our [fork](https://github.com/SaacPSI/psi).

## Files
* [Bytes Stream To Image](src/BytesStreamToImage.cs) component to use in conjonction of Unity component to get capture of the rendering.
* [Connector Info](src/ConnectorInfo.cs) class that store information form a datastream and allow bridge between pipelines.
* [I Complex Transformer](src/IComplexTransformer.cs) interface for components that tranform incoming data from RendezVous before storing them in multiple-streams.
* [I Rebooting Component](src/IRebootingComponent.cs) interface for rebootable components.
* [Rebootable Sub Pipeline](src/RebootableSubPipeline.cs) class from SubPipeline that can (re)store data from rebootable components. 
* [Rebooter Rendez Vous Pipeline](src/RebooterRendezVousPipeline.cs) class that handle RendezVous process, stores and rebootable subpipelines.
* [Rendez Vous Pipeline](src/RendezVousPipeline.cs)  class that handle RendezVous process, stores and subpipelines.
* [Rendez Vous Pipeline Configuration](src/RendezVousPipelineConfiguration.cs) configuration class for RendezVousPipeline.
* [Pipe To Message](src/Helpers/PipeToMessage.cs) transform pipeline data stream to message forwarded to the delegate given.

In **Formats** folder:
* [I Format](src/Formats/IFormat.cs) interface for (de)serialization classes.
* [Psi Format Boolean](src/Formats/PsiFormatBoolean.cs)
* [Psi Format Bytes](src/Formats/PsiFormatBytes.cs)
* [Psi Format Char](src/Formats/PsiFormatChar.cs)
* [Psi Format Command](src/Formats/PsiFormatCommand.cs)
* [Psi Format Date Time](src/Formats/PsiFormatDateTime.cs)
* [Psi Format Int](src/Formats/PsiFormatInt.cs)
* [Psi Format Matrix4x4](src/Formats/PsiFormatMatrix4x4.cs)
* [Psi Format String](src/Formats/PsiFormatString.cs)
* [Psi Format Tuple Of Vector](src/Formats/PsiFormatTupleOfVector.cs)

## Curent issues

## Future works