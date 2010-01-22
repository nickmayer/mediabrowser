using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
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

        protected override void CacheImage(System.IO.MemoryStream ms)
        {
            Image image = this.ProcessImage(Image.FromStream(ms));
            image.Save(LocalFilename);

        }

        protected override Image ProcessImage(Image rootImage)
        {
            //testing
            //Logger.ReportInfo("Processing remote image " + LocalFilename + " for " + item.Name);
            //if we have an image processor, call it
            if (Kernel.Instance.ImageProcessor != null)
            {
                return Kernel.Instance.ImageProcessor(rootImage, item);
            } 
            else return rootImage;
        }
    }
}
