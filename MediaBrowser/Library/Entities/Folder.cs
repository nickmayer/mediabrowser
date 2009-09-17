using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Library.Util;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Logging;
using System.Collections;
using System.Diagnostics;

namespace MediaBrowser.Library.Entities {

    public class ChildrenChangedEventArgs : EventArgs {
    }

    public class Folder : BaseItem, MediaBrowser.Library.Entities.IFolder {

        public event EventHandler<ChildrenChangedEventArgs> ChildrenChanged;

        Lazy<List<BaseItem>> children;
        protected IFolderMediaLocation location;
        SortOrder sortOrder = SortOrder.Name;
        object validateChildrenLock = new object();

        public Folder()
            : base() {
            children = new Lazy<List<BaseItem>>(() => GetChildren(true), () => OnChildrenChanged(null));
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

        //added by ebr
        private List<BaseItem> parentalAllowedChildren
        {
            get
            {
                // return only the children not protected
                return Kernel.Instance.ParentalControls.RemoveDisallowed(ActualChildren);
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

        public void Sort(SortOrder sortOrder) {
            Sort(sortOrder, true);
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
                    if (video != null && video.PlaybackStatus.PlayCount == 0) {
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
            var items = new List<BaseItem>();

            foreach (var item in RecursiveChildren) {
                if (searchFunction(item)) {
                    items.Add(item);
                }
            }
            return new Index(this, items);
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

        public IList<Index> IndexBy(IndexType indexType) {

            if (indexType == IndexType.None) throw new ArgumentException("Index type should not be none!");
            Func<Show, IEnumerable<BaseItem>> indexingFunction = null;

            switch (indexType) {
                case IndexType.Actor:
                    indexingFunction = show =>
                        show.Actors == null ? null : show.Actors.Select(a => (BaseItem)a.Person);
                    break;

                case IndexType.Director:
                    indexingFunction = show => MapStringsToBaseItems(show.Directors, d => Person.GetPerson(d));
                    break;


                case IndexType.Genre:
                    indexingFunction = show => MapStringsToBaseItems(show.Genres, g => Genre.GetGenre(g));
                    break;

                case IndexType.Year:
                    indexingFunction = show =>
                        show.ProductionYear == null ? null : new BaseItem[] { Year.GetYear(show.ProductionYear.ToString()) };
                    break;
                case IndexType.Studio:
                   // indexingFunction = show => MapStringsToBaseItems(show.Studios, s => Studio.GetStudio(s));
                    break;

                default:
                    break;
            }

            BaseItem unknown = new BaseItem();
            unknown.Name = "<Unknown>";
            unknown.Id = new Guid("{DA12CDDE-7F0B-4376-9E37-D2CAB4B84BF6}");

            var index = new Dictionary<BaseItem, List<BaseItem>>(new BaseItemIndexComparer());
            foreach (var item in RecursiveChildren) {
                Show show = item as Show;
                IEnumerable<BaseItem> subIndex = null;
                if (show != null) {
                    subIndex = indexingFunction(show);
                }
                bool added = false;

                if (subIndex != null) {
                    foreach (BaseItem innerItem in subIndex) {
                        AddItemToIndex(index, innerItem, item);
                        added = true;
                    }
                }

                if (!added && item is Show) {
                    AddItemToIndex(index, unknown, item);
                }

            }

            List<Index> sortedIndex = new List<Index>();

            sortedIndex.AddRange(
                index
                    .Select(pair => new Index(pair.Key, pair.Value))
                );


            sortedIndex.Sort((x, y) =>
            {
                if (x.Children.Count == 1 && y.Children.Count > 1) return 1;
                if (x.Children.Count > 1 && y.Children.Count == 1) return -1;
                return x.Name.CompareTo(y.Name);
            });

            return sortedIndex;
        }

        /// <summary>
        /// A recursive enumerator that walks through all the sub children 
        ///   Safe for multithreaded use, since it operates on list clones
        /// </summary>
        public IEnumerable<BaseItem> RecursiveChildren {
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
            Sort(sortOrder, false);

            if (ChildrenChanged != null) {
                ChildrenChanged(this, args);
            }
        }

        void ValidateChildrenImpl() {
            location = null;
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
                        changed |= currentChild.AssignFromItem(item);
                        currentChildren[item.Id] = null;
                    }
                } else {
                    changed = true;
                    lock (ActualChildren) {
                        item.Parent = this;
                        ActualChildren.Add(item);
                    }
                }
            }

            foreach (var item in currentChildren.Values.Where(item => item != null)) {
                changed = true;
                lock (ActualChildren) {
                    ActualChildren.RemoveAll(current => current.Id == item.Id);
                }
            }

            // this is a rare concurrency bug workaround - which I already fixed (it protects against regressions)
            if (!changed && childrenCopy.Count != validChildren.Count) {
                //Debug.Assert(false,"For some reason we have duplicate items in our folder, fixing this up!");
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
                OnChildrenChanged(null);
            }
        }

        List<BaseItem> GetChildren(bool allowCache) {

            List<BaseItem> items = null;
            if (allowCache) {
                items = GetCachedChildren();
            }

            if (items == null) {
                items = GetNonCachedChildren();

                if (allowCache) {
                    SaveChildren(items);
                }
            }

            SetParent(items);
            return items;
        }

        protected virtual List<BaseItem> GetNonCachedChildren() {
            List<BaseItem> items = new List<BaseItem>();

            foreach (var location in this.FolderMediaLocation.Children) {
                var item = Kernel.Instance.GetItem(location);
                if (item != null) {
                    items.Add(item);
                }
            }
            return items;
        }

        void SaveChildren(IList<BaseItem> items) {
            Kernel.Instance.ItemRepository.SaveChildren(Id, items.Select(i => i.Id));
            foreach (var item in items) {
                Kernel.Instance.ItemRepository.SaveItem(item);
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
            subItems.Add(child);
        }

        void Sort(SortOrder sortOrder, bool notifyChange) {
            this.sortOrder = sortOrder;
            lock (ActualChildren) {
                ActualChildren.Sort(new BaseItemComparer(sortOrder));
            }
            if (notifyChange) OnChildrenChanged(null);
        }

        List<BaseItem> GetCachedChildren() {
            List<BaseItem> items = null;

            var cached = Kernel.Instance.ItemRepository.RetrieveChildren(Id);
            if (cached != null) {
                items = new List<BaseItem>();
                foreach (var guid in cached) {
                    var item = Kernel.Instance.ItemRepository.RetrieveItem(guid);
                    if (item != null) {
                        items.Add(item);
                    }
                }
            }
            return items;
        }

        public bool HasVideoChildren {
            get {
                return this.RecursiveChildren.Select(i => i as Video).Where(v => v != null).Count() > 0;
            }
        }


    }
}
