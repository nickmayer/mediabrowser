using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.LibraryManagement;
using System.IO;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Configuration;

namespace MediaBrowser.Library.Providers
{
    [ProviderPriority(20)]
    [SupportedType(typeof(BaseItem))]
    class ImageByNameProvider : ImageFromMediaLocationProvider
    {

        protected override string Location
        {
            get {

                if (string.IsNullOrEmpty(Item.Name)) return "";

                string location = Config.Instance.ImageByNameLocation;
                if ((location == null) || (location.Length == 0))
                    location = Path.Combine(ApplicationPaths.AppConfigPath, "ImagesByName");

                //sub-folder is based on the type of thing we're looking for
                switch (Path.GetExtension(Item.GetType().ToString())) //cheap way to grab the type name without all the prefix
                {
                    case ".Genre":
                        location = Path.Combine(location, "Genre");
                        break;
                    case ".Actor":
                    case ".Person":
                        location = Path.Combine(location, "People");
                        break;
                    case ".Studio":
                        location = Path.Combine(location, "Studio");
                        break;
                    case ".Year":
                        location = Path.Combine(location, "Year");
                        break;
                    default:
                        location = Path.Combine(location, "General");
                        break;
                }
                char[] invalid = Path.GetInvalidFileNameChars();

                string name = Item.Name;
                foreach (char c in invalid)
                    name = name.Replace(c.ToString(), "");
                return Path.Combine(location, name);
            }
        }

        
    }
}
