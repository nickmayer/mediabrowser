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


namespace MediaBrowser.Library.Localization
{
    [Serializable]
    public class LocalizedStringData
    {
        public string FileName; //this is public so it will serialize and we know where to save ourselves

        //we store us as an xml doc so we can get the strings out by property
        private XmlDocument usAsXML;

        //these are our strings keyed by property name
        public string LoggingDesc = "Write messages to a log file at run time.";
        public string AllowInternetBasedProvidersDesc = "Search the Internet for Cover Art, Backdrops and Metadata.";
        public string AutomaticUpdatesDesc = "Automatically Download and Install Updates to MediaBrowser.";
        public string BetaUpdatesDesc = "Include Beta Versions in Automatic Updates";
        public string EnableEnhancedHomeScreenDesc = "Enable the Enhanced View for Top-Level Items.";
        public string TransparentBackgroundDesc = "Let the Media Center Background Show Through MediaBrowser";
        public string ShowClockDesc = "Show the Current Time in MediaBrowser Screens.";
        public string DimUnselectedPostersDesc = "Dim the Main Selection Area When not in Focus (e.g. When on a Menu)";
        public string HideFocusFrameDesc = "Don't Show a Border Around Selected Posters in Poster Views.";
        public string PosterGridSpacingDesc = "Number of Pixels to Put Between Each Item in a Grid of Posters.";
        public string ThumbWidthSplitDesc = "Number of Pixels to Use as the Width of the Poster Area in Thumb View";
        public string ShowBackdroponSupportedViewsDesc = "Show Fan Art as a Background in Views with this Capability.";
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
        public string ShowConfigButtonDesc = "Show the Config Button on All Vanilla Screens.";
        public string AlphaBlendingDesc = "The Level of Transparency to Use Behind Text Areas to Make Them More Readable. (Vanilla Only)";
        public string StartMovieViewinMinimalModeDesc = "Use the Minimized Display for Movie Detail Screens by Default.  (Diamond Only)";
        public string EnableEpisodeViewDesc = "Present a Detailed View for TV Episodes Instead of Directly Playing Them - Allows Delete. (Diamond Only)";
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
        public string UnlockDesc = "Temporarily Dis-able Parental Control for the Entire Library.  Will Re-Lock Automatically.";



        LocalizedStringData(string file)
        {
            this.FileName = file;
        }

        LocalizedStringData()
        {
        }

        public string GetString(string key)
        {
            if (usAsXML == null)
            {
                usAsXML = new XmlDocument();
                usAsXML.Load(FileName);
            }
            try
            {
                return usAsXML.SafeGetString("LocalizedStringData/" + key) ?? "";
            }
            catch
            {
                //not there - just return emptystring
                return "";
            }
        }

        public static LocalizedStringData FromFile(string file)
        {
            
            if (!File.Exists(file))
            {
                LocalizedStringData s = new LocalizedStringData(file);
                s.Save();
            }
            Logger.ReportInfo("Using String Data from " + file);
            XmlSerializer xs = new XmlSerializer(typeof(LocalizedStringData));
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return (LocalizedStringData)xs.Deserialize(fs);
            }
        }

        public static string GetFileName()
        {
            string path = ApplicationPaths.AppLocalizationPath;
            string name = Path.Combine(path, "strings-" + CultureInfo.CurrentCulture + ".xml");
            if (File.Exists(name))
            {
                return name;
            }
            else
            {
                name = Path.Combine(path, "strings-" + CultureInfo.CurrentCulture.Parent + ".xml");
                if (File.Exists(name))
                {
                    return name;
                }
                else
                {
                    //just return default
                    return Path.Combine(path, "strings-en.xml");
                }
            }
        }


        public void Save()
        {
            Save(FileName);
        }

        /// <summary>
        /// Write current config to file
        /// </summary>
        public void Save(string file)
        {
            XmlSerializer xs = new XmlSerializer(typeof(LocalizedStringData));
            using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                xs.Serialize(fs, this);
            }
        }
    }
}
