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

        [Label("Trailer Suffix:")]
        public string TrailerSuffix = "Trailer";

        [Label("Auto Download")]
        public bool AutoDownload = false;

        [Label("Max Bandwidth (KBps)")]
        public string MaxBandWidth = "";

        //[Label("Sort Value")]
        //public string SortOrder = "";

        [Label("Cache Directory")] 
        public string CacheDir = "";

        [Label("Clear Old Downloads")]
        public bool AutoClearCache = false;

        [Label("Fetch Backdrops")]
        public bool FetchBackdrops = false;

        private bool _changed = false;
        public bool Changed
        {
            get { return _changed; }
            set { _changed = value; }
        }
    }
}
