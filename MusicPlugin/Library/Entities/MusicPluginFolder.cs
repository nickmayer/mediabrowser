using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MusicPlugin.Util;
using MusicPlugin.LibraryManagement;

namespace MusicPlugin.Library.Entities
{
    public class MusicPluginFolder: Folder
    {
        protected override List<BaseItem> ActualChildren
        {
            get
            {
                List<BaseItem> items =  base.ActualChildren;
                if (Settings.ShowPlaylistAsFolder)
                {                    
                    if ((items != null) && !items.Contains(MusicHelper.GetPlaylistFolder()))
                        items.Add(MusicHelper.GetPlaylistFolder());
                }

                return items;                
            }
        }
    }
}
