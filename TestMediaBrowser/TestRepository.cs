﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MediaBrowser.Library;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Metadata;
using MediaBrowser.Library.Providers;
using MediaBrowser.Library.Interfaces;

namespace TestMediaBrowser {

    class TestProvider : BaseMetadataProvider {

        [Persist]
        public DateTime date = DateTime.MinValue;

        public override void Fetch() {
            throw new NotImplementedException();
        }

        public override bool NeedsRefresh() {
            throw new NotImplementedException();
        }
    }

    [TestFixture]
    public class TestRepository {

        class TempClass : BaseItem { 
            
        }

        [Test]
        public void TestProviderPersistance() {
            List<IMetadataProvider> providers = new List<IMetadataProvider>();
            var provider = new TestProvider();
            provider.date = DateTime.Now;
            providers.Add(provider);
            Guid guid = Guid.Empty;
            Kernel.Instance.ItemRepository.SaveProviders(guid, providers);
            providers = Kernel.Instance.ItemRepository.RetrieveProviders(guid).ToList();
            Assert.AreEqual(provider.date, (providers[0] as TestProvider).date);
        }

        [Test]
        public void TestBackdropsAreDetected() {

            var movie = new Movie();
            movie.Path = @"..\..\SampleMedia\Movies\Flawless";
            ImageFromMediaLocationProvider provider = new ImageFromMediaLocationProvider();
            provider.Item = movie;
            Assert.IsTrue(provider.NeedsRefresh());
            provider.Fetch();
            Assert.AreEqual(2, movie.BackdropImagePaths.Count);

        }

        [Ignore("This is only used to test network performance")]
        [Test]
        public void TestHowLongItTakesToLoadItall() {
            var rootLocation = Kernel.Instance.GetLocation(ApplicationPaths.AppInitialDirPath);
            var root = Kernel.Instance.GetItem <Folder>(rootLocation);

            foreach (var item in root.RecursiveChildren) {
               // Console.WriteLine(item.Path);
            }
        }

        [Test]
        public void TestMediaInfoSavesProperly() {
            Movie movie = new Movie();
            movie.MediaInfo = new MediaInfoData();
            movie.MediaInfo.Height = 10;
            movie.MediaInfo.Width = 20;
            movie.MediaInfo.VideoCodec = "hello";
            movie.MediaInfo.AudioFormat = "goodby";
            movie.MediaInfo.VideoBitRate = 100;
            movie.MediaInfo.AudioBitRate = 200;

            var clone = Serializer.Clone(movie);
            Assert.AreEqual(clone.MediaInfo.Height, movie.MediaInfo.Height);
            Assert.AreEqual(clone.MediaInfo.Width, movie.MediaInfo.Width);
            Assert.AreEqual(clone.MediaInfo.VideoCodec, movie.MediaInfo.VideoCodec);
            Assert.AreEqual(clone.MediaInfo.AudioFormat, movie.MediaInfo.AudioFormat);
            Assert.AreEqual(clone.MediaInfo.VideoBitRate, movie.MediaInfo.VideoBitRate);
            Assert.AreEqual(clone.MediaInfo.AudioBitRate, movie.MediaInfo.AudioBitRate);

        }

        [Test]
        public void TestChildPersistance() {
            var owner = Guid.NewGuid();
            var children = Enumerable.Range(0, 100).Select(i => Guid.NewGuid()).ToArray();
            Kernel.Instance.ItemRepository.SaveChildren(owner, children);

            var childrenCopy = Kernel.Instance.ItemRepository.RetrieveChildren(owner);

            Assert.AreEqual(children.Count(), childrenCopy.Count(), "Expecting counts to match up!");
            Assert.AreEqual(0, childrenCopy.Except(children).Count(), "Expecting all items to be the same!");
        }

        [Test]
        public void TestCustomEntityPersistance() {
            TempClass t = new TempClass();
            t.Id = Guid.NewGuid();
            Kernel.Instance.ItemRepository.SaveItem(t);
            var copy = Kernel.Instance.ItemRepository.RetrieveItem(t.Id);
            Assert.IsInstanceOfType(typeof(TempClass), copy);
        }

        [Test]
        public void TestVideoPersistance() {
            Video video = new Video();
            video.Path = "c:\\test.avi";
            video.MediaType = MediaType.HDDVD;
            video.Id = Guid.NewGuid();

            Kernel.Instance.ItemRepository.SaveItem(video);

            var copy = Kernel.Instance.ItemRepository.RetrieveItem(video.Id) as Video;

            Assert.IsInstanceOfType(typeof(Video), copy);
            Assert.AreEqual(video.Path, copy.Path);
            Assert.AreEqual(video.MediaType, copy.MediaType);
            Assert.AreEqual(video.Id, copy.Id);
        }

        public void TestMoviePersistance() {
            var movie = new Movie();
            movie.Path = "c:\\test";
            movie.MediaType = MediaType.HDDVD;
            movie.Id = Guid.NewGuid();
            movie.Actors = new List<Actor>();
            movie.Actors.Add(new Actor() { Name = "Kevin Spacey" });
            movie.Actors.Add(new Actor() { Name = "Kevin Rudd", Role = "PM" });
            movie.Directors = new List<string>();
            movie.Directors.Add("hello");
            movie.Directors.Add("goodbye");

            Kernel.Instance.ItemRepository.SaveItem(movie);

            var copy = Kernel.Instance.ItemRepository.RetrieveItem(movie.Id) as Movie;

            Assert.IsInstanceOfType(typeof(Video), copy);
            Assert.AreEqual(movie.Path, copy.Path);
            Assert.AreEqual(movie.MediaType, copy.MediaType);
            Assert.AreEqual(movie.Id, copy.Id);

            Assert.AreEqual(2, copy.Actors.Count);
            Assert.AreEqual("Kevin Spacey", copy.Actors[0].Name);
            Assert.AreEqual("Kevin Rudd", copy.Actors[1].Name);
            Assert.AreEqual("PM", copy.Actors[1].Role);

            Assert.AreEqual(2, copy.Directors.Count);
            Assert.AreEqual(copy.Directors[0], "hello");
            Assert.AreEqual(copy.Directors[1], "goodbye");
        
        } 
    }
}
