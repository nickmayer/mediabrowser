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

namespace MusicPlugin.Util
{
    public class TheSettings
    {
        //settings to save
        public bool FirstLoad
        { get; set; }
        public bool ShowGenreIniTunesLibrary
        { get; set; }
        public bool ShowArtistIniTunesLibrary
        { get; set; }
        public bool LoadiTunesLibrary
        { get; set; }
        public string iTunesLibraryXMLPath
        { get; set; }
        public bool ForceRefreshiTunesLibrary
        { get; set; }
        public string iTunesLibraryVirtualFolderName
        { get; set; }
        public bool LoadNormalLibrary
        { get; set; }
        public string NormalLibraryVirtualFolderName
        { get; set; }
        public string InitialPath
        { get; set; }
        public string SongImage
        { get; set; }
        public bool ShowPlaylistAsFolder
        { get; set; }
        public string PlayListFolderName
        { get; set; }
        public string iTunesLibraryIcon
        { get; set; }
        public string NormalLibraryIcon
        { get; set; }
        public string NormalLibraryPath
        { get; set; }
        //private List<string> _normalLibraryPaths;
        //public List<string> NormalLibraryPaths
        //{
        //    get
        //    {
        //        if (_normalLibraryPaths == null)
        //            _normalLibraryPaths = new List<string>();
        //        return _normalLibraryPaths;
        //    }
        //}
    }
    static class Settings
    {
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
       
    }
}
