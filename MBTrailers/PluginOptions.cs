using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;
using MediaBrowser.Library.Plugins.Configuration;

namespace MBTrailers
{
    public class PluginOptions : PluginConfigurationOptions
    {
        [Label("Menu Name:")]
        public string MenuName = "MB Trailers";

        [Label("Use HD Trailers")]
        public bool HDTrailers = false;

        [Label("Show Local 'My Trailers'")]
        public bool ShowMyTrailers = false;

        [Label("My Trailers Name:")]
        public string MyTrailerName = "My Trailers";

        //[Label("Sort Value")]
        //public string SortOrder = "";

        [Label("Cache Directory")] 
        public string CacheDir = "";

        [Label("Force Reload")]
        [Hidden]
        public bool Changed = false;
    }
}
