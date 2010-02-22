﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library;
using System.IO;
using MediaBrowser.Library.Configuration;
using System.Reflection;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using ICSharpCode.SharpZipLib.Zip;
using MediaBrowser.Library.Extensions;

namespace MtnFrameGrabProvider {
    public class Plugin : BasePlugin {

        internal const string PluginName = "High Quality Thumbnails";
        internal const string PluginDescription = "High quality automatic thumbnails powered by the mtn project. http://moviethumbnail.sourceforge.net"; 


        public static readonly string MtnPath = Path.Combine(ApplicationPaths.AppPluginPath, "mtn");
        public static readonly string MtnExe = Path.Combine(MtnPath, "mtn.exe");
        public static readonly string FrameGrabsPath = Path.Combine(MtnPath, "FrameGrabs");

        public override void Init(Kernel kernel) {

            EnsureMtnIsExtracted();

            kernel.MetadataProviderFactories.Add(new MetadataProviderFactory(typeof(FrameGrabProvider)));

            kernel.ImageResolvers.Add((path,canBeProcessed,item) =>
            {
                if (path.ToLower().StartsWith("mtn")) {
                    return new GrabImage(item);
                }
                return null;
            });
            Logger.ReportInfo(Name + " (version " + Version + ") Loaded.");
        }

        public override string Name {
            get { return PluginName; }
        }

        public override string Description {
            get { return PluginDescription; }
        }

        public override System.Version RequiredMBVersion
        {
            get
            {
                return new System.Version(2, 2, 1, 0);
            }
        }
        public override System.Version TestedMBVersion
        {
            get
            {
                return new System.Version(2, 2, 2, 0);
            }
        }
        public static void EnsureMtnIsExtracted()
        {
           
            if (!Directory.Exists(MtnPath)) {
                Directory.CreateDirectory(MtnPath);
                Directory.CreateDirectory(FrameGrabsPath);
                

                string name = "MtnFrameGrabProvider.mtn.zip";
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name); 
                
                using (var zip = new ZipInputStream(stream)) {

                    ZipEntry entry; 
                    while ((entry = zip.GetNextEntry()) != null) {
                        string destination = Path.Combine(MtnPath, entry.Name);
                        File.WriteAllBytes(destination, zip.ReadAllBytes());
                    }
                }
            }
        }


    }
}
