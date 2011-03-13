using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Configuration;

namespace MediaInfoProvider {
    public class Plugin : BasePlugin {

        internal const string PluginName = "MediaInfo Provider";
        internal const string PluginDescription = "This plugin uses the MediaInfo project to provide rich information about your media, such as codecs, bitrates, resolution, etc..\n\nISO's and WTV files are currently not supported.\n\nThis version includes MediaInfo.dll version " + Plugin.includedMediaInfoDLL + ".";
        internal const string includedMediaInfoDLL = "0.7.42.0";
        public static PluginConfiguration<PluginOptions> PluginOptions { get; set; }
        public static int ServiceTimeout = 12000;

        public override void Init(Kernel kernel) {
            PluginOptions = new PluginConfiguration<PluginOptions>(kernel, this.GetType().Assembly);
            PluginOptions.Load();
            if (PluginOptions.Instance.ClearBadFiles)
            {
                foreach (var badFile in PluginOptions.Instance.BadFiles)
                {
                    if (!PluginOptions.Instance.FormerBadFiles.Contains(badFile)) PluginOptions.Instance.FormerBadFiles.Add(badFile);
                }
                PluginOptions.Instance.BadFiles.Clear();
                PluginOptions.Instance.ClearBadFiles = false;
                PluginOptions.Save();
            }

            int.TryParse(PluginOptions.Instance.ServiceTimeout, out ServiceTimeout);
            kernel.MetadataProviderFactories.Add(MetadataProviderFactory.Get<MediaInfoProvider>()); 
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
                return new System.Version(2, 3, 1, 0);
            }
        }
        public override System.Version TestedMBVersion
        {
            get
            {
                return new System.Version(2, 3, 0, 0);
            }
        }
    }
}
