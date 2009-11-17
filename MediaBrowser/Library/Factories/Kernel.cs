using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.ImageManagement;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Library.RemoteControl;
using MediaBrowser.Library.Metadata;
using MediaBrowser.Library.Interfaces;
using MediaBrowser.Library.Configuration;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.UI;
using MediaBrowser.Library.Localization;
using MediaBrowser.Library.Input;
using System.IO;
using System.Diagnostics;
using System.Net;

namespace MediaBrowser.Library {

    [Flags]
    public enum KernelLoadDirective { 
        None,

        /// <summary>
        /// Ensure plugin dlls are not locked 
        /// </summary>
        ShadowPlugins
    } 

    /// <summary>
    /// This is the one class that contains all the dependencies. 
    /// </summary>
    public class Kernel {

        static object sync = new object();
        static Kernel kernel;

        private static MultiLogger GetDefaultLogger(ConfigData config) {
            var logger = new MultiLogger();

            if (config.EnableTraceLogging) {
                logger.AddLogger(new FileLogger(ApplicationPaths.AppLogPath));
#if (!DEBUG)
                logger.AddLogger(new TraceLogger());
#endif
            }
#if DEBUG
            logger.AddLogger(new TraceLogger());
#endif
            return logger;
        }

        public static void Init() {
            Init(KernelLoadDirective.None);
        }

        public static void Init(ConfigData config) {
           Init(KernelLoadDirective.None, config);
        }

        public static void Init(KernelLoadDirective directives) { 
            Init(directives, ConfigData.FromFile(ApplicationPaths.ConfigFile));
        } 

        public static void Init(KernelLoadDirective directives, ConfigData config) {
            lock (sync) {

                // we must set up some paths as well as a side effect (should be refactored) 
                if (!string.IsNullOrEmpty(config.UserSettingsPath) && Directory.Exists(config.UserSettingsPath)) {
                    ApplicationPaths.SetUserSettingsPath(config.UserSettingsPath);
                }
                
                // Its critical to have the logger initialized early so initialization 
                //   routines can use the right logger.
                if (Logger.LoggerInstance != null) {
                    Logger.LoggerInstance.Dispose();
                }
                    
                Logger.LoggerInstance = GetDefaultLogger(config);
                
                var kernel = GetDefaultKernel(config, directives);
                Kernel.Instance = kernel;

                // add the podcast home
                var podcastHome = kernel.GetItem<Folder>(kernel.ConfigData.PodcastHome);
                if (podcastHome != null && podcastHome.Children.Count > 0) {
                    kernel.RootFolder.AddVirtualChild(podcastHome);
                }
            }
        } 

        private static void DisposeKernel(Kernel kernel){
            if (kernel.PlaybackControllers != null) {
                foreach (var playbackController in kernel.PlaybackControllers) {
                    var disposable = playbackController as IDisposable;
                    if (disposable != null) {
                        disposable.Dispose();
                    }
                }
            }
        }

        private static string ResolveInitialFolder(string start) {
            if (start == Helper.MY_VIDEOS)
                start = Helper.MyVideosPath;
            return start;
        }

        private static ChainedEntityResolver DefaultResolver(ConfigData config) {
            return
                new ChainedEntityResolver() { 
                new VodCastResolver(),
                new EpisodeResolver(), 
                new SeasonResolver(), 
                new SeriesResolver(), 
                new MovieResolver(
                        config.EnableMoviePlaylists?config.PlaylistLimit:1, 
                        config.EnableNestedMovieFolders), 
                new FolderResolver(),
            };
        }

        private static List<ImageResolver> DefaultImageResolvers(bool enableProxyLikeCaching) {
            return new List<ImageResolver>() {
                (path) =>  { 
                    if (path != null && path.ToLower().StartsWith("http")) {
                        if (enableProxyLikeCaching)
                            return new ProxyCachedRemoteImage();
                        else
                            return new RemoteImage();
                    }
                    return null;
                }
            };
        }

        static List<IPlugin> DefaultPlugins(bool forceShadow) {
            List<IPlugin> plugins = new List<IPlugin>();
            foreach (var file in Directory.GetFiles(ApplicationPaths.AppPluginPath)) {
                if (file.ToLower().EndsWith(".dll")) {
                    try {
                        plugins.Add(new Plugin(Path.Combine(ApplicationPaths.AppPluginPath, file),forceShadow));
                    } catch (Exception ex) {
                        Debug.Assert(false, "Failed to load plugin: " + ex.ToString());
                        Logger.ReportException("Failed to load plugin", ex);
                    }
                } else
                    //look for pointer plugin files - load these from ehome or GAC
                    if (file.ToLower().EndsWith(".pgn"))
                    {
                        try
                        {
                            plugins.Add(new Plugin(Path.ChangeExtension(Path.GetFileName(file), ".dll"), forceShadow));
                        }
                        catch (FileNotFoundException)
                        {
                            //couldn't find it in our home directory or GAC (may be called from another process)
                            //try windows ehome directory
                            try
                            {
                                plugins.Add(new Plugin(Path.Combine(Path.Combine(Environment.GetEnvironmentVariable("windir"), "ehome"), Path.ChangeExtension(Path.GetFileName(file), ".dll")), forceShadow));
                            }
                            catch (Exception ex)
                            {
                                Debug.Assert(false, "Failed to load plugin: " + ex.ToString());
                                Logger.ReportException("Failed to load plugin", ex);
                            }
                        }
                        catch (ArgumentException)
                        {
                            //couldn't find it in our home directory or GAC (may be called from another process)
                            //try windows ehome directory
                            try
                            {
                                plugins.Add(new Plugin(Path.Combine(Path.Combine(Environment.GetEnvironmentVariable("windir"), "ehome"), Path.ChangeExtension(Path.GetFileName(file), ".dll")), forceShadow));
                            }
                            catch (Exception ex)
                            {
                                Debug.Assert(false, "Failed to load plugin: " + ex.ToString());
                                Logger.ReportException("Failed to load plugin", ex);
                            }
                        }


                        catch (Exception ex)
                        {
                            Debug.Assert(false, "Failed to load plugin: " + ex.ToString());
                            Logger.ReportException("Failed to load plugin", ex);
                        }
                    }

            }
            return plugins;
        }

        static Kernel GetDefaultKernel(ConfigData config, KernelLoadDirective loadDirective) {

            var kernel = new Kernel()
            {
                PlaybackControllers = new List<IPlaybackController>(),
                MetadataProviderFactories = MetadataProviderHelper.DefaultProviders(),
                ConfigData = config,
                StringData = new LocalizedStrings(),
                ImageResolvers = DefaultImageResolvers(config.EnableProxyLikeCaching),                
                ItemRepository = new SafeItemRepository(new ItemRepository()),
                MediaLocationFactory = new MediaBrowser.Library.Factories.MediaLocationFactory()
            };

            kernel.StringData.Save(); //save this in case we made mods (no other routine saves this data)
            kernel.PlaybackControllers.Add(new PlaybackController());
       

            // set up assembly resolution hooks, so earlier versions of the plugins resolve properly 
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);

            kernel.EntityResolver = DefaultResolver(kernel.ConfigData);

            // we need to enforce that the root folder is an aggregate folder
            var root = kernel.GetLocation(ResolveInitialFolder(kernel.ConfigData.InitialFolder));
            kernel.RootFolder = (AggregateFolder)BaseItemFactory<AggregateFolder>.Instance.CreateInstance(root, null);

            // our root folder needs metadata
            kernel.RootFolder = kernel.ItemRepository.RetrieveItem(kernel.RootFolder.Id) as AggregateFolder ??
                kernel.RootFolder;
            // create a mouseActiveHooker for us to know if the mouse is active on our window (used to handle mouse scrolling control)
            // we will wire it to an event on application
            kernel.MouseActiveHooker = new IsMouseActiveHooker();

            kernel.Plugins = DefaultPlugins((loadDirective & KernelLoadDirective.ShadowPlugins) == KernelLoadDirective.ShadowPlugins);

            // initialize our plugins (maybe we should add a kernel.init ? )
            // The ToList enables us to remove stuff from the list if there is a failure
            foreach (var plugin in kernel.Plugins.ToList()) {
                try {
                    plugin.Init(kernel);
                } catch (Exception e) {
                    Logger.ReportException("Failed to initialize Plugin : " + plugin.Name, e);
                    kernel.Plugins.Remove(plugin);
                }
            }

            return kernel;

        }

        static System.Reflection.Assembly OnAssemblyResolve(object sender, ResolveEventArgs args) {
            Logger.ReportInfo(args.Name + " is being resolved!");
            if (args.Name.StartsWith("MediaBrowser,")) {
                return typeof(Kernel).Assembly;
            }
            return null;
        }

        public static Kernel Instance {
            get {
                if (kernel != null) return kernel;
                lock (sync) {
                    if (kernel != null) return kernel;
                    Init(); 
                }
                return kernel;
            }
            set {
                lock (sync) {
                    kernel = value;
                }
            }
        }


        public AggregateFolder RootFolder { get; set; }
        public List<IPlugin> Plugins { get; set; }
        public List<IPlaybackController> PlaybackControllers { get; set; }
        public List<MetadataProviderFactory> MetadataProviderFactories { get; set; }
        public List<ImageResolver> ImageResolvers { get; set; }
        public ChainedEntityResolver EntityResolver { get; set; }
        public ConfigData ConfigData { get; set; }
        public LocalizedStrings StringData { get; set; }
        public IItemRepository ItemRepository { get; set; }
        public IMediaLocationFactory MediaLocationFactory { get; set; }
        public IsMouseActiveHooker MouseActiveHooker;
        private ParentalControl parentalControls;
        public ParentalControl ParentalControls
        {
            get
            {
                if (this.parentalControls == null)
                    this.parentalControls = new ParentalControl();
                return this.parentalControls;
            }

        }

        public bool ParentalAllowed(Item item)
        {
            return this.ParentalControls.Allowed(item);
        }
        public bool ProtectedFolderAllowed(Folder folder)
        {
            return this.ParentalControls.ProtectedFolderEntered(folder);
        }

        public void ClearProtectedAllowedList()
        {

            this.ParentalControls.ClearEnteredList();
        }
        public Dictionary<string, string> ConfigPanels = new Dictionary<string, string>() {
            {"General",""},{"Media Options",""},{"Themes",""},{"ParentalControl",""} }; //defaults are embedded in configpage others will be added to end

        //method for external entities (plug-ins) to add a new config panels
        //panel should be a resx reference to a UI that fits within the config panel area and takes Application and FocusItem as parms
        public void AddConfigPanel(string name, string panel)
        {
            ConfigPanels.Add(name, panel);
        }

        public Dictionary<string, ViewTheme> AvailableThemes = new Dictionary<string, ViewTheme>()
            {
                {"Default", new ViewTheme()},
                {"Diamond", new ViewTheme("Diamond", "resx://MediaBrowser/MediaBrowser.Resources/PageDiamond#PageDiamond", "resx://MediaBrowser/MediaBrowser.Resources/DiamondMovieView#DiamondMovieView")},
                {"Vanilla", new ViewTheme("Vanilla", "resx://MediaBrowser/MediaBrowser.Resources/PageVanilla#Page", "resx://MediaBrowser/MediaBrowser.Resources/ViewMovieVanilla#ViewMovieVanilla")},
            };

        //method for external entities (plug-ins) to add a new theme - only support replacing detail areas for now...
        public void AddTheme(string name, string pageArea, string detailArea)
        {
            AvailableThemes.Add(name, new ViewTheme(name, pageArea, detailArea));
        }

        public T GetItem<T>(string path) where T : BaseItem
        {
            return GetItem<T>(GetLocation<IMediaLocation>(path));
        }

        public T GetItem<T>(IMediaLocation location) where T : BaseItem {
            BaseItem item = null;

            BaseItemFactory factory;
            IEnumerable<InitializationParameter> setup;

            EntityResolver.ResolveEntity(location, out factory, out setup);
            if (factory != null) {
                item = factory.CreateInstance(location, setup);
            }
            return item as T;
        }

        public BaseItem GetItem(IMediaLocation location) {
            return GetItem<BaseItem>(location);
        }

        public T GetLocation<T>(string path) where T : class, IMediaLocation {
            return MediaLocationFactory.Create(path) as T;
        }

        public IMediaLocation GetLocation(string path) {
            return GetLocation<IMediaLocation>(path);
        }

        public LibraryImage GetImage(string path) {
            return LibraryImageFactory.Instance.GetImage(path);
        }

        public void DeletePlugin(IPlugin plugin) {
            if (!(plugin is Plugin)) {
                Logger.ReportWarning("Attempting to remove a plugin that we have no location for!");
                throw new ApplicationException("Attempting to remove a plugin that we have no location for!");
            }

            (plugin as Plugin).Delete();
            Plugins.Remove(plugin);
        }

        public void InstallPlugin(string path)
        {
            //in case anyone is calling us with old interface
            InstallPlugin(path, false);
        }

        public void InstallPlugin(string path, bool globalTarget) {
            string target;
            if (globalTarget)
            {
                //install to ehome for now - can change this to GAC if figure out how...
                target = Path.Combine(System.Environment.GetEnvironmentVariable("windir"), Path.Combine("ehome", Path.GetFileName(path)));
                //and put our pointer file in "plugins"
                File.Create(Path.Combine(ApplicationPaths.AppPluginPath, Path.ChangeExtension(Path.GetFileName(path),".pgn")));
            }
            else
            {
                target = Path.Combine(ApplicationPaths.AppPluginPath, Path.GetFileName(path));
            }

            if (path.ToLower().StartsWith("http")) {
                WebRequest request = WebRequest.Create(path);
                using (var response = request.GetResponse()) {
                    using (var stream = response.GetResponseStream()) {
                        File.WriteAllBytes(target, stream.ReadAllBytes());
                    }
                }
            } else {
                File.Copy(path, target);
            }

            var plugin = Plugin.FromFile(target, true);
            plugin.Init(this);
            IPlugin pi = Plugins.Find(p => p.Filename == plugin.Filename);
            if (pi != null) Plugins.Remove(pi); //we were updating
            Plugins.Add(plugin);
        }
    }
}
