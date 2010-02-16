using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;

namespace ITunesTrailers {
    public class Plugin : BasePlugin {

        internal const string PluginName = "ITunes Trailers";
        internal const string PluginDescription = "HD Trailers powered by Apple.";

        static readonly Guid TrailersGuid = new Guid("{828DCFEF-AEAF-44f2-B6A8-32AEAF27F3DA}");
        public static PluginConfiguration<PluginOptions>  PluginOptions {get;set;}
        
        public override void Init(Kernel kernel) {
            PluginOptions = new PluginConfiguration<PluginOptions>(kernel, this.GetType().Assembly);
            PluginOptions.Load();
            
            var trailers = kernel.ItemRepository.RetrieveItem(TrailersGuid) ?? new ITunesTrailerFolder();
            trailers.Path = "";
            trailers.Id = TrailersGuid;            
            kernel.RootFolder.AddVirtualChild(trailers);            
        }

        public override IPluginConfiguration PluginConfiguration {
            get {
                return PluginOptions;
            }
        }

        public override string Name {
            get { return PluginName; }
        }

        public override string Description {
            get { return PluginDescription; }
        }

        public override bool IsConfigurable
        {
            get
            {
                return true;
            }
        }

    }
}
