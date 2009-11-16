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

namespace Crystal {
    class Plugin : BasePlugin
    {

        static readonly Guid CrystalGuid = new Guid("{CDEC944E-60BB-4945-B06D-7C7D8969130E}");

        public override void Init(Kernel kernel)
        {
            try
            {
                kernel.AddTheme("Crystal", "resx://Crystal/Crystal.Resources/PageCrystal#PageCrystal", "resx://Crystal/Crystal.Resources/CrystalMovieView#CrystalMovieView");
                //test out config panel extension
                kernel.AddConfigPanel("Crystal Options", "resx://Crystal/Crystal.Resources/ConfigPanel#ConfigPanel");
                kernel.StringData.AddStringData(CrystalStrings.FromFile(CrystalStrings.GetFileName("Crystal-")));
                Logger.ReportInfo("Crystal Theme Loaded.");
            }
            catch (Exception ex)
            {
                Logger.ReportException("Error adding theme - probably incompatable MB version", ex);
            }

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
                return new System.Version(0, 1, 0, 2);
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
