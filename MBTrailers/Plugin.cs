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
        internal const string PluginDescription = "HD Trailers for MediaBrowser (Titan SP1). Now includes cache management and backdrop fetching.\n\nUnrestricted version is available to supporters.";

        internal const int ProxyPort = 8752;

        public static HttpProxy proxy;

        static readonly Guid TrailersGuid = new Guid("{828DCFEF-AEAF-44f2-B6A8-32AEAF27F3DA}");
        public static PluginConfiguration<PluginOptions>  PluginOptions {get;set;}
        static readonly Guid MyTrailersGuid = new Guid("d2a2ff50-8d51-4fab-824a-cb9317e9212c");
        private LocalTrailerFolder localTrailers;
        
        public override void Init(Kernel kernel) {
            PluginOptions = new PluginConfiguration<PluginOptions>(kernel, this.GetType().Assembly);
            PluginOptions.Load();
            
            var trailers = (kernel.ItemRepository.RetrieveItem(TrailersGuid) as MBTrailerFolder) ?? new MBTrailerFolder();
            trailers.Path = "";
            trailers.Id = TrailersGuid;
            //validate sort value and fill in
            //int sort = 0;
            //int.TryParse(PluginOptions.Instance.SortOrder, out sort);
            //if (sort > 0) trailers.SortName = sort.ToString("000");
            //Logger.ReportInfo("MBTrailers Sort is: " + trailers.SortName);

            kernel.RootFolder.AddVirtualChild(trailers);

            if (Kernel.LoadContext == MBLoadContext.Service || Kernel.LoadContext == MBLoadContext.Core)  //create proxy in core and service (will only listen in service)
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

                long maxBandwidth = 1000 * 1024;
                long.TryParse(PluginOptions.Instance.MaxBandWidth, out maxBandwidth);
                if (maxBandwidth < 0)
                    maxBandwidth = 1000 * 1024;
                else 
                    maxBandwidth = maxBandwidth * 1024L;

                proxy = new HttpProxy(cachePath, ProxyPort);
                if (Kernel.LoadContext == MBLoadContext.Service)
                { //only actually start the proxy in the service
                    Logger.ReportInfo("MBTrailers starting proxy server on port: " + ProxyPort);
                    proxy.Start(PluginOptions.Instance.AutoDownload, maxBandwidth);
                    //and clean up the cache if requested
                    if (PluginOptions.Instance.AutoClearCache)
                    {
                        MediaBrowser.Library.Threading.Async.Queue("MBTrailers cache clear", () =>
                        {
                             while (true)
                             {
                                 trailers.CleanCache();
                                 System.Threading.Thread.Sleep(24 * 60 * 60000); //once per day
                             }

                        });
                    }
                }
                else
                {
                    if (proxy.AlreadyRunning())
                        Logger.ReportInfo("MBTrailers not starting proxy.  Running in service.");
                    else
                        Logger.ReportInfo("MBTrailers - no proxy running.  Start Media Browser Service.");
                }
                //load mytrailers option if specified
                if (PluginOptions.Instance.ShowMyTrailers)
                {
                    localTrailers = kernel.ItemRepository.RetrieveItem(MyTrailersGuid) as LocalTrailerFolder ?? new LocalTrailerFolder();
                    localTrailers.Id = MyTrailersGuid;
                    kernel.RootFolder.AddVirtualChild(localTrailers);
                    kernel.ItemRepository.SaveItem(localTrailers);

                    //make sure our image cache is there
                    if (!Directory.Exists(Util.ImageCache))
                        try
                        {
                            Directory.CreateDirectory(Util.ImageCache);
                        }
                        catch (Exception e)
                        {
                            Logger.ReportException("Error Creating MyTrailers Image Cache: " + Util.ImageCache, e);
                        }

                    //Tell the log we loaded.
                    Logger.ReportInfo("My Trailers Support Loaded.");
                }

                //refresh the paths of trailers, but wait a bit in case the service side isn't up yet (we load before the service)
                MediaBrowser.Library.Threading.Async.Queue("MB Trailers refresh",() => trailers.RefreshProxy(), 3000);

                //tell core our types are playable (for menus)
                kernel.AddExternalPlayableItem(typeof(ITunesTrailer));
                kernel.AddExternalPlayableFolder(typeof(MBTrailerFolder));
            }
            Logger.ReportInfo("MBTrailers (version "+Version+") Plug-in loaded.");
     
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

        public override Version TestedMBVersion
        {
            get
            {
                return new Version("2.3.2.0");
            }
        }

        public override Version RequiredMBVersion
        {
            get
            {
                return new Version("2.3.2.0");
            }
        }

    }
}
