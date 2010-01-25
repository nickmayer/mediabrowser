using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Persistance;

namespace MediaBrowser.Library.ImageManagement {
    public class FilesystemImage : LibraryImage{

        [Persist]
        bool imageIsCached;
        bool isValid = false;

        public override void Init() {
            base.Init();

            imageIsCached = System.IO.Path.GetPathRoot(this.Path).ToLower() != System.IO.Path.GetPathRoot(cachePath).ToLower();

        }

        protected override string LocalFilename {
            get {
                if (!imageIsCached) return Path;
                return base.LocalFilename;
            }
        }

        private static DateTime Max(DateTime first, DateTime second) {
            if (first > second) return first;
            return second;
        } 

        protected DateTime CacheDate {
            get {
                DateTime date = DateTime.MaxValue; 
                
                if (imageIsCached) {
                    var info = new System.IO.FileInfo(LocalFilename);
                    date = Max(info.CreationTimeUtc,info.LastWriteTimeUtc);
                } else {
                    var files = Directory.GetFiles(cachePath, Id.ToString() + "*");
                    if (files.Length > 0) {
                        date = files
                            .Select(file => new System.IO.FileInfo(file))
                            .Select(info => Max(info.LastWriteTimeUtc, info.CreationTimeUtc))
                            .Max();
                    } 
	            
                }
                return date; 
            }
        }

        public override string GetLocalImagePath() {
            lock (Lock) {
                if (!isValid && File.Exists(LocalFilename)) {
                    
                    var remoteInfo = new System.IO.FileInfo(Path);
                    var localInfo = remoteInfo;
                    if (imageIsCached) {
                        localInfo = new System.IO.FileInfo(LocalFilename);
                    } 

                    isValid = CacheDate > Max(remoteInfo.LastWriteTimeUtc, remoteInfo.CreationTimeUtc);
                    //isValid &= localInfo.Length == remoteInfo.Length; //isn't date enough?  This will always be true if we process the image...
                }

                if (!isValid) {
                    ClearLocalImages();
                    if (imageIsCached) {
                        this.CacheImage();
                    }
                    isValid = true;
                }
                
                return LocalFilename;
            }
        }

        protected virtual void CacheImage()
        {
            byte[] data = File.ReadAllBytes(Path);
            using (var stream = ProtectedFileStream.OpenExclusiveWriter(LocalFilename))
            {
                BinaryWriter bw = new BinaryWriter(stream);
                bw.Write(data);
                stream.Close();
            }
        }

    
    }
}
