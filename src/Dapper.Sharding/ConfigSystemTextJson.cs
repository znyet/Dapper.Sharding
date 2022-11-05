#if CORE6
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dapper.Sharding
{
    public class ConfigSystemTextJson
    {
        public static JsonObject LoadObject(string jsonFile)
        {
            var file = $"{AppDomain.CurrentDomain.BaseDirectory}{jsonFile}";
            var text = File.ReadAllText(file);
            return JsonSerializer.Deserialize<JsonObject>(text);
        }

        public static ClientConfig LoadConfig(string jsonFile, string key = null)
        {
            var jobject = LoadObject(jsonFile);
            if (!string.IsNullOrEmpty(key))
            {
                jobject = jobject[key].AsObject();
            }

            var cfg = new ClientConfig();
            cfg.Config = jobject.Deserialize<DataBaseConfig>();
            if (jobject.TryGetPropertyValue("AutoCreateDatabase", out JsonNode node))
            {
                cfg.AutoCreateDatabase = node.GetValue<bool>();
            }

            if (jobject.TryGetPropertyValue("AutoCreateTable", out node))
            {
                cfg.AutoCreateTable = node.GetValue<bool>();
            }

            if (jobject.TryGetPropertyValue("AutoCompareTableColumn", out node))
            {
                cfg.AutoCompareTableColumn = node.GetValue<bool>();
            }

            if (jobject.TryGetPropertyValue("AutoCompareTableColumnDelete", out node))
            {
                cfg.AutoCompareTableColumnDelete = node.GetValue<bool>();
            }

            if (jobject.TryGetPropertyValue("Database", out node))
            {
                cfg.Database = node.GetValue<string>();
            }
            return cfg;
        }
    }
}
#endif