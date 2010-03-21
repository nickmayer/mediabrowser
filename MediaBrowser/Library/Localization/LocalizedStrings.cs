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
        private List<object> data = new List<object>();


        public LocalizedStrings()
        {
            //start with our main string data - others can be added at a later time
            this.data.Add(BaseStrings.FromFile(LocalizedStringData.GetFileName()));
        }

        public void AddStringData(object stringData)
        {
            this.data.Add(stringData);
        }

        public string GetString(string key)
        {
            string str = ""; 

            //search our data elements until we find it
            foreach (object stringData in data)
            {
                var field = stringData.GetType().GetField(key);
                if (field != null) {
                    str = (field.GetValue(stringData) as string) ?? "";
                    if (str != "") break;
                } 
            }
            return str; 
        }

        public string GetKey(string str)
        {
            string key = "";

            //search our data elements until we find it
            foreach (object stringData in data)
            {
                foreach (var field in stringData.GetType().GetFields())
                {
                    if (field != null)
                    {
                        key = (field.GetValue(stringData) as string) ?? "";
                        if (str == key) return field.Name;
                    }
                }
            }
            return "";
        }

    }
}
