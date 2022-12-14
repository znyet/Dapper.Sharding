#sharding for mysql、postgresql、sqlserver、sqlite、clickhouse、oracle

```csharp
var config = new DataBaseConfig { Server = "127.0.0.1", UserId = "root", Password = "123", Port = 3306 };

//client must be singleton mode(必须是单例模式)
static IClient client = ShardingFactory.CreateClient(DataBaseType.MySql, config);
//static IClient client = ShardingFactory.CreateClient(DataBaseType.SqlServer, config, DataBaseVersion.SqlServer2012);
//client.AutoCreateDatabase = true; //自动创建数据库
//client.AutoCreateTable = true; //自动创建表
//client.AutoCompareTableColumn = false; //是否自动对比列
//client.AutoCompareTableColumnDelete = false; //是否自动对比删除列

var db = client.GetDatabase("test");
//var db2 = client.GetDatabase("test2"); //this will create test2 database(自由分库)

var table = db.GetTable<Student>("student"); //自由分表
table.Insert(new Student { Id = ShardingFactory.NextObjectId(), Name = "lina" });

var table2 = db.GetTable<Student>("student2");
table2.Insert(new Student { Id = ShardingFactory.NextObjectId(), Name = "lina2" });

var table3 = db.GetTable<Student>("student3");
table3.Insert(new Student { Id = ShardingFactory.NextObjectId(), Name = "lina3" });

//sharding query on all table(分片查询)
var query = new ShardingQuery(table, table2, table3); 
var total = await query.CountAsync();
or
var data = await query.QueryAsync("SELECT * FROM $table"); //$table is each table name

//Transaction(分布式事务)
var tran = new DistributedTransaction();
try
{
    table.Insert(new Student { Id = ShardingFactory.NextObjectId(), Name = "lina" }, tran);
    table.Delete("1", tran);
    table2.Delete("2", tran);
    table3.Delete("3", tran);
    tran.Commit();
}
catch
{
    tran.Rollback();    
}

//Transaction CAP
//https://gitee.com/znyet/dapper.sharding.cap

namespace ConsoleApp
{
    [Table("Id", false, "学生表")]
    public class Student
    {
        [Column(24, "主键id")]
        public string Id { get; set; }

        [Column(50, "名字")]
        public string Name { get; set; }

        [Column(20, "年龄")]
        public long Age { get; set; }

        [Ignore]
        public string NoDatabaseColumn { get; set; }
    }
}
```

```csharp
//custom column type
[Column(columnType: "text")]
[Column(columnType: "geometry")]
```

```csharp
//IQuery
var query = table.AsQuery("a")
     .LeftJoin(table2, "b", "a.bid=b.id")
     .Where("a.name=@name")
     .OrderBy("a.id")
     .ReturnFields("a.*,b.name as bname")
     //.Limit(10)
     .Page(1, 10)
     .Param(new { name = "lili" });

var data = query.Query<T>();
var data2 = query.QueryPageAndCount<T>();

//IUnion
var union = query.Union(query2)
			     .Union(query3)
				 .Where("name=@name")
				 .OrderBy("id")
				 .Page(1, 10)
				 .Param(new { name = "lili" });

var data = union.Query<T>();
```

```csharp
//client must singleton mode(必须是单例模式)

/*===mysql need MySqlConnector≥1.3.14===*/
public static IClient Client = ShardingFactory.CreateClient(DataBaseType.MySql, new DataBaseConfig { Server = "127.0.0.1", UserId = "root", Password = "123", Port = 3306 })

/*===sqlite need System.Data.SQLite.Core≥1.0.115.5===*/
//public static IClient Client = ShardingFactory.CreateClient(DataBaseType.Sqlite, new DataBaseConfig { Server = "D:\\DatabaseFile" })

/*===sqlserver need Microsoft.Data.SqlClient≥2.1.4 ===*/
//public static IClient Client = ShardingFactory.CreateClient(DataBaseType.SqlServer2008, new DataBaseConfig { Server = ".\\express", UserId = "sa", Password = "123456", Database_Path = "D:\\DatabaseFile" })

/*===clickhouse need ClickHouse.Ado.Dapper≥1.0.8 ===*/
//public static IClient ClientHouse = ShardingFactory.CreateClient(DataBaseType.ClickHouse, new DataBaseConfig { Server = "192.168.0.200" });

/*===postgresql need Npgsql≥4.0.12===*/
//public static IClient Client = ShardingFactory.CreateClient(DataBaseType.Postgresql, new DataBaseConfig { Server = "127.0.0.1", UserId = "postgres", Password = "123" })

/*===oracle need Oracle.ManagedDataAccess.Core===*/
static DataBaseConfig oracleConfig = new DataBaseConfig
{
    Server = "127.0.0.1",
    UserId = "test",
    Password = "123",
    Oracle_ServiceName = "xe",
    Oracle_SysUserId = "sys",
    Oracle_SysPassword = "123",
    Database_Path = "D:\\DatabaseFile",
    Database_Size_Mb = 1,
    Database_SizeGrowth_Mb = 1
};
//public static IClient Client = ShardingFactory.CreateClient(DataBaseType.Oracle, oracleConfig)
```

```csharp
//json or json string field
namespace ConsoleApp1
{
    [Table("Id", true, "人类")]
    public class School
    {
        public int Id { get; set; }

        public string Name { get; set; }
        
        //[JsonString]
        //[Column(columnType: "varchar(8000)")]
        [Column(columnType: "jsonb")]
        public Student Stu { get; set; }  //json or json string
    }

    public class Student
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }
}

var config = new DataBaseConfig { Password = "123" };
var client = ShardingFactory.CreateClient(DataBaseType.Postgresql, config);
client.AutoCompareTableColumn = true;

//TypeHandlerJsonNet.Add(typeof(Student)); //json.net
TypeHandlerSystemTextJson.Add(typeof(Student)); //System.Text.Json add dapper typehandler

var db = client.GetDatabase("test");
var table = db.GetTable<School>("school");

var school = new School
{
    Name = "test",
    Stu = new Student
    {
        Name = "lihua",
        Age = 18
    }
};
table.Insert(school);
//table.InsertMany(list);
var model = table.GetById(1);
Console.WriteLine(model.Stu.Name);

//System.Text.Json
TypeHandlerSystemTextJson.Add(typeof(Student));
TypeHandlerSystemTextJson.Add(typeof(List<Student>));
TypeHandlerSystemTextJson.Add(typeof(JsonObject));
TypeHandlerSystemTextJson.Add(typeof(JsonArray));
/**Add Assembly***/
TypeHandlerSystemTextJson.Add(typeof(Student).Assembly);

//Json.Net
TypeHandlerJsonNet.Add(typeof(Student));
TypeHandlerJsonNet.Add(typeof(List<Student>));
TypeHandlerJsonNet.Add(typeof(JObject));
TypeHandlerJsonNet.Add(typeof(JArray));
/**Add Assembly***/
TypeHandlerJsonNet.Add(typeof(Student).Assembly);
```

```csharp
GeneratorClassFile(can create class entity file from database) //代码生成器

db.GeneratorTableFile("D:\\Class"); //生成表实体类

db.GeneratorDbContextFile("D:\\Class"); //生成请求上下文文件
```

```csharp
//Npgsql GeoJson
NpgsqlConnection.GlobalTypeMapper.UseGeoJson();
NpgsqlGeoJsonFactory.UseGeoJson();

//Npgsql NetTopologySuite
NpgsqlConnection.GlobalTypeMapper.UseNetTopologySuite();
NpgsqlGeoFactory.UseGeo();
```

```csharp
//Npgsql DateTimeOffset
//https://www.npgsql.org/doc/types/basic.html
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
```
```javascript
//json config
{
    "Server": "127.0.0.1",
    "UserId": "sa",
    "Password": "123456",
    "AutoCreateDatabase": true,
    "AutoCreateTable": true,
    "AutoCompareTableColumn": true,
    "AutoCompareTableColumnDelete": true,
    "Database": "test",
    "node": {
        "Server": "127.0.0.2",
        "UserId": "saa",
        "Password": "1234567",
        "AutoCreateDatabase": true,
        "AutoCreateTable": true,
        "AutoCompareTableColumn": true,
        "AutoCompareTableColumnDelete": true,
        "Database": "test2"
    }

}
```
```csharp
//load json config
var c1 = ConfigSystemTextJson.LoadConfig("db.json");
var c2 = ConfigJsonNet.LoadConfig("db.json");

var c3 = ConfigSystemTextJson.LoadConfig("db.json", "node");
var c4 = ConfigJsonNet.LoadConfig("db.json", "node");
```