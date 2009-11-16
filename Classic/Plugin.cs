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

//******************************************************************************************************************
//  This class is the heart of your plug-in Theme.  It is instantiated by the initial loading logic of MediaBrowser.
//
//  For your project to build you need to be sure the reference to the MediaBrowser assembly is current.  More than
//  likely, it is broken in this template as the template cannot copy binaries, plus it would probably be out of date
//  anyway.  Expand the References tree in the Solution Explorer to the right and make sure you have a good reference
//  to a current version of the mediabrowser.dll.
//******************************************************************************************************************

namespace Classic
{
    class Plugin : BasePlugin
    {

        static readonly Guid ClassicGuid = new Guid("a23ac5df-b135-48c0-81d8-a777536fd102");

        /// <summary>
        /// The Init method is called when the plug-in is loaded by MediaBrowser.  You should perform all your specific initializations
        /// here - including adding your theme to the list of available themes.
        /// </summary>
        /// <param name="kernel"></param>
        public override void Init(Kernel kernel)
        {
            try
            {
                //the AddTheme method will add your theme to the available themes in MediaBrowser.  You need to call it and send it
                //resx references to your mcml pages for the main "Page" selector and the MovieDetailPage for your theme.
                //The template should have generated some default pages and values for you here but you will need to create all the 
                //specific mcml files for the individual views (or, alternatively, you can reference existing views in MB.
                kernel.AddTheme("Classic", "resx://Classic/Classic.Resources/Page#PageClassic", "resx://Classic/Classic.Resources/DetailMovieView#ClassicMovieView");

                //The AddConfigPanel method will allow you to extend the config page of MediaBrowser with your own options panel.
                //You must create this as an mcml UI that fits within the standard config panel area.  It must take Application and 
                //FocusItem as parameters.  The project template should have generated an example ConfigPage.mcml that you can modify
                //or, if you don't wish to extend the config, remove it and the following call to AddConfigPanel
                //kernel.AddConfigPanel("Classic Options", "resx://Classic/Classic.Resources/ConfigPanel#ConfigPanel");

                //The AddStringData method will allow you to extend the localized strings used by MediaBrowser with your own.
                //This is useful for adding descriptive text to go along with your theme options.  If you don't have any theme-
                //specific options or other needs to extend the string data, remove the following call.
                //kernel.StringData.AddStringData(MyStrings.FromFile(MyStrings.GetFileName("Classic-")));

                //Tell the log we loaded.
                Logger.ReportInfo("Classic Theme Loaded.");
            }
            catch (Exception ex)
            {
                Logger.ReportException("Error adding theme - probably incompatable MB version", ex);
            }

        }

        public override string Name
        {
            //provide a short name for your theme - this will display in the configurator list box
            get { return "Classic Theme"; }
        }

        public override string Description
        {
            //provide a longer description of your theme - this will display when the user selects the theme in the plug-in section
            get { return "A new Theme for MediaBrowser"; }
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
                return new System.Version(0, 1, 0, 1);
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
