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

namespace MediaBrowser.Library.EntityDiscovery {
    public class EpisodeResolver : EntityResolver {
        
        public override void ResolveEntity(IMediaLocation location, 
            out BaseItemFactory factory, 
            out IEnumerable<InitializationParameter> setup) 
        {
            factory = null;
            setup = null;

            if (!(location is IFolderMediaLocation) && 
                !location.IsHidden() &&
                (Helper.IsVideo(location.Path) || Helper.IsIso(location.Path)) && 
                TVUtils.IsEpisode(location.Path)) {
                
                factory = BaseItemFactory<Episode>.Instance; 
            }
        }
    }
}
