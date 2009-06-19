using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MediaBrowser.Library.Providers;
using MediaBrowser.Library.Metadata;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library.Entities;

namespace TestMediaBrowser {
    [TestFixture]
    public class TestMetadataProvider {

        [SupportedType(typeof(BaseItem))]
        [SlowProvider]
        class SlowProvider : BaseMetadataProvider {
            public override void Fetch() {
                throw new NotImplementedException();
            }

            public override bool NeedsRefresh() {
                throw new NotImplementedException();
            }
        }

        [SupportedType(typeof(BaseItem))]
        [RequiresInternet]
        class RequiresInternetProvider : BaseMetadataProvider {
            public override void Fetch() {
                throw new NotImplementedException();
            }

            public override bool NeedsRefresh() {
                throw new NotImplementedException();
            }
        } 


        [Test]
        public void TestImageFromMediaLocationProviderIsFirst() {
            Assert.AreEqual(typeof(VirtualFolderProvider), MetadataProviderHelper.ProviderTypes[0]);
            Assert.AreEqual(typeof(ImageFromMediaLocationProvider), MetadataProviderHelper.ProviderTypes[1]);
        }

        [Test]
        public void TestRequiresInternetWorks() {
            var p = new RequiresInternetProvider();
            Assert.IsTrue(p.RequiresInternet);
            Assert.IsFalse(p.IsSlow);
        }

        [Test]
        public void TestSlowProviderWorks() {
            var p = new SlowProvider();
            Assert.IsFalse(p.RequiresInternet);
            Assert.IsTrue(p.IsSlow);
        }

        [Test]
        public void TestStaticIsNotPlayingTricks() {
            TestRequiresInternetWorks();
            TestSlowProviderWorks();
        }
    }
}
