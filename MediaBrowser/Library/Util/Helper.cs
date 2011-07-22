using System;
using System.Collections.Generic;
using System.Text;
using System.Resources;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Reflection;
using System.Management;
//using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using MediaBrowser.Library.Configuration;
using MediaBrowser.Library;
using Microsoft.MediaCenter.UI;
using MediaBrowser.Interop;


namespace MediaBrowser.LibraryManagement
{
    using System.Drawing.Drawing2D;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using MediaBrowser.Util;
    using System.Net;
    using System.Xml;
    using MediaBrowser.Library.Logging;

    public static class Helper
    {
        public const string MY_VIDEOS = "MyVideos";
        static readonly string[] isoExtensions = { "iso", "img" };

        public static Dictionary<string, bool> perceivedTypeCache = new Dictionary<string, bool>();

        public static bool IsExtenderNativeVideo(string filename)
        {
            string extension = System.IO.Path.GetExtension(filename).ToLower();
            var extensions = Config.Instance.ExtenderNativeTypes.Split(',');
            foreach (var item in extensions)
            {
                if (item == extension)
                {
                    return true;
                }
            }
            return false;
        }

        // Check if this file is an Iso.  (This is not used to determine what files
        // are videos, etc.  It is more used to filter certain cases
        // that are handled differently for Isos).
        public static bool IsIso(string filename)
        {
            string extension = System.IO.Path.GetExtension(filename).ToLower();
            foreach (string e in isoExtensions)
                if (extension == "." + e)
                    return true;
            return false;
        }

        public static List<string> GetIsoFiles(string path)
        {
            List<string> files = new List<string>();
            foreach(string ext in isoExtensions)
                files.AddRange(Directory.GetFiles(path, "*." + ext));
            return files;
        }

        // I left the hardcoded list, cause the failure mode is better, at least it will show
        // videos if the codecs are not installed properly
        public static bool IsVideo(string filename)
        {
            string extension = System.IO.Path.GetExtension(filename).ToLower();

            switch (extension)
            {
                // special case so DVD files are never considered videos and ISOs not
                case ".vob":
                case ".bup":
                case ".ifo":
                case ".iso":
                    return false; 
                case ".rmvb":
                case ".mov":
                case ".avi":
                case ".mpg":
                case ".mpeg":
                case ".wmv":
                case ".mp4":
                case ".mkv":
                case ".divx":
                case ".dvr-ms":
                case ".wtv":
                case ".ogm":
                case ".ogv":
                case ".asf":
                case ".m4v":
                case ".flv":
                case ".f4v":
                case ".3gp":
                    return true;

                default:

                    bool isVideo;
                    lock (perceivedTypeCache)
                    {
                        if (perceivedTypeCache.TryGetValue(extension, out isVideo))
                        {
                            return isVideo;
                        }
                    }

                    string pt = null;
                    RegistryKey key = Registry.ClassesRoot;
                    key = key.OpenSubKey(extension);
                    if (key != null)
                    {
                        pt = key.GetValue("PerceivedType") as string;
                    }
                    if (pt == null) pt = "";
                    pt = pt.ToLower();

                    lock (perceivedTypeCache)
                    {
                        perceivedTypeCache[extension] = (pt == "video");
                    }

                    return perceivedTypeCache[extension];
            }
            
        }

        private static string myVideosPath = null;
        public static string MyVideosPath
        {
            get
            {
                if (myVideosPath == null)
                {
                    // Missing from System.Environment
                    int CSIDL_MYVIDEO = 0xe;

                    StringBuilder lpszPath = new StringBuilder(260);
                    MediaBrowser.Interop.ShellNativeMethods.SHGetFolderPath(IntPtr.Zero, CSIDL_MYVIDEO, IntPtr.Zero, 0, lpszPath);
                    myVideosPath = lpszPath.ToString();
                }
                return myVideosPath;

            }
        }

        public static bool IsVob(String filename)
        {
            string extension = System.IO.Path.GetExtension(filename).ToLower();
            return extension == ".vob";
        }

        public static bool IsIfo(string filename) {
            string extension = System.IO.Path.GetExtension(filename).ToLower();
            return extension == ".ifo";
        }

        public static bool IsShortcut(string filename)
        {
            return System.IO.Path.GetExtension(filename).ToLower() == ".lnk";
        }

        internal static bool IsVirtualFolder(string filename)
        {
            return System.IO.Path.GetExtension(filename).ToLower() == ".vf";
        }

        public static string ResolveShortcut(string filename)
        {
            return MediaBrowser.Interop.ShortcutNativeMethods.ResolveShortcut(filename);
        }

        public static bool ContainsFile(string path, string filter)
        {
            if (Directory.Exists(path))
                return Directory.GetFiles(path, filter).Length > 0;
            else
                return false;
        }

        public static bool IsFolder(FileSystemInfo fsi)
        {
            return ((fsi.Attributes & FileAttributes.Directory) == FileAttributes.Directory);
        }

        public static bool IsFolder(string path)
        {
            //faster than directory.exists?
            try
            {
                return (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="files">A pre obtained list of the files in the path folder if available, else null</param>
        /// <param name="folders">A pre obtained list of folders in the path folder if available, else null</param>
        /// <returns></returns>
        public static bool IsDvDFolder(string path,string[] files, string[] folders)
        {
            if (files == null)
                files = Directory.GetFiles(path);
            foreach (string f in files)
                if ((f.Length > 4) && (f.Substring(f.Length - 4).ToLower() == ".vob"))
                    return true;
            if (folders == null)
                folders = Directory.GetDirectories(path);
            foreach (string f in folders)
                if (f.ToUpper().EndsWith("VIDEO_TS"))
                    return true;
            return false;
        }

        public static bool IsBluRayFolder(string path, string[] folders)
        {
            if (folders == null)
                folders = Directory.GetDirectories(path);
            foreach (string f in folders)
                if (f.ToUpper().EndsWith("BDMV"))
                    return true;
            return false;
        }

        public static bool IsHDDVDFolder(string path,  string[] folders)
        {
            if (folders == null)
                folders = Directory.GetDirectories(path);
            foreach (string f in folders)
                if (f.ToUpper().EndsWith("HVDVD_TS"))
                    return true;
            return false; 
        }

        public static int IsoCount(string path, string[] files)
        {
            if (files == null)
            {
                if (Directory.Exists(path))
                {
                    return GetIsoFiles(path).Count;
                }
                else
                    return 0;
            }
            else
            {
                int i = 0;
                foreach (string f in files)
                    if (f.Length > 4)
                    {
                        string ext = f.Substring(f.Length - 4).ToLower();
                        foreach(string e in isoExtensions)
                            if (ext == "." + e)
                            {
                                i++;
                                break;
                            }
                    }
                return i;
            }  
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="files">A pre obtained list of the files in the path folder if available, else null</param>
        /// <param name="folders">A pre obtained list of folders in the path folder if available, else null</param>
        /// <returns></returns>
        public static bool ContainsNestedDvdOrIso(string path, string[] files, string[] folders)
        {
            if (files == null)
                files = Directory.GetFiles(path);
            if (IsoCount(path, files) > 0)
                return true;
            if (folders == null)
                folders = Directory.GetDirectories(path);
            if (IsDvDFolder(path, files, folders))
                return true;
            if (IsBluRayFolder(path,  folders))
                return true;
            if (IsHDDVDFolder(path, folders))
                return true;
            
            foreach (string f in folders)
            {
                if (ContainsNestedDvdOrIso(f, null,null))
                    return true;
            }
            return false;  
        }


        static Regex commentExpression = new Regex(@"(\[[^\]]*\])");
        public static string RemoveCommentsFromName(string name)
        {
            return name == null ? null : commentExpression.Replace(name, "");
        }

        internal static bool HasNoAutoPlaylistFile(string path, string[] files)
        {
            foreach (string file in files)
                if (file.ToLower().EndsWith("noautoplaylist"))
                    return true;
            return false;
        }

        internal static bool IsRoot(string path)
        {
            return (Config.Instance.InitialFolder==path) || (Config.Instance.InitialFolder == Helper.MY_VIDEOS && path == Helper.MyVideosPath);
        }

        private static readonly Regex alphaNumeric = new Regex("[^a-zA-Z0-9]");
        public static bool IsAlphaNumeric(string str)
        {
            return (!alphaNumeric.IsMatch(str));
        }

        public static string RemoveInvalidFileChars(string filename) {

            var cleanName = new StringBuilder();
            foreach (var letter in filename) {
                if (!System.IO.Path.GetInvalidFileNameChars().Contains(letter)) {
                    cleanName.Append(letter);
                }
            }
            return cleanName.ToString();

        }

        /// <summary>
        /// Fetch an XmlDocument
        /// </summary>
        /// <param name="url"></param>
        /// <returns>document on success, null on failure</returns>
        public static XmlDocument Fetch(string url) {
            try {

                int attempt = 0;
                while (attempt < 2) {
                    attempt++;
                    try {
                        var req = (HttpWebRequest)HttpWebRequest.Create(url);
                        req.Timeout = 15000;
                        req.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
                        req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                        using (WebResponse resp = req.GetResponse())
                            try
                            {
                                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                                {
                                    XmlDocument doc = new XmlDocument();
                                    // this makes it a bit easier to debug.
                                    string payload = reader.ReadToEnd();
                                    doc.LoadXml(payload);
                                    return doc;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.ReportException("Error getting xml from plugin source",ex);
                            }
                            finally
                            {
                                resp.Close();
                                GC.Collect();  //forcing a collection here appears to solve the issue with timeouts on secondary sources
                                               //which probably means there is another issue here, but I cannot find it - tried closing
                                               //everything, 'using' clauses and this is the only thing that seems to work. -ebr
                            }
                    } catch (WebException ex) {
                        Logger.ReportWarning("Error requesting: " + url + "\n" + ex.ToString());
                    } catch (IOException ex) {
                        Logger.ReportWarning("Error requesting: " + url + "\n" + ex.ToString());
                    }
                        
                }
            } catch (Exception ex) {
                Logger.ReportWarning("Failed to fetch url: " + url + "\n" + ex.ToString());
            }

            return null;
        }

        public static string GetNameFromFile(string filename)
        {
            string temp;
            string fn;
            //first, if the specified name is a file system folder, it probably doesn't have an extention so use the whole name
            if (System.IO.Directory.Exists(filename))
                fn = System.IO.Path.GetFileName(filename);
            else
                fn = System.IO.Path.GetFileNameWithoutExtension(filename);

            //now - strip out anything inside brackets
            temp = GetStringInBetween("[", "]", fn, true, true)[0];
            while (temp.Length > 0)
            {
                fn = fn.Replace(temp, "");
                temp = GetStringInBetween("[", "]", fn, true, true)[0];
            }
            return fn;
        }

        public static string[] GetStringInBetween(string strBegin,
            string strEnd, string strSource,
            bool includeBegin, bool includeEnd)
        {
            string[] result = { "", "" };
            int iIndexOfBegin = strSource.IndexOf(strBegin);
            if (iIndexOfBegin != -1)
            {
                // include the Begin string if desired 
                if (includeBegin)
                    iIndexOfBegin -= strBegin.Length;
                strSource = strSource.Substring(iIndexOfBegin
                    + strBegin.Length);
                int iEnd = strSource.IndexOf(strEnd);
                if (iEnd != -1)
                {
                    // include the End string if desired 
                    if (includeEnd)
                        iEnd += strEnd.Length;
                    result[0] = strSource.Substring(0, iEnd);
                    // advance beyond this segment 
                    if (iEnd + strEnd.Length < strSource.Length)
                        result[1] = strSource.Substring(iEnd
                            + strEnd.Length);
                }
            }
            else
                // stay where we are 
                result[1] = strSource;
            return result;
        }

        public static Microsoft.MediaCenter.UI.Image GetMediaInfoImage(string name)
        {
            name = name.ToLower().Replace('/','-');
            string baseLocation = Config.Instance.ImageByNameLocation;
            if ((baseLocation == null) || (baseLocation.Length == 0))
                baseLocation = Path.Combine(ApplicationPaths.AppConfigPath, "ImagesByName");
            baseLocation += "\\MediaInfo";

            //we'll look first in a theme-specific folder if it exists
            string ibnLocation = Path.Combine(baseLocation, Config.Instance.ViewTheme);
            if (!Directory.Exists(ibnLocation)) //no theme-specific one - use default
                ibnLocation = Path.Combine(baseLocation, "all"); //don't use 'default' cuz that's the name of a theme...

            string fileName = Path.Combine(ibnLocation, RemoveInvalidFileChars(name) + ".png");
            if (File.Exists(fileName))
            {
                return new Image("file://" + fileName);
            }
            else
            {
                //not there, get it from resources in default or the current theme
                string resourceRef;
                if (Kernel.Instance.HasInternalIcons(Config.Instance.ViewTheme))
                {
                    //cheap way to grab a valid reference to the current themes resources...
                    resourceRef = Application.CurrentInstance.CurrentTheme.PageArea.Substring(0, Application.CurrentInstance.CurrentTheme.PageArea.LastIndexOf("/") + 1);
                }
                else
                {
                    resourceRef = "resx://MediaBrowser/MediaBrowser.Resources/";
                }                    
                return new Image(resourceRef + name);
            }
        }

        public static string FirstCap(string aStr)
        {
            string first = aStr.Substring(0, 1);
            string theRest = aStr.Substring(1);
            return first.ToUpper() + theRest.ToLower();
        }

        /// <summary>
        /// Returns MAC Address from first Network Card in Computer
        /// </summary>
        /// <returns>[string] MAC Address</returns>
        public static string GetMACAddress()
        {
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            string MACAddress = String.Empty;
            foreach (ManagementObject mo in moc)
            {
                if (MACAddress == String.Empty)  // only return MAC Address from first card
                {
                    try
                    {
                        if ((bool)mo["IPEnabled"] == true) MACAddress = mo["MacAddress"].ToString();
                    }
                    catch
                    {
                        mo.Dispose();
                        return "";
                    }
                }
                mo.Dispose();
            }
            MACAddress = MACAddress.Replace(":", "");
            return MACAddress;
        }

        /// <summary>
        /// Get the machine's physical memory in GB
        /// </summary>
        /// <returns></returns>
        public static double GetPhysicalMemory()
        {
            double totalCapacity = 0;
            ObjectQuery objectQuery = new ObjectQuery("select * from Win32_PhysicalMemory");
            ManagementObjectSearcher searcher = new
            ManagementObjectSearcher(objectQuery);
            ManagementObjectCollection vals = searcher.Get();

            foreach (ManagementObject val in vals)
            {
                totalCapacity += System.Convert.ToDouble(val.GetPropertyValue("Capacity"));
            }
            return totalCapacity / 1073741824;
        }
    


        public static int SystemIdleTime
        {
            get
            {
                // Get the system uptime
                int systemUptime = Environment.TickCount;

                // The tick at which the last input was recorded
                int LastInputTicks = 0;

                // The number of ticks that passed since last input
                int IdleTicks = 0;

                // Set the struct
                ShellNativeMethods.LASTINPUTINFO LastInputInfo = new ShellNativeMethods.LASTINPUTINFO();

                LastInputInfo.cbSize = (uint)Marshal.SizeOf(LastInputInfo);

                LastInputInfo.dwTime = 0;



                // If we have a value from the function

                if (ShellNativeMethods.GetLastInputInfo(ref LastInputInfo))
                {
                    // Get the number of ticks at the point when the last activity was seen
                    LastInputTicks = (int)LastInputInfo.dwTime;
                    // Number of idle ticks = system uptime ticks - number of ticks at last input
                    IdleTicks = systemUptime - LastInputTicks;

                }
                return IdleTicks;
            }
        }

        /// <summary>
        /// check the availability of the location and wait for it to be valid up to the timeout (BLOCKING)
        /// </summary>
        /// <param name="location">the location to check (assumed to be a network location)</param>
        /// <param name="timeout">milliseconds to wait if not avail</param>
        internal static bool WaitForLocation(string location, int timeout)
        {
            double elapsed = 0;
            string dir = Path.HasExtension(location) ? Path.GetDirectoryName(location) : location; //try and be sure it is a directory (will give parent if already directory)
            DateTime started = DateTime.Now;
            while (elapsed < timeout && !Directory.Exists(dir))
            {
                Logger.ReportInfo("Unable to access location: " + dir + ". Waiting "+timeout+"ms for it to be available..."+elapsed.ToString("0")+"ms so far...");
                System.Threading.Thread.Sleep(1000);
                var now = DateTime.Now;
                elapsed = (now - started).TotalMilliseconds;
            }
            if (elapsed >= timeout) Logger.ReportWarning("Timed out attempting to access " + dir);
            return elapsed < timeout;
        }
    }
}
