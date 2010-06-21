using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.MediaCenter.UI;
using System.IO;
using MediaBrowser.Library.Configuration;
using Microsoft.MediaCenter;

namespace Diamond
{
    public class Config : ModelItem
    {
        private ConfigData data;
        private bool isValid;

        private readonly string configFilePath = Path.Combine(ApplicationPaths.AppPluginPath, "Configurations\\Diamond.xml");
        private readonly string configFolderPath = Path.Combine(ApplicationPaths.AppPluginPath, "Configurations");

        public Config()
        {
            isValid = Load();
        }

        #region Config Options

        public bool MiniMode
        {
            get { return this.data.MiniMode; }
            set
            {
                if (this.data.MiniMode != value)
                {
                    this.data.MiniMode = value;
                    Save();
                    FirePropertyChanged("MiniMode");
                }
            }
        }
        public bool DisplayEndTime
        {
            get { return this.data.DisplayEndTime; }
            set
            {
                if (this.data.DisplayEndTime != value)
                {
                    this.data.DisplayEndTime = value;
                    Save();
                    FirePropertyChanged("DisplayEndTime");
                }
            }
        }
        public bool InfoBoxCoverflow
        {
            get { return this.data.InfoBoxCoverflow; }
            set
            {
                if (this.data.InfoBoxCoverflow != value)
                {
                    this.data.InfoBoxCoverflow = value;
                    Save();
                    FirePropertyChanged("InfoBoxCoverflow");
                }
            }
        }

        public bool InfoBoxThumbstrip
        {
            get { return this.data.InfoBoxThumbstrip; }
            set
            {
                if (this.data.InfoBoxThumbstrip != value)
                {
                    this.data.InfoBoxThumbstrip = value;
                    Save();
                    FirePropertyChanged("InfoBoxThumbstrip");
                }
            }
        }

        public bool InfoBoxPoster
        {
            get { return this.data.InfoBoxPoster; }
            set
            {
                if (this.data.InfoBoxPoster != value)
                {
                    this.data.InfoBoxPoster = value;
                    Save();
                    FirePropertyChanged("InfoBoxPoster");
                }
            }
        }

        public bool DisplayGlassOverlay
        {
            get { return this.data.DisplayGlassOverlay; }
            set
            {
                if (this.data.DisplayGlassOverlay != value)
                {
                    this.data.DisplayGlassOverlay = value;
                    Save();
                    FirePropertyChanged("DisplayGlassOverlay");
                }
            }
        }

        public float BackdropTransitionPeriod
        {
            get { return this.data.BackdropTransitionPeriod; }
            set
            {
                if (this.data.BackdropTransitionPeriod != value)
                {
                    this.data.BackdropTransitionPeriod = value;
                    Save();
                    FirePropertyChanged("BackdropTransitionPeriod");
                }
            }
        }
        public bool AutoExtenderLayout
        {
            get { return this.data.AutoExtenderLayout; }
            set
            {
                if (this.data.AutoExtenderLayout != value)
                {
                    this.data.AutoExtenderLayout = value;
                    Save();
                    FirePropertyChanged("AutoExtenderLayout");
                }
            }
        }

        public int BackdropSwitchingDelay
        {
            get { return this.data.BackdropSwitchingDelay; }
            set
            {
                if (this.data.BackdropSwitchingDelay != value)
                {
                    this.data.BackdropSwitchingDelay = value;
                    Save();
                    FirePropertyChanged("BackdropSwitchingDelay");
                }
            }
        }

        public bool DiamondDisplayWeather
        {
            get { return this.data.DiamondDisplayWeather; }
            set
            {
                if (this.data.DiamondDisplayWeather != value)
                {
                    this.data.DiamondDisplayWeather = value;
                    Save();
                    FirePropertyChanged("DiamondDisplayWeather");
                }
            }
        }

        public int AlphaOpacity
        {
            get { return this.data.AlphaOpacity; }
            set
            {
                if (this.data.AlphaOpacity != value)
                {
                    this.data.AlphaOpacity = value;
                    Save();
                    FirePropertyChanged("AlphaOpacity");
                }
            }
        }
        #endregion


        #region Save / Load Configuration

        private void Save() 
        { 
            lock (this) this.data.Save(); 
        }

        private bool Load()
        {
            try
            {
                this.data = ConfigData.FromFile(configFilePath);
                return true;
            }
            catch (Exception ex)
            {
                MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment; 
                DialogResult r = ev.Dialog(ex.Message + "\nReset to default?", "Error in configuration file", DialogButtons.Yes | DialogButtons.No, 600, true); 
                if (r == DialogResult.Yes)
                {
                    if (!Directory.Exists(configFolderPath)) 
                        Directory.CreateDirectory(configFolderPath); 
                    this.data = new ConfigData(configFilePath);
                    Save();
                    return true;
                }
                else 
                    return false;
            }
        }

        #endregion
    }
}
