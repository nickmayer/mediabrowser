using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;

namespace NndbMetadataProvider {
    class Plugin : BasePlugin {

        internal const string PluginName = "Nndb image provider";
        internal const string PluginDescription = "Downloads actor and director images from nndb.com";


        public override void Init(Kernel kernel) {
            kernel.MetadataProviderFactories.Add(MetadataProviderFactory.Get<NndbPeopleProvider>());
        }

        public override string Name {
            get { return PluginName; }
        }

        public override string Description {
            get { return PluginDescription; }
        }
    }
}
