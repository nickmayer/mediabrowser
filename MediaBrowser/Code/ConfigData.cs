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

namespace MediaBrowser
{
    [Serializable]
    public class ConfigData
    {

        [Comment(@"By default we track a videos position to support resume, this can be disabled by setting this for diagnostic purposes")]
        public bool EnableResumeSupport = true; 
        [Comment(@"Any folder named trailers will be ignored and treated a folder containing trailers")]
        public bool EnableLocalTrailerSupport = true; 
        [Comment(@"If you enable this, make sure System.Data.SQLite.DLL is copied to c:\program data\mediabrowser, make sure you install the right version there is a x32 and x64")]
        public bool EnableExperimentalSqliteSupport = false;
        public bool AlwaysShowDetailsPage = true;
        public bool EnableVistaStopPlayStopHack = true;
        public bool EnableRootPage = true;
        public bool IsFirstRun = true;
        public string ImageByNameLocation = Path.Combine(ApplicationPaths.AppConfigPath, "ImagesByName");
        public Vector3 OverScanScaling = new Vector3() {X=1, Y=1, Z=1};
        public Inset OverScanPadding = new Inset();
        public bool EnableTraceLogging = false;
        public Size DefaultPosterSize = new Size() {Width=220, Height=330};
        public Size GridSpacing = new Size();
        public float MaximumAspectRatioDistortion = 0.2F;
        public bool EnableTranscode360 = false;
        public string ExtenderNativeTypes = ".dvr-ms,.wmv";
        public bool ShowThemeBackground = false;
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

        public bool ShowHDIndicatorOnPosters = false;
        public bool ShowRemoteIndicatorOnPosters = true;
        public bool ExcludeRemoteContentInSearch = true;

        public bool ShowUnwatchedIndicator = false;

        public List<string> PluginSources = new List<string>() {"http://www.mediabrowser.tv/plugins/plugin_info.xml"};

        public class ExternalPlayer
        {
            public MediaType MediaType { get; set; }
            public string Command { get; set; }
            public string Args { get; set; }

            public override string ToString()
            {
                return MediaType.ToString();
            }
        }

        // for the serializer
        public ConfigData ()
	    {
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
        } 

    }
}
