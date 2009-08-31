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

namespace Configurator
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ConfigData config;
        StartMenuRegistryEditor startMenuRegistryEditor = new StartMenuRegistryEditor();
        CheckedListBox lbEntryPoints = new CheckedListBox();


        public MainWindow()
        {
            try {        
                Initialize();
            } catch (Exception ex) {
                MessageBox.Show("Failed to start up, please post this contents on mediabrowser.tv/forums " + ex.ToString());
            }

        }

        private void Initialize() {
            Kernel.Init(KernelLoadDirective.ShadowPlugins);
            
            InitializeComponent();
            LoadComboBoxes();            
            
            tabControl1.SelectionChanged += new SelectionChangedEventHandler(tabControl1_SelectionChanged);
            this.lbEntryPoints.CheckBoxCheckedChanged += new CheckedListBox.CheckBoxChangedHandler(lbEntryPoints_CheckBoxCheckedChanged);
            this.lbEntryPoints.SelectionChanged += new SelectionChangedEventHandler(lbEntryPoints_SelectionChanged);

            dpEntryPoints.Children.Add(this.lbEntryPoints);
            this.lbEntryPoints.Width = dpEntryPoints.Width;
            this.lbEntryPoints.Height = dpEntryPoints.Height;            

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

        void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (((TabItem)e.AddedItems[0]).Header == this.multipleEntryTab.Header)
                {
                    this.lbEntryPoints.SelectionChanged -= new SelectionChangedEventHandler(lbEntryPoints_SelectionChanged);
                    this.RefreshMultipleEntriesTab();                   
                    this.lbEntryPoints.UnselectAll();
                    this.lbEntryPoints.Refresh();
                    this.canvasEntryPointDetails.Visibility = Visibility.Hidden;
                    this.lbEntryPoints.SelectionChanged += new SelectionChangedEventHandler(lbEntryPoints_SelectionChanged);
                }
            }
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
            cbxOptionTransparent.IsChecked = config.TransparentBackground;
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
        }

        private void SaveConfig()
        {
            config.Save(ApplicationPaths.ConfigFile);
        }

        private void LoadComboBoxes()
        {
            // Themes
            ddlOptionViewTheme.Items.Add("Classic"); 
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

        private void RefreshMultipleEntriesTab()
        {
            StartMenuRegistryEditor startMenuRegistryEditor = new StartMenuRegistryEditor();
            if (!startMenuRegistryEditor.TestRegistryAccess())
            {
                MessageBox.Show("You do not have the proper permissions to read/write to the registry. Multiple Entry cannot be modified using this account.");
                return;
            }

            MCEntryPointItem MainEntryPoint = startMenuRegistryEditor.FetchEntryPoint(Constants.MB_MAIN_ENTRYPOINT_GUID);
            List<MediaCenterStartMenuItem> StartMenuItems = startMenuRegistryEditor.GetStartMenuItems();

            try
            {
                this.RefreshlbEntryPoints();

                if (MainEntryPoint != null && MainEntryPoint.EntryPointUID.ToLower() == Constants.MB_MAIN_ENTRYPOINT_GUID.ToLower())
                {
                    if (startMenuRegistryEditor.MultipleEntryPointsEnabled)
                    {
                        this.rbDisableMultipleEntry.IsChecked = true;
                        this.canvasMultipleEntryMain.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        this.rbEnableMultipleEntry.IsChecked = true;
                        this.canvasMultipleEntryMain.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    throw new Exception(Constants.MB_MAIN_ENTRYPOINT_GUID + " doesn't exist or is corrupt");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error detecting if Multiple entries is enabled/disabled. The \"Enabled\" setting in " + MainEntryPoint.EntryPointUID + " might be corrupt. " + ex.Message);
                this.rbDisableMultipleEntry.IsChecked = true;
                this.canvasMultipleEntryMain.Visibility = Visibility.Hidden;
                this.rbDisableMultipleEntry.Checked += new RoutedEventHandler(rbDisableMultipleEntry_Checked);
                this.rbEnableMultipleEntry.Checked += new RoutedEventHandler(rbEnableMultipleEntry_Checked);
                return;
            }            
           
            if (StartMenuItems != null && StartMenuItems.Count > 0)
            {
                this.cbStartMenuItems.Items.Clear();
                this.cbStartMenuItems.Items.Add(new MediaCenterStartMenuItem());
                foreach(MediaCenterStartMenuItem item in StartMenuItems)
                {
                    this.cbStartMenuItems.Items.Add(item);
                }
            }

            this.rbDisableMultipleEntry.Checked += new RoutedEventHandler(rbDisableMultipleEntry_Checked);
            this.rbEnableMultipleEntry.Checked += new RoutedEventHandler(rbEnableMultipleEntry_Checked);
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
            this.RefreshlbEntryPoints();
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
            this.RefreshlbEntryPoints();
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
            this.RefreshlbEntryPoints();
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
            config.TransparentBackground = (bool)cbxOptionTransparent.IsChecked;
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
        #endregion

        #region Header Selection Methods
        private void hdrBasic_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetHeader(hdrBasic);
            externalPlayersTab.Visibility = displayTab.Visibility = extendersTab.Visibility = Visibility.Collapsed;
        }

        private void hdrAdvanced_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetHeader(hdrAdvanced);
            externalPlayersTab.Visibility = displayTab.Visibility = extendersTab.Visibility = multipleEntryTab.Visibility = Visibility.Visible;
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
            this.RefreshlbEntryPoints();
        }

        private void addPlugin_Click(object sender, RoutedEventArgs e) {
            AddPluginWindow window = new AddPluginWindow();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
            this.RefreshlbEntryPoints();
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


        #region Multiple Entry Points        

        private void RefreshlbEntryPoints()
        {

            if (!startMenuRegistryEditor.MultipleEntryPointsEnabled) { return; }

            Kernel.Init(KernelLoadDirective.ShadowPlugins);
            Kernel.Instance.RootFolder.ValidateChildren();

            StartMenuRegistryEditor smre = new StartMenuRegistryEditor();

            List<MCEntryPointItem> EntryPoints = smre.FetchMediaBrowserEntryPoints();

            List<MediaCenterStartMenuItem> StartMenuItems = smre.GetStartMenuItems();

            int SelectedIndex = this.lbEntryPoints.SelectedIndex;

            this.lbEntryPoints.Clear();

            foreach (var Child in Kernel.Instance.RootFolder.Children)
            {
                BaseItem Item = (BaseItem)Child;
                bool EntryPointFound = false;

                foreach (var ep in EntryPoints)
                {
                    try
                    {
                        if (Item.Id.ToString().ToLower() == ep.Values.Context.Value.ToLower().Trim().TrimStart('{').TrimEnd('}') || (Item.Path.ToLower() == ep.Values.Context.Value.ToLower() && Item.Path.Length > 1))
                        {
                            this.lbEntryPoints.Add(ep);

                            bool onStartMenu = false;
                            foreach (var startMenu in StartMenuItems)
                            {
                                if (startMenu.Guid.ToLower() == ep.StartMenuCategoryAppId.ToLower() && startMenu.StartMenuCategory.ToLower() == ep.StartMenuCategory.ToLower())
                                {
                                    onStartMenu = true;
                                    break;
                                }
                            }

                            if (Convert.ToBoolean(ep.Values.Enabled.Value))
                            {
                                if (onStartMenu)
                                {
                                    this.lbEntryPoints.CheckItem(ep);
                                }
                                else
                                {
                                    try
                                    {
                                        ep.Values.Enabled.Value = "false";
                                        smre.SaveEntryPoint(ep);
                                    }
                                    catch (Exception)
                                    { }
                                }
                            }

                            EntryPointFound = true;
                            break;
                        }
                    }
                    catch (Exception)
                    { }
                }
                if (!EntryPointFound)
                {
                    String Context = "{" + Item.Id.ToString() + "}";
                    if (Item.Path.Length > 1)
                    {
                        Context = Item.Path;
                    }
                    MCEntryPointItem ep = smre.CreateNewEntryPoint(Item.Name, Context);
                    this.lbEntryPoints.Add(ep);
                }
            }


            //remove unused entrypoints
            try
            {
                foreach (var ep in EntryPoints)
                {
                    bool match = false;
                    foreach (var item in this.lbEntryPoints.Items)
                    {
                        if (ep.EntryPointUID == ((MCEntryPointItem)item).EntryPointUID)
                        {
                            match = true;
                            break;
                        }
                    }
                    if (!match)
                    {
                        smre.DeleteEntryPointKey(ep.EntryPointUID);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error deleting entry point.");
            }

            //this.lbEntryPoints.Sort();

            if (SelectedIndex >= 0)
            {
                this.lbEntryPoints.SelectedIndex = SelectedIndex;
                this.lbEntryPoints.Refresh();
            }
        }

        private void SaveEntryPointDetails()
        {
            StartMenuRegistryEditor smre = new StartMenuRegistryEditor();

            MCEntryPointItem EntryPoint = (MCEntryPointItem)this.lbEntryPoints.SelectedValue;

            if (EntryPoint == null)
            {
                MessageBox.Show("Cannot save " + EntryPoint.EntryPointUID + " as it doesn't exist in the registry or it is corrupt");
            }
            else
            {
                if (this.lbEntryPoints.isChecked(EntryPoint))
                {
                    if (cbStartMenuItems.SelectedValue == null || cbStartMenuItems.SelectedValue.ToString() == String.Empty)
                    {
                        MessageBox.Show("Must select a Start Menu Strip before enabling");
                        return;
                    }
                    EntryPoint.Values.Description.Value = this.tbEntryPointDescription.Text;

                    try
                    {
                        FileInfo file = new FileInfo(new Uri(this.imEntryPointActive.Source.ToString()).LocalPath);
                        if (file.Exists)
                        {
                            EntryPoint.Values.ImageUrl.Value = file.FullName;
                        }
                        else
                        {
                            throw new Exception(String.Empty);
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Active Image is not a valid file. Please select a valid file before enabling.");
                        return;
                    }

                    try
                    {
                        FileInfo file = new FileInfo(new Uri(this.imEntryPointInActive.Source.ToString()).LocalPath);
                        if (file.Exists)
                        {
                            EntryPoint.Values.InactiveImageUrl.Value = file.FullName;
                        }
                        else
                        {
                            throw new Exception(String.Empty);
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Inactive Image is not a valid file. Please select a valid file before enabling.");
                        return;
                    }

                    EntryPoint.StartMenuCategory = ((MediaCenterStartMenuItem)cbStartMenuItems.SelectedItem).StartMenuCategory;
                    EntryPoint.StartMenuCategoryAppId = ((MediaCenterStartMenuItem)cbStartMenuItems.SelectedItem).Guid;

                    int count = 0;
                    try
                    {
                        String EntryPointsInSameMenuStrip = String.Empty;

                        foreach (MCEntryPointItem item in this.lbEntryPoints.Items)
                        {
                            if (item.StartMenuCategoryAppId == EntryPoint.StartMenuCategoryAppId && item.Values.Enabled.Value.ToLower() == true.ToString().ToLower())
                            {
                                count++;
                                EntryPointsInSameMenuStrip += item.Values.Title.Value + ",";
                            }
                        }
                        EntryPointsInSameMenuStrip = EntryPointsInSameMenuStrip.TrimEnd(',');

                        int maxNumItemsInMenuStrip = -1;
                        if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1)// Win 7
                        {
                            maxNumItemsInMenuStrip = Constants.MAX_ITEMS_IN_MENU_STRIP_WIN7;
                        }
                        else
                        {
                            maxNumItemsInMenuStrip = Constants.MAX_ITEMS_IN_MENU_STRIP_VISTA;
                        }

                        if (count >= maxNumItemsInMenuStrip)
                        {
                            MessageBox.Show("You can only have a maximum of " + maxNumItemsInMenuStrip.ToString() + " items in a Start Menu strip."
                                + " The following items are currently in the Menu strip " + EntryPoint.StartMenuCategory + ": " + EntryPointsInSameMenuStrip);

                            this.lbEntryPoints.UnCheckItem(EntryPoint);
                            //this.SaveEntryPointDetails();
                            EntryPoint.Values.Enabled.Value = false.ToString();
                            smre.SaveEntryPoint(EntryPoint);
                            this.RefreshlbEntryPoints();

                            return;
                        }
                    }
                    catch (Exception)
                    {

                    }
                    EntryPoint.Values.Enabled.Value = "true";
                    smre.SaveEntryPoint(EntryPoint);
                }
                else // Don't validate because entrypoint isn't enabled
                {
                    try
                    {
                        EntryPoint.Values.Title.Value = this.tbEntryPointTitle.Content.ToString();
                        EntryPoint.Values.Description.Value = this.tbEntryPointDescription.Text;
                        EntryPoint.Values.Enabled.Value = this.lbEntryPoints.isChecked(EntryPoint).ToString();

                        if (this.imEntryPointActive.Source != null)
                        {
                            EntryPoint.Values.ImageUrl.Value = this.imEntryPointActive.Source.ToString();
                        }
                        else
                        {
                            EntryPoint.Values.ImageUrl.Value = String.Empty;
                        }
                        if (this.imEntryPointInActive.Source != null)
                        {
                            EntryPoint.Values.InactiveImageUrl.Value = new Uri(this.imEntryPointInActive.Source.ToString()).LocalPath;
                        }
                        else
                        {
                            EntryPoint.Values.InactiveImageUrl.Value = String.Empty;
                        }
                        EntryPoint.StartMenuCategory = ((MediaCenterStartMenuItem)cbStartMenuItems.SelectedItem).StartMenuCategory;
                        EntryPoint.StartMenuCategoryAppId = ((MediaCenterStartMenuItem)cbStartMenuItems.SelectedItem).Guid;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error saving entrypoint. Please check input parameters for errors. Error: " + ex.Message);
                        return;
                    }
                    smre.SaveEntryPoint(EntryPoint);
                }

                if (cbStartMenuItems.SelectedValue.ToString().ToUpper() == "TV + Movies".ToUpper() &&
                    Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 0)// Tv + Movies Only for Vista
                {
                    MessageBox.Show("Warning, Only 1 entrypoint for Media Browser can show up in the \"TV + Movies\" Menu Strip. You must create another Menu strip to add multiple entry points.");
                    this.RefreshlbEntryPoints();
                }

            }
        }        
        
        private bool areEntryPointValuesUpdated(String GUID)
        {
            try
            {
                StartMenuRegistryEditor smre = new StartMenuRegistryEditor();
                MCEntryPointItem EntryPoint = smre.FetchEntryPoint(GUID);

                if ( EntryPoint.Values.Description.Value.ToLower() != this.tbEntryPointDescription.Text.ToLower()
                    || EntryPoint.Values.Title.Value.ToLower() != this.tbEntryPointTitle.Content.ToString().ToLower()                    
                    || (new FileInfo(new Uri(EntryPoint.Values.ImageUrl.Value).LocalPath)).FullName.ToLower() != this.getImageSourceFilePath(this.imEntryPointActive).ToLower()
                    || (new FileInfo(new Uri(EntryPoint.Values.InactiveImageUrl.Value).LocalPath)).FullName.ToLower() != this.getImageSourceFilePath(this.imEntryPointInActive).ToLower())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void UpdateEntryPointDetails(MCEntryPointItem entryPoint)
        {
            canvasEntryPointDetails.Visibility = Visibility.Visible;

            try
            {
                if (entryPoint != null && entryPoint.Values != null)
                {
                   
                    this.tbEntryPointTitle.Content = entryPoint.Values.Title.Value;
                    this.tbEntryPointDescription.Text = entryPoint.Values.Description.Value;                    

                    try
                    {
                        String ActivePath = (new FileInfo(new Uri(entryPoint.Values.ImageUrl.Value).LocalPath)).FullName;
                        BitmapImage imageActive = new BitmapImage(new Uri(ActivePath, UriKind.Absolute));
                        this.imEntryPointActive.Source = imageActive;
                        imEntryPointActive.ToolTip = imageActive.UriSource.LocalPath;
                    }
                    catch (Exception)
                    {
                        BitmapImage imageActive = new BitmapImage();
                        this.imEntryPointActive.Source = imageActive;
                        imEntryPointActive.ToolTip = "No image selected";
                    }

                    try
                    {
                        String InActivePath = (new FileInfo(new Uri(entryPoint.Values.InactiveImageUrl.Value).LocalPath)).FullName;
                        BitmapImage imageInActive = new BitmapImage(new Uri(InActivePath, UriKind.Absolute));
                        this.imEntryPointInActive.Source = imageInActive;
                        imEntryPointInActive.ToolTip = imageInActive.UriSource.LocalPath;
                    }
                    catch (Exception)
                    {
                        BitmapImage imageInActive = new BitmapImage();
                        this.imEntryPointInActive.Source = imageInActive;
                        imEntryPointInActive.ToolTip = "No image selected";
                    }
                    try
                    {
                        for (int i=0; i < cbStartMenuItems.Items.Count; i++)
                        {
                            if (entryPoint.StartMenuCategory.ToUpper() == ((MediaCenterStartMenuItem)cbStartMenuItems.Items[i]).StartMenuCategory.ToUpper())
                            {
                                cbStartMenuItems.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("There is an issue with determining where entrypoint " + entryPoint.EntryPointUID + " is located in the Media center start menu. " + ex.Message);
                    }                    
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error formating data for " + entryPoint.EntryPointUID);
                canvasEntryPointDetails.Visibility = Visibility.Hidden;
            }
        }

        private String getImageSourceFilePath(Image image)
        {
            try
            {
                FileInfo file = new FileInfo(new Uri(image.Source.ToString()).LocalPath);
                return file.FullName;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private BitmapImage SelectImagePrompt()
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Image files (*.png, *.jpg)|*.png;*.jpg|All files (*.*)|*.*";
                
                if (ofd.ShowDialog() == true)
                {
                    return new BitmapImage(new Uri(ofd.FileName, UriKind.Absolute));
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void canvasMultipleEntryMain_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (canvasMultipleEntryMain.Visibility == Visibility.Hidden)
            {
                this.canvasEntryPointDetails.Visibility = Visibility.Hidden;
            }
        }

        private void btSaveEntryPointDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {                
                MCEntryPointItem entryPoint = ((MCEntryPointItem)this.lbEntryPoints.SelectedValue);
                
                if (!this.lbEntryPoints.isChecked(entryPoint))
                {
                    if (MessageBox.Show("Would you like to enable " + entryPoint.Values.Title.Value + "?", "Enable " + entryPoint.Values.Title.Value, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        entryPoint.Values.Enabled.Value = true.ToString();
                        this.lbEntryPoints.CheckItem(entryPoint);
                        this.lbEntryPoints_CheckBoxCheckedChanged(this.lbEntryPoints.SelectedValue, null);
                    }
                    else
                    {
                        this.SaveEntryPointDetails();                        
                    }
                }
                else
                {
                    this.SaveEntryPointDetails();                    
                }

                this.RefreshlbEntryPoints();                                             
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unhandled exception occured when trying to save entryPoint. Error:" + ex.Message);
            }
        }       

        private void rbDisableMultipleEntry_Checked(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you would like to disable Multiple Entry access to Media Browser?", "Disable Multiple Entry", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (!(new StartMenuRegistryEditor()).TestRegistryAccess())
                {
                    MessageBox.Show("You do not have the proper permissions to read/write to the registry.");
                    rbEnableMultipleEntry.Checked -= new RoutedEventHandler(rbEnableMultipleEntry_Checked);
                    rbEnableMultipleEntry.IsChecked = true;
                    rbEnableMultipleEntry.Checked += new RoutedEventHandler(rbEnableMultipleEntry_Checked);
                }
                else
                {
                    try
                    {
                        (new StartMenuRegistryEditor()).DisableMultipleEntry();
                        this.canvasMultipleEntryMain.Visibility = Visibility.Hidden;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error disabling multiple entries. " + ex.Message);
                        this.canvasMultipleEntryMain.Visibility = Visibility.Visible;
                    }
                }                
            }
            else
            {
                rbEnableMultipleEntry.Checked -= new RoutedEventHandler(rbEnableMultipleEntry_Checked);
                rbEnableMultipleEntry.IsChecked = true;
                rbEnableMultipleEntry.Checked += new RoutedEventHandler(rbEnableMultipleEntry_Checked);
            }
        }

        private void rbEnableMultipleEntry_Checked(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you would like to enable Multiple Entry access to Media Browser?", "Enable Multiple Entry", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (!(new StartMenuRegistryEditor()).TestRegistryAccess())
                {
                    MessageBox.Show("You do not have the proper permissions to read/write to the registry.");
                    rbDisableMultipleEntry.Checked -= new RoutedEventHandler(rbDisableMultipleEntry_Checked);
                    rbDisableMultipleEntry.IsChecked = true;
                    rbDisableMultipleEntry.Checked += new RoutedEventHandler(rbDisableMultipleEntry_Checked);
                }
                else
                {
                    try
                    {
                        (new StartMenuRegistryEditor()).EnableMultipleEntry();
                        this.canvasMultipleEntryMain.Visibility = Visibility.Visible;
                        this.lbEntryPoints.UnselectAll();                        
                        this.lbEntryPoints.Refresh();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error enabling multiple entries. " + ex.Message);
                        this.canvasMultipleEntryMain.Visibility = Visibility.Hidden;
                    }
                }
            }
            else
            {
                rbDisableMultipleEntry.Checked -= new RoutedEventHandler(rbDisableMultipleEntry_Checked);
                rbDisableMultipleEntry.IsChecked = true;
                rbDisableMultipleEntry.Checked += new RoutedEventHandler(rbDisableMultipleEntry_Checked);
            }
        }      

        private void lbEntryPoints_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            if (e.RemovedItems.Count > 0)
            {
                if (this.areEntryPointValuesUpdated(((MCEntryPointItem)e.RemovedItems[0]).EntryPointUID))
                {
                    if (MessageBox.Show("You have not saved your changes. Do you want to DISCARD your changes?", "Not saved", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        ((CheckedListBox)sender).SelectionChanged -= new SelectionChangedEventHandler(lbEntryPoints_SelectionChanged);
                        ((CheckedListBox)sender).SelectedValue = e.RemovedItems[0];

                        int SelectedIndex = this.lbEntryPoints.SelectedIndex;
                        this.RefreshlbEntryPoints();
                        this.lbEntryPoints.SelectedIndex = SelectedIndex;
                        //this.lbEntryPoints.Refresh();
                        ((CheckedListBox)sender).SelectionChanged += new SelectionChangedEventHandler(lbEntryPoints_SelectionChanged);
                        return;
                    }
                    else
                    {
                        ((CheckedListBox)sender).SelectionChanged -= new SelectionChangedEventHandler(lbEntryPoints_SelectionChanged);
                        this.RefreshlbEntryPoints();
                        ((CheckedListBox)sender).SelectedValue = e.AddedItems[0];
                        ((CheckedListBox)sender).SelectionChanged += new SelectionChangedEventHandler(lbEntryPoints_SelectionChanged);
                    }
                }
            }

            if(e.AddedItems.Count > 0)
            {
                Type t = e.AddedItems[0].GetType();
                
                if (e.AddedItems[0].GetType() != typeof(MCEntryPointItem))
                {
                    MessageBox.Show("Item in list box is not of type MCEntryPointItem.");
                    return;
                }

                MCEntryPointItem entryPoint = (MCEntryPointItem)e.AddedItems[0];

                if (entryPoint != null)
                {
                    UpdateEntryPointDetails(entryPoint);
                }
            }           
        }                
        
        private void bdActiveImage_MouseDown(object sender, MouseButtonEventArgs e)
        {            
            BitmapImage image = this.SelectImagePrompt();

            if (image != null)
            {
                imEntryPointActive.Source = image;
                imEntryPointActive.ToolTip = image.UriSource.LocalPath;
            }
        }    
                
        private void bdInActiveImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BitmapImage image = this.SelectImagePrompt();

            if (image != null)
            {
                imEntryPointInActive.Source = image;
                imEntryPointInActive.ToolTip = image.UriSource.LocalPath;
            }
        }

        private void btEntryPointPriorityUp_Click(object sender, RoutedEventArgs e)
        {
            this.lbEntryPoints.SelectionChanged -= new SelectionChangedEventHandler(lbEntryPoints_SelectionChanged);
            try
            {               
                int CurrentSelectedIndex = this.lbEntryPoints.SelectedIndex;
                if (CurrentSelectedIndex > 0)
                {
                    object CurrentSelectedValue = this.lbEntryPoints.SelectedValue;
                    this.lbEntryPoints.UnselectAll();

                    StartMenuRegistryEditor smre = new StartMenuRegistryEditor();
                    MCEntryPointItem ep1 = smre.FetchEntryPoint(((MCEntryPointItem)this.lbEntryPoints.Items[CurrentSelectedIndex]).EntryPointUID);
                    MCEntryPointItem ep2 = smre.FetchEntryPoint(((MCEntryPointItem)this.lbEntryPoints.Items[CurrentSelectedIndex - 1]).EntryPointUID);
                    smre.SwapTimeStamp(ep1, ep2);
                    smre.SaveEntryPoint(ep1);
                    smre.SaveEntryPoint(ep2);

                    this.RefreshlbEntryPoints();
                    this.lbEntryPoints.SelectedIndex = CurrentSelectedIndex-1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error moving item up. " + ex.Message);
            }
            this.lbEntryPoints.SelectionChanged += new SelectionChangedEventHandler(lbEntryPoints_SelectionChanged);
        }

        private void btEntryPointPriorityDn_Click(object sender, RoutedEventArgs e)
        {
            this.lbEntryPoints.SelectionChanged -= new SelectionChangedEventHandler(lbEntryPoints_SelectionChanged);
            try
            {
                int CurrentSelectedIndex = this.lbEntryPoints.SelectedIndex;
                
                if (CurrentSelectedIndex >=0 && CurrentSelectedIndex < this.lbEntryPoints.Items.Count - 1)
                {
                    this.lbEntryPoints.UnselectAll();

                    StartMenuRegistryEditor smre = new StartMenuRegistryEditor();
                    MCEntryPointItem ep1 = smre.FetchEntryPoint(((MCEntryPointItem)this.lbEntryPoints.Items[CurrentSelectedIndex]).EntryPointUID);
                    MCEntryPointItem ep2 = smre.FetchEntryPoint(((MCEntryPointItem)this.lbEntryPoints.Items[CurrentSelectedIndex + 1]).EntryPointUID);
                    smre.SwapTimeStamp(ep1, ep2);
                    smre.SaveEntryPoint(ep1);
                    smre.SaveEntryPoint(ep2);

                    this.RefreshlbEntryPoints();
                    this.lbEntryPoints.SelectedIndex = CurrentSelectedIndex + 1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error moving item down. " + ex.Message);
            }
            this.lbEntryPoints.SelectionChanged += new SelectionChangedEventHandler(lbEntryPoints_SelectionChanged);
        }
  
        private void lbEntryPoints_CheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            StartMenuRegistryEditor smre = new StartMenuRegistryEditor();
            MCEntryPointItem EntryPoint = (MCEntryPointItem)sender;

            if (this.lbEntryPoints.isChecked((MCEntryPointItem)sender))
            {
                this.SaveEntryPointDetails();                                
            }
            else
            {
                EntryPoint.Values.Enabled.Value = "false";
                smre.SaveEntryPoint(EntryPoint);
            }
        }

        #endregion
        
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
