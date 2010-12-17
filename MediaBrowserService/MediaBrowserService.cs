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
using System.Runtime.CompilerServices;

namespace MediaBrowser
{
    public partial class MediaBrowserService : ServiceBase
    {
        public MediaBrowserService()
        {
            InitializeComponent();
            
        }


        // This works around some super annoying msi behavior
        // I can not figure out how to launch the service after installation, so we are stuck with potentially no assembly in the GAC
        private void Startup() 
        {
            Thread.Sleep(10 * 1000);
            int retries = 10;
            while (true)
            {
                try
                {
                    // ensure the assembly is there 
                    CheckIfReady(); 
                    break;
                }
                catch
                {
                    if (retries-- < 0)
                    {
                        // failed to init, crash the service
                        throw;
                    }
                    Thread.Sleep(10 * 1000);
                }
            }

            Go();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CheckIfReady()
        {
            Logger.ReportInfo("Starting Service");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Go()
        {

            Kernel.Init(KernelLoadDirective.LoadServicePlugins);
            Async.Every(60 * 1000, FullRefresh);
        }

        protected override void OnStart(string[] args)
        {
            Thread t = new Thread(new ThreadStart(Startup));
            t.Start();
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
