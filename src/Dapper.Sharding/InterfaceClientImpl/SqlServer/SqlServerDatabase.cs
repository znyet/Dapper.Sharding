﻿using System;
using System.Collections.Generic;
using System.Data;
#if !CORE
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapper.Sharding
{
    internal class SqlServerDatabase : IDatabase
    {
        public SqlServerDatabase(string name, SqlServerClient client) : base(name, client)
        {
            ConnectionString = ConnectionStringBuilder.BuilderSqlServer(client.Config, name);
        }

        protected override ITable<T> CreateITable<T>(string name)
        {
            return new SqlServerTable<T>(name, this);
        }

        public override string ConnectionString { get; set; }

        public override void DropTable(string name)
        {
            Execute($"IF EXISTS(SELECT 1 FROM sysObjects WHERE Id=OBJECT_ID(N'{name}') AND xtype='U' AND uid=1)DROP TABLE [{name}]");
            TableCache.TryRemove(name, out _);
        }

        public override bool ExistsTable(string name)
        {
            //return ExecuteScalar($"SELECT 1 FROM sysObjects WHERE Id=OBJECT_ID(N'{name}') AND xtype='U'") != null;
            return ExecuteScalar<long>($"SELECT COUNT(1) FROM sysObjects WHERE xtype='U' AND uid=1 AND name='{name}'") > 0;
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

        public override IEnumerable<string> GetTableColumnList(string name)
        {
            return Query<string>($"Select Name FROM SysColumns Where id=Object_Id('{name}')");
        }

        public override TableEntity GetTableEntityFromDatabase(string name, bool firstCharToUpper = false)
        {
            var entity = new TableEntity();
            entity.PrimaryKey = "";
            var manager = GetTableManager(name);
            entity.IndexList = manager.GetIndexEntityList();
            entity.ColumnList = manager.GetColumnEntityList(entity, firstCharToUpper);
            string sql = $@"select ROW_NUMBER() OVER (ORDER BY a.name) AS Num, 
a.name AS Name,
CONVERT(NVARCHAR(100),isnull(g.[value],'')) AS Comment
from
sys.tables a left join sys.extended_properties g
on (a.object_id = g.major_id AND g.minor_id = 0) where a.schema_id=1 AND a.type='U' AND a.Name='{name}'";
            var row = QueryFirstOrDefault(sql);
            entity.Comment = row.Comment;
            return entity;
        }

        public override IEnumerable<string> GetTableList()
        {
            return Query<string>("SELECT name FROM sysObjects WHERE xtype='U' AND uid=1");
        }

        public override ITableManager GetTableManager(string name)
        {
            return new SqlServerTableManager(name, this);
        }

        public override string GetTableScript<T>(string name)
        {
            var tableEntity = ClassToTableEntityUtils.Get<T>(Client.DbType);
            var sb = new StringBuilder();

            sb.Append($"IF NOT EXISTS (SELECT * FROM sys.objects WHERE name='{name}' AND type='U' AND schema_id=1)");
            sb.Append($"CREATE TABLE [{name}](");
            foreach (var item in tableEntity.ColumnList)
            {
                if (DbType == DataBaseType.SqlServer2005 && item.CsType == typeof(DateTime))
                {
                    sb.Append($"[{item.Name}] datetime");
                }
                else
                {
                    sb.Append($"[{item.Name}] {item.DbType}");
                }

                if (tableEntity.PrimaryKey.ToLower() == item.Name.ToLower())
                {
                    if (tableEntity.IsIdentity)
                    {
                        sb.Append(" identity(1,1)");
                    }
                    sb.Append(" PRIMARY KEY");
                }

                if (item != tableEntity.ColumnList.Last())
                {
                    sb.Append(",");
                }
            }
            sb.Append(");");

            foreach (var item in tableEntity.IndexList.Where(w => w.Type != IndexType.PrimaryKey))
            {
                sb.Append("CREATE ");
                if (item.Type == IndexType.Unique)
                {
                    sb.Append("UNIQUE");
                }
                sb.Append($" NONCLUSTERED INDEX [{name}_{item.Name}] ON [dbo].[{name}]({item.Columns});");

            }
            sb.Append($"EXEC sp_addextendedproperty 'MS_Description', N'{tableEntity.Comment}','SCHEMA', N'dbo','TABLE', N'{name}';");
            foreach (var item in tableEntity.ColumnList)
            {
                sb.Append($"EXEC sp_addextendedproperty 'MS_Description', N'{item.Comment}', 'SCHEMA', N'dbo','TABLE', N'{name}','COLUMN', N'{item.Name}';");
            }
            return sb.ToString();
        }

        public override void TruncateTable(string name)
        {
            Execute($"TRUNCATE TABLE [{name}]");
        }

        public override void OptimizeTable(string name, bool final = false, bool deduplicate = false)
        {
            throw new NotImplementedException();
        }

        public override void OptimizeTable(string name, string partition, bool final = false, bool deduplicate = false)
        {
            throw new NotImplementedException();
        }

        public override void Vacuum()
        {
            var sql = $@"DECLARE @DB NVARCHAR(MAX)='{Name}'
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
