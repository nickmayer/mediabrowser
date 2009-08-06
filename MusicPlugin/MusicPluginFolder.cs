using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using System.Globalization;
using System.Net;
using System.Xml;
using System.IO;
using MediaBrowser;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Entities.Attributes;
using MediaBrowser.Library.EntityDiscovery;
using MusicPlugin.Library.EntityDiscovery;
using MediaBrowser.Library;

namespace MusicPlugin {
    public class MusicPluginFolder : Folder
    {
        public MusicPluginFolder()
            : base()
        {
           
        }

        public override string Name
        {
            get
            {
                return "Music";
            }
            set
            {
                // name should never be set for this item 
            }
        }

        public override bool RefreshMetadata(MediaBrowser.Library.Metadata.MetadataRefreshOptions options)
        {
            return true;
        }

        [Persist]
        [NotSourcedFromProvider]
        List<BaseItem> childern = new List<BaseItem>();

        [Persist]
        [NotSourcedFromProvider]
        DateTime lastUpdated = DateTime.MinValue;

        // The critical override, you need to override this to take control of the children 
        protected override List<BaseItem> ActualChildren
        {
            get
            {
                return base.ActualChildren;
            }
        }

        // validation is overidden so it can do nothing if a period of time has not elapsed
        public override void ValidateChildren()
        {
            //lastUpdated = DateTime.Now;
            //this.trailers = GetTrailers();
            //this.OnChildrenChanged(null);
            // cache the children
            //Kernel.Instance.ItemRepository.SaveItem(this);
        }

        
    }
}
