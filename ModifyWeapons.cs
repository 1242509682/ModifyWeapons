using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using static ModifyWeapons.Configuration;

namespace ModifyWeapons;

[ApiVersion(2, 1)]
public class Plugin : TerrariaPlugin
{
    #region 插件信息
    public override string Name => "修改武器";
    public override string Author => "羽学";
    public override Version Version => new Version(1, 2, 4);
    public override string Description => "修改玩家物品数据并自动储存重读,可使用/mw指令给予玩家指定属性的物品";
    #endregion

    #region 注册与释放
    public Plugin(Main game) : base(game) { }
    public override void Initialize()
    {
        LoadConfig();
        GeneralHooks.ReloadEvent += ReloadConfig;
        GetDataHandlers.PlayerUpdate.Register(this.OnPlayerUpdate);
        ServerApi.Hooks.NetGreetPlayer.Register(this, this.OnGreetPlayer);
        TShockAPI.Commands.ChatCommands.Add(new Command("mw.use", Commands.CMD, "修改武器", "mw"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            GetDataHandlers.PlayerUpdate.UnRegister(this.OnPlayerUpdate);
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
            Commands.UpdataRead(plr, data);

            plr.SendMessage($"【触发重读】 进服自动修正修改数值", 255, 244, 155);
            if (Config.Auto != 1 && !plr.HasPermission("mw.admin") && !plr.HasPermission("mw.cd"))
            {
                plr.SendMessage($"【提示】消耗[c/FF6863:1次]重读次数 剩余:[c/8AD0EA:{data.ReadCount}]次", 255, 244, 155);
                plr.SendMessage($"可输入指令关闭进服重读:[c/8AD0EA:/mw join]", 255, 244, 155);
            }
        }
    }
    #endregion

    #region 自动修正参数方法
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

        var last = 0f;
        var now = DateTime.UtcNow;
        if (datas.ReadTime != default)
        {
            // 上次重读时间，保留2位小数
            last = (float)Math.Round((now - datas.ReadTime).TotalSeconds, 2);
        }

        if (datas.Dict.TryGetValue(plr.Name, out var DataList))
        {
            foreach (var data in DataList)
            {
                if (datas.Process == 0)
                {
                    if (Sel.type != data.type && !tplr.controlUseItem)
                    {
                        continue;
                    }

                    if (Sel.ammo != data.ammo)
                    {
                        plr.SendMessage($"《[c/AD89D5:修][c/D68ACA:改][c/DF909A:武][c/E5A894:器]》触发自动更新![c/4C95DD:请勿乱动!]", 255, 244, 155);
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
                        plr.SendMessage($"《[c/AD89D5:修][c/D68ACA:改][c/DF909A:武][c/E5A894:器]》触发自动更新![c/4C95DD:请勿乱动!]", 255, 244, 155);
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

        if (datas.Process == 1 && last >= Config.AutoTimer)
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

            datas.Process = 0;
            datas.ReadTime = now;
            DB.UpdateData(datas);
            Commands.ReloadItem(plr, datas);
        }
    }
    #endregion
}