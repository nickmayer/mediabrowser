using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Library.Plugins {

    /// <summary>
    /// This is the class plugins should inherit off, it implements gathering basic version info
    /// </summary>
    public abstract class BasePlugin : IPlugin {
        #region IPlugin Members

        public abstract void Init(Kernel kernel);

        public abstract string Name {
            get;
        }

        public abstract string Description {
            get;
        }

        public virtual string RichDescURL
        {
            get { return ""; }
        }

        /// <summary>
        /// Filename is assigned by the plugin discovery piece. 
        /// Do not override this.
        /// </summary>
        public string Filename {
            get {
                // if you instansiate a class inheriting off BasePlugin 
                // There is no way to tell what the plugin file name is.
                throw new NotSupportedException(); 
            } 
        } 

        public virtual System.Version Version {
            get {
                return this.GetType().Assembly.GetName().Version;
            }
        }

        /// <summary>
        /// Only override this if you want to control the logic for determining the latest version.
        /// </summary>
        public virtual System.Version LatestVersion {
            get;
            set;
        }

        public virtual System.Version RequiredMBVersion
        {
            get {return new System.Version(2,0,0,0);}
        }

        public virtual System.Version TestedMBVersion
        {
            get { return new System.Version(2, 2, 1, 0); }
        }

        public virtual bool IsConfigurable
        {
            get
            {
                return false;
            }
        }

        public virtual bool InstallGlobally
        {
            get
            {
                return false;
            }
        }

        public virtual bool Installed { get; set; }
        public virtual bool IsLatestVersion { get; set; }
        public virtual bool UpdateAvail { get; set; }

        public string ListDisplayString
        {
            get
            {
                return Name + " (v" + Version + ")";
            }
        }

        public virtual IPluginConfiguration PluginConfiguration {
            get {
                return null;
            }
        }

        public virtual MBLoadContext InitDirective { 
            get {
                //by default, any plug-in that produces an interface should only init in core
                if (InstallGlobally)
                    return MBLoadContext.Core | MBLoadContext.Other;
                else
                    return MBLoadContext.All; 
            } 
        }

        public virtual string PluginClass
        {
            get { return InstallGlobally ? PluginClasses.Themes : PluginClasses.Other; }
        }

        public virtual void Configure()
        {
            if (PluginConfiguration != null) {
                if (PluginConfiguration.BuildUI() == true)
                    PluginConfiguration.Save();
                else
                    PluginConfiguration.Load();
            }
        }


        #endregion

    }
}
