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
    public override Version Version => new Version(1, 2, 0);
    public override string Description => "修改玩家物品数据并自动储存重读,可使用/mw指令给予玩家指定属性的物品";
    #endregion

    #region 注册与释放
    public Plugin(Main game) : base(game) { }
    public override void Initialize()
    {
        LoadConfig();
        GeneralHooks.ReloadEvent += ReloadConfig;
        ServerApi.Hooks.NetGreetPlayer.Register(this, this.OnGreetPlayer);
        TShockAPI.Commands.ChatCommands.Add(new Command("mw.use", Commands.CMD, "修改武器", "mw"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            ServerApi.Hooks.NetGreetPlayer.Deregister(this, this.OnGreetPlayer);
            TShockAPI.Commands.ChatCommands.RemoveAll(x => x.CommandDelegate == Commands.CMD);
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 配置重载读取与写入方法
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

    #region 创建玩家数据方法
    private void OnGreetPlayer(GreetPlayerEventArgs args)
    {
        if (!Config.Enabled)
        {
            return;
        }

        var plr = TShock.Players[args.Who];
        var data = Config.data.FirstOrDefault(p => p.Name == plr.Name);

        if (plr == null)
        {
            return;
        }

        // 如果启用进服只给管理建数据，且玩家数据为空的情况下直接返回
        var adamin = plr.HasPermission("mw.admin");

        // 检查并初始化 Config.Dict
        if (!Config.Dict.ContainsKey(plr.Name))
        {
            if (Config.Enabled2 && !adamin)
            {
                return;
            }

            Config.Dict[plr.Name] = new List<ItemData>();
        }

        // 检查并初始化 Config.data
        if (data == null)
        {
            if (Config.Enabled2 && !adamin)
            {
                return;
            }

            // 如果玩家数据不存在，创建新数据并保存
            Config.data.Add(new Configuration.PlayerData
            {
                Name = plr.Name,
                Hand = true,
                Join = false,
                ReadCount = Config.ReadCount,
                ReadTime = DateTime.UtcNow
            });

            Config.Write();
        }
        else if (data.Join)
        {
            Commands.UpdataRead(plr, data);
        }
    }
    #endregion

}