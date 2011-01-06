using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using HttpServer.Headers;
using System.IO;
using HttpServer.Messages;
using HttpServer;
using HttpServer.Modules;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Web.Framework
{
    public class TinyWebModule : IModule
    {
        Dictionary<string, IServableContent> content = new Dictionary<string, IServableContent>();

        public TinyWebModule()
            : this(typeof(TinyWebModule).Assembly)
        {

        }

        private string GuessContentHeader(string filename)
        {
            if (filename.EndsWith(".js"))
            {
                return "application/javascript";
            }
            if (filename.EndsWith(".html"))
            {
                return "text/html";
            }
            if (filename.EndsWith(".png"))
            {
                return "image/png";
            }
            if (filename.EndsWith(".jpg") || filename.EndsWith("jpeg"))
            {
                return "image/jpg";
            }
            if (filename.EndsWith(".gif"))
            {
                return "image/gif";
            }
            if (filename.EndsWith(".ico"))
            {
                return "image/vnd.microsoft.icon";
            }

            return "text/plain";
        }

        public TinyWebModule(Assembly assembly)
        {


            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                if (!resourceName.ToLower().Contains(".content."))
                    continue;

                string fileName = resourceName.Substring(resourceName.ToLower().IndexOf(".content.") + 9);

                content["/" + fileName] = new EmbeddedContent
                {
                    ResourceName = resourceName,
                    ContentTypeHeader = new ContentTypeHeader(GuessContentHeader(fileName)),
                    Assembly = assembly
                };

            }

            // json

            foreach (var type in assembly.GetTypes())
            {
                if (typeof(JsonService).IsAssignableFrom(type))
                {
                    foreach (var method in type.GetMethods())
                    {
                        var routes = method.GetCustomAttributes(typeof(RouteAttribute), false);
                        foreach (RouteAttribute route in routes)
                        {
                            content[route.Path] = new ActionRunner { Type = type, Action = method };
                        }
                    }
                }
            }
        }

        public void MapPath(string origin, string target)
        {
            content[origin] = content[target];
        }

        public ProcessingResult Process(RequestContext context)
        {

            IServableContent hit = null;
            content.TryGetValue(context.Request.Uri.AbsolutePath, out hit);
            var runner = hit as ActionRunner;

            if (runner != null)
            {
                var service = (JsonService)runner.Type.GetConstructor(Type.EmptyTypes).Invoke(null);
                string result = null;
                try
                {
                    result = runner.Invoke(service, context.Request);
                }
                catch (Exception e) 
                {
                    Logger.ReportException("Failed to execute action in MBWeb: " + context.Request.Uri.AbsolutePath, e);
                    throw;
                }

                var body = new MemoryStream();
                var bytes = Encoding.UTF8.GetBytes(result);
                body.Write(bytes, 0, bytes.Length);

                context.Response.ContentLength.Value = body.Length;

                context.Response.Add(new ContentTypeHeader("application/json"));

                var generator = new ResponseWriter();
                generator.SendHeaders(context.HttpContext, context.Response);
                generator.SendBody(context.HttpContext, body);

                return ProcessingResult.Abort;
            }

            EmbeddedContent embeddedContent = hit as EmbeddedContent;
            if (embeddedContent != null)
            {
                var actual = new MemoryStream();

                using (var body = embeddedContent.Stream)
                {
                    StreamReader reader = new StreamReader(body);
                    var read = reader.ReadToEnd();
                    var bytes = Encoding.UTF8.GetBytes(read);

                    actual.Write(bytes, 0, bytes.Length);
                    context.Response.ContentLength.Value = bytes.Length;
                    actual.Seek(0, SeekOrigin.Begin);
                }

                context.Response.Add(embeddedContent.ContentTypeHeader);
                var generator = new ResponseWriter();
                generator.SendHeaders(context.HttpContext, context.Response);
                generator.SendBody(context.HttpContext, actual);

                return ProcessingResult.Abort;
            }


            // fake 404 for now 
            var badRequest = new MemoryStream(Encoding.UTF8.GetBytes("Not Found : " + context.Request.Uri.AbsolutePath));
            var rw = new ResponseWriter();
            context.Response.ContentLength.Value = badRequest.Length;
            rw.SendHeaders(context.HttpContext, context.Response);
            rw.SendBody(context.HttpContext, badRequest);
            return ProcessingResult.Abort;

        }
    }
}
