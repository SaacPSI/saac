using Microsoft.Psi;
using Microsoft.Psi.Interop.Transport;
using System;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SAAC.AnnotationsComponents
{
    /// <summary>
    /// Component that handles WebSocket connections and serves HTML pages for annotation requests.
    /// </summary>
    public class HTTPAnnotationsComponent : WebSocketsManager
    {
        private string htmlContent;
        private string annotationConfiguration;
        private Microsoft.Psi.Data.Annotations.AnnotationSchema annotationSchema;
        private PipelineServices.RendezVousPipeline rdvPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="HTTPAnnotationsComponent"/> class.
        /// </summary>
        /// <param name="rdvPipeline">The rendezvous pipeline to connect to.</param>
        /// <param name="prefixAddress">The address to listen to.</param>
        /// <param name="annotationConfigurationFile">The path of the annonation schema used.</param>
        /// <param name="htmlFile">The path of the html file.</param>
        /// <param name="restrictToSecure">Boolean to force the use of ssl websocket.</param>
        public HTTPAnnotationsComponent(PipelineServices.RendezVousPipeline rdvPipeline, List<string> prefixAddress, string annotationConfigurationFile = @".\AnnotationFiles\annotation.json", string htmlFile = @".\AnnotationFiles\annotation.html", bool restrictToSecure = false)
            : base(true, prefixAddress, restrictToSecure)
        {
            if (!File.Exists(htmlFile))
                throw new FileNotFoundException($"Annotation html file not found: {htmlFile}");
            this.htmlContent = File.ReadAllText(htmlFile);

            if (!File.Exists(annotationConfigurationFile) || !Microsoft.Psi.Data.Annotations.AnnotationSchema.TryLoadFrom(annotationConfigurationFile, out this.annotationSchema))
                throw new FileNotFoundException($"Annotation configuration file not found: {annotationConfigurationFile}");
     
            this.annotationConfiguration = File.ReadAllText(annotationConfigurationFile);
            base.OnNewWebSocketConnectedHandler += this.AnnotationConnection;
            this.rdvPipeline = rdvPipeline;
        }

        /// <summary>
        /// Overrides the ProcessContexts method to handle HTTP requests for HTML pages.
        /// </summary>
        protected override async void ProcessContexts()
        {
            while (!this.token.IsCancellationRequested)
            {
                var result = await this.httpListener.GetContextAsync();
                if (result != null)
                {
                    try
                    {
                        // Check if this is a WebSocket upgrade request
                        if (result.Request.IsWebSocketRequest)
                        {
                            // Handle WebSocket connection
                            AcceptWebsocketClients(result);
                        }
                        else
                        {
                            // Handle HTML page requests
                            HandleHTMLRequest(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"HTTPAnnotationsComponent ProcessContexts Exception: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Handles HTTP requests for HTML pages.
        /// </summary>
        /// <param name="context">The HTTP listener context.</param>
        private async void HandleHTMLRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                // Check if this is a request for the annotation page
                string urlPath = request.Url.AbsolutePath;

                // Serve the HTML content for annotation interface
                if (urlPath == "/" || urlPath == "/index.html" || urlPath.StartsWith("/annotation"))
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(this.htmlContent);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "text/html; charset=utf-8";
                    response.StatusCode = 200;

                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
                else if(urlPath.Contains("/topics"))
                {
                    // Example: Serve a simple JSON response for topics
                    byte[] buffer = Encoding.UTF8.GetBytes(this.annotationConfiguration);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "application/json; charset=utf-8";
                    response.StatusCode = 200;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
                else
                {
                    // Serve static content or return 404
                    response.StatusCode = 404;
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"HTTPAnnotationsComponent HandleHTMLRequest Exception: {ex.Message}");
                response.StatusCode = 500;
                response.Close();
            }
            finally
            {
                response.Close();
            }
        }

        private void AnnotationConnection(object sender, (string, string) connectionInfo)
        {
           if (connectionInfo.Item2 == "annotation")
           {
                string name = $"{connectionInfo.Item1}-Annotation";
                Pipeline pipeline = rdvPipeline.GetOrCreateSubpipeline(name);
                WebSocketSource<string>? source = this.ConnectWebsocketSource<string>(pipeline, PsiFormats.PsiFormatString.GetFormat(), connectionInfo.Item1, connectionInfo.Item2, false);
                if (source is null || rdvPipeline.CurrentSession is null)
                    return;
                Microsoft.Psi.Data.PsiExporter store = rdvPipeline.GetOrCreateStore(pipeline, rdvPipeline.CurrentSession, rdvPipeline.GetStoreName("Annotation", name, rdvPipeline.CurrentSession).Item2);
                AnnotationProcessor annotationProcessor = new AnnotationProcessor(pipeline, this.annotationSchema, $"{name}Processor");
                annotationProcessor.Write(this.annotationSchema, "Annotation", store);
                source.Out.PipeTo(annotationProcessor.In);
                pipeline.RunAsync();
                Trace.WriteLine($"New annotation WebSocket connection established for host {connectionInfo.Item1}");
            }
        }
    }
}
