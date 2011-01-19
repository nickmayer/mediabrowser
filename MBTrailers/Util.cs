using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MediaBrowser;
using MediaBrowser.Library;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Logging;

namespace MBTrailers
{
    public static class Util
    {
        public static string ImageCache = Path.Combine(ApplicationPaths.AppPluginPath, "MyTrailersCache");

        public static string CloneImage(string path)
        {
            string newPath = Path.Combine(ImageCache, path.GetMD5().ToString()+Path.GetExtension(path));
            try
            {
                File.Copy(path, newPath, true);
            }
            catch (Exception e)
            {
                Logger.ReportException("Error trying to create image: " + newPath, e);
            }
            return newPath;
        }
    }
}
