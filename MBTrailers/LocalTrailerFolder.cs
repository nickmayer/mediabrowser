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
using System.ServiceModel;
using WebProxy.WCFInterfaces;

namespace MBTrailers
{
    class LocalTrailerFolder : Folder
    {
        [Persist]
        [NotSourcedFromProvider]
        DateTime lastUpdated = DateTime.MinValue;

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

        public override void ValidateChildren()
        {
            System.Threading.Thread.Sleep(5000); //wait to be sure the service process is up...

            //only validate once per day or when forced or on service refresh
            if (Kernel.LoadContext == MBLoadContext.Service || Plugin.PluginOptions.Instance.Changed || DateTime.Now > lastUpdated.AddHours(23))
            {
                Logger.ReportInfo("MBTrailers validating MyTrailers " + lastUpdated);
                Plugin.PluginOptions.Instance.Changed = false;
                Plugin.PluginOptions.Save();
                lastUpdated = DateTime.Now;
                base.ValidateChildren();
                Kernel.Instance.ItemRepository.SaveItem(this);
            }
            else
            {
                //just go through our existing children and be sure the proxy has them in the list
                using (ChannelFactory<ITrailerProxy> factory = new ChannelFactory<ITrailerProxy>(new NetNamedPipeBinding(), "net.pipe://localhost/mbtrailers"))
                {
                    ITrailerProxy proxyServer = factory.CreateChannel();
                    try
                    {
                        foreach (Movie item in this.Children)
                        {
                            var trailerInfo = new TrailerInfo(TrailerType.Local, item.Path.ToLower(), item.ParentalRating, item.Genres);
                            proxyServer.SetTrailerInfo(trailerInfo);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.ReportException("Error setting trailer info", e);
                        Logger.ReportError("Inner Exception: " + e.InnerException.Message);
                    }
                    finally
                    {
                        (proxyServer as ICommunicationObject).Close();
                    }
                }
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
                                trailer.RealMovieID = movie.Id;
                                trailer.Overview = movie.Overview;
                                trailer.Genres = movie.Genres;
                                trailer.MpaaRating = movie.MpaaRating;
                                trailer.Path = movie.TrailerFiles.First();
                                trailer.Parent = this;
                                //and add to our children
                                validChildren.Add(trailer);
                                actualMovieRefs.Add(movie.Path.ToLower()); //we keep track so we don't get dups
                            }
                        }
                    }
                }
            }
            return validChildren;
        }
    }
}
