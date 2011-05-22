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

namespace MediaBrowser.Library.Persistance
{
    static class SqliteExtensions
    {

        public static bool TableExists(this SQLiteConnection cnn, string table)
        {
            var cmd = cnn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE name=@name";
            cmd.Parameters.Add(new SQLiteParameter("@name", table));
            using (var reader = cmd.ExecuteReader())
            {
                return reader.Read();
            }
        }

        public static int Exec(this SQLiteConnection cnn, string sql)
        {
            var cmd = cnn.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteNonQuery();
        }

        public static int Exec(this SQLiteCommand cmd, string sql)
        {
            cmd.CommandText = sql;
            return cmd.ExecuteNonQuery();
        }

        public static SQLiteParameter AddParam(this SQLiteCommand cmd, string param)
        {
            var sqliteParam = new SQLiteParameter(param);
            cmd.Parameters.Add(sqliteParam);
            return sqliteParam;
        }

        public static SQLiteParameter AddParam(this SQLiteCommand cmd, string param, object data)
        {
            var sqliteParam = AddParam(cmd, param);
            sqliteParam.Value = data;
            return sqliteParam;
        }


        public static byte[] GetBytes(this SQLiteDataReader reader, int col)
        {
            byte[] buffer = new byte[8000];
            using (var ms = new MemoryStream())
            {
                long read = 0;
                long offset = 0;
                while ((read = reader.GetBytes(col, offset, buffer, 0, buffer.Length)) > 0)
                {
                    offset += read;
                    ms.Write(buffer, 0, (int)read);
                }
                return ms.GetBuffer();
            }
        }
    }

    public class SQLiteRepository
    {
        protected static System.Reflection.Assembly sqliteAssembly;
        protected static System.Reflection.Assembly SqliteResolver(object sender, ResolveEventArgs args)
        {
            Logger.ReportInfo(args.Name + " is being resolved!");
            if (args.Name.StartsWith("System.Data.SQLite,"))
            {
                return sqliteAssembly;
            }
            return null;
        }

        protected SQLiteConnection connection;
        protected List<SQLiteCommand> delayedCommands = new List<SQLiteCommand>();

        public void ShutdownWriter()
        {
            alive = false;
            Thread.Sleep(1000); //wait for it to shutdown
        }

        protected void QueueCommand(SQLiteCommand cmd)
        {
            lock (delayedCommands)
            {
                delayedCommands.Add(cmd);
            }
        }

        ManualResetEvent flushing = new ManualResetEvent(false);

        protected bool alive = true;

        protected void DelayedWriter()
        {
            while (alive)
            {
                flushing.Reset();
                InternalFlush();
                flushing.Set();

                Thread.Sleep(1000);
            }
        }

        private void InternalFlush()
        {
            try
            {

                List<SQLiteCommand> copy;
                lock (delayedCommands)
                {
                    copy = delayedCommands.ToList();
                    delayedCommands.Clear();
                }

                lock (connection)
                {
                    var tran = connection.BeginTransaction();
                    foreach (var command in copy)
                    {
                        command.Transaction = tran;
                        command.ExecuteNonQuery();
                    }
                    tran.Commit();
                }

            }
            catch (Exception e)
            {
                Logger.ReportException("Critical Exception Failed to Flush:", e);
            }
        }

        public void FlushWriter()
        {
            InternalFlush();
            flushing.WaitOne();
        }


    }
}