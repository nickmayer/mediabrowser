﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.MediaCenter.UI;
using System.Threading;
using Microsoft.MediaCenter.Hosting;
using System.Diagnostics;
using Microsoft.MediaCenter;
using MediaBrowser.Code.ModelItems;
using MediaBrowser.Library.RemoteControl;
using MediaBrowser.Util;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Threading;
using System.Reflection;


namespace MediaBrowser
{

    public class PlaybackController : BaseModelItem, IPlaybackController
    {

        volatile EventHandler<PlaybackStateEventArgs> progressHandler;
        Thread governatorThread;
        object sync = new object();
        bool terminate = false;

        // dont allow multicast events 
        public event EventHandler<PlaybackStateEventArgs> OnProgress
        {
            add
            {
                progressHandler = value;
            }
            remove
            {
                if (progressHandler == value)
                {
                    progressHandler = null;
                }
            }
        }

        public virtual bool RequiresExternalPage
        {
            get
            {
                return false;
            }
        }

        // Default controller can play everything
        public virtual bool CanPlay(string filename)
        {
            return true;
        }

        // Default controller can play everything
        public virtual bool CanPlay(IEnumerable<string> files)
        {
            return true;
        }

        // commands are not routed in this way ... 
        public virtual void ProcessCommand(RemoteCommand command)
        {
            // dont do anything (only plugins need to handle this)
        }

        public PlaybackController()
        {
            PlayState = MediaTransport == null ? PlayState.Undefined : MediaTransport.PlayState;
            if (PlayState == PlayState.Playing)
            {
                Logger.ReportVerbose("Something already playing on controller creation...");
                OnProgress += new EventHandler<PlaybackStateEventArgs>(ExternalItem_OnProgress);
            }

            governatorThread = new Thread(GovernatorThreadProc);
            governatorThread.IsBackground = true;
            governatorThread.Start();
        }


        bool lastWasDVD = true;
        public virtual void PlayDVD(string path)
        {
            PlayPath(path);
            lastWasDVD = true;
            returnedToApp = false;
        }

        void ExternalItem_OnProgress(object sender, PlaybackStateEventArgs e)
        {
           //do nothing - we are used when something is already playing when we are created
        }

        public virtual void PlayMedia(string path)
        {
            if (lastWasDVD) mediaTransport = null;
            PlayPath(path, MediaType.Video, false);
            lastWasDVD = false;

            // vista bug - stop play stop required so we automate it ...
            var version = System.Environment.OSVersion.Version;
            if (version.Major == 6 && version.Minor == 0 && MediaBrowser.Library.Kernel.Instance.ConfigData.EnableVistaStopPlayStopHack)
            {
                var mce = AddInHost.Current.MediaCenterEnvironment;
                WaitForStream(mce);
                //pause

                Async.Queue("Playback Pauser", () =>
                {
                    Microsoft.MediaCenter.UI.Application.DeferredInvoke(_ =>
                    {
                        mce.MediaExperience.Transport.PlayRate = 1;
                        mce.MediaExperience.Transport.PlayRate = 2;

                    });
                }, 2000);


            }
        }

        public virtual void QueueMedia(IEnumerable<string> paths)
        {
            foreach (string path in paths)
                PlayPath(path, MediaType.Video, true);
        }

        public virtual void PlayMedia(IEnumerable<string> paths)
        {
            foreach (string path in paths)
                PlayPath(path, MediaType.Video, false);
        }

        public virtual void QueueMedia(string path)
        {
            PlayPath(path, MediaType.Video, true);
        }

        public virtual void Seek(long position)
        {
            var mce = AddInHost.Current.MediaCenterEnvironment;
            Logger.ReportVerbose("Trying to seek position :" + new TimeSpan(position).ToString());
            WaitForStream(mce);
            mce.MediaExperience.Transport.Position = new TimeSpan(position);
        }

        private static void WaitForStream(MediaCenterEnvironment mce)
        {
            int i = 0;
            while ((i++ < 15) && (mce.MediaExperience.Transport.PlayState != Microsoft.MediaCenter.PlayState.Playing))
            {
                // settng the position only works once it is playing and on fast multicore machines we can get here too quick!
                Thread.Sleep(100);
            }
        }


        private void PlayPath(string path)
        {
            PlayPath(path, Microsoft.MediaCenter.MediaType.Video, false);
        }

        public void PlayPath(string path, Microsoft.MediaCenter.MediaType asType, bool addToQueue)
        {
            try
            {
                if (!AddInHost.Current.MediaCenterEnvironment.PlayMedia(asType, path, addToQueue))
                {
                    Logger.ReportInfo("PlayMedia returned false");
                }
            }
            catch (Exception ex)
            {
                Logger.ReportException("Playing media failed.", ex);
                Application.ReportBrokenEnvironment();
                return;
            }
        }

        public virtual void GoToFullScreen()
        {
            var mce = AddInHost.Current.MediaCenterEnvironment.MediaExperience;

            // great window 7 has bugs, lets see if we can work around them 
            if (mce == null)
            {
                System.Threading.Thread.Sleep(200);
                mce = AddInHost.Current.MediaCenterEnvironment.MediaExperience;
                if (mce == null)
                {
                    try
                    {
                        var fi = AddInHost.Current.MediaCenterEnvironment.GetType()
                            .GetField("_checkedMediaExperience", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (fi != null)
                        {
                            fi.SetValue(AddInHost.Current.MediaCenterEnvironment, false);
                            mce = AddInHost.Current.MediaCenterEnvironment.MediaExperience;
                        }

                    }
                    catch (Exception e)
                    {
                        // give up ... I do not know what to do 
                        Logger.ReportException("AddInHost.Current.MediaCenterEnvironment.MediaExperience is null", e);
                    }

                }
            }

            if (mce != null)
            {
                Logger.ReportVerbose("Going fullscreen...");
                mce.GoToFullScreen();
            }
            else
            {
                Logger.ReportError("AddInHost.Current.MediaCenterEnvironment.MediaExperience is null, we have no way to go full screen!");



                AddInHost.Current.MediaCenterEnvironment.Dialog(Application.CurrentInstance.StringData("CannotMaximizeDial"), "", Microsoft.MediaCenter.DialogButtons.Ok, 0, true);
            }
        }

        #region Playback status

        public virtual bool IsPlayingVideo
        {
            get { return IsPlaying; }
        }

        public virtual bool IsPlaying
        {
            get { return PlayState == PlayState.Playing; }
        }

        public virtual bool IsStopped
        {
            get { return PlayState == PlayState.Stopped; }
        }

        public virtual bool IsPaused
        {
            get { return PlayState == PlayState.Paused; }
        }

        public PlayState PlayState { get; protected set; }

        #endregion
        const int ForceRefreshMillisecs = 5000;
        private void GovernatorThreadProc()
        {
            try
            {
                while (!terminate)
                {
                    lock (sync)
                    {
                        Monitor.Wait(sync, ForceRefreshMillisecs);
                        if (!MediaBrowser.Library.Kernel.Instance.ConfigData.EnableResumeSupport)
                        {
                            continue;
                        }
                        if (terminate)
                        {
                            break;
                        }
                        if (progressHandler != null)
                        {
                            Microsoft.MediaCenter.UI.Application.DeferredInvoke(_ => AttachAndUpdateStatus());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.ReportException("Governator thread proc died!", e);
            }
        }

        private void AttachAndUpdateStatus()
        {
            try
            {
                var transport = MediaTransport;
                if (transport != null)
                {
                    if (transport.PlayState != PlayState)
                    {
                        ReAttach();
                    }
                    UpdateStatus();
                }
            }
            catch (Exception e)
            {
                // dont crash the background thread 
                Logger.ReportException("FAIL: something is wrong with media experience!", e);
                mediaTransport = null;
            }
        }

        protected MediaExperience MediaExperience
        {
            get
            {
                return AddInHost.Current.MediaCenterEnvironment.MediaExperience;
            }
        }

        private MediaTransport mediaTransport;
        protected MediaTransport MediaTransport
        {
            get
            {
                if (mediaTransport != null) return mediaTransport;
                try
                {
                    MediaExperience experience;

                    experience = AddInHost.Current.MediaCenterEnvironment.MediaExperience;

                    if (experience != null)
                    {
                        mediaTransport = experience.Transport;
                    }
                }
                catch (InvalidOperationException e)
                {
                    // well if we are inactive we are not allowed to get media experience ...
                    Logger.ReportException("EXCEPTION : ", e);
                }
                return mediaTransport;
            }
        }

        protected virtual void ReAttach()
        {
            var transport = MediaTransport;
            if (transport != null)
            {
                transport.PropertyChanged -= new PropertyChangedEventHandler(TransportPropertyChanged);
                transport.PropertyChanged += new PropertyChangedEventHandler(TransportPropertyChanged);
            }
        }

        protected virtual void Detach()
        {
            var transport = MediaTransport;
            if (transport != null)
            {
                transport.PropertyChanged -= new PropertyChangedEventHandler(TransportPropertyChanged);
            }
            progressHandler = null; //also detatch the playble item so we won't keep trying to track progress when stopped
        }

        DateTime lastCall = DateTime.Now;

        void TransportPropertyChanged(IPropertyObject sender, string property)
        {
            // protect against really agressive calls
            var diff = (DateTime.Now - lastCall).TotalMilliseconds;
            // play state is critical otherwise stop hack for win 7 will not work.
            if (diff < 1000 && diff >= 0 && property != "PlayState")
            {
                return;
            }

            //Logger.ReportVerbose("TransportPropertyChanged was called with property = " + property);

            lastCall = DateTime.Now;
            UpdateStatus();
        }


        long position;
        string title;
        long? duration = null;
        double metaDuration = 0;
        public double MetaDuration
        {
            set { metaDuration = value; }
        }

        protected virtual void UpdateStatus()
        {
            var transport = MediaTransport;
            PlayState state = PlayState.Undefined;
            if (transport != null)
            {
                state = transport.PlayState;
                //changed this to get the "Name" property instead.  That makes it compatable with DVD playback as well.
                string title = null;
                try
                {
                    title = MediaExperience.MediaMetadata["Name"] as string;
                    //Logger.ReportVerbose("Full title: " + title);
                    title = title.ToLower(); //lowercase it for comparison
                }
                catch (Exception e)
                {
                    Logger.ReportException("Failed to get name on current media item! Trying Title...", e);
                    try
                    {
                        title = MediaExperience.MediaMetadata["Title"] as string;
                        //Logger.ReportVerbose("Full title: " + title);
                        title = title.ToLower();
                    }
                    catch (Exception ex)
                    {
                        Logger.ReportException("That didn't work either.  Giving up...", ex);
                    }
                }

                if (title != this.title) duration = null;  //changed items we were playing

                long position = transport.Position.Ticks;
                try
                {
                    //only track position for a reasonable portion of the video
                    if (duration == null) //only need to do this once per item
                    {
                        //first try mediacenter
                        duration = (TimeSpan.Parse((string)MediaExperience.MediaMetadata["Duration"])).Ticks;
                        if (duration == 0) //not there - see if we have it from meta
                            duration = (TimeSpan.FromMinutes(metaDuration)).Ticks;
                    }
                    //Logger.ReportVerbose("position "+position+ " duration "+duration);
                    if (duration > 0)
                    {
                        decimal pctIn = Decimal.Divide(position,duration.Value) * 100;
                        //Logger.ReportVerbose("pctIn: " + pctIn + " duration: " + duration);
                        if (pctIn < Config.Instance.MinResumePct || pctIn > Config.Instance.MaxResumePct) position = 0; //don't track in very begginning or very end
                    }
                }
                catch { } // couldn't get duration - no biggie just don't blow chow

                if (title != null && progressHandler != null && (this.title != title || this.position != position) && (duration == 0 || (duration / TimeSpan.TicksPerMinute) >= Config.Instance.MinResumeDuration))
                {

                    //Logger.ReportVerbose("progressHandler was called with : position =" + position.ToString() + " title :" + title);
                    
                    progressHandler(this, new PlaybackStateEventArgs() { Position = position, Title = title });
                    this.title = title;
                    this.position = position;
                }
            }

            if (state != PlayState)
            {
                Logger.ReportVerbose("Playstate changed to "+state+" for " + title);
                PlayState = state;
                Logger.ReportVerbose("Invoking Playstate changed events...");
                Microsoft.MediaCenter.UI.Application.DeferredInvoke(_ => PlayStateChanged());
                Logger.ReportVerbose("Setting now playing status...");
                Application.CurrentInstance.ShowNowPlaying = (
                    (state == Microsoft.MediaCenter.PlayState.Playing) ||
                    (state == Microsoft.MediaCenter.PlayState.Paused));
                Logger.ReportVerbose("Updating Resume status...");
                Application.CurrentInstance.CurrentItem.UpdateResume();
                if (state == Microsoft.MediaCenter.PlayState.Finished || state == Microsoft.MediaCenter.PlayState.Stopped || state == Microsoft.MediaCenter.PlayState.Undefined)
                {
                    Logger.ReportVerbose("Stopped so detaching...");
                    Detach(); //we don't want to continue to get updates if play something outside MB
                    //we're done - call post-processor
                    Logger.ReportVerbose("Calling post play...");
                    Application.CurrentInstance.RunPostPlayProcesses();
                    Logger.ReportVerbose("Finished all playstate events");
                }
            }


            if (IsWindows7)
            {
                if (lastWasDVD && !returnedToApp && state == PlayState.Stopped)
                {
                    Logger.ReportVerbose("Ensuring MB is front-most app");
                    Microsoft.MediaCenter.UI.Application.DeferredInvoke(_ =>
                    {
                        AddInHost.Current.ApplicationContext.ReturnToApplication();
                    });
                    returnedToApp = true;
                }
            }
            //Logger.ReportVerbose("Out of updatestatus");
        }

        static bool? isWindows7;
        private static bool IsWindows7
        {
            get
            {
                if (!isWindows7.HasValue)
                {
                    var version = System.Environment.OSVersion.Version;
                    isWindows7 = version.Major == 6 && version.Minor == 1;
                }
                return isWindows7.Value;
            }
        }

        bool returnedToApp = true;

        protected void PlayStateChanged()
        {
            FirePropertyChanged("PlayState");
            FirePropertyChanged("IsPlaying");
            FirePropertyChanged("IsPlayingVideo");
            FirePropertyChanged("IsStopped");
            FirePropertyChanged("IsPaused");
        }

        public virtual void Pause()
        {
            var transport = MediaTransport;
            if (transport != null)
            {
                transport.PlayRate = 1;
            }
        }


        public virtual void Stop()
        {
            var transport = MediaTransport;
            if (transport != null)
            {
                transport.PlayRate = 0;
            }
        }

        protected override void Dispose(bool isDisposing)
        {

            Logger.ReportVerbose("Playback controller is being disposed");

            if (isDisposing)
            {
                lock (sync)
                {
                    terminate = true;
                    Monitor.Pulse(sync);
                }
            }

            base.Dispose(isDisposing);

        }
    }
}
