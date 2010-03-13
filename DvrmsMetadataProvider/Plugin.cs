using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;

namespace DvrmsMetadataProvider {
    public class Plugin : BasePlugin {

        internal const string PluginName = "DVR-MS metadata";
        internal const string PluginDescription = "This plugin provides metadata for DVR-MS files. (all your recorded tv shows start off as dvr-ms files)"; 

        public override void Init(Kernel kernel) {
            kernel.MetadataProviderFactories.Add(new MetadataProviderFactory(typeof(DvrmsMetadataProvider))); 
        }

        public override string Name {
            get { return PluginName; }
        }

        public override string Description {
            get { return PluginDescription; }
        }

        public override System.Version RequiredMBVersion {
            get {
                return new System.Version(2, 2, 1, 0);
            }
        }
        public override System.Version TestedMBVersion {
            get {
                return new System.Version(2, 2, 3, 0);
            }
        }
    }
}
