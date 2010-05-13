using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Globalization;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Logging;
using Microsoft.MediaCenter.UI;


namespace MediaBrowser.Library.Localization
{
    public class LocalizedStrings
    {
        private List<object> data = new List<object>();
        public PropertySet LocalStrings = new PropertySet();
        private PropertySet localStringsReverse = new PropertySet();


        public LocalizedStrings()
        {
            //start with our main string data - others can be added at a later time
            AddStringData(BaseStrings.FromFile(LocalizedStringData.GetFileName()));
        }

        public void AddStringData(object stringData)
        {
            //translate our object definition into a properyset for mcml lookups
            // and a reverse dictionary so we can lookup keys by value
            foreach (var field in stringData.GetType().GetFields())
            {
                if (field != null)
                {
                    try
                    {
                        LocalStrings.Entries.Add(field.Name, field.GetValue(stringData) as string);
                        localStringsReverse.Entries.Add(field.GetValue(stringData) as string, field.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.ReportException("Error adding string element.", ex);
                    }
                }
            }

        }

        public string GetString(string key)
        {
            //return the string from our propertyset
            return LocalStrings[key] as string;
        }

        public string GetKey(string str)
        {
            //return the key from our reverse-lookup dictionary
            return localStringsReverse[str] as string;
        }

    }
}
