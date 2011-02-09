using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;
using MediaBrowser.Library.Logging;

namespace MediaInfoProvider {
    public class Plugin : BasePlugin {

        internal const string PluginName = "MediaInfo Provider";
        internal const string PluginDescription = "This plugin uses the MediaInfo project to provide rich information about your media, such as codecs, aspect ratio, resolution, etc..\n\nFolder rips, ISO's and WTV files are currently not supported.\n\nThis version includes MediaInfo.dll version " + Plugin.includedMediaInfoDLL + ".";
        internal const string includedMediaInfoDLL = "0.7.38.0"; 

        public override void Init(Kernel kernel) {
            kernel.MetadataProviderFactories.Add(MetadataProviderFactory.Get<MediaInfoProvider>()); 
        }

        public override string Name {
            get { return PluginName; }
        }

        public override string Description {
            get { return PluginDescription; }
        }
        public override System.Version RequiredMBVersion
        {
            get
            {
                return new System.Version(2, 3, 0, 0);
            }
        }
        public override System.Version TestedMBVersion
        {
            get
            {
                return new System.Version(2, 3, 0, 0);
            }
        }
    }
}
