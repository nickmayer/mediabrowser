using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;
using WebProxy;

namespace MBTrailers {
    public class Plugin : BasePlugin {

        internal const string PluginName = "Media Browser Trailers";
        internal const string PluginDescription = "HD Trailers for MediaBrowser.";

        internal const int ProxyPort = 8752;

        public static HttpProxy proxy;

        static readonly Guid TrailersGuid = new Guid("{828DCFEF-AEAF-44f2-B6A8-32AEAF27F3DA}");
        public static PluginConfiguration<PluginOptions>  PluginOptions {get;set;}
        private bool isMC = false;
        
        public override void Init(Kernel kernel) {
            PluginOptions = new PluginConfiguration<PluginOptions>(kernel, this.GetType().Assembly);
            PluginOptions.Load();
            
            var trailers = (kernel.ItemRepository.RetrieveItem(TrailersGuid) as MBTrailerFolder) ?? new MBTrailerFolder();
            trailers.Path = "";
            trailers.Id = TrailersGuid;            
            kernel.RootFolder.AddVirtualChild(trailers);

            isMC = AppDomain.CurrentDomain.FriendlyName.Contains("ehExtHost");
            if (isMC)  //only want to startup the proxy if we are actually in MediaCenter (not configurator)
            {
                string cachePath = PluginOptions.Instance.CacheDir;
                if (string.IsNullOrEmpty(cachePath) || !System.IO.Directory.Exists(cachePath))
                {
                    cachePath = System.IO.Path.Combine(ApplicationPaths.AppConfigPath, "TrailerCache");
                    if (!Directory.Exists(cachePath))
                    {
                        Directory.CreateDirectory(cachePath);
                    }
                }

                int port = ProxyPort;
                proxy = new HttpProxy(cachePath, port);
                while (proxy.AlreadyRunning() && port < ProxyPort + 10)
                {
                    //try a different port if already running
                    port++;
                    proxy = new HttpProxy(cachePath, port);
                }
                if (port >= ProxyPort + 10)
                {
                    Logger.ReportError("MBTrailers failed to start proxy server.");
                    return;
                }
                proxy.Start();

                trailers.RefreshProxy();
            }
     
        }

        public override IPluginConfiguration PluginConfiguration {
            get {
                return PluginOptions;
            }
        }

        public override void Configure()
        {
            PluginOptions.Instance.Changed = true; //this will tell us we need to refresh on next start
            base.Configure();
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
