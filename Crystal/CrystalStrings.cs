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
using MediaBrowser.Library.Localization;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Persistance;

namespace Crystal
{
    [Serializable]
    public class CrystalStrings : LocalizedStringData
    {
        const string VERSION = "1.0000"; //this is used to see if we have changed and need to re-save

        //these are our strings keyed by property name
        public string CrystalOptionsDesc = "Options for the Crystal Theme.";
        public string CrystalTestOptionDesc = "This option is for the Crystal Theme.";

        public CrystalStrings() //for the serializer
        {
        }

        public static CrystalStrings FromFile(string file)
        {
            CrystalStrings s = new CrystalStrings() ;
            XmlSettings<CrystalStrings> settings = XmlSettings<CrystalStrings>.Bind(s, file);
           
            Logger.ReportInfo("Using String Data from " + file);
           
            if (VERSION != s.Version)
            {
                File.Delete(file);
                s = new CrystalStrings();
                settings = XmlSettings<CrystalStrings>.Bind(s, file);
            }
            return s;
        }
    }
}
