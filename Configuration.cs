using Microsoft.Xna.Framework;
using ModifyWeapons.Progress;
using Newtonsoft.Json;
using TShockAPI;
using static ModifyWeapons.Progress.ProgressType;

namespace ModifyWeapons;

internal class Configuration
{
    #region 实例变量
    [JsonProperty("公用武器进度类型", Order = -1)]
    public string[] ProgID { get; set; } = new string[]
    {
        "0 无 | 1 克眼 | 2 史王 | 3 世吞克脑 | 4 骷髅王 | 5 蜂王 | 6 巨鹿 | 7 肉后",
        "8 一王后 | 9 双子魔眼 | 10 毁灭者 | 11 铁骷髅王 | 12 世花 | 13 石巨人 | 14 猪鲨",
        "15 拜月 | 16 月总 | 17 光女 | 18 史后 | 19 哀木 | 20 南瓜王 | 21 尖叫怪 | 22 冰雪女皇",
        "23 圣诞坦克 | 24 飞碟 | 25 小丑 | 26 日耀柱 | 27 星旋柱 | 28 星云柱 | 29 星尘柱",
        "30 哥布林 | 31 海盗 | 32 霜月 | 33 血月 | 34 旧日一 | 35 旧日二 | 36 双足翼龙",
        "37 雨天 | 38 白天 | 39 夜晚 | 40 大风天 | 41 万圣节 | 42 派对 | 43 醉酒种子 | 44 十周年",
        "45 ftw种子 | 46 颠倒种子 | 47 蜂蜜种子 | 48 饥荒种子 | 49 天顶种子 | 50 陷阱种子",
        "51 满月 | 52 亏凸月 | 53 下弦月 | 54 残月 | 55 新月 | 56 娥眉月 | 57 上弦月 | 58 盈凸月",
    };

    [JsonProperty("插件开关", Order = -2)]
    public bool Enabled { get; set; } = true;
    [JsonProperty("缓存日志", Order = -1)]
    public bool CacheLog { get; set; } = true;
    [JsonProperty("进服只给管理建数据", Order = 0)]
    public bool Enabled2 { get; set; } = false;
    [JsonProperty("每页显示武器数量", Order = 1)]
    public int Page { get; set; } = 5;
    [JsonProperty("初始重读次数", Order = 2)]
    public int ReadCount { get; set; } = 2;
    [JsonProperty("增加重读次数的冷却秒数", Order = 3)]
    public float ReadTime { get; set; } = 1800;

    [JsonProperty("给完物品的延迟指令", Order = 4)]
    public bool Alone { get; set; } = true;
    [JsonProperty("延迟指令毫秒", Order = 5)]
    public float DelayCMDTimer { get; set; } = 500.0f;
    [JsonProperty("延迟指令表", Order = 6)]
    public HashSet<string> AloneList { get; set; } = new HashSet<string>();

    [JsonProperty("清理修改武器(丢出或放箱子会消失)", Order = 7)]
    public bool ClearItem = true;
    [JsonProperty("免清表", Order = 8)]
    public int[] ExemptItems { get; set; } = new int[] { 1 };
    [JsonProperty("触发重读指令检测表", Order = 9)]
    public HashSet<string> Text { get; set; } = new HashSet<string>();

    [JsonProperty("启用公用武器", Order = 10)]
    public bool PublicWeapons { get; set; } = true;
    [JsonProperty("公用武器播报标题", Order = 11)]
    public string Title { get; set; } = "羽学开荒服 ";
    [JsonProperty("公用武器表", Order = 12)]
    public List<ItemData>? ItemDatas { get; set; }
    #endregion

    #region 公用武器数据结构
    public class ItemData : ItemProperties
    {
        [JsonProperty("进度", Order = -1)]
        public ProgressType Progress { get; set; } = None;
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

        // 复制构造函数
        public ItemData(ItemData other)
        {
            Name = other.Name;
            type = other.type;
            stack = other.stack;
            prefix = other.prefix;
            damage = other.damage;
            scale = other.scale;
            knockBack = other.knockBack;
            useTime = other.useTime;
            useAnimation = other.useAnimation;
            shoot = other.shoot;
            shootSpeed = other.shootSpeed;
            ammo = other.ammo;
            useAmmo = other.useAmmo;
            color = other.color;
        }
    }
    #endregion

    #region 预设参数方法
    public void Ints()
    {
        this.Text = new HashSet<string> { "deal", "shop", "fishshop", "fs" };
        this.AloneList = new HashSet<string> { "/mw read" };
        this.ItemDatas = new List<ItemData>()
        {
            new ItemData("",96,1,82,30,1,5.5f,10,15,10,9,0,97,default),
            new ItemData("",800,1,82,35,1,2.5f,10,15,5,6,0,97,default),
        };
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