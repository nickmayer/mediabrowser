using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library;
using MediaBrowser.Library.Entities;
using MusicPlugin.Library.EntityDiscovery;
using MediaBrowser.Library.Factories;
using MusicPlugin.Library.Playables;
using MusicPlugin.Library.Entities;
using MusicPlugin.Code.ModelItems;
using MusicPlugin.Library.Providers;
using System.Reflection;
using System.IO;

namespace MusicPlugin
{
    public class Plugin : BasePlugin
    {

        //static readonly Guid MusicGuid = new Guid("{97581452-7374-11DE-B53C-716855D89593}");
        
        public override void Init(Kernel kernel)
        {
            //AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            kernel.EntityResolver.Insert(kernel.EntityResolver.Count - 2, new SongResolver());
            kernel.EntityResolver.Insert(kernel.EntityResolver.Count - 2, new ArtistAlbumResolver());
            //kernel.EntityResolver.Insert(kernel.EntityResolver.Count - 2, new AlbumResolver());
            //kernel.EntityResolver.Insert(kernel.EntityResolver.Count - 2, new ArtistResolver());
            PlayableItemFactory.Instance.PlayableItems.Add(PlayableMusicFile.CanPlay, typeof(PlayableMusicFile));
            PlayableItemFactory.Instance.PlayableItems.Add(PlayableMultiFileMusic.CanPlay, typeof(PlayableMultiFileMusic));
            //kernel.MetadataProviderFactories.Add(new MetadataProviderFactory(typeof(ArtistAlbumProvider)));
            kernel.PlaybackControllers.Insert(0,new PlaybackControllerMusic());
            MediaBrowser.Library.ItemFactory.Instance.AddFactory(MusicFolderModel.IsOne, typeof(MusicFolderModel));
            
            //var music = kernel.ItemRepository.RetrieveItem(MusicGuid) ?? new MusicPluginFolder();
            //music.Path = "c:\\music"; //get from config
            //music.Id = MusicGuid;
            //kernel.RootFolder.AddVirtualChild(music);
                        
        }

        System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("taglib"))
                return Assembly.LoadFrom(@"c:\ProgramData\MediaBrowser\Plugins\taglib\taglib-sharp.dll");
            
            return Assembly.LoadFrom("");
        }

        public override string Name
        {
            get { return "Music"; }
        }

        public override string Description
        {
            get { return "Music plugin for MediaBrowser."; }
        }

    }
}
