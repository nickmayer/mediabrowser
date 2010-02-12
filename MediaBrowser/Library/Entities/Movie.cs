using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Library.Filesystem;

namespace MediaBrowser.Library.Entities {
    public class Movie : Show {

        [Persist]
        public string TrailerPath {get; set;}

        /// <summary>
        /// This paths of all the parts of the movie. Eg part1.avi, part2.avi
        /// </summary>
        [Persist]
        List<string> VolumePaths { get; set; }

        public override void Assign(IMediaLocation location, IEnumerable<InitializationParameter> parameters, Guid id) {
            base.Assign(location, parameters, id);

            if (parameters != null) {
                foreach (var parameter in parameters) {
                    var movieVolumeParam = parameter as MovieVolumeInitializationParameter;
                    if (movieVolumeParam != null) {
                        VolumePaths = movieVolumeParam.Volumes.Select(o => o.Path).ToList();
                        // this is how we calculate dates on movies ... min of all the actual movie paths
                        DateCreated = movieVolumeParam.Volumes.Select(o => o.DateCreated).Min();
                    }
                }
            }
        }

        public bool ContainsTrailers {
            get {
                return TrailerFiles.Count() > 0;
            }
        }

        public IEnumerable<string> TrailerFiles { 
            get {
                var folder = MediaLocation as IFolderMediaLocation; 
                if (folder != null && folder.ContainsChild(MovieResolver.TrailersPath)) {

                    var trailers = folder.GetChild(MovieResolver.TrailersPath) as IFolderMediaLocation;
                    if (trailers != null) {
                        foreach (var path in GetChildVideos(trailers, new string[] { MovieResolver.TrailersPath })) {
                            yield return path;
                        }
                    }
                }
            }
        }

        public override IEnumerable<string> VideoFiles {
            get {

                string[] ignore = null;
                if (Kernel.Instance.ConfigData.EnableLocalTrailerSupport) {
                    ignore = new string[] { MovieResolver.TrailersPath };
                }

                if (!ContainsRippedMedia && MediaLocation is IFolderMediaLocation) {
                    foreach (var path in GetChildVideos((IFolderMediaLocation)MediaLocation, ignore)) {
                        yield return path;
                    }
                } else {
                    yield return Path;
                }
            }
        }

    }
}
