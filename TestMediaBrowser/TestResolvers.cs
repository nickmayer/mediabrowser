using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TestMediaBrowser.SupportingClasses;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library;
using MediaBrowser.Library.EntityDiscovery;

namespace TestMediaBrowser {
    [TestFixture]
    public class TestResolvers {

        [Test]
        public void MovieResolverShouldIgnoreHiddenFiles() {
            MovieResolver resolver = new MovieResolver(2, true);

            var location = new MockMediaLocation("c:\\movie.avi");

            BaseItemFactory factory;
            IEnumerable<InitializationParameter> setup;
            resolver.ResolveEntity(location, out factory, out setup);

            Assert.IsNotNull(factory);

            location.Attributes = System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System;

            resolver.ResolveEntity(location, out factory, out setup);

            Assert.IsNull(factory);
        }

        [Test]
        public void ShouldNotResolve() {
            var root = new MockFolderMediaLocation(); 
            var location = new MockFolderMediaLocation();
            location.Parent = root;
            location.Path = @"c:\A series\Season 08\metadata";

            Assert.IsFalse(location.IsSeriesFolder()); 

            Assert.IsNull(Kernel.Instance.GetItem(location));
        }

        public void WeShouldNotBeResolvingTheRecycleBin() {

            var root = new MockFolderMediaLocation();
            var location = new MockFolderMediaLocation();
            location.Parent = root;
            location.Path = @"c:\$ReCycle.bin";
            Assert.IsNull(Kernel.Instance.GetItem(location));
        }
    }
}
