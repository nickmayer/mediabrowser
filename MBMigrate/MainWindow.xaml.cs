using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Threading;
using System.IO;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;
using MediaBrowser;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Threading;
using MediaBrowser.Library.Entities;

namespace MBMigrate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private ServiceConfigData _serviceConfig;
        private ConfigData _config;

        public MainWindow()
        {
            InitializeComponent();
            //_serviceConfig = ServiceConfigData.FromFile(ApplicationPaths.ServiceConfigFile);
            _config = ConfigData.FromFile(ApplicationPaths.ConfigFile);
            Async.Queue("Migration", () =>
            {
                Migrate25();
                Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(() => this.Close()));
            });
        }

        public void Migrate25()
        {
            //version 2.5 migration
            Version current = new Version(_config.MBVersion);
            if (current > new Version(2, 0) && current < new Version(2, 5))
            {
                string sqliteDb = Path.Combine(ApplicationPaths.AppCachePath, "cache.db");
                string sqliteDll = Path.Combine(ApplicationPaths.AppConfigPath, "system.data.sqlite.dll");
                if (!_config.EnableExperimentalSqliteSupport)
                    //clean up any old sql db...
                    try
                    {
                        File.Delete(sqliteDb);
                    }
                    catch { }

                Kernel.Init(_config);
                Logger.ReportInfo("==== Migration Process Started...");
                var newRepo = Kernel.Instance.ItemRepository;
                try
                {
                    //var oldRepo = new MediaBrowser.Library.ItemRepository();
                    //UpdateProgress("Preparing...", .03);
                    //Thread.Sleep(15000); //allow old repo to load
                    if (_config.EnableExperimentalSqliteSupport)
                    {
                        UpdateProgress("Backing up DB", .05);
                        Logger.ReportInfo("Attempting to backup cache db...");
                        if (newRepo.BackupDatabase()) Logger.ReportInfo("Database backed up successfully");
                    }
                    //UpdateProgress("PlayStates", .10);
                    ////newRepo.MigratePlayState(oldRepo);
                    
                    //UpdateProgress("DisplayPrefs", .20);
                    //newRepo.MigrateDisplayPrefs(oldRepo);

                    UpdateProgress("Images", .01);
                    //MediaBrowser.Library.ImageManagement.ImageCache.Instance.DeleteResizedImages();

                    if (_config.EnableExperimentalSqliteSupport)
                    {
                        //were already using SQL - our repo can migrate itself
                        UpdateProgress("Items", .80);
                        newRepo.MigrateItems();
                    }
                    else
                    {
                        //need to go through the file-based repo and re-save
                        MediaBrowser.Library.Entities.BaseItem item;
                        int cnt = 0;
                        string[] cacheFiles = Directory.GetFiles(Path.Combine(ApplicationPaths.AppCachePath, "Items"));
                        double total = cacheFiles.Count();
                        foreach (var file in cacheFiles)
                        {
                            UpdateProgress("Items", (double)(cnt / total));
                            try
                            {
                                using (Stream fs = MediaBrowser.Library.Filesystem.ProtectedFileStream.OpenSharedReader(file))
                                {
                                    BinaryReader reader = new BinaryReader(fs);
                                    item = Serializer.Deserialize<MediaBrowser.Library.Entities.BaseItem>(fs);
                                }

                                if (item != null)
                                {
                                    Logger.ReportInfo("Migrating Item: " + item.Name);
                                    newRepo.SaveItem(item);
                                    if (item is Folder)
                                    {
                                        //need to save our children refs
                                        var children = RetrieveChildrenOld(item.Id);
                                        if (children != null) newRepo.SaveChildren(item.Id, children);
                                    }
                                    cnt++;
                                    if (item is Video && (item as Video).RunningTime != null)
                                    {
                                        TimeSpan duration = TimeSpan.FromMinutes((item as Video).RunningTime.Value);
                                        if (duration.Ticks > 0)
                                        {
                                            PlaybackStatus ps = newRepo.RetrievePlayState(item.Id);
                                            decimal pctIn = Decimal.Divide(ps.PositionTicks, duration.Ticks) * 100;
                                            if (pctIn > Kernel.Instance.ConfigData.MaxResumePct)
                                            {
                                                Logger.ReportInfo("Setting " + item.Name + " to 'Watched' based on last played position.");
                                                ps.PositionTicks = 0;
                                                newRepo.SavePlayState(ps);
                                            }
                                        }
                                    }
                                }
                                    
                            }
                            catch (Exception e)
                            {
                                //this could fail if some items have already been refreshed before we migrated them
                                Logger.ReportException("Could not migrate item (probably just old data) " + file + e != null && e.InnerException != null ? " Inner Exception: " + e.InnerException.Message : "", e);
                            }
                        }
                        Logger.ReportInfo(cnt + " Items migrated successfully.");
                    }
                }
                catch (Exception e)
                {
                    Logger.ReportException("Error in migration - will need to re-build cache.", e);
                    try
                    {
                        File.Delete(sqliteDb);
                    }
                    catch { }
                }
                UpdateProgress("Finishing up...",1);
                try
                {
                    Async.RunWithTimeout(newRepo.ShutdownDatabase, 30000); //be sure all writes are flushed
                }
                catch
                {
                    Logger.ReportWarning("Timed out attempting to close out DB.  Assuming all is ok and moving on...");
                }
            }
            else Logger.ReportInfo("Nothing to Migrate.  Version is: " + _config.MBVersion);
            
        }

        public IEnumerable<Guid> RetrieveChildrenOld(Guid id)
        {

            List<Guid> children = new List<Guid>();
            string file = Path.Combine(Path.Combine(ApplicationPaths.AppCachePath, "Children"), id.ToString("N"));
            if (!File.Exists(file)) return null;

            try
            {

                using (Stream fs = MediaBrowser.Library.Filesystem.ProtectedFileStream.OpenSharedReader(file))
                {
                    BinaryReader br = new BinaryReader(fs);
                    lock (children)
                    {
                        var count = br.ReadInt32();
                        var itemsRead = 0;
                        while (itemsRead < count)
                        {
                            children.Add(br.ReadGuid());
                            itemsRead++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.ReportException("Failed to retrieve children:", e);
                return null;
            }
            return children.Count == 0 ? null : children;
        }

        private void UpdateProgress(string step, double pctDone)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(() =>
            {
                lblCurrent.Content = step;
                progress.Value = pctDone;
            }));
        }
    }
}
