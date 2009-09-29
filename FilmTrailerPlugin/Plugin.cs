using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;

namespace FilmTrailerPlugin {
    class Plugin : BasePlugin {

        static readonly Guid TrailersGuid = new Guid("{B70517FE-9B66-44a7-838B-CC2A2B6FEC0C}");
        public static PluginConfiguration<PluginOptions> PluginOptions { get; set; }

        public override void Init(Kernel kernel) {
            PluginOptions = new PluginConfiguration<PluginOptions>(kernel, this.GetType().Assembly);
            PluginOptions.Load();

            var trailers = kernel.ItemRepository.RetrieveItem(TrailersGuid) ?? new FilmTrailerFolder(); 
            trailers.Path = "";
            trailers.Id = TrailersGuid;

            kernel.RootFolder.AddVirtualChild(trailers);
        }

        public override bool IsConfigurable {
            get {
                return true;
            }
        }

        public override IPluginConfiguration PluginConfiguration {
            get {
                return PluginOptions;
            }
        }

        public override string Name {
            get { return "Film Trailers"; }
        }

        public override string Description {
            get { return "Film Trailers powered by filmtrailer.com"; }
        }

    }
}
