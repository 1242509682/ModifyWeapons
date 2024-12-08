using System.Text;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using Microsoft.Xna.Framework;
using static ModifyWeapons.Configuration;
using static TShockAPI.GetDataHandlers;

namespace ModifyWeapons;

[ApiVersion(2, 1)]
public class Plugin : TerrariaPlugin
{
    #region 插件信息
    public override string Name => "修改武器";
    public override string Author => "羽学";
    public override Version Version => new Version(1, 2, 6);
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
        ServerApi.Hooks.ServerChat.Register(this, this.OnChat);
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
            ServerApi.Hooks.ServerChat.Deregister(this, this.OnChat);
            GetDataHandlers.PlayerUpdate.UnRegister(this.OnPlayerUpdate);
            GetDataHandlers.ChestItemChange.UnRegister(this.OnChestItemChange);
            ServerApi.Hooks.NetGreetPlayer.Deregister(this, this.OnGreetPlayer);
            TShockAPI.Commands.ChatCommands.RemoveAll(x => x.CommandDelegate == Commands.CMD);
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 配置重载读取与写入方法
    public static Database DB = new();
    internal static Configuration Config = new();
    private static void ReloadConfig(ReloadEventArgs args)
    {
        LoadConfig();
        args.Player.SendInfoMessage("[修改武器]重新加载配置完毕。");
        if (Config.PublicWeapons && Config.ItemDatas != null)
        {
            PublicWeaponsMess();
            SyncPw();
        }
    }

    private static void LoadConfig()
    {
        Config = Read();
        WriteName();
        Config.Write();
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

        // 检查并初始化 Config.AllData
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
                Join = false,
                ReadCount = Config.ReadCount,
                Process = 0,
                ReadTime = DateTime.UtcNow,
                SyncTime = DateTime.UtcNow,
                Dict = new Dictionary<string, List<Database.PlayerData.ItemData>>()
            };
            DB.AddData(newData);
        }
        else if (data.Join)
        {
            //自动更新模式每次进服都会加2次重读次数
            if (Config.Auto == 1)
            {
                data.ReadCount += 2;
            }

            data.Process = 2;
            DB.UpdateData(data);

            Commands.UpdataRead(plr, data);
        }
    }
    #endregion

    #region 自动模式与公用武器数据写入方法
    private void OnPlayerUpdate(object? sender, GetDataHandlers.PlayerUpdateEventArgs e)
    {
        var plr = e.Player;
        var tplr = plr.TPlayer;
        var datas = DB.GetData(plr.Name);
        var Sel = plr.SelectedItem;
        if (plr == null || !plr.IsLoggedIn || !plr.Active || datas == null || !Config.Enabled)
        {
            return;
        }

        //自动模式
        if (Config.Auto == 1)
        {
            var flag = false;

            if (datas.Dict.TryGetValue(plr.Name, out var DataList))
            {
                foreach (var data in DataList)
                {
                    if (datas.Process == 0)
                    {
                        if (!tplr.controlUseItem) continue;

                        if (Sel.type == data.type)
                        {
                            if (Sel.ammo != data.ammo)
                            {
                                plr.SendInfoMessage($"《[c/AD89D5:自][c/D68ACA:动][c/DF909A:重][c/E5A894:读]》 玩家:{plr.Name}");
                                plr.SendMessage($"《[c/FCFE63:弹药转换]》[c/15EDDB:{Lang.GetItemName(Sel.type)}] " +
                                    $"[c/FF6863:{Sel.ammo}] => [c/8AD0EA:{data.ammo}]", 255, 244, 155);

                                flag = true;
                            }

                            if (Sel.prefix != data.prefix && Sel.prefix != 0)
                            {
                                var pr = TShock.Utils.GetPrefixById(data.prefix);
                                if (string.IsNullOrEmpty(pr))
                                {
                                    pr = "无";
                                }
                                plr.SendInfoMessage($"《[c/AD89D5:自][c/D68ACA:动][c/DF909A:重][c/E5A894:读]》 玩家:{plr.Name}");
                                plr.SendMessage($"《[c/FCFE63:词缀转换]》[c/15EDDB:{Lang.GetItemName(Sel.type)}] " +
                                    $"[c/FF6863:{pr}] => " +
                                    $"[c/8AD0EA:{TShock.Utils.GetPrefixById(Sel.prefix)}]", 255, 244, 155);

                                data.prefix = Sel.prefix;
                                flag = true;
                            }

                            if (flag)
                            {
                                datas.Process = 1;
                                break;
                            }
                        }
                    }
                }
            }

            if (datas.Process == 1)
            {
                for (int i = 0; i < plr.TPlayer.inventory.Length; i++)
                {
                    var inv = plr.TPlayer.inventory[i];
                    if (inv.type == 4346)
                    {
                        inv.TurnToAir();
                        plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, i);
                        plr.GiveItem(5391, 1);
                    }
                }

                datas.Process = 2;
                DB.UpdateData(datas);
                Commands.UpdataRead(plr, datas);
            }
        }

        //公用武器
        if (Config.PublicWeapons)
        {
            var now = DateTime.UtcNow;
            if ((now - datas.SyncTime).TotalSeconds >= Config.SyncTime)
            {
                if (!datas.Dict.ContainsKey(plr.Name))
                {
                    datas.Dict[plr.Name] = new List<Database.PlayerData.ItemData>();
                }

                foreach (var item in Config.ItemDatas!)
                {
                    var data = datas.Dict[plr.Name].FirstOrDefault(d => d.type == item.type);

                    if (data == null)
                    {
                        datas.Dict[plr.Name].Add(new Database.PlayerData.ItemData(item.type, item.stack, item.prefix, item.damage, item.scale, item.knockBack, item.useTime, item.useAnimation, item.shoot, item.shootSpeed, item.ammo, item.useAmmo, item.color));
                    }
                    else
                    {
                        data.stack = item.stack;
                        data.prefix = item.prefix;
                        data.damage = item.damage;
                        data.scale = item.scale;
                        data.knockBack = item.knockBack;
                        data.useTime = item.useTime;
                        data.useAnimation = item.useAnimation;
                        data.shoot = item.shoot;
                        data.shootSpeed = item.shootSpeed;
                        data.ammo = item.ammo;
                        data.useAmmo = item.useAmmo;
                        data.color = item.color;
                    }
                }

                datas.SyncTime = now;
                DB.UpdateData(datas);
            }
        }
    }
    #endregion

    #region 同步配置中的公用武器结构到玩家数据中
    private static void SyncPw()
    {
        var AllData = DB.GetAll();

        foreach (var data in AllData)
        {
            if (data.Dict != null && data.Dict.TryGetValue(data.Name, out var dataList))
            {
                var RM = dataList.Where(data => !Config.ItemDatas!.Any(item => item.type == data.type)).ToList();

                foreach (var remove in RM)
                {
                    dataList.Remove(remove);
                }

                if (RM.Any())
                {
                    DB.UpdateData(data);
                }
            }
        }
    }
    #endregion

    #region 获取公用武器的物品中文名
    public static void WriteName()
    {
        foreach (var item in Config.ItemDatas!)
        {
            var name = Lang.GetItemNameValue(item.type);
            if (item.Name == "")
            {
                item.Name = name;
            }
        }
    }
    #endregion

    #region 播报公用武器变动数值方法
    public static void PublicWeaponsMess()
    {
        var itemName = new HashSet<int>();

        if (Config.Title.Any())
        {
            TShock.Utils.Broadcast(Config.Title + "[c/AD89D5:公][c/D68ACA:用][c/DF909A:武][c/E5A894:器] 已更新公用武器", 240, 255, 150);
        }

        foreach (var item in Config.ItemDatas)
        {
            if (!itemName.Contains(item.type))
            {
                itemName.Add(item.type);
            }

            var user = TShock.UserAccounts.GetUserAccounts();
            foreach (var acc in user)
            {
                var plr = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Active && p.Name == acc.Name);
                if (plr == null) continue;
                var datas = DB.GetData(plr.Name);
                var data = datas.Dict[plr.Name].FirstOrDefault(d => d.type == item.type);
                if (data != null)
                {
                    var diffs = CompareItem2(item, data.type, data.stack, data.prefix, data.damage, data.scale,
                        data.knockBack, data.useTime, data.useAnimation, data.shoot, data.shootSpeed,
                        data.ammo, data.useAmmo, data.color);

                    if (diffs.Count > 0)
                    {
                        var mess = new StringBuilder($"[c/92C5EC:{Lang.GetItemName(item.type)}] ");
                        foreach (var diff in diffs)
                        {
                            mess.Append($" {diff.Key}{diff.Value}");
                        }
                        plr.SendMessage($"{mess}", 244, 255, 150);
                    }
                }
            }
        }
    }
    #endregion

    #region 对比公用武器配置文件与玩家数据差异 列出给玩家看
    internal static Dictionary<string, object> CompareItem2(Configuration.ItemData item, int type, int stack, byte prefix, int damage, float scale, float knockBack, int useTime, int useAnimation, int shoot, float shootSpeed, int ammo, int useAmmo, Color color)
    {
        string ColorToHex(Color color)
        {
            return $"{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        var pr = TShock.Utils.GetPrefixById(item.prefix);
        if (string.IsNullOrEmpty(pr))
        {
            pr = "无";
        }

        var diff = new Dictionary<string, object>();
        if (item.type != type) diff.Add($"{Lang.GetItemNameValue(item.type)}", item.type);
        if (item.stack != stack) diff.Add("数量", item.stack);
        if (item.prefix != prefix) diff.Add("前缀", pr);
        if (item.damage != damage) diff.Add("伤害", item.damage);
        if (item.scale != scale) diff.Add("大小", item.scale);
        if (item.knockBack != knockBack) diff.Add("击退", item.knockBack);
        if (item.useTime != useTime) diff.Add("用速", item.useTime);
        if (item.useAnimation != useAnimation) diff.Add("攻速", item.useAnimation);
        if (item.shoot != shoot) diff.Add("弹幕", item.shoot);
        if (item.shootSpeed != shootSpeed) diff.Add("射速", item.shootSpeed);
        if (item.ammo != ammo) diff.Add("弹药", item.ammo);
        if (item.useAmmo != useAmmo) diff.Add("发射器", item.useAmmo);
        if (item.color != color) diff.Add("颜色", ColorToHex(item.color));

        return diff;
    }
    #endregion

    #region 发送指令：检查玩家背包是否有修改物品 有则重读
    private void OnChat(ServerChatEventArgs e)
    {
        var plr = TShock.Players[e.Who];
        var datas = DB.GetData(plr.Name);
        if (plr == null || !plr.IsLoggedIn || !plr.Active || datas == null || !Config.Enabled || Config.Auto != 1)
        {
            return;
        }

        var flag = false;
        var flag2 = false;

        if (e.Text.StartsWith(TShock.Config.Settings.CommandSpecifier) || e.Text.StartsWith(TShock.Config.Settings.CommandSilentSpecifier))
        {
            flag = true;
        }

        if (Config.Text.Any(text => e.Text.Contains(text)))
        {
            flag2 = true;
        }

        if (flag && flag2)
        {
            if (datas.Dict.TryGetValue(plr.Name, out var DataList))
            {
                foreach (var data in DataList)
                {
                    for (int i = 0; i < plr.TPlayer.inventory.Length; i++)
                    {
                        var inv = plr.TPlayer.inventory[i];
                        if (inv.type == data.type)
                        {
                            plr.SendInfoMessage($"《[c/AD89D5:自][c/D68ACA:动][c/DF909A:重][c/E5A894:读]》 玩家:{plr.Name}\n" +
                            $"检测到经济指令 重读物品:[c/4C95DD:{Lang.GetItemNameValue(data.type)}]!");

                            datas.Process = 2;
                            DB.UpdateData(datas);
                            Commands.UpdataRead(plr, datas);
                        }
                    }
                }
            }
        }
    }
    #endregion

    #region 修改武器掉落的清理方法
    private void OnItemDrop(object? sender, ItemDropEventArgs e)
    {
        var plr = e.Player;
        var datas = DB.GetData(plr.Name);

        if (plr == null || !plr.IsLoggedIn || !plr.Active ||
            datas == null || !Config.Enabled || !Config.ClearItem ||
            datas.Process == 2 || plr.HasPermission("mw.admin") ||
            Config.ExemptItems.Contains(e.ID))
        {
            return;
        }

        if (datas.Dict.TryGetValue(plr.Name, out var DataList))
        {
            foreach (var data in DataList)
            {
                if (data.type == e.Type)
                {
                    plr.SendInfoMessage($"《[c/AD89D5:清][c/D68ACA:理][c/DF909A:警][c/E5A894:告]》 玩家:{plr.Name}\n" +
                        $"禁止乱丢修改物品:[c/4C95DD:{Lang.GetItemNameValue(e.Type)}]!");
                    e.Handled = true;
                    break;
                }
            }
        }
    }
    #endregion

    #region 箱子内出现修改武器的清理方法
    private void OnChestItemChange(object? sender, ChestItemEventArgs e)
    {
        var plr = e.Player;
        var datas = DB.GetData(plr.Name);

        if (plr == null || !plr.IsLoggedIn || !plr.Active ||
            datas == null || !Config.Enabled || !Config.ClearItem ||
            plr.HasPermission("mw.admin") ||
            Config.ExemptItems.Contains(e.ID))
        {
            return;
        }

        if (datas.Dict.TryGetValue(plr.Name, out var DataList))
        {
            foreach (var data in DataList)
            {
                if (data.type == e.Type)
                {
                    plr.SendInfoMessage($"《[c/AD89D5:清][c/D68ACA:理][c/DF909A:警][c/E5A894:告]》 玩家:{plr.Name}\n" +
                        $"修改物品禁止放箱子:[c/4C95DD:{Lang.GetItemNameValue(e.Type)}]!");
                    e.Handled = true;
                    break;
                }
            }
        }
    }
    #endregion

}