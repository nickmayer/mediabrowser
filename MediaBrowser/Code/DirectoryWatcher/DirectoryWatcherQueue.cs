using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Library.DirectoryWatcher
{
    public class DirectoryWatcherQueue
    {
        private List<string> UpdatedDirectories = new List<string>();             

        public bool Add(string folder)
        {
            try
            {
                if (!this.UpdatedDirectories.Contains(folder))
                {
                    this.UpdatedDirectories.Add(folder);
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding directory to queue. " + ex.Message);
            }
        }

        public void ClearQueue()
        {
            try
            {
                this.UpdatedDirectories.Clear();
                this.UpdatedDirectories = new List<string>();
            }
            catch (Exception ex)
            {
                throw new Exception("Error clearing queue. " + ex.Message);
            }
        }

        public string[] GetUpdatedDirectories()
        {
            return (string[])UpdatedDirectories.ToArray();
        }       
    }
}
