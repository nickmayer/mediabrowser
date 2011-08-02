using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Threading;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace MediaBrowser.Library.Playables
{
    public class PlayableExternal : PlayableItem
    {
        //alesbal: begin
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool SetWindowPlacement(IntPtr hWnd,
                           ref WINDOWPLACEMENT lpwndpl);
        private struct POINTAPI
        {
            public int x;
            public int y;
        }

        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINTAPI ptMinPosition;
            public POINTAPI ptMaxPosition;
            public RECT rcNormalPosition;
        }
        //alesbal: end
        
        private static object lck = new object();
        private static Dictionary<MediaType, ConfigData.ExternalPlayer> configuredPlayers = null;
        private string path;
        private TimeSpan duration;
        public PlayableExternal(Media media)
        {
            Video video = (media as Video);
            this.path = video.VideoFiles.ToArray()[0];
            this.duration = TimeSpan.FromMinutes(video.RunningTime ?? 0);
        }

        public PlayableExternal(string path) {
            this.path = path;
        }

        public override void Prepare(bool resume) { }

        public override string Filename
        {
            get { return path; }
        }

        private DateTime startTime;
        protected override void PlayInternal(bool resume)
        {
            
            PlaybackController.Stop(); //stop whatever is playing
            startTime = DateTime.Now; //grab this so we can attempt to determine how long we are playing the item
            //MediaBrowser.Library.Logging.Logger.ReportInfo("Playing external.  Duration: " + duration);
            MediaType type  = MediaTypeResolver.DetermineType(path);
            ConfigData.ExternalPlayer p = configuredPlayers[type];
            string args = string.Format(p.Args, path);
            Process player = Process.Start(p.Command, args);
            Async.Queue("Ext Player Mgmt", () => ManageExtPlayer(player, p.MinimizeMCE, p.ShowSplashScreen));
        }

        private void ManageExtPlayer(Process player, bool minimizeMCE, bool showSplash)
        {

            //minimize MCE if indicated
            IntPtr mceWnd = FindWindow(null, "Windows Media Center");
            WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
            GetWindowPlacement(mceWnd, ref wp);
            if (showSplash)
            {
                //throw up a form to cover the desktop if we minimize and we are in the primary monitor
                if (System.Windows.Forms.Screen.FromHandle(mceWnd).Primary)
                {
                    ExternalSplashForm.Display();
                }
            }
            if (minimizeMCE)
            {
                wp.showCmd = 2; // 1- Normal; 2 - Minimize; 3 - Maximize;
                SetWindowPlacement(mceWnd, ref wp);
            }
            //give the player focus
            Async.Queue("Ext Player Focus",() => GiveFocusToExtPlayer(player));
            //and wait for it to exit
            player.WaitForExit();
            //mark as watched based on if we were running long enough to play the item
            TimeSpan elapsed = DateTime.Now - startTime;
            //MediaBrowser.Library.Logging.Logger.ReportInfo("finished playing.  played for: " + elapsed);
            if (duration == TimeSpan.FromMinutes(0) || elapsed.Ticks >= (duration.Ticks * (double)(Convert.ToDouble(Config.Instance.MaxResumePct)/100)))
            {
                //MediaBrowser.Library.Logging.Logger.ReportInfo("marking watched");
                MarkWatched();
            }
            //now re-store MCE 
            wp.showCmd = 1; // 1- Normal; 2 - Minimize; 3 - Maximize;
            SetWindowPlacement(mceWnd, ref wp);
            ExternalSplashForm.Hide();
            SetForegroundWindow(mceWnd);
            Application.CurrentInstance.RunPostPlayProcesses(); //finally call the post play
        }

        private void GiveFocusToExtPlayer(Process player)
        {
            //set external player to foreground
            player.Refresh();
            player.WaitForInputIdle(5000); //give the external player 5 secs to show up and then minimize MCE
            SetForegroundWindow(player.MainWindowHandle);
        }


        public static bool CanPlay(Media media)
        {
            bool canPlay = false;
            var video = media as Video;

            if (video != null) {
                var files = video.VideoFiles.ToArray();
                if (files.Length == 1) {
                    canPlay = CanPlay(files[0]);
                }
            }
            return canPlay;
        }

        public static bool CanPlay(string path)
        { 
            if (RunningOnExtender)
                return false;
            if (configuredPlayers==null)
                lock(lck)
                    if (configuredPlayers==null)
                        LoadConfig();
            MediaType type = MediaTypeResolver.DetermineType(path);
            if (configuredPlayers.ContainsKey(type))
                return true;
            else
                return false;
        }

        private static void LoadConfig()
        {
 	        configuredPlayers = new Dictionary<MediaType,ConfigData.ExternalPlayer>();
            if (Config.Instance.ExternalPlayers!=null)
                foreach (var x in Config.Instance.ExternalPlayers)
                    configuredPlayers[x.MediaType] = x;
        }
    }
}
