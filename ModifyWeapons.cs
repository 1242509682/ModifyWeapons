using System.Text;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using static ModifyWeapons.Configuration;
using static TShockAPI.GetDataHandlers;

namespace ModifyWeapons;

[ApiVersion(2, 1)]
public class Plugin : TerrariaPlugin
{
    #region 插件信息
    public override string Name => "修改武器";
    public override string Author => "羽学";
    public override Version Version => new Version(1, 2, 9);
    public override string Description => "修改玩家物品数据并自动储存重读,可使用/mw指令给予玩家指定属性的物品";
    #endregion

    #region 注册与释放
    public Plugin(Main game) : base(game) { }
    public override void Initialize()
    {
        LoadConfig();
        GeneralHooks.ReloadEvent += ReloadConfig;
        ItemDrop.Register(this.OnItemDrop);
        PlayerUpdate.Register(this.OnPlayerUpdate);
        PlayerHooks.PlayerCommand += this.OnPlayerCommand;
        GetDataHandlers.ChestItemChange.Register(this.OnChestItemChange);
        ServerApi.Hooks.NetGreetPlayer.Register(this, this.OnGreetPlayer);
        TShockAPI.Commands.ChatCommands.Add(new Command("mw.use", Commands.CMD, "修改武器", "mw"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            GetDataHandlers.ItemDrop.UnRegister(this.OnItemDrop);
            PlayerHooks.PlayerCommand -= this.OnPlayerCommand;
            PlayerUpdate.UnRegister(this.OnPlayerUpdate);
            GetDataHandlers.ChestItemChange.UnRegister(this.OnChestItemChange);
            ServerApi.Hooks.NetGreetPlayer.Deregister(this, this.OnGreetPlayer);
            TShockAPI.Commands.ChatCommands.RemoveAll(x => x.CommandDelegate == Commands.CMD);
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 全局变量
    public static Database DB = new(); // 数据库
    internal static Caches Cache = new(); // 内存缓存
    internal static Configuration Config = new(); // 配置文件
    #endregion

    #region 配置重载读取与写入方法
    private static void ReloadConfig(ReloadEventArgs args)
    {
        LoadConfig();
        args.Player.SendInfoMessage("[修改武器]重新加载配置完毕。");
    }

    private static void LoadConfig()
    {
        Config = Read();
        PublicWeapons.WriteName();
        Config.Write();

        //更新在线玩家的公用武器数据
        if (Config.PublicWeapons)
            PublicWeapons.WritePublicWeapons();
    }
    #endregion

    #region 进服自动创建玩家数据方法
    private void OnGreetPlayer(GreetPlayerEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        var data = DB.GetData(plr.Name);
        var adamin = plr.HasPermission("mw.admin");
        if (!Config.Enabled || plr == null)
        {
            return;
        }

        if (data == null)
        {
            if (Config.Enabled2 && !adamin)
            {
                return;
            }

            var newData = new Database.PlayerData
            {
                Name = plr.Name,
                Hand = true,
                Join = true,
                ReadCount = Config.ReadCount,
                ReadTime = DateTime.UtcNow,
            };

            DB.AddData(newData);
        }
        else if (data.Join)
        {
            Commands.UpdataRead(plr, data);
        }

        //更新离线玩家的公用武器数据
        if (Config.PublicWeapons)
        {
            PublicWeapons.AddPublicWeapons(plr);
        }
    }
    #endregion

    #region 延时指令方法
    public static Dictionary<string, DateTime> DelayCooldown = new Dictionary<string, DateTime>();
    public static Dictionary<string, bool> DelayFlag = new Dictionary<string, bool>();
    private void OnPlayerUpdate(object? sender, PlayerUpdateEventArgs e)
    {
        var plr = e.Player;
        var now = DateTime.UtcNow;
        if (plr == null || !plr.IsLoggedIn || !plr.Active ||
            !Config.Enabled || !Config.Alone) return;

        if (!DelayFlag.ContainsKey(plr.Name))
        {
            DelayFlag[plr.Name] = false;
        }

        if (!DelayCooldown.ContainsKey(plr.Name))
        {
            DelayCooldown[plr.Name] = now;
        }

        // 触发延迟指令
        if (DelayFlag[plr.Name])
        {
            if ((now - DelayCooldown[plr.Name]).TotalMilliseconds >= Config.DelayCMDTimer)
            {
                DelayCommand(plr);
                DelayCooldown[plr.Name] = now;
                DelayFlag[plr.Name] = false;
            }
        }
    }
    #endregion

    #region 玩家发送经济指令时触发自动重读
    private void OnPlayerCommand(PlayerCommandEventArgs e)
    {
        var plr = e.Player;
        if (plr == null || !plr.IsLoggedIn || !plr.Active || !Config.Enabled) return;

        // 判断是否是关键词指令（如经济相关）
        if (!Config.Text.Any(text => e.CommandText.Contains(text)))
        {
            return;
        }

        // 遍历背包
        for (int i = 0; i < plr.TPlayer.inventory.Length; i++)
        {
            var inv = plr.TPlayer.inventory[i];
            if (IsModifiedWeapon(plr.Name, inv.type))
            {
                DelayFlag[plr.Name] = true;
                DelayCooldown[plr.Name] = DateTime.UtcNow;
            }
        }
    }
    #endregion

    #region 修改武器掉落的清理方法
    private void OnItemDrop(object? sender, ItemDropEventArgs e)
    {
        var plr = e.Player;
        if (plr == null || !plr.IsLoggedIn || !plr.Active ||
            !Config.Enabled || !Config.ClearItem || plr.HasPermission("mw.admin"))
            return;

        // 如果是免清表的物品 忽略
        if (Config.ExemptItems.Contains(e.ID)) return;

        // 判断该物品是否是“修改武器”
        if (IsModifiedWeapon(plr.Name, e.Type))
        {
            plr.SendInfoMessage($"《[c/AD89D5:清][c/D68ACA:理][c/DF909A:警][c/E5A894:告]》 玩家:{plr.Name}\n" +
                $"禁止乱丢修改物品:[i/s{1}:{e.Type}]");
            e.Handled = true;
        }
    }
    #endregion

    #region 箱子内出现修改武器的清理方法
    private void OnChestItemChange(object? sender, ChestItemEventArgs e)
    {
        var plr = e.Player;

        if (plr == null || !plr.IsLoggedIn || !plr.Active ||
            !Config.Enabled || !Config.ClearItem ||
            plr.HasPermission("mw.admin") ||
            Config.ExemptItems.Contains(e.ID))
        {
            return;
        }

        // 如果是免清表的物品 忽略
        if (Config.ExemptItems.Contains(e.ID)) return;

        if (IsModifiedWeapon(plr.Name, e.Type))
        {
            plr.SendInfoMessage($"《[c/AD89D5:清][c/D68ACA:理][c/DF909A:警][c/E5A894:告]》 玩家:{plr.Name}\n" +
                $"修改物品禁止放箱子:[i/s{1}:{e.Type}]");
            e.Handled = true;
        }
    }
    #endregion

    #region 用临时超管权限让玩家执行延时命令
    private void DelayCommand(TSPlayer plr)
    {
        var mess = new StringBuilder();
        mess.Append("触发延时指令:");
        Group group = plr.Group;
        try
        {
            plr.Group = new SuperAdminGroup();
            foreach (var cmd in Config.AloneList)
            {
                TShockAPI.Commands.HandleCommand(plr, cmd);
                mess.Append($" [c/91DFBB:{cmd}]");
            }
        }
        finally
        {
            plr.Group = group;
        }
        plr.SendMessage($"{mess}", 0, 196, 177);
    }
    #endregion

    #region 判断是否是修改武器
    public static bool IsModifiedWeapon(string playerName, int itemType)
    {
        var dbItem = DB.GetData2(playerName, itemType);
        return dbItem != null;
    }
    #endregion

    #region 更新所有修改武器数据缓存
    public static DateTime LoggedTimer = DateTime.UtcNow;
    public static void UpdateCache()
    {
        if (Cache.WeaponsCache == null)
        {
            Cache.WeaponsCache = new List<Database.MyItemData>();
        }
        else
        {
            Cache.WeaponsCache.Clear();
        }

        if (!Config.Enabled || !Config.PublicWeapons)
        {
            TShock.Log.ConsoleError("[修改武器] 插件未启用或公用武器未开启，无法缓存数据。");
            return;
        }

        var all = DB.GetAll2();
        if (all == null || all.Count == 0)
        {
            TShock.Log.ConsoleError("[修改武器] 没有找到任何修改武器数据。");
            Cache.WeaponsCache.Clear();
            return;
        }

        // 使用 AddRange 高效添加所有数据
        Cache.WeaponsCache.AddRange(all);

        // 输出缓存信息到控制台
        if (Config.CacheLog && (DateTime.UtcNow - LoggedTimer).TotalMilliseconds >= 500)
        {
            var form = new List<string>();
            var names = new List<string>();

            foreach (var weapon in Cache.WeaponsCache)
            {
                if (weapon == null || weapon.type <= 0)
                {
                    continue;
                }

                form.Add(weapon.PlayerName);

                var itemInfo = TShock.Utils.GetItemById(weapon.type);
                if (itemInfo != null)
                {
                    names.Add($"{itemInfo.Name}({weapon.type})");
                }
                else
                {
                    names.Add($"未知物品(ID:{weapon.type})");
                }
            }

            // 输出日志信息
            TShock.Log.ConsoleInfo($"\n已成功缓存 {Cache.WeaponsCache.Count} 个修改物品:");
            if (names.Count > 0)
            {
                TShock.Log.ConsoleInfo(string.Join(", ", names.Distinct()));
            }
            TShock.Log.ConsoleError($"来源: {string.Join(", ", form.Distinct())}\n");

            LoggedTimer = DateTime.UtcNow;
        }
    }
    #endregion

}