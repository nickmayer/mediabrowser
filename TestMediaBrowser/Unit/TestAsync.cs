using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using MediaBrowser.Library.Threading;
using System.Threading;
namespace TestMediaBrowser.Unit {
    [TestFixture]
    public class TestAsync {

        [Test]
        public void TestDelayedQueue() {
            bool done = false;
            Async.Queue("Test", () => { done = true; }, 9);
            Thread.Sleep(5);
            Assert.AreEqual(false, done);
            Thread.Sleep(15);
            Assert.AreEqual(true, done);
        }
    }
}
