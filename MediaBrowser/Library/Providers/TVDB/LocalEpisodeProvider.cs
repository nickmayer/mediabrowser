using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Persistance;
using System.IO;
using System.Diagnostics;
using System.Xml;

namespace MediaBrowser.Library.Providers.TVDB {

    [SupportedType(typeof(Episode))]
    public class LocalEpisodeProvider : BaseMetadataProvider  {
        
        [Persist]
        string metadataFile;
        [Persist]
        DateTime metadataFileDate;

        public Episode Episode { get { return (Episode)Item; } }

        public override bool NeedsRefresh() {

            bool changed;
            string mfile = XmlLocation;

            changed = (metadataFile != mfile);

            if (!changed && mfile != null) {
                changed = (new FileInfo(mfile).LastWriteTimeUtc != metadataFileDate);
            }
            return changed;
        }

        public override void Fetch() 
        {
            Episode episode = Episode;
            Debug.Assert(episode != null);

            // store the location so we do not fetch again 
            metadataFile = XmlLocation;
            // no data, do nothing
            if (metadataFile == null) return;

            metadataFileDate = new FileInfo(metadataFile).LastWriteTimeUtc;

            string metadataFolder = Path.GetDirectoryName(metadataFile);
            
            XmlDocument metadataDoc = new XmlDocument();
            metadataDoc.Load(metadataFile);

            var p = metadataDoc.SafeGetString("Item/filename");
            if (p != null && p.Length > 0)
            {
                string image = System.IO.Path.Combine(metadataFolder, System.IO.Path.GetFileName(p));
                if (File.Exists(image))
                    Item.PrimaryImagePath = image;
            }
            else
            {
                string primaryExt = ".jpg";
                string secondaryExt = ".png";

                if (Config.Instance.PNGTakesPrecedence)
                {
                    primaryExt = ".png";
                    secondaryExt = ".jpg";
                }

                string file = Path.GetFileNameWithoutExtension(Item.Path);
                string image = System.IO.Path.Combine(metadataFolder, file + primaryExt);
                if (File.Exists(image))
                {
                    Item.PrimaryImagePath = image;
                }
                else
                {
                    image = System.IO.Path.Combine(metadataFolder, file + secondaryExt);
                    if (File.Exists(image))
                        Item.PrimaryImagePath = image;
                }
            }

            //if added date is in xml override the file/folder date
            DateTime added = DateTime.MinValue;
            DateTime.TryParse(metadataDoc.SafeGetString("Title/Added"), out added);
            if (added > DateTime.MinValue) episode.DateCreated = added;
               

            episode.Overview = metadataDoc.SafeGetString("Item/Overview");
            episode.EpisodeNumber = metadataDoc.SafeGetString("Item/EpisodeNumber");
            episode.Name = episode.EpisodeNumber + " - " + metadataDoc.SafeGetString("Item/EpisodeName");
            episode.SeasonNumber = metadataDoc.SafeGetString("Item/SeasonNumber");
            episode.ImdbRating = metadataDoc.SafeGetSingle("Item/Rating", (float)-1, 10);
            episode.FirstAired = metadataDoc.SafeGetString("Item/FirstAired");
            DateTime airDate = DateTime.MinValue;
            DateTime.TryParse(episode.FirstAired, out airDate);
            episode.ProductionYear = airDate.Year;
            


            string writers = metadataDoc.SafeGetString("Item/Writer");
            if (writers != null)
                episode.Writers = new List<string>(writers.Trim('|').Split('|'));


            string directors = metadataDoc.SafeGetString("Item/Director");
            if (directors != null)
                episode.Directors = new List<string>(directors.Trim('|').Split('|'));


            var actors = ActorListFromString(metadataDoc.SafeGetString("Item/GuestStars"));
            if (actors != null) {
                if (episode.Actors == null)
                    episode.Actors = new List<Actor>();
                episode.Actors = actors;
            }

            if (episode.DisplayMediaType == null)
            {
                episode.DisplayMediaType = metadataDoc.SafeGetString("Item/Type", "");
                switch (episode.DisplayMediaType.ToLower())
                {
                    case "blu-ray":
                        episode.DisplayMediaType = MediaType.BluRay.ToString();
                        break;
                    case "dvd":
                        episode.DisplayMediaType = MediaType.DVD.ToString();
                        break;
                    case "hd dvd":
                        episode.DisplayMediaType = MediaType.HDDVD.ToString();
                        break;
                    case "":
                        episode.DisplayMediaType = null;
                        break;
                }
            }
            if (episode.AspectRatio == null)
                episode.AspectRatio = metadataDoc.SafeGetString("Item/AspectRatio");

            if (episode.MediaInfo == null) episode.MediaInfo = new MediaInfoData();
            if (string.IsNullOrEmpty(episode.MediaInfo.AudioFormat))
            {
                //we need to decode metabrowser strings to format and profile
                string audio = metadataDoc.SafeGetString("Item/MediaInfo/Audio/Codec", "");
                if (audio != "")
                {
                    switch (audio.ToLower())
                    {
                        case "dts-es":
                        case "dts-es matrix":
                        case "dts-es discrete":
                            episode.MediaInfo.OverrideData.AudioFormat = "DTS";
                            episode.MediaInfo.OverrideData.AudioProfile = "ES";
                            break;
                        case "dts-hd hra":
                        case "dts-hd high resolution":
                            episode.MediaInfo.OverrideData.AudioFormat = "DTS";
                            episode.MediaInfo.OverrideData.AudioProfile = "HRA";
                            break;
                        case "dts-hd ma":
                        case "dts-hd master":
                            episode.MediaInfo.OverrideData.AudioFormat = "DTS";
                            episode.MediaInfo.OverrideData.AudioProfile = "MA";
                            break;
                        case "dolby digital":
                        case "dolby digital surround ex":
                        case "dolby surround":
                            episode.MediaInfo.OverrideData.AudioFormat = "AC-3";
                            break;
                        case "dolby digital plus":
                            episode.MediaInfo.OverrideData.AudioFormat = "E-AC-3";
                            break;
                        case "dolby truehd":
                            episode.MediaInfo.OverrideData.AudioFormat = "AC-3";
                            episode.MediaInfo.OverrideData.AudioProfile = "TrueHD";
                            break;
                        case "mp2":
                            episode.MediaInfo.OverrideData.AudioFormat = "MPEG Audio";
                            episode.MediaInfo.OverrideData.AudioProfile = "Layer 2";
                            break;
                        case "other":
                            break;
                        default:
                            episode.MediaInfo.OverrideData.AudioFormat = audio;
                            break;
                    }
                }
            }
            if (episode.MediaInfo.AudioStreamCount == 0) episode.MediaInfo.OverrideData.AudioStreamCount = metadataDoc.SelectNodes("Item/MediaInfo/Audio").Count;
            if (string.IsNullOrEmpty(episode.MediaInfo.AudioChannelCount)) episode.MediaInfo.OverrideData.AudioChannelCount = metadataDoc.SafeGetString("Item/MediaInfo/Audio/Channels", "");
            if (episode.MediaInfo.AudioBitRate == 0) episode.MediaInfo.OverrideData.AudioBitRate = metadataDoc.SafeGetInt32("Item/MediaInfo/Audio/BitRate");
            if (string.IsNullOrEmpty(episode.MediaInfo.VideoCodec))
            {
                string video = metadataDoc.SafeGetString("Item/MediaInfo/Video/Codec", "");
                if (video != "")
                {
                    switch (video.ToLower())
                    {
                        case "sorenson h.263":
                            episode.MediaInfo.OverrideData.VideoCodec = "Sorenson H263";
                            break;
                        case "h.262":
                            episode.MediaInfo.OverrideData.VideoCodec = "MPEG-2 Video";
                            break;
                        case "h.264":
                            episode.MediaInfo.OverrideData.VideoCodec = "AVC";
                            break;
                        default:
                            episode.MediaInfo.OverrideData.VideoCodec = video;
                            break;
                    }
                }
            }
            if (episode.MediaInfo.VideoBitRate == 0) episode.MediaInfo.OverrideData.VideoBitRate = metadataDoc.SafeGetInt32("Item/MediaInfo/Video/BitRate");
            if (episode.MediaInfo.Height == 0) episode.MediaInfo.OverrideData.Height = metadataDoc.SafeGetInt32("Item/MediaInfo/Video/Height");
            if (episode.MediaInfo.Width == 0) episode.MediaInfo.OverrideData.Width = metadataDoc.SafeGetInt32("Item/MediaInfo/Video/Width");
            if (string.IsNullOrEmpty(episode.MediaInfo.ScanType)) episode.MediaInfo.OverrideData.ScanType = metadataDoc.SafeGetString("Item/MediaInfo/Video/ScanType", "");
            if (string.IsNullOrEmpty(episode.MediaInfo.VideoFPS)) episode.MediaInfo.OverrideData.VideoFPS = metadataDoc.SafeGetString("Item/MediaInfo/Video/FrameRate", "");
            if (episode.MediaInfo.RunTime == 0) episode.MediaInfo.OverrideData.RunTime = metadataDoc.SafeGetInt32("Item/MediaInfo/Video/Duration");
            if (episode.MediaInfo.RunTime > 0) episode.RunningTime = episode.MediaInfo.RunTime;
            if (string.IsNullOrEmpty(episode.MediaInfo.AudioLanguages))
            {
                XmlNodeList nodes = metadataDoc.SelectNodes("Item/MediaInfo/Audio/Language");
                List<string> Langs = new List<string>();
                foreach (XmlNode node in nodes)
                {
                    string m = node.InnerText;
                    if (!string.IsNullOrEmpty(m))
                        Langs.Add(m);
                }
                if (Langs.Count > 1)
                {
                    episode.MediaInfo.OverrideData.AudioLanguages = String.Join(" / ", Langs.ToArray());
                }
                else
                {
                    episode.MediaInfo.OverrideData.AudioLanguages = metadataDoc.SafeGetString("Item/MediaInfo/Audio/Language", "");
                }
            }
            if (string.IsNullOrEmpty(episode.MediaInfo.Subtitles))
            {
                XmlNodeList nodes = metadataDoc.SelectNodes("Item/MediaInfo/Subtitle/Language");
                List<string> Subs = new List<string>();
                foreach (XmlNode node in nodes)
                {
                    string n = node.InnerText;
                    if (!string.IsNullOrEmpty(n))
                        Subs.Add(n);
                }
                if (Subs.Count > 1)
                {
                    episode.MediaInfo.OverrideData.Subtitles = String.Join(" / ", Subs.ToArray());
                }
                else
                {
                    episode.MediaInfo.OverrideData.Subtitles = metadataDoc.SafeGetString("Item/MediaInfo/Subtitle/Language", "");
                }
            }
        }


        private static List<Actor> ActorListFromString(string unsplit) {

            List<Actor> actors = null;
            if (unsplit != null) {
                actors = new List<Actor>();
                foreach (string name in unsplit.Trim('|').Split('|')) {
                    actors.Add(new Actor { Name = name });
                }
            }
            return actors;
        }

        private string XmlLocation {
            get {

                string metadataFolder = Path.Combine(Path.GetDirectoryName(Item.Path), "metadata");
                string file = Path.GetFileNameWithoutExtension(Item.Path);
                
                var location = Path.Combine(metadataFolder, file + ".xml");
                if (!File.Exists(location)) {
                    location = null;
                }

                return location;
            }
        }
    }
}
