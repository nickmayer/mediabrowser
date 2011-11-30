using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library;
using MediaBrowser.Library.Plugins.Configuration;

namespace DvrmsMetadataProvider
{
    public class PluginOptions : PluginConfigurationOptions
    {
        [Label("Show Genres")]
        public bool UseGenres = true;

        [Label("Show Star Ratings")]
        public bool UseStarRatings = true;

        [Label("Name = Series+RecDate")]
        public bool AppendRecordedDate = false;

        [Label("or Series+FirstAiredDate")]
        public bool AppendFirstAiredDate = false; 

    }
}
