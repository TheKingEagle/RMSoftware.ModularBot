
using HttpMultipartParser;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;

namespace RMSoftware.Http
{
    public class QuickServer
    {
        public string Host { get;private set; }
        public int Port { get; private set; }

        private readonly Dictionary<string, Action<HttpListenerContext>> Routes = new Dictionary<string, Action<HttpListenerContext>>();
        private static readonly Dictionary<string, string> StaticRoutes = new Dictionary<string, string>();
        private readonly HttpListener Listener = new HttpListener();

        public QuickServer(string host, int port) 
        { 
            Host = host;
            Port = port;

        }

        public void Start() 
        {
            string baseUrl = $"http://{Host}:{Port}/";
            Console.WriteLine($"Listening for http requests on {baseUrl}");
            Listener.Prefixes.Add(baseUrl);
            Listener.Start();
            while (true)
            {
                try
                {
                    HttpListenerContext context = Listener.GetContext();
                    ThreadPool.QueueUserWorkItem((o) =>
                    {
                        var ctx = o as HttpListenerContext;
                        RouteRequest(ctx);
                    }, context);
                }
                catch (HttpListenerException ex)
                {
                    if (ex.ErrorCode != 995)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

        }

        public void Stop() => Listener.Abort();

        public void DefineRoute(string path, Action<HttpListenerContext> handler)
        {
            Routes[path] = handler;
        }

        public void DefineStaticFileRoute(string routePath, string folderPath)
        {
            StaticRoutes[routePath] = folderPath;
        }
        void HandleStaticFileRequest(HttpListenerContext context, string filePath)
        {
            if (File.Exists(filePath))
            {
                SendFileResponse(context.Response, filePath);
            }
            else
            {
                SendResponse(context.Response, "404 - Not Found", HttpStatusCode.NotFound);
            }
        }
        private void RouteRequest(HttpListenerContext context)
        {
            string urlPath = context.Request.Url.LocalPath;

            // Check if the request matches a static file route
            foreach (var staticRoute in StaticRoutes)
            {
                string routePath = staticRoute.Key;
                string staticFolder = staticRoute.Value;

                if (urlPath.StartsWith(routePath))
                {
                    string subPath = urlPath.Substring(routePath.Length);
                    string filePath = Path.Combine(staticFolder, subPath.TrimStart('/'));

                    // Ensure the constructed file path is within the static folder
                    if (filePath.StartsWith(staticFolder))
                    {
                        HandleStaticFileRequest(context, filePath);
                        return;
                    }
                }
            }

            if (Routes.TryGetValue(urlPath, out var handler))
            {
                handler(context);
            }
            else
            {
                SendResponse(context.Response, "404 - Not Found", HttpStatusCode.NotFound);
            }
        }

        private void SendFileResponse(HttpListenerResponse response, string filePath)
        {
            
            string ext = Path.GetExtension(filePath).ToLower();
            string contentType = FileMime.ContentTypes.ContainsKey(ext) ? FileMime.ContentTypes[ext] : "application/octet-stream";

            response.ContentType = contentType;

            try
            {
                byte[] buffer = File.ReadAllBytes(filePath);
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                SendResponse(response, $"Error: {ex.Message}", HttpStatusCode.InternalServerError);
            }
            finally
            {
                response.Close();
            }
        }

        public void SendResponse(HttpListenerResponse response,string contentType, byte[] content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            response.ContentType = contentType;
            response.StatusCode = (int)statusCode;
            response.OutputStream.Write(content, 0, content.Length);
            response.Close();
        }

        public void SendResponse(HttpListenerResponse response, string content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            response.ContentType = "text/html";
            response.StatusCode = (int)statusCode;
            byte[] c = Encoding.UTF8.GetBytes(content);
            response.OutputStream.Write(c, 0, c.Length);
            response.Close();
        }

        public (FormField[] Fields, FileField[] Files) ParseFormData(HttpListenerRequest request)
        {
            if (request.ContentType != null)
            {
                if (request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse URL-encoded form data
                    string formData = GetFormDataAsString(request.InputStream);
                    NameValueCollection formCollection = HttpUtility.ParseQueryString(formData);

                    var formFields = new List<FormField>();
                    foreach (string key in formCollection.AllKeys)
                    {
                        formFields.Add(new FormField
                        {
                            Name = key,
                            Value = formCollection[key]
                        });
                    }

                    return (formFields.ToArray(), null);
                }
                else if (request.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse multipart form data
                    MultipartFormDataParser parser = MultipartFormDataParser.Parse(request.InputStream);

                    var formFields = new List<FormField>();
                    var fileFields = new List<FileField>();

                    foreach (var parameter in parser.Parameters)
                    {
                        formFields.Add(new FormField
                        {
                            Name = parameter.Name,
                            Value = parameter.Data
                        });
                    }

                    foreach (var file in parser.Files)
                    {
                        fileFields.Add(new FileField
                        {
                            Name = file.Name,
                            FileName = file.FileName,
                            Data = file.Data
                        });
                    }

                    return (formFields.ToArray(), fileFields.ToArray());
                }
            }

            return (null, null);
        }

        private string GetFormDataAsString(Stream inputStream)
        {
            using (var reader = new StreamReader(inputStream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public class FormField
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class FileField
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public Stream Data { get; set; }
    }
}
