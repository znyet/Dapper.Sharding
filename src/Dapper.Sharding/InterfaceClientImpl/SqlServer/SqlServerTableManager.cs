using System;
using System.Collections.Generic;
using System.Linq;

namespace Dapper.Sharding
{
    internal class SqlServerTableManager : ITableManager
    {
        public SqlServerTableManager(string name, IDatabase database) : base(name, database)
        {

        }

        public override void CreateIndex(string name, string columns, IndexType indexType)
        {
            string sql = null;
            switch (indexType)
            {
                case IndexType.Normal: sql = $"CREATE INDEX {name} ON [{Name}] ({columns});"; break;
                case IndexType.Unique: sql = $"CREATE UNIQUE INDEX {name} ON [{Name}] ({columns});"; break;
            }
            DataBase.Execute(sql);
        }

        public override void DropIndex(string name)
        {
            DataBase.Execute($"DROP INDEX {name} ON [{Name}]");
        }

        public override void AddColumn(string name, Type t, double length = 0, string comment = null, string columnType = null, int scale = 0)
        {

            if (DataBase.DbVersion == DataBaseVersion.SqlServer2005 && t == typeof(DateTime))
            {
                DataBase.Execute($"alter table [{Name}] add  [{name}] datetime");
            }
            else
            {
                var dbType = CsharpTypeToDbType.Create(DataBase.DbType, DataBase.DbVersion, t, length, columnType, scale);
                DataBase.Execute($"alter table [{Name}] add  [{name}] {dbType}");
            }

            if (!string.IsNullOrEmpty(comment))
            {
                DataBase.Execute($"EXEC sp_addextendedproperty 'MS_Description', N'{comment}', 'SCHEMA', N'dbo','TABLE', N'{Name}','COLUMN', N'{name}'");
            }
        }

        public override void DropColumn(string name)
        {
            DataBase.Execute($"alter table [{Name}] drop column [{name}]");
        }

        public override void ModifyColumn(string name, Type t, double length = 0, string comment = null, string columnType = null, int scale = 0)
        {
            var dbType = CsharpTypeToDbType.Create(DataBase.DbType, DataBase.DbVersion, t, length, columnType, scale);
            DataBase.Execute($"alter table [{Name}] alter column [{name}] {dbType}");

            if (!string.IsNullOrEmpty(comment))
            {
                DataBase.Execute($@"
IF ((SELECT COUNT(*) FROM ::fn_listextendedproperty('MS_Description',
'SCHEMA', N'dbo',
'TABLE', N'{Name}',
'COLUMN', N'{name}')) > 0)
  EXEC sp_updateextendedproperty
'MS_Description', N'{comment}',
'SCHEMA', N'dbo',
'TABLE', N'ZTEST',
'COLUMN', N'Name'
ELSE
  EXEC sp_addextendedproperty
'MS_Description', N'{comment}',
'SCHEMA', N'dbo',
'TABLE', N'{Name}',
'COLUMN', N'{name}'");
            }
        }

        public override void ReNameColumn(string name, string newName, Type t = null, double length = 0, string comment = null, string columnType = null, int scale = 0)
        {
            DataBase.Execute($"EXEC sp_rename '[dbo].[{Name}].[{name}]', '{newName}', 'COLUMN'");
        }

        public override List<IndexEntity> GetIndexEntityList()
        {
            //IEnumerable<dynamic> data = DataBase.Query($"EXEC sp_helpindex '{DataBase.Name}.dbo.{Name}'");
            IEnumerable<dynamic> data = DataBase.Query($"EXEC sp_helpindex [{Name}]");
            var list = new List<IndexEntity>();
            foreach (var row in data)
            {
                var model = new IndexEntity();
                model.Name = row.index_name;
                model.Columns = row.index_keys;
                var descript = ((string)row.index_description).ToLower();
                if (descript.IndexOf("nonclustered") >= 0)
                {
                    if (descript.IndexOf("unique") >= 0)
                    {
                        model.Type = IndexType.Unique;
                    }
                    else
                    {
                        model.Type = IndexType.Normal;
                    }
                }
                else
                {
                    model.Type = IndexType.PrimaryKey;
                }

                list.Add(model);
            }
            return list;
        }

        public override List<ColumnEntity> GetColumnEntityList(TableEntity tb = null, bool firstCharToUpper = false)
        {
            if (tb == null)
                tb = new TableEntity();
            string sql = @"SELECT  
ColumnName=a.name, 
IsKey=case when exists(SELECT 1 FROM sysobjects where xtype='PK' and name in (
  SELECT name FROM sysindexes WHERE indid in(
   SELECT indid FROM sysindexkeys WHERE id = a.id AND colid=a.colid 
   ))) then 1 else 0 end, 
IsIdentity=case when COLUMNPROPERTY(a.id,a.name,'IsIdentity')=1 then 1 else 0 end, 
ColumnType=b.name, 
ColumnLength=COLUMNPROPERTY(a.id,a.name,'PRECISION'), 
DecimalDigit =isnull(COLUMNPROPERTY(a.id,a.name,'Scale'),0), 
ColumnCommnent=isnull(g.[value],''),
AllowNull=case when a.isnullable=1 then 1 else 0 end, 
DefaultValue=isnull(e.text,'')
FROM syscolumns a 
left join systypes b on a.xtype=b.xusertype 
inner join sysobjects d on a.id=d.id and d.xtype='U' and d.name<>'dtproperties' 
left join syscomments e on a.cdefault=e.id 
left join sys.extended_properties g on a.id=g.major_id and a.colid=g.minor_id 
left join sys.extended_properties f on d.id=f.major_id and f.minor_id =0 
where d.name=@Name and d.uid=1
order by a.id,a.colorder";

            var list = new List<ColumnEntity>();
            IEnumerable<dynamic> data = DataBase.Query(sql, new { Name });
            var indexList = GetIndexEntityList();
            var keyName = indexList.FirstOrDefault(f => f.Type == IndexType.PrimaryKey)?.Columns;
            if (!string.IsNullOrEmpty(keyName))
            {
                if (keyName.Contains(","))//如果是复合主键，拿第一个字段做key
                {
                    keyName = keyName.Split(',')[0].Trim();
                }
            }
            foreach (var row in data)
            {
                var model = new ColumnEntity();
                if (firstCharToUpper)
                {
                    model.Name = ((string)row.ColumnName).FirstCharToUpper();
                }
                else
                {
                    model.Name = (string)row.ColumnName;
                }

                model.Comment = row.ColumnCommnent;

                var t = (string)row.ColumnType;
                model.DbType = t.ToLower();

                var map = DbCsharpTypeMap.SqlServerMap.FirstOrDefault(f => f.DbType == t);

                if (map != null)
                {
                    model.CsStringType = map.CsStringType;
                    model.CsType = map.CsType;
                }
                else
                {
                    model.CsStringType = "object";
                    model.CsType = typeof(object);
                }

                if (model.DbType == "decimal")
                {
                    model.Scale = Convert.ToInt32(row.DecimalDigit);
                    if (model.Scale > 0 && model.Scale.ToString().EndsWith("0"))
                    {
                        model.Length = Convert.ToDouble(row.ColumnLength);
                    }
                    else
                    {
                        model.Length = Convert.ToDouble($"{row.ColumnLength}.{row.DecimalDigit}");
                        model.Scale = 0;
                    }
                    model.DbLength = $"{row.ColumnLength},{row.DecimalDigit}";
                }
                else if (model.DbType == "datetime" || model.DbType == "datetime2")
                {
                    model.Length = row.DecimalDigit;
                    model.DbLength = model.Length.ToString();
                }
                else if (model.DbType == "datetimeoffset")
                {
                    model.Length = row.DecimalDigit;
                    model.DbLength = model.Length.ToString();
                }
                else
                {
                    model.Length = row.ColumnLength;
                    model.DbLength = row.ColumnLength.ToString();
                }

                if (!string.IsNullOrEmpty(keyName))
                {
                    if (model.Name.ToLower() == keyName.ToLower())
                    {
                        tb.PrimaryKey = model.Name;
                        if (row.IsIdentity == 1)
                        {
                            tb.IsIdentity = true;
                        }
                        tb.PrimaryKeyType = model.CsType;
                    }
                }
                else
                {
                    if (row.IsKey == 1)
                    {
                        tb.PrimaryKey = model.Name;
                        if (row.IsIdentity == 1)
                        {
                            tb.IsIdentity = true;
                        }
                        tb.PrimaryKeyType = model.CsType;
                    }
                }
                list.Add(model);
            }
            return list;
        }
    }
}
