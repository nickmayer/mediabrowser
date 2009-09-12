using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Entities.Attributes;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library;
using MusicPlugin.Library.Helpers;
using MusicPlugin.Util;

namespace MusicPlugin.Library.Entities
{
    public class iTunesMusicLibrary:Folder
    {        
        #region Base Item methods

        [Persist]
        [NotSourcedFromProvider]
        public List<BaseItem> Genres = new List<BaseItem>();

        [Persist]
        [NotSourcedFromProvider]
        public List<BaseItem> Artists = new List<BaseItem>();

        [Persist]
        [NotSourcedFromProvider]
        public List<BaseItem> Albums = new List<BaseItem>();

        [Persist]
        [NotSourcedFromProvider]
        DateTime lastUpdated = DateTime.MinValue;

        public DateTime LastUpdate
        {
            get
            {
                return lastUpdated;
            }
            set
            {
                lastUpdated = value;
            }
        }
        public override void ValidateChildren()
        {
            
        }
        public override string Name
        {
            get
            {
                return Settings.Instance.iTunesLibraryVirtualFolderName;
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
                if (Settings.Instance.ShowGenreIniTunesLibrary)
                    return Genres;
                else if (Settings.Instance.ShowArtistIniTunesLibrary)
                    return Artists;
                else
                    return Albums;
            }
        }

        #endregion

        #region Parse Feed

       

        public override bool RefreshMetadata(MediaBrowser.Library.Metadata.MetadataRefreshOptions options)
        {
            return false;
        }


        #endregion

    }


}
