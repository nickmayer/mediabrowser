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

namespace Crystal
{
    [Serializable]
    public class CrystalStrings : LocalizedStringData
    {
        private string version = "1.0000"; //this is used to see if we have changed and need to re-save

        //these are our strings keyed by property name
        public string CrystalOptionsDesc = "Options for the Crystal Theme.";
        public string CrystalTestOptionDesc = "This option is for the Crystal Theme.";

        CrystalStrings(string file)
        {
            this.FileName = file;
        }

        CrystalStrings() //for the serializer
        {
        }

        public static CrystalStrings FromFile(string file)
        {
            CrystalStrings s;

            if (!File.Exists(file))
            {
                s = new CrystalStrings(file);
                s.Save();
            }
            Logger.ReportInfo("Using String Data from " + file);
            XmlSerializer xs = new XmlSerializer(typeof(CrystalStrings));
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                try
                {
                    s = (CrystalStrings)xs.Deserialize(fs);
                }
                catch
                {
                    //file is mucked up - just re-create it
                    s = new CrystalStrings(file);
                }
            }
            if (s.Version != s.version)
            {
                //new version - save over old file
                s = new CrystalStrings(file);
                s.Version = s.version;
                s.Save();
            }
            return s;
        }
    }
}
