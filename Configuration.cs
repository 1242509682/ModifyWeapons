using Newtonsoft.Json;
using Microsoft.Xna.Framework;
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
        [JsonProperty("只给指定名字物品", Order = 2)]
        public bool OnlyItem { get; set; } = true;

        [JsonProperty("自动重读", Order = 3)]
        public int Auto { get; set; } = 1;
        [JsonProperty("触发重读指令检测表", Order = 4)]
        public HashSet<string> Text { get; set; } = new HashSet<string>();

        [JsonProperty("清理修改武器(丢出或放箱子会消失)", Order = 5)]
        public bool ClearItem = true;
        [JsonProperty("免清表", Order = 6)]
        public int[] ExemptItems { get; set; } = new int[] { 1 };

        [JsonProperty("进服只给管理建数据", Order = 10)]
        public bool Enabled2 { get; set; } = false;

        [JsonProperty("增加重读次数的冷却秒数", Order = 11)]
        public float ReadTime { get; set; } = 1800;

        [JsonProperty("启用公用武器", Order = 12)]
        public bool PublicWeapons { get; set; } = true;

        [JsonProperty("同步数据秒数", Order = 13)]
        public int SyncTime { get; set; } = 15;

        [JsonProperty("公用武器播报标题", Order = 14)]
        public string Title { get; set; } = "羽学开荒服 ";

        [JsonProperty("公用武器表", Order = 15)]
        public List<ItemData>? ItemDatas { get; set; }
        #endregion

        #region 预设参数方法
        public void Ints()
        {
            this.Text = new HashSet<string> { "deal", "shop", "fishshop", "fs" };
            this.ItemDatas = new List<ItemData>()
            {
                new ItemData("",96,1,82,30,1,5.5f,10,15,10,9,0,97,default),
                new ItemData("",800,1,82,35,1,2.5f,10,15,5,6,0,97,default),
            };
        }
        #endregion

        #region 公用武器数据结构
        public class ItemData
        {
            [JsonProperty("名称", Order = 0)]
            public string Name { get; set; }
            [JsonProperty("ID", Order = 0)]
            public int type { get; set; }
            [JsonProperty("数量", Order = 0)]
            public int stack { get; set; }
            [JsonProperty("前缀", Order = 0)]
            public byte prefix { get; set; }
            [JsonProperty("伤害", Order = 0)]
            public int damage { get; set; }
            [JsonProperty("大小", Order = 0)]
            public float scale { get; set; }
            [JsonProperty("击退", Order = 0)]
            public float knockBack { get; set; }
            [JsonProperty("用速", Order = 0)]
            public int useTime { get; set; }
            [JsonProperty("攻速", Order = 0)]
            public int useAnimation { get; set; }
            [JsonProperty("弹幕", Order = 0)]
            public int shoot { get; set; }
            [JsonProperty("弹速", Order = 0)]
            public float shootSpeed { get; set; }
            [JsonProperty("弹药", Order = 0)]
            public int ammo { get; set; }
            [JsonProperty("发射器", Order = 0)]
            public int useAmmo { get; set; }
            [JsonProperty("颜色", Order = 0)]
            public Color color { get; set; }

            [JsonConstructor]
            public ItemData(string name, int type, int stack, byte prefix, int damage, float scale, float knockBack,
                int useTime, int useAnimation, int shoot, float shootSpeed, int ammo, int useAmmo, Color color = default)
            {
                this.Name = name ?? "";
                this.type = type;
                this.stack = stack;
                this.prefix = prefix;
                this.damage = damage;
                this.scale = scale;
                this.knockBack = knockBack;
                this.useTime = useTime;
                this.useAnimation = useAnimation;
                this.shoot = shoot;
                this.shootSpeed = shootSpeed;
                this.ammo = ammo;
                this.useAmmo = useAmmo;
                this.color = color;
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