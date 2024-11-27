using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace ModifyWeapons
{
    public class Database
    {
        #region ���ݱ�ṹ
        public Database()
        {
            var sql = new SqlTableCreator(TShock.DB, new SqliteQueryCreator());

            // ���岢ȷ�� ModifyWeapons ��Ľṹ
            sql.EnsureTableStructure(new SqlTable("ModifyWeapons", //����
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, Unique = true, AutoIncrement = true }, // ������
                new SqlColumn("Name", MySqlDbType.TinyText) { NotNull = true }, // �ǿ��ַ�����
                new SqlColumn("Enabled", MySqlDbType.Int32) { DefaultValue = "0" }, // boolֵ��
                new SqlColumn("ItemId", MySqlDbType.Int32), // ��ƷID
                new SqlColumn("Prefix", MySqlDbType.Int32), //ǰ׺
                new SqlColumn("Damage", MySqlDbType.Int32), // �˺�
                new SqlColumn("Scale", MySqlDbType.Float), //��С
                new SqlColumn("KnockBack", MySqlDbType.Float), //����
                new SqlColumn("UseTime", MySqlDbType.Int32), //����
                new SqlColumn("UseAnimation", MySqlDbType.Int32), //����
                new SqlColumn("Shoot", MySqlDbType.Int32), //��Ļ
                new SqlColumn("ShootSpeed", MySqlDbType.Float) //����
            ));
        }
        #endregion

        #region ��������
        internal bool UpdateData(Configuration.PlayerData data)
        {
            // �������м�¼
            if (TShock.DB.Query(
                "UPDATE ModifyWeapons SET Enabled = @0, ItemId = @1, Prefix = @2, Damage = @3, Scale = @4, KnockBack = @5, UseTime = @6, UseAnimation = @7, Shoot = @8, ShootSpeed = @9 WHERE Name = @10",
                data.Enabled ? 1 : 0, data.ItemId, data.Prefix, data.Damage, data.Scale,data.KnockBack, data.UseTime, data.UseAnimation, data.Shoot, data.ShootSpeed, data.Name) != 0)
            {
                return true;
            }

            // ���û�и��µ��κμ�¼��������¼�¼
            return TShock.DB.Query(
                "INSERT INTO ModifyWeapons (Name, Enabled, ItemId, Prefix, Damage, Scale, KnockBack, UseTime, UseAnimation, Shoot, ShootSpeed) VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10)",
                data.Name, data.Enabled ? 1 : 0, data.ItemId, data.Prefix, data.Damage, data.Scale, data.KnockBack, data.UseTime, data.UseAnimation, data.Shoot, data.ShootSpeed) != 0;
        }
        #endregion

        #region �����ݿ��л�ȡָ���������
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

        #region ������������
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

        #region ɾ��ָ���������
        internal bool DelData(string name)
        {
            return TShock.DB.Query("DELETE FROM ModifyWeapons WHERE Name = @0", name) != 0;
        }
        #endregion

        #region �����������ݷ���
        public bool ClearData()
        {
            return TShock.DB.Query("DELETE FROM ModifyWeapons") != 0;
        }
        #endregion
    }
}