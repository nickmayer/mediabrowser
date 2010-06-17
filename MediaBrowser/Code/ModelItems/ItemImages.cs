﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library;
using Microsoft.MediaCenter.UI;
using System.Threading;
using MediaBrowser.Library.ImageManagement;
using System.Reflection;
using System.IO;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Factories;
using System.Diagnostics;
using MediaBrowser.Code.ModelItems;
using MediaBrowser.Library.Threading;
using System.Runtime.InteropServices;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Library
{
    public partial class Item
    {
        public bool HasBannerImage
        {
            get
            {
                return (BaseItem.BannerImagePath != null) ||
                    (PhysicalParent != null ? PhysicalParent.HasBannerImage : false);
            }
        }

        AsyncImageLoader bannerImage = null;
        public Image BannerImage
        {
            get
            {
                if (!HasBannerImage)
                {
                    if (PhysicalParent != null)
                    {
                        return PhysicalParent.BannerImage;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (bannerImage == null)
                {
                    bannerImage = new AsyncImageLoader(
                        () => baseItem.BannerImage,
                        null,
                        () => this.FirePropertiesChanged("PreferredImage", "BannerImage"));
                }
                return bannerImage.Image;
            }
        }

        public bool HasBackdropImage
        {
            get
            {
                return baseItem.BackdropImagePath != null;
            }
        }

        AsyncImageLoader backdropImage = null;
        public Image BackdropImage
        {
            get
            {
                if (!HasBackdropImage)
                {
                    if (PhysicalParent != null)
                    {
                        return PhysicalParent.BackdropImage;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (backdropImage == null)  
                {
                    if (Config.Instance.RandomizeBackdrops)
                    {
                        getRandomBackdropImage();
                    }
                    else
                    {
                       getPrimaryBackdropImage(); 
                    }
                }
                if (backdropImage != null) //may not have had time to fill this in yet - if not, a propertychanged event will fire it again
                {
                    return backdropImage.Image;
                }
                else
                {
                    return null;
                }
            }
        }

        private void getPrimaryBackdropImage()
        {
            backdropImage = new AsyncImageLoader(
                () => baseItem.BackdropImage,
                null,
                () => this.FirePropertyChanged("BackdropImage"));
            backdropImage.LowPriority = true;
        }

        private void getRandomBackdropImage()
        {
            backdropImage = new AsyncImageLoader(
                () => baseItem.BackdropImages[randomizer.Next(baseItem.BackdropImages.Count)],
                null,
                () => this.FirePropertyChanged("BackdropImage"));
            backdropImage.LowPriority = true;
        }

        public Image PrimaryBackdropImage
        {
            get
            {
                getPrimaryBackdropImage();
                return backdropImage.Image;
            }
        }

        List<AsyncImageLoader> backdropImages = null;
        public List<Image> BackdropImages
        {
            get
            {
                if (!HasBackdropImage)
                {
                    return null;
                }

                if (backdropImages == null)
                {
                    EnsureAllBackdropsAreLoaded();
                }

                lock (backdropImages)
                {
                    return backdropImages.Select(async => async.Image).ToList();
                }
            }
        }

        private void EnsureAllBackdropsAreLoaded()
        {
            if (backdropImages == null)
            {
                backdropImages = new List<AsyncImageLoader>();

     
                Async.Queue("Backdrop Loader", () =>
                {
                    foreach (var image in baseItem.BackdropImages)
                    {
                        // this is really subtle, we need to capture the image otherwise they will all be the same
                        var captureImage = image;
                        var asyncImage = new AsyncImageLoader(
                             () => captureImage,
                             null,
                             () => this.FirePropertiesChanged("BackdropImages", "BackdropImage"));

                        lock (backdropImages)
                        {
                            backdropImages.Add(asyncImage);
                            // trigger a load
                            var ignore = asyncImage.Image;
                        }
                    }
                });
            }
        }

        int backdropImageIndex = 0;
        Random randomizer = new Random();
        public void GetNextBackDropImage()
        {
            if (!Config.Instance.RotateBackdrops) return; // only do this if we want to rotate

            backdropImageIndex++;
            EnsureAllBackdropsAreLoaded();
            var images = new List<AsyncImageLoader>();
            lock (backdropImages)
            {
                images.AddRange(backdropImages);
            }

            if (images != null && images.Count > 1)
            {
                if (Config.Instance.RandomizeBackdrops)
                {
                    backdropImageIndex = randomizer.Next(images.Count);
                }
                else
                {

                    backdropImageIndex = backdropImageIndex % images.Count;
                }
                if (images[backdropImageIndex].Image != null)
                {
                    backdropImage = images[backdropImageIndex];
                    FirePropertyChanged("BackdropImage");
                }
            }
        }

        AsyncImageLoader primaryImage = null;
        public Image PrimaryImage
        {
            get
            {
                if (baseItem.PrimaryImagePath == null)
                {
                    return DefaultImage;
                }
                EnsurePrimaryImageIsSet();
                return primaryImage.Image;
            }
        }

        private void EnsurePrimaryImageIsSet()
        {
            if (primaryImage == null)
            {
                primaryImage = new AsyncImageLoader(
                    () => baseItem.PrimaryImage,
                    DefaultImage,
                    PrimaryImageChanged);
                var ignore = primaryImage.Image;
            }
        }

        void PrimaryImageChanged()
        {
            FirePropertiesChanged("PrimaryImage", "PreferredImage", "PrimaryImageSmall", "PreferredImageSmall");
        }

        AsyncImageLoader primaryImageSmall = null;
        // these all come in from the ui thread so no sync is required. 
        public Image PrimaryImageSmall
        {
            get
            {

                if (baseItem.PrimaryImagePath != null) {
                    EnsurePrimaryImageIsSet();

                    if (primaryImage.IsLoaded &&
                        preferredImageSmallSize != null &&
                        (preferredImageSmallSize.Width > 0 ||
                        preferredImageSmallSize.Height > 0)) {

                        if (primaryImageSmall == null) {
                            LoadSmallPrimaryImage();
                        }
                    }

                    return primaryImageSmall != null ? primaryImageSmall.Image : null;
                } else {
                    return DefaultImage;
                }

              
            }
        }

        private void LoadSmallPrimaryImage() {
            float aspect = primaryImage.Size.Height / (float)primaryImage.Size.Width;
            float constraintAspect = aspect;

            if (preferredImageSmallSize.Height > 0 && preferredImageSmallSize.Width > 0) {
                constraintAspect = preferredImageSmallSize.Height / (float)preferredImageSmallSize.Width;
            }

            primaryImageSmall = new AsyncImageLoader(
                () => baseItem.PrimaryImage,
                DefaultImage,
                PrimaryImageChanged);

            if (aspect == constraintAspect) {
                smallImageIsDistorted = false;
            } else {
                smallImageIsDistorted = Math.Abs(aspect - constraintAspect) < Config.Instance.MaximumAspectRatioDistortion;
            }

            if (smallImageIsDistorted) {
                primaryImageSmall.Size = preferredImageSmallSize;
            } else {

                int width = preferredImageSmallSize.Width;
                int height = preferredImageSmallSize.Height;

                if (aspect > constraintAspect || width <= 0) {
                    width = (int)((float)height / aspect);
                } else {
                    height = (int)((float)width * aspect);
                }

                primaryImageSmall.Size = new Size(width, height);
            }

            FirePropertyChanged("SmallImageIsDistorted");
        }

        bool smallImageIsDistorted = false;
        public bool SmallImageIsDistorted
        {
            get
            {
                return smallImageIsDistorted;
            }
        }

        public Image PreferredImage
        {
            get
            {
                return preferBanner ? BannerImage : PrimaryImage;
            }
        }


        public Image PreferredImageSmall
        {
            get
            {
                return preferBanner ? BannerImage : PrimaryImageSmall;
            }
        }

        Microsoft.MediaCenter.UI.Size preferredImageSmallSize;
        public Microsoft.MediaCenter.UI.Size PreferredImageSmallSize
        {
            get
            {
                return preferredImageSmallSize;
            }
            set
            {
                if (value != preferredImageSmallSize)
                {
                    preferredImageSmallSize = value;
                    primaryImageSmall = null;
                    FirePropertyChanged("PreferredImageSmall");
                    FirePropertyChanged("PrimaryImageSmall");
                }
            }
        }


        public bool HasPrimaryImage
        {
            get { return baseItem.PrimaryImagePath != null; }
        }

        public bool HasPreferredImage
        {
            get { return (PreferBanner ? HasBannerImage : HasPrimaryImage); }
        }

        bool preferBanner;
        public bool PreferBanner
        {
            get
            {
                return preferBanner;
            }
            set
            {
                preferBanner = value;
                FirePropertyChanged("HasPreferredImage");
                FirePropertyChanged("PreferredImage");
            }
        }


        internal float PrimaryImageAspect
        {
            get
            {
                return GetAspectRatio(baseItem.PrimaryImagePath);
            }
        }

        internal float BannerImageAspect
        {
            get
            {
                return GetAspectRatio(baseItem.BannerImagePath);
            }
        }

        float GetAspectRatio(string path)
        {

            float aspect = 0;
            if (path != null)
            {
                LibraryImage image;
                if (BaseItem is Media)
                {
                    image = LibraryImageFactory.Instance.GetImage(path, true, BaseItem);
                }
                else
                {
                    image = LibraryImageFactory.Instance.GetImage(path);
                }
                aspect = ((float)image.Height) / (float)image.Width;
            }
            return aspect;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);


        public void SetPrimarySmallToTiny() {
            var windowSize = GetWindowSize(new Size(1280, 720));
            this.preferredImageSmallSize = new Size(-1, windowSize.Height / 8);
        }


        // I can not figure out any way to pass the size of an element to the code
        // so I cheat 
        public void SetPreferredImageSmallToEstimatedScreenSize()
        {

            var folder = this as FolderModel;
            if (folder == null) return;

            Size size = GetWindowSize(new Size(1280, 720));

            size.Width = -1;
            size.Height = size.Height / 3;

            foreach (var item in folder.Children)
            {
                item.PreferredImageSmallSize = size;
            }

        }

        private static Size GetWindowSize(Size size) {

            try {

                // find ehshell 
                var ehshell = Process.GetProcessesByName("ehshell").First().MainWindowHandle;

                if (ehshell != IntPtr.Zero) {

                    RECT windowSize;
                    GetWindowRect(ehshell, out windowSize);

                    size = new Size(
                        (windowSize.Right - windowSize.Left) ,
                        (windowSize.Bottom - windowSize.Top)
                        );

                }
            } catch (Exception e) {
                Logger.ReportException("Failed to gather size information, made a guess ", e);
            }
            return size;
        }


        static Image DefaultVideoImage = new Image("res://ehres!MOVIE.ICON.DEFAULT.PNG");
        static Image DefaultActorImage = new Image("resx://MediaBrowser/MediaBrowser.Resources/MissingPerson");
        static Image DefaultStudioImage = new Image("resx://MediaBrowser/MediaBrowser.Resources/BlankGraphic");
        static Image DefaultFolderImage = new Image("resx://MediaBrowser/MediaBrowser.Resources/folder");

        public Image DefaultImage
        {
            get
            {
                Image image = DefaultFolderImage;

                if (baseItem is Video)
                {
                    image = DefaultVideoImage;
                }
                else if (baseItem is Person)
                {
                    image = DefaultActorImage;
                }
                else if (baseItem is Studio)
                {
                    image = DefaultStudioImage;
                }

                return image;
            }
        }

    }
}
