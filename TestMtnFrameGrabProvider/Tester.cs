using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MtnFrameGrabProvider;

namespace TestMtnFrameGrabProvider {

    [TestFixture]
    public class Tester {

        [Test]
        public void TestFileExtracts() {
            Plugin.EnsureMtnIsExtracted();
        }

        [Test]
        public void TestTumbnailing() {
            string image = "C:\\Users\\sam\\Desktop\\videos 123\\hello2.jpg";
            ThumbCreator.CreateThumb(@"C:\Users\sam\Desktop\videos 123\01.avi", ref image, 600); 
        } 
    }

}
