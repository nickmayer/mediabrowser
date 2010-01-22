using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Library.ImageManagement
{
    public class RemoteProcessedImage : RemoteImage
    {
        protected BaseItem item;

        public RemoteProcessedImage(BaseItem parentItem)
        {
            item = parentItem;
        }

        protected override void ProcessImage()
        {
            //testing
            Logger.ReportInfo("Processing remote image " + LocalFilename + " for " + item.Name);
            //if we have an image processor, call it
            if (Kernel.Instance.ImageProcessor != null)
            {
                Kernel.Instance.ImageProcessor(LocalFilename, item);
            }
        }
    }
}
