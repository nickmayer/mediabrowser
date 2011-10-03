using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Configuration;
using System.Reflection;
using System.Xml;

using MediaBrowser.Code.ShadowTypes;
using MediaBrowser.Library;
using MediaBrowser.LibraryManagement;
using System.Xml.Serialization;
using MediaBrowser.Library.Playables;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using MediaBrowser.Code;

namespace MediaBrowser
{
    [Serializable]
    public class ConfigData
    {
        //moved keyfile re-routing to here so can be accessed from service (outside MC) -ebr
        [SkipField]
        private KeyFile _keyFile;
        private KeyFile keyFile //only want to create this file if we try to access it
        {
            get
            {
                if (_keyFile == null)
                {
                    _keyFile = new KeyFile(Path.Combine(ApplicationPaths.AppConfigPath, "MB.lic"));
                }
                return _keyFile;
            }
        }

        //this is re-routed to a separate file
        [SkipField]
        public string SupporterKey
        {
            get { return this.keyFile.SupporterKey; }
            set { if (this.keyFile.SupporterKey != value) { this.keyFile.SupporterKey = value; this.keyFile.Save(); } }
        }

        [Comment(@"The version is used to determine if this is the first time a particular version has been run")]
        public string MBVersion = "1.0.0.0"; //default value will tell us if it is a brand new install
        [Comment(@"By default we track a videos position to support resume, this can be disabled by setting this for diagnostic purposes")]
        public bool EnableResumeSupport = true; 
        [Comment(@"Any folder named trailers will be ignored and treated a folder containing trailers")]
        public bool EnableLocalTrailerSupport = true; 
        [Comment(@"If you enable this, make sure System.Data.SQLite.DLL is copied to c:\program data\mediabrowser, make sure you install the right version there is a x32 and x64")]
        public bool EnableExperimentalSqliteSupport = false;
        [Comment(@"If you enable this MB will watch for changes in your file system and update the UI as it happens, may not work properly with SMB shares")]
        public bool EnableDirectoryWatchers = true;

        [Comment(@"If set to true when sorting by unwatched the unwatched folders will be sorted by name")]
        public bool SortUnwatchedByName = false;

        [Comment("Show now playing for default mode as text")]
        public bool ShowNowPlayingInText = false;

        [Comment("The date auto update last checked for a new version")]
        public DateTime LastAutoUpdateCheck = DateTime.Today.AddYears(-1);

        public bool AlwaysShowDetailsPage = true;
        public bool EnableVistaStopPlayStopHack = true;
        public bool EnableRootPage = true;
        public bool IsFirstRun = true;
        public string ImageByNameLocation = Path.Combine(ApplicationPaths.AppConfigPath, "ImagesByName");
        public Vector3 OverScanScaling = new Vector3() {X=1, Y=1, Z=1};
        public Inset OverScanPadding = new Inset();
        public bool EnableTraceLogging = true;
        public Size DefaultPosterSize = new Size() {Width=220, Height=330};
        public Size GridSpacing = new Size();
        public float MaximumAspectRatioDistortion = 0.2F;
        public bool EnableTranscode360 = false;
        public string ExtenderNativeTypes = ".dvr-ms,.wmv";
        public bool ShowThemeBackground = true;
        public bool DimUnselectedPosters = true;
        public bool EnableNestedMovieFolders = true;
        public bool EnableMoviePlaylists = true;
        public int PlaylistLimit = 2;
        public string InitialFolder = ApplicationPaths.AppInitialDirPath;
        public bool EnableUpdates = true;
        public bool EnableBetas = false;
        public string DaemonToolsLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"DAEMON Tools Lite\\daemon.exe");
        public string DaemonToolsDrive = "E";
        public bool EnableAlphanumericSorting = true;
        public bool EnableListViewTicks = false;
        public Colors ListViewWatchedColor = Colors.LightSkyBlue;
        public bool EnableListViewWatchedColor = true;
        public bool ShowUnwatchedCount = true;
        public bool ShowWatchedTickOnFolders = true;
        public bool ShowWatchTickInPosterView = true;
        public bool DefaultToFirstUnwatched = false;
        public bool AutoEnterSingleDirs = false; 
        public DateTime AssumeWatchedBefore = DateTime.Today.AddYears(-1);
        public bool InheritDefaultView = true;
        public string DefaultViewType = ViewType.Poster.ToString();
        public bool DefaultShowLabels = false;
        public bool DefaultVerticalScroll = false;
        public int BreadcrumbCountLimit = 2;
        public string SortRemoveCharacters = ",|&|-|{|}";
        public string SortReplaceCharacters = ".|+|%";
        public string SortReplaceWords = "the|a|an";
        public bool AllowInternetMetadataProviders = true;
        public bool EnableFileWatching = false;
        public int ThumbStripPosterWidth = 550;
        public bool RememberIndexing = false;
        public bool ShowIndexWarning = true;
        public double IndexWarningThreshold = 0.1;
        public string PreferredMetaDataLanguage = "en";
        public List<ExternalPlayer> ExternalPlayers = new List<ExternalPlayer>();
        public string Theme = "Default";
        public string FontTheme = "Default";
        // I love the clock, but it keeps on crashing the app, so disabling it for now
        public bool ShowClock = false;
        public bool EnableAdvancedCmds = false;
        public bool Advanced_EnableDelete = false;
        public bool UseAutoPlayForIso = false;
        public bool ShowBackdrop = true;
        public string InitialBreadcrumbName = "Media";

        public string UserSettingsPath = null;
        public string ViewTheme = "Default";
        public int AlphaBlending = 80;
        public bool ShowConfigButton = false;

        public bool EnableSyncViews = true;
        public string YahooWeatherFeed = "UKXX0085";
        public string YahooWeatherUnit = "c";
        public bool ShowRootBackground = true;

        public string PodcastHome = ApplicationPaths.DefaultPodcastPath;
        public bool HideFocusFrame = false;

        public bool EnableProxyLikeCaching = false;
        public int MetadataCheckForUpdateAge = 14;

        public int ParentalUnlockPeriod = 3;
        public bool HideParentalDisAllowed = true; 
        public bool ParentalBlockUnrated = false;
        public bool UnlockOnPinEntry = true;
        public bool ParentalControlEnabled = false;
        public string ParentalPIN = "0000";
        public int MaxParentalLevel = 3;

        public bool EnableMouseHook = false;

        public int RecentItemCount = 20;
        public int RecentItemDays = 60;
        public string RecentItemOption = "added";
        public int RecentItemCollapseThresh = 2;

        public bool ShowHDIndicatorOnPosters = false;
        public bool ShowRemoteIndicatorOnPosters = true;
        public bool ExcludeRemoteContentInSearch = true;

        public bool ShowUnwatchedIndicator = false;
        public bool PNGTakesPrecedence = false;

        public bool RandomizeBackdrops = false;
        public bool RotateBackdrops = true;
        public int BackdropRotationInterval = 8; //Controls time delay, in seconds, between backdrops during rotation
        public float BackdropTransitionInterval = 1.5F; //Controls animation fade time, in seconds
        public int BackdropLoadDelay = 300; //Delays loading of the first backdrop on new item in milliseconds. Helps with performance

        public bool ProcessBanners = false; //hook to allow future processing of banners
        public bool ProcessBackdrops = false; //hook to allow future processing of backdrops

        public int MinResumeDuration = 0; //minimum duration of video to have resume functionality
        public int MinResumePct = 1; //if this far or less into video, don't resume
        public int MaxResumePct = 95; //if this far or more into video, don't resume

        public bool YearSortAsc = false; //true to sort years in ascending order

        public bool AutoScrollText = false; //Turn on/off Auto Scrolling Text (typically for Overviews)
        public int AutoScrollDelay = 8; //Delay to Start and Reset scrolling text
        public int AutoScrollSpeed = 1; //Scroll Speed for scrolling Text

        public bool AutoValidate = true; //automatically validate and refresh items as we access them

        public LogSeverity MinLoggingSeverity = LogSeverity.Info;

        public bool UseBMPsInCache = false; //experimental option to use bmps for backdrops to increase speed
        public bool EnableScreenSaver = true; //enable default screen saver functionality
        public int ScreenSaverTimeOut = 10; //minutes of inactivity for screen saver to kick in

        public bool AskIncludeChildrenRefresh = true; //prompt to include children on a folder refresh
        public bool DefaultIncludeChildrenRefresh = true; //if we don't prompt, are children included?

        public int NetworkAvailableTimeOut = 5000; //milliseconds to wait for network to be available on validations

        //public bool UseSQLImageCache = false; //switch to use the new SQLite image cache

        [Comment("Cache all images in memory so navigation is faster, consumes a lot more memory")]
        public bool CacheAllImagesInMemory = false;

        [Comment("The number of days to retain log files.  Files older than this will be deleted periodically")]
        public int LogFileRetentionDays = 30;

        [Comment("Whether to send os and memory stats during update check")]
        public bool SendStats = false;

        [Comment("Suppress the statistics nag msg")]
        public bool SuppressStatsNag = false;

        [Comment("This is a hack until I can rewrite some file date processing")]
        public bool EnableShortcutDateHack = true;

        public List<string> PluginSources = new List<string>() { "http://www.mediabrowser.tv/plugins/multi/plugin_info.xml" };

        public class ExternalPlayer
        {
            public MediaType MediaType { get; set; }
            public string Command { get; set; }
            public string Args { get; set; }
            public bool MinimizeMCE = true; //whether or not to minimize MCE when starting external player
            public bool ShowSplashScreen = true; //whether or not to show the MB splash screen

            public override string ToString()
            {
                return MediaType.ToString();
            }
        }

        // for our reset routine
        public ConfigData ()
	    {
            try
            {
                File.Delete(ApplicationPaths.ConfigFile);
            }
            catch (Exception e)
            {
                MediaBrowser.Library.Logging.Logger.ReportException("Unable to delete config file " + ApplicationPaths.ConfigFile, e);
            }
            //continue anyway
            this.file = ApplicationPaths.ConfigFile;
            this.settings = XmlSettings<ConfigData>.Bind(this, file);
	    }


        public ConfigData(string file)
        {
            this.file = file;
            this.settings = XmlSettings<ConfigData>.Bind(this, file);
        }

        [SkipField]
        string file;

        [SkipField]
        XmlSettings<ConfigData> settings;


        public static ConfigData FromFile(string file)
        {
            return new ConfigData(file);  
        }

        public void Save() {
            this.settings.Write();
            //notify of the change
            MediaBrowser.Library.Threading.Async.Queue("Config notify", () => Kernel.Instance.NotifyConfigChange());
        } 

    }
}
