using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HttpServer.Headers;
using System.IO;
using System.Reflection;

namespace MediaBrowser.Web.Framework
{
    public class EmbeddedContent : IServableContent
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
}
