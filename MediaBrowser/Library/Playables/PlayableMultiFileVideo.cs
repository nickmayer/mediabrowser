﻿using System;
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

namespace MediaBrowser.Library.Playables
{
    class PlayableMultiFileVideo : PlayableItem
    {
        Video video;
        string playListFile;
        List<string> videoFiles;
        PlayableExternal playableExternal = null;

        
        public PlayableMultiFileVideo(Media media)
            : base()
        {
            this.video = media as Video;
        }

        public override void Prepare(bool resume)
        {
      
            videoFiles = video.VideoFiles.ToList();
            if (videoFiles.Count==1)
            {
                playListFile = videoFiles[0];
                if (PlayableExternal.CanPlay(playListFile))
                    this.playableExternal = new PlayableExternal(playListFile);
            }
            else
            {
                videoFiles.Sort();
                int pos = 0;
                if (resume)
                    pos = PlayState.PlaylistPosition;

                if (PlayableExternal.CanPlay(videoFiles[0]))
                {
                    playListFile = Path.Combine(ApplicationPaths.AutoPlaylistPath, video.Name + ".pls");
                    StringBuilder contents = new StringBuilder("[playlist]\n");
                    int x = 1;
                    foreach (string file in videoFiles)
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
                    playListFile = CreateWPLPlaylist(video.Name, videoFiles.Skip(pos));
                }
            }
            
        }

        public override bool UpdatePosition(string title, long positionTicks)
        {
            Logger.ReportVerbose("Updating multi file position for " + title + " position " + positionTicks);

            if (title == null || videoFiles == null) 
                return false; 
            
            int i = 0;
            foreach (var filename in videoFiles)
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
            return (media is Video) && (media as Video).VideoFiles.Count() > 1;
        }

        protected override void PlayInternal(bool resume)
        {
            if (this.playableExternal != null)
                this.playableExternal.Play(this.PlayState, resume);
            else
                base.PlayInternal(resume);
        }
    }
}
