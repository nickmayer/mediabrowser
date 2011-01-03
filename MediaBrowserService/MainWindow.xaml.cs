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
using System.Security.Principal;
using System.Security.AccessControl;
using System.Threading;
using System.Diagnostics;
using MediaBrowser.Library;
using MediaBrowser.Library.Threading;
using MediaBrowser.Library.Logging;
using MediaBrowser.Util;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Metadata;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Configuration;

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
        private bool includeImagesOption = false;
        private bool includeGenresOption = false;
        private bool includeStudiosOption = false;
        private bool includePeopleOption = false;
        private bool clearCacheOption = false;
        private bool firstIteration = true;
        public static MainWindow Instance;
        private bool shutdown = false;
        private bool refreshCanceled = false;
        private DateTime startTime = DateTime.Now;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                Instance = this;
                Go();
            }
            catch (Exception e)
            {
                Console.WriteLine("Critical error launching Media Browser Service, please report this bug with the full contents below to http://community.mediabrowser.tv");
                Console.WriteLine(e.ToString());
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.InnerException);
                Console.ReadKey();
            }
        }

        private void CreateNotifyIcon()
        {
            //create menu
            System.Windows.Forms.ContextMenu main = new System.Windows.Forms.ContextMenu();
            System.Windows.Forms.MenuItem restore = new System.Windows.Forms.MenuItem();
            restore.Text = "Show Interface...";
            restore.Click += new EventHandler(restore_Click);
            main.MenuItems.Add(restore);
            System.Windows.Forms.MenuItem refresh = new System.Windows.Forms.MenuItem();
            refresh.Text = "Refresh Now";
            refresh.Click += new EventHandler(refresh_Click);
            main.MenuItems.Add(refresh);
            System.Windows.Forms.MenuItem sep = new System.Windows.Forms.MenuItem();
            sep.Text = "-";
            main.MenuItems.Add(sep);
            System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem();
            exit.Text = "Exit";
            exit.Click += new EventHandler(exit_Click);
            main.MenuItems.Add(exit);
            //set up our systray icon
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.BalloonTipTitle = "Media Browser Service";
            notifyIcon.BalloonTipText = "Running in background. Click icon to configure...";
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/MediaBrowserService;component/MBService.ico")).Stream;
            notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            notifyIcon.DoubleClick += notifyIcon_Click;
            notifyIcon.ContextMenu = main;
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(2000);
        }

        void refresh_Click(object sender, EventArgs e)
        {
            //re-route
            btnRefresh_Click(this, null);
        }

        void restore_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = storedWindowState;
        }

        void exit_Click(object sender, EventArgs e)
        {
            Shutdown();
        }

        public void Shutdown()
        {
            //close the app, but wait for refresh to finish if it is going
            if (refreshRunning)
            {
                forceClose = true;
                shutdown = true;
            }
            else
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(() =>
                {
                    forceClose = true;
                    Close();
                }));
        }

        void OnClose(object sender, System.ComponentModel.CancelEventArgs args)
        {
            if (forceClose)
            {
                if (notifyIcon != null)
                {
                    notifyIcon.Dispose();
                    notifyIcon = null;
                }
                if (hasHandle) mutex.ReleaseMutex();
            }
            else
            {
                //force updates of any changes
                tbxRefreshHour_LostFocus(this, null);
                tbxRefreshInterval_LostFocus(this, null);
                args.Cancel = true; //don't close
                this.Hide();
                notifyIcon.ShowBalloonTip(1000);
            }
        }

        private WindowState storedWindowState = WindowState.Normal; //we come up minimized
        void OnStateChanged(object sender, EventArgs args)
        {
            if (WindowState == WindowState.Minimized)
            {
                //force updates of any changes
                tbxRefreshHour_LostFocus(this, null);
                tbxRefreshInterval_LostFocus(this, null);
                Hide();
                //if (notifyIcon != null)
                //    notifyIcon.ShowBalloonTip(2000);
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
            if (refreshCanceled)
            {
                lblSvcActivity.Content = "Last Refresh was canceled by user...";
            }
            else
            {
                lblSvcActivity.Content = "Last Refresh was " + config.LastFullRefresh.ToString("yyyy-MM-dd HH:mm:ss");
            }
            DateTime nextRefresh = config.LastFullRefresh.Date.AddDays(config.FullRefreshInterval);
            if (DateTime.Now.Date >= nextRefresh && DateTime.Now.Hour >= config.FullRefreshPreferredHour) nextRefresh = nextRefresh.AddDays(1);
            string nextRefreshStr = (DateTime.Now > nextRefresh) ? "Today/Tonight at " + (config.FullRefreshPreferredHour * 100).ToString("00:00") : 
                nextRefresh.ToString("yyyy-MM-dd") + " at " + (config.FullRefreshPreferredHour * 100).ToString("00:00");
            lblNextSvcRefresh.Content = "Next Refresh: " + nextRefreshStr;
        }

        private void UpdateElapsedTime()
        {
            if (Application.Current != null && Application.Current.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)UpdateElapsedTime);
                return;
            }
            //update our elapsed time
            TimeSpan elapsed = DateTime.Now - startTime;
            lblElapsed.Content = string.Format("{0} Days {1} Hours and {2} Mins ", (int)elapsed.TotalDays, (int)elapsed.Hours, (int)elapsed.Minutes);
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

            lblVersion.Content = "Version " + Kernel.Instance.VersionStr;
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

        private void cbxClearCache_Checked(object sender, RoutedEventArgs e)
        {
            clearCacheOption = cbxClearCache.IsChecked.Value;
        }

        private void cbxIncludeImages_Checked(object sender, RoutedEventArgs e)
        {
            includeImagesOption = cbxIncludeImages.IsChecked.Value;
            cbxGenres.IsEnabled = cbxStudios.IsEnabled = cbxPeople.IsEnabled = cbxIncludeImages.IsChecked.Value;
        }

        private void cbxGenres_Checked(object sender, RoutedEventArgs e)
        {
            includeGenresOption = cbxGenres.IsChecked.Value;
        }

        private void cbxStudios_Checked(object sender, RoutedEventArgs e)
        {
            includeStudiosOption = cbxStudios.IsChecked.Value;
        }

        private void cbxPeople_Checked(object sender, RoutedEventArgs e)
        {
            includePeopleOption = cbxPeople.IsChecked.Value;
        }

        private void btnCancelRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Canceling a running refresh may leave your library in an incomplete state.  Are you sure you want to cancel?", "Cancel Refresh", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                btnCancelRefresh.IsEnabled = false;
                refreshCanceled = true;
            }
        }

        #endregion

        private void Go()
        {
            mutex = new Mutex(false, Kernel.MBSERVICE_MUTEX_ID);
            {
                //set up so everyone can access
                var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
                var securitySettings = new MutexSecurity();
                try
                {
                    //don't bomb if this fails
                    securitySettings.AddAccessRule(allowEveryoneRule);
                    mutex.SetAccessControl(securitySettings);
                }
                catch (Exception e)
                {
                    //just log the exception and go on
                    Logger.ReportException("Failed setting access rule for mutex.", e);
                }
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
                    lblSinceDate.Content = startTime;
                    CoreCommunications.StartListening(); //start listening for commands from core/configurator
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
            UpdateElapsedTime();
            if (refreshRunning) return; //get out of here fast
            var verylate = (config.LastFullRefresh.Date <= DateTime.Now.Date.AddDays(-(config.FullRefreshInterval * 3)) && firstIteration);
            var overdue = config.LastFullRefresh.Date <= DateTime.Now.Date.AddDays(-(config.FullRefreshInterval));

            firstIteration = false; //re set this so an interval of 0 doesn't keep firing us off
            UpdateStatus(); // do this so the info will be correct if we were sleeping through our scheduled time

            //Logger.ReportInfo("Ping...verylate: " + verylate + " overdue: " + overdue);
            if (!refreshRunning && (force || verylate || (overdue && DateTime.Now.Hour >= config.FullRefreshPreferredHour) && config.LastFullRefresh.Date != DateTime.Now.Date))
            {
                refreshRunning = true;
                Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(() =>
                {
                    gbManual.IsEnabled = false;
                    refreshProgress.Value = 0;
                    refreshProgress.Visibility = Visibility.Visible;
                    refreshCanceled = false;
                    btnCancelRefresh.IsEnabled = true;
                    btnCancelRefresh.Visibility = Visibility.Visible;
                    lblSvcActivity.Content = "Refresh Running...";
                    notifyIcon.ContextMenu.MenuItems[1].Enabled = false;
                    notifyIcon.ContextMenu.MenuItems[1].Text = "Refresh Running...";
                    Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/MediaBrowserService;component/MBServiceRefresh.ico")).Stream;
                    notifyIcon.Icon = new System.Drawing.Icon(iconStream);
                    lblNextSvcRefresh.Content = "";
                }));

                bool onSchedule = (!force && (DateTime.Now.Hour == config.FullRefreshPreferredHour));
                Logger.ReportInfo("Full Refresh Started");

                using (new Profiler(Kernel.Instance.GetString("FullRefreshProf")))
                {
                    try
                    {
                        if (force && clearCacheOption)
                        {
                            //clear all cache items except displayprefs and playstate
                            Logger.ReportInfo("Clearing Cache on manual refresh...");
                            Kernel.Instance.ItemRepository.ClearEntireCache();
                            if (includeImagesOption)
                            {
                                try
                                {
                                    Directory.Delete(ApplicationPaths.AppImagePath, true);
                                }
                                catch (Exception e) { Logger.ReportException("Error trying to clear image cache.", e); } //just log it
                                try
                                {
                                    Directory.CreateDirectory(ApplicationPaths.AppImagePath);
                                }
                                catch (Exception e) { Logger.ReportException("Error trying to create image cache.", e); } //just log it
                            }
                        }
                        if (FullRefresh(Kernel.Instance.RootFolder, MetadataRefreshOptions.Default, includeImagesOption))
                        {
                            config.LastFullRefresh = DateTime.Now;
                            config.Save();
                        }
                        MBServiceController.SendCommandToCore(IPCCommands.ReloadItems);
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
                        Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(() =>
                        {
                            refreshProgress.Value = 0;
                            refreshProgress.Visibility = Visibility.Hidden;
                            btnCancelRefresh.Visibility = Visibility.Hidden;
                            gbManual.IsEnabled = true;
                            notifyIcon.ContextMenu.MenuItems[1].Enabled = true;
                            notifyIcon.ContextMenu.MenuItems[1].Text = "Refresh Now";
                            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/MediaBrowserService;component/MBService.ico")).Stream;
                            notifyIcon.Icon = new System.Drawing.Icon(iconStream);
                        }));
                        refreshRunning = false;
                        if (onSchedule && config.SleepAfterScheduledRefresh)
                        {
                            Logger.ReportInfo("Putting computer to sleep...");
                            System.Windows.Forms.Application.SetSuspendState(System.Windows.Forms.PowerState.Suspend, true, false);
                        }
                    }
                }

            }
            if (shutdown) //we were told to shutdown on next iteration (keeps us from shutting down in the middle of a refresh
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(() =>
                {
                    Close();
                }));

        }

        bool FullRefresh(AggregateFolder folder, MetadataRefreshOptions options)
        {
            return FullRefresh(folder, options, false);
        }

        bool FullRefresh(AggregateFolder folder, MetadataRefreshOptions options, bool includeImages)
        {
            double totalIterations = folder.RecursiveChildren.Count() * 3;
            if (totalIterations == 0) return true; //nothing to do

            int currentIteration = 0;

            folder.RefreshMetadata(options);

            using (new Profiler(Kernel.Instance.GetString("FullValidationProf")))
            {
                if (!RunActionRecursively(folder, item =>
                {
                    currentIteration++;
                    UpdateProgress("Validating",currentIteration / totalIterations);
                    Folder f = item as Folder;
                    if (f != null) f.ValidateChildren();
                })) return false;
            }

            using (new Profiler(Kernel.Instance.GetString("FastRefreshProf")))
            {
                if (!RunActionRecursively(folder, item => {
                    currentIteration++;
                    UpdateProgress("Fast Metadata",currentIteration / totalIterations);
                    item.RefreshMetadata(MetadataRefreshOptions.FastOnly);
                })) return false;
            }

            List<string> studiosProcessed = new List<string>();
            List<string> genresProcessed = new List<string>();
            List<string> peopleProcessed = new List<string>();

            using (new Profiler(Kernel.Instance.GetString("SlowRefresh")))
            {
                if (!RunActionRecursively(folder, item =>
                {
                    currentIteration++;
                    UpdateProgress("Slow Metadata",(currentIteration / totalIterations));
                    item.RefreshMetadata(MetadataRefreshOptions.Default);
                    if (includeImages)
                    {
                        ThumbSize s = item.Parent != null ? item.Parent.ThumbDisplaySize : new ThumbSize(0, 0);
                        item.ReCacheAllImages(s);
                        //cause genre and studio images to cache as well
                        if (item is Show)
                        {
                            Show show = item as Show;
                            if (includeGenresOption && show.Genres != null)
                            {
                                foreach (var genre in show.Genres)
                                {
                                    if (!genresProcessed.Contains(genre))
                                    {
                                        Genre g = Genre.GetGenre(genre);
                                        g.RefreshMetadata();
                                        if (g.PrimaryImage != null) g.PrimaryImage.GetLocalImagePath();
                                        foreach (MediaBrowser.Library.ImageManagement.LibraryImage image in g.BackdropImages)
                                        {
                                            image.GetLocalImagePath();
                                        }
                                        genresProcessed.Add(genre);
                                    }
                                }
                            }
                            if (includeStudiosOption && show.Studios != null)
                            {
                                foreach (var studio in show.Studios)
                                {
                                    if (!studiosProcessed.Contains(studio))
                                    {
                                        Studio st = Studio.GetStudio(studio);
                                        st.RefreshMetadata();
                                        if (st.PrimaryImage != null) st.PrimaryImage.GetLocalImagePath();
                                        foreach (MediaBrowser.Library.ImageManagement.LibraryImage image in st.BackdropImages)
                                        {
                                            image.GetLocalImagePath();
                                        }
                                        studiosProcessed.Add(studio);
                                    }
                                }
                            }
                            if (includePeopleOption && show.Actors != null)
                            {
                                foreach (var actor in show.Actors)
                                {
                                    if (!peopleProcessed.Contains(actor.Name))
                                    {
                                        Person p = Person.GetPerson(actor.Name);
                                        p.RefreshMetadata();
                                        if (p.PrimaryImage != null) p.PrimaryImage.GetLocalImagePath();
                                        foreach (MediaBrowser.Library.ImageManagement.LibraryImage image in p.BackdropImages)
                                        {
                                            image.GetLocalImagePath();
                                        }
                                        peopleProcessed.Add(actor.Name);
                                    }
                                }
                            }
                            if (includePeopleOption && show.Directors != null)
                            {
                                foreach (var director in show.Directors)
                                {
                                    if (!peopleProcessed.Contains(director))
                                    {
                                        Person p = Person.GetPerson(director);
                                        p.RefreshMetadata();
                                        if (p.PrimaryImage != null) p.PrimaryImage.GetLocalImagePath();
                                        foreach (MediaBrowser.Library.ImageManagement.LibraryImage image in p.BackdropImages)
                                        {
                                            image.GetLocalImagePath();
                                        }
                                        peopleProcessed.Add(director);
                                    }
                                }
                            }
                        }
                    }
                })) return false;
            }
            return true;
        }

        bool RunActionRecursively(Folder folder, Action<BaseItem> action)
        {
            action(folder);
            foreach (var item in folder.RecursiveChildren.OrderByDescending(i => i.DateModified))
            {
                if (refreshCanceled) return false;
                action(item);
            }
            return true;
        }

    }
}
