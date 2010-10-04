using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Metadata;
using System.Diagnostics;
using MediaBrowser.Library;

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

        static void Main(string[] args)
        {
            Console.WriteLine(@"The refresh process runs in 3 steps: 

1. Your library is scanned for new files and missing files. They are added/removed with no metadata
2. Your library is scanned a second time, this time we try to populate metadata for fast providers (ones that do not depend on the Internet, or a large amount of processing power.
3. Your library is scanned a third time, this time all providers kick in.");
            FullRefresh(Kernel.Instance.RootFolder, MetadataRefreshOptions.Default);
        }


        static void FullRefresh(Folder folder, MetadataRefreshOptions options)
        {
            int totalItems = 0;
            int fastMetadataChanged = 0;
            int slowMetadataChanged = 0;
            folder.RefreshMetadata(options);

            var validationTime = TimeAction(() =>
            {
                RunActionRecursively("validate", folder, item =>
                {
                    Folder f = item as Folder;
                    if (f != null) f.ValidateChildren();
                });
            });

            var fastMetadataTime = TimeAction(() =>
            {
                RunActionRecursively("fast metadata", folder, item => {
                    fastMetadataChanged += item.RefreshMetadata(MetadataRefreshOptions.FastOnly) ? 1 : 0;
                    totalItems++;

                });
            });

            var slowMetadataTime = TimeAction(() =>
            {
                RunActionRecursively("slow metadata", folder, item =>
                {
                    slowMetadataChanged += item.RefreshMetadata(MetadataRefreshOptions.Default) ? 1 : 0;
                });
            });

            Console.WriteLine("We are done");
            Console.WriteLine("");
            Console.WriteLine("Validation took: " + (new DateTime(validationTime.Ticks)).ToString("HH:mm:ss"));
            Console.WriteLine("Fast metadata took: " + (new DateTime(fastMetadataTime.Ticks)).ToString("HH:mm:ss"));
            Console.WriteLine("Slow metadata took: " + (new DateTime(slowMetadataTime.Ticks)).ToString("HH:mm:ss"));

            Console.WriteLine();
            Console.WriteLine("Total items in your library: {0}", totalItems);
            Console.WriteLine("Fast metadata changed on {0} item/s", fastMetadataChanged);
            Console.WriteLine("Slow metadata changed on {0} item/s", slowMetadataChanged);
            Console.WriteLine();

            Console.WriteLine("Setting LastFullRefresh in config");
            Kernel.Instance.ConfigData.LastFullRefresh = DateTime.Now;
            Kernel.Instance.ConfigData.Save();
            
        }

        static string TimeSinceStart()
        {
            return new DateTime((DateTime.Now - startTime).Ticks).ToString("HH:mm:ss");
        }

        static void RunActionRecursively(string desc, Folder folder, Action<BaseItem> action)
        {
            Console.WriteLine("Refreshing Folder: " + folder.Path);
            Console.WriteLine("{0} - {1} : {2} {3} (t) {4}", desc, folder.GetType(), folder.Name, folder.Path, TimeSinceStart());
            action(folder);
            foreach (var item in folder.RecursiveChildren.OrderByDescending(i => i.DateModified))
            {
                Console.WriteLine("{0} - {1} : {2} {3} (t) {4}", desc ,item.GetType(), item.Name, item.Path, TimeSinceStart());
                action(item);
            }
        }
    }
}
