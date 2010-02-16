using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Factories;

namespace MediaBrowser.Library.Plugins {
    /// <summary>
    /// This interface can be implemented by plugin to provide rich information about the plugin
    ///  It also provides plugins with a place to place initialization code
    /// </summary>
    public interface IPlugin {
        void Init(Kernel kernel);
        string Filename { get; }
        string Name { get; }
        string Description { get; }
        string RichDescURL { get; }
        System.Version Version { get; }
        System.Version RequiredMBVersion { get; }
        System.Version TestedMBVersion { get; }
        bool IsConfigurable { get; }
        void Configure();
        bool InstallGlobally { get; }
    }
}
