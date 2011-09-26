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
using MediaBrowser.Util;
using MediaBrowser.Library.Threading;
using Microsoft.MediaCenter.UI;
using MediaBrowser.Library.Providers;

namespace MediaBrowser.Library {

    [Flags]
    public enum KernelLoadDirective { 
        None,

        /// <summary>
        /// Ensure plugin dlls are not locked 
        /// </summary>
        ShadowPlugins, 

        LoadServicePlugins
    }

    [Flags]
    public enum MBLoadContext
    {
        None = 0x0,
        Service = 0x1,
        Core = 0x2,
        Other = 0x4,
        Configurator = 0x8,
        All = Service | Core | Other | Configurator
    }

    /// <summary>
    /// This is the one class that contains all the dependencies. 
    /// </summary>
    public class Kernel {

        /**** Version extension is used to provide for specific versions between current releases without having to actually change the 
         * actual assembly version number.  Suggested Values:
         * "R" Released major version
         * "R+" Trunk build (not released as a build to anyone but modified since last true release)
         * "SP1", "SP2", "SPn" Service release without major version change
         * "SPn+" Trunk build after a service release
         * "A1", "A2", "An" Alpha versions
         * "B1", "B2", "Bn" Beta versions
         * 
         * This should be set to "R" (or "SPn") with each official release and then immediately changed back to "R+" (or "SPn+")
         * so future trunk builds will indicate properly.
         * */
        private const string versionExtension = "R";

        public const string MBSERVICE_MUTEX_ID = "Global\\{E155D5F4-0DDA-47bb-9392-D407018D24B1}";
        public const string MBCLIENT_MUTEX_ID = "Global\\{9F043CB3-EC8E-41bf-9579-81D5F6E641B9}";

        static object sync = new object();
        static Kernel kernel;

        //public static bool UseNewSQLRepo = false;

        public bool MajorActivity
        {
            get
            {
                if (Application.CurrentInstance != null)
                    return Application.CurrentInstance.Information.MajorActivity;
                else
                    return false;
            }
            set
            {
                if (Application.CurrentInstance != null)
                if (Application.CurrentInstance.Information.MajorActivity != value)
                    Application.CurrentInstance.Information.MajorActivity = value;
            }
        }

        private static MultiLogger GetDefaultLogger(ConfigData config) {
            var logger = new MultiLogger(config.MinLoggingSeverity);

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
            ConfigData config = null;

            config = ConfigData.FromFile(ApplicationPaths.ConfigFile);
           
            Init(directives, config);
        } 

        public static void Init(KernelLoadDirective directives, ConfigData config) {
            lock (sync) {

                // we must set up some paths as well as a side effect (should be refactored) 
                if (!string.IsNullOrEmpty(config.UserSettingsPath) && Directory.Exists(config.UserSettingsPath)) {
                    ApplicationPaths.SetUserSettingsPath(config.UserSettingsPath.Trim());
                }

                // Its critical to have the logger initialized early so initialization 
                //   routines can use the right logger.
                if (Logger.LoggerInstance != null) {
                    Logger.LoggerInstance.Dispose();
                }
                    
                Logger.LoggerInstance = GetDefaultLogger(config);
                
                var kernel = GetDefaultKernel(config, directives);
                Kernel.Instance = kernel;

                // setup IBN if not there
                string ibnLocation = Config.Instance.ImageByNameLocation;
                if (string.IsNullOrEmpty(ibnLocation))
                    ibnLocation = Path.Combine(ApplicationPaths.AppConfigPath, "ImagesByName");
                if (!Directory.Exists(ibnLocation))
                {
                    try
                    {
                        Directory.CreateDirectory(ibnLocation);
                        Directory.CreateDirectory(Path.Combine(ibnLocation, "Genre"));
                        Directory.CreateDirectory(Path.Combine(ibnLocation, "People"));
                        Directory.CreateDirectory(Path.Combine(ibnLocation, "Studio"));
                        Directory.CreateDirectory(Path.Combine(ibnLocation, "Year"));
                        Directory.CreateDirectory(Path.Combine(ibnLocation, "General"));
                        Directory.CreateDirectory(Path.Combine(ibnLocation, "MediaInfo"));
                    }
                    catch (Exception e)
                    {
                        Logger.ReportException("Unable to create IBN location.", e);
                    }
                }
                
                if (LoadContext == MBLoadContext.Core || LoadContext == MBLoadContext.Configurator)
                {
                    Async.Queue("Start Service", () =>
                    {
                        //start our service if its not already going
                        if (!MBServiceController.IsRunning)
                        {
                            Logger.ReportInfo("Starting MB Service...");
                            MBServiceController.StartService();
                        }
                    });
                }
                if (LoadContext == MBLoadContext.Core)
                {
                    //listen for commands 
                    if (!MBClientConnector.StartListening())
                    { 
                        //we couldn't start our listener - probably another instance going so we shut down
                        Logger.ReportInfo("Could not start listener - assuming another instance of MB.  Closing...");
                        Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();
                        return;
                    }
                    MBServiceController.ConnectToService(); //set up for service to tell us to do things
                }

                // create filewatchers for each of our top-level folders (only if we are in MediaCenter, though)
                bool isMC = AppDomain.CurrentDomain.FriendlyName.Contains("ehExtHost");
                if (isMC && config.EnableDirectoryWatchers) //only do this inside of MediaCenter as we don't want to be trying to refresh things if MB isn't actually running
                {
                    Async.Queue("Create Filewatchers", () =>
                    {
                        foreach (BaseItem item in kernel.RootFolder.Children)
                        {
                            Folder folder = item as Folder;
                            if (folder != null)
                            {
                                folder.directoryWatcher = new MBDirectoryWatcher(folder, false);
                            }
                        }

                        // create a watcher for the startup folder too - and watch all changes there
                        kernel.RootFolder.directoryWatcher = new MBDirectoryWatcher(kernel.RootFolder, true);
                    });
                }


                // add the podcast home
                var podcastHome = kernel.GetItem<Folder>(kernel.ConfigData.PodcastHome);
                if (podcastHome != null && podcastHome.Children.Count > 0) {
                    kernel.RootFolder.AddVirtualChild(podcastHome);
                }
            }
        }

        private static void DisposeKernel(Kernel kernel)
        {
            if (kernel.PlaybackControllers != null)
            {
                foreach (var playbackController in kernel.PlaybackControllers)
                {
                    var disposable = playbackController as IDisposable;
                    if (disposable != null)
                    {
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
                        config.EnableNestedMovieFolders, 
                        config.EnableLocalTrailerSupport), 
                new FolderResolver(),
            };
        }

        private static List<ImageResolver> DefaultImageResolvers(bool enableProxyLikeCaching) {
            return new List<ImageResolver>() {
                (path, canBeProcessed, item) =>  { 
                    if (path != null && path.ToLower().StartsWith("http")) {
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

        private static bool? _isVista;
        public static bool isVista
        {
            get
            {
                if (_isVista == null)
                {
                    System.Version ver = System.Environment.OSVersion.Version;
                    _isVista = ver.Major == 6 && ver.Minor == 0;
                }
                return _isVista.Value;
            }
        }

        static MBLoadContext? _loadContext;
        public static MBLoadContext LoadContext 
        {
            get 
            {
                if (_loadContext == null)
                {
                    string assemblyName = AppDomain.CurrentDomain.FriendlyName.ToLower();
                    if (assemblyName.Contains("mediabrowserservice"))
                        _loadContext = MBLoadContext.Service;
                    else
                        if (assemblyName.Contains("ehexthost"))
                            _loadContext = MBLoadContext.Core;
                        else
                            if (assemblyName.Contains("configurator"))
                                _loadContext = MBLoadContext.Configurator;
                            else
                                _loadContext = MBLoadContext.Other;
                }
                return _loadContext.Value;
            }
        }

        static IItemRepository GetRepository(ConfigData config)
        {
            IItemRepository repository = null;
            if (kernel != null && kernel.ItemRepository != null) kernel.ItemRepository.ShutdownDatabase(); //we need to do this for SQLite
            string sqliteDb = Path.Combine(ApplicationPaths.AppCachePath, "cache.db");
            string sqliteDll = Path.Combine(ApplicationPaths.AppConfigPath, "system.data.sqlite.dll");
            if (File.Exists(sqliteDll))
            {
                try
                {
                    repository = new SafeItemRepository(
                        new MemoizingRepository(
                            SqliteItemRepository.GetRepository(sqliteDb, sqliteDll)
                        )
                     );
                }
                catch (Exception e)
                {
                    Logger.ReportException("Failed to init sqlite!", e);
                    repository = null;
                }
            }

            return repository;
        }

        static Kernel GetDefaultKernel(ConfigData config, KernelLoadDirective loadDirective) {

            IItemRepository repository = GetRepository(config);

            var kernel = new Kernel()
            {
             PlaybackControllers = new List<IPlaybackController>(),
             MetadataProviderFactories = MetadataProviderHelper.DefaultProviders(),
             ConfigData = config,
             ServiceConfigData = ServiceConfigData.FromFile(ApplicationPaths.ServiceConfigFile),
             StringData = LocalizedStrings.Instance,
             ImageResolvers = DefaultImageResolvers(config.EnableProxyLikeCaching),
             ItemRepository = repository,
             MediaLocationFactory = new MediaBrowser.Library.Factories.MediaLocationFactory(),
             TrailerProviders = new List<ITrailerProvider>() { new LocalTrailerProvider()}
             };

            //Kernel.UseNewSQLRepo = config.UseNewSQLRepo;

            // kernel.StringData.Save(); //save this in case we made mods (no other routine saves this data)
            kernel.PlaybackControllers.Add(new PlaybackController());
       

            // set up assembly resolution hooks, so earlier versions of the plugins resolve properly 
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);

            kernel.EntityResolver = DefaultResolver(kernel.ConfigData);

            // we need to enforce that the root folder is an aggregate folder
            var root = kernel.GetLocation(ResolveInitialFolder(kernel.ConfigData.InitialFolder));
            kernel.RootFolder = (AggregateFolder)BaseItemFactory<AggregateFolder>.Instance.CreateInstance(root, null);

            // our root folder needs metadata
            kernel.RootFolder = kernel.ItemRepository.RetrieveItem(kernel.RootFolder.Id) as AggregateFolder ??  kernel.RootFolder;

            //create our default config panels with localized names
            kernel.AddConfigPanel(kernel.StringData.GetString("GeneralConfig"), "");
            kernel.AddConfigPanel(kernel.StringData.GetString("MediaOptionsConfig"), "");
            kernel.AddConfigPanel(kernel.StringData.GetString("ThemesConfig"), "");
            kernel.AddConfigPanel(kernel.StringData.GetString("ParentalControlConfig"), "");

            using (new Profiler("Plugin Loading and Init"))
            {
                kernel.Plugins = DefaultPlugins((loadDirective & KernelLoadDirective.ShadowPlugins) == KernelLoadDirective.ShadowPlugins);

                // initialize our plugins (maybe we should add a kernel.init ? )
                // The ToList enables us to remove stuff from the list if there is a failure
                foreach (var plugin in kernel.Plugins.ToList())
                {
                    try
                    {
                        //Logger.ReportInfo("LoadContext is: " + LoadContext + " " + plugin.Name + " Initdirective is: " + plugin.InitDirective);
                        if ((LoadContext & plugin.InitDirective) > 0)
                        {
                            plugin.Init(kernel);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.ReportException("Failed to initialize Plugin : " + plugin.Name, e);
                        kernel.Plugins.Remove(plugin);
                    }
                }
            }
            return kernel;
        }

        public void ReLoadRoot()
        {
            //save the items added by plugins before we re-load
            List<BaseItem> virtualItems = new List<BaseItem>();
            virtualItems.AddRange(kernel.RootFolder.VirtualChildren);

            var root = kernel.GetLocation(ResolveInitialFolder(kernel.ConfigData.InitialFolder));
            kernel.RootFolder = (AggregateFolder)BaseItemFactory<AggregateFolder>.Instance.CreateInstance(root, null);

            // our root folder needs metadata
            kernel.RootFolder = kernel.ItemRepository.RetrieveItem(kernel.RootFolder.Id) as AggregateFolder ??
                kernel.RootFolder;

            //now add back the plug-in children
            if (virtualItems != null)
            {
                foreach (var item in virtualItems)
                {
                    Logger.ReportVerbose("Adding back " + item.Name);
                    kernel.RootFolder.AddVirtualChild(item);
                }
            }

            //and re-load the repo
            ItemRepository = GetRepository(this.ConfigData);
        }

        public void ReLoadConfig()
        {
            Logger.ReportVerbose("Reloading config file (probably due to change in other process).");
            this.ConfigData = ConfigData.FromFile(ApplicationPaths.ConfigFile);
            Config.Reload();
        }

        public void NotifyConfigChange()
        {
            switch (LoadContext)
            {
                case MBLoadContext.Core:
                case MBLoadContext.Other:
                case MBLoadContext.Configurator:
                    //tell the service to re-load the config
                    MBServiceController.SendCommandToService(IPCCommands.ReloadConfig);
                    break;
                case MBLoadContext.Service:
                    //tell the core to re-load the config
                    MBServiceController.SendCommandToCore(IPCCommands.ReloadConfig);
                    break;
            }
        }


        static System.Reflection.Assembly OnAssemblyResolve(object sender, ResolveEventArgs args) {
            if (args.Name.StartsWith("MediaBrowser,")) {
                Logger.ReportInfo("Plug-in reference to "+args.Name + " is being linked to version "+System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
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

        public System.Version Version
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public string VersionStr
        {
            get { return Version + " " + versionExtension; }
        }


        public List<ITrailerProvider> TrailerProviders{ get; set; }
        public AggregateFolder RootFolder { get; set; }
        public List<IPlugin> Plugins { get; set; }
        public List<IPlaybackController> PlaybackControllers { get; set; }
        public List<MetadataProviderFactory> MetadataProviderFactories { get; set; }
        public List<ImageResolver> ImageResolvers { get; set; }
        public ChainedEntityResolver EntityResolver { get; set; }
        public ConfigData ConfigData { get; set; }
        public ServiceConfigData ServiceConfigData { get; set; }
        public LocalizedStrings StringData { get; set; }
        public IItemRepository ItemRepository { get; set; }
        public IMediaLocationFactory MediaLocationFactory { get; set; }
        public delegate System.Drawing.Image ImageProcessorRoutine(System.Drawing.Image image, BaseItem item);
        public ImageProcessorRoutine ImageProcessor;


        IsMouseActiveHooker _mouseActiveHooker;
        public IsMouseActiveHooker MouseActiveHooker
        {
            get 
            {
                lock (this)
                {
                    if (_mouseActiveHooker == null)
                    {
                        _mouseActiveHooker = new IsMouseActiveHooker();
                    }
                    return _mouseActiveHooker;
                }
            }
        }
        
        
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
        public MBPropertySet LocalStrings
        {
            get
            {
                return StringData.LocalStrings;
            }
        }

        public IEnumerable<string> GetTrailers(Movie movie)
        {
            foreach (var trailerProvider in TrailerProviders)
            {
                var trailers = trailerProvider.GetTrailers(movie).ToList();
                if (trailers.Count > 0)
                {
                    return trailers;
                }
            }
            return new List<string>();
        }

        public string GetString(string name)
        {
            return this.StringData.GetString(name);
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
        public Dictionary<string, ConfigPanel> ConfigPanels = new Dictionary<string, ConfigPanel>();

        //method for external entities (plug-ins) to add a new config panels
        //panel should be a resx reference to a UI that fits within the config panel area and takes Application and FocusItem as parms
        public void AddConfigPanel(string name, string panel)
        {
            ConfigPanels.Add(name, new ConfigPanel(panel));
        }

        public void AddConfigPanel(string name, string panel, ModelItem configObject)
        {
            ConfigPanels.Add(name, new ConfigPanel(panel,configObject));
        }

        public Dictionary<string, ViewTheme> AvailableThemes = new Dictionary<string, ViewTheme>()
            {
                {"Default", new ViewTheme()}        
            };

        //method for external entities (plug-ins) to add a new theme - only support replacing detail areas for now...
        public void AddTheme(string name, string pageArea, string detailArea)
        {
            if (AvailableThemes.ContainsKey(name)) AvailableThemes.Remove(name); //clear it if previously was there
            AvailableThemes.Add(name, new ViewTheme(name, pageArea, detailArea));
        }

        public void AddTheme(string name, string pageArea, string detailArea, ModelItem config)
        {
            if (AvailableThemes.ContainsKey(name)) AvailableThemes.Remove(name); //clear it if previously was there
            AvailableThemes.Add(name, new ViewTheme(name, pageArea, detailArea, config));
        }

        //this list tells us which themes have their own icons in resources
        private List<string> themesWithIcons = new List<string>();
        public void AddInternalIconTheme(string theme)
        {
            if (!themesWithIcons.Contains(theme.ToLower())) themesWithIcons.Add(theme.ToLower());
        }
        public bool HasInternalIcons(string theme)
        {
            return themesWithIcons.Contains(theme.ToLower());
        }
        private List<MenuItem> menuOptions = new List<MenuItem>();
        private List<Type> externalPlayableItems = new List<Type>();
        private List<Type> externalPlayableFolders = new List<Type>();

        public string ScreenSaverUI = "";

        public List<Type> ExternalPlayableItems { get { return externalPlayableItems; } }
        public List<Type> ExternalPlayableFolders { get { return externalPlayableFolders; } }

        public List<MenuItem> ContextMenuItems { get { return menuOptions.FindAll(m => (m.Available && m.Supports(MenuType.Item))); } }
        public List<MenuItem> PlayMenuItems { get { return menuOptions.FindAll(m => (m.Available && m.Supports(MenuType.Play))); } }
        public List<MenuItem> DetailMenuItems { get { return menuOptions.FindAll(m => (m.Available && m.Supports(MenuType.Detail))); } }

        public MenuItem AddMenuItem(MenuItem menuItem) {
            menuOptions.Add(menuItem);       
            return menuItem;
        }

        public MenuItem AddMenuItem(MenuItem menuItem, int position)
        {
            Debug.Assert(position <= menuOptions.Count, "cowboy you are trying to insert a menu item in an invalid position!");
            if (position > menuOptions.Count) {
                Logger.ReportWarning("Attempting to insert a menu item in an invalid position, appending to the end instead " + menuItem.Text);
                menuOptions.Add(menuItem);
            } else {
                menuOptions.Insert(position, menuItem);
            }
            return menuItem;
        }

        public delegate bool PrePlayProcess(Item item, bool PlayIntros);
        public delegate void PostPlayProcess();
        public List<PrePlayProcess> PrePlayProcesses = new List<PrePlayProcess>();
        public List<PostPlayProcess> PostPlayProcesses = new List<PostPlayProcess>();

        public PrePlayProcess AddPrePlayProcess(PrePlayProcess process)
        {
            return AddPrePlayProcess(process, 2);
        }

        public PrePlayProcess AddPrePlayProcess(PrePlayProcess process, int priority)
        {
            if (priority == 0)
            {
                PrePlayProcesses.Insert(0, process);
            }
            else
            {
                PrePlayProcesses.Add(process);
            }
            return process;
        }

        public PostPlayProcess AddPostPlayProcess(PostPlayProcess process)
        {
            return AddPostPlayProcess(process, 2);
        }

        public PostPlayProcess AddPostPlayProcess(PostPlayProcess process, int priority)
        {
            if (priority == 0)
            {
                PostPlayProcesses.Insert(0, process);
            }
            else
            {
                PostPlayProcesses.Add(process);
            }
            return process;
        }

        public void AddExternalPlayableItem(Type aType)
        {
            externalPlayableItems.Add(aType);
        }

        public void AddExternalPlayableFolder(Type aType)
        {
            externalPlayableFolders.Add(aType);
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

        public LibraryImage GetImage(string path)
        {
            return GetImage(path, false, null);
        }

        public LibraryImage GetImage(string path,bool canBeProcessed, BaseItem item) {
            return LibraryImageFactory.Instance.GetImage(path, canBeProcessed, item);
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
            InstallPlugin(path, Path.GetFileName(path), globalTarget, null, null, null);
        }

        public void InstallPlugin(string path, bool globalTarget,
                MediaBrowser.Library.Network.WebDownload.PluginInstallUpdateCB updateCB,
                MediaBrowser.Library.Network.WebDownload.PluginInstallFinishCB doneCB,
                MediaBrowser.Library.Network.WebDownload.PluginInstallErrorCB errorCB)
        {
            InstallPlugin(path, Path.GetFileName(path), globalTarget, updateCB, doneCB, errorCB);
        }

        public void InstallPlugin(string sourcePath, string targetName, bool globalTarget,
                MediaBrowser.Library.Network.WebDownload.PluginInstallUpdateCB updateCB,
                MediaBrowser.Library.Network.WebDownload.PluginInstallFinishCB doneCB,
                MediaBrowser.Library.Network.WebDownload.PluginInstallErrorCB errorCB) {
            string target;
            if (globalTarget) {
                //install to ehome for now - can change this to GAC if figure out how...
                target = Path.Combine(System.Environment.GetEnvironmentVariable("windir"), Path.Combine("ehome", targetName));
                //and put our pointer file in "plugins"
                File.Create(Path.Combine(ApplicationPaths.AppPluginPath, Path.ChangeExtension(Path.GetFileName(targetName), ".pgn")));
            }
            else {
                target = Path.Combine(ApplicationPaths.AppPluginPath, targetName);
            }

            if (sourcePath.ToLower().StartsWith("http")) {
                // Initialise Async Web Request
                int BUFFER_SIZE = 1024;
                Uri fileURI = new Uri(sourcePath);

                WebRequest request = WebRequest.Create(fileURI);
                Network.WebDownload.State requestState = new Network.WebDownload.State(BUFFER_SIZE, target);
                requestState.request = request;
                requestState.fileURI = fileURI;
                requestState.progCB = updateCB;
                requestState.doneCB = doneCB;
                requestState.errorCB = errorCB;

                IAsyncResult result = (IAsyncResult)request.BeginGetResponse(new AsyncCallback(ResponseCallback), requestState);
            }
            else {
                File.Copy(sourcePath, target, true);
                InitialisePlugin(target);
            }

            // Moved code to InitialisePlugin()
            //Function needs to be called at end of Async dl process as well
        }

        private void InitialisePlugin(string target) {
            var plugin = Plugin.FromFile(target, true);

            try {
                plugin.Init(this);
            } catch (InvalidCastException e) { 
                // this happens if the assembly with the exact same version is loaded 
                // AND the Init process tries to use types defined in its assembly 
                throw new PluginAlreadyLoadedException("Failed to init plugin as its already loaded", e);
            }
            IPlugin pi = Plugins.Find(p => p.Filename == plugin.Filename);
            if (pi != null) Plugins.Remove(pi); //we were updating
            Plugins.Add(plugin);
           

        }

        /// <summary>
        /// Main response callback, invoked once we have first Response packet from
        /// server.  This is where we initiate the actual file transfer, reading from
        /// a stream.
        /// </summary>
        /// <param name="asyncResult"></param>
        private void ResponseCallback(IAsyncResult asyncResult) {
            Network.WebDownload.State requestState = ((Network.WebDownload.State)(asyncResult.AsyncState));

            try {
                WebRequest req = requestState.request;

                // HTTP 
                if (requestState.fileURI.Scheme == Uri.UriSchemeHttp) {
                    HttpWebResponse resp = ((HttpWebResponse)(req.EndGetResponse(asyncResult)));
                    requestState.response = resp;
                    requestState.totalBytes = requestState.response.ContentLength;
                }
                else {
                    throw new ApplicationException("Unexpected URI");
                }

                // Set up a stream, for reading response data into it
                Stream responseStream = requestState.response.GetResponseStream();
                requestState.streamResponse = responseStream;

                // Begin reading contents of the response data
                IAsyncResult ar = responseStream.BeginRead(requestState.bufferRead, 0, requestState.bufferRead.Length, new AsyncCallback(ReadCallback), requestState);

                return;
            }
            catch (WebException ex) {
                //Callback to GUI to report an error has occured.
                if (requestState.errorCB != null) {
                    requestState.errorCB(ex);
                }
            }
        }

        /// <summary>
        /// Main callback invoked in response to the Stream.BeginRead method, when we have some data.
        /// </summary>
        private void ReadCallback(IAsyncResult asyncResult) {
            Network.WebDownload.State requestState = ((Network.WebDownload.State)(asyncResult.AsyncState));

            try {
                Stream responseStream = requestState.streamResponse;

                // Get results of read operation
                int bytesRead = responseStream.EndRead(asyncResult);

                // Got some data, need to read more
                if (bytesRead > 0) {
                    // Save Data
                    requestState.downloadDest.Write(requestState.bufferRead, 0, bytesRead);

                    // Report some progress, including total # bytes read, % complete, and transfer rate
                    requestState.bytesRead += bytesRead;
                    double percentComplete = ((double)requestState.bytesRead / (double)requestState.totalBytes) * 100.0f;

                    //Callback to GUI to update progress
                    if (requestState.progCB != null) {
                        requestState.progCB(percentComplete);
                    }

                    // Kick off another read
                    IAsyncResult ar = responseStream.BeginRead(requestState.bufferRead, 0, requestState.bufferRead.Length, new AsyncCallback(ReadCallback), requestState);
                    return;
                }

                // EndRead returned 0, so no more data to be read
                else {
                    responseStream.Close();
                    requestState.response.Close();
                    requestState.downloadDest.Flush();
                    requestState.downloadDest.Close();

                    // Initialise the Plugin
                    InitialisePlugin(requestState.downloadDest.Name);

                    //Callback to GUI to report download has completed
                    if (requestState.doneCB != null) {
                        requestState.doneCB();
                    }
                }
            } 
            catch (PluginAlreadyLoadedException) {
                Logger.ReportWarning("Attempting to install a plugin that is already loaded: " + requestState.fileURI);
            } 
            catch (WebException ex) {
                //Callback to GUI to report an error has occured.
                if (requestState.errorCB != null) {
                    requestState.errorCB(ex);
                }
            }
        }

    }

    [global::System.Serializable]
    public class PluginAlreadyLoadedException : Exception {
        public PluginAlreadyLoadedException() { }
        public PluginAlreadyLoadedException(string message) : base(message) { }
        public PluginAlreadyLoadedException(string message, Exception inner) : base(message, inner) { }
        protected PluginAlreadyLoadedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
