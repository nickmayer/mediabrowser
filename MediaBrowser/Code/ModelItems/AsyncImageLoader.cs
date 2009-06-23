using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using Microsoft.MediaCenter.UI;
using System.IO;
using MediaBrowser.Library.Factories;
using System.Reflection;
using MediaBrowser.Library;
using MediaBrowser.Library.ImageManagement;
using MediaBrowser.Library.Threading;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Code.ModelItems {
    class AsyncImageLoader {

        static MethodInfo ImageFromStream = typeof(Image).GetMethod("FromStream", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new Type[] { typeof(string), typeof(Stream) }, null);
        static BackgroundProcessor<Action> ImageLoadingProcessors = new BackgroundProcessor<Action>(2, action => action(), "Image loader");

        Func<LibraryImage> source;
        Action afterLoad;
        Image image = null;
        Image defaultImage = null;
        Microsoft.MediaCenter.UI.Size size;
        object sync = new object();

        public Microsoft.MediaCenter.UI.Size Size {
            get {
                return size;
            }
            set {
                lock (this) {
                    size = value;
                    image = null;
                }
            }
        }

        public bool IsLoaded {
            get;
            private set;
        }

        public AsyncImageLoader(Func<LibraryImage> source, Image defaultImage, Action afterLoad) {
            this.source = source;
            this.afterLoad = afterLoad;
            this.IsLoaded = false;
            this.defaultImage = defaultImage;

        }

        public Image Image {
            get {
                lock (this) {
                    if (image == null && source != null) {
                        ImageLoadingProcessors.Inject(LoadImage);
                    }

                    if (image != null) {
                        return image;
                    }
                    else {
                        // fall back
                        return defaultImage;
                    }
                }
            }
        }

        private void LoadImage() {
            try {
                lock (sync) {
                    LoadImageImpl();
                }
            } catch (Exception e) {
                // this may fail in if we are unable to write a file... its not a huge problem cause we will pick it up next time around
                Logger.ReportException("Failed to load image", e);
                if (Debugger.IsAttached) {
                    Debugger.Break();
                }
            }
        }

        private void LoadImageImpl() {

            bool sizeIsSet = Size != null && Size.Height > 0 && Size.Width > 0;

            var localImage = source();

            // if the image is invalid it may be null.
            if (localImage != null) {

                string localPath = localImage.GetLocalImagePath();
                if (sizeIsSet) {
                    localPath = localImage.GetLocalImagePath(Size.Width, Size.Height);
                }

                Microsoft.MediaCenter.UI.Application.DeferredInvoke(_ => {

                    Logger.ReportVerbose("Loading image : " + localPath);
                    Image newImage = new Image("file://" + localPath);
                    
                    lock (this) {
                        image = newImage;
                        if (!sizeIsSet) {
                            size = new Size(localImage.Width, localImage.Height);
                        }
                    }

                    IsLoaded = true;

                    if (afterLoad != null) {
                        afterLoad();
                    }
                });

            }
        }

    }
}
