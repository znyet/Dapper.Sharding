using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Dapper.Sharding
{
    public class ConfigJsonNet
    {
        public static JObject LoadObject(string jsonFile)
        {
            var file = $"{AppDomain.CurrentDomain.BaseDirectory}{jsonFile}";
            var text = File.ReadAllText(file);
            return JsonConvert.DeserializeObject<JObject>(text);
        }

        public static ClientConfig LoadConfig(string jsonFile, string key = null)
        {
            JObject jobject = LoadObject(jsonFile);
            if (!string.IsNullOrEmpty(key))
            {
                jobject = (JObject)jobject[key];
            }

            var cfg = new ClientConfig();
            cfg.Config = jobject.ToObject<DataBaseConfig>();
            if (jobject.TryGetValue("AutoCreateDatabase", out JToken node))
            {
                cfg.AutoCreateDatabase = node.Value<bool>();
            }

            if (jobject.TryGetValue("AutoCreateTable", out node))
            {
                cfg.AutoCreateTable = node.Value<bool>();
            }

            if (jobject.TryGetValue("AutoCompareTableColumn", out node))
            {
                cfg.AutoCompareTableColumn = node.Value<bool>();
            }

            if (jobject.TryGetValue("AutoCompareTableColumnDelete", out node))
            {
                cfg.AutoCompareTableColumnDelete = node.Value<bool>();
            }

            if (jobject.TryGetValue("Database", out node))
            {
                cfg.Database = node.Value<string>();
            }
            return cfg;
        }
    }
}
