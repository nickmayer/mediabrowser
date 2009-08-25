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

namespace MusicPlugin
{
    public class Plugin : BasePlugin
    {

        static readonly Guid MusiciTunesGuid = new Guid("{97581452-7374-11DE-B53C-716855D89593}");
        static readonly Guid MusicNormalGuid = new Guid("{D0DAD4BA-90B8-11DE-9AC9-06AC55D89593}");
        
        public override void Init(Kernel kernel)
        {            
            //AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            
            Settings.initSettings(kernel.ConfigData.InitialFolder);

            if (Settings.LoadiTunesLibrary)
            {
                try
                {
                    BaseItem itunes;
                    string message = "RefreshiTunesLibrary in the MusicPlugin.xml is set to true, this will force a rebuild of the iTunes Library, continue?";
                    string heading = "Rebuild iTunes Library Cache";

                    if (Settings.RefreshiTunesLibrary && Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Yes | DialogButtons.No, 60, true) == DialogResult.Yes)
                    {
                        
                        itunes = iTunesLibrary.GetDetailsFromXml(kernel.ItemRepository.RetrieveItem(MusiciTunesGuid) as iTunesMusicLibrary);
                        Settings.RefreshiTunesLibrary = false;
                        Settings.saveSettingsFile();
                        
                    }
                    else
                    {
                        itunes = kernel.ItemRepository.RetrieveItem(MusiciTunesGuid) ?? new iTunesLibrary().Library;
                    }
                    if ((itunes as iTunesMusicLibrary).LastUpdate != DateTime.MinValue && (itunes as iTunesMusicLibrary).LastUpdate < new System.IO.FileInfo(Settings.iTunesLibraryXMLPath).LastWriteTime)
                    {
                        message = "Your iTunes Library might have changed, do you want to rebuild it?";
                        if (Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment.Dialog(message, heading, DialogButtons.Yes | DialogButtons.No, 60, true) == DialogResult.Yes)
                            itunes = iTunesLibrary.GetDetailsFromXml(kernel.ItemRepository.RetrieveItem(MusiciTunesGuid) as iTunesMusicLibrary);
 
                    }

                    itunes.Path = "";
                    itunes.Id = MusiciTunesGuid;
                    itunes.Name = Settings.iTunesVirtualFolderName;
                    kernel.RootFolder.AddVirtualChild(itunes);
                    kernel.ItemRepository.SaveItem(itunes);
                }
                catch (Exception ex)
                {
                    Logger.ReportException("Cannot load iTunes Music Library", ex);
                }
            }

            if (Settings.LoadNormalMusicLibrary)
            {
                BaseItem music;

                music = kernel.ItemRepository.RetrieveItem(MusicNormalGuid)??new MusicPluginFolder();
                music.Id = MusicNormalGuid;
                music.Path = Settings.MusicPath;
                music.Name = Settings.MusicMBFolderName;
                
                kernel.RootFolder.AddVirtualChild(music);
                //kernel.ItemRepository.SaveItem(music);                
            }

            //if (Settings.LoadNormalMusicLibrary || Settings.LoadiTunesLibrary)
            //{

                kernel.EntityResolver.Insert(kernel.EntityResolver.Count - 2, new SongResolver());
                kernel.EntityResolver.Insert(kernel.EntityResolver.Count - 2, new ArtistAlbumResolver());
                //kernel.EntityResolver.Insert(kernel.EntityResolver.Count - 2, new AlbumResolver());
                //kernel.EntityResolver.Insert(kernel.EntityResolver.Count - 2, new ArtistResolver());
                PlayableItemFactory.Instance.PlayableItems.Add(PlayableMusicFile.CanPlay, typeof(PlayableMusicFile));
                PlayableItemFactory.Instance.PlayableItems.Add(PlayableMultiFileMusic.CanPlay, typeof(PlayableMultiFileMusic));
                //kernel.MetadataProviderFactories.Add(new MetadataProviderFactory(typeof(ArtistAlbumProvider)));
                kernel.PlaybackControllers.Insert(0, new PlaybackControllerMusic());
                MediaBrowser.Library.ItemFactory.Instance.AddFactory(MusicFolderModel.IsOne, typeof(MusicFolderModel));
            //}
            //else
            if (!Settings.LoadNormalMusicLibrary && !Settings.LoadiTunesLibrary)
                Logger.ReportInfo("Music plugin, iTunes nor Normal Music enabled, probably using folder specification via configurator.");
                        
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
            get { return "Music and iTunes library plugin for MediaBrowser. by Nephelyn"; }
        }

    }
}
