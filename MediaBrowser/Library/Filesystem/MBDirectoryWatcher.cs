using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.MediaCenter.UI;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Entities;

namespace MediaBrowser.Library.Filesystem
{
    public class MBDirectoryWatcher : IDisposable
    {        
        private List<FileSystemWatcher> fileSystemWatchers = null;
        private Timer initialTimer;
        private Timer secondaryTimer;
        private System.DateTime lastRefresh;
        private string[] watchedFolders;

        private Folder folder;

        public MBDirectoryWatcher(Folder aFolder, bool watchChanges)
        {
            lastRefresh = System.DateTime.Now.AddMilliseconds(-60000); //initialize this
            this.folder = aFolder;
            IFolderMediaLocation location = folder.FolderMediaLocation;
            if (location is VirtualFolderMediaLocation)
            {
                //virtual folder
                this.watchedFolders = ((VirtualFolderMediaLocation)location).VirtualFolder.Folders.ToArray();
            }
            else
            {
                if (location != null)
                {
                    //regular folder
                    if (Directory.Exists(location.Path))
                    {
                        this.watchedFolders = new string[] { location.Path };
                    }
                    else
                    {
                        this.watchedFolders = new string[0];
                        Logger.ReportInfo("Cannot watch non-folder location " + aFolder.Name);
                    }

                }
                else
                {
                    Logger.ReportInfo("Cannot watch non-folder location " + aFolder.Name);
                    return;
                }
            }

            this.fileSystemWatchers = new List<FileSystemWatcher>();
            InitFileSystemWatcher(this.watchedFolders, watchChanges);
            Microsoft.MediaCenter.UI.Application.DeferredInvoke(_ => { InitTimers(); }); //timers only on app thread
        }

        ~MBDirectoryWatcher()
        {
            Dispose();
        }

        public void Dispose()
        {
            Logger.ReportInfo("Disposing MBDirectoryWatcher.");

            if (initialTimer != null)
                initialTimer.Enabled = false;

            initialTimer = null;

            if (fileSystemWatchers != null)
            {
                foreach (var watcher in fileSystemWatchers)
                {
                    watcher.EnableRaisingEvents = false;
                }
            }

            fileSystemWatchers = null;
            GC.SuppressFinalize(this);
        }

        private void InitTimers()
        {
            //when a file event first occurs we will wait five seconds for it to complete before doing our refresh
            this.initialTimer = new Timer();
            this.initialTimer.Enabled = false;
            this.initialTimer.Tick += new EventHandler(InitialTimer_Timeout);
            this.initialTimer.Interval = 5000; // 5 seconds            

            //after that, if events are still occurring wait 60 seconds so we don't continually refresh during long file operations
            this.secondaryTimer = new Timer();
            this.secondaryTimer.Enabled = false;
            this.secondaryTimer.Tick += new EventHandler(SecondaryTimer_Timeout);
            this.secondaryTimer.Interval = 60000; // 60 seconds            
        }        

        private void InitFileSystemWatcher(string[] watchedFolders, bool watchChanges)
        {
            foreach (string folder in watchedFolders)
            {
                try
                {
                    FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(folder,"*.*");
                    fileSystemWatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.DirectoryName | NotifyFilters.FileName;
                    fileSystemWatcher.IncludeSubdirectories = true;
                    if (watchChanges) // we will only watch changes in special situations (startup folder)
                        fileSystemWatcher.Changed += new FileSystemEventHandler(WatchedFolderChanged); 
                    fileSystemWatcher.Created += new FileSystemEventHandler(WatchedFolderCreation);
                    fileSystemWatcher.Deleted += new FileSystemEventHandler(WatchedFolderDeletion);
                    fileSystemWatcher.Renamed += new RenamedEventHandler(WatchedFolderRename);
                    fileSystemWatcher.EnableRaisingEvents = true;

                    this.fileSystemWatchers.Add(fileSystemWatcher);
                    Logger.ReportInfo("Watching folder " + folder + " for changes.");
                }
                catch (Exception ex)
                {
                    Logger.ReportException("Error adding " + folder + " to watched folders. ", ex);
                }
            }            
        }

        private void WatchedFolderUpdated(string FullPath, WatcherChangeTypes changeType)
        {
            try
            {
                if (Directory.Exists(FullPath))
                {
                    if (System.DateTime.Now > lastRefresh.AddMilliseconds(60000))
                    {
                        //initial change event - wait 5 seconds and then update
                        this.initialTimer.Enabled = true;
                        lastRefresh = System.DateTime.Now;
                        Logger.ReportInfo("A change of type \"" + changeType.ToString() + "\" has occured in " + FullPath);
                    }
                    else
                    {
                        //another change within 60 seconds kick off timer if not already
                        if (!secondaryTimer.Enabled)
                        {
                            this.secondaryTimer.Enabled = true;
                            Logger.ReportInfo("Another change within 60 seconds on " + FullPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ReportException("Error adding VF to queue. ", ex);
            }
        }

        private void WatchedFolderChanged(object sender, FileSystemEventArgs e)
        {            
            WatchedFolderUpdated(((FileSystemWatcher)sender).Path, e.ChangeType);
        }

        private void WatchedFolderCreation(object sender, FileSystemEventArgs e)
        {
            WatchedFolderUpdated(((FileSystemWatcher)sender).Path, e.ChangeType);
        }

        private void WatchedFolderDeletion(object sender, FileSystemEventArgs e)
        {
            WatchedFolderUpdated(((FileSystemWatcher)sender).Path, e.ChangeType);
        }

        private void WatchedFolderRename(object sender, FileSystemEventArgs e)
        {
            WatchedFolderUpdated(((FileSystemWatcher)sender).Path, e.ChangeType);
        }

        private void InitialTimer_Timeout(object sender, EventArgs e)
        {
            this.initialTimer.Enabled = false;
            RefreshFolder();
        }

        private void SecondaryTimer_Timeout(object sender, EventArgs e)
        {
            this.secondaryTimer.Enabled = false;
            RefreshFolder();
        }

        private void RefreshFolder()
        {
            Logger.ReportInfo("Refreshing " + Application.CurrentInstance.CurrentFolder.Name + " due to change in "+folder.Name);
            //Refresh whatever folder we are currently viewing plus all parents up the tree
            FolderModel aFolder = Application.CurrentInstance.CurrentFolder;
            aFolder.RefreshUI();
            while (aFolder != Application.CurrentInstance.RootFolderModel && aFolder.PhysicalParent != null)
            {
                aFolder = aFolder.PhysicalParent;
                aFolder.RefreshUI();
            }
        }
        
    }
}
