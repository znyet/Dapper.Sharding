﻿namespace Dapper.Sharding
{
    internal class SqlServerUnion : IUnion
    {
        public SqlServerUnion(IDatabase db) : base(db)
        {
        }

        public SqlServerUnion(IDatabase db, string sql) : base(db, sql)
        {
        }

        public override string GetSql()
        {
            if (take == 0)
            {
                return $"SELECT {returnFields} FROM ({sqlTable}) AS UTable{string.Concat(sqlWhere, sqlGroupBy, sqlHaving, sqlOrderBy)}";
            }
            else
            {
                if (skip == 0) //第一页,使用Top语句
                {
                    return $"SELECT TOP ({take}) {returnFields} FROM ({sqlTable}) AS UTable{string.Concat(sqlWhere, sqlGroupBy, sqlHaving, sqlOrderBy)}";
                }
                else
                {
                    if (db.DbVersion == DataBaseVersion.SqlServer2012)
                    {
                        return $"SELECT {returnFields} FROM ({sqlTable}) AS UTable{string.Concat(sqlWhere, sqlGroupBy, sqlHaving, sqlOrderBy)} offset {skip} rows fetch next {take} rows only";
                    }
                    else
                    {
                        //使用ROW_NUMBER()
                        return $"SELECT {returnFields} FROM (SELECT ROW_NUMBER() OVER({sqlOrderBy.Trim()}) AS Row_Number,{returnFields} FROM ({sqlTable}) AS UTable{string.Concat(sqlWhere, sqlGroupBy, sqlHaving)}) A WHERE Row_Number BETWEEN {skip + 1} AND {skip + take}";
                    }
                }

            }
        }

        public override string GetSqlCount()
        {
            return $"SELECT COUNT(1) FROM ({sqlTable}) AS UTable{string.Concat(sqlWhere, sqlGroupBy, sqlHaving)}";
        }
    }
}
