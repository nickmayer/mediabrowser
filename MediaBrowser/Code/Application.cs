using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.MediaCenter;
using Microsoft.MediaCenter.UI;
using MediaBrowser.Util;
using System;
using System.Reflection;
using System.IO;
using System.Resources;
using Microsoft.MediaCenter.AddIn;
using MediaBrowser.Library;
using MediaBrowser.LibraryManagement;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Code;
using MediaBrowser.Library.Playables;
using System.Linq;
using MediaBrowser.Library.Threading;
using MediaBrowser.Library.Metadata;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.EntityDiscovery;
using MediaBrowser.Library.RemoteControl;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.UI;
using MediaBrowser.Library.Input;
using MediaBrowser.Library.Localization;


namespace MediaBrowser
{

    public class Application : ModelItem, IDisposable
    {
        public Config Config
        {
            get
            {
                return Config.Instance;
            }
        }

        public static Application CurrentInstance
        {
            get { return singleApplicationInstance; }
        }

        public string StringData(string name)
        {
            return Kernel.Instance.GetString(name);
        }

        public MBPropertySet LocalStrings //used to access our localized strings from mcml
        {
            get
            {
                return Kernel.Instance.LocalStrings;
            }
        }

        private static Application singleApplicationInstance;
        private MyHistoryOrientedPageSession session;
        private static object syncObj = new object();
        private bool navigatingForward;
        private IPlaybackController currentPlaybackController = null;
        private static string _background;
        //tracks whether to show recently added or watched items
        public string RecentItemOption { get { return Config.Instance.RecentItemOption; } set { Config.Instance.RecentItemOption = value; } }
        private bool pluginUpdatesAvailable = false;

        public bool PluginUpdatesAvailable
        {
            get
            {
                return pluginUpdatesAvailable;
            }
            set
            {
                pluginUpdatesAvailable = value;
                FirePropertyChanged("PluginUpdatesAvailable");
            }
        }

        public List<string> ConfigPanelNames
        {
            get
            {
                return Kernel.Instance.ConfigPanels.Keys.ToList();
            }
        }

        public string ConfigPanel(string name)
        {
            if (Kernel.Instance.ConfigPanels.ContainsKey(name))
            {
                return Kernel.Instance.ConfigPanels[name].Resource;
            }
            else
            {
                return "me:AddinPanel"; //return the embedded empty UI if not found
            }
        }

        public Choice ConfigModel { get; set; }

        public string CurrentConfigPanel
        {
            get
            {
                return Kernel.Instance.ConfigPanels[ConfigModel.Chosen.ToString()].Resource;
            }
        }

        public ModelItem CurrentConfigObject
        {
            get
            {
                if (Kernel.Instance.ConfigPanels[ConfigModel.Chosen.ToString()] != null)
                {
                    return Kernel.Instance.ConfigPanels[ConfigModel.Chosen.ToString()].ConfigObject;
                }
                else return null;
            }
        }

        public Dictionary<string, ViewTheme> AvailableThemes { get { return Kernel.Instance.AvailableThemes; } }

        public List<string> AvailableThemeNames
        {
            get
            {
                return AvailableThemes.Keys.ToList();
            }
        }

        public ViewTheme CurrentTheme
        {
            get
            {
                if (AvailableThemes.ContainsKey(Config.Instance.ViewTheme))
                {
                    return AvailableThemes[Config.Instance.ViewTheme];
                }
                else
                { //old or bogus theme - return default so we don't crash
                    //and set the config so config page doesn't crash
                    Config.Instance.ViewTheme = "Default";
                    return AvailableThemes["Default"];
                }
            }
        }

        private Item currentItem;

        public Item CurrentItem
        {
            get
            {
                if (currentItem != null)
                {
                    return currentItem;
                }
                else
                {
                    if (Application.CurrentInstance.CurrentFolder.SelectedChild != null)
                    {
                        return Application.CurrentInstance.CurrentFolder.SelectedChild;
                    }
                    else return Item.BlankItem;
                }
            }
            set
            {
                if (currentItem != value)
                {
                    currentItem = value;
                    CurrentItemChanged();
                }
            }
        }

        public void CurrentItemChanged()
        {
            FirePropertyChanged("CurrentItem");
        }

        public List<MenuItem> ContextMenu
        {
            get
            {
                return Kernel.Instance.ContextMenuItems;
            }
        }

        public List<MenuItem> PlayMenu
        {
            get
            {
                return Kernel.Instance.PlayMenuItems;
            }
        }

        public List<MenuItem> DetailMenu
        {
            get
            {
                return Kernel.Instance.DetailMenuItems;
            }
        }

        private MenuManager menuManager;

        public bool NavigatingForward
        {
            get { return navigatingForward; }
            set { navigatingForward = value; }
        }


        private string entryPointPath = string.Empty;

        public string EntryPointPath
        {
            get
            {
                return this.entryPointPath.ToLower();
            }
        }

        public const string CONFIG_ENTRY_POINT = "configmb";

        public string ConfigEntryPointVal
        {
            get
            {
                return CONFIG_ENTRY_POINT.ToLower();
            }
        }

        static Application()
        {

        }

        public Application()
            : this(null, null)
        {

        }

        public Application(MyHistoryOrientedPageSession session, Microsoft.MediaCenter.Hosting.AddInHost host)
        {

            this.session = session;
            if (session != null)
            {
                this.session.Application = this;
            }
            singleApplicationInstance = this;
            //wire up our mouseActiveHooker if enabled so we can know if the mouse is active over us
            if (Config.Instance.EnableMouseHook)
            {
                Kernel.Instance.MouseActiveHooker.MouseActive += new IsMouseActiveHooker.MouseActiveHandler(mouseActiveHooker_MouseActive);
            }
            //populate the config model choice
            ConfigModel = new Choice();
            ConfigModel.Options = ConfigPanelNames;

            //initialize our menu manager
            menuManager = new MenuManager();

        }


        /// <summary>
        /// This is an oddity under TVPack, sometimes the MediaCenterEnvironemt and MediaExperience objects go bad and become
        /// disconnected from their host in the main application. Typically this is after 5 minutes of leaving the application idle (but noot always).
        /// What is odd is that using reflection under these circumstances seems to work - even though it is only doing the same as Reflector shoulds the real 
        /// methods do. As I said it's odd but this at least lets us get a warning on the screen before the application crashes out!
        /// </summary>
        /// <param name="message"></param>
        public static void DialogBoxViaReflection(string message)
        {
            MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            FieldInfo fi = ev.GetType().GetField("_legacyAddInHost", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
            if (fi != null)
            {
                AddInHost2 ah2 = (AddInHost2)fi.GetValue(ev);
                if (ah2 != null)
                {
                    Type t = ah2.GetType();
                    PropertyInfo pi = t.GetProperty("HostControl", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public);
                    if (pi != null)
                    {
                        HostControl hc = (HostControl)pi.GetValue(ah2, null);
                        hc.Dialog(message, "Media Browser", 1, 120, true);
                    }
                }
            }
        }

        public static bool RunningOnExtender
        {
            get
            {
                try
                {
                    bool isLocal = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Capabilities.ContainsKey("Console") &&
                             (bool)Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Capabilities["Console"];
                    return !isLocal;
                }
                catch (Exception ex)
                {
                    Logger.ReportException("Error in RunningOnExtender.", ex);
                    Application.ReportBrokenEnvironment();
                    throw;
                }
            }
        }

        /// <summary>
        /// Unfortunately TVPack has some issues at the moment where the MedaCenterEnvironment stops working, we catch these errors and rport them then close.
        /// In the future this method and all references should be able to be removed, once MS fix the bugs
        /// </summary>
        internal static void ReportBrokenEnvironment()
        {
            Logger.ReportInfo("Application has broken MediaCenterEnvironment, possibly due to 5 minutes of idle while running under system with TVPack installed.\n Application will now close.");
            Logger.ReportInfo("Attempting to use reflection that sometimes works to show a dialog box");
            // for some reason using reflection still works
            Application.DialogBoxViaReflection(CurrentInstance.StringData("BrokenEnvironmentDial"));
            Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();
        }

        public void FixRepeatRate(object scroller, int val)
        {
            try
            {
                PropertyInfo pi = scroller.GetType().GetProperty("View", BindingFlags.Public | BindingFlags.Instance);
                object view = pi.GetValue(scroller, null);
                pi = view.GetType().GetProperty("Control", BindingFlags.Public | BindingFlags.Instance);
                object control = pi.GetValue(view, null);

                pi = control.GetType().GetProperty("KeyRepeatThreshold", BindingFlags.NonPublic | BindingFlags.Instance);
                pi.SetValue(control, (UInt32)val, null);
            }
            catch
            {
                // thats it, I give up, Microsoft went and changed interfaces internally 
            }

        }

        public static MediaCenterEnvironment MediaCenterEnvironment
        {
            get
            {
                return Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            }
        }

        public IPlaybackController PlaybackController
        {
            get
            {
                if (currentPlaybackController != null)
                    return currentPlaybackController;
                return Kernel.Instance.PlaybackControllers[0];
            }
        }


        public AggregateFolder RootFolder
        {
            get
            {
                return Kernel.Instance.RootFolder;
            }
        }

        public void Close()
        {
            Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();
        }

        public void BackOut()
        {
            //back up and close the app if that fails
            if (!session.BackPage())
                Close();
        }

        public void Back()
        {
            session.BackPage();
        }

        public void FinishInitialConfig()
        {
            MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            ev.Dialog(CurrentInstance.StringData("InitialConfigDial"), CurrentInstance.StringData("Restart"), DialogButtons.Ok, 60, true);
            Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();

        }

        public void DeleteMediaItem(Item Item)
        {
            // Setup variables
            MediaCenterEnvironment mce = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            var msg = CurrentInstance.StringData("DeleteMediaDial");
            var caption = CurrentInstance.StringData("DeleteMediaCapDial");

            // Present dialog
            DialogResult dr = mce.Dialog(msg, caption, DialogButtons.No | DialogButtons.Yes, 0, true);

            if (dr == DialogResult.No)
            {
                mce.Dialog(CurrentInstance.StringData("NotDeletedDial"), CurrentInstance.StringData("NotDeletedCapDial"), DialogButtons.Ok, 0, true);
                return;
            }

            if (dr == DialogResult.Yes && this.Config.Advanced_EnableDelete == true
                && this.Config.EnableAdvancedCmds == true)
            {
                Item parent = Item.PhysicalParent;
                string path = Item.Path;
                string name = Item.Name;

                try
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                    }
                    else if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch (IOException)
                {
                    mce.Dialog(CurrentInstance.StringData("NotDelInvalidPathDial"), CurrentInstance.StringData("DelFailedDial"), DialogButtons.Ok, 0, true);
                }
                catch (Exception)
                {
                    mce.Dialog(CurrentInstance.StringData("NotDelUnknownDial"), CurrentInstance.StringData("DelFailedDial"), DialogButtons.Ok, 0, true);
                }
                DeleteNavigationHelper(parent);
                this.Information.AddInformation(new InfomationItem("Deleted media item: " + name, 2));
                this.Information.AddInformation(new InfomationItem("Deleted path: " + path, 2));
            }
            else
                mce.Dialog(CurrentInstance.StringData("NotDelTypeDial"), CurrentInstance.StringData("DelFailedDial"), DialogButtons.Ok, 0, true);
        }


        private void DeleteNavigationHelper(Item Parent)
        {
            Back(); // Back to the Parent Item; This parent still contains old data.

            // These tricks are required in order to load the parent item with "fresh" data.
            if (session.BackPage()) // Double Back to the GrandParent because history still has old parent.
            {
                Navigate(Parent);  // Navigate forward to Parent 
            }
            else // No GrandParent to go back to.
            {
                Navigate(Parent); // Navigate to the parent again - this will refresh the objects
                session.BackPage(); // Now safe to go back to previous parent, and keep session history valid
            }
        }

        // Entry point for the app
        public void GoToMenu()
        {
            try
            {
                if (Config.IsFirstRun)
                {
                    OpenConfiguration(false);
                    MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
                    ev.Dialog(CurrentInstance.StringData("FirstTimeDial"), CurrentInstance.StringData("FirstTimeCapDial"), DialogButtons.Ok, 60, true);
                }
                else
                {
                    // We check config here instead of in the Updater class because the Config class 
                    // CANNOT be instantiated outside of the application thread.
                    if (Config.EnableUpdates)
                    {
                        Updater update = new Updater(this);
                        Async.Queue(Async.STARTUP_QUEUE, () =>
                        {
                            update.CheckForUpdate();
                        }, 40000);
                        Async.Queue(Async.STARTUP_QUEUE, () =>
                        {
                            PluginUpdatesAvailable = update.PluginUpdatesAvailable();
                        }, 60000);
                    }

                    Async.Queue("Full Refresh", () =>
                    {
                        using (new Profiler(CurrentInstance.StringData("FullRefreshProf")))
                        {
                            try
                            {
                               
                                if (!IsInEntryPoint)
                                {
                                    // only refresh for the root entry, this will help speed things up
                                    FullRefresh(this.RootFolder);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.ReportException("Failed to refresh library! ", ex);
                                Debug.Assert(false, "Full refresh thread should never crash!");
                            }
                        }
                    }, 20 * 1000);

                    //check for alternate entry point
                    this.entryPointPath = EntryPointResolver.EntryPointPath;

                    if (IsInEntryPoint)
                    {
                        //add in a fake breadcrumb so they will show properly
                        session.AddBreadcrumb("DIRECTENTRY");
                    }

                    if (this.EntryPointPath.ToLower() == ConfigEntryPointVal) //specialized case for config page
                    {
                        //OpenFolderPage((MediaBrowser.Library.FolderModel)ItemFactory.Instance.Create(this.RootFolder));
                        OpenConfiguration(true);
                    }
                    else
                    {
                        try
                        {
                            this.RootFolderModel = (MediaBrowser.Library.FolderModel)ItemFactory.Instance.Create(EntryPointResolver.EntryPoint(this.EntryPointPath));
                            Navigate(this.RootFolderModel);
                        }
                        catch (Exception ex)
                        {
                            Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(CurrentInstance.StringData("EntryPointErrorDial") + this.EntryPointPath + ". " + ex.ToString() + " " + ex.StackTrace.ToString(), CurrentInstance.StringData("EntryPointErrorCapDial"), DialogButtons.Ok, 30, true);
                            Close();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(CurrentInstance.StringData("CriticalErrorDial") + e.ToString() + " " + e.StackTrace.ToString(), CurrentInstance.StringData("CriticalErrorCapDial"), DialogButtons.Ok, 60, true);
                Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();
            }
        }


        bool IsInEntryPoint
        {
            get
            {
                return !String.IsNullOrEmpty(this.EntryPointPath);
            }
        }

        void FullRefresh(Folder folder)
        {
            folder.RefreshMetadata();

            using (new Profiler(CurrentInstance.StringData("FullValidationProf")))
            {
                RunActionRecursively(folder, item =>
                {
                    Folder f = item as Folder;
                    if (f != null) f.ValidateChildren();
                });
            }

            using (new Profiler(CurrentInstance.StringData("FastRefreshProf")))
            {
                RunActionRecursively(folder, item => item.RefreshMetadata(MetadataRefreshOptions.FastOnly));
            }

            using (new Profiler(CurrentInstance.StringData("SlowRefresh")))
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

        Boolean PlayStartupAnimation = true;

        public Boolean CanPlayStartup()
        {
            if (PlayStartupAnimation)
            {
                PlayStartupAnimation = false;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool displayPopupPlay = false;
        public bool DisplayPopupPlay
        {
            get
            {
                return this.displayPopupPlay;
            }
            set
            {
                this.displayPopupPlay = value;
                FirePropertyChanged("DisplayPopupPlay");
            }
        }

        private bool showNowPlaying = true;
        public bool ShowNowPlaying
        {
            get { return this.showNowPlaying && (MediaCenterEnvironment.MediaExperience != null); }
            set { }
        }


        public string NowPlayingText
        {
            get
            {
                string showName = "";
                try
                {

                    string name = null;

                    // the API works in win7 and is borked on Vista.
                    if (MediaCenterEnvironment.MediaExperience.MediaMetadata.ContainsKey("Name"))
                    {
                        name = MediaCenterEnvironment.MediaExperience.MediaMetadata["Name"] as string;
                        if (name != null && name.Contains(".wpl"))
                        {
                            int start = name.LastIndexOf('/') + 1;
                            if (start < 0) start = 0;
                            int finish = name.LastIndexOf(".wpl");
                            name = name.Substring(start, finish - start);
                        }
                        else
                        {
                            if (name.StartsWith("dvd"))
                            {
                                int start = name.LastIndexOf('/') + 1;
                                if (start < 0) start = 0;
                                name = name.Substring(start);
                            }
                            else
                            {
                                name = null;
                            }
                        }
                    }

                    showName = name ?? MediaCenterEnvironment.MediaExperience.MediaMetadata["Title"] as string;

                    // playlist fix {filename without extension)({playlist name})
                    int lastParan = showName.LastIndexOf('(');
                    if (lastParan > 0)
                    {
                        showName = showName.Substring(lastParan + 1, showName.Length - (lastParan + 2)).Trim();
                    }

                }
                catch (Exception e)
                {
                    showName = "Unknown";
                    Logger.ReportException("Something strange happend while getting media name, please report to community.mediabrowser.tv", e);
                    // never crash here

                }
                return showName;
            }
        }

        private Boolean isMouseActive = false;
        public Boolean IsMouseActive
        {
            get { return isMouseActive; }
            set
            {
                if (isMouseActive != value)
                {
                    isMouseActive = value;
                    FirePropertyChanged("IsMouseActive");
                }
            }
        }

        void mouseActiveHooker_MouseActive(IsMouseActiveHooker m, MouseActiveEventArgs e)
        {
            this.IsMouseActive = e.MouseActive;
        }

        public string BreadCrumbs
        {
            get
            {
                return session.Breadcrumbs;
            }
        }

        public void ClearCache()
        {
            MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            DialogResult r = ev.Dialog(CurrentInstance.StringData("ClearCacheDial"), CurrentInstance.StringData("ClearCacheCapDial"), DialogButtons.Yes | DialogButtons.No, 60, true);
            if (r == DialogResult.Yes)
            {
                bool ok = Kernel.Instance.ItemRepository.ClearEntireCache();
                if (!ok)
                {
                    ev.Dialog(string.Format(CurrentInstance.StringData("ClearCacheErrorDial"), ApplicationPaths.AppCachePath), CurrentInstance.StringData("ErrorDial"), DialogButtons.Ok, 60, true);
                }
                else
                {
                    ev.Dialog(CurrentInstance.StringData("RestartMBDial"), CurrentInstance.StringData("CacheClearedDial"), DialogButtons.Ok, 60, true);
                }
                Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();
            }
        }

        public void ResetConfig()
        {
            MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
            DialogResult r = ev.Dialog(CurrentInstance.StringData("ResetConfigDial"), CurrentInstance.StringData("ResetConfigCapDial"), DialogButtons.Yes | DialogButtons.No, 60, true);
            if (r == DialogResult.Yes)
            {
                Config.Instance.Reset();
                ev.Dialog(CurrentInstance.StringData("RestartMBDial"), CurrentInstance.StringData("ConfigResetDial"), DialogButtons.Ok, 60, true);
                Microsoft.MediaCenter.Hosting.AddInHost.Current.ApplicationContext.CloseApplication();
            }
        }

        public void OpenConfiguration(bool showFullOptions)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties["Application"] = this;
            properties["ShowFull"] = showFullOptions;

            if (session != null)
            {
                session.GoToPage("resx://MediaBrowser/MediaBrowser.Resources/ConfigPage", properties);
            }
            else
            {
                Logger.ReportError("Session is null in OpenPage");
            }
        }


        // accessed from Item
        internal void OpenExternalPlaybackPage(Item item)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties["Application"] = this;
            properties["Item"] = item;

            if (session != null)
            {
                session.GoToPage("resx://MediaBrowser/MediaBrowser.Resources/ExternalPlayback", properties);
            }
            else
            {
                Logger.ReportError("Session is null in OpenExternalPlaybackPage");
            }
        }

        public FolderModel CurrentFolder; //used to keep track of the current folder so we can update the UI if needed
        public FolderModel RootFolderModel; //used to keep track of root folder as foldermodel for same reason

        private void OpenFolderPage(FolderModel folder)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties["Application"] = this;
            properties["Folder"] = folder;
            properties["ThemeConfig"] = CurrentTheme.Config;
            CurrentFolder = folder; //store our current folder
            CurrentItem = null; //blank this out in case it was messed with in the last screen
            if (folder.IsRoot)
                RootFolderModel = folder; //store the root as well

            if (session != null)
            {
                folder.NavigatingInto();
                session.GoToPage(CurrentTheme.FolderPage, properties);
            }
            else
            {
                Logger.ReportError("Session is null in OpenPage");
            }
        }


        private Folder GetStartingFolder(BaseItem item)
        {
            Index currentIndex = item as Index;
            return currentIndex ?? (Folder)RootFolder;
        }

        void NavigateToActor(Item item)
        {
            var person = item.BaseItem as Person;
            Folder searchStart = GetStartingFolder(item.BaseItem.Parent);

            var index = searchStart.Search(
                ShowFinder(show => show.Actors == null ? false :
                    show.Actors.Exists(a => a.Name == person.Name)),
                    person.Name);

            index.Name = item.Name;

            Navigate(ItemFactory.Instance.Create(index));
        }


        public void NavigateToGenre(string genre, Item currentMovie)
        {
            var searchStart = GetStartingFolder(currentMovie.BaseItem.Parent);

            var index = searchStart.Search(
                ShowFinder(show => show.Genres == null ? false : show.Genres.Contains(genre)),
                genre);

            index.Name = genre;

            Navigate(ItemFactory.Instance.Create(index));
        }


        public void NavigateToDirector(string director, Item currentMovie)
        {

            var searchStart = GetStartingFolder(currentMovie.BaseItem.Parent);

            var index = searchStart.Search(
                ShowFinder(show => show.Directors == null ? false : show.Directors.Contains(director)),
                director);

            index.Name = director;

            Navigate(ItemFactory.Instance.Create(index));
        }



        Func<BaseItem, bool> ShowFinder(Func<IShow, bool> func)
        {
            return i => i is IShow ? func((i as IShow)) : false;
        }

        public void Navigate(Item item)
        {
            if (item.BaseItem is Person)
            {
                NavigateToActor(item);
                return;
            }

            if (item.BaseItem is Show)
            {
                if ((item.HasDataForDetailPage && item.BaseItem is Movie) ||
                    this.Config.AlwaysShowDetailsPage)
                {
                    // go to details screen 
                    Dictionary<string, object> properties = new Dictionary<string, object>();
                    properties["Application"] = this;
                    properties["Item"] = item;
                    properties["ThemeConfig"] = CurrentTheme.Config;
                    session.GoToPage(CurrentTheme.DetailPage, properties);
                    return;
                }
            }


            MediaBrowser.Library.FolderModel folder = item as MediaBrowser.Library.FolderModel;
            if (folder != null)
            {
                if (!Config.Instance.RememberIndexing)
                {
                    folder.DisplayPrefs.IndexBy = IndexType.None;
                }
                if (Config.Instance.AutoEnterSingleDirs && (folder.Folder.Children.Count == 1))
                {
                    if (folder.IsRoot) //special breadcrumb if we are going from a single item root
                        session.AddBreadcrumb("DIRECTENTRY");
                    else
                        session.AddBreadcrumb(folder.Name);
                    Navigate(folder.Children[0]);
                }
                else
                {
                    //call secured method if folder is protected
                    if (!folder.ParentalAllowed)
                        NavigateSecure(folder);
                    else
                        OpenFolderPage(folder);
                }
            }
            else
            {
                currentPlaybackController = item.PlaybackController;
                item.Resume();
            }
        }


        public void NavigateSecure(FolderModel folder)
        {
            //just call method on parentalControls - it will callback if secure
            Kernel.Instance.ParentalControls.NavigateProtected(folder);
        }

        public void OpenSecure(FolderModel folder)
        {
            //called if passed security
            OpenFolderPage(folder);
        }

        public void OpenSecurityPage(object prompt)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties["Application"] = this;
            properties["PromptString"] = prompt;
            this.RequestingPIN = true; //tell page we are calling it (not a back action)
            session.GoToPage("resx://MediaBrowser/MediaBrowser.Resources/ParentalPINEntry", properties);
        }

        public void OpenMCMLPage(string page, Dictionary<string, object> properties)
        {
            session.GoToPage(page, properties);
        }

        public void Shuffle(Item item)
        {
            Folder folder = item.BaseItem as Folder;
            if (folder != null)
            {
                if (folder.ParentalAllowed)
                {
                    ShuffleSecure(item);
                }
                else // need to prompt for a PIN - this routine will call back if pin is correct
                {
                    this.DisplayPopupPlay = false; //PIN screen mucks with turning this off
                    Kernel.Instance.ParentalControls.ShuffleProtected(item);
                }
            }
        }

        public void ShuffleSecure(Item item)
        {
            Folder folder = item.BaseItem as Folder;
            if (folder != null)
            {
                Random rnd = new Random();
                PlayableItem playable;

                var playableChildren = folder.RecursiveChildren.Select(i => i as Media).Where(v => v != null && v.IsPlaylistCapable() && v.ParentalAllowed).OrderBy(i => rnd.Next());
                //if (playableChildren.Count() > 0) //be sure we found something to play
                {
                    playable = new PlayableMediaCollection<Media>(item.Name, playableChildren, folder.HasVideoChildren);
                    playable.QueueItem = false;
                    playable.PlayableItems = playableChildren.Select(i => i.Path);
                    foreach (var controller in Kernel.Instance.PlaybackControllers)
                    {
                        if (controller.CanPlay(playable.PlayableItems))
                        {
                            playable.PlaybackController = controller;
                            break;
                        }
                    }
                    playable.Play(null, false);
                }

            }
        }

        public void Unwatched(Item item)
        {
            Folder folder = item.BaseItem as Folder;
            if (folder != null)
            {
                if (folder.ParentalAllowed)
                {
                    PlayUnwatchedSecure(item);
                }
                else // need to prompt for a PIN - this routine will call back if pin is correct
                {
                    this.DisplayPopupPlay = false; //PIN screen mucks with turning this off
                    Kernel.Instance.ParentalControls.PlayUnwatchedProtected(item);
                }
            }
        }

        public void PlayUnwatchedSecure(Item item)
        {
            Folder folder = item.BaseItem as Folder;

            if (folder != null)
            {
                PlayableItem playable;

                var playableChildren = folder.RecursiveChildren.Select(i => i as Media).Where(v => v != null && v.ParentalAllowed && !v.PlaybackStatus.WasPlayed).OrderBy(v => v.Path);
                if (playableChildren.Count() > 0) //be sure we have something to play
                {
                    playable = new PlayableMediaCollection<Media>(item.Name, playableChildren);
                    playable.Play(null, false);
                }
            }
        }

        public void AddToQueue(Item item)
        {
            Play(item, true);
        }
        public void Play(Item item)
        {
            Play(item, false);
        }

        public void PlayLocalTrailer(Item item)
        {
            PlayLocalTrailer(item, false);
        }

        public void PlayLocalTrailer(Item item, bool fullScreen)
        {
            if (!String.IsNullOrEmpty(item.TrailerPath))
            {
                currentPlaybackController = item.PlaybackController;
                currentPlaybackController.PlayMedia(item.TrailerPath);
                if (fullScreen) currentPlaybackController.GoToFullScreen();
            }
        }

        public void Play(Item item, bool queue)
        {
            if (item.IsPlayable || item.IsFolder)
            {
                currentPlaybackController = item.PlaybackController;

                if (queue)
                    item.Queue();
                else
                    item.Play();
            }
        }

        public void Resume(Item item)
        {
            if (item.IsPlayable)
            {
                currentPlaybackController = item.PlaybackController;
                item.Resume();
            }
        }

        public void UnlockPC()
        {
            Kernel.Instance.ParentalControls.Unlock();
        }
        public void RelockPC()
        {
            Kernel.Instance.ParentalControls.Relock();
        }

        public bool RequestingPIN { get; set; } //used to signal the app that we are asking for PIN entry

        public void EnterNewParentalPIN()
        {
            Kernel.Instance.ParentalControls.EnterNewPIN();
        }
        public string CustomPINEntry { get; set; } //holds the entry for a custom pin (entered by user to compare to pin)

        public void ParentalPINEntered()
        {
            RequestingPIN = false;
            Kernel.Instance.ParentalControls.CustomPINEntered(CustomPINEntry);
        }
        public void BackToRoot()
        {
            //back up the app to the root page - used when library re-locks itself
            while (session.BackPage()) { };
        }

        public string DescString(string name)
        {
            //get the description string for "name" out of our string data object
            //we need to translate the content of our item to the field name so that the
            //description field name can be the same across languages
            string key = Kernel.Instance.StringData.GetKey(name.Trim());
            if (string.IsNullOrEmpty(key))
            {
                //probably a string file that has not been updated
                key = name;
                key = key.Replace(" ", "");
                key = key.Replace("*", "");
                key = key.Replace(")", "");
                key = key.Replace(")", "");
                key = key.Replace("-", "");
            }
            return Kernel.Instance.StringData.GetString(key.Trim() + "Desc");
        }

        public static void DisplayDialog(string message, string caption)
        {
            DisplayDialog(message, caption, DialogButtons.Ok, 10);
        }


        public static DialogResult DisplayDialog(string message, string caption, DialogButtons buttons, int timeout)
        {
            // We won't be able to take this during a page transition.  This is good!
            // Conversly, no new pages can be navigated while this is present.
            lock (syncObj)
            {
                DialogResult result = MediaCenterEnvironment.Dialog(message, caption, buttons, timeout, true);
                return result;
            }
        }

        public string AppVersion
        {
            get { return Kernel.Instance.Version.ToString(); }
        }

        private Information _information = new Information();
        public Information Information
        {
            get
            {
                return _information;
            }
            set
            {
                _information = value;
            }
        }


        public string MainBackdrop
        {
            get
            {
                string pngImage = this.Config.InitialFolder + "\\backdrop.png";
                string jpgImage = this.Config.InitialFolder + "\\backdrop.jpg";

                if (!string.IsNullOrEmpty(_background))
                {
                    return _background;
                }
                else
                {
                    if (File.Exists(pngImage))
                    {
                        _background = "file://" + pngImage;
                        return _background;
                    }
                    else if (File.Exists(jpgImage))
                    {
                        _background = "file://" + jpgImage;
                        return _background;
                    }
                    else
                        return null;
                }
            }
        }
    }
}
