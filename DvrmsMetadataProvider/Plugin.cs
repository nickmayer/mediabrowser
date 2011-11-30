using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Configuration;

namespace DvrmsMetadataProvider {
    public class Plugin : BasePlugin {

        internal const string PluginName = "DVR-MS and WTV metadata";
        internal const string PluginDescription = "This plugin provides metadata for DVR-MS and WTV files. (all your recorded tv shows start off as dvr-ms or wtv)"; 
        public static PluginConfiguration<PluginOptions> PluginOptions { get; set; }

        public override void Init(Kernel kernel) {
            PluginOptions = new PluginConfiguration<PluginOptions>(kernel, this.GetType().Assembly);
            PluginOptions.Load();

            kernel.MetadataProviderFactories.Add(new MetadataProviderFactory(typeof(DvrmsMetadataProvider)));

            Logger.ReportInfo(Name + " (version " + Version + ") Loaded.");
        }
        
        public override bool IsConfigurable
        {
            get
            {
                return true;
            }
        }

        public override IPluginConfiguration PluginConfiguration
        {
            get
            {
                return PluginOptions;
            }
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
                return new System.Version(2, 5, 1, 0);
            }
        }
    }
}
