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

namespace MBTrailers
{
    class BackdropProvider
    {
        public Movie Item;
        private static string search = @"http://api.themoviedb.org/2.1/Movie.search/en/xml/{1}/{0}";
        private static string getInfo = @"http://api.themoviedb.org/2.1/Movie.getInfo/en/xml/{1}/{0}";
        private static readonly string ApiKey = "f6bd687ffa63cd282b6ff2c6877f2669";
        static readonly Regex[] nameMatches = new Regex[] {
            new Regex(@"(?<name>.*)\((?<year>\d{4})\)"), // matches "My Movie (2001)" and gives us the name and the year
            new Regex(@"(?<name>.*)") // last resort matches the whole string as the name
        };

        string moviedbId;

        public  void Fetch()
        {
            FetchMovieData();
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

            Logger.ReportInfo("MBTrailer Backdrop Provider: Finding id for movie data: " + name);
            string id = AttemptFindId(name, year, out matchedName, out possibles);
            if (id == null)
            {
                // try with dot and _ turned to space
                name = name.Replace(".", " ");
                name = name.Replace("  ", " ");
                name = name.Replace("_", " ");
                matchedName = null;
                possibles = null;
                return AttemptFindId(name, year, out matchedName, out possibles);
            }
            else
                return id;
        }

        public static string AttemptFindId(string name, string year, out string matchedName, out string[] possibles)
        {

            string id = null;
            string url = string.Format(search, UrlEncode(name), ApiKey);
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
                                //Logger.ReportVerbose("Result " + title + " did not match " + name);
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
            return System.Web.HttpUtility.UrlEncode(name);
        }

        void FetchMovieData(string id)
        {
            Movie movie = Item as Movie;

            string url = string.Format(getInfo, id, ApiKey);
            XmlDocument doc = Helper.Fetch(url);
            if (doc != null)
            {
                moviedbId = id;
                // This is problamatic for forign films we want to keep the alt title. 
                //if (store.Name == null)
                //    store.Name = doc.SafeGetString("//movie/title");

                movie.Studios = null;
                foreach (XmlNode n in doc.SelectNodes("//studios/studio"))
                {
                    if (movie.Studios == null)
                        movie.Studios = new List<string>();
                    string name = n.SafeGetString("@name");
                    if (!string.IsNullOrEmpty(name))
                        movie.Studios.Add(name);
                }


                movie.BackdropImagePaths = new List<string>();
                foreach (XmlNode n in doc.SelectNodes("//movie/images/image[@type='backdrop' and @size='original']/@url"))
                {
                    movie.BackdropImagePaths.Add(n.InnerText);
                }


                return;
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
