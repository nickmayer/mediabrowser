using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Entities;

namespace MusicPlugin.Library.Entities
{
    public class Song : Music
    {

        [Persist]
        public string TrackNumber { get; set; }


        //public Artist Artist
        //{
        //    get
        //    {
        //        Artist found = null;
        //        if (Parent != null)
        //        {
        //            if (Parent.GetType() == typeof(Album))
        //            {
        //                found = Parent.Parent as Artist;
        //            }
        //            else
        //            {
        //                found = Parent as Artist;
        //            }
        //        }
        //        return found;
        //    }
        //}

        public override string LongName
        {
            get
            {
                string longName = base.LongName;
                if (Parent != null)
                {
                    longName = Parent.Name + " - " + longName;
                }
                return longName;
            }
        }
    }
}
