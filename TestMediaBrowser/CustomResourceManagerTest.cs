using MediaBrowser.Library.Util;
using MediaBrowser;
using System.Diagnostics;
using System;
using NUnit.Framework;

namespace TestMediaBrowser
{
    
    
    /// <summary>
    ///This is a test class for CustomResourceManagerTest and is intended
    ///to contain all CustomResourceManagerTest Unit Tests
    ///</summary>
    public class CustomResourceManagerTest
    {


        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for AppendFonts
        ///</summary>
        [Test]
        public void AppendFontsTest1()
        {
            string prefix = "Vanilla";
            byte[] stdFontResource = Resources.TestFonts; // just test with an mcml resource that is available
            bool expected = true; 
            bool actual;
            actual = CustomResourceManager.AppendFonts(prefix, stdFontResource);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for AppendFonts
        ///</summary>
        [Test]
        public void AppendFontsTest()
        {
            string prefix = "Vanilla";
            byte[] stdFontResource = Resources.TestFonts; // just test with an mcml resource that is available
            byte[] smallFontResource = Resources.TestFontsSmall;  //again, just something to verify against
            bool expected = true; // TODO: Initialize to an appropriate value
            bool actual;
            actual = CustomResourceManager.AppendFonts(prefix, stdFontResource, smallFontResource);
            Assert.AreEqual(expected, actual);
        }
    }
}
