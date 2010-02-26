﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Persistance;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Extensions;
using System.IO;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Entities;
using System.Drawing;
using MediaBrowser.Library.Threading;

namespace MediaBrowser.Library.ImageManagement {
    public abstract class LibraryImage {

        protected BaseItem item;
        bool canBeProcessed; 

        /// <summary>
        /// The raw path of this image including http:// or grab:// 
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The image is not valid, bad url or file 
        /// </summary>
        public bool Corrupt { private set; get; }

        Guid Id { get { return Path.GetMD5(); } }

        public virtual void Init(bool canBeProcessed, BaseItem item) {
            this.item = item;
            this.canBeProcessed = canBeProcessed;
        }
       

        bool loaded = false;
        private void EnsureLoaded() {
            if (loaded) return;

            try {
                if (!loaded) {
                    var info = ImageCache.Instance.GetPrimaryImage(Id);
                    if (info == null) {
                        ImageCache.Instance.CacheImage(Id, ProcessImage(OriginalImage));
                    }
                    info = ImageCache.Instance.GetPrimaryImage(Id);
                    if (info != null) {
                        _width = info.Width;
                        _height = info.Height;
                    } else {
                        Corrupt = true;
                    }

                    Async.Queue("Validate Image Thread", () => {
                        if (info != null) {
                            if (ImageOutOfDate(info.Date)) {
                                ClearLocalImages();
                            }
                        } 
                    });

                }
            } catch (Exception e) {
                Logger.ReportException("Failed to deal with image: " + Path, e);
                Corrupt = true;
            } finally {
                loaded = true;
            }
        }

        protected virtual bool CacheOriginalImage {
            get {
                return true;
            }
        } 

        protected abstract Image OriginalImage {
            get;
        }

        protected virtual bool ImageOutOfDate(DateTime data) {
            return false;
        }

        /// <summary>
        /// Will ensure a local copy is cached and return the path to the caller
        /// </summary>
        /// <returns></returns>
        public string GetLocalImagePath() {
            EnsureLoaded();
            return ImageCache.Instance.GetImagePath(Id);
        }

        public string GetLocalImagePath(int width, int height) {
            EnsureLoaded();
            return ImageCache.Instance.GetImagePath(Id, width, height);
        }


        int _width = -1;
        int _height = -1;
        public int Width { 
            get {
                EnsureLoaded();
                return _width;
            } 
        }
       
        public int Height { 
            get {
                EnsureLoaded();
                return _height; 
            } 
        }

        public float Aspect {
            get {
                return ((float)Height) / (float)Width;;
            }
        }


        /// <summary>
        /// Will return true if the image is cached locally. 
        /// </summary>
        public bool IsCached {
            get {
                return ImageCache.Instance.GetImagePath(Id) != null;
            }
        } 

        // will clear all local copies
        public void ClearLocalImages() {
            ImageCache.Instance.ClearCache(Id);
            loaded = false;
        }


       
        System.Drawing.Image ProcessImage(System.Drawing.Image image)
        {
            if (canBeProcessed && Kernel.Instance.ImageProcessor != null) {
                return Kernel.Instance.ImageProcessor(image, item);
            } else {
                return image;
            }
        }

        /*
        protected string ConvertRemotePathToLocal(string remotePath) {
            string localPath = remotePath;

            if (localPath.ToLower().Contains("http://"))
                localPath = localPath.Replace("http://", "");

            localPath = System.IO.Path.Combine(cachePath, localPath.Replace('/', '\\'));

            return localPath;

        }*/

    }
}
