using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;
using System.ComponentModel;
using System.Windows;
using MediaBrowser.Library.Logging;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Threading;
using MediaBrowser.Library.Threading;

namespace Configurator.Code {
    public class PluginManager {

        public bool PluginsLoaded = false;

        static PluginManager instance; 
        public static PluginManager Instance {
            get {
                if (instance == null) {
                    instance = (Application.Current.FindResource("PluginManager") as ObjectDataProvider).Data as PluginManager;
                }
                return instance;
            }
        }

        internal static void Init() {
            var junk = Instance;
        }

        PluginCollection installedPlugins = new PluginCollection();
        PluginCollection availablePlugins = new PluginCollection();
        PluginSourceCollection sources = PluginSourceCollection.Instance;

        Dictionary<string, System.Version> latestVersions = new Dictionary<string, System.Version>();

        public PluginManager() {
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject())) {
                Async.Queue("Plugin refresher", () =>
                {
                    RefreshInstalledPlugins();
                    RefreshAvailablePlugins();
                    PluginsLoaded = true; //safe to go see if we have updates
                });
            }
        }

        public void RefreshAvailablePlugins() {

            if (Application.Current.Dispatcher.Thread != System.Threading.Thread.CurrentThread) {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)RefreshAvailablePlugins);
                return;
            }

            availablePlugins.Clear();
            latestVersions.Clear();

            foreach (var plugin in sources.AvailablePlugins) {
                availablePlugins.Add(plugin);
                try
                {
                    //this could blow if we have two references to the same plugin...
                    latestVersions.Add(plugin.Name + System.IO.Path.GetFileName(plugin.Filename), plugin.Version);
                }
                catch (Exception e)
                {
                    Logger.ReportException("Cannot add plugin latest version. Probably two references to same plugin.", e);
                }
            }
        } 

        public void InstallPlugin(IPlugin plugin,
          MediaBrowser.Library.Network.WebDownload.PluginInstallUpdateCB updateCB,
          MediaBrowser.Library.Network.WebDownload.PluginInstallFinishCB doneCB,
          MediaBrowser.Library.Network.WebDownload.PluginInstallErrorCB errorCB) {
            //taking this check out for now - it's just too cumbersome to have to re-compile all plug-ins to change this -ebr
            //if (plugin.TestedMBVersion < Kernel.Instance.Version) {
            //    var dlgResult = MessageBox.Show("Warning - " + plugin.Name + " has not been tested with your version of MediaBrowser. \n\nInstall anyway?", "Version not Tested", MessageBoxButton.YesNo);
            //    if (dlgResult == MessageBoxResult.No) {
            //        doneCB();
            //        return;
            //    }
            //}

            if (plugin is RemotePlugin) {
                try {
                    Kernel.Instance.InstallPlugin((plugin as RemotePlugin).BaseUrl + "\\" + plugin.Filename, plugin.InstallGlobally, updateCB, doneCB, errorCB);
                }
                catch (Exception ex) {
                    MessageBox.Show("Cannot Install Plugin.  If MediaBrowser is running, please close it and try again.\n" + ex.Message, "Install Error");
                    doneCB();
                }
            }
            else {
                var local = plugin as Plugin;
                Debug.Assert(plugin != null);
                try {
                    Kernel.Instance.InstallPlugin(local.Filename, plugin.InstallGlobally, null, null, null);
                }
                catch (Exception ex) {
                    MessageBox.Show("Cannot Install Plugin.  If MediaBrowser is running, please close it and try again.\n" + ex.Message, "Install Error");
                    doneCB();
                }
            }

        }

        public void RefreshInstalledPlugins() {

            if (Application.Current.Dispatcher.Thread != System.Threading.Thread.CurrentThread) {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,(System.Windows.Forms.MethodInvoker)RefreshInstalledPlugins);
                return;
            }

            installedPlugins.Clear();
            foreach (var plugin in Kernel.Instance.Plugins) {
                installedPlugins.Add(plugin);
            }
        }

        public void RemovePlugin(IPlugin plugin) {
            try {
                Kernel.Instance.DeletePlugin(plugin);
                installedPlugins.Remove(plugin);
            } catch (Exception e) {
                MessageBox.Show("Failed to delete the plugin.  If MediaBrowser is running, Please close it and try again.");
                Logger.ReportException("Failed to delete plugin", e);
            }
        }

        public System.Version GetLatestVersion(IPlugin plugin) {
            System.Version version;
            latestVersions.TryGetValue(plugin.Name+plugin.Filename, out version);
            return version;
        } 


        public PluginCollection InstalledPlugins {
            get {
                return installedPlugins;
            } 
        }

        public PluginCollection AvailablePlugins {
            get {
                return availablePlugins;
            } 
        }
        public PluginSourceCollection Sources
        {
            get
            {
                return sources;
            }
        }
       
    }
}
