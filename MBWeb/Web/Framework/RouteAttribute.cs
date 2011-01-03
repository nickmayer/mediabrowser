using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Web.Framework
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class RouteAttribute : Attribute
    {
        public RouteAttribute(string path)
        {
            this.Path = path;
        }
        public string Path { get; set; }
    }
}
