using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.Library.Util;
using System.IO;
using System.Linq;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Logging;
using MediaBrowser.LibraryManagement;

namespace MediaBrowser.Library.Filesystem {
    public class VirtualFolderMediaLocation : FolderMediaLocation {

        VirtualFolderContents virtualFolder;

        public VirtualFolderContents VirtualFolder { get { return virtualFolder;  } }

        public VirtualFolderMediaLocation(FileInfo info, IFolderMediaLocation parent)
            : base(info, parent) 
        {
            virtualFolder = new VirtualFolderContents(Contents);
        }

        protected override void SetName() {
            Name = System.IO.Path.GetFileNameWithoutExtension(Path);
        }

        protected override IList<IMediaLocation> GetChildren() {
            var children = new List<IMediaLocation>();
            bool networkChecked = false;
            foreach (var folder in virtualFolder.Folders) {

                if (!networkChecked && folder.StartsWith("\\\\"))
                {
                    //network location - test to be sure it is accessible
                    if (!Helper.WaitForLocation(folder, Kernel.Instance.ConfigData.NetworkAvailableTimeOut))
                    {
                        throw new Exception("Network location unavailable attempting to find " + folder + ". ABORTING to avoid cache corruption.");
                    }
                    networkChecked = true;
                }
                try
                {
                    var location = new FolderMediaLocation(new DirectoryInfo(folder).ToFileInfo(), null, this);
                    foreach (var child in location.Children) {
                        children.Add(child);
                    }
                } 
                catch (Exception ex) {
                    Logger.ReportException("Invalid folder ("+folder+") in Virtual Folder.  May just be unavailable...", ex);
                    throw new Exception("Aborting validation due to IO error to avoid possible cache corruption");
                }
            }
            return children;
        }


    }
}
