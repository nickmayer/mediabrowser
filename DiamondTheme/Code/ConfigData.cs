using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.Library.Persistance;

namespace Diamond
{
    [Serializable]
    internal class ConfigData
    {
        #region constructor
        public ConfigData()
        {
        }
        public ConfigData(string file)
        {
            this.file = file;
            this.settings = XmlSettings<ConfigData>.Bind(this, file);
        }
        #endregion

        public bool MiniMode = true;
        public bool DisplayEndTime = false;
        public bool InfoBoxThumbstrip = true;
        public bool InfoBoxCoverflow = true;
        public bool InfoBoxPoster = false;
        public bool DisplayGlassOverlay = true;

        public bool AutoExtenderLayout = false;
        public float BackdropTransitionPeriod = 1.5F;
        public int BackdropSwitchingDelay = 280;

        public bool DiamondDisplayWeather = false;
        public int AlphaOpacity = 60;

        #region Load / Save Data
        public static ConfigData FromFile(string file)
        { 
            return new ConfigData(file); 
        }

        public void Save()
        { 
            this.settings.Write(); 
        }

        [SkipField]
        string file;

        [SkipField]
        XmlSettings<ConfigData> settings;
        #endregion

    }


}

