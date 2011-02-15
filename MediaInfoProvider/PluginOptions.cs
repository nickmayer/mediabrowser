using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;
using MediaBrowser.Library.Plugins.Configuration;

namespace MediaInfoProvider
{
    public class PluginOptions : PluginConfigurationOptions
    {
        [Label("Allow BD Rips (service)")]
        public bool AllowBDRips = false;

        [Label("Timeout (ms)")]
        public string ServiceTimeout = "15000";

        [Label("Clear Bad File List")]
        public bool ClearBadFiles = false;

        public List<string> BadFiles = new List<string>();

    }
}
