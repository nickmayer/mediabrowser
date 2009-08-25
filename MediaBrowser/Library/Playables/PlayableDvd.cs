using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library.Playables
{
    class PlayableDvd : PlayableItem
    {
        Video video;

        public PlayableDvd(Media media)
            : base()
        {
            this.video = media as Video;
        }

        public override void Prepare(bool resume)
        {
        }

        public override void Play(string file) {
            this.PlaybackController.PlayDVD(file);
        }

        public override string Filename
        {
            get { return video.Path; }
        }

        public static bool CanPlay(Media media)
        {
            return (media is Video) && (media as Video).MediaType == MediaType.DVD;
        }
    }
}
