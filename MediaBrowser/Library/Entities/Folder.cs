﻿using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Library.Util;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Threading;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Localization;
using System.Collections;
using System.Diagnostics;

namespace MediaBrowser.Library.Entities {

    public class ChildrenChangedEventArgs : EventArgs {
        public bool FolderContentChanged { get; set; }
    }

    public class Folder : BaseItem, MediaBrowser.Library.Entities.IFolder {

        public event EventHandler<ChildrenChangedEventArgs> ChildrenChanged;

        Lazy<List<BaseItem>> children;
        protected IFolderMediaLocation location;
        IComparer<BaseItem> sortFunction = new BaseItemComparer(SortOrder.Name);
        object validateChildrenLock = new object();
        public MBDirectoryWatcher directoryWatcher;

        public Folder()
            : base() {
            children = new Lazy<List<BaseItem>>(() => GetChildren(true), () => OnChildrenChanged(null));
        }

        private Dictionary<string, IComparer<BaseItem>> sortOrderOptions= new Dictionary<string,IComparer<BaseItem>>() { 
            {LocalizedStrings.Instance.GetString("NameDispPref"), new BaseItemComparer(SortOrder.Name)},
            {LocalizedStrings.Instance.GetString("DateDispPref"), new BaseItemComparer(SortOrder.Date)},
            {LocalizedStrings.Instance.GetString("RatingDispPref"), new BaseItemComparer(SortOrder.Rating)},
            {LocalizedStrings.Instance.GetString("RuntimeDispPref"), new BaseItemComparer(SortOrder.Runtime)},
            {LocalizedStrings.Instance.GetString("UnWatchedDispPref"), new BaseItemComparer(SortOrder.Unwatched)},
            {LocalizedStrings.Instance.GetString("YearDispPref"), new BaseItemComparer(SortOrder.Year)}
        };
        //Dynamic Choice Items - these can be overidden or added to by sub-classes to provide for different options for different item types
        /// <summary>
        /// Dictionary of sort options - consists of a localized display string and an IComparer(Baseitem) for the sort
        /// </summary>
        public virtual Dictionary<string, IComparer<BaseItem>> SortOrderOptions
        {
            get { return sortOrderOptions; }
            set { sortOrderOptions = value; }
        }
        private Dictionary<string, string> indexByOptions = new Dictionary<string, string>() { 
            {LocalizedStrings.Instance.GetString("NoneDispPref"), ""}, 
            {LocalizedStrings.Instance.GetString("ActorDispPref"), "Actors"},
            {LocalizedStrings.Instance.GetString("GenreDispPref"), "Genres"},
            {LocalizedStrings.Instance.GetString("DirectorDispPref"), "Directors"},
            {LocalizedStrings.Instance.GetString("YearDispPref"), "ProductionYear"},
            {LocalizedStrings.Instance.GetString("StudioDispPref"), "Studios"}
        };
        /// <summary>
        /// Dictionary of index options - consists of a display value and a property name (must match the property exactly)
        /// </summary>
        public virtual Dictionary<string, string> IndexByOptions
        {
            get { return indexByOptions; }
            set { indexByOptions = value; }
        }

        /// <summary>
        /// By default children are loaded on first access, this operation is slow. So sometimes you may
        ///  want to force the children to load;
        /// </summary>
        public virtual void EnsureChildrenLoaded() {
            var ignore = ActualChildren;
        }

        public IFolderMediaLocation FolderMediaLocation {
            get {
                if (location == null) {
                    location = Kernel.Instance.GetLocation<IFolderMediaLocation>(Path);
                }
                return location;
            }
        }

        public override void Assign(IMediaLocation location, IEnumerable<InitializationParameter> parameters, Guid id) {
            base.Assign(location, parameters, id);
            this.location = location as IFolderMediaLocation;
        }

        public override bool PlayAction(Item item)
        {
            //set our flag to show the popup menu
            return Application.CurrentInstance.DisplayPopupPlay = true;
        }

        private List<BaseItem> parentalAllowedChildren
        {
            get
            {
                // return only the children not protected
                return Kernel.Instance.ParentalControls.RemoveDisallowed(ActualChildren);
            }
        }

        public virtual bool PromptForChildRefresh
        {
            get
            {
                return Kernel.Instance.ConfigData.AskIncludeChildrenRefresh;
            }
        }

        public virtual bool DefaultIncludeChildrenRefresh
        {
            get
            {
                return Kernel.Instance.ConfigData.DefaultIncludeChildrenRefresh;
            }
        }

        /// <summary>
        /// Returns a safe clone of the children
        /// </summary>
        public IList<BaseItem> Children {
            get {
                // return a clone
                lock (ActualChildren) {
                    if (Config.Instance.ParentalControlEnabled && Config.Instance.HideParentalDisAllowed)
                        return parentalAllowedChildren;
                    else
                        return ActualChildren.ToList();
                }
            }
        }

        public void Sort(IComparer<BaseItem> sortFunction) {
            Sort(sortFunction, true);
        }


        public virtual void ValidateChildren() {
            // we never want 2 threads validating children at the same time
            lock (validateChildrenLock) {
                ValidateChildrenImpl();
            }
        }

        public bool Watched {
            set {
                foreach (var item in this.EnumerateChildren()) {
                    var video = item as Video;
                    if (video != null) {
                        video.PlaybackStatus.WasPlayed = value;
                        video.PlaybackStatus.Save();
                    }
                    var folder = item as Folder;
                    if (folder != null) {
                        folder.Watched = value;
                    }
                }
            }
        }

        public int UnwatchedCount {
            get {
                int count = 0;

                // it may be expensive to bring in the playback status 
                // so don't lock up the object during.
                foreach (var item in this.Children) {
                    var video = item as Video;
                    if (video != null && !video.PlaybackStatus.WasPlayed) {
                        count++;
                    } else {
                        var folder = item as Folder;
                        if (folder != null) {
                            count += folder.UnwatchedCount;
                        }
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Will search all the children recursively
        /// </summary>
        /// <param name="searchFunction"></param>
        /// <returns></returns>
        public Index Search(Func<BaseItem, bool> searchFunction, string name) {
            var items = new Dictionary<Guid,BaseItem>();

            foreach (var item in RecursiveChildren) {
                if (searchFunction(item) && !item.IsTrailer && (!Config.Instance.ExcludeRemoteContentInSearch || !item.IsRemoteContent)) {
                    var ignore = item.BackdropImages; //force these to load
                    items[item.Id] = item;
                }
            }
            return new Index(this, items.Values.ToList());
        }

        class BaseItemIndexComparer : IEqualityComparer<BaseItem> {

            public bool Equals(BaseItem x, BaseItem y) {
                return x.Name.Equals(y.Name);
            }

            public int GetHashCode(BaseItem item) {
                return item.Name.GetHashCode();
            }
        }

        private IEnumerable<BaseItem> MapStringsToBaseItems(IEnumerable<string> strings, Func<string, BaseItem> func) {
            if (strings == null) return null;

            return strings
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .Select(s => func(s));
        }

        protected virtual Func<string, BaseItem> GetConstructor(string property) {
            switch (property) {
                case "Actors":
                case "Directors":
                    return a => Person.GetPerson(a);

                case "Genres":
                    return g => Genre.GetGenre(g);

                case "ProductionYear":
                    return y => Year.GetYear(y);

                case "Studios":
                    return s => Studio.GetStudio(s);

                default:
                    return i => GenericItem.GetItem(i);
            }
        }

        public virtual IList<Index> IndexBy(string property)
        {

            if (string.IsNullOrEmpty(property)) throw new ArgumentException("Index type should not be none!");
            var index = Kernel.Instance.ItemRepository.RetrieveIndex(this, property, GetConstructor(property));
            //build in images
            Async.Queue("Index image builder", () =>
            {
                foreach (var item in index)
                {
                    if (item.PrimaryImage == null) //this will keep us from blanking out images that are already there and the source is not available
                        item.RefreshMetadata();
                }
            });

            return index;
        }

        private static BaseItem UnknownItem(IndexType indexType) {

            const string unknown = "<Unknown>";

            switch (indexType)
            {
                case IndexType.Director:
                case IndexType.Actor:
                    return Person.GetPerson(unknown);
                case IndexType.Studio:
                    return Studio.GetStudio(unknown);
                case IndexType.Year:
                    return Year.GetYear(unknown);
                default:
                    return Genre.GetGenre(unknown);
            }
        }

        /// <summary>
        /// A recursive enumerator that walks through all the sub children
        /// that are not hidden by parental controls.  Use for UI operations.
        ///   Safe for multithreaded use, since it operates on list clones
        /// </summary>
        public virtual IEnumerable<BaseItem> RecursiveChildren {
            get {
                foreach (var item in Children) {
                    if (item.ParentalAllowed || !Config.Instance.HideParentalDisAllowed)
                        yield return item;
                    var folder = item as Folder;
                    if (folder != null) {
                        //leave out protected folders (except the ones we have entered)
                        if (folder.ParentalAllowed || Kernel.Instance.ProtectedFolderAllowed(folder)) {
                            foreach (var subitem in folder.RecursiveChildren) {
                                yield return subitem;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A recursive enumerator that walks through all the sub children
        /// ignoring parental controls (use only from refresh operations)
        ///   Safe for multithreaded use, since it operates on list clones
        /// </summary>
        public virtual IEnumerable<BaseItem> AllRecursiveChildren
        {
            get
            {
                List<BaseItem> childCopy;
                lock(ActualChildren)
                    childCopy = ActualChildren.ToList();
                foreach (var item in childCopy)
                {
                    yield return item;
                    var folder = item as Folder;
                    if (folder != null)
                    {
                        foreach (var subitem in folder.AllRecursiveChildren)
                        {
                            yield return subitem;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Protected enumeration through children, 
        ///  this has the potential to block out the item, so its not exposed publicly
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<BaseItem> EnumerateChildren() {
            lock (ActualChildren) {
                foreach (var item in ActualChildren) {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Direct access to children 
        /// </summary>
        protected virtual List<BaseItem> ActualChildren {
            get {
                return children.Value;
            }
        }

        protected void OnChildrenChanged(ChildrenChangedEventArgs args) {
            Sort(sortFunction, false);

            if (ChildrenChanged != null) {
                ChildrenChanged(this, args);
            }
        }

        bool ValidateChildrenImpl() {

            location = null;
            int unavailableItems = 0;
            // cache a copy of the children

            var childrenCopy = ActualChildren.ToList(); //changed this to reference actual children so it wouldn't keep mucking up hidden ones -ebr

            var validChildren = GetChildren(false);
            var currentChildren = new Dictionary<Guid, BaseItem>();
            // in case some how we have a non distinct list 
            foreach (var item in childrenCopy) {
                currentChildren[item.Id] = item;
            }

            bool changed = false;
            foreach (var item in validChildren) {
                BaseItem currentChild;
                if (currentChildren.TryGetValue(item.Id, out currentChild)) {
                    if (currentChild != null) {
                        bool thisItemChanged = currentChild.AssignFromItem(item);
                        if (thisItemChanged)
                        {
                            item.RefreshMetadata(MediaBrowser.Library.Metadata.MetadataRefreshOptions.Default);
                            Kernel.Instance.ItemRepository.SaveItem(item);
                        }
                        
                        currentChildren[item.Id] = null;
                    }
                } else {
                    changed = true;
                    Logger.ReportInfo("Adding new item to library: " + item.Path);
                    lock (ActualChildren) {
                        item.Parent = this;
                        ActualChildren.Add(item);
                        item.RefreshMetadata(MediaBrowser.Library.Metadata.MetadataRefreshOptions.Force); //necessary to get it to show up without user intervention
                        Kernel.Instance.ItemRepository.SaveItem(item);
                        if (item is Folder)
                        {
                            (item as Folder).ValidateChildren();
                        }
                    }
                }
            }

            foreach (var item in currentChildren.Values.Where(item => item != null))
            {
                if (FolderMediaLocation != null && FolderMediaLocation.IsUnavailable(item.Path))
                {
                    Logger.ReportInfo("Not removing missing item " + item.Name + " because its location is unavailable.");
                    unavailableItems++;
                }
                else
                {
                    changed = true;
                    Logger.ReportInfo("Removing missing item from library: (" + item.Id + ") " + item.Path);
                    lock (ActualChildren)
                    {
                        ActualChildren.RemoveAll(current => current.Id == item.Id);
                    }
                }

            }

            // this is a rare concurrency bug workaround - which I already fixed (it protects against regressions)
            if (!changed && childrenCopy.Count != (validChildren.Count + unavailableItems)) {
                Logger.ReportWarning("For some reason we have duplicate items in folder "+Name+", fixing this up!");
                childrenCopy = childrenCopy
                    .Distinct(i => i.Id)
                    .ToList();

                lock (ActualChildren) {
                    ActualChildren.Clear();
                    ActualChildren.AddRange(childrenCopy);
                }

                changed = true;
            }


            if (changed) {
                SaveChildren(Children);
                OnChildrenChanged(new ChildrenChangedEventArgs { FolderContentChanged = true });
            }
            return changed;
        }

        List<BaseItem> GetChildren(bool allowCache) {

            List<BaseItem> items = null;
            if (allowCache) {
                items = GetCachedChildren();
            }

            if (items == null) {
                items = GetNonCachedChildren();

                if (allowCache) {
                    SaveChildren(items, true);
                }
            }

            SetParent(items);
            return items;
        }

        protected virtual List<BaseItem> GetNonCachedChildren() {

            List<BaseItem> items = new List<BaseItem>();

            // don't bomb out on invalid folders - its correct to say we have no children
            if (this.FolderMediaLocation != null) {
                foreach (var location in this.FolderMediaLocation.Children) {
                    if (location != null) {
                        try
                        {
                            var item = Kernel.Instance.GetItem(location);
                            if (item != null) {
                                items.Add(item);
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.ReportException("Error trying to load item from file system: " + location.Path, e);
                        }

                    }
                }
            }
            return items;
           
        }

        protected void SaveChildren(IList<BaseItem> items)
        {
            SaveChildren(items, false);
        }

        protected void SaveChildren(IList<BaseItem> items, bool saveIndvidualChidren) {
            Kernel.Instance.ItemRepository.SaveChildren(Id, items.Select(i => i.Id));
            if (saveIndvidualChidren)
            {
                foreach (var item in items)
                {
                    Kernel.Instance.ItemRepository.SaveItem(item); 
                }
            }
        }

        void SetParent(List<BaseItem> items) {
            foreach (var item in items) {
                item.Parent = this;
            }
        }

        void AddItemToIndex(Dictionary<BaseItem, List<BaseItem>> index, BaseItem item, BaseItem child) {
            List<BaseItem> subItems;
            if (!index.TryGetValue(item, out subItems)) {
                subItems = new List<BaseItem>();
                index[item] = subItems;
            }
            if (child is Episode)
            {
                //we want to group these by series - find or create a series head
                Episode episode = child as Episode;
                Folder currentSeries = episode.Parent is IndexFolder ? episode.Parent : episode.Series; //may already be indexed
                IndexFolder series = (IndexFolder)index[item].Find(i => i.Id == (item.Name+currentSeries.Name).GetMD5());
                if (series == null)
                {
                    series = new IndexFolder() { 
                        Id = (item.Name+currentSeries.Name).GetMD5(),
                        Name = currentSeries.Name,
                        Overview = currentSeries.Overview,
                        PrimaryImagePath = currentSeries.PrimaryImagePath,
                        SecondaryImagePath = currentSeries.SecondaryImagePath,
                        BannerImagePath = currentSeries.BannerImagePath,
                        BackdropImagePaths = currentSeries.BackdropImagePaths
                    };
                    index[item].Add(series);
                }
                series.AddChild(episode);
            }
            else
            {
                if (!(child is Season)) subItems.Add(child); //never want seasons
            }
        }

        void Sort(IComparer<BaseItem> sortFunction, bool notifyChange) {
            this.sortFunction = sortFunction;
            lock (ActualChildren) {
                ActualChildren.Sort(sortFunction);
            }
            if (notifyChange && ChildrenChanged != null)
                {
                    ChildrenChanged(this, null);
                }
        }

        List<BaseItem> GetCachedChildren() {
            List<BaseItem> items = null;
            //using (new MediaBrowser.Util.Profiler(this.Name + " child retrieval"))
            {
                //Logger.ReportInfo("Getting Children for: "+this.Name);
                var children = Kernel.Instance.ItemRepository.RetrieveChildren(Id);
                items = children != null ? children.ToList() : null;
            }
            return items;
        }

        public bool HasVideoChildren {
            get {
                return this.RecursiveChildren.Select(i => i as Video).Where(v => v != null).Count() > 0;
            }
        }

        public ThumbSize ThumbDisplaySize
        {
            get
            {
                if (this.ActualChildren.Count > 0) //if we have no children, nothing to display
                {
                    Guid id = this.Id;
                    if (Config.Instance.EnableSyncViews)
                    {
                        if (this.GetType() != typeof(Folder))
                        {
                            id = this.GetType().FullName.GetMD5();
                        }
                    }

                    ThumbSize s = Kernel.Instance.ItemRepository.RetrieveThumbSize(id) ?? new ThumbSize(Kernel.Instance.ConfigData.DefaultPosterSize.Width, Kernel.Instance.ConfigData.DefaultPosterSize.Height);
                    float f = this.ActualChildren[0].PrimaryImage != null ? this.ActualChildren[0].PrimaryImage.Aspect : 1; //just use the first child as our guide
                    if (f == 0)
                        f = 1;
                    if (s.Width < 10) { s.Width = Config.Instance.DefaultPosterSize.Width; s.Height = Config.Instance.DefaultPosterSize.Height; }
                    float maxAspect = s.Height / s.Width;
                    if (f > maxAspect)
                        s.Width = (int)(s.Height / f);
                    else
                        s.Height = (int)(s.Width * f);
                    return s;
                }
                else return new ThumbSize(Config.Instance.DefaultPosterSize.Width, Config.Instance.DefaultPosterSize.Height);
            }
        }

    }
}
