using System;
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

    static class SqliteExtensions {

        public static bool TableExists(this SQLiteConnection cnn, string table) {
            var cmd = cnn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE name=@name";
            cmd.Parameters.Add(new SQLiteParameter("@name", table)); 
            using (var reader = cmd.ExecuteReader()) {
                return reader.Read();
            }
        }

        public static int Exec(this SQLiteConnection cnn, string sql) {
            var cmd = cnn.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteNonQuery();
        }

        public static int Exec(this SQLiteCommand cmd, string sql) {
            cmd.CommandText = sql;
            return cmd.ExecuteNonQuery();
        }

        public static SQLiteParameter AddParam(this SQLiteCommand cmd, string param) {
            var sqliteParam = new SQLiteParameter(param);
            cmd.Parameters.Add(sqliteParam);
            return sqliteParam;
        }

        public static SQLiteParameter AddParam(this SQLiteCommand cmd, string param, object data) {
            var sqliteParam = AddParam(cmd, param);
            sqliteParam.Value = data;
            return sqliteParam;
        }


        public static byte[] GetBytes(this SQLiteDataReader reader, int col) {
            byte[] buffer = new byte[8000];
            using (var ms = new MemoryStream()) {
                long read = 0;
                long offset = 0;
                while ((read = reader.GetBytes(col, offset, buffer, 0, buffer.Length)) > 0) {
                    offset += read;
                    ms.Write(buffer, 0, (int)read);
                }
                return ms.GetBuffer();
            }
        }
    }

    public class SqliteItemRepository : IItemRepository {


        static System.Reflection.Assembly sqliteAssembly;
        static System.Reflection.Assembly SqliteResolver(object sender, ResolveEventArgs args) {
            Logger.ReportInfo(args.Name + " is being resolved!");
            if (args.Name.StartsWith("System.Data.SQLite,")) {
                return sqliteAssembly;
            }
            return null;
        }

        public static SqliteItemRepository GetRepository(string dbPath, string sqlitePath) {
            if (sqliteAssembly == null) {
                sqliteAssembly = System.Reflection.Assembly.LoadFile(sqlitePath);
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(SqliteResolver);
            }

            return new SqliteItemRepository(dbPath);

        }


        SQLiteConnection connection;
        List<SQLiteCommand> delayedCommands = new List<SQLiteCommand>();


        ManualResetEvent flushing = new ManualResetEvent(false);

        private void DelayedWriter() {
            while (true) {
                flushing.Reset(); 
                InternalFlush();
                flushing.Set();

                Thread.Sleep(1000); 
            }
        }

        private void InternalFlush() {
            try {

                List<SQLiteCommand> copy;
                lock (delayedCommands) {
                    copy = delayedCommands.ToList();
                    delayedCommands.Clear();
                }

                lock (connection) {
                    var tran = connection.BeginTransaction();
                    foreach (var command in copy) {
                        command.Transaction = tran;
                        command.ExecuteNonQuery();
                    }
                    tran.Commit();
                }

            } catch (Exception e) {
                Logger.ReportException("Critical Exception Failed to Flush:", e);
            }
        }

        public void FlushWriter() {
            InternalFlush();
            flushing.WaitOne();
        }

        // Used to save playstate 
        ItemRepository itemRepo; 

        private SqliteItemRepository(string dbPath) {

            connection = new SQLiteConnection("Data Source=" + dbPath);
            connection.Open();

            itemRepo = new ItemRepository();

            MigratePlayState();

            string[] queries = {"create table provider_data (guid, full_name, data)",
                                "create unique index idx_provider on provider_data(guid, full_name)",
                                "create table items (guid primary key, data)",
                                "create table children (guid, child)", 
                                "create unique index idx_children on children(guid, child)",
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

            

            Async.Queue("Sqlite Writer", DelayedWriter); 

        }

        private void MigratePlayState() {
            if (connection.TableExists("play_states")) {
                var cmd = connection.CreateCommand();
                cmd.CommandText = "select guid, play_count, position_ticks, playlist_position, last_played from play_states";
                using (var reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        var state = new PlaybackStatus();
                        state.Id = reader.GetGuid(0);
                        state.PlayCount = reader.GetInt32(1);
                        state.PositionTicks = reader.GetInt64(2);
                        state.PlaylistPosition = reader.GetInt32(3);
                        state.LastPlayed = reader.GetDateTime(4);
                        try {
                            SavePlayState(state);
                        } catch (Exception e) {
                            Logger.ReportException("Failed to migrate playstate for : " + state.Id.ToString(), e);
                        }
                    }
                }
                connection.Exec("drop table play_states");
                Logger.ReportInfo("Successfully migrated Sqlite display state to Item Cache");
            }
        }

        public PlaybackStatus RetrievePlayState(Guid id) {
            return itemRepo.RetrievePlayState(id);
        }


        public void SavePlayState(PlaybackStatus playState) {
            itemRepo.SavePlayState(playState);
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



        private void QueueCommand(SQLiteCommand cmd) {
            lock (delayedCommands) {
                delayedCommands.Add(cmd);
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

        public DisplayPreferences RetrieveDisplayPreferences(Guid id) {
            return itemRepo.RetrieveDisplayPreferences(id);
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
                connection.Exec("delete from display_prefs");
                // People will get annoyed if this is lost
                // connection.Exec("delete from play_states");
                tran.Commit(); 
            }

            return true;
        }

    }
}
