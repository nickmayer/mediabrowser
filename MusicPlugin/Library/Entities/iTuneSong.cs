using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;

namespace MusicPlugin.Library.Entities
{
    public class iTunesSong : Song
    {
        public string SongName;
        //public string Location;
        public override bool RefreshMetadata(MediaBrowser.Library.Metadata.MetadataRefreshOptions options)
        {
            // do nothing metadata is assigned externally.
            return false;
        }
    }
}
