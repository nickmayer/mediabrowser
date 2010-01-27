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
    class GrabImage : FilesystemProcessedImage {

        public GrabImage(BaseItem parentItem)
        {
            this.item = parentItem;
        }

        protected override string LocalFilename
        {
            get {
                return System.IO.Path.Combine(cachePath, Id.ToString() + ".jpg");
            }
        }

        public override string GetLocalImagePath() {
            lock (Lock) {
                if (File.Exists(LocalFilename)) {
                    return LocalFilename;
                }

                // path without mtngrab://
                string video = this.Path.Substring(10);

                Logger.ReportInfo("Trying to extract mtn thumbnail for " + video);

                string tempFile = ""; //this will get filled in by the thumb grabber

                //try to grab thumb 10% into the file - if we don't know runtime then default to 10 mins
                int secondsIn = 600;
                Video videoItem = item as Video;
                if (videoItem != null)
                {
                    if (videoItem.MediaInfo.RunTime > 0)
                    {
                        secondsIn = (Int32)((videoItem.MediaInfo.RunTime / 10)*60);
                    }
                }

                if (ThumbCreator.CreateThumb(video, ref tempFile, secondsIn)) {
                    if (File.Exists(tempFile))
                    {
                        //load image and pass to processor
                        Image img = Image.FromFile(tempFile);
                        img = ProcessImage(img);
                        img.Save(LocalFilename);
                        return LocalFilename;
                    }
                    else
                    {
                        Logger.ReportError("Unable to process thumb image for " + video);
                        this.Corrupt = true;
                        return null;
                    }
                } else {
                    Logger.ReportWarning("Failed to grab mtn thumbnail for " + video);
                    this.Corrupt = true;
                    return null;
                }

            }

        }
    }
}
