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
        [Persist]
        public string ArtistName { get; set; }
    }
}
