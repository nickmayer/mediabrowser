﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MediaBrowser.Library.ImageManagement;

namespace FrameGrabProvider {
    class GrabImage : LibraryImage {

        protected override string LocalFilename {
            get {
                return System.IO.Path.Combine(cachePath, Id.ToString() + ".png");
            }
        }

        public override string GetLocalImagePath() {
            lock (Lock) {
                if (File.Exists(LocalFilename)) {
                    return LocalFilename;
                }

                // path without grab://
                string video = this.Path.Substring(7); 

                ThumbCreator.CreateThumb(video, LocalFilename, 0.2);
                return LocalFilename;
            }

            return null;
        }
    }
}
