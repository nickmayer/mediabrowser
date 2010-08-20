using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.MediaCenter.UI;
using MediaBrowser.Library.Persistance;
using MediaBrowser.LibraryManagement;

namespace MediaBrowser.Library.Entities
{
    public class MediaInfoData
    {
        public readonly static MediaInfoData Empty = new MediaInfoData { AudioFormat = "", VideoCodec = "" };

        [Persist]
        public int Height;
        [Persist]
        public int Width;
        [Persist]
        public string VideoCodec;
        [Persist]
        public string AudioFormat;
        [Persist]
        public int VideoBitRate;
        [Persist]
        public int AudioBitRate;
        [Persist]
        public int RunTime = 0;
        [Persist]
        public int AudioStreamCount = 0;
        [Persist]
        public int AudioChannelCount = 0;
        [Persist]
        public string AudioProfile;
        [Persist]
        public string Subtitles;
        [Persist]
        public string VideoFPS;


        public string CombinedInfo
        {
            get
            {
                if (this != Empty)
                {
                    switch (this.AudioFormat.ToLower())
                    {
                        case "ac3":
                        case "dts":
                        case "mpeg audio":
                            {
                                if (this.AudioProfile != null && this.AudioProfile != "")
                                    return string.Format("{0}x{1}, {2} {3}kbps, {4} {5} {6}kbps", this.Width, this.Height, this.VideoCodec, this.VideoBitRate / 1000, this.AudioFormat, this.AudioProfile, this.AudioBitRate / 1000);
                                else
                                    return string.Format("{0}x{1}, {2} {3}kbps, {4} {5}kbps", this.Width, this.Height, this.VideoCodec, this.VideoBitRate / 1000, this.AudioFormat, this.AudioBitRate / 1000);
                            }
                        default:
                            return string.Format("{0}x{1}, {2} {3}kbps, {4} {5}kbps", this.Width, this.Height, this.VideoCodec, this.VideoBitRate / 1000, this.AudioFormat, this.AudioBitRate / 1000);
                    }
                }
                else
                    return "";
            }
        }

        #region Properties Video

        protected static Dictionary<string, string> VideoImageNames = new Dictionary<string, string>() {
            {"divx 5","divx"},
            {"divx 4","divx"},
            {"divx 3 low","divx"},
            {"avc ","H264"},
            {"vc-1","vc1"},
            {"wmv1","wmv"},
            {"wmv2","wmv"},
            {"wmv3","wmv"},
            {"wmv3hd","wmv_hd"},
            {"wvc1","wmv"},
            {"wvc1hd","wmv_hd"},
            {"mpeg-4 visual","mpeg4visual"},
            {"mpeg-2 video","H264"},
            {"on2 vp6","on2_vp6"},
            {"sorenson h263","sorenson_H263"},
        };

        protected string VideoImageName {
            get {
                //first look for hd value if we are hd
                if (Width >= 1280 || Height >= 700) {
                    if (VideoImageNames.ContainsKey(VideoCodecString.ToLower()+"hd")) return "codec_" + VideoImageNames[VideoCodecString.ToLower()+"hd"];
                }
                //next see if there is a translation for our codec
                if (VideoImageNames.ContainsKey(VideoCodecString.ToLower())) return "codec_" + VideoImageNames[VideoCodecString.ToLower()];
                //finally, just try the codec itself
                return "codec_"+VideoCodecString.ToLower();
            }
        }

        public Image VideoCodecImage
        {
            get
            {
                return Helper.GetMediaInfoImage(VideoImageName);
            }
        }

        public string VideoResolutionString
        {
            get
            {
                if (this != Empty)
                    return string.Format("{0}x{1}", this.Width, this.Height);
                else
                    return "";
            }
        }

        public string VideoCodecString
        {
            get
            {
                if (this != Empty)
                    return string.Format("{0}", this.VideoCodec);
                else
                    return "";
            }
        }

        public string AspectRatioString
        {
            get
            {
                if (this != Empty)
                {
                    Single width = (Single)this.Width;
                    Single height = (Single)this.Height;
                    Single temp = (width / height);

                    if (temp < 1.4)
                        return "4:3";
                    else if (temp >= 1.4 && temp <= 1.55)
                        return "3:2";
                    else if (temp > 1.55 && temp <= 1.8)
                        return "16:9";
                    else if (temp > 1.8 && temp <= 2)
                        return "1.85:1";
                    else if (temp > 2)
                        return "2.39:1";
                    else
                        return "";
                }
                else
                    return "";
            }
        }

        public string RuntimeString
        {
            get
            {
                if (RunTime != 0)
                {
                    return RunTime.ToString() + " mins";
                }
                else return "";
            }
        }

        public string VideoFrameRateString
        {
            get
            {
                return VideoFPS;
            }
        }

        public string VideoCodecExtendedString
        {
            get
            {
                return string.Format("{0} {1}kbps", this.VideoCodec, this.VideoBitRate / 1000);
            }
        }
        #endregion

        #region Properties Audio

        protected static Dictionary<string, string> AudioImageNames = new Dictionary<string, string>() {
            {"mpeg audio layer 1","codec_MpegAudio"},
            {"mpeg audio layer 2","codec_MpegAudio"},
            {"mpeg audio layer 3","codec_Mp3"},
            {"e-ac-3 5","codec_DDPlus_50"},
            {"e-ac-3 6","codec_DDPlus_51"},
            {"e-ac-3 7","codec_DDPlus_61"},
            {"e-ac-3 8","codec_DDPlus_71"},
            {"ac-3 truehd 5","codec_DDTrueHD_50"},
            {"ac-3 truehd 6","codec_DDTrueHD_51"},
            {"ac-3 truehd 7","codec_DDTrueHD_61"},
            {"ac-3 truehd 8","codec_DDTrueHD_71"},
            {"ac-3 1","codec_DD_10"},
            {"ac-3 2","codec_DD_20"},
            {"ac-3 3","codec_DD_30"},
            {"ac-3 6","codec_DD_51"},
            {"e-ac-3","codec_DDPlus"},
            {"ac-3 truehd","codec_DDPlus"},
            {"ac-3","codec_Ac3"},
            {"dts 1","codec_DTS_DS_10"},
            {"dts 2","codec_DTS_DS_20"},
            {"dts 6","codec_DTS_DS_51"},
            {"dts 96/24 6","codec_DTS_9624_51"},
            {"dts es 6","codec_DTS_ES_51"},
            {"dts es 7","codec_DTS_ES_61"},
            {"dts hra 6","codec_DTS_HD_HRA_51"},
            {"dts hra 7","codec_DTS_HD_HRA_61"},
            {"dts hra 8","codec_DTS_HD_HRA_71"},
            {"dts ma 3","codec_DTS_HD_MA_30"},
            {"dts ma 4","codec_DTS_HD_MA_40"},
            {"dts ma 5","codec_DTS_HD_MA_50"},
            {"dts ma 6","codec_DTS_HD_MA_51"},
            {"dts ma 7","codec_DTS_HD_MA_61"},
            {"dts ma 8","codec_DTS_HD_MA_71"},
            {"dts 96/24","codec_DTS_9624"},
            {"dts es","codec_DTS_ES"},
            {"dts hra","codec_DTS_HD_HRA"},
            {"dts ma","codec_DTS_HD_MA"},
            {"dts","codec_Dts"},
            {"wma","codec_Wma"},
            {"wma2","codec_Wma"},
            {"wma3","codec_Wma"},
            {"aac","codec_Aac"},
            {"flac","codec_Flac"},
            {"vorbis","codec_Vorbis"}
        };
        protected string AudioImageName {
            get {
                if (AudioImageNames.ContainsKey(AudioCombinedString.ToLower())) return AudioImageNames[AudioCombinedString.ToLower()];
                if (AudioImageNames.ContainsKey(AudioProfileString.ToLower())) return AudioImageNames[AudioProfileString.ToLower()];
                return ""; //not found...
            }
        }

        public Image AudioCodecImage
        {
            get
            {
                return Helper.GetMediaInfoImage(AudioImageName);
            }
        }
           
        public string AudioCodecString
        {
            get
            {
                if (this != Empty)
                    return string.Format("{0}", this.AudioFormat);
                else
                    return "";
            }
        }

        public string AudioChannelString
        {
            get
            {
                return AudioChannelCount.ToString();
            }
        }

        public string AudioStreamString
        {
            get
            {
                return AudioStreamCount.ToString();
            }
        }

        public string AudioCodecExtendedString
        {
            get
            {
                switch (this.AudioFormat.ToLower())
                {
                    case "ac3":
                    case "dts":
                    case "mpeg audio":
                        {
                            if (this.AudioProfile != null && this.AudioProfile != "")
                                return string.Format("{0} {1} {2}kbps", this.AudioFormat, this.AudioProfile, this.AudioBitRate / 1000);
                            else
                                return string.Format("{0} {1}kbps", this.AudioFormat, this.AudioBitRate / 1000);
                        }
                    default:
                        return string.Format("{0} {1}kbps", this.AudioFormat, this.AudioBitRate / 1000);
                }
            }
        }

        public string AudioProfileString
        {
            get
            {
                switch (this.AudioFormat.ToLower())
                {
                    case "ac3":
                    case "dts":
                    case "mpeg audio":
                        {
                            if (this.AudioProfile != null && this.AudioProfile != "")
                                return string.Format("{0} {1}", this.AudioFormat, this.AudioProfile);
                            else
                                return this.AudioFormat;
                        }
                    default:
                        return this.AudioFormat;
                }
            }
        }

        public string AudioCombinedString
        {
            get
            {
                return string.Format("{0} {1}", this.AudioProfileString, this.AudioChannelString);
            }
        }
        #endregion

        #region Properties General
        public string SubtitleString
        {
            get
            {
                return Subtitles;
            }
        }
        #endregion
    }
}
