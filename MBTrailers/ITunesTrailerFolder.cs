using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using System.Globalization;
using System.Net;
using System.Xml;
using System.IO;
using MediaBrowser;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Entities.Attributes;
using MediaBrowser.Library;
using WebProxy;
using WebProxy.WCFInterfaces;

namespace MBTrailers {
    public class MBTrailerFolder : Folder {
        // update once a day
        const int UpdateMinuteInterval = 60 * 24;
        const string MBTrailerUrl = @"http://www.mediabrowser.tv/trailers?key={0}&hidef={1}";

        //[Persist]
        //[NotSourcedFromProvider]
        //List<BaseItem> children = new List<BaseItem>();

        [Persist]
        [NotSourcedFromProvider]
        DateTime lastUpdated = DateTime.MinValue;

        internal void RefreshProxy()
        {
            foreach (var item in ActualChildren)
            {
                var trailer = item as ITunesTrailer;
                if (trailer != null && Plugin.proxy != null)
                {
                    trailer.Path = Plugin.proxy.ProxyUrl(trailer);
                    Kernel.Instance.ItemRepository.SaveItem(trailer);
                }
            }
        }

        public override string ParentalRating
        {
            get
            {
                return "None"; //only way this can be assigned
            }
        }

        public override string Name
        {
            get {
                return Plugin.PluginOptions != null ? Plugin.PluginOptions.Instance.MenuName : "MB Trailers";
            }
            set {
                
            }
        }

        //protected override List<BaseItem> ActualChildren {
        //    get {
        //        if (lastUpdated == DateTime.MinValue) {
        //            ValidateChildren();
        //        }
        //        return children;
        //    }
        //}

        public override void  ValidateChildren()
        {
            if (!Plugin.PluginOptions.Instance.Changed && Math.Abs((lastUpdated - DateTime.Now).TotalMinutes) < UpdateMinuteInterval) return;
            Logger.ReportInfo("MBTrailers Last Updated: "+lastUpdated+" Changed: "+Plugin.PluginOptions.Instance.Changed+" Trailers Validating Children...");
            Plugin.PluginOptions.Instance.Changed = false; //reset this
            try
            {
                Plugin.PluginOptions.Save();
            }
            catch
            {
                Logger.ReportInfo("Unable to save MBTrailers configuration");
            }
            base.ValidateChildren();
            RefreshProxy();
            lastUpdated = DateTime.Now;
            Kernel.Instance.ItemRepository.SaveItem(this);

        }

        protected override List<BaseItem> GetNonCachedChildren()
        {

            try {
                // load the xml
                List<BaseItem> newChildren = GetTrailerChildren();
                if (newChildren.Count == 0) //probably just a problem getting to the site - don't blow away what we have
                {
                    Logger.ReportError("MB Trailers returned zero children - not updating existing trailers.");
                    return newChildren;
                }
                return newChildren;
                
            } catch (Exception err) {
                Logger.ReportException("Failed to update trailers", err);
                return new List<BaseItem>();
            }
        }

        public void CleanCache()
        {
            //some safety validations to be sure we don't accidentally clear out downloaded trailers because of a load failure
            if (this.ActualChildren.Count < 5) return;
            //clear files no longer referenced from our cache
            //first build a list of files that are there
            var cacheFiles = new DirectoryInfo(Plugin.proxy.CacheDirectory).GetFiles();
            //then go through and match them up to current items
            Logger.ReportInfo("MBTrailers - Clearing cache of old trailers.");
            foreach (var file in cacheFiles)
            {
                MediaBrowser.Library.Entities.BaseItem item = this.ActualChildren.Find(i => System.IO.Path.GetFileName(i.Path) == file.Name);
                if (item == null && (DateTime.UtcNow - file.CreationTimeUtc) > TimeSpan.FromDays(60)) //if not there and old
                {
                    try
                    {
                        File.Delete(file.FullName); // not in our children anymore clean it up
                        Logger.ReportInfo("MBTrailers deleted old trailer download from cache: " + file.FullName);
                    }
                    catch (Exception e)
                    {
                        Logger.ReportException("Unable to clear file from trailercache: " + file.FullName, e);
                    }
                }
            }
        }

        private List<BaseItem> GetTrailerChildren() {

            List<BaseItem> children = new List<BaseItem>();

            using (WebClient client = new WebClient()) {
                //Logger.ReportInfo("Getting Trailers from: "+String.Format(MBTrailerUrl,Config.Instance.SupporterKey, Plugin.PluginOptions.Instance.HDTrailers.ToString()));
                using (Stream stream = client.OpenRead(String.Format(MBTrailerUrl,Config.Instance.SupporterKey, Plugin.PluginOptions.Instance.HDTrailers.ToString()))) {
                    XmlTextReader reader = new XmlTextReader(stream);

                    reader.Read();

                    ITunesTrailer trailer = null;

                    DateTimeFormatInfo dateFormat = new DateTimeFormatInfo();
                    dateFormat.ShortDatePattern = "yyyy-MM-dd";

                    while (reader.Read()) {
                        if (reader.NodeType == XmlNodeType.Element) {
                            switch (reader.Name) {
                                case "movieinfo":
                                    if (trailer != null)
                                    {
                                        //found a new entry - add last one
                                        children.Add(trailer);
                                    }

                                    trailer = new ITunesTrailer() { DisplayMediaType = "Trailer" };

                                    // trailer.Id = reader.GetAttribute(0);
                                    break;

                                case "title":
                                    trailer.Name = ReadToValue(reader);
                                    trailer.Id = trailer.Name.GetMD5();
                                    break;

                                case "runtime":
                                    // trailer.RunningTime = ReadToValue(reader);
                                    break;

                                case "rating":
                                    trailer.MpaaRating = ReadToValue(reader);
                                    break;

                                case "studio":
                                    if (trailer.Studios == null)
                                        {
                                            trailer.Studios = new List<string>();
                                        }
                                        trailer.Studios.Add(Name = ReadToValue(reader));                                     
                                    break;

                                case "postdate":
                                    trailer.DateCreated = DateTime.Parse(ReadToValue(reader), dateFormat);
                                    break;

                                case "releasedate":
                                    //  trailer.ProductionYear = DateTime.Parse(ReadToValue(reader), dateFormat).Year;
                                    break;

                                case "director":
                                    trailer.Directors = new List<string>() { ReadToValue(reader) };
                                    break;

                                case "description":
                                    trailer.Overview = ReadToValue(reader);
                                    break;

                                case "cast":
                                    while (reader.Read()) {
                                        if (reader.NodeType == XmlNodeType.EndElement &&
                                            reader.Name == "cast")
                                            break;

                                        if (reader.Name == "name") {
                                            if (trailer.Actors == null) {
                                                trailer.Actors = new List<Actor>();
                                            }
                                            trailer.Actors.Add(new Actor() { Name = ReadToValue(reader) });
                                        }
                                    }
                                    break;

                                case "genre":
                                    while (reader.Read()) {
                                        if (reader.NodeType == XmlNodeType.EndElement &&
                                            reader.Name == "genre")
                                            break;

                                        if (reader.Name == "name") {
                                            if (trailer.Genres == null) {
                                                trailer.Genres = new List<string>();
                                            }
                                            trailer.Genres.Add(ReadToValue(reader));
                                        }
                                    }
                                    break;

                                case "poster":
                                    while (reader.Read()) {
                                        if (reader.NodeType == XmlNodeType.EndElement &&
                                            reader.Name == "poster")
                                            break;

                                        if (reader.Name == "xlarge") {
                                            trailer.PrimaryImagePath = ReadToValue(reader);
                                        }
                                    }
                                    break;

                                case "preview":
                                    while (reader.Read()) {
                                        if (reader.NodeType == XmlNodeType.EndElement &&
                                            reader.Name == "preview")
                                            break;

                                        if (reader.Name == "large") {
                                            trailer.RealPath = ReadToValue(reader); //.Replace("movies.apple.com", "www.apple.com");
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    if (trailer != null)
                        children.Add(trailer); //add the last one we built
                }
            }

            return children;
        }

        private static string ReadToValue(XmlTextReader reader) {
            while (reader.Read())
                if (reader.NodeType == XmlNodeType.Text)
                    break;

            return reader.ReadContentAsString();
        }
    }
}
