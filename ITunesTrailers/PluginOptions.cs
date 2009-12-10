using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;
using MediaBrowser.Library.Plugins.Configuration;

namespace ITunesTrailers
{
    public class PluginOptions : PluginConfigurationOptions
    {
        [Label("Menu Name:")]
        public string MenuName = "Apple Trailers";

        [Label("Use HD Trailers")]
        public bool HDTrailers = false;
    }
}
