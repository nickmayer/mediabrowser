using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Metadata;
using System.Diagnostics;
using MediaBrowser.Library;
using System.IO;
using MediaBrowser.Library.Configuration;

namespace FullRefresh
{
    class Program
    {
        public static TimeSpan TimeAction(Action func)
        {
            var watch = new Stopwatch();
            watch.Start();
            func();
            watch.Stop();
            return (watch.Elapsed);
        }

        static DateTime startTime = DateTime.Now;

        static bool rebuildImageCache = false;
        static bool writeToFullRefreshLog = false;

        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args.Count() > 0 && args[i].ToLower() == "/i") rebuildImageCache = true;
                if (args.Count() > 0 && args[i].ToLower() == "/log") writeToFullRefreshLog = true;
            }

            if (writeToFullRefreshLog)
            {
                // Redirect output to a file named FullRefresh.log
                StreamWriter sw = new StreamWriter(@".\FullRefresh.log");
                sw.AutoFlush = true;
                Console.SetOut(sw);
            }

            Console.Out.WriteLine(@"
===[Information]=========================================

The refresh process runs in 3 steps: 

1. Your library is scanned for new files and missing files. They are added/removed with no metadata
2. Your library is scanned a second time, this time we try to populate metadata for fast providers (ones that do not depend on the Internet, or a large amount of processing power.
3. Your library is scanned a third time, this time all providers kick in.

/i on command line will cause image cache to be cleared and re-built during the last step.");
            FullRefresh(Kernel.Instance.RootFolder, MetadataRefreshOptions.Default);
        }


        static void FullRefresh(Folder folder, MetadataRefreshOptions options)
        {
            int totalItems = 0;
            int fastMetadataChanged = 0;
            int slowMetadataChanged = 0;
            folder.RefreshMetadata(options);
            Console.Out.WriteLine();
            Console.Out.WriteLine("===[Validate]============================================");
            Console.Out.WriteLine();
            var validationTime = TimeAction(() =>
            {
                RunActionRecursively("validate", folder, item =>
                {
                    Folder f = item as Folder;
                    if (f != null) f.ValidateChildren();
                });
            });
            Console.Out.WriteLine();
            Console.Out.WriteLine("===[Fast Metadata]=======================================");
            Console.Out.WriteLine();
            var fastMetadataTime = TimeAction(() =>
            {
                RunActionRecursively("fast metadata", folder, item => {
                    fastMetadataChanged += item.RefreshMetadata(MetadataRefreshOptions.FastOnly) ? 1 : 0;
                    totalItems++;

                });
            });
            if (rebuildImageCache)
            {
                Console.Out.WriteLine();
                Console.Out.WriteLine("===[Recreate ImageCache]=================================");
                Console.Out.WriteLine();
                Console.Out.WriteLine("/i specified - Clearing Image Cache for re-build..");
                Console.Out.WriteLine();
                try
                {
                    Console.Out.WriteLine("Deleting ImageCache folder.");
                    Directory.Delete(ApplicationPaths.AppImagePath, true);
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine("Error trying to delete ImageCache folder. " + ex.Message);
                }
                Console.Out.WriteLine("Sleeping 2 seconds.");
                System.Threading.Thread.Sleep(2000); // give it time to fully delete
                Console.Out.WriteLine("Continuing.");
                try
                {
                    Console.Out.WriteLine("Creating ImageCache folder.");
                    Directory.CreateDirectory(ApplicationPaths.AppImagePath);
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine("Error trying to create ImageCache folder. " + ex.Message);
                }
                Console.Out.WriteLine("Sleeping 2 seconds.");
                System.Threading.Thread.Sleep(2000); // give it time to fully create
                Console.Out.WriteLine("Continuing.");
            }
            Console.Out.WriteLine();
            Console.Out.WriteLine("===[Slow Metadata]=======================================");
            Console.Out.WriteLine();
            var slowMetadataTime = TimeAction(() =>
            {
                RunActionRecursively("slow metadata", folder, item =>
                {
                    slowMetadataChanged += item.RefreshMetadata(MetadataRefreshOptions.Default) ? 1 : 0;

                    if (rebuildImageCache)
                    {
                        //touch all the images - causing them to be re-cached
                        Console.Out.WriteLine("Caching images for " + item.Name);
                        string ignore = null;
                        if (item.PrimaryImage != null)
                        {
                            //get the display size of our primary image if known
                            if (item.Parent != null)
                            {
                                DisplayPreferences dp = Kernel.Instance.ItemRepository.RetrieveDisplayPreferences(item.Id);
                                Microsoft.MediaCenter.UI.Size s = new Microsoft.MediaCenter.UI.Size();
                                if (dp != null) s = dp.ThumbConstraint.Value;
                                if (s.Width > 0 && s.Height > 0)
                                {
                                    Console.Out.WriteLine("Cacheing primary image at " + s.Width + " x " + s.Height);
                                    ignore = item.PrimaryImage.GetLocalImagePath(s.Width, s.Height); //force to re-cache at display size
                                }
                                else
                                    ignore = item.PrimaryImage.GetLocalImagePath(); //no size - cache at full size
                            }
                            else
                            {
                                ignore = item.PrimaryImage.GetLocalImagePath(); //no parent or display prefs - cache at full size
                            }
                            if (item.SecondaryImage != null)
                                ignore = item.SecondaryImage.GetLocalImagePath();
                            if (item.BackdropImages != null)
                                foreach (var image in item.BackdropImages)
                                {
                                    ignore = image.GetLocalImagePath();
                                }
                            if (item.BannerImage != null)
                                ignore = item.BannerImage.GetLocalImagePath();
                        }
                    }
                });
            });
            Console.Out.WriteLine();
            Console.Out.WriteLine("===[Saving LastFullRefresh]==============================");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Saving LastFullRefresh in config");
            Kernel.Instance.ConfigData.LastFullRefresh = DateTime.Now;
            Kernel.Instance.ConfigData.Save();
            Console.Out.WriteLine();
            Console.Out.WriteLine("===[Results]=============================================");
            Console.Out.WriteLine();
            Console.Out.WriteLine("We are done");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Validation took:              " + (new DateTime(validationTime.Ticks)).ToString("HH:mm:ss"));
            Console.Out.WriteLine("Fast metadata took:           " + (new DateTime(fastMetadataTime.Ticks)).ToString("HH:mm:ss"));
            Console.Out.WriteLine("Slow metadata took:           " + (new DateTime(slowMetadataTime.Ticks)).ToString("HH:mm:ss"));
            Console.Out.WriteLine("Total items in your library:  {0}", totalItems);
            Console.Out.WriteLine();
            Console.Out.WriteLine("Fast metadata changed on {0} item's", fastMetadataChanged);
            Console.Out.WriteLine("Slow metadata changed on {0} item's", slowMetadataChanged);
            Console.Out.WriteLine();
            Console.Out.WriteLine("===[EOF]==================================================");
        }

        static string TimeSinceStart()
        {
            return new DateTime((DateTime.Now - startTime).Ticks).ToString("HH:mm:ss");
        }

        static void RunActionRecursively(string desc, Folder folder, Action<BaseItem> action)
        {
            Console.Out.WriteLine("Refreshing Folder: " + folder.Path);
            Console.Out.WriteLine("{0} - {1} : {2} {3} (t) {4}", desc, folder.GetType(), folder.Name, folder.Path, TimeSinceStart());
            action(folder);
            foreach (var item in folder.RecursiveChildren.OrderByDescending(i => i.DateModified))
            {
                Console.Out.WriteLine("{0} - {1} : {2} {3} (t) {4}", desc ,item.GetType(), item.Name, item.Path, TimeSinceStart());
                action(item);
            }
        }
    }
}
