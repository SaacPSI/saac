# AnnotationsComponents

## Summary
Project providing HTTP/WebSocket-based annotation components for integrating annotation interfaces with \\psi pipelines. Allows users to annotate data streams in real-time through a web-based interface while maintaining synchronization with the pipeline.
Annotation configuration is the same as [PsiStudio](https://github.com/microsoft/psi/wiki/Time-Interval-Annotations) .

## Files
* [HTTP Annotations Component](src/HTTPAnnotationsComponent.cs) main component that handles WebSocket connections and serves HTML pages for annotation requests. Inherits from [WebSocketsManager](../InteropExtension/src/WebSocketsManager.cs).
* [Annotation Processor](src/AnnotationProcessor.cs) component that processes annotation messages received from WebSocket connections and converts them into TimeIntervalAnnotationSet objects compatible with \\psi annotation system.

## Annotation Files
* [annotation.html](AnnotationFiles/annotation.html) HTML/JavaScript interface for the annotation web application, provide as example.
* [annotation.schema.json](AnnotationFiles/annotation.schema.json) schema definition file for annotation validation, provide as example from [doc](https://github.com/microsoft/psi/wiki/Time-Interval-Annotations).

## Current issues

## Future works
* Enhance the annotation system with additional features.


## Example Usage
```csharp
 // Instantiate the class that manage the RendezVous system and the pipeline execution
RendezVousPipeline rdvPipeline = new RendezVousPipeline(configuration, "Server");
rdvPipeline.CreateOrGetSession("TestAnnotationsSession");
List<string> adresss = new List<string>() { "http://localhost:8080/ws/", "http://localhost:8080/" };
SAAC.AnnotationsComponents.HTTPAnnotationsComponent annotationManager = new SAAC.AnnotationsComponents.HTTPAnnotationsComponent(rdvPipeline, adresss, @"C:\Users\User\Documents\PsiStudio\AnnotationSchemas\annotation.schema.json", @"\AnnotationFiles\annotation.html");

rdvPipeline.Start();
annotationManager.Start((e) => { });

//...

//Stop the annotation component and the pipeline
annotationManager.Stop();
rdvPipeline.Dispose();
```
