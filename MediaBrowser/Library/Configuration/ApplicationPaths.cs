﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MediaBrowser.Library.Configuration {
    public static class ApplicationPaths {

        [DllImport("MSI.DLL", CharSet = CharSet.Auto)]
        private static extern UInt32 MsiGetComponentPath(
            string szProduct,
            string szComponent,
            StringBuilder lpPathBuf,
            ref int pcchBuf);

        private const string INSTALL_PRODUCT_CODE = "89A08369-DD80-41c6-966E-A8A057B03FFF";
        private const string SERVICE_COMPONENT_ID = "D19774DD-253A-47E2-94FA-3EFB220E2B77";
        private const string CONFIGURATOR_COMPONENT_ID = "3186FBE0-641C-45f9-AA16-7EAA85B7405C";

        private static string GetComponentPath(string product, string component)
        {
            int pathLength = 1024;
            StringBuilder path = new StringBuilder(pathLength);
            MsiGetComponentPath(product, component, path, ref pathLength);
            return path.ToString();
        }

        static Dictionary<string, string> pathMap;

        static string[,] tree = { 
                    { "AppConfigPath",       "app_data",         "MediaBrowser"  }, 
                    { "AppCachePath",        "AppConfigPath",    "Cache"         },
                    { "AppUserSettingsPath", "AppConfigPath",    "Cache"           },
                    { "AutoPlaylistPath",    "AppCachePath",     "autoPlaylists" }, 
                    { "AppImagePath",        "AppConfigPath",    "ImageCache"},
                    { "AppInitialDirPath",   "AppConfigPath",    "StartupFolder" },
                    { "AppPluginPath",       "AppConfigPath",    "Plugins" },
                    { "AppRSSPath",          "AppConfigPath",    "RSS"},
                    { "AppLogPath",          "AppConfigPath",    "Logs"},
                    { "DefaultPodcastPath", "AppConfigPath", "Podcasts"    },
                    { "AppLocalizationPath","AppConfigPath", "Localization" },
                    { "PluginConfigPath", "AppPluginPath", "Configurations"}
            };


        static ApplicationPaths() {
            pathMap = new Dictionary<string, string>();
            pathMap["app_data"] = System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData);
            
            BuildTree();
        }

        static void BuildTree() {
            for (int i = 0; i <= tree.GetUpperBound(0); i++) {
                var e = Path.Combine(pathMap[tree[i, 1]], tree[i, 2]);
                if (!Directory.Exists(e)) {
                    Directory.CreateDirectory(e);
                }
                pathMap[tree[i, 0]] = e;
            }
        }


        public static void SetUserSettingsPath(string path) {
            Debug.Assert(Directory.Exists(path));

            pathMap["AppUserSettingsPath"] = path;
        }

        public static string DefaultPodcastPath {
            get {
                return pathMap["DefaultPodcastPath"];
            } 
        } 

        public static string AppLogPath {
            get {
                return pathMap["AppLogPath"];
            }
        }

        public static string AppPluginPath {
            get {
                return pathMap["AppPluginPath"];
            }
        }

        public static string AppImagePath {
            get {
                return pathMap["AppImagePath"];
            }
        }

        public static string AppInitialDirPath {
            get {
                return pathMap["AppInitialDirPath"];
            }
        }

        public static string AppConfigPath {
            get {
                return pathMap["AppConfigPath"];
            }
        }

        public static string AppCachePath {
            get {
                return pathMap["AppCachePath"];
            }
        }

        public static string PluginConfigPath
        {
            get
            {
                return pathMap["PluginConfigPath"];
            }
        }



        public static string AutoPlaylistPath {
            get {
                return pathMap["AutoPlaylistPath"];
            }
        }

        public static string AppUserSettingsPath {
            get {
                return pathMap["AppUserSettingsPath"];
            }
        }


        public static string ConfigFile {
            get {
                var path = AppConfigPath;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return Path.Combine(path, "MediaBrowserXml.config");
            }
        }

        public static string ServiceConfigFile
        {
            get
            {
                var path = AppConfigPath;
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return Path.Combine(path, "MBServiceXml.config");
            }
        }

        public static string ServiceExecutableFile
        {
            get
            {
                string serviceEXE = GetComponentPath(INSTALL_PRODUCT_CODE, SERVICE_COMPONENT_ID);
                return serviceEXE.Length > 0 ? serviceEXE : Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES(X86)") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MediaBrowser\\MediaBrowser\\MediaBrowserService.exe");
            }
        }

        public static string ConfiguratorExecutableFile
        {
            get
            {
                string configEXE = GetComponentPath(INSTALL_PRODUCT_CODE, CONFIGURATOR_COMPONENT_ID);
                return configEXE.Length > 0 ? configEXE : Path.Combine(Environment.GetEnvironmentVariable("PROGRAMFILES(X86)") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MediaBrowser\\MediaBrowser\\configurator.exe");
            }
        }

        public static string AppRSSPath
        {
            get {
                return pathMap["AppRSSPath"];
            }
        }
        public static string AppLocalizationPath
        {
            get
            {
                return pathMap["AppLocalizationPath"];
            }
        }

    }
}
