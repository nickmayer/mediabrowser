using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MusicPlugin.Util;
using MusicPlugin.LibraryManagement;
using MediaBrowser.Library.Logging;

namespace MusicPlugin.Library.Entities
{
    public class MusicPluginFolder: Folder
    {
        protected override List<BaseItem> ActualChildren
        {
            get
            {
                List<BaseItem> items = base.ActualChildren;
                if (Settings.Instance.ShowPlaylistAsFolder)
                {
                    if (items != null) 
                    {
                        Folder folder = MusicHelper.GetPlaylistFolder();
                        if (folder != null)
                        {
                            if (!items.Contains(folder))
                                items.Add(folder);
                        }
                    }
                }

                return items;
            }
        }

        string _name;
        public override string Name
        { 
            get
            { 
                if (Settings.Instance.LoadNormalLibrary)
                    return Settings.Instance.NormalLibraryVirtualFolderName;
                return _name;
            }
            set 
            {
                base.Name = value;
                _name = value;
            }
        }
    }
}
