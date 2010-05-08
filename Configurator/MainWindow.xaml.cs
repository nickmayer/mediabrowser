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
using System.Security.AccessControl;
using System.Security.Principal;

namespace Configurator
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        ConfigData config;
        Ratings ratings = new Ratings();
        PermissionDialog waitWin;

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
            config = Kernel.Instance.ConfigData;
            LoadComboBoxes();


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

            Async.Queue("Startup Validations", () =>
            {

                RefreshEntryPoints(false);
                ValidateMBAppDataFolderPermissions();


                //wait for plugins to get loaded and then go see if we have updates
                while (!PluginManager.Instance.PluginsLoaded) { }
                if (pluginUpgradesAvailable()) MessageBox.Show("Some of your installed plug-ins have newer versions available.  You should upgrade these plugins from the 'Plug-ins' tab.\n\nYour current versions may not work with this version of MediaBrowser.", "Upgrade Plugins");
            },() =>
                {
                    //be sure latest version gets selected properly
                    Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(() =>
                    {
                        pluginList_SelectionChanged(this, null);
                    }));
                });
        }

        private bool pluginUpgradesAvailable()
        {
            //Look to see if any of our installed plugins have upgrades available
            foreach (IPlugin plugin in Kernel.Instance.Plugins)
            {
                System.Version v = PluginManager.Instance.GetLatestVersion(plugin);
                if (v != null)
                {
                    if (v > plugin.Version && plugin.RequiredMBVersion <= Kernel.Instance.Version) return true;
                }
            }
            return false;
        }


        public void ValidateMBAppDataFolderPermissions()
        {
            String windowsAccount = "Users"; 
            FileSystemRights fileSystemRights = FileSystemRights.FullControl;
            DirectoryInfo folder = new DirectoryInfo(ApplicationPaths.AppConfigPath);

            if(!folder.Exists)
            {
                MessageBox.Show(folder.FullName + " does not exist. Cannot validate permissions.");
                return;
            }
            

            if (!ValidateFolderPermissions(windowsAccount, fileSystemRights, folder))
            {               
                String folderSecurityQuestion = "Your folder permission are not set correctly for MediaBrowser.  "+
                    "Would you like to set these permissions properly?\n\nIf you click 'Yes', here's what we'll do:"+
                    "\n\nThe Group 'Users' will be given full access to ONLY the private program data directory for MediaBrowser."+
                    "\n\nNo other permissions will be altered.\n\nIf you click 'No', no permissions will be altered but MediaBrowser may not function correctly.";
                if (MessageBox.Show(folderSecurityQuestion, "Folder permissions", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    object[] args = new object[3] {folder, windowsAccount, fileSystemRights };
                    this.Dispatcher.Invoke(new SetAccessProcess(setAccess),args);
                }
            }
        }

        public delegate void SetAccessProcess(DirectoryInfo folder, string account,FileSystemRights fsRights);
        public void setAccess(DirectoryInfo folder, string account, FileSystemRights fsRights)
        {
            //hide our main window and throw up a quick dialog to tell user what is going on
            this.Visibility = Visibility.Hidden;
            waitWin = new PermissionDialog();
            waitWin.Show();
            Async.Queue("Set Directory Permissions", () => {
                SetDirectoryAccess(folder, account, fsRights, AccessControlType.Allow);
            }, () => { this.Dispatcher.Invoke(new doneProcess(permissionsDone)); });
        }

        public delegate void doneProcess();
        public void permissionsDone()
        {
            //close window and make us visible
            waitWin.Close();
            this.Visibility = Visibility.Visible;
        }
    


        public bool ValidateFolderPermissions(String windowsAccount, FileSystemRights fileSystemRights, DirectoryInfo folder)
        { 
            try
            {                              
                DirectorySecurity dSecurity = folder.GetAccessControl();

                foreach (FileSystemAccessRule rule in dSecurity.GetAccessRules(true, false, typeof(SecurityIdentifier)))
                {
                    //NTAccount account = new NTAccount(windowsAccount);
                    //SecurityIdentifier sID = account.Translate(typeof(SecurityIdentifier)) as SecurityIdentifier;
                    SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null); 
                    if (sid.CompareTo(rule.IdentityReference as SecurityIdentifier) == 0)
                    {
                        if (fileSystemRights == rule.FileSystemRights)
                            return true; // Validation complete 
                            //return false; //test
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                string msg = "Error validating permissions set on " + folder.FullName + " for the Account \"" + windowsAccount + "\"";
                Logger.ReportException(msg, ex);
                MessageBox.Show(msg);
                return false;
            }                       
        }

        public void SetDirectoryAccess(DirectoryInfo folder, String windowsAccount, FileSystemRights rights, AccessControlType controlType)
        {
            try
            {
                DirectorySecurity dSecurity = folder.GetAccessControl();
                SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                dSecurity.AddAccessRule(new FileSystemAccessRule(sid, rights, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, controlType));                
                folder.SetAccessControl(dSecurity);
            }
            catch (Exception ex)
            {
                string msg = "Error applying permissions to " + folder.FullName + " for the Account \"" + windowsAccount + "\"";
                Logger.ReportException(msg, ex);
                MessageBox.Show(msg);
            }
        }

        public void InitFolderTree()
        {
            tvwLibraryFolders.BeginInit();
            tvwLibraryFolders.Items.Clear();
            tabControl1.Cursor = Cursors.Wait;
            string[] vfs = Directory.GetFiles(ApplicationPaths.AppInitialDirPath,"*.vf");
            foreach (string vfName in vfs)
            {
                TreeViewItem dummyNode = new TreeViewItem();
                dummyNode.Header = new DummyTreeItem();

                TreeViewItem aNode = new TreeViewItem();
                LibraryFolder aFolder = new LibraryFolder(vfName);
                aNode.Header = aFolder;
                aNode.Items.Add(dummyNode);
                
                tvwLibraryFolders.Items.Add(aNode);
            }
            tvwLibraryFolders.EndInit();
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
                    TreeViewItem aNode; // = new TreeViewItem();
                    //LibraryFolder aFolder = new LibraryFolder(subdir);
                    //aNode.Header = aFolder;
                    
                    // Throw back up to main thread to add to TreeView
                    // (System.Windows.Forms.MethodInvoker)(() => { aNode = addLibraryFolderNode(parent, subdir); }));
                    AddLibraryFolderCB addNode = new AddLibraryFolderCB(addLibraryFolderNode);

                    Object returnType;
                    returnType = Dispatcher.Invoke(addNode, DispatcherPriority.Background, parent, subdir);

                    aNode = (TreeViewItem)returnType;

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
                && Directory.GetFiles(path, "*.mpg").Length == 0
                && Directory.GetFiles(path, "*.mpeg").Length == 0
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

                Async.Queue("Podcast Refresher", () =>
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
            cbxOptionAutoEnter.IsChecked = config.AutoEnterSingleDirs;

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
            config.Save();
        }

        private void RefreshThemes()
        {
            ddlOptionViewTheme.ItemsSource = Kernel.Instance.AvailableThemes.Keys;
            if (ddlOptionViewTheme.Items != null)
            {
                if (!ddlOptionViewTheme.Items.Contains(config.ViewTheme))
                {
                    //must have just deleted our theme plugin - set to default
                    config.ViewTheme = "Default";
                    SaveConfig();
                    ddlOptionViewTheme.SelectedItem = config.ViewTheme;
                }
            }
        }

        private void LoadComboBoxes()
        {
            // Themes
            RefreshThemes();            
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

            SortedList<string, VirtualFolder> vfs = new SortedList<string, VirtualFolder>();
            int i = 0; //use this to fill in sortorder if not there

            foreach (var filename in Directory.GetFiles(config.InitialFolder))
            {
                try
                {
                    if (filename.ToLowerInvariant().EndsWith(".vf") ||
                        filename.ToLowerInvariant().EndsWith(".lnk"))
                    {
                        //add to our sorted list
                        VirtualFolder vf = new VirtualFolder(filename);
                        if (vf.SortName == null)
                        {
                            //give it a sortorder if its not there
                            vf.SortName = i.ToString("D3");
                            vf.Save();
                        }
                        vfs.Add(vf.SortName, vf);
                        i = i + 10;
                    }
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
            //now add our items in sorted order
            foreach (VirtualFolder v in vfs.Values)
                folderList.Items.Add(v);
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

        private void WriteVirtualFolder(string dir)
        {
            int sortorder = 0;
            if (folderList.Items != null)
                sortorder = folderList.Items.Count*10;
            var imagePath = FindImage(dir);
            string vf = string.Format(
@"
folder: {0}
sortorder: {2}
{1}
", dir, imagePath,sortorder.ToString("D3"));

            string name = System.IO.Path.GetFileName(dir);
            // workaround for adding c:\
            if (name.Length == 0) {
                name = dir;
                foreach (var chr in System.IO.Path.GetInvalidFileNameChars()) {
                    name = name.Replace(chr.ToString(), "");
                }
            }
            var destination = System.IO.Path.Combine(ApplicationPaths.AppInitialDirPath, name + ".vf");

     
            for (int i = 1; i < 999; i++) {
                if (!File.Exists(destination)) break;
                destination = System.IO.Path.Combine(ApplicationPaths.AppInitialDirPath, name  + i.ToString() + ".vf");
            }

            File.WriteAllText(destination,
                vf.Trim());
        }

        private void updateFolderSort(int start)
        {
            if (folderList.Items != null && (folderList.Items.Count*10) > start)
            {
                //update the sortorder in the list starting with the specified index (we just removed or moved something)
                for (int i = start; i < folderList.Items.Count*10; i = i + 10)
                {
                    VirtualFolder vf = (VirtualFolder)folderList.Items[i/10];
                    vf.SortName = i.ToString("D3");
                    vf.Save();
                }
            }
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
                RefreshEntryPoints(false);
            }
        }

        private void btnFolderTree_Click(object sender, RoutedEventArgs e)
        {
            InitFolderTree();
        }

        private void RefreshEntryPoints()
        {
            this.RefreshEntryPoints(true);
        }

        private void RefreshEntryPoints(bool RefreshPlugins)
        {
            EntryPointManager epm = null;

            try
            {
                epm = new EntryPointManager();
            }
            catch (Exception ex)
            {
                //Write to error log, don't prompt user.
                Logger.ReportError("Error starting Entry Point Manager in RefreshEntryPoints(). " + ex.Message);
                return;
            }

            try
            {
                List<EntryPointItem> entryPoints = new List<EntryPointItem>();

                try
                {
                    Logger.ReportInfo("Reloading Virtual children");
                    if (RefreshPlugins)
                        Kernel.Init(KernelLoadDirective.ShadowPlugins);

                    Kernel.Instance.RootFolder.ValidateChildren();
                }
                catch (Exception ex)
                {
                    Logger.ReportError("Error validating children. " + ex.Message, ex);
                    throw new Exception("Error validating children. " + ex.Message);
                }

                foreach (var folder in Kernel.Instance.RootFolder.Children)                
                {
                    String displayName = folder.Name;
                    if (displayName == null || displayName.Length <= 0)
                        continue;

                    String path = string.Empty;

                    if (folder.GetType() == typeof(Folder) && folder.Path != null && folder.Path.Length > 1)
                    {
                        path = folder.Path;
                    }
                    else
                    {
                        path = folder.Id.ToString();
                    }

                    EntryPointItem ep = new EntryPointItem(displayName, path);
                    entryPoints.Add(ep);                    
                }

                epm.ValidateEntryPoints(entryPoints);
            }
            catch (Exception ex)
            {
                String msg = "Error Refreshing Entry Points. " + ex.Message;
                Logger.ReportError(msg, ex);
                MessageBox.Show(msg);
            }
        }

        private void btnRename_Click(object sender, RoutedEventArgs e)
        {
            String CurrentName = String.Empty;
            String NewName = String.Empty;
            String CurrentContext = String.Empty;
            String NewContext = String.Empty;

            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder != null)
            {
                CurrentName = virtualFolder.Name;
                CurrentContext = virtualFolder.Path;

                var form = new RenameForm(virtualFolder.Name);
                form.Owner = this;
                form.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                var result = form.ShowDialog();
                if (result == true)
                {
                    virtualFolder.Name = form.tbxName.Text;
                    NewName = virtualFolder.Name;
                    NewContext = virtualFolder.Path;
                    this.RenameVirtualFolderEntryPoint(CurrentName, NewName, CurrentContext, NewContext);

                    RefreshItems();
                    RefreshEntryPoints(false);

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

        private void RenameVirtualFolderEntryPoint(String OldName, String NewName, String OldContext, String NewContext)
        {
            EntryPointManager epm = null;

            try
            {
                epm = new EntryPointManager();
            }
            catch (Exception ex)
            {
                //Write to error log, don't prompt user.
                Logger.ReportError("Error starting Entry Point Manager in RenameVirtualFolderEntryPoint(). " + ex.Message);
                return;
            }

            try
            {
                epm.RenameEntryPointTitle(OldName, NewName, OldContext, NewContext);
            }
            catch (Exception ex)
            {
                String msg = "Error renaming Entry Points. " + ex.Message;
                Logger.ReportError(msg);
                MessageBox.Show(msg);
            }
        }

        private void btnRemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            var virtualFolder = folderList.SelectedItem as VirtualFolder;
            if (virtualFolder != null)
            {
                int current = folderList.SelectedIndex*10;

                var message = "About to remove the folder \"" + virtualFolder.Name + "\" from the menu.\nAre you sure?";
                if (
                   MessageBox.Show(message, "Remove folder", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                {

                    File.Delete(virtualFolder.Path);
                    folderList.Items.Remove(virtualFolder);
                    updateFolderSort(current);
                    infoPanel.Visibility = Visibility.Hidden;
                    RefreshEntryPoints(false);
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
                    if (v > plugin.Version && plugin.RequiredMBVersion <= Kernel.Instance.Version)
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

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            //move the current item up in the list
            VirtualFolder vf = (VirtualFolder)folderList.SelectedItem;
            int current = folderList.SelectedIndex*10;
            if (vf != null && current > 0)
            {
                //remove from current location
                folderList.Items.RemoveAt(current/10);
                //add back above item above us
                folderList.Items.Insert((current/10) - 1, vf);
                //and re-index the items below us
                updateFolderSort(current - 10);
                //finally, re-select this item
                folderList.SelectedItem = vf;
            }
        }

        private void btnDn_Click(object sender, RoutedEventArgs e)
        {
            //move the current item down in the list
            VirtualFolder vf = (VirtualFolder)folderList.SelectedItem;
            int current = folderList.SelectedIndex*10;
            if (vf != null && folderList.SelectedIndex < folderList.Items.Count-1)
            {
                //remove from current location
                folderList.Items.RemoveAt(current/10);
                //add back below item below us
                folderList.Items.Insert((current/10) + 1, vf);
                //and re-index the items below us
                updateFolderSort(current);
                //finally, re-select this item
                folderList.SelectedItem = vf;
            }

        }

        private delegate void callBack();

        public void UpgradeFinished()
        {
            //called when the upgrade process finishes - we just hide progress bar and re-enable
            this.IsEnabled = true;
            progress.Value = 0;
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
        private void cbxOptionAutoEnter_Click(object sender, RoutedEventArgs e)
        {
            config.AutoEnterSingleDirs = (bool)cbxOptionAutoEnter.IsChecked;
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
            try
            {

                if (slUnlockPeriod != null)
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
            cacheTab.Visibility = externalPlayersTab.Visibility = displayTab.Visibility = extendersTab.Visibility = folderSecurityTab.Visibility = parentalControlTab.Visibility = Visibility.Collapsed;
        }

        private void hdrAdvanced_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetHeader(hdrAdvanced);
            cacheTab.Visibility = externalPlayersTab.Visibility = displayTab.Visibility = extendersTab.Visibility = folderSecurityTab.Visibility = parentalControlTab.Visibility = Visibility.Visible;
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
                RefreshEntryPoints(false);
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
                    RefreshEntryPoints(false);
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
                RefreshEntryPoints(true);
                RefreshThemes();
            }
        }

        private void addPlugin_Click(object sender, RoutedEventArgs e) {
            AddPluginWindow window = new AddPluginWindow();
            window.Owner = this;
            window.Top = 10;
            window.Left = this.Left + 50;
            if (window.Left + window.Width > SystemParameters.WorkArea.Width) window.Left = SystemParameters.WorkArea.Width - window.Width - 5;
            if (window.Left < 0) window.Left = 5;
            if (SystemParameters.WorkArea.Height - 10 < (window.Height)) window.Height = SystemParameters.WorkArea.Height - 10;
            window.ShowDialog();
            Async.Queue("Refresh after plugin add", () =>
            {
                RefreshEntryPoints(true);
            });
            RefreshThemes();
        }

        private void configurePlugin_Click(object sender, RoutedEventArgs e)
        {
            if (pluginList.SelectedItem != null)            
                ((Plugin)pluginList.SelectedItem).Configure();
            
            this.RefreshEntryPoints(true);  
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
                if (!String.IsNullOrEmpty(curFolder.CustomRating))
                    btnDelFolderRating.IsEnabled = true;
                else
                    btnDelFolderRating.IsEnabled = false;

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
                        {
                            curFolder.SaveXML();
                            btnDelFolderRating.IsEnabled = true;
                        }
                    }
                }
            }
        }

        private void btnDelFolderRating_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem curItem = (TreeViewItem)tvwLibraryFolders.SelectedItem;
            LibraryFolder curFolder = (LibraryFolder)curItem.Header;
            if (curFolder != null)
            {
                curFolder.CustomRating = null;
                ddlFolderRating.SelectedItem = null;
                curFolder.DeleteXML();
                btnDelFolderRating.IsEnabled = false;
            }
        }

        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // Any SelectionChanged event from any controls contained in the TabControl will bubble up and be handled by this event.
            // We are only interested in events related to the Tab selection changing so ignore evertthing else.
            if (e.OriginalSource.ToString().Contains("Controls.Tab")) {
                TabControl tabControl = (sender as TabControl);

                if (tabControl.SelectedItem != null) {
                    TabItem tab = (tabControl.SelectedItem as TabItem);
                    if (tab.Name == "folderSecurityTab") {
                        // Initialise the Folder list by populating the top level items based on the .vf files
                        InitFolderTree();
                    }
                }
            }
        }

        private void tvwLibraryFolders_ItemExpanded(object sender, RoutedEventArgs e) {
            TreeViewItem item = e.OriginalSource as TreeViewItem;
            if (item != null) {
                if ((item.Items.Count == 1) && (((TreeViewItem)item.Items[0]).Header is DummyTreeItem)) {
                    tvwLibraryFolders.Cursor = Cursors.Wait;
                    item.Items.Clear();

                    LibraryFolder aFolder = item.Header as LibraryFolder;
                    VirtualFolder vf = new VirtualFolder(aFolder.FullPath);

                    Async.Queue("LibraryFoldersExpand", () => {
                        foreach (string folder in vf.Folders) {
                            getLibrarySubDirectories(folder, item);
                        }
                    }, () => {
                        Dispatcher.Invoke(DispatcherPriority.Background, (System.Windows.Forms.MethodInvoker)(() => {
                            tvwLibraryFolders.Cursor = Cursors.Hand;
                        }));
                    });
                    }
                }
        }

        TreeViewItem addLibraryFolderNode(TreeViewItem parent, string dir) {
            if (parent.Dispatcher.CheckAccess()) {

                TreeViewItem aNode = new TreeViewItem();
                LibraryFolder aFolder = new LibraryFolder(dir);
                aNode.Header = aFolder;

                parent.Items.Add(aNode);

                return aNode;
            }
            else {
                parent.Dispatcher.Invoke(new AddLibraryFolderCB(this.addLibraryFolderNode), parent, dir);
                return null;
            }
        }

        private delegate TreeViewItem AddLibraryFolderCB(TreeViewItem parent, string dir);

        private void btnClearCache_Click(object sender, RoutedEventArgs e)
        {
            bool error = false;
            //clear selected cache folders
            if (cbxItemCache.IsChecked.Value) {
                try
                {
                    this.Cursor = Cursors.Wait;
                    Directory.Delete(System.IO.Path.Combine(ApplicationPaths.AppCachePath, "items"), true);
                    Directory.Delete(System.IO.Path.Combine(ApplicationPaths.AppCachePath, "children"), true);
                    Directory.Delete(System.IO.Path.Combine(ApplicationPaths.AppCachePath, "providerdata"), true);
                    File.Delete(System.IO.Path.Combine(ApplicationPaths.AppCachePath, "cache.db"));

                    //recreate the directories
                    Directory.CreateDirectory(System.IO.Path.Combine(ApplicationPaths.AppCachePath, "items"));
                    Directory.CreateDirectory(System.IO.Path.Combine(ApplicationPaths.AppCachePath, "children"));
                    Directory.CreateDirectory(System.IO.Path.Combine(ApplicationPaths.AppCachePath, "providerdata"));

                }
                catch (Exception ex)
                {
                    Logger.ReportException("Error trying to delete items cache.", ex);
                    error = true;
                }
                finally
                {
                    this.Cursor = Cursors.Arrow;
                }
            }

            if (cbxImageCache.IsChecked.Value)
            {
                try
                {
                    Directory.Delete(ApplicationPaths.AppImagePath,true);
                    Directory.CreateDirectory(ApplicationPaths.AppImagePath);
                }
                catch (Exception ex)
                {
                    Logger.ReportException("Error trying to delete image cache.", ex);
                    error = true;
                }
            }

            if (cbxPlaystateCache.IsChecked.Value)
            {
                try
                {
                    if (Directory.Exists(System.IO.Path.Combine(ApplicationPaths.AppCachePath, "playstate")))
                    {
                        string[] files = Directory.GetFiles(System.IO.Path.Combine(ApplicationPaths.AppCachePath, "playstate"));
                        foreach (string file in files)
                        {
                            File.Delete(file);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.ReportException("Error trying to delete playstate cache.", ex);
                    error = true;
                }
            }

            if (cbxDisplayCache.IsChecked.Value)
            {
                try
                {
                    string[] files = Directory.GetFiles(System.IO.Path.Combine(ApplicationPaths.AppCachePath, "display"));
                    foreach (string file in files) {
                        File.Delete(file);
                    }
                }
                catch (Exception ex)
                {
                    Logger.ReportException("Error trying to delete display cache.", ex);
                    error = true;
                }
            }

            if (error)
            {
                MessageBox.Show("Unable to clear cache.  If MediaBrowser is running, please close it and try again.  Check log for details.", "Error");
            }
            else
            {
                MessageBox.Show("Selected Cache Areas Cleared Succcessfully.", "Cache Clear");
            }
        }

        private void cbxCache_Click(object sender, RoutedEventArgs e)
        {
            btnClearCache.IsEnabled = cbxItemCache.IsChecked.Value | cbxImageCache.IsChecked.Value | cbxDisplayCache.IsChecked.Value | cbxPlaystateCache.IsChecked.Value;
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

    #region DummyTreeItem Class
    class DummyTreeItem {
    }
    #endregion

}
