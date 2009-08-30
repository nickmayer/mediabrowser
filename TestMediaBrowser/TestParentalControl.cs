using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TestMediaBrowser.SupportingClasses;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library;
using MediaBrowser.Code;
using MediaBrowser.Code.ModelItems;

namespace TestMediaBrowser
{
    [TestFixture]
    public class TestParentalControl
    {
        ParentalControl parentalControls = new ParentalControl();
        Movie allowedItem;
        Movie lockedItem;
        Movie customItem;
        Movie unratedItem;

        [TestFixtureSetUp]
        public void SetUp()
        {
            parentalControls = new ParentalControl();
            allowedItem = new Movie();
            lockedItem = new Movie();
            customItem = new Movie();
            unratedItem = new Movie();
            allowedItem.MpaaRating = "G";
            lockedItem.MpaaRating = "R";
            customItem.MpaaRating = "R";
            customItem.CustomRating = "PG-13";
            unratedItem.MpaaRating = "";
        }

        [Test]
        public void TestAllowed()
        {
            // Test that each item is treated properly - depends on config file containing max allowed of 3 ("PG-13") and parentalControEnabled true
            Assert.AreEqual(true, parentalControls.Allowed(allowedItem));
            Assert.AreEqual(true,allowedItem.ParentalAllowed);
            Assert.AreEqual(false, parentalControls.Allowed(lockedItem));
            Assert.AreEqual(false,lockedItem.ParentalAllowed);
            Assert.AreEqual(true, parentalControls.Allowed(customItem)); //this should be allowed even though mpaa is "R" due to custom rating
            Assert.AreEqual(true, customItem.ParentalAllowed);
            // now change custom rating and be sure not allowed
            customItem.CustomRating = "NC-17";
            Assert.AreEqual(false, parentalControls.Allowed(customItem));
            Assert.AreEqual(false, customItem.ParentalAllowed);
        }

        [Test]
        public void TestMaxAllowed()
        {
            //<MaxParentalLevel> in config file should be 3 
            Assert.AreEqual(3, parentalControls.MaxAllowed);
            Assert.AreEqual("PG-13", parentalControls.MaxAllowedString);
        }

        [Test]
        public void TestBlockUnrated()
        {
            //first set to block unrated
            parentalControls.SwitchUnrated(true);
            Assert.AreEqual(false, parentalControls.Allowed(unratedItem));
        }

        [Test]
        public void TestAllowUnrated()
        {
            //now set to not block
            parentalControls.SwitchUnrated(false);
            Assert.AreEqual(true, parentalControls.Allowed(unratedItem));
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            parentalControls = null;
            allowedItem = null;
            lockedItem = null;
            customItem = null;
            unratedItem = null;
        }

    }
}
