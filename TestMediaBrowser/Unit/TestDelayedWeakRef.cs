using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using MediaBrowser.Library.Threading;
using System.Threading;
namespace TestMediaBrowser.Unit {
    [TestFixture]
    public class TestDelayedWeakRef {

        [Test]
        public void TestDelayedFlush() {
            var delayedRef = new DelayedWeakReference<object>(new object(), 5);
            Assert.IsNotNull(delayedRef.Value);
            Thread.Sleep(10);
            GC.Collect(2);
            GC.WaitForFullGCComplete();
            Assert.IsNull(delayedRef.Value);
        }
    }
}
