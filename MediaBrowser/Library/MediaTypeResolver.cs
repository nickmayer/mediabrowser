using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MediaBrowser.Library {
    static class MediaTypeResolver {
        public static MediaType DetermineType(string path) {
            path = path.ToLower();
            if (path.Contains("video_ts"))
                return MediaType.DVD;
            if (path.EndsWith(".avi"))
                return MediaType.Avi;
            if (path.EndsWith(".mpg"))
                return MediaType.Mpg;
            if (path.EndsWith(".mpeg"))
                return MediaType.Mpeg;
            if (path.EndsWith(".mkv"))
                return MediaType.Mkv;
            if (path.EndsWith(".wmv"))
                return MediaType.Wmv;
            if (path.EndsWith(".mp4"))
                return MediaType.Mp4;
            if (path.EndsWith(".pls"))
                return MediaType.PlayList;
            if (path.EndsWith(".ts"))
                return MediaType.TS;
            if (path.EndsWith(".dvr-ms"))
                return MediaType.DVRMS;
            if (path.EndsWith(".wtv"))
                return MediaType.WTV;
            if (path.EndsWith(".flv"))
                return MediaType.Flv;
            if (path.EndsWith(".f4v"))
                return MediaType.F4v;
            if (path.EndsWith(".mov"))
                return MediaType.Mov;
            if (path.EndsWith(".ogv"))
                return MediaType.Ogv;
            if (path.EndsWith(".m4v"))
                return MediaType.M4v;
            if (path.EndsWith(".3gp"))
                return MediaType.Threegp;
            if (path.Contains("bdmv"))
                return MediaType.BluRay;
            if (path.Contains("hvdvd_ts"))
                return MediaType.HDDVD;
            if (Directory.Exists(Path.Combine(path, "VIDEO_TS")))
                return MediaType.DVD;
            if (Directory.Exists(Path.Combine(path, "BDMV")))
                return MediaType.BluRay;
            if (Directory.Exists(Path.Combine(path, "HVDVD_TS")))
                return MediaType.HDDVD;
            return MediaType.Unknown;
        }
    }
}
