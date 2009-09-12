using System;
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

namespace MusicPlugin
{
    public class Plugin : BasePlugin
    {

        static readonly Guid MusiciTunesGuid = new Guid("{97581452-7374-11DE-B53C-716855D89593}");
        static readonly Guid MusicNormalGuid = new Guid("{D0DAD4BA-90B8-11DE-9AC9-06AC55D89593}");
        
        public override void Init(Kernel kernel)
        {            
            //AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);


            if (ValidateSettings(kernel.ConfigData.InitialFolder))
            {
                if (Settings.Instance.LoadiTunesLibrary)
                {
                    try
                    {
                        BaseItem itunes;
                        string message = "RefreshiTunesLibrary in the MusicPlugin.xml is set to true, this will force a rebuild of the iTunes Library, continue?";
                        string heading = "Rebuild iTunes Library Cache";

                        if (Settings.Instance.ForceRefreshiTunesLibrary && Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Yes | DialogButtons.No, 60, true) == DialogResult.Yes)
                        {

                            itunes = iTunesLibrary.GetDetailsFromXml(kernel.ItemRepository.RetrieveItem(MusiciTunesGuid) as iTunesMusicLibrary);
                            Settings.Instance.ForceRefreshiTunesLibrary = false;
                            Settings.SaveSettingsFile();

                        }
                        else
                        {
                            itunes = kernel.ItemRepository.RetrieveItem(MusiciTunesGuid) ?? new iTunesLibrary().Library;
                        }
                        if ((itunes as iTunesMusicLibrary).LastUpdate != DateTime.MinValue && (itunes as iTunesMusicLibrary).LastUpdate < new System.IO.FileInfo(Settings.Instance.iTunesLibraryXMLPath).LastWriteTime)
                        {
                            message = "Your iTunes Library might have changed, do you want to rebuild it?";
                            if (Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Yes | DialogButtons.No, 60, true) == DialogResult.Yes)
                                itunes = iTunesLibrary.GetDetailsFromXml(kernel.ItemRepository.RetrieveItem(MusiciTunesGuid) as iTunesMusicLibrary);

                        }

                        itunes.Path = "";
                        itunes.Id = MusiciTunesGuid;
                        itunes.Name = Settings.Instance.iTunesLibraryVirtualFolderName;
                        if (!string.IsNullOrEmpty(Settings.Instance.iTunesLibraryIcon))
                            itunes.PrimaryImagePath = Settings.Instance.iTunesLibraryIcon;

                        kernel.RootFolder.AddVirtualChild(itunes);
                        kernel.ItemRepository.SaveItem(itunes);
                    }
                    catch (Exception ex)
                    {
                        Logger.ReportException("Cannot load iTunes Music Library", ex);
                    }
                }

                if (Settings.Instance.LoadNormalLibrary)
                {
                    BaseItem music;

                    music = kernel.ItemRepository.RetrieveItem(MusicNormalGuid) ?? new MusicPluginFolder();
                    music.Id = MusicNormalGuid;
                    music.Path = Settings.Instance.NormalLibraryPath;
                    music.Name = Settings.Instance.NormalLibraryVirtualFolderName;
                    if (!string.IsNullOrEmpty(Settings.Instance.NormalLibraryIcon))
                        music.PrimaryImagePath = Settings.Instance.NormalLibraryIcon;
                    kernel.RootFolder.AddVirtualChild(music);
                    //kernel.ItemRepository.SaveItem(music);                
                }
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
                Logger.ReportInfo("Music plugin, iTunes nor Normal Music enabled, probably using folder specification (vf files) via configurator.");
                        
        }

        System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("taglib"))
                return Assembly.LoadFrom(@"c:\ProgramData\MediaBrowser\Plugins\taglib\taglib-sharp.dll");
            
            return Assembly.LoadFrom("");
        }

        public override string Name
        {
            get { return "Music and iTunes library."; }
        }

        public override string Description
        {
            get { return "Music and iTunes library plugin for MediaBrowser by Nephelyn."; }
        }

        private bool ValidateSettings(string initialFolder)
        {
            string heading = "MusicPlugin";
            string message;

            try
            {
                Settings.InitSettings(initialFolder);
            }
            catch (Exception e)
            {
                Logger.ReportException("MusicPlugin", e);
                message = "The MusicPlugin could not be loaded. Please enable logging in MediaBrowser and check the log.";
                Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Ok, 60, true);
                return false;
            }

            if (!File.Exists(Settings.SettingPath))
            {
                message = "The MusicPlugin could not create a config file. It will not be loaded.";
                Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Ok, 60, true);
                return false;
            }

            if (Settings.Instance.FirstLoad)
            {
                message = "The MusicPlugin has created its own configuration file, please close MediaBrowser and configure " + Settings.SettingPath+".";
                Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Ok, 60, true);
                Settings.Instance.FirstLoad = false;
                Settings.SaveSettingsFile();
                return false;
            }

            if (Settings.Instance.LoadiTunesLibrary && (string.IsNullOrEmpty(Settings.Instance.iTunesLibraryXMLPath) || !File.Exists(Settings.Instance.iTunesLibraryXMLPath)))
            {
                message = "Your iTunes Library is enabled, but the specified xml path is invalid.";
                Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Ok, 60, true);                
                return false;
            }

            if (Settings.Instance.LoadNormalLibrary && (string.IsNullOrEmpty(Settings.Instance.NormalLibraryPath) || !Directory.Exists(Settings.Instance.NormalLibraryPath)))
            {
                message = "Your Normal Library is enabled, but the specified directory is invalid.";
                Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Ok, 60, true);
                return false;
            }

            if (Settings.Instance.LoadNormalLibrary && Settings.Instance.LoadiTunesLibrary && Settings.Instance.NormalLibraryVirtualFolderName == Settings.Instance.iTunesLibraryVirtualFolderName)
            {
                message = "Your Normal and iTunes Libraries are enabled, but your virtual folders names are the same.";
                Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Ok, 60, true);
                return false;
            }

            if (Settings.Instance.LoadNormalLibrary && Settings.Instance.LoadiTunesLibrary && Settings.Instance.NormalLibraryVirtualFolderName == Settings.Instance.iTunesLibraryVirtualFolderName)
            {
                message = "Your Normal and iTunes Libraries are enabled, but your virtual folders names are the same.";
                Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Ok, 60, true);
                return false;
            }

            if (!string.IsNullOrEmpty(Settings.Instance.iTunesLibraryIcon) && !File.Exists(Settings.Instance.iTunesLibraryIcon))
            {
                message = "Your iTunes Library is enabled, but the specified icon path is invalid.";
                Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Ok, 60, true);
                return false;
            }

            if (!string.IsNullOrEmpty(Settings.Instance.NormalLibraryIcon) && !File.Exists(Settings.Instance.NormalLibraryIcon))
            {
                message = "Your Normal Library is enabled, but the specified icon path is invalid.";
                Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Ok, 60, true);
                return false;
            }

            if (Settings.Instance.ShowPlaylistAsFolder && string.IsNullOrEmpty(Settings.Instance.PlayListFolderName))
            {
                message = "Your playlist folder is enabled, but the specified name is invalid.";
                Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Ok, 60, true);
                return false;
            }

            if (!string.IsNullOrEmpty(Settings.Instance.SongImage) && !File.Exists(Settings.Instance.SongImage))
            {
                message = "The specified song image is invalid.";
                Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Ok, 60, true);
                return false;
            }

            return true;
        }

    }
}
