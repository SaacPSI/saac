using Microsoft.Psi;
using Microsoft.Psi.Interop.Transport;
using System;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace SAAC.AnnotationsComponents
{
    /// <summary>
    /// Component that handles WebSocket connections and serves HTML pages for annotation requests.
    /// </summary>
    public class HTTPAnnotationsComponent : WebSocketsManager
    {
        private string htmlContent;
        private string annotationsConfiguration;
        private string sessionName;
        private Dictionary<string, Microsoft.Psi.Data.Annotations.AnnotationSchema> annotationSchemas;
        private Dictionary<string, string> annotationSchemasJson;
        private PipelineServices.RendezVousPipeline rdvPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="HTTPAnnotationsComponent"/> class.
        /// </summary>
        /// <param name="rdvPipeline">The rendezvous pipeline to connect to.</param>
        /// <param name="prefixAddress">The address to listen to.</param>
        /// <param name="annotationsFolder">The path of the annonation folder.</param>
        /// <param name="htmlFile">The path of the html file.</param>
        /// <param name="restrictToSecure">Boolean to force the use of ssl websocket.</param>
        public HTTPAnnotationsComponent(PipelineServices.RendezVousPipeline rdvPipeline, List<string> prefixAddress, string annotationsFolder = @".\AnnotationFiles\", string htmlFile = @".\AnnotationFiles\annotation.html", string sessionName = "Annotation", bool restrictToSecure = false)
            : base(true, prefixAddress, restrictToSecure)
        {
            if (!File.Exists(htmlFile))
                throw new FileNotFoundException($"Annotation html file not found: {htmlFile}");
            this.htmlContent = File.ReadAllText(htmlFile);

            this.annotationSchemas = new Dictionary<string, Microsoft.Psi.Data.Annotations.AnnotationSchema>();
            this.annotationSchemasJson = new Dictionary<string, string>();
            this.annotationsConfiguration = "";
            this.sessionName = sessionName;
            this.LoadAnnotationSchemas(annotationsFolder);
            base.OnNewWebSocketConnectedHandler += this.AnnotationConnection;
            this.rdvPipeline = rdvPipeline;
            this.rdvPipeline.Pipeline.PipelineRun += (s, e) => this.Start( (e) => { });
            this.rdvPipeline.Pipeline.PipelineCompleted += (s, e) => this.Stop(e.CompletedOriginatingTime, () => { });
        }


        /// <summary>
        /// Overrides the ProcessContexts method to handle HTTP requests for HTML pages.
        /// </summary>
        protected override async void ProcessContexts()
        {
            while (!this.token.IsCancellationRequested)
            { 
                try
                    {
                var result = await this.httpListener.GetContextAsync();
                if (result != null)
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
                }
                catch (Exception ex)
                {
                    rdvPipeline.Log($"HTTPAnnotationsComponent ProcessContexts Exception: {ex.Message}");
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
                    byte[] buffer = Encoding.UTF8.GetBytes(this.annotationsConfiguration);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "application/json; charset=utf-8";
                    response.StatusCode = 200;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
                else if (urlPath.Contains("/schema"))
                {
                    if (this.annotationSchemasJson.ContainsKey(request.Url.Query.TrimStart('?')))
                    {
                        string json = this.annotationSchemasJson[request.Url.Query.TrimStart('?')];
                        byte[] buffer = Encoding.UTF8.GetBytes(json);
                        response.ContentLength64 = buffer.Length;
                        response.ContentType = "application/json; charset=utf-8";
                        response.StatusCode = 200;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        response.StatusCode = 404;
                        response.Close();
                    }
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
                rdvPipeline.Log($"HTTPAnnotationsComponent HandleHTMLRequest Exception: {ex.Message}");
                response.StatusCode = 500;
                response.Close();
            }
            finally
            {
                response.Close();
            }
        }

        private void AnnotationConnection(object sender, (string, string, Uri) connectionInfo)
        {
            if (connectionInfo.Item2 == "annotation")
            {
                // Parse query string to get schema parameter
                string schemaName = null;
                if (!string.IsNullOrEmpty(connectionInfo.Item3.Query))
                {
                    var queryParams = HttpUtility.ParseQueryString(connectionInfo.Item3.Query);
                    schemaName = queryParams["schema"];
                }

                if (!string.IsNullOrEmpty(schemaName) && this.annotationSchemas.ContainsKey(schemaName))
                {
                    Microsoft.Psi.Data.Annotations.AnnotationSchema annotationSchema = this.annotationSchemas[schemaName];
                    string name = $"{connectionInfo.Item1}-Annotation";
                    Pipeline pipeline = rdvPipeline.GetOrCreateSubpipeline(name);
                    WebSocketSource<string>? source = this.ConnectWebsocketSource<string>(pipeline, PsiFormats.PsiFormatString.GetFormat(), connectionInfo.Item1, connectionInfo.Item2, false);
                    if (source is null)
                    { 
                        return;
                    }
                    Microsoft.Psi.Data.Session session = rdvPipeline.CreateOrGetSessionFromMode(this.sessionName);
                    Microsoft.Psi.Data.PsiExporter store = rdvPipeline.GetOrCreateStore(pipeline, session, rdvPipeline.GetStoreName(this.sessionName, name, session).Item2);
                    AnnotationProcessor annotationProcessor = new AnnotationProcessor(pipeline, annotationSchema, $"{name}Processor");
                    annotationProcessor.Write(annotationSchema, "Annotation", store);
                    source.Out.PipeTo(annotationProcessor.In);
                    pipeline.RunAsync();
                    rdvPipeline.Log($"New annotation WebSocket connection established for host {connectionInfo.Item1} with schema {schemaName}");
                }
                else
                {
                    rdvPipeline.Log($"Invalid or missing schema parameter in annotation WebSocket connection: {schemaName}");
                }
            }
        }

        private void LoadAnnotationSchemas(string annotationConfigurationFolder)
        {
            // For each files inside the folder, load the AnnotationSchema and store it in the dictionary
            foreach (string annotationConfigurationFile in Directory.GetFiles(annotationConfigurationFolder, "*.schema.json"))
            {
                if (Microsoft.Psi.Data.Annotations.AnnotationSchema.TryLoadFrom(annotationConfigurationFile, out Microsoft.Psi.Data.Annotations.AnnotationSchema annotationSchema))
                {
                    this.annotationSchemas.Add(annotationSchema.Name, annotationSchema);
                    this.annotationSchemasJson.Add(annotationSchema.Name, File.ReadAllText(annotationConfigurationFile));
                }
            }
            this.annotationsConfiguration = JsonConvert.SerializeObject(new { Names = this.annotationSchemas.Keys });
        }
    }
}
