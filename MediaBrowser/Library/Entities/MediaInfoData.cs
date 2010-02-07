using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Persistance;

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
        public string Subtitles;
        [Persist]
        public string VideoFPS;


        public string CombinedInfo
        {
            get
            {
                if (this != Empty)
                    return string.Format("{0}x{1}, {2} {3}kbps, {4} {5}kbps", this.Width, this.Height, this.VideoCodec, this.VideoBitRate / 1000, this.AudioFormat, this.AudioBitRate / 1000);
                else
                    return "";
            }
        }

        #region Properties Video

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
                return string.Format("{0} {1}kbps", this.AudioFormat, this.AudioBitRate / 1000);
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
