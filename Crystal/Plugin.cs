using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.MediaCenter.UI;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Logging;
using MediaBrowser;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library;
using MediaBrowser.Library.Localization;

namespace Crystal {
    class Plugin : BasePlugin
    {

        static readonly Guid CrystalGuid = new Guid("{CDEC944E-60BB-4945-B06D-7C7D8969130E}");

        private MenuItem tickMenuItem;
        private Timer aTimer;

        public override void Init(Kernel kernel)
        {
            try
            {
                kernel.AddTheme("Crystal", "resx://Crystal/Crystal.Resources/PageCrystal#PageCrystal", "resx://Crystal/Crystal.Resources/CrystalMovieView#CrystalMovieView");
                //test out config panel extension
                kernel.AddConfigPanel("Crystal Options", "resx://Crystal/Crystal.Resources/ConfigPanel#ConfigPanel");
                kernel.StringData.AddStringData(CrystalStrings.FromFile(LocalizedStringData.GetFileName("Crystal-")));

                //test out context menu
                try
                {
                    //kernel.AddMenuItem(new MenuItem("Pin to EHS", "resx://MediaBrowser/MediaBrowser.Resources/PivotArrowRight", TestCommand2));
                    //tickMenuItem = kernel.AddMenuItem(new MenuItem(TickOptionText,"resx://MediaBrowser/MediaBrowser.Resources/Tick",TestCommand));
                    //kernel.AddMenuItem(new MenuItem("Add to Favorites", "resx://MediaBrowser/MediaBrowser.Resources/Star_Full", TestCommand));
                    //kernel.AddMenuItem(new MenuItem("Queue All", "resx://MediaBrowser/MediaBrowser.Resources/Star_Full", TestCommand, new List<Type>(){typeof(Folder), typeof(Series), typeof(Season)}));
                }
                catch (Exception ex)
                {
                    Logger.ReportException("Error adding menus in Crystal Plugin", ex);
                }

                //watch the theme selection so our options are only available if we are the current theme
                //Config.Instance.PropertyChanged += new PropertyChangedEventHandler(config_PropertyChanged);
                //if (Config.Instance.ViewTheme != "Crystal") 
                //    tickMenuItem.Available = false;

                Logger.ReportInfo("Crystal Theme Loaded.");
            }
            catch (Exception ex)
            {
                Logger.ReportException("Error adding theme - probably incompatable MB version", ex);
            }

        }

        void config_PropertyChanged(IPropertyObject sender, string property)
        {
            if (property == "ViewTheme")
            {
                if (Config.Instance.ViewTheme == "Crystal")
                    tickMenuItem.Available = true;
                else
                    tickMenuItem.Available = false;
            }
        }

        public void TestCommand(Item item)
        {
            tickMenuItem.Enabled = !tickMenuItem.Enabled;
            if (aTimer == null) initTimer(null);
            aTimer.Enabled = true; //start our pretend operation and re-set the button;
            MediaBrowser.Application.CurrentInstance.Information.AddInformationString("Ticked " + item.Name); //and display a message

        }

        public void TestCommand2(Item item)
        {
            MediaBrowser.Application.CurrentInstance.Information.AddInformationString("Pinned " + item.Name); //and display a message

        }

        private void initTimer(object args)
        {
            aTimer = new Timer();
            aTimer.Enabled = false; //don't need this until we unlock
            aTimer.Interval = 30000; //30secs
            aTimer.Tick += new EventHandler(aTimer_Tick);
        }

        void aTimer_Tick(object sender, EventArgs e)
        {
            tickMenuItem.Enabled = true;
            aTimer.Enabled = false;
        }

        public string TickOptionText(Item item)
        {
            if (tickMenuItem.Enabled)
                if (item.HaveWatched)
                    return "Mark Unwatched";
                else return "Mark Watched";
            else
                return "Unavailable";
        }

        public override string Name
        {
            get { return "Crystal Theme"; }
        }

        public override string Description
        {
            get { return "A new Theme for MediaBrowser"; }
        }

        public override bool InstallGlobally
        {
            get
            {
                return true; //we need to be installed in a globally-accessible area (GAC, ehome)
            }
        }

        public override System.Version LatestVersion
        {
            get
            {
                return new System.Version(0, 1, 0, 3);
            }
            set
            {
            }
        }

        public override System.Version Version
        {
            get
            {
                return LatestVersion;
            }
        }
    }


}
