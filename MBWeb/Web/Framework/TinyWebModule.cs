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

namespace MediaBrowser.Web.Framework
{
    public class TinyWebModule : IModule
    {
        class EmbeddedContent
        {
            public ContentTypeHeader ContentTypeHeader { get; set; }
            public Stream Stream
            {
                get
                {
                    return Assembly.GetManifestResourceStream(ResourceName);
                }
            }
            public string ResourceName { get; set; }
            public Assembly Assembly { get; set; }
        }

        Dictionary<string, ActionRunner> actions = new Dictionary<string, ActionRunner>();
        Dictionary<string, EmbeddedContent> embeddedContent = new Dictionary<string, EmbeddedContent>();

        public TinyWebModule()
            : this(Assembly.GetCallingAssembly())
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

                embeddedContent["/" + fileName] = new EmbeddedContent
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
                            actions[route.Path] = new ActionRunner { Type = type, Action = method };
                        }
                    }
                }
            }
        }

        public ProcessingResult Process(RequestContext context)
        {
            ActionRunner runner;
            if (actions.TryGetValue(context.Request.Uri.AbsolutePath, out runner))
            {
                var service = (JsonService)runner.Type.GetConstructor(Type.EmptyTypes).Invoke(null);
                string result = runner.Invoke(service,context.Request);

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

            EmbeddedContent content;
            if (embeddedContent.TryGetValue(context.Request.Uri.AbsolutePath, out content))
            {
                var actual = new MemoryStream();

                using (var body = content.Stream)
                {
                    StreamReader reader = new StreamReader(body);
                    var read = reader.ReadToEnd();
                    var bytes = Encoding.UTF8.GetBytes(read);

                    actual.Write(bytes, 0, bytes.Length);
                    context.Response.ContentLength.Value = bytes.Length;
                    actual.Seek(0, SeekOrigin.Begin);
                }

                context.Response.Add(content.ContentTypeHeader);
                var generator = new ResponseWriter();
                generator.SendHeaders(context.HttpContext, context.Response);
                generator.SendBody(context.HttpContext, actual);

                return ProcessingResult.Abort;
            }

            return ProcessingResult.Continue;

        }
    }
}
