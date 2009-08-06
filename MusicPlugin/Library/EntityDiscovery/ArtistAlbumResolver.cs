using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Factories;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Providers.TVDB;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.EntityDiscovery;
using MusicPlugin.LibraryManagement;
using MusicPlugin.Library.Entities;

namespace MusicPlugin.Library.EntityDiscovery
{
    public class ArtistAlbumResolver : EntityResolver
    {

        public override void ResolveEntity(IMediaLocation location,
            out BaseItemFactory factory,
            out IEnumerable<InitializationParameter> setup)
        {

            factory = null;
            setup = null;

            if (location is IFolderMediaLocation && !MusicHelper.IsHidden(location.Path) && MusicHelper.IsArtistAlbumFolder(location.Path))
            {
                factory = BaseItemFactory<ArtistAlbum>.Instance;
            }
        }
    }
}
