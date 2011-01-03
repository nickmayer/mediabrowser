using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;
using MediaBrowser.Library.Plugins.Configuration;

namespace MediaBrowser.Web
{
    public class PluginOptions : PluginConfigurationOptions
    {
        [Label("Port:")]
        public int Port = 88;
    }
}
