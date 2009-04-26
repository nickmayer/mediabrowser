﻿using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.LibraryManagement;
using System.IO;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library.Providers
{
    [ProviderPriority(20)]
    [SupportedType(typeof(BaseItem))]
    class ImageByNameProvider : ImageFromMediaLocationProvider
    {

        protected override string Location
        {
            get {
                string location = Config.Instance.ImageByNameLocation;
                if ((location == null) || (location.Length == 0))
                    location = Path.Combine(Helper.AppConfigPath, "ImagesByName");
                char[] invalid = Path.GetInvalidFileNameChars();
                string name = Item.Name;
                foreach (char c in invalid)
                    name = name.Replace(c.ToString(), "");
                return Path.Combine(location, name);
            }
        }

        
    }
}
