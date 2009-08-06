using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library;
using MediaBrowser.Library.Entities;
using MusicPlugin.LibraryManagement;
using MusicPlugin.Library.Entities;
using MediaBrowser.Library.Logging;
using MediaBrowser;
using MediaBrowser.Library.Extensions;

namespace MusicPlugin.Code.ModelItems
{
    public class MusicFolderModel : FolderModel
    {
        public static bool IsOne(BaseItem baseItem)
        {
            if (baseItem is Folder)
                return ((baseItem as Folder).Children.Select(x => x as Song).Where(i => i != null).Count()) > 0;

            return false;
        }

        protected override void LoadDisplayPreferences()
        {
            Logger.ReportInfo("Loading display prefs for " + this.Path);

            Guid id = Id;

            if (Config.Instance.EnableSyncViews)           
                id = baseItem.GetType().FullName.GetMD5();

            DisplayPreferences dp = Kernel.Instance.ItemRepository.RetrieveDisplayPreferences(id);
            if (dp == null) {
                LoadDefaultDisplayPreferences(ref id, ref dp);
            }
            this.DisplayPrefs = dp;
        }
    }
}
