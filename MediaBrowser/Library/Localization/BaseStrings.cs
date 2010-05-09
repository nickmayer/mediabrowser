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
        const string VERSION = "1.0008";

        public string Version = VERSION; //this is used to see if we have changed and need to re-save

        //these are our strings keyed by property name
        public string LoggingDesc = "Write messages to a log file at run time.";
        public string EnableInternetProvidersDesc = "Search the Internet for Cover Art, Backdrops and Metadata.";
        public string AutomaticUpdatesDesc = "Automatically Download and Install Updates to MediaBrowser.";
        public string BetaUpdatesDesc = "Include Beta Versions in Automatic Updates";
        public string EnableEHSDesc = "Enable the Enhanced Home Screen for Top-Level Items.";
        public string ShowClockDesc = "Show the Current Time in MediaBrowser Screens.";
        public string DimUnselectedPostersDesc = "Make Posters That are not Selected Slightly Darker.";
        public string HideFocusFrameDesc = "Don't Show a Border Around Selected Posters in Poster Views.";
        public string PosterGridSpacingDesc = "Number of Pixels to Put Between Each Item in a Grid of Posters.";
        public string ThumbWidthSplitDesc = "Number of Pixels to Use as the Width of the Poster Area in Thumb View";
        public string GeneralDesc = "General Configuration Items.";
        public string MediaOptionsDesc = "Media Related Configuration Items.";
        public string ThemesDesc = "Select the Visual Presentation Style of MediaBrowser.";
        public string ParentalControlDesc = "Parental Control Configuration.  Requires PIN to Access.";
        public string RememberFolderIndexingDesc = "Remember Folder Indexing.  e.g. If a Folder is Indexed by Genre, It Will Stay Indexed Each Time It is Entered.";
        public string ShowUnwatchedCountDesc = "Show the Number of Unwatched Items in a Folder on the Folder Poster.";
        public string WatchedIndicatoronFoldersDesc = "Show an Indicator if All Items Inside a Folder Have Been Watched.";
        public string WatchedIndicatoronVideosDesc = "Show an Indicator if a Show Has Been Marked Watched.";
        public string WatchedIndicatorinDetailViewDesc = "Show the Watched Indicator in Lists as Well as Poster Views.";
        public string DefaultToFirstUnwatchedItemDesc = "Scroll to the First Unwatched Item When Entering a Folder.";
        public string AllowNestedMovieFoldersDesc = "Allow the Ability to Put Movie Folders Inside of Other Movie Folders.";
        public string TreatMultipleFilesAsSingleMovieDesc = "If a Folder Contains More than One Playable Item, Play Them in Sequence. Turn this off if you are having trouble with small collections.";
        public string AutoEnterSingleFolderItemsDesc = "If a Folder Contains Only One Item, Automatically Select and Either Play or Go to the Detail View for That Item.";
        public string MultipleFileSizeLimitDesc = "The Maximum Number of Items that will Automatically Play in Sequence.";
        public string BreadcrumbCountDesc = "The Number of Navigation Items to Show in the Trail of Items Entered.";
        public string VisualThemeDesc = "The Basic Presentation Style for MediaBrowser Screens.";
        public string ColorSchemeDesc = "The Style of Colors for Backgrounds, etc.  Won't Take Effect Until MediaBrowser is Restarted.";
        public string FontSizeDesc = "The Size of the Fonts to Use in MediaBrowser.  Won't Take Effect Until MediaBrowser is Restarted.";
        public string ShowConfigButtonDesc = "Show the config button on all screens.";
        public string AlphaBlendingDesc = "The Level of Transparency to Use Behind Text Areas to Make Them More Readable.";
        public string AlwaysShowDetailsDesc = "Always display the details page for media.";
        public string StartDetailsPageinMiniModeDesc = "Default Media Details Page to Mini-Mode. [DIAMOND ONLY]";
        public string SecurityPINDesc = "The 4-Digit Code For Access to Parental Controlled Items.";
        public string EnableParentalBlocksDesc = "Enable Parental Control.  Items Over The Designated Rating Will Be Hidden or Require PIN.";
        public string BlockUnratedContentDesc = "Treat Items With NO RATING INFO as Over the Limit.  Items Actually Rated 'Unrated' Will Behave Like NC-17.";
        public string MaxAllowedRatingDesc = "The Maximum Rating that Should NOT be Blocked.";
        public string HideBlockedContentDesc = "Hide All Items Over the Designated Rating.";
        public string UnlockonPINEntryDesc = "Temporarily Unlock the Entire Library Whenever the Global PIN is Entered.";
        public string UnlockPeriodHoursDesc = "The Amount of Time (in Hours) Before the Library Will Automatically Re-Lock.";
        public string EnterNewPINDesc = "Change the Global Security Code.";
        public string ContinueDesc = "Return to the Previous Screen.  (All Changes Are Saved Automatically)";
        public string ResetDefaultsDesc = "Reset Configuration Items to Their Default Values.  USE WITH CAUTION - Setings Will Be Overwritten.";
        public string ClearCacheDesc = "Delete the Internal Data Files MediaBrowser Uses and Cause Them to be Re-built.";
        public string UnlockDesc = "Temporarily Disable Parental Control for the Entire Library.  Will Re-Lock Automatically.";
        public string AssumeWatchedIfOlderThanDesc = "Mark All Items Older Than This as Watched.";
        public string ShowThemeBackgroundDesc = "Display Theme background. [TIER 3] Highest tier background effect takes precedence.";
        public string ShowInitialFolderBackgroundDesc = "Display initial backdrop in all views. (backdrop.png or backdrop.jpg sourced from your initial folder) [TIER 2] Highest tier background effect takes precedence.";
        public string ShowFanArtonViewsDesc = "Display fan art as a Background in views that support this capability. [TIER 1] Highest tier background effect takes precedence.";
        public string EnhancedMouseSupportDesc = "Enable Better Scrolling Support with the Mouse.  Leave OFF if You Don't Use a Mouse.  Won't Take Effect Until MediaBrowser is Restarted.";
        public string ShowHDOverlayonPostersDesc = "Show 'HD' or resolution overlay on Hi-def items in Poster Views.";
        public string ShowIcononRemoteContentDesc = "Show an indicator on items from the web in Poster Views.";
        public string ExcludeRemoteContentInSearchesDesc = "Don't show content from the web when searching entire library.";
        public string HighlightUnwatchedItemsDesc = "Show a Highlight on Un-watched Content.";

        //Config Panel
        public string ConfigConfig = "Configuration";
        public string VersionConfig = "version";
        public string ContinueConfig = "Continue";
        public string ResetDefaultsConfig = "Reset Defaults";
        public string ClearCacheConfig = "Clear Cache";
        public string UnlockConfig = "Unlock";
        public string GeneralConfig = "General";
        public string TrackingConfig = "Tracking";
        public string AssumeWatchedIfOlderThanConfig = "Assume Watched If Older Than";
        public string MetadataConfig = "Metadata";
        public string EnableInternetProvidersConfig = "Allow Internet Based Providers";
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
        public string RestartDial = "Restart";
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
        public string ErrorDial = "Error";
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
        public string EnterPINDial = "Please Enter PIN to Unlock Library";
        public string NoContentDial = "No Content that can be played in this context.";
        public string NoContentCapDial = "Play";
        public string FontsMissingDial = "CustomFonts.mcml as been patched with missing values";
        public string StyleMissingDial = "{0} has been patched with missing values";

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

            if (VERSION != s.Version)
            {
                File.Delete(file);
                s = new BaseStrings();
                settings = XmlSettings<BaseStrings>.Bind(s, file);
            }
            return s;
        }
    }
}
