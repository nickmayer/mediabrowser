using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MediaBrowser.Library.Entities;

namespace TestMediaBrowser.Unit {
    
    [TestFixture]
    public class TestSorting {

        [Test] 
        public void TestSortNameForEpisodeShouldIncludeEpisodeNumber() {
            Episode episode = new Episode();
            episode.Name = "My Episode";
            episode.EpisodeNumber = "04";
            Assert.AreEqual(episode.SortName, "004 - my episode");
        }

        [Test]
        public void TestSortNameForEpisodeShouldNotIncludeEpisodeNumberWhenMissing() {
            Episode episode = new Episode();
            episode.Name = "my episode";
            episode.EpisodeNumber = null;
            Assert.AreEqual(episode.SortName, episode.Name);
        }
    }
}
