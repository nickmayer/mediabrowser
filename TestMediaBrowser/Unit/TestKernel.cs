using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TestMediaBrowser.SupportingClasses;
using MediaBrowser.Library;

namespace TestMediaBrowser.Unit {
    [TestFixture]
    public class TestKernel {


        [Test]
        public void TestKernelLoadTime() {

            Benchmarking.TimeAction("Kernel Load", () => {
                var kernel = Kernel.Instance;
            });

        }
    
    }
}
