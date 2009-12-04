using System;
using System.Collections.Generic;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;
using MediaBrowser.Library.Entities;
using MusicPlugin.Library.EntityDiscovery;
using MediaBrowser.Library.Factories;
using MusicPlugin.Library.Playables;
using MusicPlugin.Library.Entities;
using MusicPlugin.Code.ModelItems;
using System.Reflection;
using MusicPlugin.Library.Helpers;
using MusicPlugin.Util;
using Microsoft.MediaCenter;
using System.IO;
using MusicPlugin.Views;
using MediaBrowser;

namespace MusicPlugin
{
    public class Plugin : BasePlugin
    {        
        static readonly Guid MusiciTunesGuid = new Guid("{97581452-7374-11DE-B53C-716855D89593}");
        static readonly Guid MusicNormalGuid = new Guid("{D0DAD4BA-90B8-11DE-9AC9-06AC55D89593}");
        Kernel _kernel;
        
        public override void Init(Kernel kernel)
        {
            _kernel = kernel;
            //AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            Logger.ReportInfo(string.Format("Tyring to load {0} v{1} loaded by {2}.", Name, LatestVersion.ToString(), AppDomain.CurrentDomain.FriendlyName));
            bool isConfigurator = AppDomain.CurrentDomain.FriendlyName.Contains("Configurator");

            if (Settings.ValidateSettings(kernel.ConfigData.InitialFolder, true))
            {
                if (Settings.Instance.LoadiTunesLibrary)
                {
                    if (Settings.ValidateiTunesLibrary(true))
                    {
                        try
                        {
                            BaseItem itunes;
                            string message = "Refresh iTunes Library is set to true, this will force a rebuild of the iTunes Library, continue?";
                            string heading = "Rebuild iTunes Library Cache";

                            if (Settings.Instance.ForceRefreshiTunesLibrary && (isConfigurator || Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Yes | DialogButtons.No, 60, true) == DialogResult.Yes))
                            {

                                itunes = iTunesLibrary.GetDetailsFromXml(kernel.ItemRepository.RetrieveItem(MusiciTunesGuid) as iTunesMusicLibrary);
                                Settings.Instance.ForceRefreshiTunesLibrary = false;
                                Settings.SaveSettingsFile();

                            }
                            else
                            {
                                itunes = kernel.ItemRepository.RetrieveItem(MusiciTunesGuid) ?? new iTunesLibrary().Library;
                            }
                            if (((iTunesMusicLibrary) itunes).LastUpdate != DateTime.MinValue && (itunes as iTunesMusicLibrary).LastUpdate < new System.IO.FileInfo(Settings.Instance.iTunesLibraryXMLPath).LastWriteTime)
                            {
                                message = "Your iTunes Library might have changed, do you want to rebuild it?";
                                if (isConfigurator || Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Yes | DialogButtons.No, 60, true) == DialogResult.Yes)
                                    itunes = iTunesLibrary.GetDetailsFromXml(kernel.ItemRepository.RetrieveItem(MusiciTunesGuid) as iTunesMusicLibrary);

                            }

                            itunes.Path = "";
                            itunes.Id = MusiciTunesGuid;
                            Logger.ReportInfo("Music iTunes id - " + itunes.Id);
                            itunes.Name = Settings.Instance.iTunesLibraryVirtualFolderName;
                            Logger.ReportInfo("Music iTunes vf name - " + itunes.Name);
                            if (!string.IsNullOrEmpty(Settings.Instance.iTunesLibraryIcon))
                                itunes.PrimaryImagePath = Settings.Instance.iTunesLibraryIcon;

                            kernel.RootFolder.AddVirtualChild(itunes);
                            kernel.ItemRepository.SaveItem(itunes);
                            //add types to supported types
                            kernel.AddExternalPlayableItem(typeof(iTunesSong));
                            kernel.AddExternalPlayableFolder(typeof(iTunesAlbum));

                        }
                        catch (Exception ex)
                        {
                            Logger.ReportException("Cannot load iTunes Music Library", ex);
                        }
                    }
                }

                if (Settings.Instance.LoadNormalLibrary)                                    
                {
                    if (Settings.ValidateNormalLibrary(true))
                    {
                        BaseItem music;

                        music = kernel.ItemRepository.RetrieveItem(MusicNormalGuid) ?? new MusicPluginFolder();
                        music.Id = MusicNormalGuid;
                        Logger.ReportInfo("Music normal id - " + music.Id);
                        music.Path = Settings.Instance.NormalLibraryPath;
                        Logger.ReportInfo("Music normal path - " + music.Path);
                        music.Name = Settings.Instance.NormalLibraryVirtualFolderName;
                        Logger.ReportInfo("Music normal name - " + music.Name);
                        if (!string.IsNullOrEmpty(Settings.Instance.NormalLibraryIcon))
                            music.PrimaryImagePath = Settings.Instance.NormalLibraryIcon;
                        kernel.RootFolder.AddVirtualChild(music);
                        kernel.ItemRepository.SaveItem(music);
                        //add types to supported types
                        kernel.AddExternalPlayableItem(typeof(Song));
                        kernel.AddExternalPlayableFolder(typeof(ArtistAlbum));
                    }
                }
            }

            //add our music specific menu items
            if (!isConfigurator)
            {
                kernel.AddMenuItem(new MenuItem("Queue All", "resx://MediaBrowser/MediaBrowser.Resources/Lines", this.queue, new List<Type>() { typeof(ArtistAlbum) }, new List<MenuType>() { MenuType.Item, MenuType.Play }));
                kernel.AddMenuItem(new MenuItem("Queue", "resx://MediaBrowser/MediaBrowser.Resources/Lines", this.queue, new List<Type>() { typeof(Song) }, new List<MenuType>() { MenuType.Item, MenuType.Play }));
            }

            kernel.EntityResolver.Insert(kernel.EntityResolver.Count - 2, new SongResolver());
            kernel.EntityResolver.Insert(kernel.EntityResolver.Count - 2, new ArtistAlbumResolver());
            //kernel.EntityResolver.Insert(kernel.EntityResolver.Count - 2, new AlbumResolver());
            //kernel.EntityResolver.Insert(kernel.EntityResolver.Count - 2, new ArtistResolver());
            PlayableItemFactory.Instance.PlayableItems.Add(PlayableMusicFile.CanPlay, typeof(PlayableMusicFile));
            PlayableItemFactory.Instance.PlayableItems.Add(PlayableMultiFileMusic.CanPlay, typeof(PlayableMultiFileMusic));
            //kernel.MetadataProviderFactories.Add(new MetadataProviderFactory(typeof(ArtistAlbumProvider)));
            kernel.PlaybackControllers.Insert(0, new PlaybackControllerMusic());
            MediaBrowser.Library.ItemFactory.Instance.AddFactory(MusicFolderModel.IsOne, typeof(MusicFolderModel));

            if (!Settings.Instance.LoadNormalLibrary && !Settings.Instance.LoadiTunesLibrary)
                Logger.ReportInfo("Music plugin, iTunes nor Normal Music enabled, probably using folder specification (vf files) via configurator, PLEASE DO NOT USE VFs USE PLUGIN CONFIGURATOR.");
                        
        }
        
        public override string Name
        {
            get { return "Music and iTunes library."; }
        }

        public override string Description
        {
            get { return "Music and iTunes library plugin for MediaBrowser by Nephelyn."; }
        }
        public override System.Version LatestVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
            set
            {
                //base.LatestVersion = value;
            }
        }

        public override System.Version Version
        {
            get
            {
                return LatestVersion;
            }
        }


        public override bool IsConfigurable
        {
            get
            {
                return true;
            }
        }

        public override void Configure()
        {
            Settings.InitSettings(_kernel.ConfigData.InitialFolder);
            if (ConfigureView.BuildUI(Settings.Instance) == System.Windows.Forms.DialogResult.OK)
            {
                Settings.SaveSettingsFile();
            }
        }

        private void queue(Item item)
        {
            Application.CurrentInstance.AddToQueue(item);
        }
    }
}
