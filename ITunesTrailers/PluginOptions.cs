using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Attributes;
using System.Windows.Controls;

namespace ITunesTrailers
{
    public class PluginOptions : PluginConfigurationOptions
    {
        public PluginOptions()
        {
        }

        [LabelAttribute("Menu Name:")]
        [ControlAttribute(typeof(TextBox))]
        [DefaultAttribute("Trailers")]
        public string MenuName
        { get; set; }
    }
}
