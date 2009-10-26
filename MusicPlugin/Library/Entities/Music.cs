using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Library.Entities.Attributes;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Extensions;
using System.IO;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library;
using MusicPlugin.LibraryManagement;

namespace MusicPlugin.Library.Entities
{
    public class Music : Media
    {
        [NotSourcedFromProvider]
        [Persist]
        public MediaType MediaType { get; set; }

        [Persist]
        public int? RunningTime { get; set; }

        [Persist]
        public MediaInfoData MediaInfo { get; set; }  //check this

        public override void Assign(IMediaLocation location, IEnumerable<InitializationParameter> parameters, Guid id)
        {
            base.Assign(location, parameters, id);
            Name = location.Name;
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    var mediaTypeParam = parameter as MediaTypeInitializationParameter;
                    if (mediaTypeParam != null)
                    {
                        MediaType = mediaTypeParam.MediaType;
                    }
                }
            }
        }

        public override bool AssignFromItem(BaseItem item)
        {
            bool changed = this.MediaType != ((Music)item).MediaType;
            this.MediaType = ((Music)item).MediaType;
            return changed | base.AssignFromItem(item);
        }

        public override bool RefreshMetadata(MediaBrowser.Library.Metadata.MetadataRefreshOptions options)
        {
            return false;
        }
        public override IEnumerable<string> Files
        {
            get { return MusicFiles; }
        }
        public virtual IEnumerable<string> MusicFiles
        {
            get
            {
                if (MediaLocation is IFolderMediaLocation)
                {
                    foreach (var path in GetChildFiles((IFolderMediaLocation)MediaLocation))
                    {
                        yield return path;
                    }
                }
                else
                {
                    yield return Path;
                }
            }
        }
        
        protected IMediaLocation location;

        public override PlaybackStatus PlaybackStatus
        {
            get
            {

                if (playbackStatus != null) return playbackStatus;

                playbackStatus = Kernel.Instance.ItemRepository.RetrievePlayState(this.Id);
                if (playbackStatus == null)
                {
                    playbackStatus = PlaybackStatusFactory.Instance.Create(Id); // initialise an empty version that items can bind to
                    if (DateCreated <= Kernel.Instance.ConfigData.AssumeWatchedBefore)
                        playbackStatus.PlayCount = 1;
                    playbackStatus.Save();
                }
                return playbackStatus;
            }
        }

        public IMediaLocation MediaLocation
        {
            get
            {
                if (location == null)
                {
                    location = Kernel.Instance.GetLocation<IMediaLocation>(Path);
                }
                return location;
            }
        }

        protected IEnumerable<string> GetChildFiles(IFolderMediaLocation location)
        {
            if (location.Path.EndsWith("$RECYCLE.BIN")) yield break;

            foreach (var child in location.Children)
            {
                if (MusicHelper.IsMusic(child.Path)) yield return child.Path;
                else if (child is IFolderMediaLocation)
                {
                    foreach (var grandChild in GetChildFiles(child as IFolderMediaLocation))
                    {
                        yield return grandChild;
                    }
                }
            }
        }
    }
}
