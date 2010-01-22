using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MediaBrowser.Library.ImageManagement;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Entities;

namespace FrameGrabProvider {
    class GrabImage : FilesystemProcessedImage {

        public GrabImage(BaseItem parentItem)
        {
            this.item = parentItem;
        }

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

                Logger.ReportInfo("Trying to extract thumbnail for " + video);

                if (ThumbCreator.CreateThumb(video, LocalFilename, 0.2)) {
                    ProcessImage();
                    return LocalFilename;
                } else {
                    Logger.ReportWarning("Failed to grab thumbnail for " + video);
                    return null;
                }

            }

        }
    }
}
