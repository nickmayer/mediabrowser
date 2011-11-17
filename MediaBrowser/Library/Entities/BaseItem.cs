﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Interfaces;
using MediaBrowser.Library.Providers;
using MediaBrowser.Library.Entities.Attributes;
using System.Diagnostics;
using MediaBrowser.Library.ImageManagement;
using MediaBrowser.Library.Sorting;
using MediaBrowser.Library.Metadata;
using System.ComponentModel;
using MediaBrowser.Library.Logging;
using MediaBrowser.LibraryManagement;

namespace MediaBrowser.Library.Entities {

    public class MetadataChangedEventArgs : EventArgs { }

    public class BaseItem {

        public EventHandler<MetadataChangedEventArgs> MetadataChanged;

        public Folder Parent { get; set; }

        public Guid TopParent
        {
            get
            {
                Folder parent = this.Parent;
                while (parent != null && parent.Parent != null && parent.Parent != Kernel.Instance.RootFolder)
                {
                    parent = parent.Parent;
                }
                return parent != null ? parent.Id : Guid.Empty;
            }
        }

        #region Images

        [Persist]
        public virtual string PrimaryImagePath { get; set; }
        [Persist]
        public virtual string SecondaryImagePath { get; set; }
        [Persist]
        public virtual string LogoImagePath { get; set; }
        [Persist]
        public virtual string ArtImagePath { get; set; }
        [Persist]
        public virtual string ThumbnailImagePath { get; set; }
        [Persist]
        public virtual string BannerImagePath { get; set; }

        public string BackdropImagePath {
            get {
                string path = null;
                if (BackdropImagePaths != null && BackdropImagePaths.Count != 0) {
                    path = BackdropImagePaths[0];
                }
                return path;
            }
            set {
                if (BackdropImagePaths == null) {
                    BackdropImagePaths = new List<string>();
                }
                if (BackdropImagePaths.Contains(value)) {
                    BackdropImagePaths.Remove(value);
                }
                BackdropImagePaths.Insert(0, value);
            }
        }

        [Persist]
        public virtual List<string> BackdropImagePaths { get; set; }


        public LibraryImage PrimaryImage {
            get {
                if (this is Media || this is Folder || this is Person )
                    return GetImage(PrimaryImagePath, true);
                else
                    return GetImage(PrimaryImagePath);
            }
        }

        public LibraryImage SecondaryImage {
            get {
                return GetImage(SecondaryImagePath) ?? PrimaryImage;
            }
        }

        public LibraryImage ThumbnailImage {
            get {
                return GetImage(ThumbnailImagePath) ?? PrimaryImage;
            }
        }

        // banner images will fall back to parent
        public LibraryImage BannerImage {
            get {
                return SearchParents<LibraryImage>(this, item => item.GetImage(item.BannerImagePath, Kernel.Instance.ConfigData.ProcessBanners));
            }
        }

        // logo images will fall back to parent
        public LibraryImage LogoImage {
            get {
                return SearchParents<LibraryImage>(this, item => item.GetImage(item.LogoImagePath));
            }
        }

        // art images will fall back to parent
        public LibraryImage ArtImage {
            get {
                return SearchParents<LibraryImage>(this, item => item.GetImage(item.ArtImagePath));
            }
        }

        static T SearchParents<T>(BaseItem item, Func<BaseItem, T> search) where T : class {
            var result = search(item);
            if (result == null && item.Parent != null) {
                result = SearchParents(item.Parent, search);
            }
            return result;
        }

        public LibraryImage BackdropImage {
            get {
                return GetImage(BackdropImagePath, Kernel.Instance.ConfigData.ProcessBackdrops);
            }
        }

        public LibraryImage PrimaryBackdropImage
        {
            get
            {
                if (BackdropImagePaths != null && BackdropImagePaths.Count != 0)
                {
                    return GetImage(BackdropImagePaths[0], Kernel.Instance.ConfigData.ProcessBackdrops);
                }
                else return null;
            }
        }


        public List<LibraryImage> BackdropImages {
            get {
                var images = new List<LibraryImage>();
                if (BackdropImagePaths == null)
                {
                    // inherit from parent
                    if (Parent != null)
                    {
                        BackdropImagePaths = Parent.BackdropImagePaths;
                    }
                }
                if (BackdropImagePaths != null) {
                    foreach (var path in BackdropImagePaths) {
                        var image = GetImage(path, Kernel.Instance.ConfigData.ProcessBackdrops);
                        if (image != null) images.Add(image);
                    }
                }  
                return images;
            }
        }

        private LibraryImage GetImage(string path)
        {
            return GetImage(path, false);
        }

        private LibraryImage GetImage(string path, bool canBeProcessed) {
            if (string.IsNullOrEmpty(path)) return null;
            return Kernel.Instance.GetImage(path, canBeProcessed, this);
        }

        #endregion

        [NotSourcedFromProvider]
        [Persist]
        public Guid Id { get; set; }

        [NotSourcedFromProvider]
        [Persist]
        public DateTime DateCreated { get; set; }

        [NotSourcedFromProvider]
        [Persist]
        public DateTime DateModified { get; set; }

        [NotSourcedFromProvider]
        [Persist]
        string defaultName;

        [NotSourcedFromProvider]
        [Persist]
        public string Path { get; set; }

        [Persist]
        string name;

        public virtual string Name {
            get {
                return name ?? defaultName;
            }
            set {
                name = value;
            }
        }


        public virtual string LongName {
            get {
                return Name;
            }
        }

        [Persist]
        string sortName;

        public virtual string SortName { 
            get {
                return SortHelper.GetSortableName(sortName ?? Name);
            }
            set {
                sortName = value;
            }
        }


        [Persist]
        public virtual string Overview { get; set; }

        [Persist]
        public virtual string SubTitle { get; set; }

        [Persist]
        public virtual string DisplayMediaType { get; set; }

        [Persist]
        public string CustomRating { get; set; }
        [Persist]
        public string CustomPIN { get; set; }

        public virtual string OfficialRating
        {
            get
            {
                return ""; //will be implemented by sub-classes
            }
            set { }
        }

        public virtual string ShortDescription
        {
            get
            {
                return ""; //will be implemented by sub-classes
            }
            set { }
        }

        public virtual string TagLine
        {
            get
            {
                return ""; //will be implemented by sub-classes
            }
            set { }
        }

        public bool ParentalAllowed { get { return Kernel.Instance.ParentalControls.Allowed(this); } }
        public virtual string ParentalRating
        {
            get
            {
                if (string.IsNullOrEmpty(this.CustomRating)) {
                    if (this == Kernel.Instance.RootFolder)
                    {
                        //never want to block the root
                        return "None";
                    }
                    else
                    {
                        return OfficialRating;
                    }
                }           
                else
                    return this.CustomRating;
            }
        }

        public virtual bool PlayAction(Item item)
        {
            //this will be overridden by sub-classes to perform the proper action for that item type
            Logger.ReportWarning("No play action defined for item type " + this.GetType() + " on item " + item.Name);
            return false;
        }

        public virtual bool SelectAction(Item item)
        {
            //this can be overridden by sub-classes to perform the proper action for that item type
            Application.CurrentInstance.Navigate(item);  //default is open the item
            return true;
        }

        bool? isRemoteContent = null;
        public bool IsRemoteContent
        {
            get
            {
                if (isRemoteContent == null) { 
                    isRemoteContent = Path != null && Path.ToLower().StartsWith("http://");
                }
                return isRemoteContent.Value;
            }
        }

        public virtual Series OurSeries
        {
            get
            {
                //default baseItem has no series - return a valid blank item so MCML won't blow chow
                return Series.BlankSeries;
            }
        }

        bool? isTrailer = null;
        public bool IsTrailer
        {
            get
            {
                if (isTrailer == null)
                {
                    isTrailer = DisplayMediaType != null && DisplayMediaType.ToLower() == "trailer";
                }
                return isTrailer.Value;
            }
        }

        public virtual void Assign(IMediaLocation location, IEnumerable<InitializationParameter> parameters, Guid id)
        {
            this.Id = id;
            this.Path = location.Path;
            this.DateModified = location.DateModified;
            this.DateCreated = location.DateCreated;

            if (location is IFolderMediaLocation) {
                defaultName = location.Name;
            } else {
                defaultName = Helper.GetNameFromFile(location.Path);
            }
        }

        // we may want to do this automatically, somewhere down the line
        public virtual bool AssignFromItem(BaseItem item) {
            // we should never reasign identity 
            Debug.Assert(item.Id == this.Id);
            if (item.Path != null)
                Debug.Assert(item.Path.ToLower() == this.Path.ToLower());
            Debug.Assert(item.GetType() == this.GetType());
            bool changed = false;
            //the following is to get around an anomoly with how directory creation dates seem to be returned from the actual item vs a shortcut to it
            //  I will attempt to re-write the date generations in a future release -ebr
            if (Kernel.Instance.ConfigData.EnableShortcutDateHack) 
            {
                changed = this.DateModified.ToShortDateString() != item.DateModified.ToShortDateString();
                changed |= this.DateCreated.ToShortDateString() != item.DateCreated.ToShortDateString();
            }
            else
            {
                changed = this.DateModified != item.DateModified;
                changed |= this.DateCreated != item.DateCreated;
            }
            changed |= this.defaultName != item.defaultName;
            //if (changed && Debugger.IsAttached) Debugger.Break();

            this.Path = item.Path;
            this.DateModified = item.DateModified;
            this.DateCreated = item.DateCreated;
            this.defaultName = item.defaultName;

            return changed;
        }

        protected void OnMetadataChanged(MetadataChangedEventArgs args) {
            if (MetadataChanged != null) {
                MetadataChanged(this, args);
            }
        }
        
        /// <summary>
        /// Refresh metadata on this item, will return true if the metadata changed  
        /// </summary>
        /// <returns>true if the metadata changed</returns>
        public bool RefreshMetadata() {
            return RefreshMetadata(MetadataRefreshOptions.Default);
        }

        /// <summary>
        /// Refresh metadata on this item, will return true if the metadata changed 
        /// </summary>
        /// <param name="fastOnly">Only use fast providers (excluse internet based and slow providers)</param>
        /// <param name="force">Force a metadata refresh</param>
        /// <returns>True if the metadata changed</returns>
        public virtual bool RefreshMetadata(MetadataRefreshOptions options) {
            if (!Kernel.isVista) Kernel.Instance.MajorActivity = true; //this borks the UI on vista
            if ((options & MetadataRefreshOptions.Force) == MetadataRefreshOptions.Force) {
                var images = new List<LibraryImage>();
                images.Add(PrimaryImage);
                images.Add(SecondaryImage);
                images.Add(BannerImage);
                images.AddRange(BackdropImages);

                foreach (var image in images) {
                    try {
                        if (image != null) {
                            image.ClearLocalImages();
                            LibraryImageFactory.Instance.ClearCache(image.Path);
                        }
                    } catch (Exception ex) {
                        Logger.ReportException("Failed to clear local image (its probably in use)", ex);
                    }
                }
            }

            bool changed = MetadataProviderHelper.UpdateMetadata(this, options);
            if (changed) {
                OnMetadataChanged(null);
            }
            if (!Kernel.isVista) Kernel.Instance.MajorActivity = false;

            return changed;
        }

        public void ReCacheAllImages()
        {
            string ignore;
            if (this.PrimaryImage != null)
            {
                PrimaryImage.ClearLocalImages();
                ignore = this.PrimaryImage.GetLocalImagePath(); //no size - cache at original size
            }

            foreach (MediaBrowser.Library.ImageManagement.LibraryImage image in this.BackdropImages)
            {
                image.ClearLocalImages();
                ignore = image.GetLocalImagePath(); //force the backdrops to re-cache
            }
            if (this.BannerImage != null)
            {
                this.BannerImage.ClearLocalImages();
                ignore = this.BannerImage.GetLocalImagePath(); //and, finally, banner
            }
        }
        public void MigrateAllImages()
        {
            if (this.PrimaryImage != null)
            {
                Logger.ReportInfo("Migrating primary image for " + Name );
                    this.PrimaryImage.MigrateFromOldID(); 
            }

            foreach (MediaBrowser.Library.ImageManagement.LibraryImage image in this.BackdropImages)
            {
                image.MigrateFromOldID(); 
            }
            if (this.BannerImage != null)
            {
                this.BannerImage.MigrateFromOldID(); 
            }
        }
    }
}
