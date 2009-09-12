﻿using System.IO;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Entities;
using MusicPlugin.Util;
using System;
namespace MusicPlugin.LibraryManagement
{
    public static class MusicHelper
    {
        public static bool IsHidden(string filename)
        {
            if (Path.HasExtension(filename))
                return (new System.IO.FileInfo(filename).Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
            else
                return (new System.IO.DirectoryInfo(filename).Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
        }

        public static bool IsArtistFolder(this IMediaLocation location)
        {
            IFolderMediaLocation folder = location as IFolderMediaLocation;
            if (folder != null)
            {
                if (Path.HasExtension(folder.Path))
                    return false;

                if (MusicHelper.IsAlbumFolder(folder.Path))
                    return false;

                DirectoryInfo directoryInfo = new DirectoryInfo(folder.Path);
                foreach (DirectoryInfo directory in directoryInfo.GetDirectories())
                    if (IsAlbumFolder(directory.FullName))
                        return true;

                return false;


            }
            return false;
        }

        public static bool IsPlaylistFolder(string folder)
        {
            return true;
        }

        public static bool IsMusic(string filename)
        {
            string extension = System.IO.Path.GetExtension(filename).ToLower();

            switch (extension)
            {
                case ".mp3":
                case ".wma":
                case ".acc":
                case ".wpl":
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsAlbumFolder(string path)
        {
            if (Path.HasExtension(path))
                return false;

            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            foreach (System.IO.FileInfo fileInfo in directoryInfo.GetFiles())
                if (MusicHelper.IsMusic(fileInfo.FullName))
                    return true;

            return false;
        }

        public static bool IsArtistAlbumFolder(string path)
        {
            if (Path.HasExtension(path))
                return false;

            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            foreach (System.IO.FileInfo fileInfo in directoryInfo.GetFiles())
                if (MusicHelper.IsMusic(fileInfo.FullName))
                    return true;

            return false;
        }

        private static Folder _playlistFolder;
        public static Folder GetPlaylistFolder()
        {
            if (_playlistFolder == null)
            {
                _playlistFolder = new Folder();
                _playlistFolder.Name = Settings.Instance.PlayListFolderName;
                _playlistFolder.Path = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Playlists");
            }
            return _playlistFolder;
        }

    }
}

