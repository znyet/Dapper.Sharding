using System;
using System.Collections.Generic;
using System.Data;
#if !CORE
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapper.Sharding
{
    internal class SqlServerClient : IClient
    {

        public SqlServerClient(DataBaseConfig config, DataBaseVersion version) : base(DataBaseType.SqlServer, config, version)
        {
            if (!string.IsNullOrEmpty(config.Database_Path))
            {
                if (!Directory.Exists(config.Database_Path))
                {
                    Directory.CreateDirectory(config.Database_Path);
                }
            }
            ConnectionString = ConnectionStringBuilder.BuilderSqlServer(config);
        }

        public override string ConnectionString { get; set; }


        #region protected method

        protected override IDatabase CreateIDatabase(string name)
        {
            return new SqlServerDatabase(name, this);
        }

        #endregion

        public override string GetDatabaseScript(string name, bool useGis = false, string ext = null)
        {
            var sb = new StringBuilder();
            sb.Append($"IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name='{name}') CREATE DATABASE [{name}] ");
            if (!string.IsNullOrEmpty(Config.Database_Path))
            {
                sb.Append($"ON (NAME={name},FILENAME='{Path.Combine(Config.Database_Path, name)}.mdf'");
                if (Config.Database_Size_Mb != 0)
                {
                    sb.Append($",SIZE={Config.Database_Size_Mb}MB");
                }
                if (Config.Database_SizeGrowth_Mb != 0)
                {
                    sb.Append($",FILEGROWTH={Config.Database_SizeGrowth_Mb}MB");
                }
                sb.Append(")");
                sb.Append($"LOG ON (NAME={name}_log,FILENAME='{Path.Combine(Config.Database_Path, name)}_log.ldf'");
                if (Config.Database_LogSize_Mb != 0)
                {
                    sb.Append($",SIZE={Config.Database_LogSize_Mb}MB");
                }
                if (Config.Database_LogSizGrowth_Mb != 0)
                {
                    sb.Append($",FILEGROWTH={Config.Database_LogSizGrowth_Mb}MB");
                }
                sb.Append(")");
            }

            return sb.ToString();
        }

        public override void CreateDatabase(string name, bool useGis = false, string ext = null)
        {
            var sb = new StringBuilder();
            sb.Append($"IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name='{name}') CREATE DATABASE [{name}] ");
            if (!string.IsNullOrEmpty(Config.Database_Path))
            {
                sb.Append($"ON (NAME={name},FILENAME='{Path.Combine(Config.Database_Path, name)}.mdf'");
                if (Config.Database_Size_Mb != 0)
                {
                    sb.Append($",SIZE={Config.Database_Size_Mb}MB");
                }
                if (Config.Database_SizeGrowth_Mb != 0)
                {
                    sb.Append($",FILEGROWTH={Config.Database_SizeGrowth_Mb}MB");
                }
                sb.Append(")");
                sb.Append($"LOG ON (NAME={name}_log,FILENAME='{Path.Combine(Config.Database_Path, name)}_log.ldf'");
                if (Config.Database_LogSize_Mb != 0)
                {
                    sb.Append($",SIZE={Config.Database_LogSize_Mb}MB");
                }
                if (Config.Database_LogSizGrowth_Mb != 0)
                {
                    sb.Append($",FILEGROWTH={Config.Database_LogSizGrowth_Mb}MB");
                }
                sb.Append(")");
            }

            Execute(sb.ToString());
        }

        public override void DropDatabase(string name)
        {
            if (ExistsDatabase(name))
            {
                Execute($"USE [master];ALTER DATABASE [{name}] SET SINGLE_USER with ROLLBACK IMMEDIATE;DROP DATABASE [{name}]");
            }
            DataBaseCache.TryRemove(name, out _);
        }

        public override bool ExistsDatabase(string name)
        {
            return ExecuteScalar<int>($"SELECT COUNT(1) FROM sys.databases WHERE name='{name}'") > 0;
        }

        public override IDbConnection GetConn()
        {
            var conn = new SqlConnection(ConnectionString);
            if (conn.State == ConnectionState.Closed)
            {
                try
                {
                    conn.Open();
                }
                catch (Exception ex)
                {
                    conn.Dispose();
                    throw ex;
                }
            }
            return conn;
        }

        public override async Task<IDbConnection> GetConnAsync()
        {
            var conn = new SqlConnection(ConnectionString);
            if (conn.State == ConnectionState.Closed)
            {
                try
                {
                    await conn.OpenAsync();
                }
                catch (Exception ex)
                {
#if CORE
                    await conn.DisposeAsync();
#else
                    conn.Dispose();
#endif
                    throw ex;
                }
            }
            return conn;
        }

        public override IEnumerable<string> ShowDatabases()
        {
            return Query<string>("SELECT name FROM sys.databases");
        }

        public override IEnumerable<string> ShowDatabasesExcludeSystem()
        {
            return ShowDatabases().Where(w => w != "master" && w != "tempdb" && w != "model" && w != "msdb" && w.ToLower() != "resource");
        }

        public override void Vacuum(string dbname)
        {
            var sql = $@"DECLARE @DB NVARCHAR(MAX)='{dbname}'
DECLARE @sql NVARCHAR(MAX)='
ALTER DATABASE '+@DB+' SET RECOVERY SIMPLE WITH NO_WAIT
ALTER DATABASE '+@DB+' SET RECOVERY SIMPLE'

DECLARE @sql2 NVARCHAR(MAX)='
USE ['+@DB+']
DBCC SHRINKDATABASE('+@DB+') 
DBCC SHRINKFILE(1 , 1, TRUNCATEONLY)'

DECLARE @sql3 NVARCHAR(MAX)='
ALTER DATABASE '+@DB+' SET RECOVERY FULL WITH NO_WAIT
ALTER DATABASE '+@DB+' SET RECOVERY FULL'
EXEC(@sql)
EXEC(@sql2)
EXEC(@sql3)";
            Execute(sql);
        }
    }
}
