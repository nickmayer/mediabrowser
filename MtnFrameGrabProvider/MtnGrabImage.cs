﻿using System;
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

                if (ThumbCreator.CreateThumb(video, LocalFilename, 600)) {
                    if (File.Exists(LocalFilename))
                    {
                        //load image and pass to processor
                        Image img = Image.FromFile(LocalFilename);
                        img = ProcessImage(img);
                        img.Save(LocalFilename);
                    }
                    return LocalFilename;
                } else {
                    Logger.ReportWarning("Failed to grab mtn thumbnail for " + video);
                    this.Corrupt = true;
                    return null;
                }

            }

        }
    }
}
