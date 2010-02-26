using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;

namespace FrameGrabProvider {
    public class Plugin : BasePlugin {

        internal const string PluginName = "Frame Grab provider";
        internal const string PluginDescription = "This plugin provides frame grabs for videos which contain no cover art.";

        public override void Init(Kernel kernel) {


            kernel.MetadataProviderFactories.Add(new MetadataProviderFactory(typeof(FrameGrabProvider)));

            kernel.ImageResolvers.Add((path,canBeProcessed, item) =>
            {
                if (path.ToLower().StartsWith("grab")) {
                    return new GrabImage(); 
                }
                return null;
            });
        }


        public override string Name {
            get { return PluginName; }
        }

        public override string Description {
            get { return PluginDescription; }
        }

    }
}
