using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Providers;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Providers.Attributes;
using MediaBrowser.Library;
using Toub.MediaCenter.Dvrms.Metadata;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using MediaBrowser.LibraryManagement;
using System.Drawing.Imaging;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library.Logging;
using MediaBrowser.Library.Localization;

namespace DvrmsMetadataProvider {

    static class DvrmsMetadataEditorHelpers {

        public static T GetValue<T>(this IDictionary dict, string lookup) where T : class  {
            T val = default(T);
            var metaItem = dict[lookup] as MetadataItem;
            if (metaItem != null) {
                val = metaItem.Value as T;
            }
            return val;
        }
    }

    [SupportedType(typeof(Video),SubclassBehavior.Include)]

    public class DvrmsMetadataProvider : BaseMetadataProvider{

        public Show Show { get { return (Show)Item; } }
        public bool IsDvrms { get { return Item.Path.ToLower().EndsWith(".dvr-ms") || Item.Path.ToLower().EndsWith(".wtv"); ; } }

        [Persist]
        DateTime updateDate = DateTime.MinValue;

        public override void Fetch() {
            if (!IsDvrms) return;

            bool success = false;
            int attempt = 1;
            while (attempt < 6 && !success) {
                try {
                    UpdateMetadata();
                    success = true;
                    attempt++;
                } catch (Exception ex) {
                    Trace.WriteLine("Failed to get metadata: retrying " + ex.ToString());
                    Logger.ReportWarning("Dvrms Metadata Provider: Fetching metadata failed on attempt " + attempt + " for " + Item.Path);
                    attempt++;
                }
            }
            updateDate = DateTime.Now;
        }

        private void UpdateMetadata() 
        {
            Logger.ReportInfo("Dvrms Metadata Provider: Getting Metadata for " + Item.Path);
            using (new MediaBrowser.Util.Profiler("Dvrms Metadata Provider: Metadata extraction"))
            using (DvrmsMetadataEditor editor = new DvrmsMetadataEditor(Item.Path)) {
                var attribs = editor.GetAttributes();
                string name = attribs.GetValue<string>(MetadataEditor.Title);

                // australia ice tv adds MOVIE in front of movies
                if (name != null && name.StartsWith("MOVIE: ")) {
                    name = name.Substring(7);
                }

                string subtitle = attribs.GetValue<string>(MetadataEditor.Subtitle);
                string overview = attribs.GetValue<string>(MetadataEditor.SubtitleDescription);

                Item.SubTitle = subtitle;
                Item.Overview = overview;


                var MediaIsMovie = attribs["WM/MediaIsMovie"] as MetadataItem;
                bool IsMovie = MediaIsMovie.Value.ToString().Equals("True");


                // Not used
                //var HDContent = attribs["WM/WMRVHDContent"] as MetadataItem;
                //bool IsHDContent = HDContent.Value.ToString().Equals("True");


                // Get ReleaseYear
                try {
                    string OriginalReleaseTime = attribs.GetValue<string>("WM/OriginalReleaseTime");
                    Logger.ReportVerbose("DMP: OriginalReleaseTime: " + OriginalReleaseTime);
                    int ReleaseYear = 0;

                    if (!string.IsNullOrEmpty(OriginalReleaseTime)) {
                        DateTime releaseDate;
                        int releaseYear = DateTime.TryParse(OriginalReleaseTime, out releaseDate) ? releaseDate.Year : -1;
                        // Catch case where OriginalReleaseTime = DateTime.MinValue
                        if (releaseYear > 1) ReleaseYear = releaseYear;
                        // Catch case where only the year is used for OriginalReleaseTime
                        else {
                            int _releaseYear;
                            if (Int32.TryParse(OriginalReleaseTime, out _releaseYear) && _releaseYear > 1850 && _releaseYear < 2200) {
                                ReleaseYear = _releaseYear;
                            }
                        }
                    }


                    // Get OriginalBroadcastYear and OriginalBroadcastDate if item is not a movie
                    int BroadcastYear = 0;
                    string BroadcastDate = "";

                    if ((!IsMovie)) {
                        string OriginalBroadcastDateTime = attribs.GetValue<string>("WM/MediaOriginalBroadcastDateTime");
                        Logger.ReportVerbose("DMP: OriginalBroadcastDateTime: " + OriginalBroadcastDateTime);

                        if (!string.IsNullOrEmpty(OriginalBroadcastDateTime)) {
                            DateTime broadcastDate;
                            int broadcastYear = DateTime.TryParse(OriginalBroadcastDateTime, out broadcastDate) ? broadcastDate.Year : -1;
                            if (broadcastYear > 1) {
                                BroadcastYear = broadcastYear;
                                BroadcastDate = broadcastDate.ToString("yyyy/MM/dd");
                            }
                            else {
                                int _broadcastYear;
                                if (Int32.TryParse(OriginalBroadcastDateTime, out _broadcastYear) && _broadcastYear > 1850 && _broadcastYear < 2200) {
                                    BroadcastYear = _broadcastYear;
                                }
                            }
                        }
                    }


                    // For ProductionYear use ReleaseYear if available else use OriginalBroadcastYear for non movie items
                    if (ReleaseYear != 0) Show.ProductionYear = ReleaseYear;
                    else if (BroadcastYear != 0) Show.ProductionYear = BroadcastYear;
                    Logger.ReportVerbose("DMP: ProductionYear: " + Show.ProductionYear);


                    // Get RecordedDate
                    var EncodeTime = attribs["WM/WMRVEncodeTime"] as MetadataItem;
                    Logger.ReportVerbose("DMP: EncodeTime: " + EncodeTime.Value.ToString());

                    string RecordedDate = "";
                    int encodeYear = 0;
                    Int64 encodeTime = 0;

                    Int64.TryParse(EncodeTime.Value.ToString(), out  encodeTime);
                    DateTime eTUtc = new DateTime(encodeTime, DateTimeKind.Utc);
                    DateTime eTLocal = eTUtc.ToLocalTime();
                    encodeYear = eTLocal.Year;

                    // Check for FileTime value
                    if (encodeYear > 350 && encodeYear < 600)
                    {
                        RecordedDate = (DateTime.FromFileTime(encodeTime)).ToString("yyyy/MM/dd");
                    }
                    else if (encodeYear > 1950 && encodeYear < 2200)
                    {
                        RecordedDate = eTLocal.ToString("yyyy/MM/dd");
                    }
                    Logger.ReportVerbose("DMP: EncodeDate: " + RecordedDate);


                    // Option to avoid multiple identical names by appending RecordedDate or OriginalBroadcastDate to name of series item
                    string SeriesUID = attribs.GetValue<string>("WM/WMRVSeriesUID");
                    if ((!IsMovie) && (!string.IsNullOrEmpty(SeriesUID))) {
                         if (Plugin.PluginOptions.Instance.AppendFirstAiredDate && BroadcastDate != "") {
                            name = name + " " + BroadcastDate;
                            Logger.ReportInfo("Dvrms Metadata Provider: Modified Name: " + name);
                        }
                        else if (Plugin.PluginOptions.Instance.AppendRecordedDate && RecordedDate != "") {
                            name = name + " " + RecordedDate;
                            Logger.ReportInfo("Dvrms Metadata Provider: Modified Name: " + name);
                        }
                    }
                }
                catch (Exception ex) {
                    Logger.ReportInfo("Dvrms Metadata Provider: Error getting DateTime data. " + ex.Message);
                }

                Item.Name = name;


                // WM/ParentalRating: format can be ("***+;PG;TV-PG") OR ("***+" or "PG" or "TV-PG")
                // Possible US parental rating values: G, PG, PG-13, R, NC-17, NR, TV-Y, TV-Y7, TV-Y7 FV, TV-G, TV-PG, TV-14, TV-MA, NR.
                try {
                    string ParentalRating = attribs.GetValue<string>(MetadataEditor.ParentalRating);
                    Logger.ReportVerbose("DMP: ParentalRating: " + ParentalRating);
                    if (!string.IsNullOrEmpty(ParentalRating)) {
                        string starRating = "";
                        string mpaaRating = ""; {
                            if (ParentalRating.Contains(';')) {
                                string[] parentalRating = ParentalRating.Split(';');
                                starRating = (parentalRating[0]);
                                string movieRating = (parentalRating[1]);
                                string tvRating = (parentalRating[2]);

                                if ((IsMovie) && movieRating != "") mpaaRating = movieRating;
                                else if (tvRating != "") mpaaRating = tvRating;
                                else if (movieRating != "") mpaaRating = movieRating;
                            }
                            else if ((ParentalRating.Contains('*')) || (ParentalRating.Contains('+'))) starRating = ParentalRating;
                            else mpaaRating = ParentalRating;

                            if (mpaaRating == "TV-Y7 FV") Show.MpaaRating = "TV-Y7-FV";
                            else Show.MpaaRating = mpaaRating;
                            Logger.ReportVerbose("DMP: MpaaRating: " + Show.MpaaRating);

                            // Convert 4 star rating systen "***+" to decimal as used by IMDb and TMDb
                            char[] split = starRating.ToCharArray();
                            int starCount = 0;
                            int plusCount = 0;
                            foreach (char c in split) {
                                if (c.Equals('*'))
                                    starCount++;
                                if (c.Equals('+'))
                                    plusCount++;
                            }
                            double i = 0;
                            i = Math.Round(Convert.ToSingle((starCount * 2.5) + (plusCount * 1.25)), 1);
                            if (Plugin.PluginOptions.Instance.UseStarRatings && (i != 0)) {
                                Show.ImdbRating = (float)i;
                                Logger.ReportVerbose("DMP: ImdbRating: " + Show.ImdbRating);
                            }
                        }
                    }
                }
                catch (Exception ex) {
                    Logger.ReportInfo("Dvrms Metadata Provider: Error getting Ratings. " + ex.Message);
                }


                // WM/MediaCredits format: (Actor1/Actor2;Director1/Director2;Host1/Host2;OtherCredit1/OtherCredit2)
                try {
                    string Credits = attribs.GetValue<string>(MetadataEditor.Credits);
                    Logger.ReportVerbose("DMP: Credits: " + Credits);

                    string[] credits = Credits.Split(';');
                    string cast = (credits[0]);
                    string directors = (credits[1]);
                    string hosts = (credits[2]);
                    string otherCredits = (credits[3]);

                    Logger.ReportVerbose("DMP: Directors: " + directors);
                    Logger.ReportVerbose("DMP: Actors: " + cast);
                    Logger.ReportVerbose("DMP: Hosts: " + hosts);

                    if (cast != "" || hosts != "") {
                        Show.Actors = new List<Actor>();
                        foreach (string CastName in cast.Split('/')) {
                            if (CastName != "") Show.Actors.Add(new Actor { Name = CastName.Trim() });
                        }
                        foreach (string HostsName in hosts.Split('/')) {
                            string host = Kernel.Instance.StringData.GetString("HostDetail");
                            // Use host = "Host" unless localized in core
                            if (HostsName != "" && host != "") Show.Actors.Add(new Actor { Name = HostsName.Trim(), Role = host });
                            else if (HostsName != "") Show.Actors.Add(new Actor { Name = HostsName.Trim(), Role = "Host" });
                        }
                    } 
                    if (directors != "") Show.Directors = new List<string>(directors.Split('/'));
                }
                catch (Exception ex) {
                    Logger.ReportInfo("Dvrms Metadata Provider: Error getting Cast & Crew. " + ex.Message);
                }


                string studios = attribs.GetValue<string>("WM/MediaStationCallSign");
                // Special case for UK channel "5*"
                studios = studios.Replace("5*", "5star");
                if (!string.IsNullOrEmpty(studios)) {
                    Show.Studios = new List<string>(studios.Split(';'));
                    Logger.ReportVerbose("DMP: Network: " + studios);
                }


                string genres = attribs.GetValue<string>(MetadataEditor.Genre);
                if (Plugin.PluginOptions.Instance.UseGenres && (!string.IsNullOrEmpty(genres))) {
                    Show.Genres = new List<string>(genres.Split(';'));
                    Logger.ReportVerbose("DMP: Genres: " + genres);
                }


                // Not used, allows option to remove certain Genres from list.
                // We would need a list of genres used by broadcasters to filter or convert these properly
                // plus they can be in different languages

                /*List<string> badGenres = new List<string>();
                //badGenres.Add("shows");
                //badGenres.Add("film");
                string genres = attribs.GetValue<string>(MetadataEditor.Genre);
                if (Plugin.PluginOptions.Instance.UseGenres && (!string.IsNullOrEmpty(genres))) {
                    List<string> genresList = new List<string>(genres.Split(';'));
                    for(int i = 0; i <= genresList.Count -1; i++) {
                        string genre = genresList[i];
                        if (badGenres.Contains(genre.ToLower())) {
                            genresList.Remove(genre);
                            i -= 1;
                        }
                    }
                    if (genresList.Count > 0) Show.Genres = genresList;
                }*/


                // Duration is the length of the file in minutes
                var Duration = attribs["Duration"] as MetadataItem;
                Int64 runTime = 0;
                int RunTime;
                Int64.TryParse(Duration.Value.ToString(), out  runTime);
                // Show RunTime as 1 minute if less than 60 seconds as Media Center does
                // Not used, due to Resume issue when watched item is less than 60 seconds
                //if (runTime > 0 & runTime < 600000000) RunTime = 1;
                //else
                RunTime = Convert.ToInt32(runTime / 600000000);
                if (runTime > 0) {
                    Show.RunningTime = RunTime;
                    Logger.ReportVerbose("DMP: Duration: " + Show.RunningTime);
                }


                // Image processing
                Logger.ReportVerbose("DMP: Process thumbnail started");
                double defaultAspectRatio = 1.778;
                double thumbAspectRatio = 0;
                try {
                    // Calculate correct aspect ratio of embedded thumbnail                    
                    var thumbAspectRatioX = attribs["WM/MediaThumbAspectRatioX"] as MetadataItem;
                    var thumbAspectRatioY = attribs["WM/MediaThumbAspectRatioY"] as MetadataItem;
                    thumbAspectRatio = Math.Round(Double.Parse(thumbAspectRatioX.Value.ToString()) / Double.Parse(thumbAspectRatioY.Value.ToString()), 3);
                    if ((Double.IsNaN(thumbAspectRatio)) || (Double.IsInfinity(thumbAspectRatio)) || thumbAspectRatio == 0) {
                        thumbAspectRatio = defaultAspectRatio;
                        Logger.ReportInfo("Dvrms Metadata Provider: Failed to get thumbnail aspect ratio so using default value " + thumbAspectRatio);
                    }
                    else { 
                        Show.AspectRatio = Math.Round(thumbAspectRatio, 2).ToString();
                        Logger.ReportVerbose("DMP: AspectRatio " + Show.AspectRatio); 
                    }
                }
                catch {
                    thumbAspectRatio = defaultAspectRatio;
                    Logger.ReportInfo("Dvrms Metadata Provider: Error getting thumbnail aspect ratio so using default value " + defaultAspectRatio);
                }

                var image = editor.GetWMPicture();
                if (image != null) {
                        lock (typeof(BaseMetadataProvider)) {
                            var imagePath = Path.Combine(ApplicationPaths.AppImagePath, Item.Id.ToString() + "-orig.png");
                            // Set aspect ratio of extracted image to be the same as thumbAspectRatio  
                            ResizeImage(image.Picture, thumbAspectRatio).Save(imagePath, ImageFormat.Png);
                            Item.PrimaryImagePath = imagePath;
                            Logger.ReportVerbose("DMP: Process thumbnail finished");
                            
                        }
                } else Logger.ReportInfo("Dvrms Metadata Provider: No thumbnail image in video.. try viewing item in WMC Recorded TV library to create one");

                //Logger.ReportInfo("Dvrms Metadata Provider: Metadata extraction complete");
            } 
        }


        private static Image ResizeImage(Image sourceImg, double aspectRatio = 0) {
            int outputWidth = 400;
            int sourceWidth = sourceImg.Width;
            int sourceHeight = sourceImg.Height;
            int outputHeight;
            if (aspectRatio != 0) outputHeight = (int)Math.Round((outputWidth / aspectRatio));
            else {
                float ratio = outputWidth / sourceWidth;
                outputHeight = (int)Math.Round((ratio * sourceHeight));
            }
            Bitmap outputImg = new Bitmap(outputWidth, outputHeight);
            Graphics g = Graphics.FromImage((Image)outputImg);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            // Maintain dimensions but trim 1.5% from each edge of image to remove overscan distortions
            g.DrawImage(sourceImg, (int)((outputWidth * -0.015)), (int)((outputHeight * -0.015)), (int)(outputWidth * 1.03), (int)(outputHeight * 1.03));
            g.Dispose();
            return (Image)outputImg;
        }


        private static void OutputDiagnostics(IDictionary attribs) {
            foreach (var key in attribs.Keys) {
                var item = attribs[key] as MetadataItem;
                Trace.WriteLine(key.ToString() + " " + item.Value.ToString());
            }
            Trace.WriteLine("");
        }


        public override bool NeedsRefresh() {
            return IsDvrms && new FileInfo(Item.Path).LastWriteTime > updateDate;
        }
    }
}
