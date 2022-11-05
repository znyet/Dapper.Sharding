namespace Dapper.Sharding
{
    public class ClientConfig
    {
        public DataBaseConfig Config { get; set; }

        public bool AutoCreateDatabase { get; set; }

        public bool AutoCreateTable { get; set; }

        public bool AutoCompareTableColumn { get; set; }

        public bool AutoCompareTableColumnDelete { get; set; }

        public string Database { get; set; }

    }
}
