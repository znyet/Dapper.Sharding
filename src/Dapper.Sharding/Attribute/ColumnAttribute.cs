using System;

namespace Dapper.Sharding
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        /// <summary>
        /// 字段长度
        /// </summary>
        public double Length;

        /// <summary>
        /// 列注释
        /// </summary>
        public string Comment;

        /// <summary>
        /// 数据库类型
        /// </summary>
        public string ColumnType;

        /// <summary>
        /// decimal小数点精确位数
        /// </summary>
        public int Scale;

        public ColumnAttribute(double length = 0, string comment = null, string columnType = null, int scale = 0)
        {
            Length = length;
            Comment = comment;
            ColumnType = columnType;
            Scale = scale;
        }


    }
}
