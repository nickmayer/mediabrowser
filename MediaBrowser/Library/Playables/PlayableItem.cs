using System;
using MediaBrowser.LibraryManagement;
using System.Diagnostics;
using System.IO;
using MediaBrowser.Library.Entities;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using MediaBrowser.Code.ModelItems;
using MediaBrowser.Library.RemoteControl;
using MediaBrowser.Library.Configuration;
using System.Linq;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Playables;

namespace MediaBrowser.Library
{

    /// <summary>
    /// Encapsulates play back of different types of item. Builds playlists, mounts iso etc. where appropriate
    /// </summary>
    public abstract class PlayableItem
    {

        IPlaybackController playbackController = Application.CurrentInstance.PlaybackController;
        public IPlaybackController PlaybackController
        {
            get
            {
                return playbackController;
            }
            set
            {
                playbackController = value;
            }
        }

        public bool QueueItem { get; set; }
        public IEnumerable<string> PlayableItems { get; set; }

        static Transcoder transcoder;
        private string fileToPlay;
        private string currentTitle;
        private string alternateTitle;
        protected PlaybackStatus PlayState { get; private set; }
        public PlayableItem()
        {
            QueueItem = false;

        }

        public abstract void Prepare(bool resume);
        public abstract string Filename { get; }

        protected bool PlayCountIncremented = false;



        public void Play(PlaybackStatus playstate, bool resume)
        {
            this.PlayState = playstate;
            this.Prepare(resume);
            if (this is PlayableCollection && (this.PlayableItems == null || this.PlayableItems.Count() < 1))
            {
                Microsoft.MediaCenter.MediaCenterEnvironment ev = Microsoft.MediaCenter.Hosting.AddInHost.Current.MediaCenterEnvironment;
                ev.Dialog(Application.CurrentInstance.StringData("NoContentDial"), Application.CurrentInstance.StringData("Playstr"), Microsoft.MediaCenter.DialogButtons.Ok, 500, true);
            }
            else
            {
                PlayInternal(resume);
            }
        }

        protected virtual void PlayInternal(bool resume)
        {
            try
            {
                PlayCountIncremented = false; //reset

                if (!RunningOnExtender || !Config.Instance.EnableTranscode360 || Helper.IsExtenderNativeVideo(this.Filename))
                    PlayAndGoFullScreen(this.Filename);
                else
                {
                    // if we are on an extender, we need to start up our transcoder
                    try
                    {
                        PlayFileWithTranscode(this.Filename);
                    }
                    catch
                    {
                        // in case t360 is not installed - we may get an assembly loading failure 
                        PlayAndGoFullScreen(this.Filename);
                    }
                }

                if (resume)
                {
                    PlaybackController.Seek(PlayState.PositionTicks);
                }
                else
                {
                    if (this is PlayableDvd)
                    {
                        PlaybackController.Seek(0); //force DVD to start at beginning (player will try to auto-resume)
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.ReportException("Failed to play " + this.Filename, ex);
            }
        }


        private void PlayFileWithTranscode(string filename)
        {
            if (transcoder == null)
                transcoder = new Transcoder();

            string bufferpath = transcoder.BeginTranscode(this.Filename);

            // if bufferpath comes back null, that means the transcoder i) failed to start or ii) they
            // don't even have it installed
            if (bufferpath == null)
            {
                Application.DisplayDialog("Could not start transcoding process", "Transcode Error");
                return;
            }
            PlayAndGoFullScreen(bufferpath);
        }


        private void PlayAndGoFullScreen(string file)
        {
            this.fileToPlay = file;
            this.currentTitle = file.Replace('\\','/').ToLower();
            this.alternateTitle = Path.GetFileNameWithoutExtension(file).ToLower();
            Play(file);
            if (!QueueItem)
                PlaybackController.GoToFullScreen();

            PlaybackController.OnProgress += new EventHandler<PlaybackStateEventArgs>(PlaybackController_OnProgress);
        }

        public virtual void Play(string file)
        {
            Logger.ReportVerbose("About to play : " + file);
            if (QueueItem && this.PlayableItems != null && this.PlayableItems.Count() > 0)
                PlaybackController.QueueMedia(this.PlayableItems);
            else if (QueueItem)
                PlaybackController.QueueMedia(file);
            else
                PlaybackController.PlayMedia(file);
        }

        void PlaybackController_OnProgress(object sender, PlaybackStateEventArgs e)
        {
            if (!UpdatePosition(e.Title, e.Position))
            {
                PlaybackController.OnProgress -= new EventHandler<PlaybackStateEventArgs>(PlaybackController_OnProgress);
            }
        }

        protected void MarkWatched()
        {
            if (PlayState != null)
            {
                PlayState.LastPlayed = DateTime.Now;
                PlayState.PlayCount = PlayState.PlayCount + 1;
                PlayState.Save();
            }
        }

        public virtual bool UpdatePosition(string title, long positionTicks)
        {
            if (PlayState == null)
            {
                return false;
            }

            if (title.EndsWith(currentTitle) || title == alternateTitle)
            {
                if (!PlayCountIncremented && positionTicks > 0) //the first time we are called with a valid position mark us as being watched
                {
                    PlayState.PlayCount++;
                    PlayState.LastPlayed = DateTime.Now;
                    PlayCountIncremented = true;
                }
                //Logger.ReportVerbose("Updating the position for " + title + " position " + positionTicks);
                PlayState.PositionTicks = positionTicks;
                PlayState.Save();
                return true;
            }
            else
            {
                Logger.ReportVerbose("Detaching because title doesn't match.  Current title: " + currentTitle + " Alternate Title: "+alternateTitle);
                return false;
            }
        }

        protected static bool RunningOnExtender
        {
            get
            {
                return Application.RunningOnExtender;
            }
        }


        public static string CreateWPLPlaylist(string name, IEnumerable<string> files)
        {

            // we need to filter out all invalid chars 
            name = new string(name
                .ToCharArray()
                .Where(e => !Path.GetInvalidFileNameChars().Contains(e))
                .ToArray());

            var playListFile = Path.Combine(ApplicationPaths.AutoPlaylistPath, name + ".wpl");


            StringWriter writer = new StringWriter();
            XmlTextWriter xml = new XmlTextWriter(writer);

            xml.Indentation = 2;
            xml.IndentChar = ' ';

            xml.WriteStartElement("smil");
            xml.WriteStartElement("body");
            xml.WriteStartElement("seq");

            foreach (string file in files)
            {
                xml.WriteStartElement("media");
                xml.WriteAttributeString("src", file);
                xml.WriteEndElement();
            }

            xml.WriteEndElement();
            xml.WriteEndElement();
            xml.WriteEndElement();

            System.IO.File.WriteAllText(playListFile, @"<?wpl version=""1.0""?>" + writer.ToString());

            return playListFile;
        }

    }

}
