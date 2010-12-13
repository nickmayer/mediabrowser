using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using MediaBrowser.Library;
using MediaBrowser.Library.Threading;
using MediaBrowser.Library.Logging;
using MediaBrowser.Util;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Metadata;
using System.Threading;

namespace MediaBrowser
{
    public partial class MediaBrowserService : ServiceBase
    {
        public MediaBrowserService()
        {
            InitializeComponent();
            
        }

        protected override void OnStart(string[] args)
        {
            Async.Queue("Startup", () =>
            {
                int retries = 10;
                while (true)
                {
                    try
                    {
                        // ensure the assembly is there 
                        Logger.ReportInfo("Starting the Media Browser Service");
                        break;
                    }
                    catch
                    {
                        if (retries-- < 0)
                        {
                            // failed to init, crash the service
                            throw;
                        }
                        Thread.Sleep(10 * 10000);
                    }
                }

                Kernel.Init(KernelLoadDirective.LoadServicePlugins);
                Async.Every(60 * 1000, FullRefresh);
            }, null , false, 10 * 1000);
        }

        private bool refreshRunning = false;

        private void FullRefresh()
        {
            var verylate = Kernel.Instance.ConfigData.LastFullRefresh < DateTime.Now.AddHours(-(Kernel.Instance.ConfigData.FullRefreshInterval * 3));
            var overdue = Kernel.Instance.ConfigData.LastFullRefresh < DateTime.Now.AddHours(-(Kernel.Instance.ConfigData.FullRefreshInterval));

            if (!refreshRunning && (verylate || (overdue && DateTime.Now.Hour == Kernel.Instance.ConfigData.FullRefreshPreferredHour)))
            {
                refreshRunning = true;
                
                using (new Profiler(Kernel.Instance.GetString("FullRefreshProf")))
                {
                    try
                    {

                        // only refresh for the root entry, this will help speed things up
                        FullRefresh(Kernel.Instance.RootFolder, MetadataRefreshOptions.Default);
                        Kernel.Instance.ConfigData.LastFullRefresh = DateTime.Now;
                        Kernel.Instance.ConfigData.Save();

                    }
                    catch (Exception ex)
                    {
                        Logger.ReportException("Failed to refresh library! ", ex);
                        Debug.Assert(false, "Full refresh thread should never crash!");
                    }
                    finally
                    {
                        refreshRunning = false;
                    }
                }
                
            }
        }

        void FullRefresh(AggregateFolder folder, MetadataRefreshOptions options)
        {

            folder.RefreshMetadata(options);

            using (new Profiler(Kernel.Instance.GetString("FullValidationProf")))
            {
                RunActionRecursively(folder, item =>
                {
                    Folder f = item as Folder;
                    if (f != null) f.ValidateChildren();
                });
            }

            using (new Profiler(Kernel.Instance.GetString("FastRefreshProf")))
            {
                RunActionRecursively(folder, item => item.RefreshMetadata(MetadataRefreshOptions.FastOnly));
            }

            using (new Profiler(Kernel.Instance.GetString("SlowRefresh")))
            {
                RunActionRecursively(folder, item => item.RefreshMetadata(MetadataRefreshOptions.Default));
            }

        }

        void RunActionRecursively(Folder folder, Action<BaseItem> action)
        {
            action(folder);
            foreach (var item in folder.RecursiveChildren.OrderByDescending(i => i.DateModified))
            {
                action(item);
            }
        }

        protected override void OnStop()
        {
        }
    }
}
