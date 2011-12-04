using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library;
using System.IO;
using System.Diagnostics;
using MediaBrowser.Library.Configuration;
using System.Reflection;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using ICSharpCode.SharpZipLib.Zip;
using MediaBrowser.Library.Extensions;

namespace MtnFrameGrabProvider {
    public class Plugin : BasePlugin {

        internal const string PluginName = "High Quality Thumbnails";
        internal const string PluginDescription = "High quality automatic thumbnails powered by the mtn project. http://moviethumbnail.sourceforge.net\n\nWTV files are not supported.";
        internal const string includedMtnVersion = "2010.11.10";
        public static PluginConfiguration<PluginOptions> PluginOptions { get; set; }

        static Plugin() {
            try { 
                MtnPath = Path.Combine(ApplicationPaths.AppPluginPath, "mtn");
            } catch (Exception) {
                // mono workaround 
                MtnPath = "temp";
            }
            MtnExe = Path.Combine(MtnPath, "mtn.exe");           
        }

        public static readonly string MtnPath;
        public static readonly string MtnExe;

        public override void Init(Kernel kernel) {
            PluginOptions = new PluginConfiguration<PluginOptions>(kernel, this.GetType().Assembly);
            PluginOptions.Load();

            EnsureMtnIsExtracted();

            kernel.MetadataProviderFactories.Add(new MetadataProviderFactory(typeof(FrameGrabProvider)));

            kernel.ImageResolvers.Add((path,canBeProcessed,item) =>
            {
                if (path.ToLower().StartsWith("mtn")) {
                    return new GrabImage();
                }
                return null;
            });
            Logger.ReportInfo(Name + " (version " + Version + ") Loaded.");
        }

        public override bool IsConfigurable
        {
            get
            {
                return true;
            }
        }

        public override IPluginConfiguration PluginConfiguration
        {
            get
            {
                return PluginOptions;
            }
        }

        public override string Name
        {
            get { return PluginName; }
        }

        public override string Description {
            get { return PluginDescription; }
        }

        public override System.Version RequiredMBVersion
        {
            get
            {
                return new System.Version(2, 3, 1, 0);
            }
        }
        public override System.Version TestedMBVersion
        {
            get
            {
                return new System.Version(2, 5, 1, 0);
            }
        }
        
        public static void EnsureMtnIsExtracted()
        {
            string name = "MtnFrameGrabProvider.mtn.zip";
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name); 
            bool needsToExtractMtn = false;
           
            /*
            if (!Directory.Exists(MtnPath)) {
                Directory.CreateDirectory(MtnPath);
                //Directory.CreateDirectory(FrameGrabsPath);
                
                using (var zip = new ZipInputStream(stream)) {

                    ZipEntry entry; 
                    while ((entry = zip.GetNextEntry()) != null) {
                        string destination = Path.Combine(MtnPath, entry.Name);
                        File.WriteAllBytes(destination, zip.ReadAllBytes());
                    }
                }
            } else {
                FileVersionInfo mtnVersion = FileVersionInfo.GetVersionInfo(MtnExe);
                Logger.ReportInfo("Using mtn version:{0}", mtnVersion.FileVersion);
                if (mtnVersion.FileVersion == null)
                    Logger.ReportInfo("Using a pre-versioning copy of mtn.");
            }
            */

            if (!Directory.Exists(MtnPath)) 
            {
                Directory.CreateDirectory(MtnPath);
                needsToExtractMtn = true;
            } 
            else 
            {
                FileVersionInfo mtnVersion = FileVersionInfo.GetVersionInfo(MtnExe);
                Logger.ReportInfo("Using mtn version:{0}", mtnVersion.FileVersion);

                if (mtnVersion.FileVersion != null) 
                {
                    if (mtnVersion.FileVersion.ToString() != includedMtnVersion) 
                    {
                        Logger.ReportInfo("mtn too old!");
                        needsToExtractMtn = true;
                    }
                } 
                else 
                {
                    Logger.ReportInfo("Using pre-versioning copy of mtn!");
                    needsToExtractMtn = true;
                }

            }

            if (needsToExtractMtn) 
            {
                using (var zip = new ZipInputStream(stream))
                {
                    ZipEntry entry;
                    while ((entry = zip.GetNextEntry()) != null)
                    {
                        string destination = Path.Combine(MtnPath, entry.Name);
                        File.WriteAllBytes(destination, zip.ReadAllBytes());
                    }
                }
            }
        }
        


    }
}
