﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dapper.Sharding
{
    internal class SQLiteClient : IClient
    {
        public SQLiteClient(DataBaseConfig config) : base(DataBaseType.Sqlite, config)
        {
            ConnectionString = config.Server;
        }

        public override string ConnectionString { get; }

        #region protected method

        protected string GetFileName(string name)
        {
            if (!Directory.Exists(ConnectionString))
            {
                Directory.CreateDirectory(ConnectionString);
            }
            return Path.Combine(ConnectionString, name);
        }

        protected override IDatabase CreateIDatabase(string name)
        {
            return new SQLiteDatabase(name, this);
        }

        public override IDatabase GetDatabase(string name, bool useGis = false, string gisExt = null)
        {
            if (!DataBaseCache.ContainsKey(name))
            {
                lock (Locker.GetObject(name))
                {
                    if (!DataBaseCache.ContainsKey(name))
                    {
                        CreateDatabase(name);
                        DataBaseCache.TryAdd(name, CreateIDatabase(name));
                    }
                }
            }
            return DataBaseCache[name];
        }

        #endregion

        public override IDbConnection GetConn()
        {
            throw new NotImplementedException();
        }

        public override Task<IDbConnection> GetConnAsync()
        {
            throw new NotImplementedException();
        }

        public override void CreateDatabase(string name, bool useGis = false, string gisExt = null)
        {
            var fileName = GetFileName(name);
            if (!File.Exists(fileName))
            {
                SQLiteConnection.CreateFile(fileName);
            }
        }

        public override void DropDatabase(string name)
        {
            var fileName = GetFileName(name);
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            DataBaseCache.TryRemove(name.ToLower(), out _);
        }

        public override bool ExistsDatabase(string name)
        {
            var fileName = GetFileName(name);
            return File.Exists(fileName);
        }

        public override IEnumerable<string> ShowDatabases()
        {
            return Directory.GetFiles(ConnectionString).Select(s => Path.GetFileName(s));
        }

        public override IEnumerable<string> ShowDatabasesExcludeSystem()
        {
            return ShowDatabases();
        }


    }
}
