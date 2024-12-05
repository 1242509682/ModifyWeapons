using System.Text.Json;
using System.Text.Json.Serialization;
using MySql.Data.MySqlClient;
using Microsoft.Xna.Framework;
using TShockAPI;
using TShockAPI.DB;

namespace ModifyWeapons;

public class Database
{
    #region ���ݵĽṹ��
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        WriteIndented = true, //����
        //ͳһ���루����Dict�ļ���Ϊ����ʱ��������ã�
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
    };

    public class PlayerData
    {
        public string Name { get; set; }
        public int ReadCount { get; set; }
        public int Process { get; set; }
        public bool Hand { get; set; }
        public bool Join { get; set; }
        public DateTime ReadTime { get; set; }
        public Dictionary<string, List<ItemData>> Dict { get; set; } = new Dictionary<string, List<ItemData>>();
        internal PlayerData(string name = "",int readCount = 0, bool hand = false, bool join = true, DateTime? readTime = null, Dictionary<string, List<ItemData>>? dict = null, int process = 0)
        {
            this.Name = name ?? "";
            this.ReadCount = readCount;
            this.Hand = hand;
            this.Join = join;
            this.ReadTime = readTime ?? DateTime.UtcNow;
            this.Dict = dict ?? new Dictionary<string, List<ItemData>>();
            this.Process = process;
        }

        public class ItemData
        {
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
            public ItemData(){}

            [JsonConstructor]
            public ItemData(int type, int stack, byte prefix, int damage, float scale, float knockBack,
                int useTime, int useAnimation, int shoot, float shootSpeed, int ammo, int useAmmo, Color color)
            {
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
    }
    #endregion

    #region ���ݿ���ṹ��ʹ��Tshock�Դ������ݿ���Ϊ�洢��
    public Database()
    {
        var sql = new SqlTableCreator(TShock.DB, new SqliteQueryCreator());

        sql.EnsureTableStructure(new SqlTable("ModifyWeapons", //����
            new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, Unique = true, AutoIncrement = true }, // ������
            new SqlColumn("Name", MySqlDbType.TinyText) { NotNull = true }, // �ǿ��ַ�����
            new SqlColumn("ReadCount", MySqlDbType.Int32) { DefaultValue = "0" },
            new SqlColumn("IsProcess", MySqlDbType.Int32) { DefaultValue = "0" },
            new SqlColumn("Hand", MySqlDbType.Int32) { DefaultValue = "0" },
            new SqlColumn("IsJoin", MySqlDbType.Int32) { DefaultValue = "0" },
            new SqlColumn("ReadTime", MySqlDbType.DateTime) { DefaultValue = "CURRENT_TIMESTAMP" },
            new SqlColumn("Dict", MySqlDbType.LongText)  // �ı��У����ڴ洢���л�����ƷID�б�
        ));
    }
    #endregion

    #region Ϊ��Ҵ������ݷ���
    public bool AddData(PlayerData data)
    {
        var dictJson = JsonSerializer.Serialize(data.Dict, Options);
        return TShock.DB.Query("INSERT INTO ModifyWeapons (Name, ReadCount,IsProcess, Hand, IsJoin, ReadTime, Dict) VALUES (@0, @1, @2, @3, @4, @5, @6)",
            data.Name, data.ReadCount, data.Process, data.Hand ? 1 : 0, data.Join ? 1 : 0, data.ReadTime, dictJson) != 0;
    }
    #endregion

    #region �����������ݷ���
    public bool UpdateData(PlayerData data)
    {
        var dictJson = JsonSerializer.Serialize(data.Dict, Options);

        return TShock.DB.Query("UPDATE ModifyWeapons SET ReadCount = @0,IsProcess = @1, Hand = @2, IsJoin = @3, ReadTime = @4, Dict = @5 WHERE Name = @6",
            data.ReadCount, data.Process, data.Hand ? 1 : 0, data.Join ? 1 : 0, data.ReadTime, dictJson, data.Name) != 0;
    }
    #endregion

    #region ����ָ������ض���������
    public bool UpReadCount(string name, int num)
    {
        return TShock.DB.Query("UPDATE ModifyWeapons SET ReadCount = ReadCount + @0 WHERE Name = @1", num, name) != 0;
    }
    #endregion

    #region ɾ��ָ��������ݷ���
    public bool DeleteData(string name)
    {
        return TShock.DB.Query("DELETE FROM ModifyWeapons WHERE Name = @0", name) != 0;
    }
    #endregion

    #region ��ȡָ��������ݷ���
    public PlayerData? GetData(string name)
    {
        using var reader = TShock.DB.QueryReader("SELECT * FROM ModifyWeapons WHERE Name = @0", name);

        if (reader.Read())
        {
            var dictJson = reader.Get<string>("Dict");
            var dict = JsonSerializer.Deserialize<Dictionary<string, List<PlayerData.ItemData>>>(dictJson, Options);
            return new PlayerData(
                name: reader.Get<string>("Name"),
                readCount: reader.Get<int>("ReadCount"),
                hand: reader.Get<int>("Hand") == 1,
                process: reader.Get<int>("IsProcess"),
                join: reader.Get<int>("IsJoin") == 1,
                readTime: reader.Get<DateTime>("ReadTime"),
                dict: dict
            );
        }

        return null;
    }
    #endregion

    #region ��ȡ����������ݷ���
    public List<PlayerData> GetAll()
    {
        var data = new List<PlayerData>();
        using var reader = TShock.DB.QueryReader("SELECT * FROM ModifyWeapons");
        while (reader.Read())
        {
            var dictJson = reader.Get<string>("Dict");
            var dict = JsonSerializer.Deserialize<Dictionary<string, List<PlayerData.ItemData>>>(dictJson, Options);

            data.Add(new PlayerData(
                name: reader.Get<string>("Name"),
                readCount: reader.Get<int>("ReadCount"),
                hand: reader.Get<int>("Hand") == 1,
                process: reader.Get<int>("IsProcess"),
                join: reader.Get<int>("IsJoin") == 1,
                readTime: reader.Get<DateTime>("ReadTime"),
                dict: dict
            ));
        }

        return data;
    }
    #endregion

    #region �����������ݷ���
    public bool ClearData()
    {
        return TShock.DB.Query("DELETE FROM ModifyWeapons") != 0;
    }
    #endregion
}