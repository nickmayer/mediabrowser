using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Metadata;
using MediaBrowser.Library.Persistance;

namespace MBTrailers {
    public class ITunesTrailer : Movie {

        [Persist]
        public string RealPath { get; set; }

        public override bool RefreshMetadata(MetadataRefreshOptions options) {
            // do nothing, metadata is assigned external to the provider framework
            return false;
        }
    }
}
