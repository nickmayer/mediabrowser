using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MediaBrowser.Library.Util;

namespace TestMediaBrowser {

    
    [TestFixture]
    public class TestBreadCrumbs {

        [Test]
        public void TestToStringWorksProperly() {
            BreadCrumbs crumbs = new BreadCrumbs(2);
            crumbs.Push("sam");
            crumbs.Push("hello");
            crumbs.Push("world");

            Assert.AreEqual(3, crumbs.Count());
            Assert.AreEqual("hello | world", crumbs.ToString());
        }

        [Test]
        public void TestLinkedCrumbs() {
            BreadCrumbs crumbs = new BreadCrumbs(2);
            crumbs.Push("bob");
            crumbs.Push("linked", true);
            crumbs.Push("new");
            crumbs.Pop();
            Assert.AreEqual(1, crumbs.Count());
            Assert.AreEqual("bob", crumbs.ToString());
        }

    }
}
