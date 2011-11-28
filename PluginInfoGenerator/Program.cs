/*
 * This program is used to generate a plugin info xml file 
 *  from a directory containing plugins
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MediaBrowser.Library.Plugins;
using System.Xml;
using System.Reflection;

namespace PluginInfoGenerator {
    class Program {

        const string PLUGIN_INFO = "plugin_info.xml";

        static Assembly mediaBrowserAssembly;

        static System.Reflection.Assembly OnAssemblyResolve(object sender, ResolveEventArgs args) {
            if (args.Name.StartsWith("MediaBrowser,")) {
                return mediaBrowserAssembly;
            }
            return null;
        }


        static void Main(string[] args) {
            if (args.Length != 1 || !Directory.Exists(args[0])) {
                Usage();
                return;
            }
            string dir = args[0];

            mediaBrowserAssembly = typeof(IPlugin).Assembly;
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);

            XmlTextWriter writer = new XmlTextWriter(System.IO.Path.Combine(dir,PLUGIN_INFO) ,Encoding.ASCII);
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 3;
            writer.WriteStartElement("Plugins");

            foreach (var file in Directory.GetFiles(dir,"*.dll")) {
                try {
                    var plugin = Plugin.FromFile(file, false);

                    writer.WriteStartElement("Plugin");
                    writer.WriteElementString("Version", plugin.Version.ToString());
                    writer.WriteElementString("Name", plugin.Name);
                    writer.WriteElementString("Description", plugin.Description);
                    writer.WriteElementString("SourceFilename", Path.GetFileName(file));
                    writer.WriteElementString("Filename", stripID(Path.GetFileName(file)));
                    if (!string.IsNullOrEmpty(plugin.RichDescURL))
                    {
                        writer.WriteElementString("RichDescURL", plugin.RichDescURL);
                    }
                    writer.WriteElementString("RequiredMBVersion", plugin.RequiredMBVersion.ToString());
                    writer.WriteElementString("TestedMBVersion", plugin.TestedMBVersion.ToString());
                    if (plugin.InstallGlobally) {
                        writer.WriteElementString("InstallGlobally", plugin.InstallGlobally.ToString().ToLower());
                    }
                    writer.WriteElementString("PluginClass", plugin.PluginClass);
                    writer.WriteElementString("UpgradeInfo", plugin.UpgradeInfo);
                    writer.WriteElementString("IsPremium", plugin.IsPremium.ToString().ToLower());
                    writer.WriteEndElement();

                } catch (Exception e) {
                    Console.WriteLine("Failed to get info for {0} : {1}", file, e);
                }
            }

            writer.WriteEndElement();
            writer.Close();

            Console.WriteLine("Wrote data to " + PLUGIN_INFO);

        }

        private static string stripID(string filename)
        {
            //this will provide backward compatability with old configurator
            int firstDotPos = filename.IndexOf('.');
            int id = -1;
            Int32.TryParse(filename.Substring(0,firstDotPos), out id);
            if (id > 0)
                return filename.Substring(firstDotPos+1); //we have an id, strip it
            else
                return filename; //no id (old format)
        }

        private static void Usage() {
            Console.WriteLine("This program will generate a plugin info file from a directory containing plugins");
            Console.WriteLine("Usage: PluginInfoGenerator <Path>"); 
        }
    }
}
