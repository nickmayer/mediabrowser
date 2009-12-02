﻿using System;
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
        const string VERSION = "1.0003";

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
        public string RememberIndexByStateDesc = "Remember Folder Indexing.  e.g. If a Folder is Indexed by Genre, It Will Stay Indexed Each Time It is Entered.";
        public string ShowUnwatchedCountDesc = "Show the Number of Unwatched Items in a Folder on the Folder Poster.";
        public string WatchedIndicatoronFoldersDesc = "Show an Indicator if All Items Inside a Folder Have Been Watched.";
        public string WatchedIndicatoronVideosDesc = "Show an Indicator if a Show Has Been Marked Watched.";
        public string WatchedIndicatorinDetailViewDesc = "Show the Watched Indicator in Lists as Well as Poster Views.";
        public string DefaultToFirstUnwatchedItemDesc = "Scroll to the First Unwatched Item When Entering a Folder.";
        public string AllowNestedMovieFoldersDesc = "Allow the Ability to Put Movie Folders Inside of Other Movie Folders.";
        public string MoviePlaylistsDesc = "If a Folder Contains More than One Playable Item, Play Them in Sequence.";
        public string AutoEnterSingleFolderItemsDesc = "If a Folder Contains Only One Item, Automatically Select and Either Play or Go to the Detail View for That Item.";
        public string PlaylistSizeLimitDesc = "The Maximum Number of Items that will Automatically Play in Sequence.";
        public string BreadcrumbCountDesc = "The Number of Navigation Items to Show in the Trail of Items Entered.";
        public string VisualThemeDesc = "The Basic Presentation Style for MediaBrowser Screens.";
        public string ColorSchemeDesc = "The Style of Colors for Backgrounds, etc.  Won't Take Effect Until MediaBrowser is Restarted.";
        public string FontSizeDesc = "The Size of the Fonts to Use in MediaBrowser.  Won't Take Effect Until MediaBrowser is Restarted.";
        public string ShowConfigButtonDesc = "Show the config button on all screens. [VANILLA ONLY]";
        public string AlphaBlendingDesc = "The Level of Transparency to Use Behind Text Areas to Make Them More Readable. [VANILLA ONLY]";
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


        public BaseStrings() //for the serializer
        {
        }

        public static BaseStrings FromFile(string file)
        {
            BaseStrings s = new BaseStrings() ;
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
