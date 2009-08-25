using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MusicPlugin.Library.Helpers;
using MediaBrowser.Library.Entities.Attributes;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library;
using MusicPlugin.Library.Entities;


namespace MusicPlugin.Library.Entities
{
    public class iTunesAlbum : Folder
    {

        #region Base Item methods

        [Persist]
        [NotSourcedFromProvider]
        public List<BaseItem> Songs = new List<BaseItem>();

        [Persist]
        [NotSourcedFromProvider]
        DateTime lastUpdated = DateTime.MinValue;

        public bool HasImage = false;

        public override string Name
        {
            get
            {
                return AlbumName;
            }
            set
            {
                // no control for this yet
            }
        }

        // The critical override, you need to override this to take control of the children 
        protected override List<BaseItem> ActualChildren
        {
            get
            {
                return Songs;
            }
        }
        //protected override List<BaseItem> GetNonCachedChildren()
        //{
        //    return new List<BaseItem>();
        //}
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
        public string AlbumName;

    }
}
