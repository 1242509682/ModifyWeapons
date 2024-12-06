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
    public override Version Version => new Version(1, 2, 5);
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
    }
    private static void LoadConfig()
    {
        Config = Read();
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

        // 检查并初始化 Config.data
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
                Dict = new Dictionary<string, List<Database.PlayerData.ItemData>>()
            };
            DB.AddData(newData);
        }
        else if (data.Join)
        {
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

    #region 识别玩家将修改为弹药的物品作为武器使用 或词缀与数据不同的重读方法
    private void OnPlayerUpdate(object? sender, GetDataHandlers.PlayerUpdateEventArgs e)
    {
        var plr = e.Player;
        var tplr = plr.TPlayer;
        var datas = DB.GetData(plr.Name);
        var Sel = plr.SelectedItem;
        if (plr == null || !plr.IsLoggedIn || !plr.Active || datas == null || !Config.Enabled || Config.Auto != 1)
        {
            return;
        }

        if (datas.Dict.TryGetValue(plr.Name, out var DataList))
        {
            foreach (var data in DataList)
            {
                if (datas.Process == 0)
                {
                    if (Sel.type == data.type && tplr.controlUseItem)
                    {
                        if (Sel.ammo != data.ammo)
                        {
                            plr.SendInfoMessage($"《[c/AD89D5:自][c/D68ACA:动][c/DF909A:重][c/E5A894:读]》 玩家:{plr.Name}");
                            plr.SendMessage($"《[c/FCFE63:弹药转换]》[c/15EDDB:{Lang.GetItemName(Sel.type)}] " +
                                $"[c/FF6863:{Sel.ammo}] => [c/8AD0EA:{data.ammo}]", 255, 244, 155);

                            datas.Process = 1;
                            DB.UpdateData(datas);
                            break;
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
                            datas.Process = 1;
                            DB.UpdateData(datas);
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