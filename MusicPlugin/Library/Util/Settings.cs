using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Configuration;
using System.Reflection;
using MediaBrowser.Library;
using System.Xml.Serialization;
using MusicPlugin.Code.Attributes;
using System.Windows.Forms;
using MusicPlugin.Views;
using MediaBrowser.Library.Logging;
using Microsoft.MediaCenter;

namespace MusicPlugin.Util
{
    public class TheSettings
    {
        //settings to save
        [ControlAttribute(typeof(CheckBox))]
        [Description("First Load")]
        [HiddenAttribute(true)]
        [GroupAttribute("General")]
        public bool FirstLoad
        { get; set; }
        [Description("For office use")]
        [ControlAttribute(typeof(TextBox))]
        [HiddenAttribute(true)]
        [GroupAttribute("General")]
        public string InitialPath
        { get; set; }

        [Description("Enabled:")]
        [ControlAttribute(typeof(CheckBox))]
        [HiddenAttribute(false)]
        [GroupAttribute("iTunes Library (*changes require a re-cache)")]
        public bool LoadiTunesLibrary
        { get; set; }
        [Description("Name:")]
        [ControlAttribute(typeof(TextBox))]
        [HiddenAttribute(false)]
        [GroupAttribute("iTunes Library (*changes require a re-cache)")]
        public string iTunesLibraryVirtualFolderName
        { get; set; }
        [Description("Show Genre:")]
        [ControlAttribute(typeof(CheckBox))]
        [HiddenAttribute(false)]
        [GroupAttribute("iTunes Library (*changes require a re-cache)")]
        public bool ShowGenreIniTunesLibrary
        { get; set; }
        [Description("Show Artist:")]
        [ControlAttribute(typeof(CheckBox))]
        [HiddenAttribute(false)]
        [GroupAttribute("iTunes Library (*changes require a re-cache)")]
        public bool ShowArtistIniTunesLibrary
        { get; set; }

        [Description("iTunes XML Path*:")]
        [ControlAttribute(typeof(FileTextBox))]
        [HiddenAttribute(false)]
        [GroupAttribute("iTunes Library (*changes require a re-cache)")]
        [ExtAttribute("XML Files (*.xml)|*.xml")]
        public string iTunesLibraryXMLPath
        { get; set; }
        [Description("Image used for Library:")]
        [ControlAttribute(typeof(FileTextBox))]
        [HiddenAttribute(false)]
        [GroupAttribute("iTunes Library (*changes require a re-cache)")]
        [ExtAttribute("Image Files (*.png;*.jpg)|*.png;*.jpg|All files (*.*)|*.*")]
        public string iTunesLibraryIcon
        { get; set; }
        [Description("Re-cache iTunes Library:")]
        [ControlAttribute(typeof(CheckBox))]
        [HiddenAttribute(false)]
        [GroupAttribute("iTunes Library (*changes require a re-cache)")]
        public bool ForceRefreshiTunesLibrary
        { get; set; }

        [Description("Enabled:")]
        [ControlAttribute(typeof(CheckBox))]
        [HiddenAttribute(false)]
        [GroupAttribute("Normal Library")]
        public bool LoadNormalLibrary
        { get; set; }
        [Description("Name:")]
        [ControlAttribute(typeof(TextBox))]
        [HiddenAttribute(false)]
        [GroupAttribute("Normal Library")]
        public string NormalLibraryVirtualFolderName
        { get; set; }
        [Description("Music Path:")]
        [ControlAttribute(typeof(FolderTextBox))]
        [HiddenAttribute(false)]
        [GroupAttribute("Normal Library")]
        public string NormalLibraryPath
        { get; set; }
        [Description("Image used for Library:")]
        [ControlAttribute(typeof(FileTextBox))]
        [HiddenAttribute(false)]
        [GroupAttribute("Normal Library")]
        [ExtAttribute("Image Files (*.png;*.jpg)|*.png;*.jpg|All files (*.*)|*.*")]
        public string NormalLibraryIcon
        { get; set; }

        [Description("Enabled:")]
        [ControlAttribute(typeof(CheckBox))]
        [HiddenAttribute(false)]
        [GroupAttribute("Playlist")]
        public bool ShowPlaylistAsFolder
        { get; set; }
        [Description("Playlist Folder Name:")]
        [ControlAttribute(typeof(TextBox))]
        [HiddenAttribute(false)]
        [GroupAttribute("Playlist")]
        public string PlayListFolderName
        { get; set; }

        [Description("Image used for Songs*:")]
        [ControlAttribute(typeof(FileTextBox))]
        [HiddenAttribute(false)]
        [GroupAttribute("General (*changes require re-cache of iTunes library)")]
        [ExtAttribute("Image Files (*.png;*.jpg)|*.png;*.jpg|All files (*.*)|*.*")]
        public string SongImage
        { get; set; }
    }
    public static class Settings
    {
        public static string DIALOGHEADING = "MusicPlugin";
        public static string SettingPath;
        private static TheSettings _instance;
        public static TheSettings Instance 
        {
            get
            {
                if (_instance == null)
                    _instance = new TheSettings();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
        
                
        public static void InitSettings(string path)
        {
            SettingPath = Path.GetFullPath(path + "\\..\\plugins\\MusicPlugin.xml");
            if (File.Exists(SettingPath))
            {
                LoadSettings();
                //incase new settings have been added.
                Instance.InitialPath = path + "\\..\\ImageCache";
                SaveSettingsFile();                
            }
            else
            {
                Instance.FirstLoad = true;
                Instance.ShowGenreIniTunesLibrary = true;
                Instance.ShowArtistIniTunesLibrary = true;
                Instance.LoadiTunesLibrary = false;
                Instance.iTunesLibraryXMLPath = @"c:\iTunes Music Library.xml";
#if DEBUG
                Instance.iTunesLibraryXMLPath = @"c:\Users\_UserName_\Music\iTunes\iTunes Music Library.xml";
#endif
                Instance.ForceRefreshiTunesLibrary = false;
                Instance.iTunesLibraryVirtualFolderName = "iTunes Library";
                Instance.InitialPath = path + "\\..\\ImageCache";
                Instance.SongImage = "";
                Instance.LoadNormalLibrary = false;
                Instance.NormalLibraryVirtualFolderName = "Music Library";
                Instance.NormalLibraryPath = @"C:\music";
                Instance.PlayListFolderName = "_Playlists";
                Instance.ShowPlaylistAsFolder = true;
                Instance.iTunesLibraryIcon = "";
                Instance.NormalLibraryIcon = "";
                SaveSettingsFile();
            }
        }

        private static void LoadSettings()
        {
            XmlSerializer ser = new XmlSerializer(typeof(TheSettings));

            TextReader reader = new StreamReader(SettingPath);
            Instance = (TheSettings) ser.Deserialize(reader);
            reader.Close();
        }

        public static void SaveSettingsFile()
        {
            XmlSerializer ser = new XmlSerializer(typeof(TheSettings));

            TextWriter writer = new StreamWriter(SettingPath);
            ser.Serialize(writer, Settings.Instance);
            writer.Close();

        }

        public static bool ValidateSettings(string initialFolder, bool mceValidation)
        {
            string message;

            if (mceValidation) //MCE load and validates settings.
                try
                {
                    Settings.InitSettings(initialFolder);
                }
                catch (Exception e)
                {
                    Logger.ReportException("MusicPlugin", e);
                    message = "The MusicPlugin could not be loaded. Please enable logging in MediaBrowser and check the log.";
                    ShowWarning(mceValidation, message);
                    return false;
                }

            if (!File.Exists(Settings.SettingPath))
            {
                message = "The MusicPlugin could not create a config file. It will not be loaded.";
                ShowWarning(mceValidation, message);
                return false;
            }

            if (_instance.FirstLoad)
            {
                message = "The MusicPlugin has created its own configuration file, please close MediaBrowser and configure " + Settings.SettingPath + ".";
                ShowWarning(mceValidation, message);
                _instance.FirstLoad = false;
                Settings.SaveSettingsFile();
                return false;
            }

            if (_instance.LoadNormalLibrary && _instance.LoadiTunesLibrary && _instance.NormalLibraryVirtualFolderName == _instance.iTunesLibraryVirtualFolderName)
            {
                message = "Your Normal and iTunes Libraries are enabled, but your virtual folders names are the same.";
                ShowWarning(mceValidation, message);
                return false;
            }

            if ((_instance.LoadiTunesLibrary) && !string.IsNullOrEmpty(_instance.iTunesLibraryIcon) && !File.Exists(_instance.iTunesLibraryIcon))
            {
                message = "Your iTunes Library is enabled, but the specified icon path is invalid.";
                ShowWarning(mceValidation, message);
                return false;
            }

            if ((_instance.LoadNormalLibrary) && !string.IsNullOrEmpty(_instance.NormalLibraryIcon) && !File.Exists(_instance.NormalLibraryIcon))
            {
                message = "Your Normal Library is enabled, but the specified icon path is invalid.";
                ShowWarning(mceValidation, message);
                return false;
            }

            if (_instance.ShowPlaylistAsFolder && string.IsNullOrEmpty(_instance.PlayListFolderName))
            {
                message = "Your playlist folder is enabled, but the specified name is invalid.";
                ShowWarning(mceValidation, message);
                return false;
            }

            if (!string.IsNullOrEmpty(_instance.SongImage) && !File.Exists(_instance.SongImage))
            {
                message = "The specified song image is invalid.";
                ShowWarning(mceValidation, message);
                return false;
            }

            return true;
        }

        private static void ShowWarning(bool mceValidation, string message)
        {
            if (mceValidation)
                Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, DIALOGHEADING, DialogButtons.Ok, 60, true);
            else
                MessageBox.Show(message, DIALOGHEADING, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        public static bool ValidateiTunesLibrary(bool mceValidation)
        {
            string message;

            if (_instance.LoadiTunesLibrary && (string.IsNullOrEmpty(_instance.iTunesLibraryXMLPath) || !File.Exists(_instance.iTunesLibraryXMLPath)))
            {
                message = "Your iTunes Library is enabled, but the specified xml path is invalid. It will not be loaded.";
                ShowWarning(mceValidation, message);
                return false;
            }

            return true;
        }

        public static bool ValidateNormalLibrary(bool mceValidation)
        {
            string message;

            if (_instance.LoadNormalLibrary && (string.IsNullOrEmpty(_instance.NormalLibraryPath) || !Directory.Exists(_instance.NormalLibraryPath)))
            {
                message = "Your Normal Library is enabled, but the specified directory is invalid. It will not be loaded.";
                ShowWarning(mceValidation, message);
                return false;
            }

            return true;
        }
    }
}
