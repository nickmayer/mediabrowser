using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Persistance;

namespace MusicPlugin.Library.Entities
{

    // our provider seem to think a series is a type of season, so the entity is reflecting that
    // further down the line this may change

    public class Album : Artist
    {

        [Persist]
        public string AlbumName { get; set; }
    }
}
