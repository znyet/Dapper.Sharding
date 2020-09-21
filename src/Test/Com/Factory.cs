﻿using Dapper.Sharding;
using Test.Com;

namespace Test
{
    public class Factory
    {
        //must singleton mode(必须是单例模式)
        public static IClient Client = ShardingFactory.CreateClient(DataBaseType.MySql, "server=127.0.0.1;port=3306;user=root");
        public static IClient Client2 = ShardingFactory.CreateClient(DataBaseType.MySql, "server=127.0.0.1;port=3307;user=root");

        //public static IClient Client = ClientFactory.CreateClient(DataBaseType.SqlServer2008, "data source=.\\express;user=sa;password=123456");

        public static ReadWirteClient RWClient = ShardingFactory.CreateReadWriteClient(Client, Client2);

        public static IDatabase Db
        {
            get
            {
                return Client.GetDatabase("test");
            }
        }

        public static IDatabase Db2
        {
            get
            {
                return Client2.GetDatabase("test");
            }
        }

        public static ITableManager TableManager
        {
            get
            {
                return Db.GetTableManager("People");
            }
        }

        public static ITable<People> peopleTable
        {
            get
            {
                return Db.GetTable<People>("People");
            }
        }

        public static ITable<Student> studentTable
        {
            get
            {
                return Db.GetTable<Student>("Student");
            }
        }

        public static ITable<Teacher> teacherTable
        {
            get
            {
                return Db.GetTable<Teacher>("Teacher");
            }
        }

        public static ISharding<Student> ShardingAuto
        {
            get
            {
                var tableList = new ITable<Student>[]
                {
                    Db.GetTable<Student>("s1"),
                    Db.GetTable<Student>("s2"),
                    Db.GetTable<Student>("s3"),
                    Db2.GetTable<Student>("s4"),
                    Db2.GetTable<Student>("s5"),
                    Db2.GetTable<Student>("s6")
                };
                return ShardingFactory.CreateShardingAuto(tableList);
            }
        }
    }
}
