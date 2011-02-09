using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;
using MediaBrowser;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Entities.Attributes;

namespace MBTrailers
{
    class LocalTrailerFolder : Folder
    {
        public override string Name
        {
            get
            {
                return Plugin.PluginOptions != null ? Plugin.PluginOptions.Instance.MyTrailerName : "My Trailers";
            }
            set
            {

            }
        }

        public override string ParentalRating
        {
            get
            {
                return "None"; //only way this can be assigned
            }
        }

        protected override List<BaseItem> GetNonCachedChildren()
        {
            //build our list of trailers
            var validChildren = new List<BaseItem>();
            var actualMovieRefs = new List<string>();

            foreach (Folder folder in Kernel.Instance.RootFolder.Children)
            {
                if (folder != this && folder is Folder) {
                    foreach (BaseItem item in folder.AllRecursiveChildren) //parental controls will kick in on the trailers so don't exclude now
                    {
                        if (item is Movie)
                        {
                            Movie movie = item as Movie;
                            if (movie.ContainsTrailers && !actualMovieRefs.Contains(movie.Path.ToLower()))
                            {
                                //create our movie item
                                MovieTrailer trailer = new MovieTrailer();
                                //now assign the id and fill in essentials - pointing to trailer as actual movie
                                trailer.Id = ("MBTrailers.MovieTrailer" + movie.TrailerFiles.First().ToLower()).GetMD5();
                                trailer.RealMovie = movie;
                                trailer.Overview = movie.Overview;
                                trailer.Genres = movie.Genres;
                                trailer.MpaaRating = movie.MpaaRating;
                                trailer.Path = movie.TrailerFiles.First();
                                trailer.Parent = this;
                                //and add to our children
                                validChildren.Add(trailer);
                                actualMovieRefs.Add(movie.Path.ToLower()); //we keep track so we don't get dups
                                //Plugin.proxy.SetTrailerInfo(trailer);
                            }
                        }
                    }
                }
            }
            return validChildren;
        }
    }
}
