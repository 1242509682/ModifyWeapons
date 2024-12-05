using System;
using System.Data;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using static ModifyWeapons.Database.PlayerData;
using static ModifyWeapons.Plugin;
using static MonoMod.InlineRT.MonoModRule;

namespace ModifyWeapons;

public class Commands
{
    public static void CMD(CommandArgs args)
    {
        var plr = args.Player;
        var data = DB.GetData(plr.Name);
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
                        UpdataRead(plr, data);
                        if (!plr.HasPermission("mw.admin") && !plr.HasPermission("mw.cd") && Config.Auto != 1)
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
                        var data2 = DB.GetData(other);
                        if (data2 != null)
                        {
                            data2.Join = !data2.Join;

                            DB.UpdateData(data2);
                            plr.SendSuccessMessage(data.Join ?
                                $"{data2.Name}的进服重读:[c/92C5EC:启用]" :
                                $"{data2.Name}的进服重读:[c/E8585B:关闭]");
                        }
                    }
                    return;

                case "del":
                    if (plr.HasPermission("mw.admin"))
                    {
                        var other = args.Parameters[1];
                        plr.SendSuccessMessage($"已[c/E8585B:删除] {other} 的数据！");
                        DB.DeleteData(other);
                    }
                    return;

                case "add":
                    if (plr.HasPermission("mw.admin"))
                    {
                        var other = args.Parameters[1]; // 获取玩家名称
                        if (int.TryParse(args.Parameters[2], out var num2))
                        {
                            var NewCount = DB.UpReadCount(other, num2);

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
                                    var data2 = DB.GetData(acc.Name);
                                    var plr2 = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Active && p.Name == acc.Name);
                                    if (plr2 != null && data2 != null) // 如果目标玩家在线，则发送消息并直接重读数值
                                    {
                                        data2.ReadCount += 2;
                                        plr2.SendMessage($"\n管理员 [c/D4E443:{plr.Name}] 已为所有玩家[c/92C5EC:手动重读]修改物品", 0, 196, 177);
                                        UpdataRead(plr2, data2);
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

                case "at":
                case "auto":
                    if (plr.HasPermission("mw.admin"))
                    {
                        if (args.Parameters.Count > 1 && int.TryParse(args.Parameters[1], out var num))
                        {
                            Config.Auto = num;
                            Config.Write();
                            if (Config.Auto == 1)
                            {
                                plr.SendSuccessMessage($"已[c/92C5EC:开启]自动重读修改功能！");
                            }
                            else
                            {
                                plr.SendSuccessMessage($"已[c/E8585B:关闭]自动重读修改功能！");
                            }
                        }
                        else
                        {
                            plr.SendMessage($"[c/ED3241:注：]开自动重读会关闭[c/F28F2B:玩家重读次数]机制\n" +
                                $"[c/FFFAA1:手持修改物品时]触发重读条件:\n" +
                                $"0.冷却秒数 [c/FFB357:{Config.AutoTimer}秒]\n" +
                                $"1.正在使用 [c/4C95DD:修改的物品]\n" +
                                $"2.词缀数据 [c/4C95DD:不等于] [c/86F06E:手上词缀]\n" +
                                $"3.作为弹药 [c/FFB357:物品] [c/86F06E:直接使用]\n" +
                                $"4.指令格式 [c/FFF540:/mw auto 1 或 0]", 100, 210, 190);
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
                    var page = 1; // 默认第一页
                    if (args.Parameters.Count > 1 && int.TryParse(args.Parameters[1], out page))
                    {
                        ListItem(plr, page);
                    }
                    else
                    {
                        ListItem(plr, page = 1);
                    }
                    return;

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
                            SetItem(plr, data, Sel.damage, Sel.stack, Sel.prefix, Sel.scale, Sel.knockBack, Sel.useTime, Sel.useAnimation, Sel.shoot, Sel.shootSpeed, Sel.ammo, Sel.useAmmo, Sel.color);
                        }
                        else
                        {
                            SetError(plr);
                        }
                    }
                    break;

                case "up":
                case "update":
                    if (plr.HasPermission("mw.admin"))
                    {
                        if (args.Parameters.Count >= 3)
                        {
                            var other = args.Parameters[1]; // 玩家名称
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

                            if (UpdateItem(acc.Name, item.type, ItemVal)) // 更新玩家的指定物品属性
                            {
                                // 播报给执行者
                                var datas = DB.GetData(acc.Name);
                                if (datas.Dict.TryGetValue(acc.Name, out var dataList))
                                {
                                    foreach (var data2 in dataList.Where(d => d.type == item.type))
                                    {
                                        var diffs = CompareItem(data2, item.type, item.stack, item.prefix, item.damage, item.scale, item.knockBack,
                                            item.useTime, item.useAnimation, item.shoot, item.shootSpeed, item.ammo, item.useAmmo, item.color);

                                        if (diffs.Count > 0) // 只有在有差异的情况下才构建和发送消息
                                        {
                                            var mess = new StringBuilder();
                                            mess.Append($"已更新玩家 [c/2F99D7:{acc.Name}] 的[c/92B9D4:{Lang.GetItemName(data2.type)}]");

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
                            UpdateError(plr);
                        }
                    }
                    return;

                case "g":
                case "give":
                    if (plr.HasPermission("mw.admin"))
                    {
                        if (args.Parameters.Count >= 4)
                        {
                            var other = args.Parameters[1];
                            var items = args.Parameters[2];
                            var Items = TShock.Utils.GetItemByIdOrName(items);

                            if (Items.Count > 1)
                            {
                                args.Player.SendMultipleMatchError(Items.Select(i => i.Name));
                                return;
                            }
                            var item = Items[0];
                            item.SetDefaults(item.type);
                            Dictionary<string, string> ItemVal = new Dictionary<string, string>();
                            Parse(args.Parameters, out ItemVal, 3);

                            var datas = DB.GetData(other);
                            if (datas == null)
                            {
                                var newData = new Database.PlayerData
                                {
                                    Name = other,
                                    Hand = true,
                                    Join = true,
                                    ReadCount = Config.ReadCount,
                                    Process = 0,
                                    ReadTime = DateTime.UtcNow,
                                    Dict = new Dictionary<string, List<Database.PlayerData.ItemData>>()
                                };
                                DB.AddData(newData);
                                plr.SendMessage($"管理员 [c/D4E443:{plr.Name}] 已为 [c/92C5EC:{other}] 创建数据", 0, 196, 177);
                            }
                            else
                            {
                                datas.ReadCount += 2;
                                var plr2 = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Active && p.Name == other);
                                if (plr2 != null) //在线重读并通告玩家 物品修改信息
                                {
                                    plr2.SendMessage($"\n管理员 [c/D4E443:{plr.Name}] 已发送修改物品: [c/92C5EC:{Lang.GetItemName(item.type)}]", 0, 196, 177);

                                    if (datas.Dict.TryGetValue(other, out var DataList))
                                    {
                                        foreach (var item2 in DataList)
                                        {
                                            var hasItem = plr2.TPlayer.inventory.Take(58).Any(x => x != null && x.type == item2.type);

                                            if (!hasItem)
                                            {
                                                plr2.GiveItem(item2.type, item.maxStack);
                                            }
                                        }
                                    }

                                    UpdatePT(plr2.Name, item, ItemVal);
                                    SaveItem(other, datas, item.type, item.stack, item.prefix, item.damage, item.scale, item.knockBack, item.useTime, item.useAnimation, item.shoot, item.shootSpeed, item.ammo, item.useAmmo, item.color);
                                    UpdataRead(plr2, datas);
                                }
                                else //不在线 保存
                                {
                                    UpdatePT(other, item, ItemVal);
                                    SaveItem(other, datas, item.type, item.stack, item.prefix, item.damage, item.scale, item.knockBack, item.useTime, item.useAnimation, item.shoot, item.shootSpeed, item.ammo, item.useAmmo, item.color);
                                }

                                // 播报给执行者
                                if (datas.Dict.TryGetValue(other, out var dataList))
                                {
                                    foreach (var data2 in dataList.Where(d => d.type == item.type))
                                    {
                                        var item2 = new Item();
                                        var diffs = CompareItem(data2, item2.type, item2.stack, item2.prefix, item2.damage, item2.scale, item2.knockBack,
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
                                if (datas == null)
                                {
                                    var newData = new Database.PlayerData
                                    {
                                        Name = acc.Name,
                                        Hand = true,
                                        Join = true,
                                        Process = 0,
                                        ReadCount = Config.ReadCount,
                                        ReadTime = DateTime.UtcNow,
                                        Dict = new Dictionary<string, List<Database.PlayerData.ItemData>>()
                                    };
                                    DB.AddData(newData);

                                    if (!Creat) // 检查是否已经发送过消息
                                    {
                                        plr.SendMessage($"管理员 [c/D4E443:{plr.Name}] 已为所有玩家 创建数据", 0, 196, 177);
                                        Creat = true;
                                    }
                                }
                                else
                                {
                                    flag = true;
                                    name.Add(acc.Name);
                                    var plr2 = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Active && p.Name == acc.Name);
                                    if (plr2 != null)// 如果目标玩家在线，则发送消息并直接重读数值
                                    {
                                        datas.ReadCount += 2;
                                        plr2.SendMessage($"\n管理员 [c/D4E443:{plr.Name}] 已发送修改物品: [c/92C5EC:{Lang.GetItemName(item.type)}]", 0, 196, 177);

                                        if (datas.Dict.TryGetValue(plr2.Name, out var DataList))
                                        {
                                            foreach (var item2 in DataList)
                                            {
                                                var hasItem = plr2.TPlayer.inventory.Take(50).Any(x => x != null && x.type == item2.type);

                                                if (!hasItem)
                                                {
                                                    plr2.GiveItem(item2.type, item.maxStack);
                                                }
                                            }
                                        }

                                        UpdatePT(plr2.Name, item, ItemVal);
                                        SaveItem(plr2.Name, datas, item.type, item.stack, item.prefix, item.damage, item.scale, item.knockBack, item.useTime, item.useAnimation, item.shoot, item.shootSpeed, item.ammo, item.useAmmo, item.color);
                                        UpdataRead(plr2, datas);
                                    }
                                    else
                                    {
                                        UpdatePT(acc.Name, item, ItemVal);
                                        SaveItem(acc.Name, datas, item.type, item.stack, item.prefix, item.damage, item.scale, item.knockBack, item.useTime, item.useAnimation, item.shoot, item.shootSpeed, item.ammo, item.useAmmo, item.color);
                                    }
                                }
                            }

                            if (name.Count > 0)
                            {
                                mess.Append($"\n《已修改物品名单》:\n");

                                if (flag) // 播报给执行者
                                {
                                    var index = 1;
                                    bool flag2 = false;
                                    var itemDiffs = new List<string>(); // 用于收集所有差异信息
                                    const int plrLine = 4; // 每行玩家数
                                    const int maxMessage = 20; // 每条消息最多玩家数

                                    foreach (var other in name)
                                    {
                                        var datas = DB.GetData(other);
                                        if (datas.Dict.TryGetValue(other, out var dataList))
                                        {
                                            foreach (var data2 in dataList.Where(d => d.type == item.type))
                                            {
                                                var item2 = new Item();
                                                var diffs = CompareItem(data2, item2.type, item2.stack, item2.prefix, item2.damage, item2.scale, item2.knockBack,
                                                    item2.useTime, item2.useAnimation, item2.shoot, item2.shootSpeed, item2.ammo, item2.useAmmo, item2.color);

                                                if (diffs.Count > 0 && !flag2)
                                                {
                                                    flag2 = true;

                                                    // 收集所有差异信息，假设这些差异对于所有玩家是相同的
                                                    foreach (var diff in diffs)
                                                    {
                                                        itemDiffs.Add($"{diff.Key}[c/E83C10:{diff.Value}]");
                                                    }
                                                }

                                                if (diffs.Count > 0)
                                                {
                                                    // 构建玩家列表，并每4个玩家换行
                                                    mess.Append($"[{index}][c/2F99D7:{other}]");
                                                    if (index % plrLine == 0 || index % maxMessage == 0)
                                                    {
                                                        mess.AppendLine(); // 换行或结束当前消息块
                                                    }
                                                    else
                                                    {
                                                        mess.Append(" "); // 同一行内的玩家之间加空格
                                                    }

                                                    // 如果达到最大玩家数，则发送当前消息并重置StringBuilder
                                                    if (index % maxMessage == 0)
                                                    {
                                                        plr.SendMessage(mess.ToString(), 216, 223, 153); // 发送当前消息块
                                                        mess.Clear();
                                                        mess.Append($"《下一批修改物品名单》:\n"); // 继续名单
                                                    }
                                                    index++;
                                                }
                                            }
                                        }
                                    }

                                    if (flag2)
                                    {
                                        // 只在最后添加一次物品变化信息
                                        var itemInfo = string.Join(" ", itemDiffs);
                                        if (mess.Length > 0 && ((index - 1) % plrLine != 0)) // 如果最后一行不满4人，则添加换行
                                        {
                                            mess.AppendLine();
                                        }
                                        mess.AppendLine($"{itemInfo}");

                                        // 发送剩余的消息（如果有）
                                        if (mess.Length > 0)
                                        {
                                            mess.AppendFormat($" - 共计[C/91DFBB:{name.Count}个]玩家 -");
                                            plr.SendMessage($"{mess}\n", 216, 223, 153); // 发送最终消息
                                        }
                                    }
                                }
                            }
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

    #region 菜单方法
    private static void HelpCmd(TSPlayer plr)
    {
        var data = DB.GetData(plr.Name);
        var sel = plr.SelectedItem;
        if (data == null)
        {
            plr.SendInfoMessage("请用角色[c/D95065:重进服务器]后输入：/mw 指令查看菜单\n" +
            "羽学声明：本插件纯属[c/7E93DE:免费]请勿上当受骗", 217, 217, 217);
            return;
        }

        var itemData = data.Dict.FirstOrDefault(p => p.Key == plr.Name);

        if (!plr.HasPermission("mw.admin")) //没有管理权限
        {
            plr.SendMessage("\n          [i:3455][c/AD89D5:修][c/D68ACA:改][c/DF909A:武][c/E5A894:器][i:3454]\n" +
            "/mw hand —— 获取[c/FFB357:手持物品]信息开关\n" +
            "/mw join —— 切换[c/4C95DD:进服重读]开关\n" +
            "/mw list —— [c/FF6863:列出所有]修改物品\n" +
            "/mw read —— [c/FFF540:手动重读]修改物品", 224, 210, 168);

            if (data.Hand)
            {
                HandText(plr, sel, data, itemData);
            }
        }
        else
        {
            plr.SendMessage("\n          [i:3455][c/AD89D5:修][c/D68ACA:改][c/DF909A:武][c/E5A894:器][i:3454]\n" +
            "/mw hand —— 获取[c/FFB357:手持物品]信息开关\n" +
            "/mw join —— 切换[c/4C95DD:进服重读]开关\n" +
            "/mw list —— [c/FF6863:列出所有]修改物品\n" +
            "/mw read —— [c/FFF540:手动重读]修改物品\n" +
            "/mw auto —— [c/F28F2B:自动重读]修改物品\n" +
            "/mw open 玩家名 —— 切换[c/7ECCED:别人进服]重读\n" +
            "/mw add 玩家名 次数 ——添加[c/D68ACA:重读次数]\n" +
            "/mw del 玩家名 —— [c/DF909A:删除指定]玩家数据\n" +
            "/mw up —— 修改玩家[c/AD89D5:已有物品]的指定属性\n" +
            "/mw set  —— 修改[c/FFB357:自己手持]物品属性\n" +
            "/mw give —— [c/4C95DD:给指定玩家]修改物品并[c/86F06E:建数据]\n" +
            "/mw all —— [c/FFF540:给所有玩家]修改物品并[c/FF6863:建数据]\n" +
            "/mw reads —— [c/D68ACA:帮助所有人]重读\n" +
            "/mw reset —— [c/ED3241:重置所有]玩家数据", 224, 210, 168);

            if (data.Hand)
            {
                HandText(plr, sel, data, itemData);
            }
        }
    }
    #endregion

    #region 设置自己手上物品方法
    private static void SetItem(TSPlayer plr, Database.PlayerData? data, int damage, int stack, byte prefix, float scale, float knockBack, int useTime, int useAnimation,
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

            SaveItem(plr.Name, data, newItem.type, newItem.stack, newItem.prefix, damage, scale, knockBack, useTime, useAnimation, shoot, shootSpeed, ammo, useAmmo, color);
        }
        // 保存修改后的物品数据
        data.Join = true;
        DB.UpdateData(data);
    }
    #endregion

    #region 新物品属性
    private static void MyNewItem(TSPlayer plr, Item item, Item newItem, int damage, int stack, byte prefix, float scale, float knockBack, int useTime, int useAnimation,
        int shoot, float shootSpeed, int ammo, int useAmmo)
    {
        newItem.playerIndexTheItemIsReservedFor = plr.Index;

        if (plr.SelectedItem.type == item.type)
        {
            newItem.prefix = plr.SelectedItem.prefix;
        }
        else
        {
            newItem.prefix = prefix;
        }

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

    #region 重读物品数据方法
    internal static void ReloadItem(TSPlayer plr, Database.PlayerData datas)
    {
        if (datas.Dict.TryGetValue(plr.Name, out var DataList))
        {
            // 记录成功重读的物品类型
            List<int> ReItem = new List<int>();

            foreach (var data in DataList)
            {
                var item = TShock.Utils.GetItemById(data.type);
                var MyIndex = new TSPlayer(plr.Index);
                for (int i = 0; i < plr.TPlayer.inventory.Length; i++)
                {
                    var inv = plr.TPlayer.inventory[i];
                    var find = false;
                    var slot = -1;

                    //背包有负重石 直接清掉 给个非负重石
                    if (inv.type == 4346)
                    {
                        inv.TurnToAir();
                        plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, i);
                        plr.GiveItem(5391, 1);
                        continue;
                    }

                    if (inv.type == item.type && !find)
                    {
                        find = true;
                        slot = i;
                    }

                    if (find)
                    {
                        var MyItem = Item.NewItem(null, (int)MyIndex.X, (int)MyIndex.Y, item.width, item.height, item.type, item.stack);
                        var newItem = Main.item[MyItem];
                        MyNewItem(plr, item, newItem, data.damage, data.stack, data.prefix, data.scale, data.knockBack, data.useTime, data.useAnimation, data.shoot, data.shootSpeed, data.ammo, data.useAmmo);

                        inv.SetDefaults(0);
                        NetMessage.SendData(5, -1, -1, null, plr.Index, slot);
                        plr.SendData(PacketTypes.PlayerSlot, null, MyItem);
                        plr.SendData(PacketTypes.UpdateItemDrop, null, MyItem);
                        plr.SendData(PacketTypes.ItemOwner, null, MyItem);
                        plr.SendData(PacketTypes.TweakItem, null, MyItem, 255f, 63f);
                        ReItem.Add(item.type); // 记录成功重载的物品类型
                    }
                }
            }

            if (ReItem.Count < 1)
            {
                plr.SendInfoMessage("您身上没有任何修改物品");
            }
            else
            {
                ShowReadItem(plr, ReItem);
            }
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

        if (Config.Auto != 1) //当自动更新关闭时
        {
            var last = 0f;
            var now = DateTime.UtcNow;
            if (data.ReadTime != default)
            {
                // 上次重读时间，保留2位小数
                last = (float)Math.Round((now - data.ReadTime).TotalSeconds, 2);
            }

            if (!plr.HasPermission("mw.cd") && !plr.HasPermission("mw.admin")) // 没有权限
            {
                if (data.ReadCount >= 1) // 有重读次数 直接重读
                {
                    ReloadItem(plr, data);
                    data.ReadCount = Math.Max(0, data.ReadCount - 1); // 最少1次 最多减到0
                }
                else // 没有重读次数
                {
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
            else // 有1个权限直接重读
            {
                ReloadItem(plr, data);
            }
        }
        else
        {
            ReloadItem(plr, data);
        }
    }
    #endregion

    #region 解析输入参数的距离 如:da 1
    private static void Parse(List<string> parameters, out Dictionary<string, string> itemValues, int Index)
    {
        itemValues = new Dictionary<string, string>();
        for (int i = Index; i < parameters.Count; i += 2)
        {
            if (i + 1 < parameters.Count) // 确保有下一个参数
            {
                string propertyName = parameters[i].ToLower();
                string value = parameters[i + 1];
                itemValues[propertyName] = value;
            }
        }
    }
    #endregion

    #region 解析输入参数的属性名 通用方法 如:da = 伤害
    private static void UpdatePT(string name, Item item, Dictionary<string, string> itemValues)
    {
        var mess = new StringBuilder();
        mess.AppendFormat("数值不对手动重读:[c/94D3E4:/mw read]\n");
        mess.Append($"修改数值:");
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
    public static bool UpdateItem(string name, int id, Dictionary<string, string> itemValues)
    {
        var mess = new StringBuilder();
        mess.AppendFormat("数值不对手动重读:[c/94D3E4:/mw read]\n");
        mess.Append($"修改数值:");

        var datas = DB.GetData(name);
        if (datas.Dict.ContainsKey(name))
        {
            var data = datas.Dict[name];
            var item = data.FirstOrDefault(i => i.type == id);

            if (item != null)
            {
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
                            return false;
                    }
                    mess.AppendFormat("[c/94D3E4:{0}]([c/94D6E4:{1}]):[c/FF6975:{2}] ", propName, kvp.Key, kvp.Value);
                }

                datas.ReadCount += 2;

                //判断玩家是否在线
                var plr = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Active && p.Name == name);
                if (plr != null)
                {
                    plr.SendMessage($"[c/D4E443:管理员] 已修改你的: [c/92C5EC:{Lang.GetItemName(item.type)}]", 0, 196, 177);
                    plr.SendMessage(mess.ToString(), 255, 244, 150);
                    SaveItem(name, datas, item.type, item.stack, item.prefix, item.damage, item.scale, item.knockBack, item.useTime, item.useAnimation, item.shoot, item.shootSpeed, item.ammo, item.useAmmo, item.color);
                    UpdataRead(plr, datas); //在线 给重读
                }
                else //不在线 保存数据
                {
                    SaveItem(name, datas, item.type, item.stack, item.prefix, item.damage, item.scale, item.knockBack, item.useTime, item.useAnimation, item.shoot, item.shootSpeed, item.ammo, item.useAmmo, item.color);
                }
                return true;
            }
        }
        return false;
    }
    #endregion

    #region 保存修改后的物品数据
    private static void SaveItem(string name, Database.PlayerData data, int type, int stack, byte prefix, int damage, float scale, float knockBack, int useTime,
        int useAnimation, int shoot, float shootSpeed, int ammo, int useAmmo, Color color)
    {
        if (!data.Dict.ContainsKey(name))
        {
            data.Dict[name] = new List<Database.PlayerData.ItemData>();
        }

        // 检查是否已经存在相同的物品，如果存在则更新
        var item = data.Dict[name].FirstOrDefault(item => item.type == type);
        if (item == null)
        {
            data.Dict[name].Add(new Database.PlayerData.ItemData(type, stack, prefix, damage, scale, knockBack, useTime, useAnimation, shoot, shootSpeed, ammo, useAmmo, color));
        }
        else
        {
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
        }
        DB.UpdateData(data);
    }
    #endregion

    #region 重读时显示修改物品数量
    private static void ShowReadItem(TSPlayer plr, List<int> ReItem)
    {
        // 构建消息
        var mess = new StringBuilder();
        var WNames = new HashSet<string>();

        // 添加重载的物品到集合中
        if (ReItem != null && ReItem.Any())
        {
            foreach (var itemType in ReItem)
            {
                WNames.Add(Lang.GetItemName(itemType).ToString());
            }
        }

        if (WNames.Count > 0)
        {
            // 为每个物品名称添加较亮的随机颜色
            var color = WNames.Select(name =>
            {
                // 生成较亮的随机颜色
                var random = new Random();
                int r = random.Next(128, 256); // 生成128到255之间的随机数
                int g = random.Next(128, 256);
                int b = random.Next(128, 256);

                // 将RGB转换为16进制字符串 返回带有颜色标签的物品名称
                string hexColor = $"{r:X2}{g:X2}{b:X2}";
                return $"[c/{hexColor}:{name}]";
            });

            // 使用逗号分隔物品名称
            mess.Append($"已重读身上[C/91DFBB:{WNames.Count}]个修改物品:{string.Join(", ", color)}");
        }

        // 发送消息给玩家
        plr.SendMessage(mess.ToString(), 255, 244, 150);
    }
    #endregion

    #region 列出自己所有修改物品与其属性
    private static void ListItem(TSPlayer plr, int page)
    {
        var data = DB.GetData(plr.Name);
        if (data.Dict.TryGetValue(plr.Name, out var list) && list.Count > 0)
        {
            var Size = 1; // 每页显示一个物品
            var Total = list.Count; // 总页数等于物品总数

            if (page < 1 || page > Total)
            {
                plr.SendErrorMessage("无效的页码,总共有 {0} 页。", Total);
                return;
            }

            // 计算当前页的起始索引
            var index = (page - 1) * Size;

            // 获取当前页的物品
            var data2 = list[index];

            var itemName = Lang.GetItemNameValue(data2.type);
            var itemPrefix = TShock.Utils.GetPrefixById(data2.prefix);

            if (string.IsNullOrEmpty(itemPrefix))
            {
                itemPrefix = "无";
            }

            string ColorToHex(Color color)
            {
                return $"{color.R:X2}{color.G:X2}{color.B:X2}";
            }

            // 定义属性列表及其对应的参数名称
            var properties = new[]
            {
                (name: "数量", value: $"{data2.stack}", param: "st"),
                (name: "伤害", value: $"{data2.damage}", param: "da"),
                (name: "大小", value: $"{data2.scale}", param: "sc"),
                (name: "击退", value: $"{data2.knockBack}", param: "kb"),
                (name: "用速", value: $"{data2.useTime}", param: "ut"),
                (name: "攻速", value: $"{data2.useAnimation}", param: "ua"),
                (name: "弹幕", value: $"{data2.shoot}", param: "sh"),
                (name: "射速", value: $"{data2.shootSpeed}", param: "ss"),
                (name: "弹药", value: $"{data2.ammo}", param: "am"),
                (name: "发射器", value: $"{data2.useAmmo}", param: "uaa"),
                (name: "颜色", value: $"{ColorToHex(data2.color)}", param: "hc"),
            };

            // 单独处理 "物品名" 与 "前缀名" 属性
            var Name = $"物品([c/94D3E4:{data2.type}]):[c/FFF4{new Random().Next(0, 256).ToString("X2")}:{itemName}] | " +
                $"前缀([c/94D3E4:{data2.prefix}]):[c/FFF4{new Random().Next(0, 256).ToString("X2")}:{itemPrefix}]";

            // 拼接其他属性
            Name += "\n" + string.Join("\n", Enumerable.Range(0, (properties.Length + 2) / 3)
                .Select(i => string.Join("  ", properties.Skip(i * 3).Take(3)
                    .Select(prop => $"{prop.name}([c/94D3E4:{prop.param}]):[c/FFF4{new Random().Next(0, 256).ToString("X2")}:{prop.value}]"))));

            // 翻页提示信息
            var all = Name;
            if (page < Total)
            {
                var nextPage = page + 1;
                var prompt = $"请输入 [c/68A7E8:/mw list {nextPage}] 查看更多";
                all += $"\n{prompt}";
            }
            else if (page > 1)
            {
                var prevPage = page - 1;
                var prompt = $"请输入 [c/68A7E8:/mw list {prevPage}] 查看上一页";
                all += $"\n{prompt}";
            }

            // 发送消息
            plr.SendMessage($"[c/FE727D:《物品列表》]第 [c/68A7E8:{page}] 页，共 [c/EC6AC9:{Total}] 页:\n{all}", 255, 244, 150);
        }
        else
        {
            plr.SendInfoMessage("您没有任何修改物品。");
        }
    }
    #endregion

    #region 比较物品的修改数值 只列出修改过的属性
    private static Dictionary<string, object> CompareItem(Database.PlayerData.ItemData Data, int type, int stack, int prefix, int damage, float scale, float knockBack, int useTime, int useAnimation, int shoot, float shootSpeed, int ammo, int useAmmo, Color color)
    {
        string ColorToHex(Color color)
        {
            return $"{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        var pr = TShock.Utils.GetPrefixById(Data.prefix);
        if (string.IsNullOrEmpty(pr))
        {
            pr = "无";
        }

        var diff = new Dictionary<string, object>();
        if (Data.type != type) diff.Add($"{Lang.GetItemNameValue(Data.type)}", Data.type);
        if (Data.stack != stack) diff.Add("数量", Data.stack);
        if (Data.prefix != prefix) diff.Add("前缀", pr);
        if (Data.damage != damage) diff.Add("伤害", Data.damage);
        if (Data.scale != scale) diff.Add("大小", Data.scale);
        if (Data.knockBack != knockBack) diff.Add("击退", Data.knockBack);
        if (Data.useTime != useTime) diff.Add("用速", Data.useTime);
        if (Data.useAnimation != useAnimation) diff.Add("攻速", Data.useAnimation);
        if (Data.shoot != shoot) diff.Add("弹幕", Data.shoot);
        if (Data.shootSpeed != shootSpeed) diff.Add("射速", Data.shootSpeed);
        if (Data.ammo != ammo) diff.Add("弹药", Data.ammo);
        if (Data.useAmmo != useAmmo) diff.Add("发射器", Data.useAmmo);
        if (Data.color != color) diff.Add("颜色", ColorToHex(Data.color));

        return diff;
    }
    #endregion

    #region 参数不对的信息反馈
    private static void param(TSPlayer plr)
    {
        plr.SendInfoMessage($"\n弹药相关(远程只需前2步):\n" +
            $"1.先用[c/91DFBB:m 1]指定1个物品作为[c/C9360D:弹药]\n" +
            $"2.再用[c/FF6975:aa 1]把另1个物品设为[c/0DD65D:发射器]\n" +
            $"3.近战想用[c/FF5338:弹药]得加个[c/2F99D7:sh 1]弹幕属性");
        plr.SendMessage("免堆叠检测权限:[c/40A9DE:tshock.ignore.itemstack]", 227, 158, 97);
        plr.SendInfoMessage("颜色相关:[c/28DE5E:hc 2858de] (16进制无#号)");
        plr.SendMessage("词缀相关:玩家[c/D45B7E:手上不是修改物品]时才能改", Color.Lavender);
        plr.SendMessage("用速相关:影响所有[c/FFBE38:远程]越小单次间隔越密集", 233, 77, 53);
        plr.SendMessage("攻速相关:影响所有[c/EAF836:近战]越小挥舞间隔越快", 47, 153, 215);
        plr.SendMessage("除了[c/DADEA0:/mw up]其他都会还原数值再修改", Color.YellowGreen);
        plr.SendMessage("伤害[c/FF6975:d] 数量[c/74E55D:sk] 前缀[c/74E55D:pr] 大小[c/5C9EE1:sc] \n" +
            "击退[c/F79361:kb] 用速[c/74E55D:ut] 攻速[c/F7B661:ua] 射速[c/F7F261:ss] \n" +
            "弹幕[c/A3E295:sh] 弹药[c/91DFBB:m] 发射器[c/5264D9:aa] 颜色[c/5264D9:hc]", 141, 209, 214);
    }

    private static void SetError(TSPlayer plr)
    {
        param(plr);
        plr.SendSuccessMessage("修改自己手上物品参数,格式为:");
        plr.SendMessage("/mw s d 20 ua 10 … 自定义组合", Color.AntiqueWhite);
    }

    private static void GiveError(TSPlayer plr)
    {
        param(plr);
        plr.SendSuccessMessage("给别人指定物品并改参数,格式为:");
        plr.SendMessage("/mw g 玩家名 物品名 d 20 ua 10 …", Color.AntiqueWhite);
        plr.SendInfoMessage("发3次:[c/91DFBB:建数据>发物品>同步数值]");
    }

    private static void AllError(TSPlayer plr)
    {
        param(plr);
        plr.SendSuccessMessage("给所有人指定物品并改参数,格式为:");
        plr.SendMessage("/mw all 物品名 d 200 ua 10 …", Color.AntiqueWhite);
        plr.SendInfoMessage("\n发3次:[c/91DFBB:建数据>发物品>同步数值]");
    }

    private static void UpdateError(TSPlayer plr)
    {
        param(plr);
        plr.SendInfoMessage("\n保留原有修改参数进行二次更改,格式为:");
        plr.SendMessage("/mw up 玩家名 物品名 d 20 ua 10", Color.AntiqueWhite);
    }
    #endregion

    #region 获取手上物品信息方法
    private static void HandText(TSPlayer plr, Item Sel, Database.PlayerData? data, KeyValuePair<string, List<Database.PlayerData.ItemData>> ItemData)
    {
        if (!data.Join)
        {
            plr.SendSuccessMessage("重读开关:[c/E8585B:未开启]");
        }

        if (!Sel.IsAir)
        {
            var pr = TShock.Utils.GetPrefixById(Sel.prefix);
            if (string.IsNullOrEmpty(pr))
            {
                pr = "无";
            }

            string ColorToHex(Color color)
            {
                return $"{color.R:X2}{color.G:X2}{color.B:X2}";
            }

            plr.SendInfoMessage("手持[c/92C5EC:{0}] 伤害[c/FF6975:{1}] 前缀[c/F5DDD3:{2}] 数量[c/2CCFC6:{3}] 大小[c/5C9EE1:{4}] \n" +
                "击退[c/5C9EE1:{5}] 用速[c/74E55D:{6}] 攻速[c/94BAE0:{7}] 弹幕[c/E83C10:{8}] 射速[c/F0EC9E:{9}]\n" +
                "弹药[c/91DFBB:{10}] 发射器[c/5264D9:{11}] 颜色[c/F5DDD3:{12}]",
            Lang.GetItemName(Sel.type), Sel.damage, pr, Sel.stack,
            Sel.scale, Sel.knockBack, Sel.useTime, Sel.useAnimation, Sel.shoot, Sel.shootSpeed, Sel.ammo, Sel.useAmmo, ColorToHex(Sel.color));

            var mess = new StringBuilder();
            mess.Append($"修改:[c/92B9D4:{Lang.GetItemName(Sel.type)}]");
            if (ItemData.Value != null && ItemData.Value.Count > 0)
            {
                foreach (var Store in ItemData.Value)
                {
                    if (Store.type == Sel.type)
                    {
                        var diffs = CompareItem(Store, Sel.type, Sel.stack, Sel.prefix, Sel.damage, Sel.scale, Sel.knockBack,
                            Sel.useTime, Sel.useAnimation, Sel.shoot, Sel.shootSpeed, Sel.ammo, Sel.useAmmo, Sel.color);

                        if (diffs.Count > 0)
                        {
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
    }
    #endregion
}
