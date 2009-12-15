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

            if (!location.IsHidden()) {

                bool isDvd = IsDvd(location); 
                bool isVideo = !(location is IFolderMediaLocation) &&
                    (Helper.IsVideo(location.Path) || Helper.IsIso(location.Path));

                if ( (isDvd || isVideo ) &&
                    TVUtils.IsEpisode(location.Path)) {
                    factory = BaseItemFactory<Episode>.Instance;
                }
            }
        }

        private bool IsDvd(IMediaLocation location) {
            bool isDvd = false;

            var folder = location as IFolderMediaLocation;
            if (folder != null && folder.Children != null) {
                foreach (var item in folder.Children) {
                    isDvd |= Helper.IsVob(item.Path);
                    isDvd |= item.Path.ToUpper().EndsWith("VIDEO_TS");
                    if (isDvd) break;
                } 
            }
            
            return isDvd;
        }
    }
}
