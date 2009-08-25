using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Configuration;
using System.Reflection;
using MediaBrowser.Library;

namespace MusicPlugin.Util
{
    static class Settings
    {
        static string settingPath;

        //settings to save
        public static bool ShowGenreIniTunesLibrary
        { get; set; }
        public static bool ShowArtistIniTunesLibrary
        { get; set; }
        public static bool LoadiTunesLibrary
        { get; set; }
        public static string iTunesLibraryXMLPath
        { get; set; }
        public static bool RefreshiTunesLibrary
        { get; set; }
        public static string iTunesVirtualFolderName
        { get; set; }
        public static bool LoadNormalMusicLibrary
        { get; set; }
        public static string MusicMBFolderName
        { get; set; }
        public static string MusicPath
        { get; set; }
        public static string InitialPath
        { get; set; }
        public static string SongImage
        { get; set; }
        public static bool ShowPlaylistAsFolder
        { get; set; }
        public static string PlayListFolderName
        { get; set; }

        //static string _info;
        //public static string Info
        //{
        //    get
        //    {
        //        return _info;
        //    }

        //    set
        //    {
        //        _info = string.Concat(_info,value);
        //        saveSettingsFile();
        //    }
        //}

        public static void initSettings(string path)
        {
            settingPath = path + "\\..\\plugins\\MusicPlugin.xml";
            if (File.Exists(settingPath))
            {
                loadSettings();
                //incase new settings have been added.
                InitialPath = path+"\\..\\ImageCache";
                saveSettingsFile();
            }
            else
            {
                ShowGenreIniTunesLibrary = true;
                ShowArtistIniTunesLibrary = true;
                LoadiTunesLibrary = false;
                iTunesLibraryXMLPath = @"usually something like c:\Users\ _UserName_ \Music\iTunes\iTunes Music Library.xml";
                RefreshiTunesLibrary = false;
                iTunesVirtualFolderName = "iTunes Library";
                InitialPath = path+"\\..\\ImageCache";
                SongImage = "";
                LoadNormalMusicLibrary = false;
                MusicMBFolderName = "Music Library";
                MusicPath = @"C:\music";
                PlayListFolderName = "_Playlists";
                ShowPlaylistAsFolder = true;
                saveSettingsFile();
            }
        }

        private static void loadSettings()
        {
            XmlTextReader textReader = new XmlTextReader(settingPath);
            while (textReader.Read())
            {
                if (textReader.Name == "Setting")
                {
                    textReader.MoveToFirstAttribute();
                    string name = textReader.Name;
                    string value = textReader.Value;

                    PropertyInfo[] propertyInfos = typeof(Settings).GetProperties(BindingFlags.Public | BindingFlags.Static);
                    foreach (PropertyInfo pi in propertyInfos)
                    {
                        if (pi.Name == name)
                        {
                            if (pi.PropertyType == typeof(bool))
                            {
                                pi.SetValue(null, bool.Parse(value), null);
                            }
                            else if (pi.PropertyType == typeof(int))
                            {
                                pi.SetValue(null, int.Parse(value), null);
                            }
                            else if (pi.PropertyType == typeof(string))
                            {
                                pi.SetValue(null, value, null);
                            }
                        }
                    }
                }
            }
            textReader.Close();
        }

        public static void saveSettingsFile()
        {

            PropertyInfo[] propertyInfos = typeof(Settings).GetProperties(BindingFlags.Public | BindingFlags.Static);

            using (XmlTextWriter textWriter = new XmlTextWriter(settingPath,null))
            {
                textWriter.Formatting = Formatting.Indented;
                textWriter.WriteStartDocument();
                textWriter.WriteStartElement("Settings");
                foreach (PropertyInfo pi in propertyInfos)
                {
                    string name = pi.Name;

                    var temp = pi.GetValue(null, null);

                    string value;
                    if (temp is int)
                    {
                        value = ((int)temp).ToString();
                    }
                    else if (temp is string)
                    {
                        value = (temp as string);
                    }
                    else if (temp is bool)
                    {
                        value = ((bool)temp).ToString();
                    }
                    else
                    {
                        continue;
                    }

                    textWriter.WriteStartElement("Setting");

                    textWriter.WriteAttributeString(name, value as string);

                    textWriter.WriteEndElement();

                }
                textWriter.WriteEndElement();
                textWriter.WriteEndDocument();
                textWriter.Close();
            }
        }
       
    }
}
