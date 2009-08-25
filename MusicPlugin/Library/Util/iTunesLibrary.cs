using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using MusicPlugin.Library.Entities;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.ImageManagement;
using System.IO;
using MediaBrowser.Library.Entities;
using MusicPlugin.Util;
using MediaBrowser.Library.Logging;
using MusicPlugin.LibraryManagement;

namespace MusicPlugin.Library.Helpers
{
    public class iTunesLibrary
    {
        public iTunesLibrary()
        {
            
        }

        iTunesMusicLibrary _library;
        public iTunesMusicLibrary Library
        {
            get
            {
                if (_library == null)
                    _library = GetDetailsFromXml(null);
                return _library;
            }
        }

        public static iTunesMusicLibrary GetDetailsFromXml(iTunesMusicLibrary existingLibrary)
        {
            string path = Settings.iTunesLibraryXMLPath;

            if (!File.Exists(path))
            {                
                throw new FileNotFoundException("iTunesLibraryXMLPath " + path + " not found!");
            }
            
            XPathDocument doc = new XPathDocument(path);
            XPathNavigator nav = doc.CreateNavigator();

            // Move to plist, then master library and tracks
            nav.MoveToChild("plist", "");
            nav.MoveToChild("dict", "");
            nav.MoveToChild("dict", "");
            Dictionary<string, iTunesArtist> dictionaryArtist = new Dictionary<string, iTunesArtist>();
            Dictionary<string, iTunesAlbum> dictionaryAlbum = new Dictionary<string, iTunesAlbum>();
            Dictionary<string, iTunesAlbum> dictionaryUniqueAlbum = new Dictionary<string, iTunesAlbum>();
            Dictionary<string, iTunesGenre> dictionaryGenre = new Dictionary<string, iTunesGenre>();
            iTunesArtist childArtistFolder;
            iTunesAlbum childAlbumFolder;
            iTunesGenre childGenreFolder = null;
            iTunesAlbum childUniqueAlbumFolder;

            // Move to first track info
            bool success = nav.MoveToChild("dict", "");
            //int count = 0;
            // Read each song until we have enough or no more
            iTunesMusicLibrary folder;
            if (existingLibrary == null)
                folder = new iTunesMusicLibrary();
            else
            {
                folder = existingLibrary;
                ClearCache(folder);                
            }

            folder.LastUpdate = DateTime.Now;

            while (success)
            {
                success = nav.MoveToFirstChild();

                // Read each piece of information about the song
                Dictionary<string, string> data = new Dictionary<string, string>();
                while (success)
                {
                    string key = nav.Value;
                    nav.MoveToNext();
                    data[key] = nav.Value;
                    success = nav.MoveToNext();
                }

                if (!data.ContainsKey("Name"))
                    data.Add("Name", "");
                if (!data.ContainsKey("Artist"))
                    data.Add("Artist", "");
                if (!data.ContainsKey("Album"))
                    data.Add("Album", "");
                if (!data.ContainsKey("Genre"))
                    data.Add("Genre", "");

                if (string.IsNullOrEmpty(data["Artist"]) || string.IsNullOrEmpty(data["Album"]))
                {
                    nav.MoveToParent();
                    success = nav.MoveToNext("dict", "");
                    continue;
                }

                childGenreFolder = BuildGenre(dictionaryGenre, childGenreFolder, folder, data);

                childArtistFolder = BuildArtist(dictionaryArtist, childGenreFolder, folder, data);

                childAlbumFolder = BuildAlbum(dictionaryAlbum, childArtistFolder, data);

                iTunesSong newSong = BuildSong(childArtistFolder, childAlbumFolder, data);

                ReadFileImage(childAlbumFolder, newSong);

                if (dictionaryUniqueAlbum.ContainsKey(data["Album"]))
                    childUniqueAlbumFolder = dictionaryUniqueAlbum[data["Album"]];
                else
                {
                    childUniqueAlbumFolder = new iTunesAlbum();
                    childUniqueAlbumFolder.Name = data["Album"];
                    childUniqueAlbumFolder.AlbumName = data["Album"];
                    childUniqueAlbumFolder.Id = childUniqueAlbumFolder.AlbumName.GetMD5();
                    folder.Albums.Add(childUniqueAlbumFolder);
                    dictionaryUniqueAlbum.Add(data["Album"], childUniqueAlbumFolder);
                }
                childUniqueAlbumFolder.Songs.Add(newSong);
                childUniqueAlbumFolder.PrimaryImagePath = childAlbumFolder.PrimaryImagePath;
                childUniqueAlbumFolder.BackdropImagePath = childAlbumFolder.BackdropImagePath;
                
                nav.MoveToParent();
                success = nav.MoveToNext("dict", "");
            }
            
            if (Settings.ShowPlaylistAsFolder)
                AddSpecialMusicFolder(folder);

            return folder;
        }

        private static void AddSpecialMusicFolder(iTunesMusicLibrary folder)
        {
            Folder musicFolder = MusicHelper.GetPlaylistFolder();
            
            if (folder.Albums != null)
                folder.Albums.Add(musicFolder);

            if (folder.Artists != null)
                folder.Artists.Add(musicFolder);

            if (folder.Genres != null)
                folder.Genres.Add(musicFolder);
        }

        
        

        private static void ClearCache(iTunesMusicLibrary folder)
        {
            folder.Artists.Clear();
            folder.Genres.Clear();
            folder.Albums.Clear();
        }

        private static iTunesGenre BuildGenre(Dictionary<string, iTunesGenre> dictionaryGenre, iTunesGenre childGenreFolder, iTunesMusicLibrary folder, Dictionary<string, string> data)
        {
            if (!string.IsNullOrEmpty(data["Genre"]))
            {
                if (dictionaryGenre.ContainsKey(data["Genre"]))
                    childGenreFolder = dictionaryGenre[data["Genre"]];
                else
                {
                    childGenreFolder = new iTunesGenre();
                    childGenreFolder.GenreName = data["Genre"];
                    childGenreFolder.Id = childGenreFolder.Name.GetMD5();
                    folder.Genres.Add(childGenreFolder);
                    dictionaryGenre.Add(data["Genre"], childGenreFolder);
                }
            }
            return childGenreFolder;
        }

        private static iTunesArtist BuildArtist(Dictionary<string, iTunesArtist> dictionaryArtist, iTunesGenre childGenreFolder, iTunesMusicLibrary folder, Dictionary<string, string> data)
        {
            iTunesArtist childArtistFolder;
            if (dictionaryArtist.ContainsKey(data["Artist"]))
                childArtistFolder = dictionaryArtist[data["Artist"]];
            else
            {
                childArtistFolder = new iTunesArtist();
                childArtistFolder.ArtistName = data["Artist"];
                childArtistFolder.Id = childArtistFolder.Name.GetMD5();

                if (childGenreFolder != null && !string.IsNullOrEmpty(data["Genre"]))
                {
                    childGenreFolder.Artists.Add(childArtistFolder);
                }
                folder.Artists.Add(childArtistFolder);
                dictionaryArtist.Add(data["Artist"], childArtistFolder);
            }
            return childArtistFolder;
        }

        private static iTunesAlbum BuildAlbum(Dictionary<string, iTunesAlbum> dictionaryAlbum, iTunesArtist childArtistFolder, Dictionary<string, string> data)
        {
            iTunesAlbum childAlbumFolder;
            if (dictionaryAlbum.ContainsKey(string.Concat(data["Artist"], data["Album"])))
                childAlbumFolder = dictionaryAlbum[string.Concat(data["Artist"], data["Album"])];
            else
            {
                childAlbumFolder = new iTunesAlbum();
                childAlbumFolder.Name = data["Album"];
                childAlbumFolder.AlbumName = data["Album"];
                childAlbumFolder.Id = string.Concat(data["Artist"], data["Album"]).GetMD5();
                childArtistFolder.Albums.Add(childAlbumFolder);
                childAlbumFolder.Parent = childArtistFolder;
                dictionaryAlbum.Add(string.Concat(data["Artist"], data["Album"]), childAlbumFolder);
            }
            return childAlbumFolder;
        }

        private static iTunesSong BuildSong(iTunesArtist childArtistFolder, iTunesAlbum childAlbumFolder, Dictionary<string, string> data)
        {            
            iTunesSong newSong = new iTunesSong();
            newSong.SongName = data["Name"];
            newSong.Name = data["Name"];
            newSong.Path = data["Location"];
            newSong.Id = newSong.Path.GetMD5();
            childAlbumFolder.Songs.Add(newSong);
            childAlbumFolder.Parent = childArtistFolder;
            return newSong;
        }

        static void ReadFileImage(iTunesAlbum folder, iTunesSong song)
        {
            if (folder.HasImage)
                return;

            string uncFilePath = new Uri(song.Path).LocalPath;
            uncFilePath = uncFilePath.Replace("\\\\localhost\\", " ").Trim();

            if (!System.IO.File.Exists(uncFilePath))
                return;

            string folderPath = Path.Combine(Path.GetDirectoryName(uncFilePath),"folder.jpg");

            if (System.IO.File.Exists(folderPath))
                folder.PrimaryImagePath = folderPath;
            
            string backPath = Path.Combine(Path.GetDirectoryName(uncFilePath), "Backdrop.jpg");

            if (System.IO.File.Exists(backPath))
                folder.BackdropImagePath = backPath;
            else
            {
                backPath = Path.Combine(Directory.GetParent(Path.GetDirectoryName(uncFilePath)).FullName,"Backdrop.jpg");
                if (System.IO.File.Exists(backPath))
                {
                    folder.BackdropImagePath = backPath;
                    if (folder.Parent != null)
                        folder.Parent.BackdropImagePath = backPath;
                }
            }

            backPath = Path.Combine(Directory.GetParent(Path.GetDirectoryName(uncFilePath)).FullName, "Backdrop.jpg");
            if (System.IO.File.Exists(backPath))
            {
                if (folder.Parent != null)
                    folder.Parent.BackdropImagePath = backPath;
            }


        }

    }
}
