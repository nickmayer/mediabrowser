using System;
using System.Collections.Generic;
namespace MediaBrowser.Library.Entities {
    public interface IShow {
        List<Actor> Actors { get; set; }
        List<string> Directors { get; set; }
        List<string> Genres { get; set; }
        float? ImdbRating { get; set; }
        string MpaaRating { get; set; }
        int? RunningTime { get; set; }
        List<Studio> Studios { get; set; }
    }
}
