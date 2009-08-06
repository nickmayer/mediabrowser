using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using MediaBrowser.Library.Interfaces;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library.Providers;
using MusicPlugin.Code.ModelItems;
using MusicPlugin.Library.Entities;

namespace MusicPlugin.Library.Providers
{
    [SupportedType(typeof(ArtistAlbum))]
    class ArtistAlbumProvider : BaseMetadataProvider
    {        
        [Persist]
        DateTime lastWriteTime = DateTime.MinValue;

        #region IMetadataProvider Members

        public override bool NeedsRefresh()
        {            
            return false;
        }

        public override void Fetch()
        {
            ReaderMetaData();
        }

        public override bool RequiresInternet
        {
            get
            {
                return false;
            }
        }

        public override bool IsSlow
        {
            get
            {
                return false ;
            }
        }

            
        public void ReaderMetaData()
        {
            //foreach (var item in (this.Item as Folder).Children)
            //{
            //TagLib.File file = TagLib.File.Create(item.Path);
            //file.Tag.

                //System.Console.WriteLine("Title:  " + file.Tag.Title);
                //System.Console.WriteLine("Album:  " + file.Tag.Album);

                //// Some entries support multiple strings.
                //foreach (string artist in file.Tag.AlbumArtists)
                //    System.Console.WriteLine("Artist: " + artist);
            //}
        }

        #endregion
    }
}
