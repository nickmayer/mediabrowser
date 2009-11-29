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

namespace MediaBrowser.Library.Entities {

    public class MetadataChangedEventArgs : EventArgs { }

    public class BaseItem {

        public EventHandler<MetadataChangedEventArgs> MetadataChanged;

        public Folder Parent { get; set; }

        #region Images

        [Persist]
        public virtual string PrimaryImagePath { get; set; }
        [Persist]
        public virtual string SecondaryImagePath { get; set; }
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
                return GetImage(PrimaryImagePath);
            }
        }

        public LibraryImage SecondaryImage {
            get {
                return GetImage(SecondaryImagePath);
            }
        }

        // banner images will fall back to parent
        public LibraryImage BannerImage {
            get {
                return SearchParents<LibraryImage>(this, item => item.GetImage(item.BannerImagePath));
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
                return GetImage(BackdropImagePath);
            }
        }

        public List<LibraryImage> BackdropImages {
            get {
                var images = new List<LibraryImage>();
                if (BackdropImagePaths != null) {
                    foreach (var path in BackdropImagePaths) {
                        var image = GetImage(path);
                        if (image != null) images.Add(image);
                    }
                }
                return images;
            }
        }

        private LibraryImage GetImage(string path) {
            if (string.IsNullOrEmpty(path)) return null;
            return Kernel.Instance.GetImage(path);
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
        public string Overview { get; set; }

        [Persist]
        public string SubTitle { get; set; }

        [Persist]
        public string CustomRating { get; set; }
        [Persist]
        public string CustomPIN { get; set; }

        public bool ParentalAllowed { get { return Kernel.Instance.ParentalControls.Allowed(this); } }
        public string ParentalRating
        {
            get
            {
                if (string.IsNullOrEmpty(this.CustomRating)) {
                    var aShow = this as IShow;
                    if (aShow != null)
                        if (string.IsNullOrEmpty(aShow.MpaaRating))
                        {
                            //see if we are an episode or a season of a TV series and inherit their ratings
                            if (this is Season)
                            {
                                var aSeries = this.Parent as Series;
                                if (aSeries != null)
                                {
                                    return aSeries.ParentalRating;
                                }
                                else return "";
                            }
                            else
                                if (this is Episode)
                                {
                                    var anEpisode = this as Episode;
                                    if (anEpisode.Series != null)
                                    {
                                        return anEpisode.Series.ParentalRating;
                                    }
                                    else return "";
                                }
                                else
                                {
                                    return "";
                                }
                        }
                        else return aShow.MpaaRating;
                    else
                        return "G"; //if not a show and no custom rating return something valid that will always be allowed
                }           
                else
                    return this.CustomRating;
            }
        }

        public virtual bool PlayAction(Item item)
        {
            //this will be overridden by sub-classes to perform the proper action for that item type
            return false;
        }


        public virtual void Assign(IMediaLocation location, IEnumerable<InitializationParameter> parameters, Guid id) {
            this.Id = id;
            this.Path = location.Path;
            this.DateModified = location.DateModified;
            this.DateCreated = location.DateCreated;

            if (location is IFolderMediaLocation) {
                defaultName = location.Name;
            } else {
                defaultName = System.IO.Path.GetFileNameWithoutExtension(location.Name);
            }
        }

        // we may want to do this automatically, somewhere down the line
        public virtual bool AssignFromItem(BaseItem item) {
            // we should never reasign identity 
            Debug.Assert(item.Id == this.Id);
            Debug.Assert(item.Path == this.Path);
            Debug.Assert(item.GetType() == this.GetType());

            bool changed = this.DateModified != item.DateModified;
            changed |= this.DateCreated != item.DateCreated;
            changed |= this.defaultName != item.defaultName;

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

            return changed;
        }
    }
}
