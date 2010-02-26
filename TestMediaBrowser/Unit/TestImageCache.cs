using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NUnit.Framework;
using MediaBrowser.Library.ImageManagement;
using System.Drawing;

namespace TestMediaBrowser.Unit {
    [TestFixture]
    public class TestImageCache {

        string imagePath1 = Path.Combine(Environment.CurrentDirectory, @"..\..\SampleMedia\Images\image.png");
        string imagePath2 = Path.Combine(Environment.CurrentDirectory, @"..\..\SampleMedia\Images\image2.png");
        string tempPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\TempCache"));

        [SetUp]
        public void Setup() {
            Directory.CreateDirectory(tempPath); 
        }

        [TearDown]
        public void TearDown() {
            Directory.Delete(tempPath, true);
        }


        [Test]
        public void TestReturnsNullIfImageNotStorred() {
            ImageCache cache = new ImageCache(tempPath);
            Assert.IsNull(cache.GetImagePath(Guid.NewGuid())); 
            Assert.IsNull(cache.GetImagePath(Guid.NewGuid(),100, 100));
        }

        [Test]
        public void TestImageStorage() {
            ImageCache cache = new ImageCache(tempPath);
            Guid id = Guid.NewGuid();
            Image image = Image.FromFile(imagePath1);
            cache.CacheImage(id, image);
            Assert.IsTrue(File.Exists(cache.GetImagePath(id)));
        }

        [Test]
        public void TestCacheLoadsExistingPrimaryImage() {
            ImageCache cache = new ImageCache(tempPath);
            Guid id = Guid.NewGuid();
            Image image = Image.FromFile(imagePath1);
            cache.CacheImage(id, image);

            ImageCache cache2 = new ImageCache(tempPath);
            Assert.IsTrue(File.Exists(cache2.GetImagePath(id)));
        }

        [Test]
        public void TestImageResize() {
            ImageCache cache = new ImageCache(tempPath);
            Guid id = Guid.NewGuid();
            Image image = Image.FromFile(imagePath1);
            cache.CacheImage(id, image);

            Assert.IsTrue(File.Exists(cache.GetImagePath(id, 50, 50))); 
        }

        [Test]
        public void TestCacheLoadOfResize() {
            ImageCache cache = new ImageCache(tempPath);
            Guid id = Guid.NewGuid();
            Image image = Image.FromFile(imagePath1);
            cache.CacheImage(id, image);
            cache.GetImagePath(id, 50, 50);

            ImageCache cache2 = new ImageCache(tempPath);

            Assert.IsTrue(cache2.AvailableSizes(id).Exists(_ => _.Height == 50 && _.Width == 50));
        }

        [Test]
        public void TestCacheUpgrade() {
            Guid upgradeId = Guid.NewGuid();
            File.Copy(imagePath1, Path.Combine(tempPath, upgradeId.ToString() + ".png"));

            ImageCache cache = new ImageCache(tempPath);
            Assert.IsTrue(File.Exists(cache.GetImagePath(upgradeId)));
        } 

    }
}
