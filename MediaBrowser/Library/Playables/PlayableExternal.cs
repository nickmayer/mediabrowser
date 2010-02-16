using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using MediaBrowser.Library.Entities;
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
        public PlayableExternal(Media media)
        {
            this.path = (media as Video).VideoFiles.ToArray()[0];
        }

        public PlayableExternal(string path) {
            this.path = path;
        }

        public override void Prepare(bool resume) { }

        public override string Filename
        {
            get { return path; }
        }


        protected override void PlayInternal(bool resume)
        {
            if (PlaybackController.IsPlaying) {
                PlaybackController.Pause();
            }
              
            MediaType type  = MediaTypeResolver.DetermineType(path);
            ConfigData.ExternalPlayer p = configuredPlayers[type];
            string args = string.Format(p.Args, path);
            Process player = Process.Start(p.Command, args);
            MarkWatched();
            //alesbal: begin
            ThreadPool.QueueUserWorkItem(new WaitCallback(MinimizeMCE), player);
        }

        private void MinimizeMCE(object player)
        {
            Debug.WriteLine("minimizeMCE and then give focues to external player");
            Process extPlayer = (Process)player;

            //minimize MCE
            IntPtr mceWnd = FindWindow(null, "Windows Media Center");
            WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
            GetWindowPlacement(mceWnd, ref wp);
            wp.showCmd = 2; // 1- Normal; 2 - Minimize; 3 - Maximize;
            SetWindowPlacement(mceWnd, ref wp);

            ThreadPool.QueueUserWorkItem(new WaitCallback(GiveFocusToExtPlayer), player);
            extPlayer.WaitForExit();

            wp.showCmd = 1; // 1- Normal; 2 - Minimize; 3 - Maximize;
            SetWindowPlacement(mceWnd, ref wp);
            SetForegroundWindow(mceWnd);
        }

        private void GiveFocusToExtPlayer(object player)
        {
            //set external player to foreground
            Process extPlayer = (Process)player;
            extPlayer.Refresh();
            extPlayer.WaitForInputIdle(5000); //give the external player 5 secs to show up and then minimize MCE
            SetForegroundWindow(extPlayer.MainWindowHandle);
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
