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


namespace MediaBrowser.Library.Localization
{
    public class LocalizedStrings
    {
        private List<LocalizedStringData> data = new List<LocalizedStringData>();


        public LocalizedStrings()
        {
            //start with our main string data - others can be added at a later time
            this.data.Add(BaseStrings.FromFile(BaseStrings.GetFileName()));
        }

        public void AddStringData(LocalizedStringData stringData)
        {
            this.data.Add(stringData);
        }

        public string GetString(string key)
        {
            //search our data elements until we find it
            foreach (LocalizedStringData stringData in data)
            {
                string str = stringData.GetString(key);
                if (str != "")
                    return str;
            }
            return ""; //not found in any of our data
        }

        public void Save()
        {
            foreach (LocalizedStringData stringData in data)
            {
                stringData.Save();
            }
        }
    }
}
