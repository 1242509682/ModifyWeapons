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
        public int Auto { get; set; } = 1;
        [JsonProperty("触发重读指令检测表", Order = 3)]
        public HashSet<string> Text { get; set; } = new HashSet<string>();

        [JsonProperty("清理修改武器(丢出或放箱子会消失)", Order = 4)]
        public bool ClearItem = true;
        [JsonProperty("免清表", Order = 5)]
        public int[] ExemptItems { get; set; } = new int[] { 1 };


        [JsonProperty("进服只给管理建数据", Order = 10)]
        public bool Enabled2 { get; set; } = false;

        [JsonProperty("增加重读次数的冷却秒数", Order = 11)]
        public float ReadTime { get; set; } = 1800;
        #endregion

        #region 预设参数方法
        public void Ints()
        {
            this.Text = new HashSet<string> { "deal", "shop", "fishshop", "fs" };
        }
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
                NewConfig.Ints();
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