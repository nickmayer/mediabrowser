using System;
using System.Collections.Generic;
using Microsoft.MediaCenter.UI;
using System.Diagnostics;
using Microsoft.MediaCenter;
using MediaBrowser.Code.ModelItems;
using System.IO;
using MediaBrowser.Library.Playables;
using MediaBrowser.Library.Entities;
using MediaBrowser.Code;
using System.Threading;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Threading;
using MediaBrowser.Library.Metadata;
using MediaBrowser.Library.RemoteControl;
using MediaBrowser.Library.Logging;


namespace MediaBrowser.Library
{

    public partial class Item : BaseModelItem
    {
        static Item blank;
        static Dictionary<Type, ItemType> itemTypeMap;
        static Item() {
            itemTypeMap = new Dictionary<Type, ItemType>();
            itemTypeMap[typeof(Episode)] = ItemType.Episode;
            itemTypeMap[typeof(Movie)] = ItemType.Movie;
            itemTypeMap[typeof(Series)] = ItemType.Series;
            itemTypeMap[typeof(Season)] = ItemType.Season;
            itemTypeMap[typeof(MediaBrowser.Library.Entities.Folder)] = ItemType.Folder;

            blank = new Item();
            BaseItem item = new BaseItem();
            item.Path = "";
            item.Name = "";
            blank.Assign(item);
            
        }

        object loadMetadatLock = new object();
        protected object watchLock = new object();

        PlayableItem playable;
        private PlaybackStatus playstate;
        protected BaseItem baseItem; 
        
        protected int unwatchedCountCache = -1;


        #region Item Construction
        internal Item()
        {
        }

        internal virtual void Assign(BaseItem baseItem)
        {
            this.baseItem = baseItem;
            baseItem.MetadataChanged += new EventHandler<MetadataChangedEventArgs>(MetadataChanged);
        }

        #endregion

        public BaseItem BaseItem { get { return baseItem;  } }

        public FolderModel PhysicalParent { get; internal set; }

        public Guid Id { get { return baseItem.Id; } }

        public virtual void NavigatingInto()
        {
        }

        public bool IsVideo
        {
            get
            {
               return(baseItem is Video);
            }
        }

        public bool IsNotVideo
        {
            get {
                bool isVideo = (baseItem is Video) || (baseItem is Movie);
                return (baseItem is Folder) ? !((baseItem as Folder).HasVideoChildren) : !isVideo;
            } 
        }

        // having this in Item and not in Folder helps us avoid lots of messy mcml 
        public virtual bool ShowNewestItems {
            get {
                return false;
            }
        }

        public string Name {
            get {
                return BaseItem.Name;
            }
        }

        public string LongName {
            get {
                return BaseItem.LongName;
            }
        }

        public string Path {
            get {
                return baseItem.Path;
            }
        }

        public DateTime CreatedDate {
            get {
                return baseItem.DateCreated;
            }
        }

        public string CreatedDateString
        {
            get
            {
                return baseItem.DateCreated.ToShortDateString();
            }
        }

        
        public ItemType ItemType {
            get {
                ItemType type;
                if (!itemTypeMap.TryGetValue(baseItem.GetType(), out type)) {
                    type = ItemType.None;
                }
                return type;
            }
        }

        public string ItemTypeString {
            get {
                return ItemType.ToString();
            }
        }

        public string MediaTypeString {
            get {
                string mediaType = "";
                var video = baseItem as Video;
                if (video != null) {
                    mediaType = video.MediaType.ToString().ToLower();
                }
                return mediaType;
            }
        }

        public bool IsRoot {
            get {
                return baseItem.Id == Application.CurrentInstance.RootFolder.Id;
            }
        }

        #region Playback
        public bool SupportsMultiPlay {
            get {
                return baseItem is Folder;
            }
        }

        public bool ParentalAllowed { get { return Kernel.Instance.ParentalControls.Allowed(this); } }
        public string ParentalRating
        {
            get
            {
                return baseItem.ParentalRating;
            }
        }

        private void Play(bool resume, bool queue)
        {
            if (this.IsPlayable || this.IsFolder)
            {
                if (Config.Instance.ParentalControlEnabled && !this.ParentalAllowed)
                {
                    Application.CurrentInstance.DisplayPopupPlay = false; //PIN screen mucks with turning this off
                    Kernel.Instance.ParentalControls.PlayProtected(this, resume, queue);
                }
                else PlaySecure(resume, queue);
            }
        }

        public void PlaySecure(bool resume, bool queue)
        {
            try
            {
                if (this.IsPlayable || this.IsFolder) {

                    if (PlayableItem.PlaybackController != Application.CurrentInstance.PlaybackController && PlayableItem.PlaybackController.RequiresExternalPage)
                    {
                        Application.CurrentInstance.OpenExternalPlaybackPage(this);
                    }
                    this.PlayableItem.QueueItem = queue;                    
                    this.PlayableItem.Play(this.PlayState, resume);
                    if (!this.IsFolder && this.PhysicalParent != null) this.PhysicalParent.AddNewlyWatched(this); //add to recent watched list if not a whole folder
                }
            }
            catch (Exception)
            {
                MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
                ev.Dialog("There was a problem playing the content. Check location exists\n" + baseItem.Path, "Content Error", DialogButtons.Ok, 60, true);
            }
        }


        private void Play(bool resume)
        {
            Play(resume, false);
        }


        public void Queue()
        {
            Play(false, true);
        }

        public void Play() {
            Play(false);
        }
        public void Resume() {
            Play(true);
        }

        public bool CanResume { 
            get {
                return PlayState==null?false:PlayState.CanResume;
            } 
        }
        public string RecentDateString
        {
            get
            {
                switch (Application.CurrentInstance.RecentItemOption)
                {
                    case "watched":
                        string runTimeStr = "";
                        string watchTimeStr = "";
                        if (this.PlayState.PositionTicks > 0)
                        {
                            TimeSpan watchTime = new TimeSpan(this.PlayState.PositionTicks);
                            watchTimeStr = " "+watchTime.TotalMinutes.ToString("F0")+" ";
                            if (!String.IsNullOrEmpty(this.RunningTimeString))
                            {
                                runTimeStr = "of " + RunningTimeString;
                            }
                        }
                        return "Watched" + watchTimeStr + runTimeStr + " on " + LastPlayedString;
                    default:
                        return "Added on "+CreatedDateString;
                }
            }
        }
        public void RecentItemsChanged()
        {
            FirePropertyChanged("RecentItems");
            //if (this is FolderModel)
            //{
            //    FolderModel f = this as FolderModel;
            //    f.RefreshUI();
            //}
        }
        public string LastPlayedString {
            get {
                if (PlayState == null) return "";
                return PlayState.LastPlayed == DateTime.MinValue ? "" : PlayState.LastPlayed.ToShortDateString();
            }
        }

        private PlaybackStatus PlayState
        {
            get
            {
                if (playstate == null)
                {

                    Media media = baseItem as Media;

                    if (media != null)
                    {
                        playstate = media.PlaybackStatus;
                        // if we want any chance to reclaim memory we are going to have to use 
                        // weak event handlers
                        playstate.WasPlayedChanged += new EventHandler<EventArgs>(PlaybackStatusPlayedChanged);
                        PlaybackStatusPlayedChanged(this, null);
                    }
                }
                return playstate;
            }
        }

        void PlaybackStatusPlayedChanged(object sender, EventArgs e) {
            lock (watchLock)
                unwatchedCountCache = -1;
            FirePropertyChanged("HaveWatched");
            FirePropertyChanged("UnwatchedCount");
            FirePropertyChanged("ShowUnwatched");
            FirePropertyChanged("UnwatchedCountString");
            FirePropertyChanged("PlayState");
        }

        #endregion

        #region watch tracking

        public bool HaveWatched
        {
            get
            {
                return UnwatchedCount == 0;
            }
        }

        public bool ShowUnwatched
        {
            get { return ((Config.Instance.ShowUnwatchedCount) && (this.UnwatchedCountString.Length > 0)); }
        }

        public string UnwatchedCountString
        {
            get
            {
                if (this.IsPlayable)
                    return "";
                int i = this.UnwatchedCount;
                return (i == 0) ? "" : i.ToString();
            }
        }
       
        public virtual int UnwatchedCount
        {
            get
            {
                int count = 0;
                if (baseItem is Video)
                {
                    var video = baseItem as Video;
                    if (video != null && !video.PlaybackStatus.WasPlayed) {
                        count = 1;
                    }
                }
                return count;
            }
        }

        public void ToggleWatched()
        {
            Logger.ReportVerbose("Start ToggleWatched() initial value: " + HaveWatched.ToString());
            SetWatched(!this.HaveWatched);
            lock (watchLock)
                unwatchedCountCache = -1;
            FirePropertyChanged("HaveWatched");
            FirePropertyChanged("UnwatchedCount");
            FirePropertyChanged("ShowUnwatched");
            FirePropertyChanged("UnwatchedCountString");
            Logger.ReportVerbose("  ToggleWatched() changed to: " + HaveWatched.ToString());
            //HACK: This sort causes errors in detail lists, further debug necessary
            //this.PhysicalParent.Children.Sort();
        }

        internal virtual void SetWatched(bool value)
        {
            if (IsPlayable) {
                if (value != HaveWatched) {
                    if (value && PlayState.PlayCount == 0) {
                        PlayState.PlayCount = 1;
                        Application.CurrentInstance.Information.AddInformationString("Set Watched " + this.Name);
                    } else {
                        PlayState.PlayCount = 0;
                        Application.CurrentInstance.Information.AddInformationString("Clear Watched " + this.Name);
                    }
                    PlayState.Save();
                    lock (watchLock)
                        unwatchedCountCache = -1;
                }
            }
            
        }

        #endregion


        #region Metadata loading and refresh

        public void RefreshMetadata()
        {
            Application.CurrentInstance.Information.AddInformationString("Refresh " + this.Name);
            Async.Queue(() => { 
                baseItem.RefreshMetadata(MetadataRefreshOptions.Force); 
                // force images to reload
                primaryImage = null;
                bannerImage = null;
                primaryImageSmall = null;
            });
        }

        #endregion


        public bool IsPlayable {
            get {
                return baseItem is Media;
            }
        }

        public bool IsFolder
        {
            get
            {
                return baseItem is Folder;
            }
        }


        // this is a shortcut for MCML
        public void ProcessCommand(RemoteCommand command) {
            PlayableItem.PlaybackController.ProcessCommand(command);
        }

        public IPlaybackController PlaybackController
        {
            get
            {
                return this.PlayableItem.PlaybackController;
            }
        }

        internal PlayableItem PlayableItem {
            get {
                if (!IsPlayable && !IsFolder) return null;

                Media media = baseItem as Media;

                if (media != null && playable == null)
                    lock (this)
                        if (playable == null) {
                            playable = PlayableItemFactory.Instance.Create(media);
                        }

                if (playable != null)
                    return playable;

                Folder folder = baseItem as Folder;
                if (folder != null && playable == null)
                    lock (this)
                        if (playable == null)
                        {
                            playable = PlayableItemFactory.Instance.Create(folder);                            
                        }

                return playable;
            }
        }

    }


}
