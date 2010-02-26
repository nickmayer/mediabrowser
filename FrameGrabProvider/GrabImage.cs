using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using MediaBrowser.Library.ImageManagement;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Entities;

namespace FrameGrabProvider {
    class GrabImage : FilesystemImage {

        protected override bool ImageOutOfDate(DateTime date) {
            return false;
        }

        protected override Image OriginalImage {
            get {
                string video = this.Path.Substring(7);

                Logger.ReportInfo("Trying to extract thumbnail for " + video);

                string localFilename = System.IO.Path.GetTempFileName() + ".png";

                if (ThumbCreator.CreateThumb(video, localFilename, 0.2)) {
                    if (File.Exists(localFilename)) {
                        //load image and pass to processor
                        return Image.FromFile(localFilename);
                    }
                } 
                
                Logger.ReportWarning("Failed to grab thumbnail for " + video);
                return null;
            }
        }

    }
}
