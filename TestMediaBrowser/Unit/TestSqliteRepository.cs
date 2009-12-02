using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Providers;
using MediaBrowser.Library.Interfaces;

namespace TestMediaBrowser.Unit {
    [TestFixture]
    public class TestSqliteRepository {

        public SqliteItemRepository GetRepo() {
            var path = System.IO.Path.GetFullPath("../../../Sqlite/System.Data.SQLite.dll");
            return SqliteItemRepository.GetRepository("test.db", path);
        }

        [Test]
        public void TestChildrenStorage() {
            Guid[] children = { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            Guid parent = Guid.NewGuid();

            SqliteItemRepository repository = GetRepo();
            repository.SaveChildren(parent, children);
            var items = repository.RetrieveChildren(parent);

            Assert.IsTrue(children.OrderBy(_ => _).SequenceEqual(items.OrderBy(_ => _)));
        }

        public void TestBaseItemStorage() {

            Episode episode = new Episode();
            episode.Id = Guid.NewGuid();
            episode.Name = "This is it";

            SqliteItemRepository repository = GetRepo();
            repository.SaveItem(episode);
            var copy = repository.RetrieveItem(episode.Id);

            Assert.AreEqual(copy.Name , episode.Name);
        
        }

        public void TestFatBaseItemStorage() {

            Episode episode = new Episode();
            episode.Id = Guid.NewGuid();
            episode.Name = new string('X', 10000);

            SqliteItemRepository repository = GetRepo();
            repository.SaveItem(episode);
            var copy = (Episode)repository.RetrieveItem(episode.Id);

            Assert.AreEqual(copy.Name, episode.Name);
        }

        public void TestPlayStateStorage() {
            PlaybackStatus state = new PlaybackStatus();
            state.Id = Guid.NewGuid();
            state.LastPlayed = DateTime.Now;
            state.PlayCount = 10;
            state.PlaylistPosition = 1;
            state.LastPlayed = DateTime.Now;

            SqliteItemRepository repository = GetRepo();
            repository.SavePlayState(state);
            var clone = repository.RetrievePlayState(state.Id);

            Assert.AreEqual(state.Id, clone.Id);
            Assert.AreEqual(state.LastPlayed, clone.LastPlayed);
            Assert.AreEqual(state.PlayCount, clone.PlayCount);
            Assert.AreEqual(state.PlaylistPosition, clone.PlaylistPosition);
            Assert.AreEqual(state.LastPlayed, clone.LastPlayed);
        }

        class DummyProvider : BaseMetadataProvider {


            public override void Fetch() {
                throw new NotImplementedException();
            }

            public override bool NeedsRefresh() {
                throw new NotImplementedException();
            }

            [Persist]
            public int Velocity {get; set;} 
        }

        public void TestProviders() {

            List<IMetadataProvider> providers = new List<IMetadataProvider>() { 
                new DummyProvider() {Velocity = 1}, new DummyProvider() {Velocity = 99}
            };

            SqliteItemRepository repository = GetRepo();
            Guid id = Guid.NewGuid();
            repository.SaveProviders(id, providers);
            var clone = repository.RetrieveProviders(id).OrderBy(i => ((DummyProvider)i).Velocity).ToList();

            Assert.AreEqual(clone.Count, 2);
            Assert.AreEqual(((DummyProvider)clone[0]).Velocity, 1);
            Assert.AreEqual(((DummyProvider)clone[1]).Velocity, 99);


        }
    }
}
