using System.Data;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using static ModifyWeapons.Plugin;
using static ModifyWeapons.MessageManager;
using ModifyWeapons.Progress;

namespace ModifyWeapons;

public class Commands
{
    #region 主体指令方法
    public static void CMD(CommandArgs args)
    {
        var plr = args.Player;
        var data = DB.GetData(plr.Name);

        const string SELF = "{0}"; //站位符

        if (!Config.Enabled || plr == null)
        {
            return;
        }

        if (args.Parameters.Count == 0)
        {
            HelpCmd(args.Player);
            return;
        }

        if (args.Parameters.Count >= 1)
        {
            switch (args.Parameters[0].ToLower())
            {
                case "h":
                case "hand":
                    if (data != null)
                    {
                        data.Hand = !data.Hand;
                        plr.SendInfoMessage(data.Hand ?
                        $"玩家 [{plr.Name}] 已[c/92C5EC:启用]获取手持物品信息功能。" :
                        $"玩家 [{plr.Name}] 已[c/92C5EC:关闭]获取手持物品信息功能。");
                        DB.UpdateData(data);
                    }
                    return;

                case "j":
                case "join":
                    if (data != null)
                    {
                        data.Join = !data.Join;
                        plr.SendInfoMessage(data.Join ?
                            $"玩家 [{plr.Name}] 已[c/92C5EC:启用]进服重读功能。" :
                            $"玩家 [{plr.Name}] 已[c/92C5EC:关闭]进服重读功能。");
                        DB.UpdateData(data);
                    }
                    return;

                case "rd":
                case "read":
                    if (data != null)
                    {
                        if (Config.Alone)
                        {
                            DB.AddReadCount(plr.Name, 1);
                        }

                        UpdataRead(plr, data);

                        if (!plr.HasPermission("mw.admin") && !plr.HasPermission("mw.cd"))
                        {
                            plr.SendInfoMessage($"剩余修改物品重读次数为:[C/A7D3D6:{data.ReadCount}]");
                        }
                    }
                    return;

                case "op":
                case "open":
                    if (plr.HasPermission("mw.admin"))
                    {
                        var other = args.Parameters[1];
                        var datas = DB.GetData(other);
                        if (datas != null)
                        {
                            datas.Join = !datas.Join;
                            DB.UpdateData(datas);
                            plr.SendSuccessMessage(datas.Join ?
                                $"{datas.Name}的进服重读:[c/92C5EC:启用]" :
                                $"{datas.Name}的进服重读:[c/E8585B:关闭]");
                        }
                    }
                    return;

                case "del":
                    if (plr.HasPermission("mw.admin"))
                    {
                        var other = args.Parameters[1].Equals(SELF, StringComparison.OrdinalIgnoreCase)
                        ? plr.Name
                        : args.Parameters[1];

                        if (DB.DeleteData(other))
                        {
                            plr.SendSuccessMessage($"已[c/E8585B:删除] {other} 的数据！");
                        }
                        else
                        {
                            plr.SendSuccessMessage($"未能[c/E8585B:找到] {other} 的数据！");
                        }
                    }
                    return;

                case "add":
                    if (plr.HasPermission("mw.admin"))
                    {
                        var other = args.Parameters[1]; // 获取玩家名称
                        if (int.TryParse(args.Parameters[2], out var num2))
                        {
                            var NewCount = DB.AddReadCount(other, num2);

                            if (NewCount == true)
                            {
                                plr.SendSuccessMessage($"已[c/E8585B:添加] {other} 的重读次数！新的重读次数为: [c/74E55D:{NewCount}]");
                            }
                            else
                            {
                                plr.SendErrorMessage($"无法找到玩家 [c/E8585B:{other}] 或更新重读次数失败。");
                            }
                        }
                        else
                        {
                            plr.SendErrorMessage("请输入有效的整数作为重读次数。");
                        }
                    }
                    return;

                case "rds":
                case "reads":
                    if (plr.HasPermission("mw.admin"))
                    {
                        if (args.Parameters.Count > 1 && int.TryParse(args.Parameters[1], out var num))
                        {
                            if (num == 1)
                            {
                                var user = TShock.UserAccounts.GetUserAccounts();
                                foreach (var acc in user)
                                {
                                    var datas = DB.GetData(acc.Name);
                                    var plr2 = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Active && p.Name == acc.Name);
                                    if (plr2 != null && datas != null) // 如果目标玩家在线，则发送消息并直接重读数值
                                    {
                                        plr2.SendMessage($"\n管理员 [c/D4E443:{plr.Name}] 已为所有玩家[c/92C5EC:手动重读]修改物品", 0, 196, 177);
                                        ReloadItem(plr2); // 重读物品数据
                                    }
                                }
                            }

                            if (num == 2)
                            {
                                var ALL = DB.GetAll();
                                bool Enabled = ALL.Any(player => player.Join);
                                foreach (var plrs in ALL)
                                {
                                    plrs.Join = !Enabled;
                                    DB.UpdateData(plrs);
                                }
                                plr.SendSuccessMessage(!Enabled ?
                                    $"已[c/92C5EC:开启]所有玩家进服重读功能！" :
                                    $"已[c/E8585B:关闭]所有玩家进服重读功能！");
                            }
                        }
                        else
                        {
                            plr.SendMessage($"[c/ED3241:注：]指令格式 [c/FFF540:/mw reads 1 或 2]\n" +
                                $"[c/FFB357:1] 帮助所有在线玩家手动重读\n" +
                                $"[c/4C95DD:2] 切换所有玩家进服重读功能", 100, 210, 190);
                        }
                    }
                    return;

                case "cr":
                case "clear":
                    if (plr.HasPermission("mw.admin"))
                    {
                        if (!Config.ClearItem)
                        {
                            Config.ClearItem = true;
                            Config.Write();
                            plr.SendSuccessMessage($"已[c/92C5EC:开启]自动清理功能！");
                        }
                        else
                        {
                            Config.ClearItem = false;
                            Config.Write();
                            plr.SendSuccessMessage($"已[c/E8585B:关闭]自动清理功能！");
                        }
                    }
                    return;

                case "rs":
                case "reset":
                    if (plr.HasPermission("mw.admin"))
                    {
                        DB.ClearData();
                        plr.SendSuccessMessage($"已[c/E8585B:清空]所有人的修改物品数据！");
                    }
                    return;

                case "l":
                case "list":
                    {
                        int page = 1;

                        if (args.Parameters.Count > 1)
                        {
                            // 尝试解析第二个参数为整数页码
                            if (!int.TryParse(args.Parameters[1], out page))
                            {
                                plr.SendErrorMessage("页码必须是一个有效的整数。");
                                return;
                            }
                        }

                        ListItem(plr, page);
                        return;
                    }

                case "set":
                case "s":
                    if (plr.HasPermission("mw.admin"))
                    {
                        if (args.Parameters.Count >= 3)
                        {
                            var Sel = plr.SelectedItem;
                            Dictionary<string, string> ItemVal = new Dictionary<string, string>();
                            Parse(args.Parameters, out ItemVal, 1);
                            UpdatePT(plr.Name, Sel, ItemVal);
                            SetItem(plr, Sel.damage, Sel.stack, Sel.prefix, Sel.scale, Sel.knockBack, Sel.useTime, Sel.useAnimation, Sel.shoot, Sel.shootSpeed, Sel.ammo, Sel.useAmmo, Sel.color);
                        }
                        else
                        {
                            SetError(plr);
                        }
                    }
                    break;

                case "pw":
                case "p":
                    if (!plr.HasPermission("mw.admin"))
                    {
                        plr.SendMessage("你没有权限执行此命令。", 255, 0, 0);
                        break;
                    }

                    if (args.Parameters.Count < 2)
                    {
                        PwError(plr);
                        return;
                    }

                    var cmd = args.Parameters[1].ToLower();

                    switch (cmd)
                    {
                        case "on":
                            Config.PublicWeapons = true;
                            Config.Write();
                            plr.SendSuccessMessage("已开启公用武器");
                            break;

                        case "off":
                            Config.PublicWeapons = false;
                            Config.Write();
                            plr.SendSuccessMessage("已关闭公用武器");
                            break;

                        case "del":
                            if (args.Parameters.Count < 3)
                            {
                                plr.SendMessage("使用方法: /mw pw del <物品名>", 255, 0, 0);
                                return;
                            }

                            var name = string.Join(" ", args.Parameters.Skip(2));
                            var items = TShock.Utils.GetItemByIdOrName(name);

                            if (items.Count > 1)
                            {
                                args.Player.SendMultipleMatchError(items.Select(i => i.Name));
                                return;
                            }

                            if (items.Count == 0)
                            {
                                plr.SendMessage("找不到指定的物品。", 255, 0, 0);
                                return;
                            }

                            var item = items[0];
                            PublicWeapons.RemovePublicWeapons(item.type, plr);

                            if (DB.RemovePwData(item.type))
                            {
                                plr.SendSuccessMessage($"已成功从所有玩家的公用武器配置中移除物品 [c/92C5EC:{Lang.GetItemNameValue(item.type)}]");
                            }
                            else
                            {
                                plr.SendMessage("没有找到与指定名称匹配的物品或无法移除。", 255, 0, 0);
                            }
                            break;

                        case "进度":
                        case "jd":
                        case "pg":
                            if (args.Parameters.Count >= 3) // 参数不足时显示帮助信息和进度列表
                            {
                                // 获取物品名称和进度值
                                string pname = args.Parameters[2];
                                string val = args.Parameters[3];

                                // 获取物品
                                var items2 = TShock.Utils.GetItemByIdOrName(pname);
                                if (items2.Count == 0)
                                {
                                    plr.SendMessage($"找不到物品: {pname}", 255, 0, 0);
                                    return;
                                }
                                var item3 = items2[0]; // 取第一个匹配的物品

                                // 解析进度值 - 支持数字、英文名和中文名
                                ModifyWeapons.Progress.ProgressType type;

                                if (int.TryParse(val, out int num))
                                {
                                    // 尝试通过数字解析
                                    if (Enum.IsDefined(typeof(ModifyWeapons.Progress.ProgressType), num))
                                    {
                                        type = (ModifyWeapons.Progress.ProgressType)num;
                                    }
                                    else
                                    {
                                        plr.SendMessage($"无效的进度值: {val}", 255, 0, 0);
                                        return;
                                    }
                                }
                                else
                                {
                                    // 尝试通过名称解析（支持中文名）
                                    var PtName = FindProgressTypeByChineseName(val);
                                    if (PtName.HasValue)
                                    {
                                        type = PtName.Value;
                                    }
                                    else
                                    {
                                        // 尝试通过英文名解析
                                        if (!Enum.TryParse(val, true, out type))
                                        {
                                            plr.SendMessage($"无效的进度值: {val}", 255, 0, 0);
                                            return;
                                        }
                                    }
                                }

                                if (Config.PublicWeapons && Config.ItemDatas != null && Config.ItemDatas.Count > 0)
                                {
                                    bool found = false;
                                    foreach (var citem in Config.ItemDatas.ToList())
                                    {
                                        if (citem.type == item3.type)
                                        {
                                            citem.Progress = type;
                                            found = true;
                                        }
                                    }

                                    if (found)
                                    {
                                        Config.Write();
                                        plr.SendMessage($"已更新公用武器 [{Lang.GetItemNameValue(item3.type)}] 的进度为: {GetProgressChineseName(type)}", 0, 255, 0);
                                    }
                                    else
                                    {
                                        plr.SendMessage($"未找到公用武器 [{Lang.GetItemNameValue(item3.type)}] 的配置。", 255, 0, 0);
                                    }
                                }
                            }
                            else
                            {
                                ProgressError(plr);
                            }
                            break;

                        default:
                            var newItem = TShock.Utils.GetItemByIdOrName(cmd);
                            if (newItem.Count == 0)
                            {
                                plr.SendMessage("找不到指定的物品。", 255, 0, 0);
                                return;
                            }

                            if (newItem.Count > 1)
                            {
                                args.Player.SendMultipleMatchError(newItem.Select(i => i.Name));
                                return;
                            }

                            var item2 = newItem[0];

                            // 解析属性
                            Dictionary<string, string> ItemVal = new Dictionary<string, string>();
                            Parse(args.Parameters, out ItemVal, 2);
                            var user = TShock.UserAccounts.GetUserAccounts();
                            foreach (var acc in user)
                            {
                                var plrs = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Active && p.Name == acc.Name);
                                if (plrs != null)
                                {
                                    UpdatePT(plrs.Name, item2, ItemVal); // 调用更新逻辑
                                    PublicWeapons.UpdatePublicWeapons(plrs, item2);
                                }
                            }

                            break;
                    }
                    break;

                case "up":
                case "update":
                    if (plr.HasPermission("mw.admin"))
                    {
                        if (args.Parameters.Count >= 3)
                        {
                            var other = args.Parameters[1].Equals(SELF, StringComparison.OrdinalIgnoreCase)
                            ? plr.Name
                            : args.Parameters[1];

                            var items = args.Parameters[2];
                            var Items = TShock.Utils.GetItemByIdOrName(items);
                            if (Items.Count > 1)
                            {
                                args.Player.SendMultipleMatchError(Items.Select(i => i.Name));
                                return;
                            }
                            var item = Items[0];
                            var acc = TShock.UserAccounts.GetUserAccountByName(other);
                            Dictionary<string, string> ItemVal = new Dictionary<string, string>();
                            Parse(args.Parameters, out ItemVal, 3);

                            if (UpdatePT2(acc.Name, item.type, ItemVal)) // 更新玩家的指定物品属性
                            {
                                // 播报给执行者
                                var datas = DB.GetData2(acc.Name, item.type);
                                if (datas == null) return;

                                if (Plugin.IsModifiedWeapon(acc.Name, item.type))
                                {
                                    var diffs = CompareItem(datas, item.type, item.stack, item.prefix, item.damage, item.scale, item.knockBack,
                                        item.useTime, item.useAnimation, item.shoot, item.shootSpeed, item.ammo, item.useAmmo, item.color);

                                    if (diffs.Count > 0) // 只有在有差异的情况下才构建和发送消息
                                    {
                                        var mess = new StringBuilder();
                                        mess.Append($"已更新玩家 [c/2F99D7:{acc.Name}] 的[c/92B9D4:{Lang.GetItemName(datas.type)}]");

                                        foreach (var diff in diffs)
                                        {
                                            mess.Append($" {diff.Key}{diff.Value}");
                                        }

                                        plr.SendMessage($"{mess}", 216, 223, 153);
                                    }
                                }
                            }
                        }
                        else
                        {
                            UpdateError(plr);
                        }
                    }
                    return;

                case "g":
                case "give":
                    if (plr.HasPermission("mw.admin"))
                    {
                        //当子命令数量超过4个时
                        if (args.Parameters.Count >= 4)
                        {
                            //占位符（SELF） 或者为第一个子命令的参数“玩家名”
                            var other = args.Parameters[1].Equals(SELF, StringComparison.OrdinalIgnoreCase)
                            ? plr.Name
                            : args.Parameters[1];

                            //第二个参数为物品名
                            var items = args.Parameters[2];
                            var Items = TShock.Utils.GetItemByIdOrName(items);

                            //物品数量＞1则返回物品名
                            if (Items.Count > 1)
                            {
                                args.Player.SendMultipleMatchError(Items.Select(i => i.Name));
                                return;
                            }
                            //将获取到的物品名赋值给item
                            var item = Items[0];
                            //设置物品默认值
                            item.SetDefaults(item.type);
                            Dictionary<string, string> ItemVal = new Dictionary<string, string>();
                            Parse(args.Parameters, out ItemVal, 3);

                            //从数据库中获取数据这个指定玩家名的数据
                            var datas = DB.GetData(other);
                            var datas2 = DB.GetData2(other, item.type);
                            if (datas == null) //数据为空 则创建数据
                            {
                                var newData = new Database.PlayerData
                                {
                                    Name = other,
                                    Hand = true,
                                    Join = true,
                                    ReadCount = Config.ReadCount,
                                };
                                DB.AddData(newData);
                                plr.SendMessage($"管理员 [c/D4E443:{plr.Name}] 已为 [c/92C5EC:{other}] 创建数据", 0, 196, 177);

                            }

                            if (datas2 == null)
                            {
                                var newData2 = new Database.MyItemData
                                {
                                    PlayerName = other,
                                    type = item.type,
                                    stack = item.stack,
                                    prefix = item.prefix,
                                    damage = item.damage,
                                    scale = item.scale,
                                    knockBack = item.knockBack,
                                    useTime = item.useTime,
                                    useAnimation = item.useAnimation,
                                    shoot = item.shoot,
                                    shootSpeed = item.shootSpeed,
                                    ammo = item.ammo,
                                    useAmmo = item.useAmmo,
                                    color = item.color,
                                };

                                DB.AddData2(newData2);//初始化添加数据
                            }
                            else
                            {
                                var plr2 = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Active && p.Name == other);
                                if (plr2 != null) //在线重读并通告玩家 物品修改信息
                                {
                                    plr2.SendMessage($"\n管理员 [c/D4E443:{plr.Name}] 已发送修改物品: [c/92C5EC:{Lang.GetItemName(item.type)}]", 0, 196, 177);

                                    var hasItem = plr2.TPlayer.inventory.Take(58).Any(x => x != null && x.type == item.type);
                                    if (!hasItem)
                                    {
                                        plr2.GiveItem(item.type, item.maxStack);
                                    }

                                    if (Config.Alone)
                                    {
                                        reads[plr2.Name] = true;
                                        Plugin.Timer[plr2.Name] = DateTime.UtcNow;
                                    }

                                    UpdatePT(plr2.Name, item, ItemVal);
                                    SaveItem(other, item.type, item.stack, item.prefix, item.damage, item.scale, item.knockBack, item.useTime, item.useAnimation, item.shoot, item.shootSpeed, item.ammo, item.useAmmo, item.color);
                                    ReloadItem(plr2); // 重读物品数据
                                }
                                else //不在线 保存
                                {
                                    UpdatePT(other, item, ItemVal);
                                    SaveItem(other, item.type, item.stack, item.prefix, item.damage, item.scale, item.knockBack, item.useTime, item.useAnimation, item.shoot, item.shootSpeed, item.ammo, item.useAmmo, item.color);
                                }

                                // 播报给执行者
                                if (datas2.PlayerName == other)
                                {
                                    if (datas2.type == item.type)
                                    {
                                        var item2 = new Item();
                                        var diffs = CompareItem(datas2, item2.type, item2.stack, item2.prefix, item2.damage, item2.scale, item2.knockBack,
                                            item2.useTime, item2.useAnimation, item2.shoot, item2.shootSpeed, item2.ammo, item2.useAmmo, item2.color);

                                        if (diffs.Count > 0) // 只有在有差异的情况下才构建和发送消息
                                        {
                                            var mess = new StringBuilder();
                                            mess.Append($"已为玩家 [c/2F99D7:{other}] 发送:");

                                            foreach (var diff in diffs)
                                            {
                                                mess.Append($" {diff.Key}{diff.Value}");
                                            }

                                            plr.SendMessage($"{mess}", 216, 223, 153);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            GiveError(plr);
                        }
                    }
                    break;

                case "all":
                    if (plr.HasPermission("mw.admin"))
                    {
                        if (args.Parameters.Count >= 2)
                        {
                            var items = args.Parameters[1];
                            var Items = TShock.Utils.GetItemByIdOrName(items);

                            if (Items.Count > 1)
                            {
                                args.Player.SendMultipleMatchError(Items.Select(i => i.Name));
                                return;
                            }

                            var item = Items[0];
                            item.SetDefaults(item.type);

                            Dictionary<string, string> ItemVal = new Dictionary<string, string>();
                            Parse(args.Parameters, out ItemVal, 2);

                            var flag = false;
                            var Creat = false;
                            var name = new HashSet<string>();
                            var mess = new StringBuilder();
                            var user = TShock.UserAccounts.GetUserAccounts();

                            foreach (var acc in user)
                            {
                                var datas = DB.GetData(acc.Name);
                                var datas2 = DB.GetData2(acc.Name, item.type);
                                if (datas == null)
                                {
                                    var newData = new Database.PlayerData
                                    {
                                        Name = acc.Name,
                                        Hand = true,
                                        Join = true,
                                        ReadCount = Config.ReadCount,
                                        ReadTime = DateTime.UtcNow,
                                    };

                                    DB.AddData(newData);

                                    if (!Creat) // 检查是否已经发送过消息
                                    {
                                        plr.SendMessage($"管理员 [c/D4E443:{plr.Name}] 已为所有玩家 创建数据", 0, 196, 177);
                                        Creat = true;
                                    }
                                }

                                if (datas2 == null)
                                {
                                    var newData2 = new Database.MyItemData
                                    {
                                        PlayerName = acc.Name,
                                        type = item.type,
                                        stack = item.stack,
                                        prefix = item.prefix,
                                        damage = item.damage,
                                        scale = item.scale,
                                        knockBack = item.knockBack,
                                        useTime = item.useTime,
                                        useAnimation = item.useAnimation,
                                        shoot = item.shoot,
                                        shootSpeed = item.shootSpeed,
                                        ammo = item.ammo,
                                        useAmmo = item.useAmmo,
                                        color = item.color,
                                    };

                                    DB.AddData2(newData2);
                                }
                                else
                                {
                                    flag = true;
                                    name.Add(acc.Name);
                                    var plr2 = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Active && p.Name == acc.Name);
                                    if (plr2 != null)// 如果目标玩家在线，则发送消息并直接重读数值
                                    {
                                        if (Config.Alone)
                                        {
                                            reads[plr2.Name] = true;
                                            Plugin.Timer[plr2.Name] = DateTime.UtcNow;
                                        }

                                        plr2.SendMessage($"\n管理员 [c/D4E443:{plr.Name}] 已发送修改物品: [c/92C5EC:{Lang.GetItemName(item.type)}]", 0, 196, 177);

                                        var hasItem = plr2.TPlayer.inventory.Take(58).Any(x => x != null && x.type == item.type);
                                        if (!hasItem)
                                        {
                                            plr2.GiveItem(item.type, item.maxStack);
                                        }

                                        UpdatePT(plr2.Name, item, ItemVal);
                                        SaveItem(plr2.Name, item.type, item.stack, item.prefix, item.damage, item.scale, item.knockBack, item.useTime, item.useAnimation, item.shoot, item.shootSpeed, item.ammo, item.useAmmo, item.color);
                                        ReloadItem(plr2);
                                    }
                                    else
                                    {
                                        UpdatePT(acc.Name, item, ItemVal);
                                        SaveItem(acc.Name, item.type, item.stack, item.prefix, item.damage, item.scale, item.knockBack, item.useTime, item.useAnimation, item.shoot, item.shootSpeed, item.ammo, item.useAmmo, item.color);
                                    }
                                }
                            }

                            SendAllLists(plr, item, flag, name, mess);
                        }
                        else
                        {
                            AllError(plr);
                        }
                    }
                    break;
                default:
                    HelpCmd(args.Player);
                    break;
            }
        }
    }
    #endregion

    #region 菜单方法
    public static void HelpCmd(TSPlayer plr)
    {
        var data = DB.GetData(plr.Name);
        var data2 = DB.GetData2(plr.Name, plr.SelectedItem.type);
        var sel = plr.SelectedItem;

        if (TSServerPlayer.Server == plr)
        {
            plr.SendMessage("\n修改武器\n" +
                "/mw clear —— [c/FFB357:自动清理]功能开关\n" +
                "/mw open 玩家名 —— 切换[c/7ECCED:别人进服]重读\n" +
                "/mw add 玩家名 次数 ——添加[c/D68ACA:重读次数]\n" +
                "/mw del 玩家名 —— [c/DF909A:删除指定]玩家数据\n" +
                "/mw up —— 修改玩家[c/AD89D5:已有物品]的指定属性\n" +
                "/mw give —— [c/4C95DD:给指定玩家]修改物品并[c/86F06E:建数据]\n" +
                "/mw all —— [c/FFF540:给所有玩家]修改物品并[c/FF6863:建数据]\n" +
                "/mw pw —— [c/F28F2B:公用武器]相关修改\n" +
                "/mw reads —— [c/D68ACA:帮助所有人]重读\n" +
                "/mw reset —— [c/ED3241:重置所有]玩家数据", 224, 210, 168);
        }

        if (data != null)
        {
            if (!plr.HasPermission("mw.admin")) //没有管理权限
            {
                plr.SendMessage("\n          [i:3455][c/AD89D5:修][c/D68ACA:改][c/DF909A:武][c/E5A894:器][i:3454]\n" +
                "/mw hand —— 获取[c/FFB357:手持物品]信息开关\n" +
                "/mw join —— 切换[c/4C95DD:进服重读]开关\n" +
                "/mw list —— [c/FF6863:列出所有]修改物品\n" +
                "/mw read —— [c/FFF540:手动重读]修改物品", 224, 210, 168);

                if (data.Hand)
                {
                    HandText(plr, sel, data, data2!);
                }
            }
            else
            {
                plr.SendMessage("\n          [i:3455][c/AD89D5:修][c/D68ACA:改][c/DF909A:武][c/E5A894:器][i:3454]\n" +
                "/mw hand —— 获取[c/FFB357:手持物品]信息开关\n" +
                "/mw join —— 切换[c/4C95DD:进服重读]开关\n" +
                "/mw list —— [c/FF6863:列出所有]修改物品\n" +
                "/mw read —— [c/FFF540:手动重读]修改物品\n" +
                "/mw clear —— [c/FFB357:自动清理]功能开关\n" +
                "/mw open 玩家名 —— 切换[c/7ECCED:别人进服]重读\n" +
                "/mw add 玩家名 次数 ——添加[c/D68ACA:重读次数]\n" +
                "/mw del 玩家名 —— [c/DF909A:删除指定]玩家数据\n" +
                "/mw up —— 修改玩家[c/AD89D5:已有物品]的指定属性\n" +
                "/mw set  —— 修改[c/FFB357:自己手持]物品属性\n" +
                "/mw give —— [c/4C95DD:给指定玩家]修改物品并[c/86F06E:建数据]\n" +
                "/mw all —— [c/FFF540:给所有玩家]修改物品并[c/FF6863:建数据]\n" +
                "/mw pw —— [c/F28F2B:公用武器]相关修改\n" +
                "/mw reads —— [c/D68ACA:帮助所有人]重读\n" +
                "/mw reset —— [c/ED3241:重置所有]玩家数据", 224, 210, 168);

                if (data.Hand)
                {
                    HandText(plr, sel, data, data2!);
                }
            }
        }
        else if (TSServerPlayer.Server != plr)
        {
            plr.SendInfoMessage("请用角色[c/D95065:重进服务器]后输入：/mw 指令查看菜单\n" +
            "羽学声明：本插件纯属[c/7E93DE:免费]请勿上当受骗", 217, 217, 217);
            return;
        }
    }
    #endregion

    #region 设置自己手上物品方法
    public static void SetItem(TSPlayer plr, int damage, int stack, byte prefix, float scale, float knockBack, int useTime, int useAnimation,
        int shoot, float shootSpeed, int ammo, int useAmmo, Color color)
    {
        var item = TShock.Utils.GetItemById(plr.SelectedItem.type);
        var MyIndex = new TSPlayer(plr.Index);
        var MyItem = Item.NewItem(null, (int)MyIndex.X, (int)MyIndex.Y, item.width, item.height, item.type, item.stack);

        if (MyItem >= 0 && MyItem < Main.item.Length)
        {
            var newItem = Main.item[MyItem];
            MyNewItem(plr, item, newItem, damage, stack, prefix, scale, knockBack, useTime, useAnimation, shoot, shootSpeed, ammo, useAmmo);

            if (plr.TPlayer.selectedItem >= 0 && plr.TPlayer.selectedItem < plr.TPlayer.inventory.Length)
            {
                plr.TPlayer.inventory[plr.TPlayer.selectedItem].SetDefaults(0);
                NetMessage.SendData(5, -1, -1, null, plr.Index, plr.TPlayer.selectedItem);
            }

            plr.SendData(PacketTypes.PlayerSlot, null, MyItem);
            plr.SendData(PacketTypes.UpdateItemDrop, null, MyItem);
            plr.SendData(PacketTypes.ItemOwner, null, MyItem);
            plr.SendData(PacketTypes.TweakItem, null, MyItem, 255f, 63f);

            SaveItem(plr.Name, newItem.type, newItem.stack, newItem.prefix, damage, scale, knockBack, useTime, useAnimation, shoot, shootSpeed, ammo, useAmmo, color);
        }
    }
    #endregion

    #region 新物品属性
    public static void MyNewItem(TSPlayer plr, Item item, Item newItem, int damage, int stack, byte prefix, float scale, float knockBack, int useTime, int useAnimation,
        int shoot, float shootSpeed, int ammo, int useAmmo)
    {
        newItem.playerIndexTheItemIsReservedFor = plr.Index;
        newItem.prefix = prefix;
        newItem.damage = damage;
        newItem.stack = stack;
        newItem.scale = scale;
        newItem.knockBack = knockBack;
        newItem.useTime = useTime;
        newItem.useAnimation = useAnimation;
        newItem.shoot = shoot;
        newItem.shootSpeed = shootSpeed;
        newItem.ammo = ammo;
        newItem.useAmmo = useAmmo;
    }
    #endregion

    #region 重读玩家背包中所有已修改物品的方法
    internal static void ReloadItem(TSPlayer plr)
    {
        if (plr == null || !plr.IsLoggedIn)
        {
            return;
        }

        var ReItemList = new List<int>();
        var player = plr.TPlayer;
        bool found = false; // 添加标志位记录是否发现无效物品

        for (int i = 0; i < player.inventory.Length; i++)
        {
            var inv = player.inventory[i];
            if (inv == null || inv.IsAir)
                continue;

            if (inv.type == 4346)
            {
                inv.TurnToAir();
                plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, i);
                plr.GiveItem(5391, 1);
                continue;
            }

            var data2 = DB.GetData2(plr.Name, inv.type);
            if (data2 == null) continue;

            bool flag = false; // 新增：标记当前物品是否应跳过

            // 修改进度检查逻辑：只检查当前物品
            if (Config.PublicWeapons && Config.ItemDatas != null)
            {
                // 直接查找当前物品类型的配置（而非遍历所有）
                var citem = Config.ItemDatas.FirstOrDefault(c => c.type == inv.type);
                if (citem != null)
                {
                    if (!ProgressChecker.IsProgress(citem.Progress))
                    {
                        var pt = citem.Progress;
                        string chineseName = GetProgressChineseName(pt);
                        plr.SendMessage($"[i/s{1}:{inv.type}] 不满足进度: [{(int)pt}] {chineseName} 禁止重读", 250, 240, 150);
                        flag = true; // 标记跳过但不中断循环
                        found = true; // 记录存在无效物品
                    }
                }
            }

            // 如果标记为跳过，则处理下一个物品
            if (flag) continue;

            // 以下是正常重读逻辑...
            var item = TShock.Utils.GetItemById(inv.type);
            var MyItem = Item.NewItem(null, (int)plr.X, (int)plr.Y, item.width, item.height, item.type, item.stack);
            var newItem = Main.item[MyItem];

            MyNewItem(plr, item, newItem, data2.damage, data2.stack, data2.prefix, data2.scale,
                      data2.knockBack, data2.useTime, data2.useAnimation, data2.shoot,
                      data2.shootSpeed, data2.ammo, data2.useAmmo);

            inv.SetDefaults(0);
            NetMessage.SendData(5, -1, -1, null, plr.Index, i);
            plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, i);
            plr.SendData(PacketTypes.UpdateItemDrop, null, MyItem);
            plr.SendData(PacketTypes.ItemOwner, null, MyItem);
            plr.SendData(PacketTypes.TweakItem, null, MyItem, 255f, 63f);

            ReItemList.Add(item.type);
        }

        // 修改最终提示逻辑
        if (ReItemList.Count == 0)
        {
            plr.SendInfoMessage(found ? "所有物品均不符合进度要求" : "未找到需要重读的修改物品");
        }
        else
        {
            ShowReadItem(plr, ReItemList.Distinct().ToList());
        }
    }
    #endregion

    #region 更新重读次数方法
    internal static void UpdataRead(TSPlayer plr, Database.PlayerData data)
    {
        if (data == null)
        {
            plr.SendErrorMessage("没有找到该玩家的物品数据。");
            return;
        }

        if (!plr.HasPermission("mw.cd") && !plr.HasPermission("mw.admin")) // 没有权限
        {
            if (data.ReadCount >= 1) // 有重读次数 直接重读
            {
                ReloadItem(plr);
                data.ReadCount = Math.Max(0, data.ReadCount - 1); // 最少1次 最多减到0
            }
            else // 没有重读次数
            {
                var last = 0f;
                var now = DateTime.UtcNow;
                if (data.ReadTime != default)
                {
                    // 上次重读时间，保留2位小数
                    last = (float)Math.Round((now - data.ReadTime).TotalSeconds, 2);
                }

                if (last >= Config.ReadTime) // 计算过去时间 自动累积重读次数 重置读取时间
                {
                    data.ReadCount++;
                    data.ReadTime = DateTime.UtcNow;
                }
                else // 冷却时间没到 播报现在的冷却时间
                {
                    plr.SendInfoMessage($"您的重读冷却:[c/5C9EE1:{last}] < [c/FF6975:{Config.ReadTime}]秒 重读次数:[c/93E0D8:{data.ReadCount}]\n" +
                        $"请等待[c/93E0D8:{Config.ReadTime}]秒后手动重读指令:[c/A7D3D6:/mw read]");
                }
            }

            DB.UpdateData(data);
        }
        else
        {
            ReloadItem(plr);
        }
    }
    #endregion

    #region 解析输入参数的距离 如:da 1
    public static void Parse(List<string> part, out Dictionary<string, string> ItemVal, int Index)
    {
        ItemVal = new Dictionary<string, string>();
        for (int i = Index; i < part.Count; i += 2)
        {
            if (i + 1 < part.Count) // 确保有下一个参数
            {
                string name = part[i].ToLower();
                string value = part[i + 1];
                ItemVal[name] = value;
            }
        }
    }
    #endregion

    #region 解析输入参数的属性名 通用方法 如:da = 伤害
    public static void UpdatePT(string name, Item item, Dictionary<string, string> itemValues)
    {
        var mess = new StringBuilder();
        var n = Lang.GetItemNameValue(item.netID);
        var t = $"[i/s{1}:{item.netID}]";
        mess.Append($"{t}({n}) 修改数值:");
        foreach (var kvp in itemValues)
        {
            string propName;
            switch (kvp.Key.ToLower())
            {
                case "d":
                case "da":
                case "伤害":
                    if (int.TryParse(kvp.Value, out int da)) item.damage = da;
                    propName = "伤害";
                    break;
                case "sk":
                case "数量":
                    if (int.TryParse(kvp.Value, out int sk)) item.stack = sk;
                    propName = "数量";
                    break;
                case "pr":
                case "前缀":
                    if (byte.TryParse(kvp.Value, out byte pr)) item.prefix = pr;
                    propName = "前缀";
                    break;
                case "c":
                case "sc":
                case "大小":
                    if (float.TryParse(kvp.Value, out float sc)) item.scale = sc;
                    propName = "大小";
                    break;
                case "k":
                case "kb":
                case "击退":
                    if (float.TryParse(kvp.Value, out float kb)) item.knockBack = kb;
                    propName = "击退";
                    break;
                case "t":
                case "ut":
                case "用速":
                    if (int.TryParse(kvp.Value, out int ut)) item.useTime = ut;
                    propName = "用速";
                    break;
                case "a":
                case "ua":
                case "攻速":
                    if (int.TryParse(kvp.Value, out int ua)) item.useAnimation = ua;
                    propName = "攻速";
                    break;
                case "h":
                case "sh":
                case "弹幕":
                    if (int.TryParse(kvp.Value, out int sh)) item.shoot = sh;
                    propName = "弹幕";
                    break;
                case "s":
                case "ss":
                case "射速":
                case "弹速":
                    if (float.TryParse(kvp.Value, out float ss)) item.shootSpeed = ss;
                    propName = "射速";
                    break;
                case "m":
                case "am":
                case "弹药":
                case "作弹药":
                case "作为弹药":
                    if (int.TryParse(kvp.Value, out int am)) item.ammo = am;
                    propName = "弹药";
                    break;
                case "aa":
                case "uaa":
                case "用弹药":
                case "发射器":
                case "使用弹药":
                    if (int.TryParse(kvp.Value, out int uaa)) item.useAmmo = uaa;
                    propName = "发射器";
                    break;
                case "hc":
                case "颜色":
                    var colorValue = kvp.Value.Replace("#", "");
                    if (colorValue.Length == 6)
                    {
                        item.color = new Color
                        (
                            int.Parse(colorValue.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                            int.Parse(colorValue.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                            int.Parse(colorValue.Substring(4, 2), System.Globalization.NumberStyles.HexNumber)
                        );
                        propName = "颜色";
                    }
                    else
                    {
                        throw new ArgumentException("无效的颜色代码格式");
                    }
                    break;
                default:
                    propName = kvp.Key;
                    break;
            }

            mess.AppendFormat("[c/94D3E4:{0}]([c/94D6E4:{1}]):[c/FF6975:{2}] ", propName, kvp.Key, kvp.Value);
        }

        var plr = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Active && p.Name == name);
        if (plr != null)
        {
            plr.SendMessage(mess.ToString(), 255, 244, 150);
        }
    }
    #endregion

    #region 解析输入参数的属性名2 用于/mw up指令修改玩家已存在的修改物品中指定物品属性
    public static bool UpdatePT2(string name, int id, Dictionary<string, string> itemValues)
    {
        var mess = new StringBuilder();
        var n = Lang.GetItemNameValue(id);
        var t = $"[i/s{1}:{id}]";
        mess.Append($"{t}({n}) 修改数值:");

        var data2 = DB.GetData2(name, id);

        if (data2 == null)
        {
            return false; // 没有找到玩家数据
        }

        if (Plugin.IsModifiedWeapon(name, id))
        {
            foreach (var kvp in itemValues)
            {
                string propName;
                switch (kvp.Key.ToLower())
                {
                    case "d":
                    case "da":
                    case "伤害":
                        if (int.TryParse(kvp.Value, out int da)) data2.damage = da;
                        propName = "伤害";
                        break;
                    case "sk":
                    case "数量":
                        if (int.TryParse(kvp.Value, out int sk)) data2.stack = sk;
                        propName = "数量";
                        break;
                    case "pr":
                    case "前缀":
                        if (byte.TryParse(kvp.Value, out byte pr)) data2.prefix = pr;
                        propName = "前缀";
                        break;
                    case "c":
                    case "sc":
                    case "大小":
                        if (float.TryParse(kvp.Value, out float sc)) data2.scale = sc;
                        propName = "大小";
                        break;
                    case "k":
                    case "kb":
                    case "击退":
                        if (float.TryParse(kvp.Value, out float kb)) data2.knockBack = kb;
                        propName = "击退";
                        break;
                    case "t":
                    case "ut":
                    case "用速":
                        if (int.TryParse(kvp.Value, out int ut)) data2.useTime = ut;
                        propName = "用速";
                        break;
                    case "a":
                    case "ua":
                    case "攻速":
                        if (int.TryParse(kvp.Value, out int ua)) data2.useAnimation = ua;
                        propName = "攻速";
                        break;
                    case "h":
                    case "sh":
                    case "弹幕":
                        if (int.TryParse(kvp.Value, out int sh)) data2.shoot = sh;
                        propName = "弹幕";
                        break;
                    case "s":
                    case "ss":
                    case "射速":
                    case "弹速":
                        if (float.TryParse(kvp.Value, out float ss)) data2.shootSpeed = ss;
                        propName = "射速";
                        break;
                    case "m":
                    case "am":
                    case "弹药":
                    case "作弹药":
                    case "作为弹药":
                        if (int.TryParse(kvp.Value, out int am)) data2.ammo = am;
                        propName = "弹药";
                        break;
                    case "aa":
                    case "uaa":
                    case "用弹药":
                    case "发射器":
                    case "使用弹药":
                        if (int.TryParse(kvp.Value, out int uaa)) data2.useAmmo = uaa;
                        propName = "发射器";
                        break;
                    case "hc":
                    case "颜色":
                        var colorValue = kvp.Value.Replace("#", "");
                        if (colorValue.Length == 6)
                        {
                            data2.color = new Color
                            (
                                int.Parse(colorValue.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                                int.Parse(colorValue.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                                int.Parse(colorValue.Substring(4, 2), System.Globalization.NumberStyles.HexNumber)
                            );
                            propName = "颜色";
                        }
                        else
                        {
                            throw new ArgumentException("无效的颜色代码格式");
                        }
                        break;
                    default:
                        propName = kvp.Key;
                        return false;
                }
                mess.AppendFormat("[c/94D3E4:{0}]([c/94D6E4:{1}]):[c/FF6975:{2}] ", propName, kvp.Key, kvp.Value);
            }

            //判断玩家是否在线
            var plr = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Active && p.Name == name);
            if (plr != null)
            {
                if (Config.Alone)
                {
                    reads[plr.Name] = true;
                    Plugin.Timer[plr.Name] = DateTime.UtcNow;
                }

                plr.SendMessage(mess.ToString(), 255, 244, 150);
                SaveItem(name, data2.type, data2.stack, data2.prefix, data2.damage, data2.scale, data2.knockBack, data2.useTime, data2.useAnimation, data2.shoot, data2.shootSpeed, data2.ammo, data2.useAmmo, data2.color);
                ReloadItem(plr); //在线 给重读
            }
            else //不在线 保存数据
            {
                SaveItem(name, data2.type, data2.stack, data2.prefix, data2.damage, data2.scale, data2.knockBack, data2.useTime, data2.useAnimation, data2.shoot, data2.shootSpeed, data2.ammo, data2.useAmmo, data2.color);
            }
            return true;
        }
        return false;
    }
    #endregion

    #region 保存修改后的物品数据
    internal static void SaveItem(string name, int type, int stack, byte prefix, int damage, float scale, float knockBack, int useTime, int useAnimation, int shoot, float shootSpeed, int ammo, int useAmmo, Color color)
    {
        // 1. 先从数据库查找该玩家是否有这个类型的物品
        var Items = DB.GetAll2().Where(d => d.PlayerName == name && d.type == type).ToList();

        if (Items.Count > 0)
        {
            // 如果存在，则更新第一个匹配项（也可以根据ID等唯一标识更精确）
            var item = Items[0];
            item.stack = stack;
            item.prefix = prefix;
            item.damage = damage;
            item.scale = scale;
            item.knockBack = knockBack;
            item.useTime = useTime;
            item.useAnimation = useAnimation;
            item.shoot = shoot;
            item.shootSpeed = shootSpeed;
            item.ammo = ammo;
            item.useAmmo = useAmmo;
            item.color = color;

            DB.UpdateData2(item); // 更新数据库
        }
        else
        {
            // 如果不存在，则新建并插入数据库
            var newItem = new Database.MyItemData(
                pname: name,
                type: type,
                stack: stack,
                prefix: prefix,
                damage: damage,
                scale: scale,
                knockBack: knockBack,
                useTime: useTime,
                useAnimation: useAnimation,
                shoot: shoot,
                shootSpeed: shootSpeed,
                ammo: ammo,
                useAmmo: useAmmo,
                color: color
            );

            DB.AddData2(newItem); // 插入数据库
        }
    }
    #endregion

}
