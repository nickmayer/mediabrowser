using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Configurator.Properties;
using MediaBrowser.Library.Plugins;
using System.Xml;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Extensions;
using System.IO;
using MediaBrowser.Library.Logging;

namespace Configurator.Code {
    class PluginSourceCollection : ObservableCollection<string> {

       

        public static PluginSourceCollection Instance = new PluginSourceCollection();

        private PluginSourceCollection() {
            foreach (var item in Settings.Default.Repositories) {
                Items.Add(item);
            }
        }

        protected override void InsertItem(int index, string item) {
            base.InsertItem(index, item);
            Settings.Default.Repositories.Add(item);
            Settings.Default.Save();
        }

        protected override void RemoveItem(int index) {
            Settings.Default.Repositories.Remove(this[index]);
            Settings.Default.Save();
            base.RemoveItem(index);
        }

        public IEnumerable<IPlugin> AvailablePlugins {
            get {
                List<IPlugin> plugins = new List<IPlugin>();
                foreach (var source in this) {
                    plugins.AddRange(DiscoverPlugins(source));
                }
                return plugins;
            }
        }

        private List<IPlugin> DiscoverPlugins(string source) {
            if (source.ToLower().StartsWith("http")) {
                return DiscoverRemotePlugins(source);
            } else {
                return DiscoverLocalPlugins(source);
            }
        }

        private List<IPlugin> DiscoverLocalPlugins(string source) {
            var list = new List<IPlugin>();
            foreach (var file in Directory.GetFiles(source)) {
                if (file.ToLower().EndsWith(".dll")) {
                    list.Add(Plugin.FromFile(file, true)); 
                }
            }
            return list;
        }

        private List<IPlugin> DiscoverRemotePlugins(string source) {
            var list = new List<IPlugin>();
            XmlDocument doc = Helper.Fetch(source);
            if (doc != null) {
                foreach (XmlNode pluginRoot in doc.SelectNodes(@"Plugins//Plugin")) {
                    string installGlobally = pluginRoot.SafeGetString("InstallGlobally") ?? "false"; //get this safely in case its not there
                    list.Add(new RemotePlugin()
                    {
                        Description = pluginRoot.SafeGetString("Description"),
                        Filename = pluginRoot.SafeGetString("Filename"),
                        Version = new System.Version(pluginRoot.SafeGetString("Version")),
                        Name = pluginRoot.SafeGetString("Name"),
                        BaseUrl = GetPath(source),
                        InstallGlobally = XmlConvert.ToBoolean(installGlobally)
                    });
                }
            } else {

                Logger.ReportWarning("There appears to be no network connection. Plugin can not be installed.");
            }
            return list;
        }

        private string GetPath(string source) {
            var index = source.LastIndexOf("\\");
            if (index<=0) {
                index = source.LastIndexOf("/");
            }
            return source.Substring(0, index);
        }
    }
}
