using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Entities;

namespace MusicPlugin.Library.Entities
{
    public class ArtistAlbum : Folder
    {
        [Persist]//List<Actor>
        public string ArtistAlbumName { get; set; }

        [Persist]
        public string Genre { get; set; }

        [Persist]
        public int? RunningTime { get; set; }

        [Persist]
        public string Status { get; set; }
    }
}
