using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace ModifyWeapons
{
    public class Database
    {
        #region 数据表结构
        public Database()
        {
            var sql = new SqlTableCreator(TShock.DB, new SqliteQueryCreator());

            // 定义并确保 ModifyWeapons 表的结构
            sql.EnsureTableStructure(new SqlTable("ModifyWeapons", //表名
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, Unique = true, AutoIncrement = true }, // 主键列
                new SqlColumn("Name", MySqlDbType.TinyText) { NotNull = true }, // 非空字符串列
                new SqlColumn("Enabled", MySqlDbType.Int32) { DefaultValue = "0" }, // bool值列
                new SqlColumn("ItemId", MySqlDbType.Int32), // 物品ID
                new SqlColumn("Prefix", MySqlDbType.Int32), //前缀
                new SqlColumn("Damage", MySqlDbType.Int32), // 伤害
                new SqlColumn("Scale", MySqlDbType.Float), //大小
                new SqlColumn("KnockBack", MySqlDbType.Float), //击退
                new SqlColumn("UseTime", MySqlDbType.Int32), //用速
                new SqlColumn("UseAnimation", MySqlDbType.Int32), //攻速
                new SqlColumn("Shoot", MySqlDbType.Int32), //弹幕
                new SqlColumn("ShootSpeed", MySqlDbType.Float) //弹速
            ));
        }
        #endregion

        #region 更新数据
        internal bool UpdateData(Configuration.PlayerData data)
        {
            // 更新现有记录
            if (TShock.DB.Query(
                "UPDATE ModifyWeapons SET Enabled = @0, ItemId = @1, Prefix = @2, Damage = @3, Scale = @4, KnockBack = @5, UseTime = @6, UseAnimation = @7, Shoot = @8, ShootSpeed = @9 WHERE Name = @10",
                data.Enabled ? 1 : 0, data.ItemId, data.Prefix, data.Damage, data.Scale,data.KnockBack, data.UseTime, data.UseAnimation, data.Shoot, data.ShootSpeed, data.Name) != 0)
            {
                return true;
            }

            // 如果没有更新到任何记录，则插入新记录
            return TShock.DB.Query(
                "INSERT INTO ModifyWeapons (Name, Enabled, ItemId, Prefix, Damage, Scale, KnockBack, UseTime, UseAnimation, Shoot, ShootSpeed) VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10)",
                data.Name, data.Enabled ? 1 : 0, data.ItemId, data.Prefix, data.Damage, data.Scale, data.KnockBack, data.UseTime, data.UseAnimation, data.Shoot, data.ShootSpeed) != 0;
        }
        #endregion

        #region 从数据库中获取指定玩家数据
        internal Configuration.PlayerData GetData(string playerName)
        {
            using (var reader = TShock.DB.QueryReader("SELECT * FROM ModifyWeapons WHERE Name = @0", playerName))
            {
                if (reader.Read())
                {
                    return new Configuration.PlayerData
                    {
                        Name = reader.Get<string>("Name"),
                        Enabled = reader.Get<int>("Enabled") != 0,
                        ItemId = reader.Get<int>("ItemId"),
                        Prefix = reader.Get<int>("Prefix"),
                        Damage = reader.Get<int>("Damage"),
                        Scale = reader.Get<float>("Scale"),
                        KnockBack = reader.Get<float>("KnockBack"),
                        UseTime = reader.Get<int>("UseTime"),
                        UseAnimation = reader.Get<int>("UseAnimation"),
                        Shoot = reader.Get<int>("Shoot"),
                        ShootSpeed = reader.Get<float>("ShootSpeed")
                    };
                }
            }
            return null!;
        }
        #endregion

        #region 加载所有数据
        internal List<Configuration.PlayerData> LoadData()
        {
            var data = new List<Configuration.PlayerData>();

            using var reader = TShock.DB.QueryReader("SELECT * FROM ModifyWeapons");

            while (reader.Read())
            {
                data.Add(new Configuration.PlayerData(
                    name: reader.Get<string>("Name"),
                    enabled: reader.Get<int>("Enabled") == 1,
                    id: reader.Get<int>("ItemId"),
                    prefix: reader.Get<int>("Prefix"),
                    damage: reader.Get<int>("Damage"),
                    scale: reader.Get<float>("Scale"),
                    knockBack: reader.Get<float>("KnockBack"),
                    useTime: reader.Get<int>("UseTime"),
                    useAnimation: reader.Get<int>("UseAnimation"),
                    shoot: reader.Get<int>("Shoot"),
                    shootSpeed: reader.Get<float>("ShootSpeed")
                ));
            }

            return data;
        }
        #endregion

        #region 删除指定玩家数据
        internal bool DelData(string name)
        {
            return TShock.DB.Query("DELETE FROM ModifyWeapons WHERE Name = @0", name) != 0;
        }
        #endregion

        #region 清理所有数据方法
        public bool ClearData()
        {
            return TShock.DB.Query("DELETE FROM ModifyWeapons") != 0;
        }
        #endregion
    }
}