using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Library.ImageManagement
{
    public class FilesystemProcessedImage : FilesystemImage
    {
        protected BaseItem item;

        protected FilesystemProcessedImage() { }

        public FilesystemProcessedImage(BaseItem parentItem)
        {
            item = parentItem;
        }

        protected override void ProcessImage()
        {
            //testing
            Logger.ReportInfo("Processing local image " + LocalFilename + " for " + item.Name);
            //if we have an image processor, call it
            if (Kernel.Instance.ImageProcessor != null)
            {
                Kernel.Instance.ImageProcessor(LocalFilename, item );
            }
        }
    }
}
