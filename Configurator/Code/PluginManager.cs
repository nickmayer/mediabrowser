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

namespace Configurator.Code {
    public class PluginManager {

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

        Dictionary<IPlugin, System.Version> latestVersions = new Dictionary<IPlugin, System.Version>();

        public PluginManager() {
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject())) {
                // move this to async
                RefreshInstalledPlugins();
                RefreshAvailablePlugins();
            }
        }

        public void RefreshAvailablePlugins() {
            availablePlugins.Clear();
            var source = PluginSourceCollection.Instance;
            foreach (var plugin in source.AvailablePlugins) {
                availablePlugins.Add(plugin);
            } 
        } 

        public void InstallPlugin(IPlugin plugin) {
            if (plugin is RemotePlugin) {
                Kernel.Instance.InstallPlugin((plugin as RemotePlugin).BaseUrl + "\\" + plugin.Filename);
            } else {
                var local = plugin as Plugin;
                Debug.Assert(plugin != null);
                Kernel.Instance.InstallPlugin(local.Filename);
            }

            RefreshInstalledPlugins(); 
        }

        private void RefreshInstalledPlugins() {

            if (Application.Current.Dispatcher.Thread != System.Threading.Thread.CurrentThread) {
                try {
                    Application.Current.Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)RefreshInstalledPlugins, null);
                } catch (Exception) {
                    MessageBox.Show("You are running an old version of .Net. MediaBrowser requires .Net 3.5 SP1 in order to function! Run Windows update and upgrade your .Net framework!");
                    Application.Current.Shutdown();
                }
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
                MessageBox.Show("Failed to delete the plugin, ensure no one has a lock on the plugin file!");
                Logger.ReportException("Failed to delete plugin", e);
            }
        }

        public System.Version GetLatestVersion(IPlugin plugin) {
            System.Version version;
            latestVersions.TryGetValue(plugin, out version);
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

       
    }
}
