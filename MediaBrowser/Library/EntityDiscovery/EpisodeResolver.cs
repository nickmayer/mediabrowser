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

                bool containsIfo;
                bool isDvd = IsDvd(location, out containsIfo);
                bool isIso = Helper.IsIso(location.Path); 
                bool isVideo = !(location is IFolderMediaLocation) &&
                    (Helper.IsVideo(location.Path) || isIso || location.IsVob());

                if ( (isDvd || isVideo ) &&
                    TVUtils.IsEpisode(location.Path)) {

                    if (containsIfo || isIso) {
                        MediaType mt = isIso ? MediaType.ISO : MediaType.DVD;
                        setup = new List<InitializationParameter>() {
                            new MediaTypeInitializationParameter() {MediaType = mt}
                        };
                    } 

                    factory = BaseItemFactory<Episode>.Instance;
                }
            }
        }

        private bool IsDvd(IMediaLocation location, out bool containsIfo) {
            bool isDvd = false;
            containsIfo = false;

            var folder = location as IFolderMediaLocation;
            if (folder != null && folder.Children != null) {
                foreach (var item in folder.Children) {
                    isDvd |= Helper.IsVob(item.Path);
                    if (item.Path.ToUpper().EndsWith("VIDEO_TS")) {
                        isDvd = true;
                        containsIfo = true;
                    }
                    containsIfo |= Helper.IsIfo(item.Path);

                    if (isDvd && containsIfo) break;
                } 
            }
            
            return isDvd;
        }
    }
}
