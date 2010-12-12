using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using MediaBrowser.Library.Threading;
using System.Reflection;
using MediaBrowser.Library;
using MediaBrowser.Util;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Metadata;
using MediaBrowser.Library.Entities;

namespace MediaBrowserService
{
    public partial class Service : ServiceBase
    {
        public Service()
        {
            InitializeComponent();
            Kernel.Init(KernelLoadDirective.LoadServicePlugins);
        }

        protected override void OnStart(string[] args)
        {
            Async.Every(60 * 1000, FullRefresh);
        }

        private bool refreshRunning = false;   

        private void FullRefresh() 
        {
            if (!refreshRunning && Kernel.Instance.ConfigData.LastFullRefresh < DateTime.Now.AddHours(-(Kernel.Instance.ConfigData.FullRefreshInterval)))
            {
                refreshRunning = true;
                Async.Queue("Full Refresh", () =>
                {
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
                }, 20 * 1000);
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
