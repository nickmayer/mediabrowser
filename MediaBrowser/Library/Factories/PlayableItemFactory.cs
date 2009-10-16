using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Playables;

namespace MediaBrowser.Library.Factories {
        
    public class PlayableItemFactory 
    {
        public static PlayableItemFactory Instance = new PlayableItemFactory();

        public delegate bool CanPlay(Media media);

        Dictionary<CanPlay, Type> playableItems;

        public Dictionary<CanPlay, Type> PlayableItems { get { return playableItems; } }

        private PlayableItemFactory()
        {
            playableItems = new Dictionary<CanPlay, Type>();
            PlayableItems.Add(PlayableExternal.CanPlay, typeof(PlayableExternal));
            PlayableItems.Add(PlayableMultiFileVideo.CanPlay, typeof(PlayableMultiFileVideo));
            PlayableItems.Add(PlayableVideoFile.CanPlay, typeof(PlayableVideoFile));
            PlayableItems.Add(PlayableIso.CanPlay, typeof(PlayableIso));
            PlayableItems.Add(PlayableDvd.CanPlay, typeof(PlayableDvd));
        }

        public PlayableItem Create(Media media) {

            PlayableItem playable = null;

            foreach (CanPlay canPlay in PlayableItems.Keys)
                if (canPlay(media))
                {
                    Type type = PlayableItems[canPlay];
                    playable = (PlayableItem) Activator.CreateInstance(type, media);
                    break;
                }

            if (playable == null)
                playable = new PlayableVideoFile(media);
            
            foreach (var controller in Kernel.Instance.PlaybackControllers) {
                if (controller.CanPlay(playable.Filename)) {
                    playable.PlaybackController = controller;
                    return playable;
                }
            }

            return playable;
        
        }

        public PlayableItem Create(Folder folder)
        {            
            PlayableItem playable = null;

            var playableChildren = folder.RecursiveChildren.Select(i => i as Media).Where(v => v != null).OrderBy(v => v.Path);
            playable = new PlayableMediaCollection<Media>(folder.Name, playableChildren, folder.HasVideoChildren);
            playable.PlayableItems = playableChildren.Select(i => i.Path);


            foreach (var controller in Kernel.Instance.PlaybackControllers)
            {
                if (controller.CanPlay(playable.PlayableItems))
                {
                    playable.PlaybackController = controller;
                    return playable;
                }
            }

            return playable;

        }
    }
}
