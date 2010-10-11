using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Metadata;
using MediaBrowser.Library.Persistance;

namespace MBTrailers {
    public class ITunesTrailer : Movie {

        [Persist]
        public string RealPath { get; set; }

        public override bool RefreshMetadata(MetadataRefreshOptions options) {
            // just refresh images - metadata is assigned external to the provider framework
            if ((options & MetadataRefreshOptions.Force) == MetadataRefreshOptions.Force)
            {
                var images = new List<MediaBrowser.Library.ImageManagement.LibraryImage>();
                images.Add(PrimaryImage);
                images.Add(SecondaryImage);
                images.Add(BannerImage);
                images.AddRange(BackdropImages);

                foreach (var image in images)
                {
                    try
                    {
                        if (image != null)
                        {
                            image.ClearLocalImages();
                            MediaBrowser.Library.Factories.LibraryImageFactory.Instance.ClearCache(image.Path);
                        }
                    }
                    catch (Exception ex)
                    {
                        MediaBrowser.Library.Logging.Logger.ReportException("Failed to clear local image (its probably in use)", ex);
                    }
                }
            }
            return false;
        }
    }
}
