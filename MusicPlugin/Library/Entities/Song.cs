using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Entities;
using MusicPlugin.Util;

namespace MusicPlugin.Library.Entities
{
    public class Song : Music
    {

        public Song()
            : base()
        {
            if (!string.IsNullOrEmpty(Settings.SongImage))
                this.PrimaryImagePath = Settings.SongImage;
        }

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
