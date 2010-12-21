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
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using System.Security.AccessControl;
using System.Threading;
using System.Diagnostics;
using MediaBrowser.Library;
using MediaBrowser.Library.Threading;
using MediaBrowser.Library.Logging;
using MediaBrowser.Util;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Metadata;

namespace MediaBrowserService
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private bool forceClose = false;
        private Mutex mutex;
        private Timer mainLoop;
        private bool hasHandle = false;
        private MediaBrowser.ServiceConfigData config;

        public MainWindow()
        {
            InitializeComponent();
            Go();
        }

        private void CreateNotifyIcon()
        {
            //set up our systray icon
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.BalloonTipTitle = "Media Browser Service";
            notifyIcon.BalloonTipText = "Running in background. Click icon to configure...";
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/MediaBrowserService;component/MBService.ico")).Stream;
            notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            notifyIcon.Click += notifyIcon_Click;
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(2000);
        }


        void OnClose(object sender, System.ComponentModel.CancelEventArgs args)
        {
            if (forceClose || MessageBox.Show("Are you sure you want to close the Media Browser Service?  Library Updates may not occur...", "Exit Service", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (notifyIcon != null)
                {
                    notifyIcon.Dispose();
                    notifyIcon = null;
                }
                if (hasHandle) mutex.ReleaseMutex();
            }
            else args.Cancel = true;
        }

        private WindowState storedWindowState = WindowState.Normal; //we come up minimized
        void OnStateChanged(object sender, EventArgs args)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                if (notifyIcon != null)
                    notifyIcon.ShowBalloonTip(2000);
            }
            else
                storedWindowState = WindowState;
        }

        void notifyIcon_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = storedWindowState;
        }

        private void UpdateStatus()
        {
            if (Application.Current.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)UpdateStatus);
                return;
            }
            lblSvcActivity.Content = "Last Refresh was " + config.LastFullRefresh;
            DateTime nextRefresh = config.LastFullRefresh.Date.AddDays(config.FullRefreshInterval);
            if (DateTime.Now.Date >= nextRefresh && DateTime.Now.Hour >= config.FullRefreshPreferredHour) nextRefresh = nextRefresh.AddDays(1);
            string nextRefreshStr = (DateTime.Now > nextRefresh) ? "Today/Tonight at " + (config.FullRefreshPreferredHour * 100).ToString("00:00") : 
                nextRefresh.ToShortDateString() + " at " + (config.FullRefreshPreferredHour * 100).ToString("00:00");
            lblNextSvcRefresh.Content = "Next Refresh: " + nextRefreshStr;
        }

        #region Interface Handlers

        private void UpdateProgress(string step, double pctDone)
        {
            if (this.Visibility == Visibility.Visible)
            {
                //we only care about this if the interface is actually visible
                Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(() =>
                {
                    refreshProgress.Value = pctDone;
                    lblNextSvcRefresh.Content = step;
                }));
            }
        }


        private void RefreshInterface()
        {
            if (config == null) return;

            tbxRefreshHour.Text = config.FullRefreshPreferredHour.ToString();
            tbxRefreshInterval.Text = config.FullRefreshInterval.ToString();
            cbxSleep.IsChecked = config.SleepAfterScheduledRefresh;
            UpdateStatus();
        }

        private void tbxRefreshInterval_LostFocus(object sender, RoutedEventArgs e)
        {
            Int32.TryParse(tbxRefreshInterval.Text, out config.FullRefreshInterval);
            config.Save();
            UpdateStatus();
        }

        private void tbxRefreshHour_LostFocus(object sender, RoutedEventArgs e)
        {
            Int32.TryParse(tbxRefreshHour.Text, out config.FullRefreshPreferredHour);
            if (config.FullRefreshPreferredHour > 24)
            {
                config.FullRefreshPreferredHour = 2;
                MessageBox.Show("Hour cannot be more than 24. Reset to default.","Invalid Hour");
                tbxRefreshHour.Text = config.FullRefreshPreferredHour.ToString();
            }
            config.Save();
            UpdateStatus();
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Char.IsDigit(e.Text[0]);
            base.OnPreviewTextInput(e);
        }
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Async.Queue("Manual Refresh", () =>
            {
                FullRefresh(true);
            });
        }

        private void cbxSleep_Checked(object sender, RoutedEventArgs e)
        {
            config.SleepAfterScheduledRefresh = cbxSleep.IsChecked.Value;
            config.Save();
        }

        #endregion

        private void Go()
        {
            mutex = new Mutex(false, Kernel.MBSERVICE_MUTEX_ID);
            {
                //set up so everyone can access
                var allowEveryoneRule = new MutexAccessRule("Everyone", MutexRights.FullControl, AccessControlType.Allow);
                var securitySettings = new MutexSecurity();
                securitySettings.AddAccessRule(allowEveryoneRule);
                mutex.SetAccessControl(securitySettings);
                try
                {
                    try
                    {
                        hasHandle = mutex.WaitOne(5000, false);
                        if (hasHandle == false)
                        {
                            forceClose = true;
                            this.Close(); //another instance exists
                            return;
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                        // Log the fact the mutex was abandoned in another process, it will still get acquired
                        Logger.ReportWarning("Previous instance of service ended abnormally...");
                    }
                    CreateNotifyIcon();
                    this.WindowState = WindowState.Minimized;
                    Hide();
                    Kernel.Init(KernelLoadDirective.LoadServicePlugins);
                    config = Kernel.Instance.ServiceConfigData;
                    RefreshInterface();
                    Logger.ReportInfo("Service Started");
                    mainLoop = Async.Every(60 * 1000, () => FullRefresh(false)); //repeat every minute
                }
                catch  //some sort of error - release
                {
                    if (hasHandle)
                        mutex.ReleaseMutex();
                }
            }
        }

        private bool refreshRunning = false;

        private void FullRefresh(bool force)
        {
            if (refreshRunning) return; //get out of here fast

            var verylate = config.LastFullRefresh.Date <= DateTime.Now.Date.AddDays(-(config.FullRefreshInterval * 3));
            var overdue = config.LastFullRefresh.Date <= DateTime.Now.Date.AddDays(-(config.FullRefreshInterval));

            UpdateStatus(); // do this so the info will be correct if we were sleeping through our scheduled time

            //Logger.ReportInfo("Ping...verylate: " + verylate + " overdue: " + overdue);
            if (!refreshRunning && (force || verylate || (overdue && DateTime.Now.Hour >= config.FullRefreshPreferredHour)))
            {
                refreshRunning = true;
                bool onSchedule = (!force && (DateTime.Now.Hour == config.FullRefreshPreferredHour));
                Logger.ReportInfo("Full Refresh Started");

                using (new Profiler(Kernel.Instance.GetString("FullRefreshProf")))
                {
                    try
                    {

                        FullRefresh(Kernel.Instance.RootFolder, MetadataRefreshOptions.Default);
                        config.LastFullRefresh = DateTime.Now;
                        config.Save();
                        Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(() =>
                        {
                            UpdateStatus();
                        }));

                    }
                    catch (Exception ex)
                    {
                        Logger.ReportException("Failed to refresh library! ", ex);
                        Debug.Assert(false, "Full refresh service should never crash!");
                    }
                    finally
                    {
                        Logger.ReportInfo("Full Refresh Finished");
                        refreshRunning = false;
                        if (onSchedule && config.SleepAfterScheduledRefresh)
                        {
                            Logger.ReportInfo("Putting computer to sleep...");
                            System.Windows.Forms.Application.SetSuspendState(System.Windows.Forms.PowerState.Suspend, true, false);
                        }
                    }
                }

            }
        }

        void FullRefresh(AggregateFolder folder, MetadataRefreshOptions options)
        {
            double totalIterations = folder.RecursiveChildren.Count() * 3;
            int currentIteration = 0;

            Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(() =>
            {
                btnRefresh.IsEnabled = false;
                refreshProgress.Value = 0;
                refreshProgress.Visibility = Visibility.Visible;
                lblSvcActivity.Content = "Refresh Running...";
                lblNextSvcRefresh.Content = "";
            }));

            folder.RefreshMetadata(options);

            using (new Profiler(Kernel.Instance.GetString("FullValidationProf")))
            {
                RunActionRecursively(folder, item =>
                {
                    currentIteration++;
                    UpdateProgress("Validating",currentIteration / totalIterations);
                    Folder f = item as Folder;
                    if (f != null) f.ValidateChildren();
                });
            }

            using (new Profiler(Kernel.Instance.GetString("FastRefreshProf")))
            {
                RunActionRecursively(folder, item => {
                    currentIteration++;
                    UpdateProgress("Fast Metadata",currentIteration / totalIterations);
                    item.RefreshMetadata(MetadataRefreshOptions.FastOnly);
                });
            }

            using (new Profiler(Kernel.Instance.GetString("SlowRefresh")))
            {
                RunActionRecursively(folder, item =>
                {
                    currentIteration++;
                    UpdateProgress("Slow Metadata",(currentIteration / totalIterations));
                    item.RefreshMetadata(MetadataRefreshOptions.Default);
                });
            }

            Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(() =>
            {
                refreshProgress.Value = 0;
                refreshProgress.Visibility = Visibility.Hidden;
                btnRefresh.IsEnabled = true;
            }));
        }

        void RunActionRecursively(Folder folder, Action<BaseItem> action)
        {
            action(folder);
            foreach (var item in folder.RecursiveChildren.OrderByDescending(i => i.DateModified))
            {
                action(item);
            }
        }

    }
}
