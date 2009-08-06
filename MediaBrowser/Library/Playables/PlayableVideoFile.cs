using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.LibraryManagement;
using System.IO;
using MediaBrowser.Library.Entities;
using System.Linq;

namespace MediaBrowser.Library.Playables
{
    class PlayableVideoFile : PlayableItem
    {
        Video video;
        string path;
        public PlayableVideoFile(Media media)
            : base()
        {
            this.video = media as Video;
            this.path = this.video.VideoFiles.First();
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
            // can play DVDs and normal videos
            return (media is Video) && (media as Video).VideoFiles.Count() == 1 && !((media as Video).ContainsRippedMedia);
        }
    }
}
