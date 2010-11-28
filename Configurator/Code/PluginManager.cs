using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;
using System.ComponentModel;
using System.Windows;
using System.IO;
using MediaBrowser.Library.Logging;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Threading;
using MediaBrowser.Library.Threading;
using MediaBrowser.Library.Configuration;

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
        PluginCollection backedUpPlugins = new PluginCollection();
        PluginSourceCollection sources = PluginSourceCollection.Instance;
        string backupDir = Path.Combine(ApplicationPaths.AppPluginPath, "Backup");

        Dictionary<string, System.Version> latestVersions = new Dictionary<string, System.Version>();
        Dictionary<string, System.Version> requiredVersions = new Dictionary<string, System.Version>();

        public PluginManager() {
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject())) {
                Async.Queue("Plugin refresher", () =>
                {
                    RefreshInstalledPlugins();
                    RefreshAvailablePlugins();
                    RefreshBackedUpPlugins();
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
            requiredVersions.Clear();

            foreach (var plugin in sources.AvailablePlugins) {
                IPlugin ip = this.InstalledPlugins.Find(plugin);
                if (ip != null)
                {
                    plugin.Installed = true;
                    //we need to set this in the installed plugin here because we didn't have this info the first time we refreshed
                    plugin.UpdateAvail = ip.UpdateAvail = (plugin.Version > ip.Version && Kernel.Instance.Version >= plugin.RequiredMBVersion);
                }
                availablePlugins.Add(plugin);
                try
                {
                    //this could blow if we have two references to the same plugin...
                    latestVersions.Add(plugin.Name + System.IO.Path.GetFileName(plugin.Filename), plugin.Version);
                    requiredVersions.Add(plugin.Name + System.IO.Path.GetFileName(plugin.Filename), plugin.RequiredMBVersion);
                }
                catch (Exception e)
                {
                    Logger.ReportException("Cannot add plugin latest version. Probably two references to same plugin.", e);
                }
            }
        }

        private void RefreshBackedUpPlugins()
        {
            backedUpPlugins.Clear();
            if (Directory.Exists(backupDir))
            {
                foreach (var file in Directory.GetFiles(backupDir))
                {
                    if (file.ToLower().EndsWith(".dll"))
                    {
                        try
                        {
                            backedUpPlugins.Add(Plugin.FromFile(file, true));
                        }
                        catch (Exception e)
                        {
                            Logger.ReportException("Error attempting to load " + file + " as plug-in.", e);
                        }
                    }
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

            BackupPlugin(plugin);

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

        private bool BackupPlugin(IPlugin plugin)
        {
            //Backup current version if installed and different from the one we are installing
            try
            {
                if (plugin.Installed && InstalledPlugins.Find(plugin).Version != plugin.Version)
                {
                    if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);
                    string oldPluginPath = plugin.InstallGlobally ?
                        Path.Combine(System.Environment.GetEnvironmentVariable("windir"), Path.Combine("ehome", plugin.Filename)) :
                        Path.Combine(ApplicationPaths.AppPluginPath, plugin.Filename);
                    string bpPath = Path.Combine(backupDir, plugin.Filename);
                    File.Copy(oldPluginPath,bpPath ,true);
                    IPlugin bp = backedUpPlugins.Find(plugin);
                    if (bp != null) backedUpPlugins.Remove(bp);
                    backedUpPlugins.Add(Plugin.FromFile(bpPath,false));
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.ReportException("Error trying to backup current plugin", e);
            }
            return false;
        }

        public bool RollbackPlugin(IPlugin plugin)
        {
            try
            {
                string source = Path.Combine(backupDir, plugin.Filename);
                if (File.Exists(source))
                {
                    string target = plugin.InstallGlobally ?
                            Path.Combine(System.Environment.GetEnvironmentVariable("windir"), Path.Combine("ehome", plugin.Filename)) :
                            Path.Combine(ApplicationPaths.AppPluginPath, plugin.Filename);
                    Kernel.Instance.InstallPlugin(source, plugin.InstallGlobally, null, null, null);
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.ReportException("Error attempting to rollback plugin " + plugin.Name, e);
            }

            return false;
        }

        public void RefreshInstalledPlugins() {

            if (Application.Current.Dispatcher.Thread != System.Threading.Thread.CurrentThread) {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,(System.Windows.Forms.MethodInvoker)RefreshInstalledPlugins);
                return;
            }

            installedPlugins.Clear();
            foreach (var plugin in Kernel.Instance.Plugins.OrderBy(p => p.Name)) {
                System.Version v = GetLatestVersion(plugin);
                System.Version rv = GetRequiredVersion(plugin) ?? new System.Version(0, 0, 0, 0);
                if (v != null)
                {
                    plugin.UpdateAvail = (v > plugin.Version && rv <= Kernel.Instance.Version);
                    IPlugin ap = availablePlugins.Find(plugin);
                    if (ap != null)
                    {
                        ap.Installed = true;
                        ap.UpdateAvail = plugin.UpdateAvail;
                    }
                }
                plugin.Installed = true;
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

        public bool UpgradesAvailable()
        {
            foreach (IPlugin plugin in installedPlugins)
            {
                if (plugin.UpdateAvail) return true;
            }
            return false;
        }

        public System.Version GetLatestVersion(IPlugin plugin) {
            System.Version version;
            latestVersions.TryGetValue(plugin.Name+plugin.Filename, out version);
            return version;
        }

        public System.Version GetRequiredVersion(IPlugin plugin)
        {
            System.Version version;
            requiredVersions.TryGetValue(plugin.Name + plugin.Filename, out version);
            return version;
        }

        public System.Version GetBackedUpVersion(IPlugin plugin)
        {
            System.Version version = null;
            IPlugin p = backedUpPlugins.Find(plugin);
            if (p != null) version = p.Version;
            return version;
        }

        public PluginCollection InstalledPlugins {
            get {
                return installedPlugins;
            } 
        }

        public PluginCollection AvailablePlugins
        {
            get
            {
                return availablePlugins;
            }
        }
        public PluginCollection BackedUpPlugins
        {
            get
            {
                return backedUpPlugins;
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
