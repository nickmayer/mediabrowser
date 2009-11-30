using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Library.Entities {
    public abstract class Media : BaseItem{
        PlaybackStatus PlayState {get; set; }
        protected PlaybackStatus playbackStatus;
        public virtual PlaybackStatus PlaybackStatus { get { return playbackStatus; } }
        public abstract IEnumerable<string> Files {get;}

        public override bool PlayAction(Item item)
        {
            Application.CurrentInstance.Play(item);
            return true;
        }
    }
}
