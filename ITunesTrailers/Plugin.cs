﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;

namespace ITunesTrailers {
    public class Plugin : BasePlugin {

        static readonly Guid TrailersGuid = new Guid("{828DCFEF-AEAF-44f2-B6A8-32AEAF27F3DA}");

        public override void Init(Kernel config) {
            var trailers = new ITunesTrailerFolder();
            trailers.Name = "Trailers";
            trailers.Path = "";
            trailers.Id = TrailersGuid;
            config.RootFolder.AddVirtualChild(trailers);
        }

        public override string Name {
             get { return "ITunes Trailers"; }
        }

        public override string Description {
            get { return "HD Trailers powered by Apple."; }
        }

    }
}
