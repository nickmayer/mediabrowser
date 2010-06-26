using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using MediaBrowser.LibraryManagement;
using System.IO;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Filesystem;

namespace MediaBrowser.Library.Playables
{
    class PlayableIso : PlayableItem
    {
        string mountedFilename;
        PlayableExternal playableExternal = null;

        string path; 

        public PlayableIso(Media media)
            : base()
        {
            Video video = media as Video;
            if (video.MediaLocation is IFolderMediaLocation)
            {
                List<string> files = Helper.GetIsoFiles(video.Path);
                if (files.Count > 0)
                    this.path = files[0];
                else
                    throw new NotSupportedException(video.Path + " does not contain any iso files");
            }
            else
                path = video.Path;
        }

        public override void Prepare(bool resume)
        {
            try
            {
                // Create the process start information.
                Process process = new Process();
                //virtualclonedrive
                if (Config.Instance.DaemonToolsLocation.ToLower().EndsWith("vcdmount.exe"))
                    process.StartInfo.Arguments = "-mount \"" + path + "\"";
                //alcohol120 or alcohol52
                else if (Config.Instance.DaemonToolsLocation.ToLower().EndsWith("axcmd.exe"))
                    process.StartInfo.Arguments = Config.Instance.DaemonToolsDrive + ":\\ /M:\"" + path + "\"";
                //deamontools
                else
                    process.StartInfo.Arguments = "-mount 0,\"" + path + "\"";
                process.StartInfo.FileName = Config.Instance.DaemonToolsLocation;
                process.StartInfo.ErrorDialog = false;
                process.StartInfo.CreateNoWindow = true;

                // We wait for exit to ensure the iso is completely loaded.
                process.Start();
                process.WaitForExit();

                // Play the DVD video that was mounted.
                this.mountedFilename = Config.Instance.DaemonToolsDrive + ":\\";
                if (!Config.Instance.UseAutoPlayForIso)
                    if (PlayableExternal.CanPlay(this.mountedFilename))
                        this.playableExternal = new PlayableExternal(this.mountedFilename);
            }
            catch (Exception)
            {
                // Display the error in this case, they might wonder why it didn't work.
                Application.DisplayDialog("ISO Mounter is not correctly configured.", "Could not load ISO");
                throw (new Exception("ISO Mounter is not configured correctly"));
            }
        }

        public override string Filename
        {
            get { return this.mountedFilename; }
        }

        public static bool CanPlay(Media media)
        {
            return (media is Video) && (media as Video).MediaType == MediaType.ISO;
        }

        protected override void PlayInternal(bool resume)
        {
            if (!Config.Instance.UseAutoPlayForIso)
            {
                if (this.playableExternal != null)
                    this.playableExternal.Play(this.PlayState, resume);
                else
                    base.PlayInternal(resume);
            }
        }
    }
}
