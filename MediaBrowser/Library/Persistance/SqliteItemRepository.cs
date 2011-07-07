﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Interfaces;
using MediaBrowser.Library.Entities;
using System.Data.SQLite;
using MediaBrowser.Library.Configuration;
using System.IO;
using MediaBrowser.Library.Logging;
using System.Reflection;
using System.Threading;
using MediaBrowser.Library.Threading;


namespace MediaBrowser.Library.Persistance {


    public class SqliteItemRepository : SQLiteRepository, IItemRepository {

        private static Guid MIGRATE_MARKER = new Guid("0FD78B4C-B9DA-4249-B880-3696761AE3B4");


        public static SqliteItemRepository GetRepository(string dbPath, string sqlitePath) {
            if (sqliteAssembly == null) {
                sqliteAssembly = System.Reflection.Assembly.LoadFile(sqlitePath);
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(SqliteResolver);
            }

            return new SqliteItemRepository(dbPath);

        }


        // Used to save playstate 
        ItemRepository itemRepo; 

        private SqliteItemRepository(string dbPath) {

            SQLiteConnectionStringBuilder connectionstr = new SQLiteConnectionStringBuilder();
            connectionstr.PageSize = 4096;
            connectionstr.CacheSize = 4096;
            connectionstr.SyncMode = SynchronizationModes.Normal;
            connectionstr.DataSource = dbPath;
            connection = new SQLiteConnection(connectionstr.ConnectionString);
            connection.Open();

            itemRepo = new ItemRepository();


            string playStateDBPath = Path.Combine(ApplicationPaths.AppUserSettingsPath, "playstate.db");

            string[] queries = {"create table if not exists provider_data (guid, full_name, data)",
                                "create unique index if not exists idx_provider on provider_data(guid, full_name)",
                                "create table if not exists items (guid primary key, data)",
                                "create table if not exists children (guid, child)", 
                                "create unique index if not exists idx_children on children(guid, child)",
                                "attach database '"+playStateDBPath+"' as playstate_db",
                                "create table if not exists playstate_db.play_states (guid primary key, play_count, position_ticks, playlist_position, last_played)"
                               // @"create table display_prefs (guid primary key, view_type, show_labels, vertical_scroll 
                               //        sort_order, index_by, use_banner, thumb_constraint_width, thumb_constraint_height, use_coverflow, use_backdrop )" 
                                //,   "create table play_states (guid primary key, play_count, position_ticks, playlist_position, last_played)"
                               };


            foreach (var query in queries) {
                try {

                    connection.Exec(query);
                } catch (Exception e) {
                    Logger.ReportInfo(e.ToString());
                }
            }


            alive = true; // tell writer to keep going
            Async.Queue("Sqlite Writer", DelayedWriter);
 
            if (Kernel.LoadContext == MBLoadContext.Service) MigratePlayState(); //don't want to be migrating simultaneously in service and core...

        }

        private string GetPath(string type, string root) {
            string path = Path.Combine(root, type);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        private void MigratePlayState() {
            if (RetrievePlayState(MIGRATE_MARKER).Id != MIGRATE_MARKER) { //haven't migrated
                //delay this to allow playstate dictionary to load in old repo
                Async.Queue("Playstate Migration", () =>
                //using (ItemRepository repo = new ItemRepository())
                {
                    Logger.ReportInfo("Attempting to migrate playstates to SQL");
                    int cnt = 0;
                    foreach (PlaybackStatus ps in itemRepo.AllPlayStates)
                    {
                        //Logger.ReportVerbose("Saving playstate for " + ps.Id);
                        SavePlayState(ps);
                        cnt++;
                    }
                    Logger.ReportInfo("Successfully migrated " + cnt + " playstate items.");
                    var migrateMarker = new PlaybackStatus();
                    migrateMarker.Id = MIGRATE_MARKER;
                    SavePlayState(migrateMarker);
                }, 5000);
            }
        }

        public PlaybackStatus RetrievePlayState(Guid id) {
            var cmd = connection.CreateCommand();
            cmd.CommandText = "select guid, play_count, position_ticks, playlist_position, last_played from playstate_db.play_states where guid = @guid";
            cmd.AddParam("@guid", id);

            var state = new PlaybackStatus();
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    state.Id = reader.GetGuid(0);
                    state.PlayCount = reader.GetInt32(1);
                    state.PositionTicks = reader.GetInt64(2);
                    state.PlaylistPosition = reader.GetInt32(3);
                    state.LastPlayed = reader.GetDateTime(4);
                }
            }

            return state;
        }

        public ThumbSize RetrieveThumbSize(Guid id)
        {
            return itemRepo.RetrieveThumbSize(id);
        }

        public void SavePlayState(PlaybackStatus playState) {
            var cmd = connection.CreateCommand();
            cmd.CommandText = "replace into playstate_db.play_states(guid, play_count, position_ticks, playlist_position, last_played) values(@guid, @playCount, @positionTicks, @playlistPosition, @lastPlayed)";
            cmd.AddParam("@guid", playState.Id);
            cmd.AddParam("@playCount", playState.PlayCount);
            cmd.AddParam("@positionTicks", playState.PositionTicks);
            cmd.AddParam("@playlistPosition", playState.PlaylistPosition);
            cmd.AddParam("@lastPlayed", playState.LastPlayed);

            QueueCommand(cmd);
        }


        public void SaveChildren(Guid id, IEnumerable<Guid> children) {

            Guid[] childrenCopy;
            lock (children) {
                childrenCopy = children.ToArray();
            }

            var cmd = connection.CreateCommand();

            cmd.CommandText = "delete from children where guid = @guid";
            cmd.AddParam("@guid", id);

            QueueCommand(cmd);

            foreach (var guid in children) {
                cmd = connection.CreateCommand();
                cmd.AddParam("@guid", id);
                cmd.CommandText = "insert into children (guid, child) values (@guid, @child)";
                var childParam = cmd.Parameters.Add("@child", System.Data.DbType.Guid);

                childParam.Value = guid;
                QueueCommand(cmd);
            }
        }


        public IEnumerable<Guid> RetrieveChildren(Guid id) {

            List<Guid> children = new List<Guid>();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "select child from children where guid = @guid";
            var guidParam = cmd.Parameters.Add("@guid", System.Data.DbType.Guid);
            guidParam.Value = id;

            using (var reader = cmd.ExecuteReader()) {
                while (reader.Read()) {
                    children.Add(reader.GetGuid(0));
                }
            }

            return children.Count == 0 ? null : children;
        }

        public DisplayPreferences RetrieveDisplayPreferences(DisplayPreferences dp) {
            return itemRepo.RetrieveDisplayPreferences(dp);
        }


        public void SaveDisplayPreferences(DisplayPreferences prefs) {
            itemRepo.SaveDisplayPreferences(prefs);
        }

        public BaseItem RetrieveItem(Guid id) {

            var cmd = connection.CreateCommand();
            cmd.CommandText = "select data from items where guid = @guid";
            cmd.AddParam("@guid", id);

            BaseItem item = null;

            using (var reader = cmd.ExecuteReader()) {
                if (reader.Read()) {
                    var data = reader.GetBytes(0);
                    using (var stream = new MemoryStream(data)) {
                        item = Serializer.Deserialize<BaseItem>(stream);
                    }
                }
            }
            return item;
        }

        public void SaveItem(BaseItem item) {
            using (var fs = new MemoryStream()) {
                BinaryWriter bw = new BinaryWriter(fs);
                Serializer.Serialize(bw.BaseStream, item);

                var cmd = connection.CreateCommand();
                cmd.CommandText = "replace into items(guid, data) values (@guid, @data)";

                SQLiteParameter guidParam = new SQLiteParameter("@guid");
                SQLiteParameter dataParam = new SQLiteParameter("@data");

                cmd.Parameters.Add(guidParam);
                cmd.Parameters.Add(dataParam);

                guidParam.Value = item.Id;
                dataParam.Value = fs.ToArray();

                QueueCommand(cmd);

            }
        }



        public IEnumerable<IMetadataProvider> RetrieveProviders(Guid guid) {
            var providers = new List<IMetadataProvider>();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "select data from provider_data where guid = @guid";
            var guidParam = cmd.Parameters.Add("@guid", System.Data.DbType.Guid);
            guidParam.Value = guid;

            using (var reader = cmd.ExecuteReader()) {
                while (reader.Read()) {
                    using (var ms = new MemoryStream(reader.GetBytes(0))) {

                        var data = (IMetadataProvider)Serializer.Deserialize<object>(ms);
                        providers.Add(data);
                    }
                }
            }

            return providers.Count == 0 ? null : providers;
        }

        public void SaveProviders(Guid guid, IEnumerable<IMetadataProvider> providers) {

            IMetadataProvider[] providerCopy;
            lock (providers) {
                providerCopy = providers.ToArray();
            }
            lock (delayedCommands) {
                var cmd = connection.CreateCommand();

                cmd.CommandText = "delete from provider_data where guid = @guid";
                cmd.AddParam("@guid", guid);
                QueueCommand(cmd);

                foreach (var provider in providerCopy) {
                    cmd = connection.CreateCommand();
                    cmd.CommandText = "insert into provider_data (guid, full_name, data) values (@guid, @full_name, @data)";
                    cmd.AddParam("@guid", guid);
                    cmd.AddParam("@full_name", provider.GetType().FullName);
                    var dataParam = cmd.AddParam("@data");


                    using (var ms = new MemoryStream()) {
                        Serializer.Serialize(ms, (object)provider);
                        dataParam.Value = ms.ToArray();
                        QueueCommand(cmd);
                    }
                }
            }
        }

        public bool ClearEntireCache() {
            lock (connection) {
                var tran = connection.BeginTransaction();
                connection.Exec("delete from provider_data"); 
                connection.Exec("delete from items");
                connection.Exec("delete from children");
                //connection.Exec("delete from display_prefs");
                // People will get annoyed if this is lost
                // connection.Exec("delete from play_states");
                tran.Commit(); 
                connection.Exec("vacuum");
            }

            return true;
        }

    }
}
