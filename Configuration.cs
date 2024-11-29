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

        [JsonProperty("进服只给管理建数据", Order = 2)]
        public bool Enabled2 { get; set; } = true;

        [JsonProperty("增加重读次数的冷却秒数", Order = 3)]
        public float ReadTime { get; set; } = 1800;

        [JsonProperty("玩家数据", Order = 4)]
        public List<PlayerData> data { get; set; } = new List<PlayerData>();

        [JsonProperty("修改物品数据", Order = 5)]
        public Dictionary<string, List<ItemData>> Dict { get; set; } = new Dictionary<string, List<ItemData>>();
        #endregion

        #region 数据结构
        public class PlayerData
        {
            [JsonProperty("玩家名字", Order = 0)]
            public string Name { get; set; }
            [JsonProperty("重读次数", Order = 1)]
            public int ReadCount { get; set; }
            [JsonProperty("获取手持信息", Order = 2)]
            public bool Hand { get; set; }
            [JsonProperty("进服重读开关", Order = 3)]
            public bool Join { get; set; }
            [JsonProperty("重读冷却记录", Order = 4)]
            public DateTime ReadTime { get; set; }

            public PlayerData() { }

            public PlayerData(string name, bool enabled, bool read, DateTime readTime, int readCount)
            {
                this.Name = name ?? "";
                this.Hand = enabled;
                this.Join = read;
                this.ReadTime = readTime;
                this.ReadCount = readCount;
            }
        }

        public class ItemData
        {
            [JsonProperty("物品ID", Order = 0)]
            public int ID { get; set; }
            [JsonProperty("数量", Order = 1)]
            public int Stack { get; set; }
            [JsonProperty("前缀", Order = 2)]
            public int Prefix { get; set; }
            [JsonProperty("伤害", Order = 3)]
            public int Damage { get; set; }
            [JsonProperty("大小", Order = 4)]
            public float Scale { get; set; }
            [JsonProperty("击退", Order = 5)]
            public float KnockBack { get; set; }
            [JsonProperty("用速", Order = 6)]
            public int UseTime { get; set; }
            [JsonProperty("攻速", Order = 7)]
            public int UseAnimation { get; set; }
            [JsonProperty("弹幕ID", Order = 8)]
            public int Shoot { get; set; }
            [JsonProperty("弹速", Order = 9)]
            public float ShootSpeed { get; set; }
            [JsonProperty("作弹药", Order = 13)]
            public int Ammo { get; set; }
            [JsonProperty("用弹药", Order = 14)]
            public int UseAmmo { get; set; }

            public ItemData(int id, int stack, int prefix, int damage, float scales, float knockBack, int useTime, int useAnimation, int shoot, float shootSpeed, int ammo, int useAmmo)
            {
                this.ID = id;
                this.Stack = stack;
                this.Prefix = prefix;
                this.Damage = damage;
                this.Scale = scales;
                this.KnockBack = knockBack;
                this.UseTime = useTime;
                this.UseAnimation = useAnimation;
                this.Shoot = shoot;
                this.ShootSpeed = shootSpeed;
                this.Ammo = ammo;
                this.UseAmmo = useAmmo;
            }
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

        #region 移除数据方法
        public bool DelData(string name)
        {
            // 从 data 列表中移除玩家数据
            var data = this.data.FirstOrDefault(pd => pd.Name == name);
            if (data != null)
            {
                this.data.Remove(data);
            }
            else
            {
                return false;
            }

            // 从 Dict 字典中移除玩家的物品数据
            if (this.Dict.ContainsKey(name))
            {
                this.Dict.Remove(name);
            }

            return true;
        }
        #endregion

        #region 添加指定玩家重读次数方法
        public int AddCount(string name, int increment)
        {
            if (this.Dict.ContainsKey(name))
            {
                // 获取玩家的数据
                var data = this.data.FirstOrDefault(p => p.Name == name);
                if (data != null)
                {
                    data.ReadCount += increment;
                    this.Write();

                    // 返回更新后的重读次数
                    return data.ReadCount;
                }
            }

            return -1;
        }
        #endregion

        #region 修改玩家指定物品的属性
        public bool UpdateItem(string name, int id, Dictionary<string, string> Properties)
        {
            if (this.Dict.ContainsKey(name))
            {
                var data = this.Dict[name];
                var item = data.FirstOrDefault(i => i.ID == id);

                if (item != null)
                {
                    foreach (var kvp in Properties)
                    {
                        switch (kvp.Key.ToLower())
                        {
                            case "d":
                            case "da":
                            case "伤害":
                                if (int.TryParse(kvp.Value, out int da)) item.Damage = da;
                                break;
                            case "c":
                            case "sc":
                            case "大小":
                                if (float.TryParse(kvp.Value, out float sc)) item.Scale = sc;
                                break;
                            case "k":
                            case "kb":
                            case "击退":
                                if (float.TryParse(kvp.Value, out float kb)) item.KnockBack = kb;
                                break;
                            case "t":
                            case "ut":
                            case "用速":
                                if (int.TryParse(kvp.Value, out int ut)) item.UseTime = ut;
                                break;
                            case "a":
                            case "ua":
                            case "攻速":
                                if (int.TryParse(kvp.Value, out int ua)) item.UseAnimation = ua;
                                break;
                            case "h":
                            case "sh":
                            case "弹幕":
                                if (int.TryParse(kvp.Value, out int sh)) item.Shoot = sh;
                                break;
                            case "s":
                            case "ss":
                            case "弹速":
                                if (float.TryParse(kvp.Value, out float ss)) item.ShootSpeed = ss;
                                break;
                            case "m":
                            case "am":
                            case "作弹药":
                            case "作为弹药":
                                if (int.TryParse(kvp.Value, out int am)) item.Ammo = am;
                                break;
                            case "aa":
                            case "uaa":
                            case "用弹药":
                            case "使用弹药":
                                if (int.TryParse(kvp.Value, out int uaa)) item.UseAmmo = uaa;
                                break;
                            default:
                                return false;
                        }
                    }
                    this.Write();
                    return true;
                }
            }
            return false;
        }
        #endregion

    }
}