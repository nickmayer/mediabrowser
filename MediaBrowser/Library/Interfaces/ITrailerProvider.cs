using System.Collections.Generic;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library.Interfaces {
    public interface ITrailerProvider
    {
        IEnumerable<string> GetTrailers(Movie movie);
    } 
}
