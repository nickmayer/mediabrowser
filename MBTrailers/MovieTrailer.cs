using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Metadata;
using MediaBrowser.Library.Persistance;

namespace MBTrailers
{
    class MovieTrailer : Movie
    {
        [Persist]
        public Movie RealMovie;


        public override bool RefreshMetadata(MediaBrowser.Library.Metadata.MetadataRefreshOptions options)
        {
            MediaBrowser.Library.Logging.Logger.ReportInfo("Refreshing trailer for " + RealMovie.Name);
            this.PrimaryImagePath = Util.CloneImage(RealMovie.PrimaryImagePath);
            this.BackdropImagePaths = new List<string>();
            if (RealMovie.BackdropImagePaths != null)
            {
                foreach (string backdrop in RealMovie.BackdropImagePaths)
                {
                    this.BackdropImagePaths.Add(backdrop);
                }
            }
            if ((options & MetadataRefreshOptions.Force) == MetadataRefreshOptions.Force)
            {
                var images = new List<MediaBrowser.Library.ImageManagement.LibraryImage>();
                images.Add(PrimaryImage);
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
                //changed = RealMovie.RefreshMetadata(options);
            }

        
            this.Overview = RealMovie.Overview;
            this.ProductionYear = RealMovie.ProductionYear;
            this.RunningTime = RealMovie.RunningTime;
            this.Actors = RealMovie.Actors;
            this.Directors = RealMovie.Directors;
            this.Name = RealMovie.Name + " " + Plugin.PluginOptions.Instance.TrailerSuffix;
            this.SortName = RealMovie.SortName;
            this.Studios = RealMovie.Studios;
            this.Path = RealMovie.TrailerFiles.First();
            this.MediaInfo = RealMovie.MediaInfo;
            this.DateCreated = System.IO.File.GetCreationTime(this.Path);
            this.DisplayMediaType = "Trailer";

            return true;
        }

        
    }
}
