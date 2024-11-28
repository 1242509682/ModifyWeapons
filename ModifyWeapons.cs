﻿using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace ModifyWeapons;

[ApiVersion(2, 1)]
public class ModifyWeapons : TerrariaPlugin
{
    #region 插件信息
    public override string Name => "修改武器";
    public override string Author => "羽学";
    public override Version Version => new Version(1, 1, 0);
    public override string Description => "修改储存玩家武器数据并自动重读,可使用/mw指令给予玩家指定属性的物品";
    #endregion

    #region 注册与释放
    public ModifyWeapons(Main game) : base(game) { }
    public override void Initialize()
    {
        LoadConfig();
        LoadAllData();
        GeneralHooks.ReloadEvent += ReloadConfig;
        ServerApi.Hooks.NetGreetPlayer.Register(this, this.OnGreetPlayer);
        TShockAPI.Commands.ChatCommands.Add(new Command("mw.use", Commands.MwCmd, "mw")
        {
            AllowServer = true,
        });
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            ServerApi.Hooks.NetGreetPlayer.Deregister(this, this.OnGreetPlayer);
            TShockAPI.Commands.ChatCommands.RemoveAll(x => x.CommandDelegate == Commands.MwCmd);
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 配置重载读取与写入方法
    internal static Configuration Config = new();
    private static void ReloadConfig(ReloadEventArgs args = null!)
    {
        LoadConfig();
        foreach (var all in Config.data)
        {
            DB.UpdateData(all);
        }
        args.Player.SendInfoMessage("[修改武器]重新加载配置完毕。");
    }
    private static void LoadConfig()
    {
        Config = Configuration.Read();
        Config.Write();
    }
    #endregion

    #region 加载所有数据
    public static Database DB = new();
    public static void LoadAllData()
    {
        try
        {
            var allData = DB.LoadData();
            foreach (var data in allData)
            {
                // 检查是否已经存在该玩家的数据
                if (!Config.data.Any(d => d.Name == data.Name))
                {
                    Config.data.Add(data);
                }
                else
                {
                    // 如果存在，可以选择更新现有数据
                    var data2 = Config.data.FirstOrDefault(d => d.Name == data.Name);
                    if (data2 != null)
                    {
                        // 更新现有数据
                        data2.ItemId = data.ItemId;
                        data2.Prefix = data.Prefix;
                        data2.Damage = data.Damage;
                        data2.Scale = data.Scale;
                        data2.KnockBack = data.KnockBack;
                        data2.UseTime = data.UseTime;
                        data2.UseAnimation = data.UseAnimation;
                        data2.Shoot = data.Shoot;
                        data2.ShootSpeed = data.ShootSpeed;
                        data2.ReadTime = data.ReadTime;
                        data2.ReadCount = data.ReadCount;
                    }
                }
            }

            // 保存配置文件
            Config.Write();
        }
        catch (Exception ex)
        {
            TShock.Log.Error($"加载数据时发生错误: {ex.Message}");
        }
    }
    #endregion

    #region 玩家加入服务器后创建配置方法
    private void OnGreetPlayer(GreetPlayerEventArgs args)
    {
        var plr = TShock.Players[args.Who];

        if (!Config.Enabled || plr == null)
        {
            return;
        }

        // 从数据库中获取玩家数据
        var data = DB.GetData(plr.Name);

        if (data == null)
        {
            if (plr.SelectedItem != null)
            {
                // 如果数据库中没有该玩家的数据，则创建
                data = new Configuration.PlayerData
                {
                    Name = plr.Name,
                    Enabled = false,
                    ItemId = plr.SelectedItem.type,
                    Prefix = plr.SelectedItem.prefix,
                    Damage = plr.SelectedItem.damage,
                    Scale = plr.SelectedItem.scale,
                    KnockBack = plr.SelectedItem.knockBack,
                    UseTime = plr.SelectedItem.useTime,
                    UseAnimation = plr.SelectedItem.useAnimation,
                    Shoot = plr.SelectedItem.shoot,
                    ShootSpeed = plr.SelectedItem.shootSpeed,
                    ReadTime = DateTime.UtcNow,
                    ReadCount = Config.ReadCount
                };
                Config.data.Add(data);
                DB.UpdateData(data);
            }
        }
        else
        {
            if (data.Enabled)
            {
                Commands.UpdataRead(plr, data);
            }
        }

        // 保存配置文件
        Config.Write();
    }
    #endregion

}