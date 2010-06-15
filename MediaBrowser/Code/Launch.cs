using System.Collections.Generic;
using Microsoft.MediaCenter.Hosting;
using Microsoft.MediaCenter;
using System.Diagnostics;
using System.IO;
using System;
using System.Threading;
using MediaBrowser.LibraryManagement;
using System.Xml;
using System.Reflection;
using Microsoft.MediaCenter.UI;
using System.Text;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library;
using MediaBrowser.Library.Util;

namespace MediaBrowser
{
    public class MyAddIn : IAddInModule, IAddInEntryPoint
    {

        public void Initialize(Dictionary<string, object> appInfo, Dictionary<string, object> entryPointInfo)
        {
        }

        public void Uninitialize()
        {
        }

        public void Launch(AddInHost host)
        {
            //  uncomment to debug
#if DEBUG
            host.MediaCenterEnvironment.Dialog("Attach debugger and hit ok", "debug", DialogButtons.Ok, 100, true); 
#endif

            var config = GetConfig();
            if (config == null)
            {
                Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();
                return;
            }

            Environment.CurrentDirectory = ApplicationPaths.AppConfigPath;
            try
            {
                CustomResourceManager.SetupStylesMcml(host);
                CustomResourceManager.SetupFontsMcml(host);
            }
            catch (Exception ex)
            {
                host.MediaCenterEnvironment.Dialog(ex.Message, Application.CurrentInstance.StringData("CustomErrorDial"), DialogButtons.Ok, 100, true);
                Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();
                return;
            }
            using (new MediaBrowser.Util.Profiler("Total Kernel Init"))
            {
                Kernel.Init(config);
            }
            using (new MediaBrowser.Util.Profiler("Application Init"))
            {
                Application app = new Application(new MyHistoryOrientedPageSession(), host);

                app.GoToMenu();
            }
        }

        private static ConfigData GetConfig()
        {
            ConfigData config = null;
            try
            {
                config = ConfigData.FromFile(ApplicationPaths.ConfigFile);
            }
            catch (Exception ex)
            {
                MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
                DialogResult r = ev.Dialog(ex.Message + "\n" + Application.CurrentInstance.StringData("ConfigErrorDial"), Application.CurrentInstance.StringData("ConfigErrorCapDial"), DialogButtons.Yes | DialogButtons.No, 600, true);
                if (r == DialogResult.Yes)
                {
                    config = new ConfigData(ApplicationPaths.ConfigFile);
                    config.Save();
                }
                else
                {
                    Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();

                }
            }

            return config;
        }



    }
}