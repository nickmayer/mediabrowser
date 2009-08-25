using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Entities.Attributes;
using MediaBrowser.Library;

namespace MusicPlugin.Library.Entities
{
    public class iTunesArtist:Folder
    {

        #region Base Item methods

        [Persist]
        [NotSourcedFromProvider]
        public List<BaseItem> Albums = new List<BaseItem>();

        [Persist]
        [NotSourcedFromProvider]
        DateTime lastUpdated = DateTime.MinValue;

        public bool HasImage = false;

        public override string Name
        {
            get
            {
                return ArtistName;
            }
            set
            {
                // no control for this yet
            }
        }
        //protected override List<BaseItem> GetNonCachedChildren()
        //{
        //    return new List<BaseItem>();
        //}
        // The critical override, you need to override this to take control of the children 
        protected override List<BaseItem> ActualChildren
        {
            get
            {
                return Albums;
            }
        }

        public override void ValidateChildren()
        {

        }

        #endregion

        #region Parse Feed

        public override bool RefreshMetadata(MediaBrowser.Library.Metadata.MetadataRefreshOptions options)
        {
            return false;
        }

        #endregion

        [Persist]
        [NotSourcedFromProvider]
        public string ArtistName;

    }

}
