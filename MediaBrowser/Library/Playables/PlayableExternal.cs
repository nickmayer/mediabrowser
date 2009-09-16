﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using MediaBrowser.Library.Entities;
using System.Linq;

namespace MediaBrowser.Library.Playables
{
    public class PlayableExternal : PlayableItem
    {
        private static object lck = new object();
        private static Dictionary<MediaType, ConfigData.ExternalPlayer> configuredPlayers = null;
        private string path;
        public PlayableExternal(Media media)
        {
            this.path = (media as Video).VideoFiles.ToArray()[0];
        }

        public PlayableExternal(string path) {
            this.path = path;
        }

        public override void Prepare(bool resume) { }

        public override string Filename
        {
            get { return path; }
        }

        protected override void PlayInternal(bool resume)
        {
            if (PlaybackController.IsPlaying) {
                PlaybackController.Pause();
            }
              
            MediaType type  = MediaTypeResolver.DetermineType(path);
            ConfigData.ExternalPlayer p = configuredPlayers[type];
            string args = string.Format(p.Args, path);
            Process.Start(p.Command, args);
            MarkWatched();
        }

        public static bool CanPlay(Media media)
        {
            bool canPlay = false;
            var video = media as Video;

            if (video != null) {
                var files = video.VideoFiles.ToArray();
                if (files.Length == 1) {
                    canPlay = CanPlay(files[0]);
                }
            }
            return canPlay;
        }

        public static bool CanPlay(string path)
        { 
            if (RunningOnExtender)
                return false;
            if (configuredPlayers==null)
                lock(lck)
                    if (configuredPlayers==null)
                        LoadConfig();
            MediaType type = MediaTypeResolver.DetermineType(path);
            if (configuredPlayers.ContainsKey(type))
                return true;
            else
                return false;
        }

        private static void LoadConfig()
        {
 	        configuredPlayers = new Dictionary<MediaType,ConfigData.ExternalPlayer>();
            if (Config.Instance.ExternalPlayers!=null)
                foreach(var x in Config.Instance.ExternalPlayers)
                    configuredPlayers[x.MediaType] = x;
        }

        
    }
}
