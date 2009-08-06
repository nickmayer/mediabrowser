using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.LibraryManagement;
using System.IO;
using MediaBrowser.Library.Entities;
using System.Linq;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;
using MusicPlugin.Library.Entities;

namespace MusicPlugin.Library.Playables
{
    class PlayableMusicFile : PlayableItem
    {
        Music music;
        string path;
        public PlayableMusicFile(Media media)
            : base()
        {
            this.music = media as Music;
            this.path = music.MusicFiles.First();
        }

        public override void Prepare(bool resume)
        {

        }

        public override string Filename
        {
            get { return path; }
        }

        public static bool CanPlay(Media media)
        {
            return media is Music && (media as Music).MusicFiles.Count() == 1;
        }
    }
}
