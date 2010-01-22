using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.ImageManagement;
using System.Diagnostics;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library.Factories {

    public delegate LibraryImage ImageResolver(string path, bool canBeProcessed, BaseItem item); 

    public class LibraryImageFactory {
        public static LibraryImageFactory Instance = new LibraryImageFactory();

        private LibraryImageFactory() {
        }

        Dictionary<string, LibraryImage> cache = new Dictionary<string, LibraryImage>();

        public void ClearCache() {
            lock (cache) {
                cache.Clear();
            }
        }

        public void ClearCache(string path) {
            lock (cache) {
                if (cache.ContainsKey(path)) {
                    cache.Remove(path);
                }
            }
        }

        public LibraryImage GetImage(string path)
        {
            return GetImage(path, false, null);
        }

        public LibraryImage GetImage(string path, bool canBeProcessed, BaseItem item) {
            LibraryImage image = null;
            bool cached = false;

            lock(cache){
                cached = cache.TryGetValue(path, out image);
            }

            if (!cached && image == null) {
                try {

                    foreach (var resolver in Kernel.Instance.ImageResolvers) {
                        image = resolver(path, canBeProcessed, item);
                        if (image != null) break;
                    }

                    if (image == null) {
                        if (canBeProcessed)
                            image = new FilesystemProcessedImage(item);
                        else
                            image = new FilesystemImage();
                    }

                    image.Path = path;
                    image.Init();

                    // this will trigger a download and a resize
                    // image.EnsureImageSizeInitialized();
                } catch (Exception ex) {
                    Logger.ReportException("Failed to load image: " + path + " ", ex);
                    image = null;
                }
            }

            lock (cache) {
                cache[path] = image;
            }

            return image;
        }
    }
}
