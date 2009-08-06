using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library.Playables
{
    public class PlayableMediaCollection <T> : PlayableCollection where T : Media
    {
        IEnumerable<T> t;

        //probably not good
        public bool ContainsVideoFiles { get; set; }

        public PlayableMediaCollection()
        {
            ContainsVideoFiles = true;
        }

        public PlayableMediaCollection(string name, IEnumerable<T> media)
            : this()
        {
            this.t = media;
            this.name = name;
        }

        public PlayableMediaCollection(string name, IEnumerable<T> media, bool containsVideoFiles)
            : this(name, media)
        {
            this.ContainsVideoFiles = containsVideoFiles;
        }

        public override void Prepare(bool resume)
        {
            var files = t.Select(v2 => v2.Files).SelectMany(i => i);
            base.filename = CreateWPLPlaylist(name, files);
        }
    }
}
