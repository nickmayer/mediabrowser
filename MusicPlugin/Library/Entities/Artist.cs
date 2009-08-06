using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Entities;

namespace MusicPlugin.Library.Entities
{
     public class Artist : Folder
    {
        [Persist]//List<Actor>
        public string ArtistName { get; set; }

        [Persist]
        public string Genre { get; set; }

        [Persist]
        public int? RunningTime { get; set; }

        [Persist]
        public string Status { get; set; }
    }
}
