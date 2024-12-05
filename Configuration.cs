using Newtonsoft.Json;
using TShockAPI;

namespace ModifyWeapons
{
    internal class Configuration
    {
        #region 实例变量
        [JsonProperty("插件开关", Order = 0)]
        public bool Enabled { get; set; } = true;

        [JsonProperty("初始重读次数", Order = 1)]
        public int ReadCount { get; set; } = 2;

        [JsonProperty("自动重读", Order = 2)]
        public int Auto { get; set; } = 0;
        [JsonProperty("自动重读冷却秒数", Order = 2)]
        public int AutoTimer { get; set; } = 5;

        [JsonProperty("进服只给管理建数据", Order = 10)]
        public bool Enabled2 { get; set; } = false;

        [JsonProperty("增加重读次数的冷却秒数", Order = 11)]
        public float ReadTime { get; set; } = 1800;
        #endregion

        #region 读取与创建配置文件方法
        public static readonly string FilePath = Path.Combine(TShock.SavePath, "修改武器.json");

        public void Write()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(FilePath, json);
        }

        public static Configuration Read()
        {
            if (!File.Exists(FilePath))
            {
                var NewConfig = new Configuration();
                NewConfig.Write();
                return NewConfig;
            }
            else
            {
                var jsonContent = File.ReadAllText(FilePath);
                return JsonConvert.DeserializeObject<Configuration>(jsonContent)!;
            }
        }
        #endregion

    }
}