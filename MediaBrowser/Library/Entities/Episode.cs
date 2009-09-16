using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Persistance;

namespace MediaBrowser.Library.Entities {
    public class Episode : Show {

        [Persist]
        public string EpisodeNumber { get; set; }

        [Persist]
        public string SeasonNumber { get; set; }

        [Persist]
        public string FirstAired { get; set; }

        public override string SortName {
            get {
                if (EpisodeNumber != null && EpisodeNumber.Length < 3) {
                    return (EpisodeNumber.PadLeft(3, '0') + " - " + Name.ToLower());
                } else {
                    return base.SortName;
                }
            }
            set {
                base.SortName = value;
            }
        }

        public Season Season {
            get {
                return Parent as Season;
            }
        }

        public Series Series {
            get {
                Series found = null;
                if (Parent != null) {
                    if (Parent.GetType() == typeof(Season)) {
                        found = Parent.Parent as Series;
                    } else {
                        found = Parent as Series;
                    }
                }
                return found;
            }
        }

        public override string LongName {
            get {
                string longName = base.LongName;
                if (Season != null) {
                    longName = Season.Name + " - " + longName;
                }
                if (Series != null) {
                    longName = Series.Name + " - " + longName;
                }
                return longName;
            }
        }
    }
}
