namespace Dapper.Sharding
{
    internal class OracleUnion : IUnion
    {
        public OracleUnion(IDatabase db) : base(db)
        {
        }

        public OracleUnion(IDatabase db, string sql) : base(db, sql)
        {
        }

        public override string GetSql()
        {
            if (take == 0)
            {
                return $"SELECT {returnFields} FROM ({sqlTable}) UTable{string.Concat(sqlWhere, sqlGroupBy, sqlHaving, sqlOrderBy)}";
            }
            else
            {
                return $"SELECT * FROM(SELECT AA.*,rownum rn FROM(SELECT {returnFields} FROM ({sqlTable}) UTable{string.Concat(sqlWhere, sqlGroupBy, sqlHaving, sqlOrderBy)}) AA WHERE rownum<={skip + take}) BB WHERE rn>={skip + 1}";
            }
        }

        public override string GetSqlCount()
        {
            return $"SELECT COUNT(1) FROM ({sqlTable}) UTable{string.Concat(sqlWhere, sqlGroupBy, sqlHaving)}";
        }
    }
}
