# PipelineServices

## Summary
Project handling network connections with automatic storing data stream to store. This project works with the Psi.Runtime &  Psi.Interop packages of **Pipeline** branch tant can be found in our [fork](https://github.com/SaacPSI/psi).

## Files
* [Connector Info](src/ConnectorInfo.cs) class that store information form a datastream and allow bridge between pipelines.
* [Dataset Pipeline](src/DatasetPipeline.cs) class handling the management of a dataset, ceating session, store... inherit from [Connectors And Stores Creator](src/Helpers/ConnectorsAndStoresCreator.cs).
* [Dataset Pipeline Configuration](src/DatasetPipelineConfiguration.cs) configuration class for [Dataset Pipeline](src/DatasetPipeline.cs).
* [I Complex Transformer](src/IComplexTransformer.cs) interface for components that tranform incoming data from RendezVous before storing them in multiple-streams.
* [I Rebooting Component](src/IRebootingComponent.cs) interface for rebootable components.
* [Rebootable Sub Pipeline](src/RebootableSubPipeline.cs) class from SubPipeline that can (re)store data from rebootable components. 
* [Rebooter Rendez Vous Pipeline](src/RebooterRendezVousPipeline.cs) class that handle RendezVous process, stores and rebootable subpipelines.
* [Rendez Vous Pipeline](src/RendezVousPipeline.cs)  class that handle RendezVous process, stores and subpipelines, inherit from [Dataset Pipeline](src/DatasetPipeline.cs)
* [Rendez Vous Pipeline Configuration](src/RendezVousPipelineConfiguration.cs) configuration class for [Rendez Vous Pipeline](src/RendezVousPipeline.cs), inherit from [Dataset Pipeline Configuration](src/DatasetPipelineConfiguration.cs).
* [Replay Pipeline](src/ReplayPipeline.cs) class that load dataset and manage Pipeline for replay, inherit from [Dataset Pipeline](src/DatasetPipeline.cs). 
* [Replay Pipeline Configuration](src/ReplayPipelineConfiguration.cs) configuration class for [Replay Pipeline](src/ReplayPipeline.cs), inherit from [Dataset Pipeline Configuration](src/DatasetPipelineConfiguration.cs).


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

In **Helpers** folder:
* [Connectors And Stores Creator](src/Helpers/ConnectorsAndStoresCreator.cs) handle the creation of ConnectorInfos and stores, inherit from [Connectors Manager](src/Helpers/ConnectorsManager.cs).
* [Connectors Manager](src/Helpers/ConnectorsManager.cs) handle events and dictionnary of ConnectorInfos.
* [Dataset Loader](src/Helpers/DatasetLoader.cs) handle dataset and create ConnectorInfo for each stream, inherit from [Connectors Manager](src/Helpers/ConnectorsManager.cs).
* [Pipe To Message](src/Helpers/PipeToMessage.cs) transform pipeline data stream to message forwarded to the delegate given.
* [Tcp Writer Multi](src/Helpers/TcpWriterMulti.cs) modification of the \psi TCPWriter allowing multi TcpSource connections.

## Curent issues

## Future works