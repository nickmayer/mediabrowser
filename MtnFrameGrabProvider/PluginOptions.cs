using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;
using MediaBrowser.Library.Plugins.Configuration;

namespace MtnFrameGrabProvider
{
    public class PluginOptions : PluginConfigurationOptions
    {
        [Label("Seconds in for image:")]
        public string SecondsIn = "300";

    }
}