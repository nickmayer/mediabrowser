using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MediaBrowser.LibraryManagement;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;
using MediaBrowser.Library.Playables;
using MusicPlugin.Library.Entities;

namespace MusicPlugin.Library.Playables
{
    class PlayableMultiFileMusic : PlayableItem
    {
        Music music;
        string playListFile;
        List<string> musicFiles;
        PlayableExternal playableExternal = null;


        public PlayableMultiFileMusic(Media media)
            : base()
        {
            this.music = media as Music;
        }

        public override void Prepare(bool resume)
        {

            musicFiles = music.MusicFiles.ToList();
            if (musicFiles.Count == 1)
            {
                playListFile = musicFiles[0];
                if (PlayableExternal.CanPlay(playListFile))
                    this.playableExternal = new PlayableExternal(playListFile);
            }
            else
            {
                musicFiles.Sort();
                int pos = 0;
                if (resume)
                    pos = PlayState.PlaylistPosition;

                if (PlayableExternal.CanPlay(musicFiles[0]))
                {
                    playListFile = Path.Combine(ApplicationPaths.AutoPlaylistPath, music.Name + ".pls");
                    StringBuilder contents = new StringBuilder("[playlist]\n");
                    int x = 1;
                    foreach (string file in musicFiles)
                    {
                        if (pos > 0)
                            pos--;
                        else
                        {
                            contents.Append("File" + x + "=" + file + "\n");
                            contents.Append("Title" + x + "=Part " + x + "\n\n");
                        }
                        x++;
                    }
                    contents.Append("Version=2\n");

                    System.IO.File.WriteAllText(playListFile, contents.ToString());
                    this.playableExternal = new PlayableExternal(playListFile);
                }
                else
                {
                    playListFile = CreateWPLPlaylist(music.Name, musicFiles.Skip(pos));
                }
            }

        }

        public override bool UpdatePosition(string title, long positionTicks)
        {
            Logger.ReportVerbose("Updating multi file position for " + title + " position " + positionTicks);

            if (title == null || musicFiles == null)
                return false;

            int i = 0;
            foreach (var filename in musicFiles)
            {
                if (title.StartsWith(Path.GetFileNameWithoutExtension(filename)))
                {
                    PlayState.PlaylistPosition = i;
                    PlayState.PositionTicks = positionTicks;
                    PlayState.Save();
                    return true;
                }
                i++;
            }

            return false;
        }

        public override string Filename
        {
            get { return playListFile; }
        }

        public static bool CanPlay(Media media)
        {
            return (media is Music) && (media as Music).MusicFiles.Count() > 1;
        }
    }
}
