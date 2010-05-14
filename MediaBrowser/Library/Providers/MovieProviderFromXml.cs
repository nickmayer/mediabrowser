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

namespace MediaBrowser.Library.Providers
{
    [SupportedType(typeof(Movie))]
    class MovieProviderFromXml : BaseMetadataProvider
    {

        [Persist]
        DateTime lastWriteTime = DateTime.MinValue;

        [Persist]
        string myMovieFile;


        
        #region IMetadataProvider Members

        public override bool NeedsRefresh()
        {
            string lastFile = myMovieFile;

            string mfile = XmlLocation();
            if (!File.Exists(mfile))
                mfile = null;
            if (lastFile != mfile)
                return true;
            if ((mfile == null) && (lastFile == null))
                return false;

          
            DateTime modTime = new FileInfo(mfile).LastWriteTimeUtc;
            DateTime lastTime = lastWriteTime;
            if (modTime <= lastTime)
               return false;
            
            return true;
        }

        private string XmlLocation()
        {
            string location = Item.Path;
            return Path.Combine(location, "mymovies.xml");
        }

        public override void Fetch()
        {
            var movie = Item as Movie;
             Debug.Assert(movie != null);

            string mfile = XmlLocation();
            string location = Path.GetDirectoryName(mfile);
            if (File.Exists(mfile))
            {

                DateTime modTime = new FileInfo(mfile).LastWriteTimeUtc;
                lastWriteTime = modTime;
                myMovieFile = mfile;
                XmlDocument doc = new XmlDocument();
                doc.Load(mfile);

                string s = doc.SafeGetString("Title/LocalTitle");
                if ((s == null) || (s == ""))
                    s = doc.SafeGetString("Title/OriginalTitle");
                movie.Name = s;
                movie.SortName = doc.SafeGetString("Title/SortTitle");
                
                movie.Overview = doc.SafeGetString("Title/Description");
                if (movie.Overview != null)
                    movie.Overview = movie.Overview.Replace("\n\n", "\n");
               
             
                string front = doc.SafeGetString("Title/Covers/Front");
                if ((front != null) && (front.Length > 0))
                {
                    front = Path.Combine(location, front);
                    if (File.Exists(front))
                        Item.PrimaryImagePath = front;
                }
                
                
                string back = doc.SafeGetString("Title/Covers/Back");
                if ((back != null) && (back.Length > 0))
                {
                    back = Path.Combine(location, back);
                    if (File.Exists(back))
                        Item.SecondaryImagePath = back;
                }


                if (movie.DisplayMediaType == null)
                {
                    movie.DisplayMediaType = doc.SafeGetString("Title/Type","");
                    switch (movie.DisplayMediaType.ToLower())
                    {
                        case "blu-ray":
                            movie.DisplayMediaType = MediaType.BluRay.ToString();
                            break;
                        case "dvd":
                            movie.DisplayMediaType = MediaType.DVD.ToString();
                            break;
                        case "hd dvd":
                            movie.DisplayMediaType = MediaType.HDDVD.ToString();
                            break;
                        default:
                            movie.DisplayMediaType = null;
                            break;
                    }
                }

                if (movie.RunningTime == null)
                {
                    int rt = doc.SafeGetInt32("Title/RunningTime",0);
                    if (rt > 0)
                        movie.RunningTime = rt;
                }
                if (movie.ProductionYear == null)
                {
                    int y = doc.SafeGetInt32("Title/ProductionYear",0);
                    if (y > 1900)
                        movie.ProductionYear = y;
                }
                if (movie.ImdbRating == null)
                {
                    float i = doc.SafeGetSingle("Title/IMDBrating", (float)-1, (float)10);
                    if (i >= 0)
                        movie.ImdbRating = i;
                }
                

                foreach (XmlNode node in doc.SelectNodes("Title/Persons/Person[Type='Actor']"))
                {
                    try
                    {
                        if (movie.Actors == null)
                            movie.Actors = new List<Actor>();

                        var name = node.SelectSingleNode("Name").InnerText;
                        var role = node.SafeGetString("Role", "");
                        var actor = new Actor() {Name = name, Role = role};

                        movie.Actors.Add(actor);
                    }
                    catch
                    {
                        // fall through i dont care, one less actor
                    }
                }
                
                
                foreach (XmlNode node in doc.SelectNodes("Title/Persons/Person[Type='Director']"))
                {
                    try
                    {
                        if (movie.Directors == null)
                            movie.Directors = new List<string>();
                        movie.Directors.Add(node.SelectSingleNode("Name").InnerText);
                    }
                    catch
                    {
                        // fall through i dont care, one less director
                    }
                }
                

                foreach (XmlNode node in doc.SelectNodes("Title/Genres/Genre"))
                {
                    try
                    {
                        if (movie.Genres == null)
                            movie.Genres = new List<string>();
                        movie.Genres.Add(node.InnerText);
                    }
                    catch
                    {
                        // fall through i dont care, one less genre
                    }
                }   

               
                foreach (XmlNode node in doc.SelectNodes("Title/Studios/Studio"))
                {
                    try
                    {
                        if (movie.Studios == null)
                            movie.Studios = new List<string>();
                        movie.Studios.Add(node.InnerText);
                        //movie.Studios.Add(new Studio { Name = node.InnerText });                        
                    }
                    catch
                    {
                        // fall through i dont care, one less actor
                    }
                }
                
                if (movie.TrailerPath == null)
                    movie.TrailerPath = doc.SafeGetString("Title/LocalTrailer/URL");

                if (movie.MpaaRating == null)
                    movie.MpaaRating = doc.SafeGetString("Title/MPAARating");

                if (movie.MpaaRating == null)
                {
                    int i = doc.SafeGetInt32("Title/ParentalRating/Value", (int)7);
                    switch (i) {
                        case -1:
                            movie.MpaaRating = "NR";
                            break;
                        case 0:
                            movie.MpaaRating = "UR";
                            break; 
                        case 1:
                            movie.MpaaRating = "G";
                            break;
                        case 3:
                            movie.MpaaRating = "PG";
                            break;
                        case 4:
                            movie.MpaaRating = "PG-13";
                            break;
                        case 5:
                            movie.MpaaRating = "NC-17";
                            break;
                        case 6:
                            movie.MpaaRating = "R";
                            break;
                        default:
                            movie.MpaaRating = null;
                            break;
                    }
                }
                //if there is a custom rating - use it (if not rating will be filled with MPAARating)
                if (movie.CustomRating == null)
                    movie.CustomRating = doc.SafeGetString("Title/CustomRating");

                if (movie.CustomPIN == null)
                    movie.CustomPIN = doc.SafeGetString("Title/CustomPIN");

                if (movie.AspectRatio == null)
                    movie.AspectRatio = doc.SafeGetString("Title/AspectRatio");

            }
        }



        #endregion
    }
}
