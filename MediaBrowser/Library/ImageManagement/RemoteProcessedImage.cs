using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library.ImageManagement
{
    public class RemoteProcessedImage : RemoteImage
    {
        private BaseItem item;

        public RemoteProcessedImage(BaseItem parentItem)
        {
            item = parentItem;
        }

        protected override void ProcessImage()
        {
            //if we have an image processor, call it
            if (Kernel.Instance.ImageProcessor != null)
            {
                Kernel.Instance.ImageProcessor(LocalFilename, item);
            }
        }
    }
}
