using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Persistance;
using System.Drawing;

namespace MediaBrowser.Library.ImageManagement {
    public class FilesystemImage : LibraryImage{



        /*
         bool imageIsCached;
        public override void Init() {
            base.Init();

            imageIsCached = System.IO.Path.GetPathRoot(this.Path).ToLower() != System.IO.Path.GetPathRoot(cachePath).ToLower();

        }*/


        protected override bool ImageOutOfDate(DateTime date) {
            var info = new System.IO.FileInfo(Path);
            return date < Max(info.CreationTimeUtc, info.LastWriteTimeUtc);
        }

        protected override System.Drawing.Image OriginalImage {
            get {
                return Image.FromFile(Path);
            }
        }

        private static DateTime Max(DateTime first, DateTime second) {
            if (first > second) return first;
            return second;
        } 
    
    }
}
