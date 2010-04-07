using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using MediaBrowser.Library.ImageManagement;
using System.IO;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Entities;

namespace MtnFrameGrabProvider {
    class GrabImage : FilesystemImage {

        protected override bool ImageOutOfDate(DateTime date) {
            return false;
        }

        protected override Image OriginalImage {
            get {

                string video = this.Path.Substring(10);

                Logger.ReportInfo("Trying to extract mtn thumbnail for " + video);

                string tempFile = ""; //this will get filled in by the thumb grabber

                //try to grab thumb 10% into the file - if we don't know runtime then default to 5 mins
                int secondsIn = 0;
                Video videoItem = item as Video;
                if (videoItem != null) {
                    if (videoItem.MediaInfo != null && videoItem.MediaInfo.RunTime > 0) {
                        secondsIn = (Int32)((videoItem.MediaInfo.RunTime / 10) * 60);
                    }
                }
                if (secondsIn == 0)
                {
                    if (!Int32.TryParse(Plugin.PluginOptions.Instance.SecondsIn, out secondsIn)) secondsIn = 300;
                }
                Logger.ReportInfo("Looking " + secondsIn + " seconds into video.");
                if (ThumbCreator.CreateThumb(video, ref tempFile, secondsIn)) {
                    if (File.Exists(tempFile)) {
                        //load image and pass to processor
                        return Image.FromFile(tempFile);
                    } else {
                        Logger.ReportError("Unable to process thumb image for " + video);
                        return null;
                    }
                } else {
                    Logger.ReportWarning("Failed to grab mtn thumbnail for " + video);
                    return null;
                }
            }
        }

    }
}
