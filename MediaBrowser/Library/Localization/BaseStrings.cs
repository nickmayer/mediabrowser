using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Globalization;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Persistance;

namespace MediaBrowser.Library.Localization
{
    [Serializable]
    public class BaseStrings
    {
        const string VERSION = "1.0011";
        const string ENFILE = "strings-en.xml";

        public string Version = VERSION; //this is used to see if we have changed and need to re-save

        //these are our strings keyed by property name
        public string LoggingConfigDesc = "Write messages to a log file at run time.";
        public string EnableInternetProvidersConfigDesc = "Search the Internet for Cover Art, Backdrops and Metadata.";
        public string AutomaticUpdatesConfigDesc = "Automatically Download and Install Updates to MediaBrowser. (Currently disabled except for plug-in update check)";
        public string BetaUpdatesConfigDesc = "Include Beta Versions in Automatic Updates";
        public string EnableEHSConfigDesc = "Enable the Enhanced Home Screen for Top-Level Items.";
        public string ShowClockConfigDesc = "Show the Current Time in MediaBrowser Screens.";
        public string DimUnselectedPostersConfigDesc = "Make Posters That are not Selected Slightly Darker.";
        public string HideFocusFrameConfigDesc = "Don't Show a Border Around Selected Posters in Poster Views.";
        public string PosterGridSpacingConfigDesc = "Number of Pixels to Put Between Each Item in a Grid of Posters.";
        public string ThumbWidthSplitConfigDesc = "Number of Pixels to Use as the Width of the Poster Area in Thumb View";
        public string GeneralConfigDesc = "General Configuration Items.";
        public string MediaOptionsConfigDesc = "Media Related Configuration Items.";
        public string ThemesConfigDesc = "Select the Visual Presentation Style of MediaBrowser.";
        public string ParentalControlConfigDesc = "Parental Control Configuration.  Requires PIN to Access.";
        public string RememberFolderIndexingConfigDesc = "Remember Folder Indexing.  e.g. If a Folder is Indexed by Genre, It Will Stay Indexed Each Time It is Entered.";
        public string ShowUnwatchedCountConfigDesc = "Show the Number of Unwatched Items in a Folder on the Folder Poster.";
        public string WatchedIndicatoronFoldersConfigDesc = "Show an Indicator if All Items Inside a Folder Have Been Watched.";
        public string WatchedIndicatoronVideosConfigDesc = "Show an Indicator if a Show Has Been Marked Watched.";
        public string WatchedIndicatorinDetailViewConfigDesc = "Show the Watched Indicator in Lists as Well as Poster Views.";
        public string DefaultToFirstUnwatchedItemConfigDesc = "Scroll to the First Unwatched Item When Entering a Folder.";
        public string AllowNestedMovieFoldersConfigDesc = "Allow the Ability to Put Movie Folders Inside of Other Movie Folders.";
        public string TreatMultipleFilesAsSingleMovieConfigDesc = "If a Folder Contains More than One Playable Item, Play Them in Sequence. Turn this off if you are having trouble with small collections.";
        public string AutoEnterSingleFolderItemsConfigDesc = "If a Folder Contains Only One Item, Automatically Select and Either Play or Go to the Detail View for That Item.";
        public string MultipleFileSizeLimitConfigDesc = "The Maximum Number of Items that will Automatically Play in Sequence.";
        public string BreadcrumbCountConfigDesc = "The Number of Navigation Items to Show in the Trail of Items Entered.";
        public string VisualThemeConfigDesc = "The Basic Presentation Style for MediaBrowser Screens.";
        public string ColorSchemeConfigDesc = "The Style of Colors for Backgrounds, etc.  Won't Take Effect Until MediaBrowser is Restarted.";
        public string FontSizeConfigDesc = "The Size of the Fonts to Use in MediaBrowser.  Won't Take Effect Until MediaBrowser is Restarted.";
        public string ShowConfigButtonConfigDesc = "Show the config button on all screens.";
        public string AlphaBlendingConfigDesc = "The Level of Transparency to Use Behind Text Areas to Make Them More Readable.";
        public string AlwaysShowDetailsConfigDesc = "Always display the details page for media.";
        public string StartDetailsPageinMiniModeConfigDesc = "Default Media Details Page to Mini-Mode. [DIAMOND ONLY]";
        public string SecurityPINConfigDesc = "The 4-Digit Code For Access to Parental Controlled Items.";
        public string EnableParentalBlocksConfigDesc = "Enable Parental Control.  Items Over The Designated Rating Will Be Hidden or Require PIN.";
        public string BlockUnratedContentConfigDesc = "Treat Items With NO RATING INFO as Over the Limit.  Items Actually Rated 'Unrated' Will Behave Like NC-17.";
        public string MaxAllowedRatingConfigDesc = "The Maximum Rating that Should NOT be Blocked.";
        public string HideBlockedContentConfigDesc = "Hide All Items Over the Designated Rating.";
        public string UnlockonPINEntryConfigDesc = "Temporarily Unlock the Entire Library Whenever the Global PIN is Entered.";
        public string UnlockPeriodHoursConfigDesc = "The Amount of Time (in Hours) Before the Library Will Automatically Re-Lock.";
        public string EnterNewPINConfigDesc = "Change the Global Security Code.";
        public string ContinueConfigDesc = "Return to the Previous Screen.  (All Changes Are Saved Automatically)";
        public string ResetDefaultsConfigDesc = "Reset Configuration Items to Their Default Values.  USE WITH CAUTION - Setings Will Be Overwritten.";
        public string ClearCacheConfigDesc = "Delete the Internal Data Files MediaBrowser Uses and Cause Them to be Re-built.";
        public string UnlockConfigDesc = "Temporarily Disable Parental Control for the Entire Library.  Will Re-Lock Automatically.";
        public string AssumeWatchedIfOlderThanConfigDesc = "Mark All Items Older Than This as Watched.";
        public string ShowThemeBackgroundConfigDesc = "Display Theme background. [TIER 3] Highest tier background effect takes precedence.";
        public string ShowInitialFolderBackgroundConfigDesc = "Display initial backdrop in all views. (backdrop.png or backdrop.jpg sourced from your initial folder) [TIER 2] Highest tier background effect takes precedence.";
        public string ShowFanArtonViewsConfigDesc = "Display fan art as a Background in views that support this capability. [TIER 1] Highest tier background effect takes precedence.";
        public string EnhancedMouseSupportConfigDesc = "Enable Better Scrolling Support with the Mouse.  Leave OFF if You Don't Use a Mouse.  Won't Take Effect Until MediaBrowser is Restarted.";
        public string ShowHDOverlayonPostersConfigDesc = "Show 'HD' or resolution overlay on Hi-def items in Poster Views.";
        public string ShowIcononRemoteContentConfigDesc = "Show an indicator on items from the web in Poster Views.";
        public string ExcludeRemoteContentInSearchesConfigDesc = "Don't show content from the web when searching entire library.";
        public string HighlightUnwatchedItemsConfigDesc = "Show a Highlight on Un-watched Content.";
        public string RandomizeBackdropConfigDesc = "Select random fan art from the available ones.";
        public string RotateBackdropConfigDesc = "Show all available fan art in a sequence (can be random).";
        public string UpdateLibraryConfigDesc = "Update information on the items in your library.";

        //Config Panel
        public string ConfigConfig = "Configuration";
        public string VersionConfig = "Version";
        public string MediaOptionsConfig = "Media Options";
        public string ThemesConfig = "Themes";
        public string ParentalControlConfig = "Parental Control";
        public string ContinueConfig = "Continue";
        public string ResetDefaultsConfig = "Reset Defaults";
        public string ClearCacheConfig = "Clear Cache";
        public string UnlockConfig = "Unlock";
        public string GeneralConfig = "General";
        public string TrackingConfig = "Tracking";
        public string AssumeWatchedIfOlderThanConfig = "Assume Watched If Older Than";
        public string MetadataConfig = "Metadata";
        public string EnableInternetProvidersConfig = "Allow Internet Providers";
        public string UpdatesConfig = "Updates";
        public string AutomaticUpdatesConfig = "Automatic Updates";
        public string LoggingConfig = "Logging";
        public string BetaUpdatesConfig = "Beta Updates";
        public string GlobalConfig = "Global";
        public string EnableEHSConfig = "Enable EHS";
        public string ShowClockConfig = "Show Clock";
        public string DimUnselectedPostersConfig = "Dim Unselected Posters";
        public string HideFocusFrameConfig = "Hide Focus Frame";
        public string AlwaysShowDetailsConfig = "Always Show Details";
        public string ExcludeRemoteContentInSearchesConfig = "Exclude Remote Content In Searches";
        public string EnhancedMouseSupportConfig = "Enhanced Mouse Support";
        public string ViewsConfig = "Views";
        public string PosterGridSpacingConfig = "Poster Grid Spacing";
        public string ThumbWidthSplitConfig = "Thumb Width Split";
        public string BreadcrumbCountConfig = "Breadcrumb Count";
        public string ShowFanArtonViewsConfig = "Show Fan Art on Views";
        public string ShowInitialFolderBackgroundConfig = "Show Initial Folder Background";
        public string ShowThemeBackgroundConfig = "Show Theme Background";
        public string ShowHDOverlayonPostersConfig = "Show HD Overlay on Posters";
        public string ShowIcononRemoteContentConfig = "Show Icon on Remote Content";
        public string EnableAdvancedCmdsConfig = "Enable Advanced Commands";
        public string MediaTrackingConfig = "Media Tracking";
        public string RememberFolderIndexingConfig = "Remember Folder Indexing";
        public string ShowUnwatchedCountConfig = "Show Unwatched Count";
        public string WatchedIndicatoronFoldersConfig = "Watched Indicator on Folders";
        public string HighlightUnwatchedItemsConfig = "Highlight Unwatched Items";
        public string WatchedIndicatoronVideosConfig = "Watched Indicator on Videos";
        public string WatchedIndicatorinDetailViewConfig = "Watched Indicator in Detail View";
        public string DefaultToFirstUnwatchedItemConfig = "Default To First Unwatched Item";
        public string GeneralBehaviorConfig = "General Behavior";
        public string AllowNestedMovieFoldersConfig = "Allow Nested Movie Folders";
        public string AutoEnterSingleFolderItemsConfig = "Auto Enter Single Folder Items";
        public string MultipleFileBehaviorConfig = "Multiple File Behavior";
        public string TreatMultipleFilesAsSingleMovieConfig = "Treat Multiple Files As Single Movie";
        public string MultipleFileSizeLimitConfig = "Multiple File Size Limit";
        public string MBThemeConfig = "Media Browser Theme";
        public string VisualThemeConfig = "Visual Theme";
        public string ColorSchemeConfig = "Color Scheme *";
        public string FontSizeConfig = "Font Size *";
        public string RequiresRestartConfig = "* Requires a restart to take effect.";
        public string ThemeSettingsConfig = "Theme Specific Settings";
        public string ShowConfigButtonConfig = "Show Config Button";
        public string AlphaBlendingConfig = "Alpha Blending";
        public string SecurityPINConfig = "Security PIN";
        public string PCUnlockedTxtConfig = "Parental Controls are Temporarily Unlocked.  You cannot change values unless you re-lock.";
        public string RelockBtnConfig = "Re-Lock";
        public string EnableParentalBlocksConfig = "Enable Parental Blocks";
        public string MaxAllowedRatingConfig = "Max Allowed Rating ";
        public string BlockUnratedContentConfig = "Block Unrated Content";
        public string HideBlockedContentConfig = "Hide Blocked Content";
        public string UnlockonPINEntryConfig = "Unlock on PIN Entry";
        public string UnlockPeriodHoursConfig = "Unlock Period (Hours)";
        public string EnterNewPINConfig = "Enter New PIN";
        public string RandomizeBackdropConfig = "Randomize";
        public string RotateBackdropConfig = "Rotate";
        public string UpdateLibraryConfig = "Update Library";



        //EHS        
        public string RecentlyWatchedEHS = "recently watched";
        public string RecentlyAddedEHS = "recently added";
        public string WatchedEHS = "Watched";
        public string AddedEHS = "Added";
        public string AddedOnEHS = "Added on";
        public string OnEHS = "on";
        public string OfEHS = "of";
        public string NoItemsEHS = "No Items To Show";

        //Context menu
        public string CloseCMenu = "Close";
        public string PlayMenuCMenu = "Play Menu";
        public string ItemMenuCMenu = "Item Menu";
        public string PlayAllCMenu = "Play All";
        public string MarkUnwatchedCMenu = "Mark Unwatched";
        public string MarkWatchedCMenu = "Mark Watched";
        public string ShufflePlayCMenu = "Shuffle Play";

        //Movie Detail Page
        public string GeneralDetail = "General";
        public string ActorsDetail = "Actors";
        public string PlayDetail = "Play";
        public string ResumeDetail = "Resume";
        public string RefreshDetail = "Refresh";
        public string PlayTrailersDetail = "Play Trailer";
        public string CacheDetail = "Cache 2 xml";
        public string DeleteDetail = "Delete";
        public string IMDBRatingDetail = "IMDB Rating";
        public string OutOfDetail = "out of";
        public string DirectorDetail = "Director";
        public string RuntimeDetail = "Runtime";

        public string DirectedByDetail = "Directed By: ";
        public string WrittenByDetail = "Written By: ";

        //Display Prefs
        public string ViewDispPref = "view";
        public string CoverFlowDispPref = "Cover Flow";
        public string DetailDispPref = "Detail";
        public string PosterDispPref = "Poster";
        public string ThumbDispPref = "Thumb";
        public string ThumbStripDispPref = "Thumb Strip";
        public string ShowLabelsDispPref = "Show Labels";
        public string VerticalScrollDispPref = "Vertical Scroll";
        public string UseBannersDispPref = "Use Banners";
        public string UseCoverflowDispPref = "Use Coverflow Style";
        public string ThumbSizeDispPref = "Thumb Size";
        public string NameDispPref = "name";
        public string DateDispPref = "date";
        public string RatingDispPref = "rating";
        public string RuntimeDispPref = "runtime";
        public string UnWatchedDispPref = "unwatched";
        public string YearDispPref = "year";
        public string NoneDispPref = "none";
        public string ActorDispPref = "actor";
        public string GenreDispPref = "genre";
        public string DirectorDispPref = "director";
        public string StudioDispPref = "studio";

        //Dialog boxes
        public string BrokenEnvironmentDial = "Application will now close due to broken MediaCenterEnvironment object, possibly due to 5 minutes of idle time and/or running with TVPack installed.";
        public string InitialConfigDial = "Initial configuration is complete, please restart Media Browser";
        public string DeleteMediaDial = "Are you sure you wish to delete this media item?";
        public string DeleteMediaCapDial = "Delete Confirmation";
        public string NotDeletedDial = "Item NOT Deleted.";
        public string NotDeletedCapDial = "Delete Cancelled by User";
        public string NotDelInvalidPathDial = "The selected media item cannot be deleted due to an invalid path. Or you may not have sufficient access rights to perform this command.";
        public string DelFailedDial = "Delete Failed";
        public string NotDelUnknownDial = "The selected media item cannot be deleted due to an unknown error.";
        public string NotDelTypeDial = "The selected media item cannot be deleted due to its Item-Type or you have not enabled this feature in the configuration file.";
        public string FirstTimeDial = "As this is the first time you have run Media Browser please setup the inital configuration";
        public string FirstTimeCapDial = "Configure";
        public string EntryPointErrorDial = "Media Browser could not launch directly into ";
        public string EntryPointErrorCapDial = "Entrypoint Error";
        public string CriticalErrorDial = "Media Browser encountered a critical error and had to shut down: ";
        public string CriticalErrorCapDial = "Critical Error";
        public string ClearCacheErrorDial = "An error occured during the clearing of the cache, you may wish to manually clear it from {0} before restarting Media Browser";
        public string RestartMBDial = "Please restart Media Browser";
        public string ClearCacheDial = "Are you sure you wish to clear the cache?\nThis will erase all cached and downloaded information and images.";
        public string ClearCacheCapDial = "Clear Cache";
        public string CacheClearedDial = "Cache Cleared";
        public string ResetConfigDial = "Are you sure you wish to reset all configuration to defaults?";
        public string ResetConfigCapDial = "Reset Configuration";
        public string ConfigResetDial = "Configuration Reset";
        public string UpdateMBDial = "Do you wish to update Media Browser now?  (Requires you to grant permissions and a restart of Media Browser)";
        public string UpdateMBCapDial = "Update Available";
        public string UpdateMBExtDial = "There is an update available for Media Browser.  Please update Media Browser next time you are at your MediaCenter PC.";
        public string DLUpdateFailDial = "Media Browser will operate normally and prompt you again the next time you load it.";
        public string DLUpdateFailCapDial = "Update Download Failed";
        public string UpdateSuccessDial = "Media Browser must now exit to apply the update.  It will restart automatically when it is done";
        public string UpdateSuccessCapDial = "Update Downloaded";
        public string CustomErrorDial = "Customisation Error";
        public string ConfigErrorDial = "Reset to default?";
        public string ConfigErrorCapDial = "Error in configuration file";
        public string ContentErrorDial = "There was a problem playing the content. Check location exists";
        public string ContentErrorCapDial = "Content Error";
        public string CannotMaximizeDial = "We can not maximize the window! This is a known bug with Windows 7 and TV Pack, you will have to restart Media Browser!";
        public string IncorrectPINDial = "Incorrect PIN Entered";
        public string ContentProtected = "Content Protected";
        public string CantChangePINDial = "Cannot Change PIN";
        public string LibraryUnlockedDial = "Library Temporarily Unlocked.  Will Re-Lock in {0} Hour(s) or on Application Re-Start";
        public string LibraryUnlockedCapDial = "Unlock";
        public string PINChangedDial = "PIN Successfully Changed";
        public string PINChangedCapDial = "PIN Change";
        public string EnterPINToViewDial = "Please Enter PIN to View Protected Content";
        public string EnterPINToPlayDial = "Please Enter PIN to Play Protected Content";
        public string EnterCurrentPINDial = "Please Enter CURRENT PIN.";
        public string EnterNewPINDial = "Please Enter NEW PIN (exactly 4 digits).";
        public string EnterPINDial = "Please Enter PIN to Unlock Library";
        public string NoContentDial = "No Content that can be played in this context.";
        public string FontsMissingDial = "CustomFonts.mcml as been patched with missing values";
        public string StyleMissingDial = "{0} has been patched with missing values";
        public string ManualRefreshDial = "Library Update Started.  Will proceed in the background.";

        //Generic
        public string Restartstr = "Restart";
        public string Errorstr = "Error";
        public string Playstr = "Play";

        //Profiler
        public string WelcomeProf = "Welcome to Media Browser.";
        public string ProfilerTimeProf = "{1} took {2} seconds.";
        public string RefreshProf = "Refresh";
        public string SetWatchedProf = "Set Watched {0}";
        public string ClearWatchedProf = "Clear Watched {0}";
        public string FullRefreshProf = "Full Library Refresh";
        public string FullValidationProf = "Full Library Validation";
        public string FastRefreshProf = "Fast Metadata refresh";
        public string SlowRefresh = "Slow Metadata refresh";
        public string PluginUpdateProf = "An update is available for plug-in {0}";
        public string NoPluginUpdateProf = "No Plugin Updates Currently Available.";
        public string LibraryUnLockedProf = "Library Temporarily UnLocked. Will Re-Lock in {0} Hour(s)";
        public string LibraryReLockedProf = "Library Re-Locked";



        public BaseStrings() //for the serializer
        {
        }

        public static BaseStrings FromFile(string file)
        {
            BaseStrings s = new BaseStrings();
            XmlSettings<BaseStrings> settings = XmlSettings<BaseStrings>.Bind(s, file);

            Logger.ReportInfo("Using String Data from " + file);

            if (VERSION != s.Version && Path.GetFileName(file).ToLower() == ENFILE)
            {
                //only re-save the english version as that is the one defined internally
                File.Delete(file);
                s = new BaseStrings();
                settings = XmlSettings<BaseStrings>.Bind(s, file);
            }
            return s;
        }
    }
}
