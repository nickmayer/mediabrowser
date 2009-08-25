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
        [Persist]
        public string ArtistAlbumName { get; set; }
    }
}
