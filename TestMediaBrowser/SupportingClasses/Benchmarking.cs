using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace TestMediaBrowser.SupportingClasses {
    static class Benchmarking {
        public static void TimeAction(string description, Action func) {
            var watch = new Stopwatch();
            watch.Start();
            func();
            watch.Stop();
            Console.Write(description);
            Console.WriteLine(" Time Elapsed {0} ms", watch.ElapsedMilliseconds);
        }
    }
}
