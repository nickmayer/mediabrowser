using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MediaBrowser.Library;

namespace TestMediaBrowser.Integration {

    [TestFixture]
    public class TestKernel {
        
        [Test]
        public void KernelShouldBeReinitializable() {
            Kernel.Init(KernelLoadDirective.ShadowPlugins);
            Kernel.Init(KernelLoadDirective.ShadowPlugins);   
        }
    }
}
