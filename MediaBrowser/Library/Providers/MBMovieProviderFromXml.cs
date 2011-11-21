using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using MediaBrowser.Library.Interfaces;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Library.Providers
{
    [SupportedType(typeof(Movie))]
    public class MBMovieProviderFromXml : MovieDbProvider
    {

        [Persist]
        DateTime lastWriteTime = DateTime.MinValue;

       
        public override bool NeedsRefresh()
        {

            string mfile = XmlLocation();
            if (!File.Exists(mfile))
                return false;

          
            DateTime modTime = new FileInfo(mfile).LastWriteTimeUtc;
            if (modTime <= lastWriteTime)
               return false;

            Logger.ReportVerbose("XML changed for " + Item.Name + " mod time: " + modTime + " last update time: " + lastWriteTime);
            return true;
        }

        protected virtual string XmlLocation()
        {
            return Path.Combine(Item.Path, LOCAL_META_FILE_NAME);
        }

        public override void Fetch()
        {
            string metaFile = XmlLocation();
            if (File.Exists(metaFile))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(metaFile);
                ProcessDocument(doc, true);
                lastWriteTime = new FileInfo(metaFile).LastWriteTimeUtc + TimeSpan.FromMinutes(1); //fudge this slightly because the system sometimes reports this a few seconds behind
            }
        }

    }
}
