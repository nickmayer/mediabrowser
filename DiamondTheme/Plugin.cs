using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Logging;
using MediaBrowser;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library;
using MediaBrowser.Library.Util;
using System.Threading;

//******************************************************************************************************************
//  This class is the heart of your plug-in Theme.  It is instantiated by the initial loading logic of MediaBrowser.
//
//  For your project to build you need to be sure the reference to the MediaBrowser assembly is current.  More than
//  likely, it is broken in this template as the template cannot copy binaries, plus it would probably be out of date
//  anyway.  Expand the References tree in the Solution Explorer to the right and make sure you have a good reference
//  to a current version of the mediabrowser.dll.
//
//  For your project to load as a MediaBrowser Plug-in you need to build your release .dll and copy it to the ehome
//  directory (under your windows directory). AND ALSO create a .pgn file with the same name as your dll and put it
//  in c:\program data\mediabrowser\plugins.  The Configurator will do this automatically if provided a valid 
//  plugin_info.xml file and location for your .dll
//******************************************************************************************************************

namespace Diamond
{
    public class Plugin : BasePlugin
    {
        static readonly Guid DiamondGuid = new Guid("c5907aa9-c2bf-495c-80fb-4ffa8770b543");
        private Config config;
        private bool isMC = false;

        /// <summary>
        /// The Init method is called when the plug-in is loaded by MediaBrowser.  You should perform all your specific initializations
        /// here - including adding your theme to the list of available themes.
        /// </summary>
        /// <param name="kernel"></param>
        public override void Init(Kernel kernel)
        {
            try
            {
                kernel.AddTheme("Diamond", "resx://Diamond/Diamond.Resources/Page#PageDiamond", "resx://Diamond/Diamond.Resources/DetailMovieView#DiamondMovieView");
                isMC = AppDomain.CurrentDomain.FriendlyName.Contains("ehExtHost");
                if (isMC)
                {
                    config = new Config();
                    kernel.AddConfigPanel("Diamond Options", "resx://Diamond/Diamond.Resources/ConfigPanel#ConfigPanel", config);
                    //Tell the log we loaded.
                    Logger.ReportInfo("Diamond Theme Loaded.");
                    //Thread t = new Thread(new ThreadStart(Display));
                    //t.Start();
                }
                else
                    Logger.ReportInfo("Not creating menus for Diamond.  Appear to not be in MediaCenter.  AppDomain is: " + AppDomain.CurrentDomain.FriendlyName);
                
                kernel.StringData.AddStringData(MyStrings.FromFile(MyStrings.GetFileName("Diamond-")));                
                CustomResourceManager.AppendFonts("Diamond", Resources.DiamondFontsDefault, Resources.DiamondFontsSmall);
                CustomResourceManager.AppendStyles("Diamond", Resources.DiamondStylesDefault, Resources.DiamondStylesDefault);                
            }
            catch (Exception ex)
            {
                Logger.ReportException("Error adding theme - probably incompatable MB version", ex);
            }

        }

        public void Display()
        {
            Thread.Sleep(10000);
            if (isMC && Application.CurrentInstance.Config.ViewTheme.ToLowerInvariant() == "diamond")
                Application.CurrentInstance.Information.AddInformation(new InfomationItem("Diamond Ready v" + Version.ToString()));            
        }

        public override string Name
        {
            //provide a short name for your theme - this will display in the configurator list box
            get { return "Diamond Theme"; }
        }

        public override string Description
        {
            //provide a longer description of your theme - this will display when the user selects the theme in the plug-in section
            get { return "Diamond Theme for Media Browser"; }
        }

        public override bool InstallGlobally
        {
            get
            {
                //this must return true so we can pass references to our resources to MB
                return true; //we need to be installed in a globally-accessible area (GAC, ehome)
            }
        }

        public override System.Version LatestVersion
        {
            get
            {
                return new System.Version(0, 3, 2, 1);
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
        public override System.Version RequiredMBVersion
        {
            get
            {
                return new System.Version(2,2,4,0);
            }
        }

        

        // Return the highest version of MediaBrowser with which this plug-in has been tested
        public override System.Version TestedMBVersion
        {
            get
            {
                return new System.Version(2, 2, 4, 0);
            }
        }

        public override string RichDescURL
        {
            get
            {
                return @"http://www.manghera.com/plugins/diamond/index.htm";
            }
        }

    }


}
