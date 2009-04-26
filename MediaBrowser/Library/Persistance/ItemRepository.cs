﻿using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.LibraryManagement;
using System.IO;
using System.Diagnostics;
using System.Threading;
using MediaBrowser.Util;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Linq;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Util;
using System.Reflection;
using System.Drawing;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Filesystem;
using MediaBrowser.Library.Interfaces;

namespace MediaBrowser.Library {
    class ItemRepository : IItemRepository, IDisposable {
        public ItemRepository() {
        }

        string rootPath = Helper.AppCachePath;

        #region IItemCacheProvider Members

        public void SaveChildren(Guid id, IEnumerable<Guid> children) {
            string file = GetChildrenFilename(id);

            using (Stream fs = WriteExclusiveFileStream(file)) {
                BinaryWriter bw = new BinaryWriter(fs);
                lock (children) {
                    bw.Write(children.Count());
                    foreach (var guid in children) {
                        bw.Write(guid);
                    }
                }
                fs.Close();
            }

        }

        public IEnumerable<Guid> RetrieveChildren(Guid id) {

            List<Guid> children = new List<Guid>();
            string file = GetChildrenFilename(id);
            if (!File.Exists(file)) return null;

            try {

                using (Stream fs = ReadFileStream(file)) {
                    BinaryReader br = new BinaryReader(fs);
                    lock (children) {
                        var count = br.ReadInt32();
                        var itemsRead = 0;
                        while (itemsRead < count) {
                            children.Add(br.ReadGuid());
                            itemsRead++;
                        }
                    }
                    fs.Close();
                }
            } catch (Exception e) {
                Application.Logger.ReportException("Failed to retrieve children:", e);
#if DEBUG
                throw;
#else 
                return null;
#endif

            }

            return children.Count == 0 ? null : children;
        }


        public PlaybackStatus RetrievePlayState(Guid id) {
            string file = GetFile("playstate", id);
            byte[] bytes = null;
            if (!File.Exists(file)) return null;

            using (var fs = ReadFileStream(file)) {
                bytes = fs.ReadAllBytes();
            }
   
            try {
                using (MemoryStream ms = new MemoryStream(bytes)) {
                    var state = Serializer.Deserialize<PlaybackStatus>(ms);
                    if (state.Id == id) {
                        return state;
                    }
                }
            } catch (SerializationException e) {
                Application.Logger.ReportException("Play state information was corrupt, deleting it.", e);
                File.Delete(file);
            }

            return null;
        }

        public DisplayPreferences RetrieveDisplayPreferences(Guid id) {
            string file = GetFile("display", id);

            if (File.Exists(file)) {
                using (Stream fs = ReadFileStream(file)) {
                    DisplayPreferences dp = DisplayPreferences.ReadFromStream(id, new BinaryReader(fs));
                    return dp;
                }
            } 

            return null;
        }

        public void SavePlayState(PlaybackStatus playState) {
            string file = GetFile("playstate", playState.Id);
            using (Stream fs = WriteExclusiveFileStream(file)) {
                Serializer.Serialize(fs, playState);
            }
        }

        public void SaveDisplayPreferences(DisplayPreferences prefs) {
            string file = GetFile("display", prefs.Id);
            using (Stream fs = WriteExclusiveFileStream(file)) {
                prefs.WriteToStream(new BinaryWriter(fs));
                fs.Close();
            }
        }

        public BaseItem RetrieveItem(Guid id) {
            BaseItem item = null;
            string file = GetItemFilename(id);
            if (!File.Exists(file)) return null;

            using (Stream fs = ReadFileStream(file)) {
                using (BinaryReader reader = new BinaryReader(fs)) {
                    item = Serializer.Deserialize<BaseItem>(fs);
                }
            }

            return item;
        }

        public void SaveItem(BaseItem item) {
            string file = GetItemFilename(item.Id);
            using (Stream fs = WriteExclusiveFileStream(file)) {
                using (BinaryWriter bw = new BinaryWriter(fs)) {
                    Serializer.Serialize(bw.BaseStream, item);
                }
            }
        }



        public IMetadataProvider RetrieveProvider(Guid guid) {
            IMetadataProvider data = null;
            string file = GetProviderFilename(guid);
            if (!File.Exists(file)) return null;

            using (Stream fs = ReadFileStream(file)) {
                using (BinaryReader reader = new BinaryReader(fs)) {
                    data = (IMetadataProvider)Serializer.Deserialize<object>(fs);
                }
            }

            return data;
        }

        public void SaveProvider(Guid guid, IMetadataProvider provider) {
            string file = GetProviderFilename(guid);
            using (Stream fs = WriteExclusiveFileStream(file)) {
                using (BinaryWriter bw = new BinaryWriter(fs)) {
                    Serializer.Serialize<object>(bw.BaseStream, provider);
                }
            }
        }


        private static Stream WriteExclusiveFileStream(string file) {
            return ProtectedFileStream.OpenExclusiveWriter(file);
        }

        private static Stream ReadFileStream(string file) {
            return ProtectedFileStream.OpenSharedReader(file);
        }

        public void CleanCache() {

        }

        private string GetChildrenFilename(Guid id) {
            return GetFile("children", id);
        }

        private string GetItemFilename(Guid id) {
            return GetFile("items", id);
        }

        private string GetProviderFilename(Guid id) {
            return GetFile("providerdata", id);
        }


        private string GetFile(string type, Guid id) {
            string root = this.rootPath;
            string path = Path.Combine(root, type);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return Path.Combine(path, id.ToString("N"));
        }

        public bool ClearEntireCache() {
            bool success = true;
            lock (ProtectedFileStream.GlobalLock) {
                success &= DeleteFolder(Path.Combine(Helper.AppCachePath, "items"));
                success &= DeleteFolder(Path.Combine(Helper.AppCachePath, "providerdata"));
                success &= DeleteFolder(Path.Combine(Helper.AppCachePath, "images"));
                success &= DeleteFolder(Path.Combine(Helper.AppCachePath, "autoplaylists"));
                success &= DeleteFolder(Path.Combine(Helper.AppCachePath, "children"));
            }
            return success;
        }

        private bool DeleteFolder(string p) {
            try {
                Directory.Delete(p, true);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose() {
            GC.SuppressFinalize(this);
        }

        #endregion




    }
}
