﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Interfaces;
using MediaBrowser.Library.Entities;
using System.Diagnostics;

namespace MediaBrowser.Library.Persistance {
    public class SafeItemRepository : IItemRepository {

        IItemRepository repository;

        public SafeItemRepository (IItemRepository repository)
	    {
            this.repository = repository;
	    }

        static T SafeFunc<T>(Func<T> func) {
            T obj = default(T);
            try {
                obj = func();
            } catch (Exception ex) {
                Application.Logger.ReportException("Failed to access repository ", ex);
            }
            return obj;
        }

        static void SafeAction(Action action) {
            try {
                action();
            } catch (Exception ex) {
                Application.Logger.ReportException("Failed to access repository ", ex);
            }

        }

        public IEnumerable<IMetadataProvider> RetrieveProviders(Guid guid) {
            return SafeFunc(() => repository.RetrieveProviders(guid));
        }

        public void SaveProviders(Guid guid, IEnumerable<IMetadataProvider> providers) {
            SafeAction(() => repository.SaveProviders(guid, providers));
        }

        public void SaveItem(BaseItem item) {
            SafeAction(() => repository.SaveItem(item));
        }

        public BaseItem RetrieveItem(Guid guid) {
            return SafeFunc(() => repository.RetrieveItem(guid));
        }

        public void SaveChildren(Guid ownerName, IEnumerable<Guid> children) {
            SafeAction(() => repository.SaveChildren(ownerName, children));
        }

        public IEnumerable<Guid> RetrieveChildren(Guid id) {
            return SafeFunc(() => repository.RetrieveChildren(id));
        }

        public PlaybackStatus RetrievePlayState(Guid id) {
            return SafeFunc(() => repository.RetrievePlayState(id));
        }

        public DisplayPreferences RetrieveDisplayPreferences(Guid id) {
            return SafeFunc(() => repository.RetrieveDisplayPreferences(id));
        }

        public void SavePlayState(PlaybackStatus playState) {
            SafeAction(() => repository.SavePlayState(playState));
        }

        public void SaveDisplayPreferences(DisplayPreferences prefs) {
            SafeAction(() => repository.SaveDisplayPreferences(prefs));
        }

        public void CleanCache() {
            SafeAction(() => repository.CleanCache());
        }

        public bool ClearEntireCache() {
            return SafeFunc(() => repository.ClearEntireCache());
        }

    }
}
