using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.MediaCenter.UI;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Library.DirectoryWatcher
{
    public class MBDirectoryWatcher : IDisposable
    {        
        private List<FileSystemWatcher> fileSystemWatchers = null;
        private Timer queueTimer = null;
        private DirectoryWatcherQueue changedDirectoriesQueue = null;
        private string[] watchedFolders = null;       

        public delegate void RefreshUI(String FullPath);
        private RefreshUI refreshUI;

        public MBDirectoryWatcher(RefreshUI refreshUI, string[] watchedFolders)
        {            
            this.refreshUI = refreshUI;
            this.watchedFolders = watchedFolders;
            this.fileSystemWatchers = new List<FileSystemWatcher>();
            InitFileSystemWatcher(this.watchedFolders);
            changedDirectoriesQueue = new DirectoryWatcherQueue();
            InitQueueTimer();
        }

        ~MBDirectoryWatcher()
        {
            Dispose();
        }

        public void Dispose()
        {
            Logger.ReportInfo("Disposing MBDirectoryWatcher.");

            if (queueTimer != null)
                queueTimer.Enabled = false;

            queueTimer = null;
            changedDirectoriesQueue = null;

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

        private void InitQueueTimer()
        {
            this.queueTimer = new Timer();
            this.queueTimer.Enabled = false;
            this.queueTimer.Tick += new EventHandler(QueueTimer_Timeout);
            this.queueTimer.Interval = 5000; // 5 seconds            
        }        

        private void InitFileSystemWatcher(string[] watchedFolders)
        {
            foreach (string folder in watchedFolders)
            {
                try
                {
                    FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
                    fileSystemWatcher.Path = folder;
                    fileSystemWatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.DirectoryName;
                    fileSystemWatcher.Filter = "*.*";
                    fileSystemWatcher.IncludeSubdirectories = true;
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
                    if(this.changedDirectoriesQueue.Add(FullPath))
                        Logger.ReportInfo("A change of type \"" + changeType.ToString() + "\" has occured in " + FullPath);

                    this.queueTimer.Enabled = true;
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

        private void QueueTimer_Timeout(object sender, EventArgs e)
        {
            try
            {
                this.queueTimer.Enabled = false;
                Logger.ReportInfo("Directory watcher timer expired.");
                foreach (string folder in this.changedDirectoriesQueue.GetUpdatedDirectories())
                {
                    this.refreshUI(folder);
                }

                this.changedDirectoriesQueue.ClearQueue();
            }
            catch (Exception ex)
            {
                Logger.ReportException("Error in QueueTimer_Timeout() ", ex);
            }
        }
    }
}
