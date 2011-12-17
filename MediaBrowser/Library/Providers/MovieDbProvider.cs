using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Logging;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.ImageManagement;

namespace MediaBrowser.Library.Providers
{
    [RequiresInternet]
    [SupportedType(typeof(Movie))]
    public class MovieDbProvider : BaseMetadataProvider
    {
        private static string search = @"http://api.themoviedb.org/2.1/Movie.search/{2}/xml/{1}/{0}";
        private static string getInfo = @"http://api.themoviedb.org/2.1/Movie.getInfo/{2}/xml/{1}/{0}";
        private static readonly string ApiKey = "f6bd687ffa63cd282b6ff2c6877f2669";
        static readonly Regex[] nameMatches = new Regex[] {
            new Regex(@"(?<name>.*)\((?<year>\d{4})\)"), // matches "My Movie (2001)" and gives us the name and the year
            new Regex(@"(?<name>.*)") // last resort matches the whole string as the name
        };

        protected const string LOCAL_META_FILE_NAME = "MBMovie.xml";
        protected const string ALT_META_FILE_NAME = "MyMovies.xml";
        protected bool forceDownload = false;

        #region IMetadataProvider Members

        [Persist]
        string moviedbId;

        [Persist]
        DateTime downloadDate = DateTime.MinValue;

        public override bool NeedsRefresh()
        {
            if (Config.Instance.MetadataCheckForUpdateAge == -1 && downloadDate != DateTime.MinValue)
            {
                Logger.ReportInfo("MetadataCheckForUpdateAge = -1 wont clear and check for updated metadata");
                return false;
            }
            
            if (DateTime.Today.Subtract(Item.DateCreated).TotalDays > 180 && downloadDate != DateTime.MinValue)
                return false; // don't trigger a refresh data for item that are more than 6 months old and have been refreshed before

            if (DateTime.Today.Subtract(downloadDate).TotalDays < Config.Instance.MetadataCheckForUpdateAge) // only refresh every n days
                return false;

            if (HasAltMeta())
                return false; //never refresh if has meta from other source

            forceDownload = true; //tell the provider to re-download even if meta already there
            return true;
        }


        public override void Fetch()
        {
            if (HasAltMeta()) return;  //never fetch if external local meta exists

            if (forceDownload || !Kernel.Instance.ConfigData.SaveLocalMeta || !HasLocalMeta())
            {
                forceDownload = false; //reset
                FetchMovieData();
                downloadDate = DateTime.UtcNow;
            }
            else
            {
                Logger.ReportVerbose("MovieDBProvider not fetching because local meta exists for " + Item.Name);
            }
        }

        private bool HasLocalMeta()
        {
            //need at least the xml and folder.jpg/png or a mymovies put in by someone else
            return HasAltMeta() || (File.Exists(System.IO.Path.Combine(Item.Path,LOCAL_META_FILE_NAME)) && (File.Exists(System.IO.Path.Combine(Item.Path,"folder.jpg")) ||
                File.Exists(System.IO.Path.Combine(Item.Path,"folder.png"))));
        }

        private bool HasAltMeta()
        {
            return File.Exists(System.IO.Path.Combine(Item.Path, ALT_META_FILE_NAME));
        }

        private void FetchMovieData()
        {
            string id;
            string matchedName;
            string[] possibles;
            id = FindId(Item.Name, ((Movie)Item).ProductionYear ,out matchedName, out possibles);
            if (id != null)
            {
                Item.Name = matchedName;
                FetchMovieData(id);
            }
            else
            {
                Logger.ReportWarning("MovieDBProvider could not find " + Item.Name + ". Check name on themoviedb.org.");
            }
        }

        public static string FindId(string name, int? productionYear , out string matchedName, out string[] possibles)
        {
            string year = null;
            foreach (Regex re in nameMatches)
            {
                Match m = re.Match(name);
                if (m.Success)
                {
                    name = m.Groups["name"].Value.Trim();
                    year = m.Groups["year"] != null ? m.Groups["year"].Value : null;
                    break;
                }
            }
            if (year == "")
                year = null;

            if (year == null && productionYear != null) {
                year = productionYear.ToString();
            }

            Logger.ReportInfo("MovieDbProvider: Finding id for movie data: " + name);
            string language = Kernel.Instance.ConfigData.PreferredMetaDataLanguage.ToLower();
            string id = AttemptFindId(name, year, out matchedName, out possibles, language);
            if (id == null)
            {
                //try in english if wasn't before
                if (language != "en")
                {
                    id = AttemptFindId(name, year, out matchedName, out possibles, "en");
                }
                else
                {
                    if (id == null)
                    {
                        // try with dot and _ turned to space
                        name = name.Replace(".", " ");
                        name = name.Replace("  ", " ");
                        name = name.Replace("_", " ");
                        name = name.Replace("-", "");
                        matchedName = null;
                        possibles = null;
                        id = AttemptFindId(name, year, out matchedName, out possibles, language);
                        if (id == null && language != "en")
                        {
                            //finally again, in english
                            id = AttemptFindId(name, year, out matchedName, out possibles, "en");
                        }
                    }
                }
            }
            return id;
        }

        public static string AttemptFindId(string name, string year, out string matchedName, out string[] possibles, string language)
        {

            string id = null;
            string url = string.Format(search, UrlEncode(name), ApiKey, language);
            XmlDocument doc = Helper.Fetch(url);
            List<string> possibleTitles = new List<string>();
            if (doc != null)
            {
                XmlNodeList nodes = doc.SelectNodes("//movie");
                foreach (XmlNode node in nodes)
                {
                    matchedName = null;
                    id = null;
                    List<string> titles = new List<string>();
                    string mainTitle = null;
                    XmlNode n = node.SelectSingleNode("./name");
                    if (n != null)
                    {
                        titles.Add(n.InnerText);
                        mainTitle = n.InnerText;
                    }

                    var alt_titles = node.SelectNodes("./alternative_name");
                    {
                        foreach (XmlNode title in alt_titles)
                        {
                            titles.Add(title.InnerText);
                        }
                    }

                    if (titles.Count > 0)
                    {

                        var comparable_name = GetComparableName(name);
                        foreach (var title in titles)
                        {
                            if (GetComparableName(title) == comparable_name)
                            {
                                matchedName = title;
                                break;
                            }
                        }

                        if (matchedName != null)
                        {
                            Logger.ReportVerbose("Match " + matchedName + " for " + name);
                            if (year != null)
                            {
                                string r = node.SafeGetString("released");
                                if ((r != null) && r.Length >= 4)
                                {
                                    int db;
                                    if (Int32.TryParse(r.Substring(0, 4), out db))
                                    {
                                        int y;
                                        if (Int32.TryParse(year, out y))
                                        {
                                            if (Math.Abs(db - y) > 1) // allow a 1 year tollerance on release date
                                            {
                                                Logger.ReportVerbose("Result " + matchedName + " release on " + r + " did not match year " + year);
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                            id = node.SafeGetString("./id");
                            possibles = null;
                            return id;

                        }
                        else
                        {
                            foreach (var title in titles)
                            {
                                possibleTitles.Add(title);
                                Logger.ReportVerbose("Result " + title + " did not match " + name);
                            }
                        }
                    }
                }
            }
            possibles = possibleTitles.ToArray();
            matchedName = null;
            return null;
        }

        private static string UrlEncode(string name)
        {
            return HttpUtility.UrlEncode(name);
        }

        void FetchMovieData(string id)
        {

            string url = string.Format(getInfo, id, ApiKey, Config.Instance.PreferredMetaDataLanguage);
            moviedbId = id;
            XmlDocument doc = Helper.Fetch(url);
            ProcessDocument(doc, false);
            //and save locally
            if (Kernel.Instance.ConfigData.SaveLocalMeta)
            {
                try
                {
                    var movieNode = doc.SelectSingleNode("//movie"); //grab just movie node
                    doc.RemoveChild(doc.DocumentElement); //strip out other stuff
                    //doc.CreateXmlDeclaration("1.0", "utf-8", null);
                    doc.AppendChild(movieNode); //and put just the movie back in
                    Kernel.IgnoreFileSystemMods = true;
                    doc.Save(System.IO.Path.Combine(Item.Path, LOCAL_META_FILE_NAME));
                    Kernel.IgnoreFileSystemMods = false;
                }
                catch (Exception e)
                {
                    Logger.ReportException("Error saving local meta file " + System.IO.Path.Combine(Item.Path, LOCAL_META_FILE_NAME), e);

                }
            }
        }

        protected virtual void ProcessDocument(XmlDocument doc, bool ignoreImages) 
        {
            Movie movie = Item as Movie;
            if (doc != null)
            {
                // This is problematic for foreign films we want to keep the alt title. 
                //if (store.Name == null)
                //    store.Name = doc.SafeGetString("//movie/title");

                movie.Name = doc.SafeGetString("//movie/name");

                movie.Overview = doc.SafeGetString("//movie/overview");
                if (movie.Overview != null)
                    movie.Overview = movie.Overview.Replace("\n\n", "\n");

                movie.TagLine = doc.SafeGetString("//movie/tagline");
                movie.ImdbID = doc.SafeGetString("//movie/imdb_id");

                movie.ImdbRating = doc.SafeGetSingle("//movie/rating", -1, 10);

                string release = doc.SafeGetString("//movie/released");
                if (!string.IsNullOrEmpty(release))
                    movie.ProductionYear = Int32.Parse(release.Substring(0, 4));

                movie.RunningTime = doc.SafeGetInt32("//movie/runtime");
                if (movie.MediaInfo != null && movie.MediaInfo.RunTime > 0) movie.RunningTime = movie.MediaInfo.RunTime;

                movie.MpaaRating = doc.SafeGetString("//movie/certification");

                movie.Studios = null;
                foreach (XmlNode n in doc.SelectNodes("//studios/studio"))
                {
                    if (movie.Studios == null)
                        movie.Studios = new List<string>();
                    string name = n.SafeGetString("@name");
                    if (!string.IsNullOrEmpty(name))
                        movie.Studios.Add(name);
                }

                movie.Directors = null;
                foreach (XmlNode n in doc.SelectNodes("//cast/person[@job='Director']"))
                {
                    if (movie.Directors == null)
                        movie.Directors = new List<string>();
                    string name = n.SafeGetString("@name");
                    if (!string.IsNullOrEmpty(name))
                        movie.Directors.Add(name);
                }

                movie.Writers = null;
                foreach (XmlNode n in doc.SelectNodes("//cast/person[@job='Author']"))
                {
                    if (movie.Writers == null)
                        movie.Writers = new List<string>();
                    string name = n.SafeGetString("@name");
                    if (!string.IsNullOrEmpty(name))
                        movie.Writers.Add(name);
                }


                movie.Actors = null;
                foreach (XmlNode n in doc.SelectNodes("//cast/person[@job='Actor']"))
                {
                    if (movie.Actors == null)
                        movie.Actors = new List<Actor>();
                    string name = n.SafeGetString("@name");
                    string role = n.SafeGetString("@character");
                    if (!string.IsNullOrEmpty(name))
                        movie.Actors.Add(new Actor { Name = name, Role = role });
                }

                XmlNodeList nodes = doc.SelectNodes("//movie/categories/category[@type='genre']/@name");
                List<string> genres = new List<string>();
                foreach (XmlNode node in nodes)
                {
                    string n = MapGenre(node.InnerText);
                    if ((!string.IsNullOrEmpty(n)) && (!genres.Contains(n)))
                        genres.Add(n);
                }
                movie.Genres = genres;

                if (!ignoreImages)
                {
                    string img = doc.SafeGetString("//movie/images/image[@type='poster' and @size='" + Kernel.Instance.ConfigData.FetchedPosterSize + "']/@url");
                    if (img == null)
                    {
                        img = doc.SafeGetString("//movie/images/image[@type='poster' and @size='original']/@url"); //couldn't find preferred size
                    }
                    if (img != null)
                    {
                        if (Kernel.Instance.ConfigData.SaveLocalMeta)
                        {
                            //download and save locally
                            RemoteImage cover = new RemoteImage() { Path = img };
                            string ext = Path.GetExtension(img).ToLower();
                            string fn = (Path.Combine(Item.Path,"folder" + ext));
                            try
                            {
                                Kernel.IgnoreFileSystemMods = true;
                                cover.DownloadImage().Save(fn, ext == ".png" ? System.Drawing.Imaging.ImageFormat.Png : System.Drawing.Imaging.ImageFormat.Jpeg);
                                Kernel.IgnoreFileSystemMods = false;
                                movie.PrimaryImagePath = fn;
                            }
                            catch (Exception e)
                            {
                                Logger.ReportException("Error downloading and saving image " + fn, e);
                            }
                        }
                        else
                        {
                            movie.PrimaryImagePath = img;
                        }
                    }
                    movie.BackdropImagePaths = new List<string>();
                    int bdNo = 0;
                    RemoteImage bd;
                    foreach (XmlNode n in doc.SelectNodes("//movie/images/image[@type='backdrop' and @size='original']/@url"))
                    {
                        if (Kernel.Instance.ConfigData.SaveLocalMeta)
                        {
                            bd = new RemoteImage() { Path = n.InnerText };
                            string ext = Path.GetExtension(n.InnerText).ToLower();
                            string fn = Path.Combine(Item.Path,"backdrop" + (bdNo > 0 ? bdNo.ToString() : "") + ext);
                            try
                            {
                                Kernel.IgnoreFileSystemMods = true;
                                bd.DownloadImage().Save(fn, ext == ".png" ? System.Drawing.Imaging.ImageFormat.Png : System.Drawing.Imaging.ImageFormat.Jpeg);
                                Kernel.IgnoreFileSystemMods = false;
                                movie.BackdropImagePaths.Add(fn);
                            }
                            catch (Exception e)
                            {
                                Logger.ReportException("Error downloading/saving image " + n.InnerText, e);
                            }
                            bdNo++;
                            if (bdNo >= Kernel.Instance.ConfigData.MaxBackdrops) break;
                        }
                        else
                        {
                            movie.BackdropImagePaths.Add(n.InnerText);
                        }
                    }
                }
            }
        }
        
            

        #endregion

        private static readonly Dictionary<string, string> genreMap = CreateGenreMap();

        private static Dictionary<string, string> CreateGenreMap()
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            // some of the genres in the moviedb may be deemed too specific/detailed
            // they certainly don't align to those of other sources 
            // this collection will let us map them to alternative names or "" to ignore them
            /* these are the imdb genres that should probably be our common targets
                Action
                Adventure
                Animation
                Biography
                Comedy
                Crime
                Documentary
                Drama
                Family Fantasy
                Film-Noir
                Game-Show 
                History
                Horror
                Music
                Musical 
                Mystery
                News
                Reality-TV
                Romance 
                Sci-Fi
                Short
                Sport
                Talk-Show 
                Thriller
                War
                Western
             */
            ret.Add("Action Film", "Action");
            ret.Add("Adventure Film", "Adventure");
            ret.Add("Animation Film", "Animation");
            ret.Add("Comedy Film", "Comedy");
            ret.Add("Crime Film", "Crime");
            ret.Add("Children's Film", "Children");
            ret.Add("Disaster Film", "Disaster");
            ret.Add("Documentary Film", "Documentary");
            ret.Add("Drama Film", "Drama");
            ret.Add("Eastern", "Eastern");
            ret.Add("Environmental", "Environmental");
            ret.Add("Erotic Film", "Erotic");
            ret.Add("Family Film", "Family");
            ret.Add("Fantasy Film", "Fantasy");
            ret.Add("Historical Film", "History");
            ret.Add("Horror Film", "Horror");
            ret.Add("Musical Film", "Musical");
            ret.Add("Mystery", "Mystery");
            ret.Add("Mystery Film", "Mystery");
            ret.Add("Romance Film", "Romance");
            ret.Add("Road Movie", "Road Movie");
            ret.Add("Science Fiction Film", "Sci-Fi");
            ret.Add("Science Fiction", "Sci-Fi");
            ret.Add("Thriller", "Thriller");
            ret.Add("Thriller Film", "Thriller");
            ret.Add("Western", "Western");
            ret.Add("Music", "Music");
            ret.Add("Sport", "Sport");
            ret.Add("War", "War");
            ret.Add("Short", "Short");
            ret.Add("Biography", "Biography");
            ret.Add("Film-Noir", "Film-Noir");
            ret.Add("Game-Show", "Game-Show");

            return ret;
        }

        private string MapGenre(string g)
        {
            if (genreMap.ContainsValue(g)) return g; //the new api has cleaned up most of these

            if (genreMap.ContainsKey(g))
                return genreMap[g];
            else
            {
                Logger.ReportWarning("Tmdb category not mapped to genre: " + g);
                return "";
            }
        }

        static string remove = "\"'!`?";
        // "Face/Off" support.
        static string spacers = "/,.:;\\(){}[]+-_=–*";  // (there are not actually two - in the they are different char codes)

        internal static string GetComparableName(string name)
        {
            name = name.ToLower();
            name = name.Normalize(NormalizationForm.FormKD);
            StringBuilder sb = new StringBuilder();
            foreach (char c in name)
            {
                if ((int)c >= 0x2B0 && (int)c <= 0x0333)
                {
                    // skip char modifier and diacritics 
                }
                else if (remove.IndexOf(c) > -1)
                {
                    // skip chars we are removing
                }
                else if (spacers.IndexOf(c) > -1)
                {
                    sb.Append(" ");
                }
                else if (c == '&')
                {
                    sb.Append(" and ");
                }
                else
                {
                    sb.Append(c);
                }
            }
            name = sb.ToString();
            name = name.Replace("the", " ");

            string prev_name;
            do
            {
                prev_name = name;
                name = name.Replace("  ", " ");
            } while (name.Length != prev_name.Length);

            return name.Trim();
        }

    }
}
