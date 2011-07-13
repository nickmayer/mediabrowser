using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Entities.Attributes;

namespace MediaBrowser.Library.Entities {
    public class Episode : Show {

        [Persist]
        public string EpisodeNumber { get; set; }

        [Persist]
        public string SeasonNumber { get; set; }

        [Persist]
        public string FirstAired { get; set; }

        [NotSourcedFromProvider]
        [Persist]
        public Guid SeriesId {get; set;}

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

        public override Series OurSeries
        {
            get
            {
                return Series ?? Series.BlankSeries;
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

        public override string OfficialRating
        {
            get
            {
                if (Series != null)
                {
                    return Series.OfficialRating;
                }
                else return "None";
            }
        }

        public override bool RefreshMetadata(MediaBrowser.Library.Metadata.MetadataRefreshOptions options)
        {
            bool changed = base.RefreshMetadata(options);
            if (this.Series != null && this.Series.BannerImage != null && (options & MediaBrowser.Library.Metadata.MetadataRefreshOptions.Force) == MediaBrowser.Library.Metadata.MetadataRefreshOptions.Force)
            {
                //we cleared our our series banner image - re-cache it
                var ignore = this.Series.BannerImage.GetLocalImagePath();
            }
            return changed;
        }
    }
}
