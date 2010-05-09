﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Library.Entities;
using TestMediaBrowser.SupportingClasses;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library;

namespace TestMediaBrowser {
    [TestFixture]
    public class TestEntityResolution {


        [Test]
        public void TestNestedMoviesAreAlwaysTreatedAsBoxSets() {
            var resolver = CreateResolver();
            var movie = MockFolderMediaLocation.CreateMockLocation(@"
|Star Wars
 |part 1
  a.avi
  mymovies.xml
 |part 2
  b.avi
  mymovies.xml
");
            
            Assert.AreEqual(resolver.ResolveType(movie), typeof(Folder));

        }

        [Test]
        public void TestNothingIsTruncatedWithDots() {
            var resolver = CreateResolver();
            var movieLocation =  new MockMediaLocation("c:\\Jackass 2.5.avi"); 

            BaseItemFactory factory;
            IEnumerable<InitializationParameter> setup;
            resolver.ResolveEntity(movieLocation, out factory, out setup);

            Movie movie = (Movie)factory.CreateInstance(movieLocation, setup);

            Assert.AreEqual(movie.Name, "Jackass 2.5");

        }

        [Test]
        public void TestLocalTrailerResolution() { 
             var resolver = CreateResolver();
             var movie = MockFolderMediaLocation.CreateMockLocation(@"
|Rushmore
 |part 1
  a.avi
 |part 2
  b.avi
 |trailers
  trailer1.avi
  trailer2.avi");

             Assert.AreEqual(resolver.ResolveType(movie), typeof(Movie));
        }

        [Test]
        public void TestRecusiveMovieResolution()
        {
            var resolver = CreateResolver();
            var movie = MockFolderMediaLocation.CreateMockLocation(@"
|Rushmore
 |part 1
  a.avi
 |part 2
  b.avi
");

            Assert.AreEqual(resolver.ResolveType(movie), typeof(Movie));

        }

        [Test]
        public void TestThatUniqueIdChangesWhenTypeChanges() {
            var root = MockFolderMediaLocation.CreateMockLocation(@"
|Lib
 movie.avi
");
            var id = Kernel.Instance.GetItem(root).Id;

            Assert.AreEqual(id, Kernel.Instance.GetItem(root).Id);

            root = MockFolderMediaLocation.CreateMockLocation(@"
|Lib
  movie.avi
  movie2.avi
  movie3.avi
");

            var id2 = Kernel.Instance.GetItem(root).Id;

            Assert.AreNotEqual(id, id2);
            
        }

        [Test]
        public void TestMetadataFoldersShouldBeIgnored() {
            var resolver = CreateResolver();
            var root = MockFolderMediaLocation.CreateMockLocation(
@"
|TV
 |.metaData
  metadata.xml
  metadata2.xml
 |metadata
  metadata.xml
  metadata2.xml
 a.avi
 b.avi
 c.avi
"
                );

            BaseItemFactory factory;
            IEnumerable<InitializationParameter> setup;

            resolver.ResolveEntity(root.Children[0], out factory, out setup);
            Assert.IsNull(factory);

            resolver.ResolveEntity(root.Children[1], out factory, out setup);
            Assert.IsNull(factory);

        }

        [Test]
        public void TestDVDResolution() {
            var resolver = CreateResolver();
            var root = MockFolderMediaLocation.CreateMockLocation(
@"
|Movies
 |Fight Club
  |video_ts
   movie.vob
 |Spandax
  movie.vob
");
            BaseItemFactory factory;
            IEnumerable<InitializationParameter> setup; 

            resolver.ResolveEntity(root, out factory, out setup);

            Assert.AreEqual(typeof(MediaBrowser.Library.Entities.Folder), factory.EntityType);

            foreach (var child in root.Children) {
                resolver.ResolveEntity(child, out factory, out setup);

                Assert.AreEqual(typeof(Movie), factory.EntityType);
                Assert.AreEqual(MediaType.DVD, (setup.First() as MediaTypeInitializationParameter).MediaType);
            }

        }

        private static ChainedEntityResolver CreateResolver() {
            var resolver = new ChainedEntityResolver() { 
                new MovieResolver(2, true, true),
                new FolderResolver()
            };
            return resolver;
        }
    }
}
