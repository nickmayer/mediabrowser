﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Entities;
using MediaBrowser.Library.Extensions;
using MediaBrowser.Library.Interfaces;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Providers.Attributes;
using System.Collections;
using MediaBrowser.Library.Util;
using System.Diagnostics;
using MediaBrowser.Library.Entities.Attributes;
using System.Threading;
using MediaBrowser.Library.Plugins;
using MediaBrowser.Library.Providers.TVDB;
using MediaBrowser.Library.Providers;
using MediaBrowser.Library.Factories;
using MediaBrowser.Library.Logging;

namespace MediaBrowser.Library.Metadata {
    public class MetadataProviderHelper {


        static object sync = new object();
 
        public static Type[] ProviderTypes { 
            get { 
                return Kernel.Instance.MetadataProviderFactories.Select(p => p.Type).ToArray(); 
            } 
        }

        public static List<MetadataProviderFactory> DefaultProviders() {

            return new Type[] { 
                typeof(VirtualFolderProvider),
                typeof(ImageFromMediaLocationProvider),
                typeof(ImageByNameProvider), 
                typeof(MovieProviderFromXml),
                typeof(FolderProviderFromXml),
                typeof(LocalEpisodeProvider), 
                typeof(LocalSeriesProvider), 
                typeof(RemoteEpisodeProvider),
                typeof(RemoteSeasonProvider), 
                typeof(RemoteSeriesProvider),
                typeof(MovieDbProvider)
            }.Select(t => new MetadataProviderFactory(t)).ToList(); 
            
        }


        public static bool UpdateMetadata(BaseItem item, MetadataRefreshOptions options) {

            bool force = (options & MetadataRefreshOptions.Force) == MetadataRefreshOptions.Force;
            bool fastOnly = (options & MetadataRefreshOptions.FastOnly) == MetadataRefreshOptions.FastOnly;

            bool changed = false;
            if (force) {
                ClearItem(item); 
            }
            var providers = GetSupportedProviders(item);

            var itemClone = (BaseItem)Serializer.Clone(item);
            // Parent is not serialized so its not cloned
            itemClone.Parent = item.Parent; 

            foreach (var provider in providers) {
                provider.Item = itemClone;
            }

            if (force || NeedsRefresh(providers, fastOnly)) {

                // something changed clear the item before pulling metadata 
                if (!force) {
                    ClearItem(item);
                    ClearItem(itemClone);
                }

                // we must clear the provider data as well in case it is bad or out of date! 
                foreach (var provider in providers) {
                    ClearItem(provider);
                }

                Logger.ReportInfo("Metadata changed for the following item {0} (first pass : {1} forced via UI : {2})", item.Name, fastOnly, force);
                changed = UpdateMetadata(item, true, fastOnly, providers);
            }
      
            return changed;
        }

        /// <summary>
        /// Clear all the persistable parts of the entitiy excluding parts that are updated during initialization
        /// </summary>
        /// <param name="item"></param>
        private static void ClearItem(object item) {
            foreach (var persistable in Serializer.GetPersistables(item)) {
                if (persistable.GetAttributes<NotSourcedFromProviderAttribute>() == null) {
                    persistable.SetValue(item, null);
                }
            }
        }

        static bool NeedsRefresh(IList<IMetadataProvider> supportedProviders, bool fastOnly) {
            foreach (var provider in supportedProviders) {
                try {
                    if ((provider.IsSlow || provider.RequiresInternet) && fastOnly) continue;
                    if (provider.NeedsRefresh())
                        return true;
                } catch (Exception e) {
                    Logger.ReportException("Metadata provider failed during NeedsRefresh", e);
                    Debug.Assert(false, "Providers should catch all the exceptions that NeedsRefresh generates!");
                }
            }
            return false;
        }

        static IList<IMetadataProvider> GetSupportedProviders(BaseItem item) {

            var cachedProviders = (Kernel.Instance.ItemRepository.RetrieveProviders(item.Id) ?? new List<IMetadataProvider>())
                .ToDictionary(provider => provider.GetType());

            return Kernel.Instance.MetadataProviderFactories
                .Where(provider => provider.Supports(item))
                .Where(provider => !provider.RequiresInternet || Config.Instance.AllowInternetMetadataProviders)
                .Select(provider => cachedProviders.GetValueOrDefault(provider.Type, provider.Construct()))
                .ToList();
        }


        static bool UpdateMetadata(
            BaseItem item,
            bool force,
            bool fastOnly,
            IEnumerable<IMetadataProvider> providers
            ) 
        {
            bool changed = false;

            foreach (var provider in providers) {

                if ((provider.IsSlow || provider.RequiresInternet) && fastOnly) continue;

                try {
                    if (provider.NeedsRefresh() | force) {
                        provider.Fetch();
                        Serializer.Merge(provider.Item, item);
                        changed = true;
                    }
                } catch (Exception e) {
                    Debug.Assert(false, "Meta data provider should not be leaking exceptions");
                    Logger.ReportException("Provider failed: " + provider.GetType().ToString(), e);
                }
            }
            if (changed) {
                Kernel.Instance.ItemRepository.SaveItem(item);
                Kernel.Instance.ItemRepository.SaveProviders(item.Id, providers);
            }

            return changed;
        }
    }
}
