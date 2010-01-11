using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using System.Net;
using System.Xml.XPath;
using System.Text.RegularExpressions;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library;
using MediaBrowser.Library.Extensions;
using MediaBrowser;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Entities.Attributes;


namespace FilmTrailerPlugin
{
    public class FilmTrailerFolder : Folder
    {
        public static string FeedDefault = @"http://uk.feed.filmtrailer.com/v2.0/?ListType=Latest30InCinema&channel_user_id=441100001-1";

        public string Feed
        {
            get
            {
                string _feed = FeedDefault;
                switch (Plugin.PluginOptions.Instance.TrailerSource)
                {
                    case "Australia": _feed = @"http://au.feed.filmtrailer.com/v2.0/?ListType=Latest30InCinema&channel_user_id=441100001-1"; break;
                    case "Denmark": _feed = @"http://dk.feed.filmtrailer.com/v2.0/?ListType=Latest30InCinema&channel_user_id=441100001-1"; break;
                    case "Finland": _feed = @"http://fi.feed.filmtrailer.com/v2.0/?ListType=Latest30InCinema&channel_user_id=441100001-1"; break;
                    case "France": _feed = @"http://fr.feed.filmtrailer.com/v2.0/?ListType=Latest30InCinema&channel_user_id=441100001-1"; break;
                    case "Germany": _feed = @"http://de.feed.filmtrailer.com/v2.0/?ListType=Latest30InCinema&channel_user_id=441100001-1"; break;
                    case "Italy": _feed = @"http://it.feed.filmtrailer.com/v2.0/?ListType=Latest30InCinema&channel_user_id=441100001-1"; break;
                    case "Spain": _feed = @"http://es.feed.filmtrailer.com/v2.0/?ListType=Latest30InCinema&channel_user_id=441100001-1"; break;
                    case "Sweden": _feed = @"http://se.feed.filmtrailer.com/v2.0/?ListType=Latest30InCinema&channel_user_id=441100001-1"; break;
                    case "Switzerland": _feed = @"http://ch.feed.filmtrailer.com/v2.0/?ListType=Latest30InCinema&channel_user_id=441100001-1"; break;
                    case "Switzerland (fr)": _feed = @"http://ch-fr.feed.filmtrailer.com/v2.0/?ListType=Latest30InCinema&channel_user_id=441100001-1"; break;
                    case "The Netherlands": _feed = @"http://nl.feed.filmtrailer.com/v2.0/?ListType=Latest30InCinema&channel_user_id=441100001-1"; break;
                    case "United Kingdom": _feed = @"http://uk.feed.filmtrailer.com/v2.0/?ListType=Latest30InCinema&channel_user_id=441100001-1"; break;
                    case "** CUSTOM **": _feed = Plugin.PluginOptions.Instance.TrailerSourceCustom; break;
                    default: _feed = FeedDefault; break;
                }

                return _feed;
            }
            set { }
        }

        private int RefreshIntervalHrs
        {
            get
            {
                Int32 val;
                try
                {
                    val = Convert.ToInt32(Plugin.PluginOptions.Instance.RefreshIntervalHrs);
                }
                catch (Exception ex)
                {
                    Logger.ReportException("Bad refresh interval in film trailer plugin settings.  Must be an integer.  Using default of 24.", ex);
                    val = 24;
                }
                return val;
            }
            set { }
        }

        private string DownloadToFilePath
        {
            get
            {
                // FileName based on feed to force a refresh if feed is changed
                string fileName = Regex.Replace(Feed, ".*//(.*).feed.filmtrailer.com.*", "filmtrailers-$1.xml");
                return System.IO.Path.Combine(ApplicationPaths.AppRSSPath, fileName);
            }
            set { }
        }


        #region Base Item methods

        [Persist]
        [NotSourcedFromProvider]
        List<BaseItem> trailers = new List<BaseItem>();

        [Persist]
        [NotSourcedFromProvider]
        DateTime lastUpdated = DateTime.MinValue;


        public override string Name {
            get {
                return Plugin.PluginOptions.Instance.MenuName; 
            }
            set {
                // no control for this yet
            }
        }

        // The critical override, you need to override this to take control of the children 
        protected override List<BaseItem> ActualChildren
        {
            get
            {
                return trailers;
            }
        }

        // validation is overidden so it can do nothing if a period of time has not elapsed
        public override void ValidateChildren()
        {
#if (!DEBUG)
            if (Math.Abs((lastUpdated - DateTime.Now).TotalMinutes) < (RefreshIntervalHrs * 60)) return;
#endif
            lastUpdated = DateTime.Now;
            this.trailers = GetTrailers();
            this.OnChildrenChanged(null);
            // cache the children
            Kernel.Instance.ItemRepository.SaveItem(this);
        }
        #endregion

        #region Parse Feed

        List<BaseItem> GetTrailers()
        {
            var trailers = new List<BaseItem>();
            WebClient client = new WebClient();
            XmlDocument xDoc = new XmlDocument();
            try
            {
                if (IsRefreshRequired())
                {
                    client.DownloadFile(Feed, DownloadToFilePath);
                    Stream strm = client.OpenRead(Feed);
                    StreamReader sr = new StreamReader(strm);
                    string strXml = sr.ReadToEnd();
                    xDoc.LoadXml(strXml);
                }
                else
                {
                    xDoc.Load(DownloadToFilePath);
                }
                trailers = ParseDocument(xDoc);
            }
            catch (Exception e)
            {
                Logger.ReportException("Failed to update trailers", e);
            }
            finally
            {
                client.Dispose();
            }

            lastUpdated = DateTime.Now;

            return trailers;
        }

        private bool IsRefreshRequired()
        {
            if (File.Exists(DownloadToFilePath))
            {
                FileInfo fi = new FileInfo(DownloadToFilePath);
                if (fi.LastWriteTime < DateTime.Now.AddHours(-(RefreshIntervalHrs)))
                    return true;
                else
                    return false;
            }
            // If we get to this stage that means the file does not exists, and we should force a refresh
            return true;
        }

        private List<BaseItem> ParseDocument(XmlDocument xDoc)
        {
            List<BaseItem> trailers = new List<BaseItem>();
            XmlNodeList movieTrailers = xDoc.GetElementsByTagName("movie");

            foreach (XmlNode movie in movieTrailers)
            {
                try
                {
                    var currentTrailer = new FilmTrailer();
                    var x = movie;


                    foreach (XmlNode node in movie.ChildNodes)
                    {
                        if (node.Name == "original_title")
                        {
                            currentTrailer.Name = node.InnerText;
                        }
                        if (node.Name == "movie_duration")
                        {
                            currentTrailer.RunningTime = Int32.Parse(node.InnerText);
                        }
                        if (node.Name == "production_year")
                        {
                            currentTrailer.ProductionYear = Int32.Parse(node.InnerText);
                        }
                        if (node.Name == "actors")
                        {
                            var actors = node.SelectNodes("./actor");
                            if (currentTrailer.Actors == null)
                                currentTrailer.Actors = new List<Actor>();
                            foreach (XmlNode anode in actors)
                            {
                                 string actorName = anode.InnerText;
                                 if (!string.IsNullOrEmpty(actorName))
                                     currentTrailer.Actors.Add(new Actor { Name = actorName, Role = "" });
                            }
                        }
                        if (node.Name == "directors")
                        {
                            if (currentTrailer.Directors == null)
                                currentTrailer.Directors = new List<string>();
                            var directors = node.SelectNodes("./director");
                            if (directors.Count > 0)
                            {
                                foreach (XmlNode dnode in directors)
                                {
                                    currentTrailer.Directors.Add(dnode.InnerText);
                                }
                            }
                        }

                        if (node.Name == "regions")
                        {
                            currentTrailer.Overview = node.SelectSingleNode("./region/products/product/description").InnerText;
                            
                            currentTrailer.DateCreated = DateTime.Parse(node.SelectSingleNode("./region/products/product/pub_date").InnerText);                                
                            currentTrailer.DateModified = currentTrailer.DateCreated;
                            //currentTrailer.PrimaryImagePath = node.SelectSingleNode("./region/pictures/picture/url").InnerText;
                            var pictures = node.SelectNodes("./region/pictures/picture");
                            foreach (XmlNode pnode in pictures)
                            {
                                if (pnode.Attributes["type_name"].Value == "poster")
                                {
                                    currentTrailer.PrimaryImagePath = pnode.SelectSingleNode("./url").InnerText;
                                }
                            }


                            var genres = node.SelectNodes("./region/categories/categorie");
                            if (currentTrailer.Genres == null)
                                currentTrailer.Genres = new List<string>();
                            foreach (XmlNode gnode in genres)
                            {
                                currentTrailer.Genres.Add(gnode.InnerText);
                            }

                            var files = node.SelectNodes("./region/products/product/clips/clip/files/file");
                            foreach (XmlNode file in files)
                            {
                                if ((file.Attributes["format"].Value == "wmv" && file.Attributes["size"].Value == "xlarge") ||
                                    (file.Attributes["format"].Value == "wmv" && file.Attributes["size"].Value == "xxlarge"))
                                {
                                    foreach (XmlNode nodeFile in file)
                                    {
                                        if (nodeFile.Name == "url")
                                        {
                                            string[] pathUrl = nodeFile.InnerText.Split('?');

                                            currentTrailer.Path = pathUrl[0];
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    }
                    currentTrailer.Id = currentTrailer.Path.GetMD5();
                    currentTrailer.SubTitle = "presented by FilmTrailer.com";
                    trailers.Add(currentTrailer);
                    //Plugin.Logger.ReportInfo("FilmTrailer added trailer: " + currentTrailer.Name);
                }
                catch (Exception e)
                {
                    Logger.ReportException("Failed to parse trailer document", e);
                }
            }
            return trailers;
        }

        private string GetChildNodesValue(XPathNavigator nav, string nodeName)
        {
            string value = string.Empty;
            if (nav.MoveToChild(nodeName, ""))
            {
                value = nav.Value;
                nav.MoveToParent();
            }
            return value;
        }

        #endregion
    }
}
