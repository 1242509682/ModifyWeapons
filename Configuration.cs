using Newtonsoft.Json;
using TShockAPI;

namespace ModifyWeapons;

internal class Configuration
{
    #region 实例变量
    [JsonProperty("插件开关", Order = -8)]
    public bool Enabled { get; set; } = true;

    [JsonProperty("数据表", Order = 1)]
    public List<PlayerData> data { get; set; } = new List<PlayerData>();

    #endregion

    #region 数据结构
    public class PlayerData
    {
        [JsonProperty("玩家名字", Order = 0)]
        public string Name { get; set; }
        [JsonProperty("进服重读", Order = 1)]
        public bool Enabled { get; set; }
        [JsonProperty("物品ID", Order = 2)]
        public int ItemId { get; set; }
        [JsonProperty("前缀", Order = 3)]
        public int Prefix { get; set; }
        [JsonProperty("伤害", Order = 4)]
        public int Damage { get; set; }
        [JsonProperty("大小", Order = 5)]
        public float Scale { get; set; }
        [JsonProperty("击退", Order = 6)]
        public float KnockBack { get; set; }
        [JsonProperty("用速", Order = 7)]
        public int UseTime { get; set; }
        [JsonProperty("攻速", Order = 8)]
        public int UseAnimation { get; set; }
        [JsonProperty("弹幕", Order = 9)]
        public int Shoot { get; set; }
        [JsonProperty("弹速", Order = 10)]
        public float ShootSpeed { get; set; }

        public PlayerData(){}

        // 构造函数
        public PlayerData(string name, bool enabled, int id, int prefix, int damage, float scale, float knockBack, int useTime, int useAnimation,int shoot, float shootSpeed)
        {
            this.Name = name ?? "";
            this.Enabled = enabled;
            this.ItemId = id != 0 ? id : 1;
            this.Prefix = prefix;
            this.Damage = damage != 0 ? damage : 1;
            this.Scale = scale;
            this.KnockBack = knockBack;
            this.UseTime = useTime;
            this.UseAnimation = useAnimation;
            this.Shoot = shoot;
            this.ShootSpeed = shootSpeed;
        }
    }
    #endregion

    #region 移除数据方法
    public bool DelData(string name)
    {
        var data = this.data.FirstOrDefault(pd => pd.Name == name);
        if (data != null)
        {
            this.data.Remove(data);
            return true;
        }
        return false;
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
            new Configuration().Write();
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