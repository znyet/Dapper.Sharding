﻿using Dapper.Sharding;
using System.Collections.Generic;
using Test.Com;

namespace Test
{
    public class Factory
    {
        //must singleton mode(必须是单例模式)
        public static IClient Client = ShardingFactory.CreateClient(DataBaseType.MySql, new DataBaseConfig { Server = "127.0.0.1", UserId = "root", Password = "123", Port = 3306 });
        public static IClient Client2 = ShardingFactory.CreateClient(DataBaseType.MySql, new DataBaseConfig { Server = "127.0.0.1", UserId = "root", Password = "123", Port = 3307 });

        //public static IClient Client = ShardingFactory.CreateClient(DataBaseType.Sqlite, new DataBaseConfig { Server = "D:\\ssss" });
        //public static IClient Client = ClientFactory.CreateClient(DataBaseType.SqlServer2008, new DataBaseConfig { Server = ".\\express", UserId = "sa",Password = "123456" });

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


        public static ShardingQuery<Student> ShardingQuery
        {
            get
            {
                var list = new ITable<Student>[]
                  {
                        Db.GetTable<Student>("s1"),
                        Db.GetTable<Student>("s2"),
                        Db.GetTable<Student>("s3"),
                        Db2.GetTable<Student>("s4"),
                        Db2.GetTable<Student>("s5"),
                        Db2.GetTable<Student>("s6")
                  };
                return new ShardingQuery<Student>(list);
            }
        }

        public static ShardingQuery<Teacher> ShardingQueryTeacher
        {
            get
            {
                var list = new ITable<Teacher>[]
                  {
                        Db.GetTable<Teacher>("t1"),
                        Db.GetTable<Teacher>("t2"),
                        Db.GetTable<Teacher>("t3"),
                        Db2.GetTable<Teacher>("t4"),
                        Db2.GetTable<Teacher>("t5"),
                        Db2.GetTable<Teacher>("t6")
                  };
                return ShardingFactory.CreateShardingQuery(list);
            }
        }

        public static ISharding<Student> ShardingHash
        {
            get
            {
                var list = new ITable<Student>[]
                  {
                        Db.GetTable<Student>("s1"),
                        Db.GetTable<Student>("s2"),
                        Db.GetTable<Student>("s3"),
                        Db2.GetTable<Student>("s4"),
                        Db2.GetTable<Student>("s5"),
                        Db2.GetTable<Student>("s6")
                  };
                return ShardingFactory.CreateShardingHash(list);
            }
        }

        public static ISharding<Teacher> ShardingRange
        {
            get
            {
                var dict = new Dictionary<long, ITable<Teacher>>()
                {
                    {20000, Db.GetTable<Teacher>("t1") },
                    {40000, Db.GetTable<Teacher>("t2") },
                    {60000, Db.GetTable<Teacher>("t3") },
                    {80000, Db2.GetTable<Teacher>("t4") },
                    {90000, Db2.GetTable<Teacher>("t5") },
                    {100000, Db2.GetTable<Teacher>("t6") },
                };
                return ShardingFactory.CreateShardingRange(dict);
            }
        }
    }
}
