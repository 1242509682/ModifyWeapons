using Microsoft.Xna.Framework;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace ModifyWeapons;

public class Database
{
    #region 数据的结构体
    public class PlayerData
    {
        public string Name { get; set; }
        public int ReadCount { get; set; }
        public bool Hand { get; set; }
        public bool Join { get; set; }
        public DateTime ReadTime { get; set; }
        internal PlayerData(string name = "", int readCount = 0, bool hand = false, bool join = true, DateTime? readTime = default)
        {
            this.Name = name ?? "";
            this.ReadCount = readCount;
            this.Hand = hand;
            this.Join = join;
            this.ReadTime = readTime ?? DateTime.UtcNow;
        }
    }

    public class MyItemData : ItemProperties
    {
        //玩家名称
        public string PlayerName { get; set; } = string.Empty;
        //物品的各项属性
        public int type { get; set; }
        public int stack { get; set; }
        public byte prefix { get; set; }
        public int damage { get; set; }
        public float scale { get; set; }
        public float knockBack { get; set; }
        public int useTime { get; set; }
        public int useAnimation { get; set; }
        public int shoot { get; set; }
        public float shootSpeed { get; set; }
        public int ammo { get; set; }
        public int useAmmo { get; set; }
        public Color color { get; set; }
        public MyItemData() { }

        public MyItemData(string pname, int type, int stack, byte prefix, int damage, float scale, float knockBack,
            int useTime, int useAnimation, int shoot, float shootSpeed, int ammo, int useAmmo, Color color)
        {
            this.PlayerName = pname;
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

    #region 数据库表结构（使用Tshock自带的数据库作为存储）
    public readonly string WeaponsPlayer;
    public readonly string ModifyWeapons;
    public Database()
    {
        WeaponsPlayer = "WeaponsPlayer"; //表名
        var sql = new SqlTableCreator(TShock.DB, new SqliteQueryCreator());

        sql.EnsureTableStructure(new SqlTable(WeaponsPlayer, //表名
            new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, Unique = true, AutoIncrement = true }, // 主键列
            new SqlColumn("Name", MySqlDbType.TinyText) { NotNull = true }, // 非空字符串列
            new SqlColumn("ReadCount", MySqlDbType.Int32) { DefaultValue = "0" },
            new SqlColumn("Hand", MySqlDbType.Int32) { DefaultValue = "0" },
            new SqlColumn("IsJoin", MySqlDbType.Int32) { DefaultValue = "0" },
            new SqlColumn("ReadTime", MySqlDbType.DateTime) { DefaultValue = "CURRENT_TIMESTAMP" }
        ));

        ModifyWeapons = "ModifyWeapons"; //表名
        sql.EnsureTableStructure(new SqlTable(ModifyWeapons, //表名
            new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, Unique = true, AutoIncrement = true }, // 主键列
            new SqlColumn("PlayerName", MySqlDbType.TinyText)
            {
                NotNull = true,
            },
            new SqlColumn("type", MySqlDbType.Int32),
            new SqlColumn("stack", MySqlDbType.Int32),
            new SqlColumn("prefix", MySqlDbType.Int32),
            new SqlColumn("damage", MySqlDbType.Int32),
            new SqlColumn("scale", MySqlDbType.Int32),
            new SqlColumn("knockBack", MySqlDbType.Int32),
            new SqlColumn("useTime", MySqlDbType.Int32),
            new SqlColumn("useAnimation", MySqlDbType.Int32),
            new SqlColumn("shoot", MySqlDbType.Int32),
            new SqlColumn("shootSpeed", MySqlDbType.Int32),
            new SqlColumn("ammo", MySqlDbType.Int32),
            new SqlColumn("useAmmo", MySqlDbType.Int32),
            new SqlColumn("color", MySqlDbType.Int32)
        ));

        // 添加联合唯一索引
        TShock.DB.Query("CREATE UNIQUE INDEX IF NOT EXISTS UC_PlayerType ON " + ModifyWeapons + " (PlayerName, type);");
    }
    #endregion

    #region 为玩家创建数据方法
    public bool AddData(PlayerData data)
    {
        var tb1 = TShock.DB.Query("INSERT INTO " + WeaponsPlayer + " (Name, ReadCount, Hand, IsJoin, ReadTime) VALUES (@0, @1, @2, @3, @4)", 
            data.Name, data.ReadCount, data.Hand ? 1 : 0, data.Join ? 1 : 0, data.ReadTime);

        return tb1 != 0;
    }
    public bool AddData2(MyItemData data2)
    {
        var tb2 = TShock.DB.Query("INSERT OR REPLACE INTO " + ModifyWeapons +
            " (PlayerName, type, stack, prefix, damage, scale, knockBack, useTime, useAnimation, shoot, shootSpeed, ammo, useAmmo, color) " +
            "VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8,@9,@10,@11,@12,@13)",
            data2.PlayerName, data2.type, data2.stack, data2.prefix, data2.damage, data2.scale,
            data2.knockBack, data2.useTime, data2.useAnimation,
            data2.shoot, data2.shootSpeed,
            data2.ammo, data2.useAmmo,
            data2.color.PackedValue);

        return tb2 != 0;
    }
    #endregion

    #region 更新数据内容方法
    public bool UpdateData(PlayerData data)
    {
        var tb1 = TShock.DB.Query("UPDATE " + WeaponsPlayer + " SET ReadCount = @0, Hand = @1, IsJoin = @2, ReadTime = @3 WHERE Name = @4",
            data.ReadCount, data.Hand ? 1 : 0, data.Join ? 1 : 0, data.ReadTime, data.Name);

        return tb1 != 0;
    }
    public bool UpdateData2(MyItemData item)
    {
        var tb2 = TShock.DB.Query("UPDATE " + ModifyWeapons +
            " SET stack = @0, prefix = @1, damage = @2, scale = @3, knockBack = @4, useTime = @5, useAnimation = @6, shoot = @7, shootSpeed = @8, ammo = @9, useAmmo = @10, color = @11 " + "WHERE PlayerName = @12 AND type = @13",
            item.stack, item.prefix, item.damage, item.scale,
            item.knockBack, item.useTime, item.useAnimation,
            item.shoot, item.shootSpeed,
            item.ammo, item.useAmmo,
            item.color.PackedValue,
            item.PlayerName, item.type); // 加上 type 作为条件

        return tb2 != 0;
    }
    #endregion

    #region 增加指定玩家重读次数方法
    public bool AddReadCount(string name, int num)
    {
        return TShock.DB.Query("UPDATE " + WeaponsPlayer + " SET ReadCount = ReadCount + @0 WHERE Name = @1", num, name) != 0;
    }
    #endregion

    #region 删除指定玩家数据方法
    public bool DeleteData(string name)
    {
        var tb1 = TShock.DB.Query("DELETE FROM " + WeaponsPlayer + " WHERE Name = @0", name);
        var tb2 = TShock.DB.Query("DELETE FROM " + ModifyWeapons + " WHERE PlayerName = @0", name);
        return tb1 != 0 || tb2 != 0;
    }
    #endregion

    #region 获取指定玩家数据方法
    public PlayerData? GetData(string name)
    {
        using var reader = TShock.DB.QueryReader("SELECT * FROM " + WeaponsPlayer + " WHERE Name = @0", name);
        if (reader.Read())
        {
            return new PlayerData(
                name: reader.Get<string>("Name"),
                readCount: reader.Get<int>("ReadCount"),
                hand: reader.Get<int>("Hand") == 1,
                join: reader.Get<int>("IsJoin") == 1,
                readTime: reader.Get<DateTime>("ReadTime")
                );
        }

        return null;
    }

    public MyItemData? GetData2(string name, int type)
    {
        using var reader = TShock.DB.QueryReader("SELECT * FROM " + ModifyWeapons + " WHERE PlayerName = @0 AND type = @1", name, type);
        if (reader.Read())
        {
            return new MyItemData(
                pname: reader.Get<string>("PlayerName"),
                type: reader.Get<int>("type"),
                stack: reader.Get<int>("stack"),
                prefix: reader.Get<byte>("prefix"),
                damage: reader.Get<int>("damage"),
                scale: reader.Get<float>("scale"),
                knockBack: reader.Get<float>("knockBack"),
                useTime: reader.Get<int>("useTime"),
                useAnimation: reader.Get<int>("useAnimation"),
                shoot: reader.Get<int>("shoot"),
                shootSpeed: reader.Get<float>("shootSpeed"),
                ammo: reader.Get<int>("ammo"),
                useAmmo: reader.Get<int>("useAmmo"),
                color: (Color)TShock.Utils.DecodeColor(reader.Get<int?>("color"))!
            );
        }
        return null;
    }
    #endregion

    #region 移除所有玩家的指定物品方法
    public bool RemovePwData(int type)
    {
        var rowsAffected = TShock.DB.Query("DELETE FROM " + ModifyWeapons + " WHERE type = @0", type);
        return rowsAffected > 0;
    }
    #endregion

    #region 获取所有玩家数据方法
    public List<PlayerData> GetAll()
    {
        var data = new List<PlayerData>();
        using var reader = TShock.DB.QueryReader("SELECT * FROM " + WeaponsPlayer);
        while (reader.Read())
        {
            data.Add(new PlayerData(
                name: reader.Get<string>("Name"),
                readCount: reader.Get<int>("ReadCount"),
                hand: reader.Get<int>("Hand") == 1,
                join: reader.Get<int>("IsJoin") == 1,
                readTime: reader.Get<DateTime>("ReadTime")
            ));
        }
        return data;
    }

    public List<MyItemData> GetAll2()
    {
        var data = new List<MyItemData>();
        using var reader = TShock.DB.QueryReader("SELECT * FROM " + ModifyWeapons);
        while (reader.Read())
        {
            data.Add(new MyItemData(
                pname: reader.Get<string>("PlayerName"),
                type: reader.Get<int>("type"),
                stack: reader.Get<int>("stack"),
                prefix: reader.Get<byte>("prefix"),
                damage: reader.Get<int>("damage"),
                scale: reader.Get<float>("scale"),
                knockBack: reader.Get<float>("knockBack"),
                useTime: reader.Get<int>("useTime"),
                useAnimation: reader.Get<int>("useAnimation"),
                shoot: reader.Get<int>("shoot"),
                shootSpeed: reader.Get<float>("shootSpeed"),
                ammo: reader.Get<int>("ammo"),
                useAmmo: reader.Get<int>("useAmmo"),
                color: (Color)TShock.Utils.DecodeColor(reader.Get<int?>("color"))!
            ));
        }
        return data;
    }
    #endregion

    #region 清理所有数据方法
    public bool ClearData()
    {
        var tb1 = TShock.DB.Query("DELETE FROM " + WeaponsPlayer);
        var tb2 = TShock.DB.Query("DELETE FROM " + ModifyWeapons);
        return tb1 != 0 || tb2 != 0;
    }
    #endregion
}