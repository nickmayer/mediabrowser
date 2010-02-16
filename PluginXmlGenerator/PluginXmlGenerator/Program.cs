using System;
using System.Text;
using System.IO;
using System.Reflection;
using MediaBrowser.Library.Plugins.Attributes;
using System.Collections.Generic;
using System.Xml;

namespace PluginXmlGenerator {

    class PluginInfo {
        public Version Version { get; private set; }
        public Version TestedMBVersion { get; private set; }
        public Version RequiredMBVersion { get; private set; }
        public bool InstallGlobally { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }


        public PluginInfo(string filename) {
            var assembly = Assembly.LoadFrom(filename);

            this.TestedMBVersion = GetProperty(assembly, "TestedVersionAttribute", "Version") as Version;
            this.RequiredMBVersion = GetProperty(assembly, "RequiredVersionAttribute", "Version") as Version;

            this.Version = assembly.GetName().Version;
            this.Name = GetProperty(assembly, "AssemblyTitleAttribute", "Title") as string;
            this.Description = GetProperty(assembly, "AssemblyDescriptionAttribute", "Description") as string;

            var installGloballyArray = assembly.GetCustomAttributes(typeof(InstallGloballyAttribute), false);
            InstallGlobally = (installGloballyArray != null && installGloballyArray.Length == 1);

            
            if (Name == null) {
                throw new ApplicationException("AssemblyTitle must be set!");
            }
            if (Name == null) {
                throw new ApplicationException("AssemblyDescription must be set!");
            }
        }

        private object GetProperty(Assembly assembly, string attributeName, string property) {
            foreach (object oAttribute in assembly.GetCustomAttributes(false)) {
                var attribute = oAttribute as Attribute;
                if (attribute != null && attribute.GetType().Name == attributeName) {
                    return attribute.GetType().GetProperty(property).GetGetMethod().Invoke(attribute, null);
                }
            }
            return null;
        }
    }

    class Program {
        static void Main(string[] args) {
            if (args.Length != 2) {
                Console.WriteLine("Usage PluginXmlGenerator path plugin_xml_file.xml" );
                return;
            }

            Assembly.Load("MediaBrowser");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<Plugins/>");
            var pluginsNode = doc.FirstChild;

            var plugins = new Dictionary<string, PluginInfo>();

            foreach (var file in Directory.GetFiles(args[0], "*.dll")) {
                try {
                    var plugin = new PluginInfo(file);
                    if (plugins.ContainsKey(plugin.Name)) {
                        if (plugins[plugin.Name].Version < plugin.Version) {
                            plugins[plugin.Name] = plugin;
                        }
                    } else {
                        plugins[plugin.Name] = plugin;
                    }
                } catch (Exception e) {

                    Console.WriteLine("Failed to generate plugin info for: " + file + " " + e.ToString() + e.StackTrace);
                }
            }

            foreach (var plugin in plugins.Values)
	        {
    		    var node = doc.CreateElement("Plugin");
                AppendElementByValue(node, "Version", plugin.Version);
                AppendElementByValue(node, "Name", plugin.Name);
                AppendElementByValue(node, "Description", plugin.Description);
                AppendElementByValue(node, "TestedMBVersion", plugin.TestedMBVersion);
                AppendElementByValue(node, "RequiredMBVersion", plugin.RequiredMBVersion);
                if (plugin.InstallGlobally) {
                    AppendElementByValue(node, "InstallGlobally", true);
                }
                pluginsNode.AppendChild(node);
	        }

            Console.WriteLine(doc.OuterXml);

            File.WriteAllText(args[1], doc.OuterXml);

        }

        private static void AppendElementByValue(XmlElement node, string name, object value) {
            if (value != null) {
                var child = node.OwnerDocument.CreateElement(name);
                child.InnerText = value.ToString();
                node.AppendChild(child);
            }
        }
    }
}
