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
using MediaBrowser.Library.Configuration;
using MediaBrowserService.Code;

namespace MediaBrowserService
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private MediaBrowser.ServiceConfigData _config;
        private Mutex _mutex;
        private Timer _mainLoop;
        public static MainWindow Instance;
        
        private bool _forceClose;
        private bool _hasHandle;
        private bool _shutdown;
        private bool _refreshCanceled;
        private bool _firstIteration = true;
        private bool _refreshRunning;
        private DateTime _refreshStartTime;

        private readonly DateTime _startTime = DateTime.Now;
        private readonly ServiceGuiOptions _serviceOptions;
        private WindowState storedWindowState = WindowState.Normal; //we come up minimized

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _serviceOptions = new ServiceGuiOptions();
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
            var main = new System.Windows.Forms.ContextMenu();

            var restore = new System.Windows.Forms.MenuItem
                              {
                                  Text = "Show Interface..."
                              };
            restore.Click += new EventHandler(restore_Click);
            main.MenuItems.Add(restore);

            var refresh = new System.Windows.Forms.MenuItem
            {
                Text = "Refresh Now"
            };
            refresh.Click += new EventHandler(refresh_Click);
            main.MenuItems.Add(refresh);

            var tipOption = new System.Windows.Forms.MenuItem
            {
                Text = _config.ShowBalloonTip ? "Disable Balloon" : "Enable Balloon"
            };
            tipOption.Click += new EventHandler(notifyIcon_BalloonTipClicked);
            main.MenuItems.Add(tipOption);

            var sep = new System.Windows.Forms.MenuItem
                          {
                              Text = "-"
                          };
            main.MenuItems.Add(sep);

            var exit = new System.Windows.Forms.MenuItem
                           {
                               Text = "Exit"
                           };
            exit.Click += new EventHandler(exit_Click);
            main.MenuItems.Add(exit);

            //set up our systray icon
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/MediaBrowserService;component/MBService.ico")).Stream;
            notifyIcon = new System.Windows.Forms.NotifyIcon
                             {
                                 BalloonTipTitle = "Media Browser Service",
                                 BalloonTipText = "Running in background. Use tray icon to configure...",
                                 Text = "Media Browser Service",
                                 Icon = new System.Drawing.Icon(iconStream),
                                 ContextMenu = main,
                                 Visible = true
                             };
            notifyIcon.DoubleClick += notifyIcon_Click;
            notifyIcon.BalloonTipClicked += new EventHandler(notifyIcon_BalloonTipClicked);
            if (_config.ShowBalloonTip) notifyIcon.ShowBalloonTip(2000);
        }

        void notifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            _config.ShowBalloonTip = !_config.ShowBalloonTip;
            notifyIcon.ContextMenu.MenuItems[2].Text = _config.ShowBalloonTip ? "Disable Balloon" : "Enable Balloon";
            _config.Save();
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
            if (_refreshRunning)
            {
                _refreshCanceled = true;
                _forceClose = true;
                _shutdown = true;
            }
            else
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(() =>
                {
                    _forceClose = true;
                    Close();
                }));
        }

        void OnClose(object sender, System.ComponentModel.CancelEventArgs args)
        {
            if (_forceClose)
            {
                if (notifyIcon != null)
                {
                    notifyIcon.Dispose();
                    notifyIcon = null;
                }
                if (_hasHandle) _mutex.ReleaseMutex();
            }
            else
            {
                //force updates of any changes
                tbxRefreshHour_LostFocus(this, null);
                tbxRefreshInterval_LostFocus(this, null);
                args.Cancel = true; //don't close
                this.Hide();
                if (_config.ShowBalloonTip)
                {
                    notifyIcon.BalloonTipText = "Running in background. Use tray icon to configure.\nClick this message to silence it in the future.";
                    notifyIcon.ShowBalloonTip(1000);
                }
            }
        }

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
            if (Application.Current.Dispatcher.Thread != Thread.CurrentThread)
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)UpdateStatus);
                return;
            }
            if (_refreshCanceled)
            {
                lblSvcActivity.Content = "Last Refresh was canceled by user...";
            }
            else
            {
                lblSvcActivity.Content = "Last Refresh was " + _config.LastFullRefresh.ToString("yyyy-MM-dd HH:mm:ss");
            }
            DateTime nextRefresh = _config.LastFullRefresh.Date.AddDays(_config.FullRefreshInterval);
            if (DateTime.Now.Date >= nextRefresh && DateTime.Now.Hour >= _config.FullRefreshPreferredHour) nextRefresh = nextRefresh.AddDays(1);
            string nextRefreshStr = (DateTime.Now > nextRefresh) ? "Today/Tonight at " + (_config.FullRefreshPreferredHour * 100).ToString("00:00") : 
                nextRefresh.ToString("yyyy-MM-dd") + " at " + (_config.FullRefreshPreferredHour * 100).ToString("00:00");
            lblNextSvcRefresh.Content = "Next Refresh: " + nextRefreshStr;
        }

        private void UpdateElapsedTime()
        {
            if (Application.Current != null && Application.Current.Dispatcher.Thread != Thread.CurrentThread)
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)UpdateElapsedTime);
                return;
            }
            //update our elapsed time
            TimeSpan elapsed = DateTime.Now - _startTime;
            lblElapsed.Content = string.Format("{0} Days {1} Hours and {2} Mins ", elapsed.Days, elapsed.Hours, elapsed.Minutes);
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
                    var elapsed = DateTime.Now - _refreshStartTime;
                    lblSvcActivity.Content = "Refresh Running... Elapsed Time: " + String.Format("{0:00}:{1:00}:{2:00}",elapsed.Hours,elapsed.Minutes,elapsed.Seconds);
                }));
            }
        }


        private void RefreshInterface()
        {
            if (_config == null) return;

            lblVersion.Content = "Version " + Kernel.Instance.VersionStr;
            tbxRefreshHour.Text = _config.FullRefreshPreferredHour.ToString();
            tbxRefreshInterval.Text = _config.FullRefreshInterval.ToString();
            cbxSleep.IsChecked = _config.SleepAfterScheduledRefresh;
            UpdateStatus();
        }
        
        private void tbxRefreshInterval_LostFocus(object sender, RoutedEventArgs e)
        {
            Int32.TryParse(tbxRefreshInterval.Text, out _config.FullRefreshInterval);
            _config.Save();
            UpdateStatus();
        }

        private void tbxRefreshHour_LostFocus(object sender, RoutedEventArgs e)
        {
            Int32.TryParse(tbxRefreshHour.Text, out _config.FullRefreshPreferredHour);
            if (_config.FullRefreshPreferredHour > 24)
            {
                _config.FullRefreshPreferredHour = 2;
                MessageBox.Show("Hour cannot be more than 24. Reset to default.","Invalid Hour");
                tbxRefreshHour.Text = _config.FullRefreshPreferredHour.ToString();
            }
            _config.Save();
            UpdateStatus();
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Char.IsDigit(e.Text[0]);
            base.OnPreviewTextInput(e);
        }
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Async.Queue("Manual Refresh", () => FullRefresh(true));
        }

        private void cbxSleep_Checked(object sender, RoutedEventArgs e)
        {
            _config.SleepAfterScheduledRefresh = cbxSleep.IsChecked.Value;
            _config.Save();
        }

        private void cbxClearCache_Checked(object sender, RoutedEventArgs e)
        {
            _serviceOptions.ClearCacheOption = cbxClearCache.IsChecked.Value;
        }

        private void cbxClearImageCache_Checked(object sender, RoutedEventArgs e)
        {
            _serviceOptions.ClearImageCacheOption = cbxClearImageCache.IsChecked.Value;
            if (_serviceOptions.ClearImageCacheOption)
            {
                if (Kernel.Instance.ConfigData.AllowInternetMetadataProviders)
                {
                    //always show this warning if IP enabled
                    WarnDialog.Show("Clearing the Image Cache will delete ALL the locally-cached images in your library - including " +
                        "posters, backdrops, banners and all IBN-related images.  This can take some time and will cause a MASSIVE download operation " +
                        "to occur on the next refresh or run of Media Browser.  PLEASE do this only if you really need to.\n\n" +
                        "You do NOT need to clear the image cache in order for images to re-build on a manual refresh (if selected).", false);
                }
                else
                    if (!_config.DontWarnImageCache)
                    {
                        _config.DontWarnImageCache = WarnDialog.Show("Clearing the Image Cache will delete ALL the locally-cached images in your library - including " +
                            "posters, backdrops, banners and all IBN-related images.  This can cause the next refresh to take quite some time.  Do this only if you really need to.\n\n" +
                            "You do NOT need to clear the image cache in order for images to re-build on a manual refresh (if selected).");
                        _config.Save();
                    }
            }
        }

        private void cbxIncludeImages_Checked(object sender, RoutedEventArgs e)
        {
            _serviceOptions.IncludeImagesOption = cbxIncludeImages.IsChecked.Value;
        }

        private void cbxGenres_Checked(object sender, RoutedEventArgs e)
        {
            _serviceOptions.IncludeGenresOption = cbxGenres.IsChecked.Value;
        }

        private void cbxStudios_Checked(object sender, RoutedEventArgs e)
        {
            _serviceOptions.IncludeStudiosOption = cbxStudios.IsChecked.Value;
        }

        private void cbxPeople_Checked(object sender, RoutedEventArgs e)
        {
            _serviceOptions.IncludePeopleOption = cbxPeople.IsChecked.Value;
            if (_serviceOptions.IncludePeopleOption)
            {
                if (!_config.DontWarnPeopleImages)
                {
                    _config.DontWarnPeopleImages = WarnDialog.Show("This option will cause ALL actor and director images that are " +
                        "referenced in your library to re-build and be processed by any image processors (such as CoverArt).\n\n" +
                        "This process can take quite some time...");
                    _config.Save();
                }
            }
        }

        private void cbxYears_Checked(object sender, RoutedEventArgs e)
        {
            _serviceOptions.IncludeYearOption = cbxYears.IsChecked.Value;
        }

        private void btnCancelRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Canceling a running refresh may leave your library in an incomplete state.  Are you sure you want to cancel?", "Cancel Refresh", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                btnCancelRefresh.IsEnabled = false;
                _refreshCanceled = true;
            }
        }

        #endregion

        private void Go()
        {
            _mutex = new Mutex(false, Kernel.MBSERVICE_MUTEX_ID);
            {
                //set up so everyone can access
                var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
                var securitySettings = new MutexSecurity();
                try
                {
                    //don't bomb if this fails
                    securitySettings.AddAccessRule(allowEveryoneRule);
                    _mutex.SetAccessControl(securitySettings);
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
                        _hasHandle = _mutex.WaitOne(5000, false);
                        if (_hasHandle == false)
                        {
                            _forceClose = true;
                            this.Close(); //another instance exists
                            return;
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                        // Log the fact the mutex was abandoned in another process, it will still get acquired
                        Logger.ReportWarning("Previous instance of service ended abnormally...");
                    }
                    this.WindowState = WindowState.Minimized;
                    Hide();
                    Kernel.Init(KernelLoadDirective.LoadServicePlugins);
                    _config = Kernel.Instance.ServiceConfigData;
                    CreateNotifyIcon();
                    RefreshInterface();
                    lblSinceDate.Content = "Since: "+_startTime;
                    CoreCommunications.StartListening(); //start listening for commands from core/configurator
                    Logger.ReportInfo("Service Started");
                    _mainLoop = Async.Every(60 * 1000, () => FullRefresh(false)); //repeat every minute
                }
                catch  //some sort of error - release
                {
                    if (_hasHandle)
                    {
                        _mutex.ReleaseMutex();
                    }
                }
            }
        }


        private void FullRefresh(bool force)
        {
            UpdateElapsedTime();
            if (_refreshRunning) 
            {
                return; //get out of here fast
            }

            var verylate = (_config.LastFullRefresh.Date <= DateTime.Now.Date.AddDays(-(_config.FullRefreshInterval * 3)) && _firstIteration);
            var overdue = _config.LastFullRefresh.Date <= DateTime.Now.Date.AddDays(-(_config.FullRefreshInterval));

            _firstIteration = false; //re set this so an interval of 0 doesn't keep firing us off
            UpdateStatus(); // do this so the info will be correct if we were sleeping through our scheduled time

            //Logger.ReportInfo("Ping...verylate: " + verylate + " overdue: " + overdue);
            if (!_refreshRunning && (force || verylate || (overdue && DateTime.Now.Hour >= _config.FullRefreshPreferredHour) && _config.LastFullRefresh.Date != DateTime.Now.Date))
            {
                _refreshRunning = true;
                Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(() =>
                {
                    gbManual.IsEnabled = false;
                    refreshProgress.Value = 0;
                    refreshProgress.Visibility = Visibility.Visible;
                    _refreshCanceled = false;
                    _refreshStartTime = DateTime.Now;
                    btnCancelRefresh.IsEnabled = true;
                    btnCancelRefresh.Visibility = Visibility.Visible;
                    lblSvcActivity.Content = "Refresh Running...";
                    notifyIcon.ContextMenu.MenuItems[1].Enabled = false;
                    notifyIcon.ContextMenu.MenuItems[4].Enabled = false;
                    notifyIcon.ContextMenu.MenuItems[1].Text = "Refresh Running...";
                    Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/MediaBrowserService;component/MBServiceRefresh.ico")).Stream;
                    notifyIcon.Icon = new System.Drawing.Icon(iconStream);
                    lblNextSvcRefresh.Content = "";
                }));

                bool onSchedule = (!force && (DateTime.Now.Hour == _config.FullRefreshPreferredHour));

                Logger.ReportInfo("Full Refresh Started");

                using (new Profiler(Kernel.Instance.GetString("FullRefreshProf")))
                {
                    Kernel.Instance.ReLoadRoot(); // make sure we are dealing with the current state of the library
                    try
                    {
                        if (force)
                        {
                            if (_serviceOptions.ClearCacheOption)
                            {
                                //clear all cache items except displayprefs and playstate
                                Logger.ReportInfo("Clearing Cache on manual refresh...");
                                UpdateProgress("Clearing Cache", 0);
                                Kernel.Instance.ItemRepository.ClearEntireCache();
                            }
                            if (_serviceOptions.ClearImageCacheOption)
                            {
                                try
                                {
                                    Directory.Delete(ApplicationPaths.AppImagePath, true);
                                    Thread.Sleep(1000); //wait for the delete to fiinish
                                }
                                catch (Exception e) { Logger.ReportException("Error trying to clear image cache.", e); } //just log it
                                try
                                {
                                    Directory.CreateDirectory(ApplicationPaths.AppImagePath);
                                    Thread.Sleep(1000); //wait for the directory to create
                                }
                                catch (Exception e) { Logger.ReportException("Error trying to create image cache.", e); } //just log it
                            }
                        }

                        if (FullRefresh(Kernel.Instance.RootFolder, MetadataRefreshOptions.Default))
                        {
                            _config.LastFullRefresh = DateTime.Now;
                            _config.Save();
                        }

                        MBServiceController.SendCommandToCore(IPCCommands.ReloadItems);
                        Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(UpdateStatus));

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
                            notifyIcon.ContextMenu.MenuItems[4].Enabled = true;
                            notifyIcon.ContextMenu.MenuItems[1].Enabled = true;
                            notifyIcon.ContextMenu.MenuItems[1].Text = "Refresh Now";
                            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/MediaBrowserService;component/MBService.ico")).Stream;
                            notifyIcon.Icon = new System.Drawing.Icon(iconStream);
                        }));
                        Kernel.Instance.ReLoadRoot(); // re-dump this to stay clean
                        _refreshRunning = false;

                        if (onSchedule && _config.SleepAfterScheduledRefresh)
                        {
                            Logger.ReportInfo("Putting computer to sleep...");
                            System.Windows.Forms.Application.SetSuspendState(System.Windows.Forms.PowerState.Suspend, true, false);
                        }
                    }
                }
            }

            if (_shutdown) //we were told to shutdown on next iteration (keeps us from shutting down in the middle of a refresh
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(Close));
            }
        }
        
        bool FullRefresh(AggregateFolder folder, MetadataRefreshOptions options)
        {
            int phases = _serviceOptions.AnyImageOptionsSelected ? 3 : 2;
            double totalIterations = folder.RecursiveChildren.Count() * phases;
            if (totalIterations == 0) return true; //nothing to do

            int currentIteration = 0;

            folder.RefreshMetadata(options);

            using (new Profiler(Kernel.Instance.GetString("FullValidationProf")))
            {
                if (!RunActionRecursively(folder, item =>
                {
                    currentIteration++;
                    UpdateProgress("Validating",currentIteration / totalIterations);
                    var f = item as Folder;
                    if (f != null)
                    {
                        f.ValidateChildren();
                    }

                })) return false;
            }

            using (new Profiler(Kernel.Instance.GetString("SlowRefresh")))
            {
                if (!RunActionRecursively(folder, item =>
                {
                    currentIteration++;
                    UpdateProgress("All Metadata",(currentIteration / totalIterations));
                    item.RefreshMetadata(options);
                })) return false;
            }

            if (_serviceOptions.AnyImageOptionsSelected)
            {
                using (new Profiler(Kernel.Instance.GetString("ImageRefresh")))
                {
                    var studiosProcessed = new List<string>();
                    var genresProcessed = new List<string>();
                    var peopleProcessed = new List<string>();
                    var yearsProcessed = new List<string>();

                    if (!RunActionRecursively(folder, item =>
                    {
                        currentIteration++;
                        UpdateProgress("Images",(currentIteration / totalIterations));
                        if (_serviceOptions.IncludeImagesOption) //main images
                        {
                            ThumbSize s = item.Parent != null ? item.Parent.ThumbDisplaySize : new ThumbSize(0, 0);
                            Logger.ReportInfo("Caching all images for " + item.Name + ". Stored primary image size: " + s.Width + "x" + s.Height);
                            item.ReCacheAllImages(s);
                        }
                        // optionally cause genre, poeple, year and studio images to cache as well
                        if (item is Show)
                        {
                            var show = item as Show;
                            if (_serviceOptions.IncludeGenresOption && show.Genres != null)
                            {
                                foreach (var genre in show.Genres)
                                {
                                    if (!genresProcessed.Contains(genre))
                                    {
                                        Genre g = Genre.GetGenre(genre);
                                        g.RefreshMetadata();
                                        if (g.PrimaryImage != null)
                                        {
                                            Logger.ReportInfo("Caching image for genre: " + genre);
                                            g.PrimaryImage.ClearLocalImages();
                                            g.PrimaryImage.GetLocalImagePath();
                                        }
                                        foreach (MediaBrowser.Library.ImageManagement.LibraryImage image in g.BackdropImages)
                                        {
                                            image.GetLocalImagePath();
                                        }
                                        genresProcessed.Add(genre);
                                    }
                                }
                            }
                            if (_serviceOptions.IncludeStudiosOption && show.Studios != null)
                            {
                                foreach (var studio in show.Studios)
                                {
                                    if (!studiosProcessed.Contains(studio))
                                    {
                                        Logger.ReportInfo("Caching image for studio: " + studio);
                                        Studio st = Studio.GetStudio(studio);
                                        st.RefreshMetadata();
                                        if (st.PrimaryImage != null)
                                        {
                                            Logger.ReportInfo("Caching image for studio: " + studio);
                                            st.PrimaryImage.ClearLocalImages();
                                            st.PrimaryImage.GetLocalImagePath();
                                        }
                                        foreach (MediaBrowser.Library.ImageManagement.LibraryImage image in st.BackdropImages)
                                        {
                                            image.ClearLocalImages();
                                            image.GetLocalImagePath();
                                        }
                                        studiosProcessed.Add(studio);
                                    }
                                }
                            }
                            if (_serviceOptions.IncludePeopleOption && show.Actors != null)
                            {
                                foreach (var actor in show.Actors)
                                {
                                    if (!peopleProcessed.Contains(actor.Name))
                                    {
                                        Person p = Person.GetPerson(actor.Name);
                                        p.RefreshMetadata();
                                        if (p.PrimaryImage != null)
                                        {
                                            Logger.ReportInfo("Caching image for person: " + actor.Name);
                                            p.PrimaryImage.ClearLocalImages();
                                            p.PrimaryImage.GetLocalImagePath();
                                        }
                                        foreach (MediaBrowser.Library.ImageManagement.LibraryImage image in p.BackdropImages)
                                        {
                                            image.ClearLocalImages();
                                            image.GetLocalImagePath();
                                        }
                                        peopleProcessed.Add(actor.Name);
                                    }
                                }
                            }
                            if (_serviceOptions.IncludePeopleOption && show.Directors != null)
                            {
                                foreach (var director in show.Directors)
                                {
                                    if (!peopleProcessed.Contains(director))
                                    {
                                        Person p = Person.GetPerson(director);
                                        p.RefreshMetadata();
                                        if (p.PrimaryImage != null)
                                        {
                                            Logger.ReportInfo("Caching image for person: " + director);
                                            p.PrimaryImage.ClearLocalImages();
                                            p.PrimaryImage.GetLocalImagePath();
                                        }
                                        foreach (MediaBrowser.Library.ImageManagement.LibraryImage image in p.BackdropImages)
                                        {
                                            image.ClearLocalImages();
                                            image.GetLocalImagePath();
                                        }
                                        peopleProcessed.Add(director);
                                    }
                                }
                            }
                            if (_serviceOptions.IncludeYearOption && show.ProductionYear != null)
                            {
                                if (!yearsProcessed.Contains(show.ProductionYear.ToString()))
                                {
                                    Year yr = Year.GetYear(show.ProductionYear.ToString());
                                    yr.RefreshMetadata();
                                    if (yr.PrimaryImage != null)
                                    {
                                        Logger.ReportInfo("Caching image for year: " + yr);
                                        yr.PrimaryImage.ClearLocalImages();
                                        yr.PrimaryImage.GetLocalImagePath();
                                    }
                                    foreach (MediaBrowser.Library.ImageManagement.LibraryImage image in yr.BackdropImages)
                                    {
                                        image.ClearLocalImages();
                                        image.GetLocalImagePath();
                                    }
                                    yearsProcessed.Add(show.ProductionYear.ToString());
                                }

                            }
                        }
                    })) return false;
                }
            }
            return true;
        }

        bool RunActionRecursively(Folder folder, Action<BaseItem> action)
        {
            action(folder);
            foreach (var item in folder.RecursiveChildren.OrderByDescending(i => i.DateModified))
            {
                if (_refreshCanceled) return false;
                action(item);
            }
            return true;
        }

    }
}
