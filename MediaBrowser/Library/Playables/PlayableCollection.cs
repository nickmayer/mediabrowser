using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library.Playables {
    public class PlayableCollection : PlayableItem {

        protected string filename;
        protected string name;

        public PlayableCollection()
        {
        }

        public override void Prepare(bool resume)
        {
        }

        public override string Filename {
            get { return filename; }
        }

    }
}
