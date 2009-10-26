using System;
using System.Collections.Generic;
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


using MediaBrowser;
using MediaBrowser.LibraryManagement;
using System.IO;
using Microsoft.Win32;
using MediaBrowser.Code.ShadowTypes;
using System.Xml.Serialization;
using MediaBrowser.Library;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Network;
using MediaBrowser.Library.Logging;
using Configurator.Code;
using MediaBrowser.Library.Plugins;
using System.Diagnostics;
using MediaBrowser.Library.Threading;
using System.Windows.Threading;
using System.Threading;

namespace Configurator
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        ConfigData config;
        Ratings ratings = new Ratings();

        public MainWindow()
        {
            try {        
                Initialize();
            } catch (Exception ex) {
                MessageBox.Show("Failed to start up, please post this contents on http://community.mediabrowser.tv " + ex.ToString());
            }

        }

        private void Initialize() {
            Kernel.Init(KernelLoadDirective.ShadowPlugins);
            
            InitializeComponent();
            LoadComboBoxes();

            config = Kernel.Instance.ConfigData;

            infoPanel.Visibility = Visibility.Hidden;
            infoPlayerPanel.Visibility = Visibility.Hidden;

            // first time the wizard has run 
            if (config.InitialFolder != ApplicationPaths.AppInitialDirPath) {
                try {
                    MigrateOldInitialFolder();
                } catch {
                    MessageBox.Show("For some reason we were not able to migrate your old initial path, you are going to have to start from scratch.");
                }
            }


            config.InitialFolder = ApplicationPaths.AppInitialDirPath;
            RefreshItems();
            RefreshPodcasts();
            RefreshPlayers();

            LoadConfigurationSettings();

            for (char c = 'D'; c <= 'Z'; c++) {
                daemonToolsDrive.Items.Add(c.ToString());
            }

            try {
                daemonToolsDrive.SelectedValue = config.DaemonToolsDrive;
            } catch {
                // someone bodged up the config
            }

            daemonToolsLocation.Content = config.DaemonToolsLocation;


            RefreshExtenderFormats();
            RefreshDisplaySettings();
            podcastDetails(false);
            SaveConfig();

            PluginManager.Init();

        }

        public void InitFolderTree()
        {

            txtLibFolderLoad.Text = "Loading...";
            txtLibFolderLoad.Visibility = Visibility.Visible;
            tvwLibraryFolders.BeginInit();
            tvwLibraryFolders.Items.Clear();
            tabControl1.Cursor = Cursors.Wait;
            string[] vfs = Directory.GetFiles(ApplicationPaths.AppInitialDirPath,"*.vf");
            foreach (string vfName in vfs)
            {
                TreeViewItem aNode = new TreeViewItem();
                LibraryFolder aFolder = new LibraryFolder(vfName);
                aNode.Header = aFolder;
                tvwLibraryFolders.Items.Add(aNode);
                VirtualFolder vf = new VirtualFolder(vfName);
                foreach (string folder in vf.Folders)
                {
                    getLibrarySubDirectories(folder, aNode);
                }

            }
            tvwLibraryFolders.EndInit();
            txtLibFolderLoad.Visibility = Visibility.Hidden;
            tabControl1.Cursor = Cursors.Arrow;


        }

        private void getLibrarySubDirectories(string dir, TreeViewItem parent)
        {
            string[] dirs;
            try
            {
                dirs = Directory.GetDirectories(dir);
            }
            catch (Exception ex)
            {
                //something wrong - can't access the directory try to move on
                Logger.ReportException("Couldn't access directory " + dir, ex);
                return;
            }
            foreach (string subdir in dirs)
            {
                //only want directories that don't directly contain movies in our tree...
                if (!containsMedia(subdir))
                {
                    TreeViewItem aNode = new TreeViewItem();
                    LibraryFolder aFolder = new LibraryFolder(subdir);
                    aNode.Header = aFolder;
                    parent.Items.Add(aNode);
                    getLibrarySubDirectories(subdir, aNode);
                }
            }
        }

        private bool containsMedia(string path)
        {
            if (!File.Exists(path + "\\series.xml")
                && !File.Exists(path + "\\mymovies.xml")
                && !Directory.Exists(path + "\\VIDEO_TS")
                && !Directory.Exists(path + "\\BDMV")
                && !Directory.Exists(path + "\\HVDVD_TS")
                && Directory.GetFiles(path, "*.iso").Length == 0
                && Directory.GetFiles(path, "*.IFO").Length == 0
                && Directory.GetFiles(path, "*.VOB").Length == 0
                && Directory.GetFiles(path, "*.avi").Length == 0
                && Directory.GetFiles(path, "*.mp3").Length == 0
                && Directory.GetFiles(path, "*.mp4").Length == 0
                && Directory.GetFiles(path, "*.mkv").Length == 0
                && Directory.GetFiles(path, "*.m4v").Length == 0
                && Directory.GetFiles(path, "*.mov").Length == 0
                && Directory.GetFiles(path, "*.m2ts").Length == 0
                && Directory.GetFiles(path, "*.wmv").Length == 0 )
                return false;
            else return true;
        }



        private void RefreshPodcasts() {
            var podcasts = Kernel.Instance.GetItem<Folder>(config.PodcastHome);
            podcastList.Items.Clear();

            if (podcasts != null) {

                RefreshPodcasts(podcasts);

                Async.Queue(() =>
                {
                    podcasts.ValidateChildren();

                    foreach (var item in podcasts.Children) {
                        if (item is VodCast) {
                            (item as VodCast).ValidateChildren();
                        }
                    }

                }, () =>
                {
                    Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(() =>
                    {
                        RefreshPodcasts(podcasts);
                    }));
                });
            } 
        }

        private void RefreshPodcasts(Folder podcasts) {
            podcastList.Items.Clear();
            foreach (var item in podcasts.Children) {
                podcastList.Items.Add(item);
            }
        }

        #region Config Loading / Saving        
        private void LoadConfigurationSettings()
        {
            enableTranscode360.IsChecked = config.EnableTranscode360;
            useAutoPlay.IsChecked = config.UseAutoPlayForIso;

            cbxOptionClock.IsChecked = config.ShowClock;            
            cbxOptionTransparent.IsChecked = config.ShowThemeBackground;
            cbxOptionIndexing.IsChecked = config.RememberIndexing;
            cbxOptionDimPoster.IsChecked = config.DimUnselectedPosters;
            cbxOptionHideFrame.IsChecked = config.HideFocusFrame;

            cbxOptionUnwatchedCount.IsChecked      = config.ShowUnwatchedCount;
            cbxOptionUnwatchedOnFolder.IsChecked   = config.ShowWatchedTickOnFolders;
            cbxOptionUnwatchedOnVideo.IsChecked    = config.ShowWatchTickInPosterView;
            cbxOptionUnwatchedDetailView.IsChecked = config.EnableListViewTicks;
            cbxOptionDefaultToUnwatched.IsChecked  = config.DefaultToFirstUnwatched;
            cbxRootPage.IsChecked                  = config.EnableRootPage;
            if (config.MaximumAspectRatioDistortion == Constants.MAX_ASPECT_RATIO_STRETCH)
                cbxOptionAspectRatio.IsChecked = true;
            else
                cbxOptionAspectRatio.IsChecked = false;
            
            
            ddlOptionViewTheme.SelectedItem = config.ViewTheme;
            ddlOptionThemeColor.SelectedItem = config.Theme;
            ddlOptionThemeFont.SelectedItem = config.FontTheme;

            tbxWeatherID.Text = config.YahooWeatherFeed;
            if (config.YahooWeatherUnit.ToLower() == "f")
                ddlWeatherUnits.SelectedItem = "Farenheit";
            else
                ddlWeatherUnits.SelectedItem = "Celsius";

            //Parental Control
            cbxEnableParentalControl.IsChecked = config.ParentalControlEnabled;
            cbxOptionBlockUnrated.IsChecked = config.ParentalBlockUnrated;
            cbxOptionHideProtected.IsChecked = config.HideParentalDisAllowed;
            cbxOptionAutoUnlock.IsChecked = config.UnlockOnPinEntry;
            gbPCGeneral.IsEnabled = gbPCPIN.IsEnabled = config.ParentalControlEnabled;
            ddlOptionMaxAllowedRating.SelectedItem = ratings.ToString(config.MaxParentalLevel);
            slUnlockPeriod.Value = config.ParentalUnlockPeriod;
            txtPCPIN.Password = config.ParentalPIN;

        }

        private void SaveConfig()
        {
            config.Save(ApplicationPaths.ConfigFile);
        }

        private void LoadComboBoxes()
        {
            // Themes
            //ddlOptionViewTheme.Items.Add("Classic"); 
            ddlOptionViewTheme.Items.Add("Default");            
            ddlOptionViewTheme.Items.Add("Diamond");
            ddlOptionViewTheme.Items.Add("Vanilla");
            // Colors
            ddlOptionThemeColor.Items.Add("Default");
            ddlOptionThemeColor.Items.Add("Black");
            ddlOptionThemeColor.Items.Add("Extender Default");
            ddlOptionThemeColor.Items.Add("Extender Black");
            // Fonts 
            ddlOptionThemeFont.Items.Add("Default");
            ddlOptionThemeFont.Items.Add("Small");
            // Weather Units
            ddlWeatherUnits.Items.Add("Celsius");
            ddlWeatherUnits.Items.Add("Farenheit");
            // Parental Ratings
            ddlOptionMaxAllowedRating.ItemsSource = ratings.ToString();
            ddlFolderRating.ItemsSource = ratings.ToString();
            //ddlOptionMaxAllowedRating.Items.Add("G");
            //ddlOptionMaxAllowedRating.Items.Add("PG");
            //ddlOptionMaxAllowedRating.Items.Add("PG-13");
            //ddlOptionMaxAllowedRating.Items.Add("R");
            //ddlOptionMaxAllowedRating.Items.Add("NC-17");
            //ddlOptionMaxAllowedRating.Items.Add("CS");
            //ddlFolderRating.Items.Add("G");
            //ddlFolderRating.Items.Add("PG");
            //ddlFolderRating.Items.Add("PG-13");
            //ddlFolderRating.Items.Add("R");
            //ddlFolderRating.Items.Add("NC-17");
            //ddlFolderRating.Items.Add("CS");

        }
        #endregion

        private void RefreshExtenderFormats()
        {
            extenderFormats.Items.Clear();
            foreach (var format in config.ExtenderNativeTypes.Split(','))
            {
                extenderFormats.Items.Add(format);
            }
        }

        private void RefreshDisplaySettings()
        {
            extenderFormats.Items.Clear();
            foreach (var format in config.ExtenderNativeTypes.Split(','))
            {
                extenderFormats.Items.Add(format);
            }
        }

        private void RefreshItems()
        {

            folderList.Items.Clear();

            foreach (var filename in Directory.GetFiles(config.InitialFolder))
            {
                try
                {
                    if (filename.ToLowerInvariant().EndsWith(".vf") ||
                        filename.ToLowerInvariant().EndsWith(".lnk"))
                        folderList.Items.Add(new VirtualFolder(filename));
                    //else
                    //    throw new ArgumentException("Invalid virtual folder file extension: " + filename);
                }
                catch (ArgumentException)
                {
                    Logger.ReportWarning("Ignored file: " + filename);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Invalid file detected in the initial folder!" + e.ToString());
                    // TODO : alert about dodgy VFs and delete them
                }
            }
        }

        private void RefreshPlayers()
        {
            lstExternalPlayers.Items.Clear();
            foreach (ConfigData.ExternalPlayer item in config.ExternalPlayers)
                lstExternalPlayers.Items.Add(item);
        }

        #region Media Collection methods

        private void MigrateOldInitialFolder()
        {
            var path = config.InitialFolder;
            if (config.InitialFolder == Helper.MY_VIDEOS)
            {
                path = Helper.MyVideosPath;
            }

            foreach (var file in Directory.GetFiles(path))
            {
                if (file.ToLower().EndsWith(".vf"))
                {
                    File.Copy(file, System.IO.Path.Combine(ApplicationPaths.AppInitialDirPath, System.IO.Path.GetFileName(file)), true);
                }
                else if (file.ToLower().EndsWith(".lnk"))
                {
                    WriteVirtualFolder(Helper.ResolveShortcut(file));
                }
            }

            foreach (var dir in Directory.GetDirectories(path))
            {

                WriteVirtualFolder(dir);
            }
        }

        private static void WriteVirtualFolder(string dir)
        {
            var imagePath = FindImage(dir);
            string vf = string.Format(
@"
folder: {0}
{1}
", dir, imagePath);

            string name = System.IO.Path.GetFileName(dir);
            // workaround for adding c:\
            if (name.Length == 0) {
                name = dir;
                foreach (var chr in System.IO.Path.GetInvalidFileNameChars()) {
                    name = name.Replace(chr.ToString(), "");
                }
            }
            var destination = System.IO.Path.Combine(ApplicationPaths.AppInitialDirPath, name + ".vf");

            File.WriteAllText(destination,
                vf.Trim());
        }

        private static string FindImage(string dir)
        {
            string imagePath = "";
            foreach (var file in new string[] { "folder.png", "folder.jpeg", "folder.jpg" })
                if (File.Exists(System.IO.Path.Combine(dir, file)))
                {
                    imagePath = "image: " + System.IO.Path.Combine(dir, file);
                }
            return imagePath;
        }

        #endregion

        #region events
        private void btnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            BrowseForFolderDialog dlg = new BrowseForFolderDialog();

            if (true == dlg.ShowDialog(this))
            {
                WriteVirtualFolder(dlg.SelectedFolder);
                RefreshItems();
            }
        }

        private void btnFolderTree_Click(object sender, RoutedEventArgs e)
        {
            InitFolderTree();
        }


        private void btnRename_Click(object sender, RoutedEventArgs e)
        {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder != null)
            {
                var form = new RenameForm(virtualFolder.Name);
                form.Owner = this;
                form.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                var result = form.ShowDialog();
                if (result == true)
                {
                    virtualFolder.Name = form.tbxName.Text;

                    RefreshItems();

                    foreach (VirtualFolder item in folderList.Items)
                    {
                        if (item.Name == virtualFolder.Name)
                        {
                            folderList.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
        }

        private void btnRemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder != null)
            {

                var message = "About to remove the folder \"" + virtualFolder.Name + "\" from the menu.\nAre you sure?";
                if (
                   MessageBox.Show(message, "Remove folder", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                {

                    File.Delete(virtualFolder.Path);
                    folderList.Items.Remove(virtualFolder);
                    infoPanel.Visibility = Visibility.Hidden;
                }
            }            
        }

        private void btnChangeImage_Click(object sender, RoutedEventArgs e)
        {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder == null) return;

            var dialog = new OpenFileDialog();
            dialog.Title = "Select your image";
            dialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;
            var result = dialog.ShowDialog(this);
            if (result == true)
            {
                virtualFolder.ImagePath = dialog.FileName;
                folderImage.Source = new BitmapImage(new Uri(virtualFolder.ImagePath));
            }
        }

        private void btnAddSubFolder_Click(object sender, RoutedEventArgs e)
        {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder == null) return;

            BrowseForFolderDialog dlg = new BrowseForFolderDialog();
            
            if (true == dlg.ShowDialog(this))
            {
                virtualFolder.AddFolder(dlg.SelectedFolder);
                folderList_SelectionChanged(this, null);
            }
        }

        private void btnRemoveSubFolder_Click(object sender, RoutedEventArgs e)
        {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder == null) return;

            var path = internalFolder.SelectedItem as string;
            if (path != null)
            {
                var message = "Remove \"" + path + "\"?";
                if (
                  MessageBox.Show(message, "Remove folder", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                {
                    virtualFolder.RemoveFolder(path);
                    folderList_SelectionChanged(this, null);
                }
            }
        }

        private void folderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            internalFolder.Items.Clear();

            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder != null)
            {
                foreach (var folder in virtualFolder.Folders)
                {
                    internalFolder.Items.Add(folder);
                }

                if (!string.IsNullOrEmpty(virtualFolder.ImagePath))
                {
                    if (File.Exists(virtualFolder.ImagePath)) {
                        folderImage.Source = new BitmapImage(new Uri(virtualFolder.ImagePath));
                    }
                }
                else
                {
                    folderImage.Source = null;
                }

                infoPanel.Visibility = Visibility.Visible;
            }
        }

        private void pluginList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pluginList.SelectedItem != null)
            {
                IPlugin plugin = pluginList.SelectedItem as IPlugin;
                System.Version v = PluginManager.Instance.GetLatestVersion(plugin);
                if (v != null)
                {
                    if (v > plugin.Version)
                    {
                        upgradePlugin.IsEnabled = true;
                    }
                    else
                    {
                        upgradePlugin.IsEnabled = false;
                    }
                    latestPluginVersion.Content = v.ToString();
                }
                else
                {
                    latestPluginVersion.Content = "Unknown";
                    upgradePlugin.IsEnabled = false;
                }
            }
        }

        private void upgradePlugin_Click(object sender, RoutedEventArgs e) {
            if (pluginList.SelectedItem != null)
            {
                IPlugin plugin = pluginList.SelectedItem as IPlugin;
                //get our original source so we can upgrade...
                IPlugin newPlugin = PluginManager.Instance.AvailablePlugins.Find(plugin);
                if (newPlugin != null)
                {
                    PluginInstaller p = new PluginInstaller();
                    callBack done = new callBack(UpgradeFinished);
                    this.IsEnabled = false;
                    p.InstallPlugin(newPlugin, progress, this, done);
                }
            }
        }

        private delegate void callBack();

        public void UpgradeFinished()
        {
            //called when the upgrade process finishes - we just hide progress bar and re-enable
            this.IsEnabled = true;
            progress.Visibility = Visibility.Hidden;
        }

        private void addExtenderFormat_Click(object sender, RoutedEventArgs e)
        {
            var form = new AddExtenderFormat();
            form.Owner = this;
            form.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var result = form.ShowDialog();
            if (result == true)
            {
                var parser = new FormatParser(config.ExtenderNativeTypes);
                parser.Add(form.formatName.Text);
                config.ExtenderNativeTypes = parser.ToString();
                RefreshExtenderFormats();
                SaveConfig();
            }
        }

        private void removeExtenderFormat_Click(object sender, RoutedEventArgs e)
        {
            var format = extenderFormats.SelectedItem as string;
            if (format != null)
            {
                var message = "Remove \"" + format + "\"?";
                if (
                  MessageBox.Show(message, "Remove folder", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                {
                    var parser = new FormatParser(config.ExtenderNativeTypes);
                    parser.Remove(format);
                    config.ExtenderNativeTypes = parser.ToString();
                    RefreshExtenderFormats();
                    SaveConfig();
                }
            }
        }

        private void changeDaemonToolsLocation_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "*.exe|*.exe";
            var result = dialog.ShowDialog();
            if (result == true)
            {
                config.DaemonToolsLocation = dialog.FileName;
                daemonToolsLocation.Content = config.DaemonToolsLocation;
                SaveConfig();
            }
        }

        private void daemonToolsDrive_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (daemonToolsDrive.SelectedValue != null)
            {
                config.DaemonToolsDrive = (string)daemonToolsDrive.SelectedValue;
            }
            SaveConfig();
        }

        private void btnAddPlayer_Click(object sender, RoutedEventArgs e)
        {
            List<MediaType> list = new List<MediaType>();
            // Provide a list of media types that haven't been used. This is to filter out the selection available to the end user.
            // Don't display media types for players that we already have. 
            //
            // This also makes this scalable, we shouldn't have to adjust this code for new media types.
            Boolean found;
            foreach (MediaType item in Enum.GetValues(typeof(MediaType)))
            {
                // See if an external player has been configured for this media type.
                found = false;
                foreach (ConfigData.ExternalPlayer player in lstExternalPlayers.Items)
                    if (player.MediaType == item) {
                        found = true;
                        break;
                    }
                // If a player hasn't been configured then make it an available option to be added
                if (!found)
                    list.Add(item);
            }

            var form = new SelectMediaTypeForm(list);
            form.Owner = this;
            form.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (form.ShowDialog() == true)
            {
                ConfigData.ExternalPlayer player = new ConfigData.ExternalPlayer();
                player.MediaType = (MediaType)form.cbMediaType.SelectedItem;
                player.Args = "\"{0}\""; // Assign a default parameter
                config.ExternalPlayers.Add(player);
                lstExternalPlayers.Items.Add(player);
                lstExternalPlayers.SelectedItem = player;
                SaveConfig();
            }
        }

        private void btnRemovePlayer_Click(object sender, RoutedEventArgs e)
        {
            var mediaPlayer = lstExternalPlayers.SelectedItem as ConfigData.ExternalPlayer;
            if (mediaPlayer != null)
            {
                var message = "About to remove the media type \"" + lstExternalPlayers.SelectedItem.ToString() + "\" from the external players.\nAre you sure?";
                if (MessageBox.Show(message, "Remove Player", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    config.ExternalPlayers.Remove(mediaPlayer);
                    lstExternalPlayers.Items.Remove(mediaPlayer);
                    SaveConfig();
                    infoPlayerPanel.Visibility = Visibility.Hidden;
                }
            }
        }

        private void btnPlayerCommand_Click(object sender, RoutedEventArgs e)
        {
            var mediaPlayer = lstExternalPlayers.SelectedItem as ConfigData.ExternalPlayer;
            if (mediaPlayer != null)
            {
                var dialog = new OpenFileDialog();
                dialog.Filter = "*.exe|*.exe";
                if (mediaPlayer.Command != string.Empty)
                    dialog.FileName = mediaPlayer.Command;

                if (dialog.ShowDialog() == true)
                {
                    mediaPlayer.Command = dialog.FileName;
                    txtPlayerCommand.Text = mediaPlayer.Command;
                    SaveConfig();
                }
            }
        }

        private void btnPlayerArgs_Click(object sender, RoutedEventArgs e)
        {
            var mediaPlayer = lstExternalPlayers.SelectedItem as ConfigData.ExternalPlayer;
            if (mediaPlayer != null)
            {
                var form = new PlayerArgsForm(mediaPlayer.Args);
                form.Owner = this;
                form.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (form.ShowDialog() == true)
                {
                    mediaPlayer.Args = form.txtArgs.Text;
                    lblPlayerArgs.Text = mediaPlayer.Args;
                    SaveConfig();
                }
            }
        }

        private void lstExternalPlayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstExternalPlayers.SelectedIndex >= 0)
            {
                var mediaPlayer = lstExternalPlayers.SelectedItem as ConfigData.ExternalPlayer;
                if (mediaPlayer != null)
                {
                    txtPlayerCommand.Text = mediaPlayer.Command;
                    lblPlayerArgs.Text = mediaPlayer.Args;
                    infoPlayerPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    txtPlayerCommand.Text = string.Empty;
                    lblPlayerArgs.Text = string.Empty;
                    infoPlayerPanel.Visibility = Visibility.Hidden;
                }
            }
        }
        #endregion

        #region CheckBox Events

        private void useAutoPlay_Click(object sender, RoutedEventArgs e)
        {
            config.UseAutoPlayForIso = (bool)useAutoPlay.IsChecked;
            SaveConfig();
        }
        private void enableTranscode360_Click(object sender, RoutedEventArgs e)
        {
            config.EnableTranscode360 = (bool)enableTranscode360.IsChecked;
            SaveConfig();
        }

        private void cbxOptionClock_Click(object sender, RoutedEventArgs e)
        {
            config.ShowClock = (bool)cbxOptionClock.IsChecked;
            SaveConfig();
        }

        private void cbxOptionTransparent_Click(object sender, RoutedEventArgs e)
        {
            config.ShowThemeBackground = (bool)cbxOptionTransparent.IsChecked;
            SaveConfig();
        }

        private void cbxOptionIndexing_Click(object sender, RoutedEventArgs e)
        {
            config.RememberIndexing = (bool)cbxOptionIndexing.IsChecked;
            SaveConfig();
        }

        private void cbxOptionDimPoster_Click(object sender, RoutedEventArgs e)
        {
            config.DimUnselectedPosters = (bool)cbxOptionDimPoster.IsChecked;
            SaveConfig();
        }

        private void cbxOptionUnwatchedCount_Click(object sender, RoutedEventArgs e)
        {
            config.ShowUnwatchedCount = (bool)cbxOptionUnwatchedCount.IsChecked;
            SaveConfig();
        }

        private void cbxOptionUnwatchedOnFolder_Click(object sender, RoutedEventArgs e)
        {
            config.ShowWatchedTickOnFolders = (bool)cbxOptionUnwatchedOnFolder.IsChecked;
            SaveConfig();
        }

        private void cbxOptionUnwatchedOnVideo_Click(object sender, RoutedEventArgs e)
        {
            config.ShowWatchTickInPosterView = (bool)cbxOptionUnwatchedOnVideo.IsChecked;
            SaveConfig();
        }

        private void cbxOptionUnwatchedDetailView_Click(object sender, RoutedEventArgs e)
        {
            config.EnableListViewTicks = (bool)cbxOptionUnwatchedDetailView.IsChecked;
            SaveConfig();
        }

        private void cbxOptionDefaultToUnwatched_Click(object sender, RoutedEventArgs e)
        {
            config.DefaultToFirstUnwatched = (bool)cbxOptionDefaultToUnwatched.IsChecked;
            SaveConfig();
        }

        private void cbxOptionHideFrame_Click(object sender, RoutedEventArgs e)
        {
            config.HideFocusFrame = (bool)cbxOptionHideFrame.IsChecked;
            SaveConfig();
        }

        private void cbxOptionAspectRatio_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)cbxOptionAspectRatio.IsChecked)
            {
                config.MaximumAspectRatioDistortion = Constants.MAX_ASPECT_RATIO_STRETCH;
            }
            else
            {
                config.MaximumAspectRatioDistortion = Constants.MAX_ASPECT_RATIO_DEFAULT;
            }

            SaveConfig();
        }
        private void cbxRootPage_Click(object sender, RoutedEventArgs e)
        {
            config.EnableRootPage = (bool)cbxRootPage.IsChecked;
            SaveConfig();
        }
        private void cbxOptionBlockUnrated_Click(object sender, RoutedEventArgs e)
        {
            config.ParentalBlockUnrated = (bool)cbxOptionBlockUnrated.IsChecked;
            SaveConfig();
        }
        private void cbxEnableParentalControl_Click(object sender, RoutedEventArgs e)
        {
            //enable/disable other controls on screen
            gbPCGeneral.IsEnabled = gbPCPIN.IsEnabled = (bool)cbxEnableParentalControl.IsChecked;

            config.ParentalControlEnabled = (bool)cbxEnableParentalControl.IsChecked;
            SaveConfig();

        }

        private void cbxOptionHideProtected_Click(object sender, RoutedEventArgs e)
        {
            config.HideParentalDisAllowed = (bool)cbxOptionHideProtected.IsChecked;
            SaveConfig();
        }
        private void cbxOptionAutoUnlock_Click(object sender, RoutedEventArgs e)
        {
            config.UnlockOnPinEntry = (bool)cbxOptionAutoUnlock.IsChecked;
            SaveConfig();
        }

        #endregion

        #region ComboBox Events
        private void ddlOptionViewTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ddlOptionViewTheme.SelectedValue != null)
            {
                config.ViewTheme = ddlOptionViewTheme.SelectedValue.ToString();
            }
            SaveConfig();
        }

        private void ddlOptionThemeColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ddlOptionThemeColor.SelectedValue != null)
            {
                config.Theme = ddlOptionThemeColor.SelectedValue.ToString();
            }
            SaveConfig();
        }

        private void ddlOptionThemeFont_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ddlOptionThemeFont.SelectedValue != null)
            {
                config.FontTheme = ddlOptionThemeFont.SelectedValue.ToString();
            }
            SaveConfig();
        }
        private void ddlOptionMaxAllowedRating_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch ((string)ddlOptionMaxAllowedRating.SelectedItem) {
                case "G": 
                    config.MaxParentalLevel = 1;
                    break;
                
                case "PG":
                    config.MaxParentalLevel = 2;
                    break;
                case "PG-13":
                    config.MaxParentalLevel = 3;
                    break;
                case "R":
                    config.MaxParentalLevel = 4;
                    break;
                case "NC-17":
                    config.MaxParentalLevel = 5;
                    break;
                case "CS":
                    config.MaxParentalLevel = 999;
                    break;
                default:
                    config.MaxParentalLevel = 1000; //default to everything
                    break;
            }
            SaveConfig();
        }

        private void slUnlockPeriod_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //for some reason null check won't work on this slider value... -ebr
            try
            {

                if (slUnlockPeriod.Value != null)
                {
                    config.ParentalUnlockPeriod = (int)slUnlockPeriod.Value;
                }
                SaveConfig();
            }
            catch { }
        }
        #endregion

        #region Header Selection Methods
        private void hdrBasic_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetHeader(hdrBasic);
            externalPlayersTab.Visibility = displayTab.Visibility = extendersTab.Visibility = folderSecurityTab.Visibility = parentalControlTab.Visibility = Visibility.Collapsed;
        }

        private void hdrAdvanced_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetHeader(hdrAdvanced);
            externalPlayersTab.Visibility = displayTab.Visibility = extendersTab.Visibility = folderSecurityTab.Visibility = parentalControlTab.Visibility = Visibility.Visible;
        }

        private void ClearHeaders()
        {
            hdrAdvanced.Foreground = hdrBasic.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Gray);
            hdrAdvanced.FontWeight = hdrBasic.FontWeight = FontWeights.Normal;
            tabControl1.SelectedIndex = 0;
        }
        private void SetHeader(Label label)
        {
            ClearHeaders();
            label.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Black);
            label.FontWeight = FontWeights.Bold;
        }
        #endregion

        private void btnWeatherID_Click(object sender, RoutedEventArgs e)
        {
            if (ddlWeatherUnits.SelectedItem.ToString() == "Farenheit")
                config.YahooWeatherUnit = "f";
            else
                config.YahooWeatherUnit = "c";
            config.YahooWeatherFeed = tbxWeatherID.Text;
            SaveConfig();
        }

        private void addPodcast_Click(object sender, RoutedEventArgs e) {
            var form = new AddPodcastForm();
            form.Owner = this;
            var result = form.ShowDialog();
            if (result == true) {
                form.RSSFeed.Save(config.PodcastHome);
                RefreshPodcasts();
            } 

        }

        private void podcastList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            VodCast vodcast = podcastList.SelectedItem as VodCast;
            if (vodcast != null) {
                podcastDetails(true);
                podcastUrl.Text = vodcast.Url;
                podcastName.Content = vodcast.Name;
                podcastDescription.Text = vodcast.Overview;
            }
        }

        private void removePodcast_Click(object sender, RoutedEventArgs e) {
            VodCast vodcast = podcastList.SelectedItem as VodCast;
            if (vodcast != null) {
                var message = "Remove \"" + vodcast.Name + "\"?";
                if (
                  MessageBox.Show(message, "Remove folder", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes) {
                    File.Delete(vodcast.Path);
                    vodcast.Parent.ValidateChildren();
                    podcastDetails(false);
                    RefreshPodcasts();
                }
            }
        }

        private void renamePodcast_Click(object sender, RoutedEventArgs e) {
            VodCast vodcast = podcastList.SelectedItem as VodCast;
            if (vodcast != null) {
                var form = new RenameForm(vodcast.Name);
                form.Owner = this;
                var result = form.ShowDialog();
                if (result == true) {
                    vodcast.Name = form.tbxName.Text;
                    Kernel.Instance.ItemRepository.SaveItem(vodcast);

                    RefreshPodcasts();

                    foreach (VodCast item in podcastList.Items) {
                        if (item.Name == vodcast.Name) {
                            podcastList.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
        }

        private void removePlugin_Click(object sender, RoutedEventArgs e) {
            var plugin = pluginList.SelectedItem as IPlugin;
            var message = "Would you like to remove the plugin " + plugin.Name + "?";
            if (
                  MessageBox.Show(message, "Remove plugin", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes) {
                PluginManager.Instance.RemovePlugin(plugin);
            }
        }

        private void addPlugin_Click(object sender, RoutedEventArgs e) {
            AddPluginWindow window = new AddPluginWindow();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        private void configurePlugin_Click(object sender, RoutedEventArgs e)
        {
            if (pluginList.SelectedItem != null)
                ((Plugin)pluginList.SelectedItem).Configure();
        }

        private void podcastDetails(bool display)
        {
            if (display)
            {
                podcastName.Visibility = podcastDescription.Visibility = podcastUrl.Visibility = Visibility.Visible;
            }
            else
            {
                podcastName.Visibility = podcastDescription.Visibility = podcastUrl.Visibility = Visibility.Hidden;
            }
        }

        void HandleRequestNavigate(object sender, RoutedEventArgs e)
        {
            string navigateUri = hl.NavigateUri.ToString();
            // if the URI somehow came from an untrusted source, make sure to
            // validate it before calling Process.Start(), e.g. check to see
            // the scheme is HTTP, etc.
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }

                



        private void savePCPIN(object sender, RoutedEventArgs e)
        {
            //first be sure its valid
            if (txtPCPIN.Password.Length != 4)
            {
                MessageBox.Show("PIN Must be EXACTLY FOUR digits.", "Invalid PIN");
                return;
            }
            else try
                {
                    //try and convert to a number - it should convert to an integer
                    int test = Convert.ToInt16(txtPCPIN.Password);
                }
                catch
                {
                    MessageBox.Show("PIN Must be four DIGITS (that can be typed on a remote)", "Invalid PIN");
                    return;
                }
            //appears to be valid - save it
            config.ParentalPIN = txtPCPIN.Password;
            SaveConfig();
        }

        private void tvwLibraryFolders_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem curItem = (TreeViewItem)tvwLibraryFolders.SelectedItem;
            LibraryFolder curFolder = (LibraryFolder)curItem.Header;
            if (curFolder != null)
            {
                ddlFolderRating.IsEnabled = true;
                ddlFolderRating.SelectedItem = curFolder.CustomRating;
            }
        }

        private void ddlFolderRating_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!tvwLibraryFolders.Items.IsEmpty && ddlFolderRating.SelectedItem != null)
            {
                TreeViewItem curItem = (TreeViewItem)tvwLibraryFolders.SelectedItem;
                if (curItem != null)
                {
                    LibraryFolder curFolder = (LibraryFolder)curItem.Header;
                    if (curFolder != null && ddlFolderRating.SelectedValue != null)
                    {
                        curFolder.CustomRating = ddlFolderRating.SelectedValue.ToString();
                        if (curFolder.CustomRating != null)
                            curFolder.SaveXML();
                    }
                }
            }
        }

        
    }
    #region FormatParser Class
    class FormatParser
    {

        List<string> currentFormats = new List<string>();

        public FormatParser(string value)
        {
            currentFormats.AddRange(value.Split(','));
        }

        public void Add(string format)
        {
            format = format.Trim();
            if (!format.StartsWith("."))
            {
                format = "." + format;
            }
            format = format.ToLower();

            if (format.Length > 1)
            {
                if (!currentFormats.Contains(format))
                {
                    currentFormats.Add(format);
                }
            }
        }

        public void Remove(string format)
        {
            currentFormats.Remove(format);
        }

        public override string ToString()
        {
            return String.Join(",", currentFormats.ToArray());
        }


    }
    #endregion
}
