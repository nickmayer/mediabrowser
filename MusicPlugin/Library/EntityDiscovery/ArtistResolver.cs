using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.EntityDiscovery;
using MusicPlugin.LibraryManagement;
using MusicPlugin.Library.Entities;

namespace MusicPlugin.Library.EntityDiscovery
{
    public class ArtistResolver : EntityResolver
    {

        public override void ResolveEntity(IMediaLocation location,
            out BaseItemFactory factory,
            out IEnumerable<InitializationParameter> setup)
        {

            factory = null;
            setup = null;

            var folderLocation = location as IFolderMediaLocation;

            if (folderLocation != null && !MusicHelper.IsHidden(folderLocation.Path))
            {
                if (MusicHelper.IsArtistFolder(location))
                {
                    factory = BaseItemFactory<Artist>.Instance;
                }
            }
        }
    }
}
