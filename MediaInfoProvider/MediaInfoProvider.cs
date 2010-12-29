﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using MediaBrowser.LibraryManagement;
using MediaInfoLib;
using System.IO;
using System.Diagnostics;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Providers.Attributes;
using System.Linq;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Providers;
using MediaBrowser.Library.Logging;

namespace MediaInfoProvider
{
    [SlowProvider]
    [SupportedType(typeof(Video))]
    class MediaInfoProvider : BaseMetadataProvider
    {
        [DllImport("kernel32")]
        static extern IntPtr LoadLibrary(string lpFileName);

        private static bool enabled = CheckForLib();

        private static bool Is64Bit {
            get {
                return IntPtr.Size == 8;
            }
        }

        private static bool CheckForLib()
        {
            string path = Path.Combine(ApplicationPaths.AppPluginPath, "mediainfo");
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            string mediaInfoPath = Path.Combine(path, "mediainfo.dll");
            string resourceName = string.Format("MediaInfoProvider.MediaInfo{0}.dll.gz", Is64Bit ? 64 : 32);
            if (!File.Exists(mediaInfoPath))
            {
                Logger.ReportInfo("MediaInfo Provider: MediaInfo.dll doesn't exist. Extracting version " + Plugin.includedMediaInfoDLL);
                LibraryLoader.Extract(resourceName, mediaInfoPath);
            } else {
                FileVersionInfo mediaInfoVersion = FileVersionInfo.GetVersionInfo(mediaInfoPath);
                if (mediaInfoVersion.FileVersion.ToString() != Plugin.includedMediaInfoDLL)
                {
                    Logger.ReportInfo("MediaInfo Provider: currently MediaInfo.dll version " + mediaInfoVersion.FileVersion + " is installed. updating MediaInfo.dll to version " + Plugin.includedMediaInfoDLL);
                    LibraryLoader.Extract(resourceName, mediaInfoPath);
                }
            }

            if (File.Exists(mediaInfoPath)) {
                var handle = LoadLibrary(mediaInfoPath);
                return handle != IntPtr.Zero;
            }
            return false;
        }

        [Persist]
        string filename;

        public override void Fetch()
        {
            Video video = Item as Video;
            if (video == null || !enabled) return;

            if (video.ContainsRippedMedia) return; //can't process rips

            filename = FindVideoFile();
            if (filename != null) {
                video.MediaInfo = Merge(video.MediaInfo, GetMediaInfo(filename));
            }
        }

        private MediaInfoData Merge(MediaInfoData original, MediaInfoData acquired)
        {
            if (original == null) return acquired;
            if (original.AudioBitRate == 0) original.AudioBitRate = acquired.AudioBitRate;
            if (original.AudioChannelCount == "") original.AudioChannelCount = acquired.AudioChannelCount;
            if (original.AudioFormat == "")
            {
                original.AudioProfile = acquired.AudioProfile;
                original.AudioFormat = acquired.AudioFormat;
            }
            if (original.AudioStreamCount == 0) original.AudioStreamCount = acquired.AudioStreamCount;
            if (original.Height == 0) original.Height = acquired.Height;
            if (original.RunTime == 0) original.RunTime = acquired.RunTime;
            if (original.Subtitles == "") original.Subtitles = acquired.Subtitles;
            if (original.VideoBitRate == 0) original.VideoBitRate = acquired.VideoBitRate;
            if (original.VideoCodec == "") original.VideoCodec = acquired.VideoCodec;
            if (original.VideoFPS == "") original.VideoFPS = acquired.VideoFPS;
            if (original.Width == 0) original.Width = acquired.Width;
            return original;
        }

        private string FindVideoFile() {
            return (Item as Video).VideoFiles.First();
        }

        private MediaInfoData GetMediaInfo(string location)
        {
            Logger.ReportInfo("getting media info from " + location);
            MediaInfo mediaInfo = new MediaInfo();
            mediaInfo.Option("ParseSpeed", "0.3");
            int i = mediaInfo.Open(location);
            MediaInfoData mediaInfoData = null;
            if (i != 0)
            {
                int width;
                Int32.TryParse(mediaInfo.Get(StreamKind.Video, 0, "Width"), out width);
                int height;
                Int32.TryParse(mediaInfo.Get(StreamKind.Video, 0, "Height"), out height);
                int videoBitRate;
                Int32.TryParse(mediaInfo.Get(StreamKind.Video, 0, "BitRate"), out videoBitRate);
                int audioBitRate;
                Int32.TryParse(mediaInfo.Get(StreamKind.Audio, 0, "BitRate"), out audioBitRate);
                int runTime;
                Int32.TryParse(mediaInfo.Get(StreamKind.General, 0, "PlayTime"), out runTime);
                int streamCount;
                Int32.TryParse(mediaInfo.Get(StreamKind.Audio, 0, "StreamCount"), out streamCount);               
                string audioChannels = mediaInfo.Get(StreamKind.Audio, 0, "Channel(s)");
                string subtitles = mediaInfo.Get(StreamKind.General, 0, "Text_Language_List");
                string videoFrameRate = mediaInfo.Get(StreamKind.Video, 0, "FrameRate");


                mediaInfoData = new MediaInfoData
                {
                    VideoCodec = mediaInfo.Get(StreamKind.Video, 0, "Codec/String"),
                    VideoBitRate = videoBitRate,
                    //MI.Get(StreamKind.Video, 0, "DisplayAspectRatio")),
                    Height = height,
                    Width = width,
                    //MI.Get(StreamKind.Video, 0, "Duration/String3")),
                    AudioFormat = mediaInfo.Get(StreamKind.Audio, 0, "Format"),
                    AudioBitRate = audioBitRate,
                    RunTime = (runTime/60000),
                    AudioStreamCount = streamCount,
                    AudioChannelCount = audioChannels,
                    AudioProfile = mediaInfo.Get(StreamKind.Audio, 0, "Format_Profile"),
                    VideoFPS = videoFrameRate,
                    Subtitles = subtitles                    
                };
            }
            else
            {
                Logger.ReportInfo("Could not extract media information from " + location);
            }
            mediaInfo.Close();
            return mediaInfoData;
        }

        public override bool NeedsRefresh()
        {
            return enabled && Item is Video && filename != FindVideoFile();
        }

    }
}

#region MediaInfoDll
// MediaInfoDLL - All info about media files, for DLL
// Copyright (C) 2002-2006 Jerome Martinez, Zen@MediaArea.net
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// MediaInfoDLL - All info about media files, for DLL
// Copyright (C) 2002-2006 Jerome Martinez, Zen@MediaArea.net
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//
// Microsoft Visual C# wrapper for MediaInfo Library
// See MediaInfo.h for help
//
// To make it working, you must put MediaInfo.Dll
// in the executable folder
//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++



namespace MediaInfoLib
{
    public enum StreamKind
    {
        General,
        Video,
        Audio,
        Text,
        Chapters,
        Image
    }

    public enum InfoKind
    {
        Name,
        Text,
        Measure,
        Options,
        NameText,
        MeasureText,
        Info,
        HowTo
    }

    public enum InfoOptions
    {
        ShowInInform,
        Support,
        ShowInSupported,
        TypeOfValue
    }

    public enum InfoFileOptions
    {
        FileOption_Nothing = 0x00,
        FileOption_Recursive = 0x01,
        FileOption_CloseAll = 0x02,
        FileOption_Max = 0x04
    };


    public class MediaInfo
    {
        //Import of DLL functions. DO NOT USE until you know what you do (MediaInfo DLL do NOT use CoTaskMemAlloc to allocate memory)  
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfo_New();
        [DllImport("MediaInfo.dll")]
        public static extern void MediaInfo_Delete(IntPtr Handle);
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfo_Open(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string FileName);
        [DllImport("MediaInfo.dll")]
        public static extern void MediaInfo_Close(IntPtr Handle);
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfo_Inform(IntPtr Handle, IntPtr Reserved);
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfo_GetI(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfo_Get(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, [MarshalAs(UnmanagedType.LPWStr)] string Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfo_Option(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string Option, [MarshalAs(UnmanagedType.LPWStr)] string Value);
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfo_State_Get(IntPtr Handle);
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfo_Count_Get(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber);

        //MediaInfo class
        public MediaInfo() { Handle = MediaInfo_New(); }
        ~MediaInfo() { MediaInfo_Delete(Handle); }
        public int Open(String FileName) { return (int)MediaInfo_Open(Handle, FileName); }
        public void Close() { MediaInfo_Close(Handle); }
        public String Inform() { return Marshal.PtrToStringUni(MediaInfo_Inform(Handle, (IntPtr)0)); }
        public String Get(StreamKind StreamKind, int StreamNumber, String Parameter, InfoKind KindOfInfo, InfoKind KindOfSearch) { return Marshal.PtrToStringUni(MediaInfo_Get(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, Parameter, (IntPtr)KindOfInfo, (IntPtr)KindOfSearch)); }
        public String Get(StreamKind StreamKind, int StreamNumber, int Parameter, InfoKind KindOfInfo) { return Marshal.PtrToStringUni(MediaInfo_GetI(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, (IntPtr)Parameter, (IntPtr)KindOfInfo)); }
        public String Option(String Option, String Value) { return Marshal.PtrToStringUni(MediaInfo_Option(Handle, Option, Value)); }
        public int State_Get() { return (int)MediaInfo_State_Get(Handle); }
        public int Count_Get(StreamKind StreamKind, int StreamNumber) { return (int)MediaInfo_Count_Get(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber); }
        private IntPtr Handle;

        //Default values, if you know how to set default values in C#, say me
        public String Get(StreamKind StreamKind, int StreamNumber, String Parameter, InfoKind KindOfInfo) { return Get(StreamKind, StreamNumber, Parameter, KindOfInfo, InfoKind.Name); }
        public String Get(StreamKind StreamKind, int StreamNumber, String Parameter) { return Get(StreamKind, StreamNumber, Parameter, InfoKind.Text, InfoKind.Name); }
        public String Get(StreamKind StreamKind, int StreamNumber, int Parameter) { return Get(StreamKind, StreamNumber, Parameter, InfoKind.Text); }
        public String Option(String Option_) { return Option(Option_, ""); }
        public int Count_Get(StreamKind StreamKind) { return Count_Get(StreamKind, -1); }
    }

    public class MediaInfoList
    {
        //Import of DLL functions. DO NOT USE until you know what you do (MediaInfo DLL do NOT use CoTaskMemAlloc to allocate memory)  
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfoList_New();
        [DllImport("MediaInfo.dll")]
        public static extern void MediaInfoList_Delete(IntPtr Handle);
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfoList_Open(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string FileName, IntPtr Options);
        [DllImport("MediaInfo.dll")]
        public static extern void MediaInfoList_Close(IntPtr Handle, IntPtr FilePos);
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfoList_Inform(IntPtr Handle, IntPtr FilePos, IntPtr Reserved);
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfoList_GetI(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfoList_Get(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber, [MarshalAs(UnmanagedType.LPWStr)] string Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfoList_Option(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string Option, [MarshalAs(UnmanagedType.LPWStr)] string Value);
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfoList_State_Get(IntPtr Handle);
        [DllImport("MediaInfo.dll")]
        public static extern IntPtr MediaInfoList_Count_Get(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber);

        //MediaInfo class
        public MediaInfoList() { Handle = MediaInfoList_New(); }
        ~MediaInfoList() { MediaInfoList_Delete(Handle); }
        public int Open(String FileName, InfoFileOptions Options) { return (int)MediaInfoList_Open(Handle, FileName, (IntPtr)Options); }
        public void Close(int FilePos) { MediaInfoList_Close(Handle, (IntPtr)FilePos); }
        public String Inform(int FilePos) { return Marshal.PtrToStringUni(MediaInfoList_Inform(Handle, (IntPtr)FilePos, (IntPtr)0)); }
        public String Get(int FilePos, StreamKind StreamKind, int StreamNumber, String Parameter, InfoKind KindOfInfo, InfoKind KindOfSearch) { return Marshal.PtrToStringUni(MediaInfoList_Get(Handle, (IntPtr)FilePos, (IntPtr)StreamKind, (IntPtr)StreamNumber, Parameter, (IntPtr)KindOfInfo, (IntPtr)KindOfSearch)); }
        public String Get(int FilePos, StreamKind StreamKind, int StreamNumber, int Parameter, InfoKind KindOfInfo) { return Marshal.PtrToStringUni(MediaInfoList_GetI(Handle, (IntPtr)FilePos, (IntPtr)StreamKind, (IntPtr)StreamNumber, (IntPtr)Parameter, (IntPtr)KindOfInfo)); }
        public String Option(String Option, String Value) { return Marshal.PtrToStringUni(MediaInfoList_Option(Handle, Option, Value)); }
        public int State_Get() { return (int)MediaInfoList_State_Get(Handle); }
        public int Count_Get(int FilePos, StreamKind StreamKind, int StreamNumber) { return (int)MediaInfoList_Count_Get(Handle, (IntPtr)FilePos, (IntPtr)StreamKind, (IntPtr)StreamNumber); }
        private IntPtr Handle;

        //Default values, if you know how to set default values in C#, say me
        public void Open(String FileName) { Open(FileName, 0); }
        public void Close() { Close(-1); }
        public String Get(int FilePos, StreamKind StreamKind, int StreamNumber, String Parameter, InfoKind KindOfInfo) { return Get(FilePos, StreamKind, StreamNumber, Parameter, KindOfInfo, InfoKind.Name); }
        public String Get(int FilePos, StreamKind StreamKind, int StreamNumber, String Parameter) { return Get(FilePos, StreamKind, StreamNumber, Parameter, InfoKind.Text, InfoKind.Name); }
        public String Get(int FilePos, StreamKind StreamKind, int StreamNumber, int Parameter) { return Get(FilePos, StreamKind, StreamNumber, Parameter, InfoKind.Text); }
        public String Option(String Option_) { return Option(Option_, ""); }
        public int Count_Get(int FilePos, StreamKind StreamKind) { return Count_Get(FilePos, StreamKind, -1); }
    }

} //NameSpace

#endregion