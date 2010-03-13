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
        internal const string PluginDescription = "This plugin provides rich information about your media using the MediaInfo project."; 

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
                return new System.Version(2, 2, 2, 0);
            }
        }
        public override System.Version TestedMBVersion
        {
            get
            {
                return new System.Version(2, 2, 3, 0);
            }
        }
    }
}
