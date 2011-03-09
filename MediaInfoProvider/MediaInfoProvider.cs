using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using MediaBrowser.LibraryManagement;
using MediaInfoLib;
using System.IO;
using System.Diagnostics;
using MediaBrowser.Library;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Providers.Attributes;
using System.Linq;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Providers;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Threading;

namespace MediaInfoProvider
{
    [SlowProvider]
    [SupportedType(typeof(Movie))]
    [SupportedType(typeof(Episode))]
    class MediaInfoProvider : BaseMetadataProvider
    {
        [DllImport("kernel32")]
        static extern IntPtr LoadLibrary(string lpFileName);

        private static bool hasTimedOut = false;

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
            if (video.MediaType == MediaType.Wtv) return; //can't process .WTV files

            if (!video.ContainsRippedMedia || (Kernel.LoadContext == MBLoadContext.Service && Plugin.PluginOptions.Instance.AllowBDRips && (video.MediaType == MediaType.BluRay || video.MediaType == MediaType.DVD)))
            {

                using (new MediaBrowser.Util.Profiler("Media Info extraction"))
                {
                    filename = FindVideoFile();
                    if (Plugin.PluginOptions.Instance.BadFiles.Contains(filename)) {
                        Logger.ReportInfo("Mediainfo not scanning known bad file: "+filename);
                        return;
                    }
                    int timeout = Kernel.LoadContext == MBLoadContext.Core ? 30000 : Plugin.ServiceTimeout; //only allow 30 seconds in core but configurable for service
                    if (filename != null)
                    {
                        try
                        {
                            Async.RunWithTimeout(() => video.MediaInfo = Merge(video.MediaInfo, GetMediaInfo(filename,video.MediaType)), timeout);
                        }
                        catch (TimeoutException)
                        {
                            Logger.ReportError("MediaInfo extraction timed-out (" + timeout + "ms) for " + Item.Name + " file: " + filename);
                            //if this is the first timeout we've had, add this file to the bad file list
                            if (!hasTimedOut)
                            {
                                if (Kernel.LoadContext == MBLoadContext.Service)
                                {
                                    //only blacklist files that hung with the longer timeout
                                    Plugin.PluginOptions.Instance.BadFiles.Add(filename);
                                    Plugin.PluginOptions.Save();
                                }
                                hasTimedOut = true;
                                enabled = false;  //no telling how long the MI dll is going to be hung.  Best to not try it again this session
                            }
                        }
                    }
                    else
                    {
                        Logger.ReportInfo("MediaInfo unable to find file to process for " + Item.Name);
                    }
                }
                if (video.MediaInfo.RunTime > 0) video.RunningTime = video.MediaInfo.RunTime;
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
            if (original.AudioLanguages == "") original.AudioLanguages = acquired.AudioLanguages;
            if (original.Height == 0) original.Height = acquired.Height;
            if (original.RunTime == 0) original.RunTime = acquired.RunTime;			
            if (original.Subtitles == "") original.Subtitles = acquired.Subtitles;
            if (original.VideoBitRate == 0) original.VideoBitRate = acquired.VideoBitRate;
            if (original.VideoCodec == "") original.VideoCodec = acquired.VideoCodec;
            if (original.VideoFPS == "") original.VideoFPS = acquired.VideoFPS;
            if (original.ScanType == "") original.ScanType = acquired.ScanType;
            if (original.Width == 0) original.Width = acquired.Width;
            return original;
        }

        private string FindVideoFile() {
            var video = Item as Video;
            if (video.MediaType == MediaType.BluRay)
            {
                string subDir =  "bdmv\\stream\\";
                string filePattern =  "*.m2ts";
                //find the largest stream file
                DirectoryInfo dirInfo = new DirectoryInfo(Path.Combine(Item.Path, subDir));
                IEnumerable<System.IO.FileInfo> fileList = dirInfo.GetFiles(filePattern, System.IO.SearchOption.AllDirectories);

                FileInfo largestFile =
                    (from file in fileList
                     let len = GetFileLength(file)
                     where len > 0
                     orderby len descending
                     select file)
                    .First();
                return largestFile.FullName;
            }
            else if (video.MediaType == MediaType.DVD)
            {
                //find the IFO with the longest duration
                Int32 longestDuration = 0;
                string longestIFO = null;
                try
                {
                    foreach (var file in Directory.GetFiles(Path.Combine(Item.Path, "video_ts\\"), "*.ifo"))
                    {
                        Int32 duration = GetDuration(file);
                        if (duration > longestDuration)
                        {
                            longestDuration = duration;
                            longestIFO = file;
                        }
                    }
                    return longestIFO;
                }
                catch (Exception e)
                {
                    Logger.ReportException("MediaInfo error dealing with IFO", e);
                    return null;
                }
            }
            else
            {
                return (Item as Video).VideoFiles.First();
            }
        }

        // This method is used to swallow the possible exception
        // that can be raised when accessing the FileInfo.Length property.
        // In this particular case, it is safe to swallow the exception.
        static long GetFileLength(System.IO.FileInfo fi)
        {
            long retval;
            try
            {
                retval = fi.Length;
            }
            catch (System.IO.FileNotFoundException)
            {
                // If a file is no longer present,
                // just add zero bytes to the total.
                retval = 0;
            }
            return retval;
        }

        private Int32 GetDuration(string location)
        {
            MediaInfo mediaInfo = new MediaInfo();
            mediaInfo.Option("ParseSpeed", "0.3");
            int i = mediaInfo.Open(location);
            int runTime = 0;
            if (i != 0)
            {
                Int32.TryParse(mediaInfo.Get(StreamKind.General, 0, "PlayTime"), out runTime);
            }
            mediaInfo.Close();
            return runTime;
        }


    private MediaInfoData GetMediaInfo(string location, MediaType mediaType)
        {
            Logger.ReportInfo("Getting media info from " + location);
            MediaInfo mediaInfo = new MediaInfo();
            mediaInfo.Option("ParseSpeed", "0.2");
            int i = mediaInfo.Open(location);
            MediaInfoData mediaInfoData = null;
            if (i != 0)
            {
                string subtitles = mediaInfo.Get(StreamKind.General, 0, "Text_Language_List");
                string scanType = mediaInfo.Get(StreamKind.Video, 0, "ScanType");
                int width;
                Int32.TryParse(mediaInfo.Get(StreamKind.Video, 0, "Width"), out width);
                int height;
                Int32.TryParse(mediaInfo.Get(StreamKind.Video, 0, "Height"), out height);
                int videoBitRate;
                Int32.TryParse(mediaInfo.Get(StreamKind.Video, 0, "BitRate"), out videoBitRate);
               
                int audioBitRate;
                string aBitRate = mediaInfo.Get(StreamKind.Audio, 0, "BitRate");
                int ABindex = aBitRate.IndexOf(" /");
                if (ABindex > 0)
                    aBitRate = aBitRate.Remove(ABindex);
                Int32.TryParse(aBitRate, out audioBitRate);

                int runTime;
                Int32.TryParse(mediaInfo.Get(StreamKind.General, 0, "PlayTime"), out runTime);
                int streamCount;
                Int32.TryParse(mediaInfo.Get(StreamKind.Audio, 0, "StreamCount"), out streamCount);

                string audioChannels = mediaInfo.Get(StreamKind.Audio, 0, "Channel(s)");
                int ACindex = audioChannels.IndexOf(" /");                
                if (ACindex > 0)
                    audioChannels = audioChannels.Remove(ACindex);
					
                string audioLanguages = mediaInfo.Get(StreamKind.General, 0, "Audio_Language_List");
                
                string videoFrameRate = mediaInfo.Get(StreamKind.Video, 0, "FrameRate");

                string audioProfile = mediaInfo.Get(StreamKind.Audio, 0, "Format_Profile");
                int APindex = audioProfile.IndexOf(" /");
                if (APindex > 0)
                    audioProfile = audioProfile.Remove(APindex);
                


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
                    AudioChannelCount = audioChannels.Trim(),
                    AudioProfile = audioProfile.Trim(),
                    VideoFPS = videoFrameRate,
                    AudioLanguages = audioLanguages,
                    Subtitles = subtitles,
                    ScanType = scanType
                };
            }
            else
            {
                Logger.ReportInfo("Could not extract media information from " + location);
            }
            if (mediaType == MediaType.DVD && i != 0)
            {
                mediaInfo.Close();
                location = location.Replace("0.IFO", "1.vob");
                Logger.ReportInfo("Getting additional media info from " + location);
                mediaInfo.Option("ParseSpeed", "0.0");
                i = mediaInfo.Open(location);
                if (i != 0)
                {
                    int videoBitRate;
                    Int32.TryParse(mediaInfo.Get(StreamKind.Video, 0, "BitRate"), out videoBitRate);

                    int audioBitRate;
                    string aBitRate = mediaInfo.Get(StreamKind.Audio, 0, "BitRate");
                    int ABindex = aBitRate.IndexOf(" /");
                    if (ABindex > 0)
                        aBitRate = aBitRate.Remove(ABindex);
                    Int32.TryParse(aBitRate, out audioBitRate);
                    string scanType = mediaInfo.Get(StreamKind.Video, 0, "ScanType");

                    mediaInfoData.AudioBitRate = audioBitRate;
                    mediaInfoData.VideoBitRate = videoBitRate;
                    mediaInfoData.ScanType = scanType;
                }
                else
                {
                    Logger.ReportInfo("Could not extract additional media info from " + location);
                }
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
// Copyright (C) 2002-2009 Jerome Martinez, Zen@MediaArea.net
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
// Copyright (C) 2002-2009 Jerome Martinez, Zen@MediaArea.net
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
        FileOption_Nothing      = 0x00,
        FileOption_NoRecursive  = 0x01,
        FileOption_CloseAll     = 0x02,
        FileOption_Max          = 0x04
    };


    public class MediaInfo
    {
        //Import of DLL functions. DO NOT USE until you know what you do (MediaInfo DLL do NOT use CoTaskMemAlloc to allocate memory)  
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_New();
        [DllImport("MediaInfo.dll")]
        private static extern void   MediaInfo_Delete(IntPtr Handle);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Open(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string FileName);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Open(IntPtr Handle, IntPtr FileName);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Open_Buffer_Init(IntPtr Handle, Int64 File_Size, Int64 File_Offset);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Open(IntPtr Handle, Int64 File_Size, Int64 File_Offset);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Open_Buffer_Continue(IntPtr Handle, IntPtr Buffer, IntPtr Buffer_Size);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Open_Buffer_Continue(IntPtr Handle, Int64 File_Size, byte[] Buffer, IntPtr Buffer_Size);
        [DllImport("MediaInfo.dll")]
        private static extern Int64  MediaInfo_Open_Buffer_Continue_GoTo_Get(IntPtr Handle);
        [DllImport("MediaInfo.dll")]
        private static extern Int64  MediaInfoA_Open_Buffer_Continue_GoTo_Get(IntPtr Handle);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Open_Buffer_Finalize(IntPtr Handle);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Open_Buffer_Finalize(IntPtr Handle);
        [DllImport("MediaInfo.dll")]
        private static extern void   MediaInfo_Close(IntPtr Handle);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Inform(IntPtr Handle, IntPtr Reserved);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Inform(IntPtr Handle, IntPtr Reserved);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_GetI(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_GetI(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Get(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, [MarshalAs(UnmanagedType.LPWStr)] string Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Get(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Option(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string Option, [MarshalAs(UnmanagedType.LPWStr)] string Value);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Option(IntPtr Handle, IntPtr Option,  IntPtr Value);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_State_Get(IntPtr Handle);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Count_Get(IntPtr Handle, IntPtr StreamKind, IntPtr StreamNumber);

        //MediaInfo class
        public MediaInfo()
        {
            Handle = MediaInfo_New();
            if (Environment.OSVersion.ToString().IndexOf("Windows")==-1)
                MustUseAnsi=true;
            else
                MustUseAnsi=false;
        }
        ~MediaInfo() { MediaInfo_Delete(Handle); }
        public int Open(String FileName)
        {
            if (MustUseAnsi)
            {
                IntPtr FileName_Ptr = Marshal.StringToHGlobalAnsi(FileName);
                int ToReturn = (int)MediaInfoA_Open(Handle, FileName_Ptr);
                Marshal.FreeHGlobal(FileName_Ptr);
                return ToReturn;
            }
            else
                return (int)MediaInfo_Open(Handle, FileName);
        }
        public int Open_Buffer_Init(Int64 File_Size, Int64 File_Offset)
        {
            return (int)MediaInfo_Open_Buffer_Init(Handle, File_Size, File_Offset);
        }
        public int Open_Buffer_Continue(IntPtr Buffer, IntPtr Buffer_Size)
        {
            return (int)MediaInfo_Open_Buffer_Continue(Handle, Buffer, Buffer_Size);
        }
        public Int64 Open_Buffer_Continue_GoTo_Get()
        {
            return (int)MediaInfo_Open_Buffer_Continue_GoTo_Get(Handle);
        }
        public int Open_Buffer_Finalize()
        {
            return (int)MediaInfo_Open_Buffer_Finalize(Handle);
        }
        public void Close() { MediaInfo_Close(Handle); }
        public String Inform()
        {
            if (MustUseAnsi)
                return Marshal.PtrToStringAnsi(MediaInfoA_Inform(Handle, (IntPtr)0));
            else
                return Marshal.PtrToStringUni(MediaInfo_Inform(Handle, (IntPtr)0));
        }
        public String Get(StreamKind StreamKind, int StreamNumber, String Parameter, InfoKind KindOfInfo, InfoKind KindOfSearch)
        {
            if (MustUseAnsi)
            {
                IntPtr Parameter_Ptr=Marshal.StringToHGlobalAnsi(Parameter);
                String ToReturn=Marshal.PtrToStringAnsi(MediaInfoA_Get(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, Parameter_Ptr, (IntPtr)KindOfInfo, (IntPtr)KindOfSearch));
                Marshal.FreeHGlobal(Parameter_Ptr);
                return ToReturn;
            }
            else
                return Marshal.PtrToStringUni(MediaInfo_Get(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, Parameter, (IntPtr)KindOfInfo, (IntPtr)KindOfSearch));
        }
        public String Get(StreamKind StreamKind, int StreamNumber, int Parameter, InfoKind KindOfInfo)
        {
            if (MustUseAnsi)
                return Marshal.PtrToStringAnsi(MediaInfoA_GetI(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, (IntPtr)Parameter, (IntPtr)KindOfInfo));
            else
                return Marshal.PtrToStringUni(MediaInfo_GetI(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber, (IntPtr)Parameter, (IntPtr)KindOfInfo));
        }
        public String Option(String Option, String Value)
        {
            if (MustUseAnsi)
            {
                IntPtr Option_Ptr=Marshal.StringToHGlobalAnsi(Option);
                IntPtr Value_Ptr=Marshal.StringToHGlobalAnsi(Value);
                String ToReturn=Marshal.PtrToStringAnsi(MediaInfoA_Option(Handle, Option_Ptr, Value_Ptr));
                Marshal.FreeHGlobal(Option_Ptr);
                Marshal.FreeHGlobal(Value_Ptr);
                return ToReturn;
            }
            else
                return Marshal.PtrToStringUni(MediaInfo_Option(Handle, Option, Value));
        }
        public int State_Get() { return (int)MediaInfo_State_Get(Handle); }
        public int Count_Get(StreamKind StreamKind, int StreamNumber) { return (int)MediaInfo_Count_Get(Handle, (IntPtr)StreamKind, (IntPtr)StreamNumber); }
        private IntPtr Handle;
        private bool MustUseAnsi;

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
        private static extern IntPtr MediaInfoList_New();
        [DllImport("MediaInfo.dll")]
        private static extern void MediaInfoList_Delete(IntPtr Handle);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoList_Open(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string FileName, IntPtr Options);
        [DllImport("MediaInfo.dll")]
        private static extern void MediaInfoList_Close(IntPtr Handle, IntPtr FilePos);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoList_Inform(IntPtr Handle, IntPtr FilePos, IntPtr Reserved);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoList_GetI(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber, IntPtr Parameter, IntPtr KindOfInfo);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoList_Get(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber, [MarshalAs(UnmanagedType.LPWStr)] string Parameter, IntPtr KindOfInfo, IntPtr KindOfSearch);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoList_Option(IntPtr Handle, [MarshalAs(UnmanagedType.LPWStr)] string Option, [MarshalAs(UnmanagedType.LPWStr)] string Value);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoList_State_Get(IntPtr Handle);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoList_Count_Get(IntPtr Handle, IntPtr FilePos, IntPtr StreamKind, IntPtr StreamNumber);

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