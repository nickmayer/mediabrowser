using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins.Configuration;
using MediaBrowser.Library.Plugins;

namespace FilmTrailerPlugin {
    public class PluginOptions : PluginConfigurationOptions {
        [Label("Menu Name:")]
        public string MenuName = "Film Trailers";
        [Label("Refresh Interval (hours):")]
        public string RefreshIntervalHrs = "24";

        [Label("Trailer Source Country:")]
        [Items("Australia,Denmark,Finland,France,Germany,Italy,Spain,Sweden,Switzerland,Switzerland (fr),The Netherlands,United Kingdom,** CUSTOM **")]
        public string TrailerSource = "United Kingdom";

        [Label("Custom (Advanced Only):")]
        public string TrailerSourceCustom = FilmTrailerFolder.FeedDefault;
    }
}
