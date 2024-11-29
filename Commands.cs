using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using TShockAPI;
using static ModifyWeapons.Configuration;
using static ModifyWeapons.Plugin;

namespace ModifyWeapons;

public class Commands
{
    public static void CMD(CommandArgs args)
    {
        var plr = args.Player;
        var Sel = plr.SelectedItem;
        var data = Config.data.FirstOrDefault(config => config.Name == plr.Name);
        var ItemData = Config.Dict.FirstOrDefault(config => config.Key == plr.Name);

        if (!Config.Enabled || plr == null)
        {
            return;
        }

        if (args.Parameters.Count == 0)
        {
            HelpCmd(args.Player, Sel, data, ItemData);
            return;
        }

        if (args.Parameters.Count >= 1)
        {
            switch (args.Parameters[0].ToLower())
            {
                case "hand":
                    if (data != null)
                    {
                        data.Hand = !data.Hand;
                        plr.SendInfoMessage(data.Hand ?
                        $"玩家 [{plr.Name}] 已[c/92C5EC:启用]获取手持物品信息功能。" :
                        $"玩家 [{plr.Name}] 已[c/92C5EC:关闭]获取手持物品信息功能。");
                        Config.Write();
                    }
                    return;

                case "join":
                    if (data != null)
                    {
                        data.Join = !data.Join;
                        plr.SendInfoMessage(data.Join ?
                            $"玩家 [{plr.Name}] 已[c/92C5EC:启用]进服重读功能。" :
                            $"玩家 [{plr.Name}] 已[c/92C5EC:关闭]进服重读功能。");
                        Config.Write();
                    }
                    return;

                case "read":
                    if (data != null)
                    {
                        UpdataRead(plr, data);

                        if (ItemData.Value != null)
                        {
                            // 显示玩家的修改武器
                            ShowReadItem(plr, ItemData.Value);

                            plr.SendInfoMessage($"[i:4080]如数值不对,请输入指令恢复:[c/3CCB91:/mw read]");

                            if (!plr.HasPermission("mw.admin"))
                            {
                                plr.SendInfoMessage($"剩余修改物品重读次数为:[C/A7D3D6:{data.ReadCount = Math.Max(0, data.ReadCount - 1)}]");
                            }

                            plr.SendInfoMessage($"手持物品使用:/mw 查看对比信息");
                        }
                    }
                    return;

                case "open":
                    if (plr.HasPermission("cmd.admin"))
                    {
                        var other = args.Parameters[1];
                        var data2 = Config.data.FirstOrDefault(c => c.Name == other);
                        if (data2 != null)
                        {
                            data2.Join = !data2.Join;

                            Config.Write();
                            plr.SendSuccessMessage(data.Join ?
                                $"{data2.Name}的进服重读:[c/92C5EC:启用]" :
                                $"{data2.Name}的进服重读:[c/E8585B:关闭]");
                        }
                    }
                    return;

                case "del":
                    if (plr.HasPermission("cmd.admin"))
                    {
                        var other = args.Parameters[1];
                        plr.SendSuccessMessage($"已[c/E8585B:删除] {other} 的数据！");
                        Config.DelData(other);
                        Config.Write();
                    }
                    return;

                case "add":
                    if (plr.HasPermission("cmd.admin"))
                    {
                        // 获取玩家名称
                        var other = args.Parameters[1];

                        if (int.TryParse(args.Parameters[2], out var count))
                        {
                            // 调用 AddCount 方法增加重读次数
                            int NewCount = Config.AddCount(other, count);

                            if (NewCount != -1)
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

                case "up":
                case "update":
                    if (args.Parameters.Count >= 3 && plr.HasPermission("cmd.admin"))
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
                        var key = args.Parameters[3]; // 属性名称
                        var Value = args.Parameters[4]; // 新值
                        var acc = TShock.UserAccounts.GetUserAccountByName(other).Name;
                        var data2 = Config.data.FirstOrDefault(c => c.Name == acc);

                        // 创建一个包含要更新的属性的字典
                        var Properties = new Dictionary<string, string>
                        {
                            { key, Value }
                        };

                        // 更新物品属性
                        var success = Config.UpdateItem(other, item.type, Properties);
                        if (success)
                        {
                            data2.ReadCount++; //增加重读次数
                            var plr2 = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Active && p.Name == acc);
                            if (plr2 != null) //在线直接重读更新数值
                            {
                                UpdataRead(plr2, data2);
                                plr2.SendMessage($"[c/D4E443:{plr.Name}]已修改你的[c/92C5EC:{Lang.GetItemName(item.type)}]", 255, 244, 150);
                                plr2.SendMessage($"输入菜单指令可查看详细对比:[c/92D4B7:/mw]", 255, 244, 150);
                            }
                            plr.SendSuccessMessage($"已更新玩家[c/E8585B:{other}]的[c/74E55D:{Lang.GetItemName(item.type)}][c/5C9EE1:{key}]:[c/FF6975:{Value}]。");

                        }
                        else
                        {
                            plr.SendErrorMessage($"无法找到玩家 [c/E8585B:{other}] 或物品 [c/74E55D:{item.type}]，或属性 [c/5C9EE1:{key}] 无效。");
                        }
                    }
                    else
                    {
                        UpdateError(plr);
                    }
                    return;

                case "reads":
                    if (plr.HasPermission("cmd.admin"))
                    {
                        var Enabled = Config.data.Any(data => data.Join);
                        foreach (var All in Config.data)
                        {
                            All.Join = !Enabled;
                        }
                        Config.Write();
                        plr.SendSuccessMessage(!Enabled ?
                            $"已[c/92C5EC:开启]所有玩家进服重读功能！" :
                            $"已[c/E8585B:关闭]所有玩家进服重读功能！");
                    }
                    return;

                case "reset":
                    if (plr.HasPermission("cmd.admin"))
                    {
                        Config.data.Clear();
                        Config.Dict.Clear(); // 清空字典
                        Config.Write();
                        plr.SendSuccessMessage($"已[c/E8585B:清空]所有人的修改武器数据！");
                    }
                    return;

                case "list":
                    int page = 1; // 默认第一页
                    if (args.Parameters.Count > 1 && int.TryParse(args.Parameters[1], out page))
                    {
                        // 直接使用现有的 args 参数列表
                        ListItem(args, plr, Config.Dict, page);
                    }
                    else
                    {
                        // 如果没有提供页码或页码无效，默认显示第一页
                        ListItem(args, plr, Config.Dict, page);
                    }
                    return;

                case "set":
                case "s":
                    if (args.Parameters.Count >= 3 && !Sel.IsAir && plr.HasPermission("cmd.admin"))
                    {
                        Dictionary<string, string> ItemVal = new Dictionary<string, string>();

                        // 遍历参数列表，跳过第一个参数，因为它是命令名
                        for (int i = 1; i < args.Parameters.Count; i += 2)
                        {
                            if (i + 1 < args.Parameters.Count) // 确保有下一个参数
                            {
                                string PropertyName = args.Parameters[i].ToLower();
                                string value = args.Parameters[i + 1];
                                ItemVal[PropertyName] = value;
                            }
                        }

                        // 更新属性值
                        foreach (var kvp in ItemVal)
                        {
                            switch (kvp.Key.ToLower()) // 将键转换为小写以支持大小写不敏感
                            {
                                case "d":
                                case "da":
                                case "伤害":
                                    if (int.TryParse(kvp.Value, out int da)) Sel.damage = da;
                                    break;
                                case "c":
                                case "sc":
                                case "大小":
                                    if (float.TryParse(kvp.Value, out float sc)) Sel.scale = sc;
                                    break;
                                case "k":
                                case "kb":
                                case "击退":
                                    if (float.TryParse(kvp.Value, out float kb)) Sel.knockBack = kb;
                                    break;
                                case "t":
                                case "ut":
                                case "用速":
                                    if (int.TryParse(kvp.Value, out int ut)) Sel.useTime = ut;
                                    break;
                                case "a":
                                case "ua":
                                case "攻速":
                                    if (int.TryParse(kvp.Value, out int ua)) Sel.useAnimation = ua;
                                    break;
                                case "h":
                                case "sh":
                                case "弹幕":
                                    if (int.TryParse(kvp.Value, out int sh)) Sel.shoot = sh;
                                    break;
                                case "s":
                                case "ss":
                                case "弹速":
                                    if (float.TryParse(kvp.Value, out float ss)) Sel.shootSpeed = ss;
                                    break;
                                case "m":
                                case "am":
                                case "作弹药":
                                case "作为弹药":
                                    if (int.TryParse(kvp.Value, out int am)) Sel.ammo = am;
                                    break;
                                case "aa":
                                case "uaa":
                                case "用弹药":
                                case "使用弹药":
                                    if (int.TryParse(kvp.Value, out int uaa)) Sel.useAmmo = uaa;
                                    break;
                                default:
                                    SetError(plr);
                                    return;
                            }
                        }
                        //设置物品数值方法
                        SetItem(plr, data, Sel.damage, Sel.scale, Sel.knockBack, Sel.useTime, Sel.useAnimation, Sel.shoot, Sel.shootSpeed, Sel.ammo, Sel.useAmmo);
                        SetItemMess(plr, ItemData, ItemVal);
                    }
                    else
                    {
                        SetError(plr);
                    }
                    break;

                case "give":
                case "g":
                    if (args.Parameters.Count >= 4 && plr.HasPermission("cmd.admin"))
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
                        var damage = item.damage;
                        var stack = item.maxStack;
                        var scale = item.scale;
                        var knockBack = item.knockBack;
                        var useTime = item.useTime;
                        var useAnimation = item.useAnimation;
                        var shoot = item.shoot;
                        var shootSpeed = item.shootSpeed;
                        var ammo = item.ammo;
                        var useAmmo = item.useAmmo;


                        // 解析参数列表中的属性-值对
                        Dictionary<string, string> ItemVal = new Dictionary<string, string>();

                        for (int i = 3; i < args.Parameters.Count; i += 2)
                        {
                            if (i + 1 < args.Parameters.Count) // 确保有下一个参数
                            {
                                string ProName = args.Parameters[i].ToLower();
                                string value = args.Parameters[i + 1];
                                ItemVal[ProName] = value;
                            }
                        }

                        // 更新属性值
                        foreach (var kvp in ItemVal)
                        {
                            switch (kvp.Key.ToLower()) // 将键转换为小写以支持大小写不敏感
                            {
                                case "d":
                                case "da":
                                case "伤害":
                                    if (int.TryParse(kvp.Value, out int da)) damage = da;
                                    break;
                                case "c":
                                case "sc":
                                case "大小":
                                    if (float.TryParse(kvp.Value, out float sc)) scale = sc;
                                    break;
                                case "k":
                                case "kb":
                                case "击退":
                                    if (float.TryParse(kvp.Value, out float kb)) knockBack = kb;
                                    break;
                                case "t":
                                case "ut":
                                case "用速":
                                    if (int.TryParse(kvp.Value, out int ut)) useTime = ut;
                                    break;
                                case "a":
                                case "ua":
                                case "攻速":
                                    if (int.TryParse(kvp.Value, out int ua)) useAnimation = ua;
                                    break;
                                case "h":
                                case "sh":
                                case "弹幕":
                                    if (int.TryParse(kvp.Value, out int sh)) shoot = sh;
                                    break;
                                case "s":
                                case "ss":
                                case "弹速":
                                    if (float.TryParse(kvp.Value, out float ss)) shootSpeed = ss;
                                    break;
                                case "m":
                                case "am":
                                case "作弹药":
                                case "作为弹药":
                                    if (int.TryParse(kvp.Value, out int am)) ammo = am;
                                    break;
                                case "aa":
                                case "uaa":
                                case "用弹药":
                                case "使用弹药":
                                    if (int.TryParse(kvp.Value, out int uaa)) useAmmo = uaa;
                                    break;
                                default:
                                    GiveError(plr);
                                    return;
                            }
                        }

                        //从Tshcok的数据库调取玩家名字，名字与输入时的一致 则创建玩家数据 并写入配置文件
                        var acc = TShock.UserAccounts.GetUserAccountByName(other).Name;
                        if (!Config.Dict.ContainsKey(acc))
                        {
                            Config.data.Add(new Configuration.PlayerData()
                            {
                                Name = acc,
                                Hand = true,
                                Join = true,
                                ReadCount = Config.ReadCount,
                                ReadTime = DateTime.UtcNow,
                            });

                            Config.Write();
                        }

                        // 更新目标玩家的数据
                        var data2 = Config.data.FirstOrDefault(c => c.Name == acc);
                        data2.Join = true;
                        data2.ReadCount++;

                        //保存修改后的物品数据
                        SaveItem(acc, Config.Dict, item.type, stack, item.prefix, damage, scale, knockBack, useTime, useAnimation, shoot, shootSpeed, ammo, useAmmo);

                        // 播报给执行者
                        var mess = new StringBuilder();
                        mess.AppendFormat("已成功修改玩家 [{0}] 的武器:[c/92C5EC:{1}] 伤害:[c/FF6975:{2}] 大小:[c/5C9EE1:{3}] 击退:[c/5C9EE1:{4}] " +
                            "用速:[c/74E55D:{5}] 攻速:[c/94BAE0:{6}] 弹幕:[c/A3E295:{7}] 弹速:[c/F0EC9E:{8}]",
                            other, Lang.GetItemName(item.type), damage, scale, knockBack, useTime, useAnimation, shoot, shootSpeed);
                        plr.SendMessage(mess.ToString(), 255, 244, 150);

                        // 获取目标玩家 并保存修改后的物品数据
                        var plr2 = TShock.Players.FirstOrDefault(p => p != null && p.IsLoggedIn && p.Active && p.Name == acc);
                        //如果目标玩家在线，则发送消息并直接重读数值
                        if (plr2 != null)
                        {
                            UpdataRead(plr2, data2);
                            plr2.SendMessage($"管理员 [c/D4E443:{plr.Name}] 已发送修改物品: [c/92C5EC:{Lang.GetItemName(item.type)}]", 255, 244, 150);
                            plr2.SendMessage($"输入菜单指令可查看详细修改数值:[c/92D4B7:/mw]", 255, 244, 150);
                        }
                    }
                    else
                    {
                        GiveError(plr);
                    }
                    break;
                default:
                    HelpCmd(args.Player, Sel, data, ItemData);
                    break;
            }
        }
    }

    #region 设置物品回馈信息
    private static void SetItemMess(TSPlayer plr, KeyValuePair<string, List<ItemData>> itemdata, Dictionary<string, string> ProValues)
    {
        // 播报更新信息
        var mess = new StringBuilder();
        mess.Append($"手持物品使用:/mw 查看对比信息\n");
        mess.Append($"当前修改数值: ");

        foreach (var kvp in ProValues)
        {
            string propName;
            switch (kvp.Key.ToLower())
            {
                case "d":
                case "da":
                case "伤害": propName = "伤害"; break;
                case "c":
                case "sc":
                case "大小": propName = "大小"; break;
                case "k":
                case "kb":
                case "击退": propName = "击退"; break;
                case "t":
                case "ut":
                case "用速": propName = "用速"; break;
                case "a":
                case "ua":
                case "攻速": propName = "攻速"; break;
                case "h":
                case "sh":
                case "弹幕": propName = "弹幕"; break;
                case "s":
                case "ss":
                case "弹速": propName = "弹速"; break;
                case "m":
                case "am":
                case "作弹药":
                case "作为弹药": propName = "作为弹药"; break;
                case "aa":
                case "uaa":
                case "用弹药":
                case "使用弹药": propName = "使用弹药"; break;
                default: propName = kvp.Key; break;
            }
            mess.AppendFormat("[c/94D3E4:{0}]([c/94D6E4:{1}]):[c/FF6975:{2}] ", propName, kvp.Key, kvp.Value);
        }

        plr.SendMessage(mess.ToString(), 255, 244, 150);

    }
    #endregion

    #region 设置物品方法
    private static void SetItem(TSPlayer plr, Configuration.PlayerData data,
        int damage, float scale, float knockBack, int useTime, int useAnimation,
        int shoot, float shootSpeed, int ammo, int useAmmo)
    {
        var item = TShock.Utils.GetItemById(plr.SelectedItem.type);
        var index = new TSPlayer(plr.Index);
        var @new = Item.NewItem(new EntitySource_DebugCommand(), (int)index.X, (int)index.Y, item.width, item.height, item.type, item.maxStack);

        if (@new >= 0 && @new < Main.item.Length)
        {
            var newItem = Main.item[@new];

            newItem.playerIndexTheItemIsReservedFor = plr.Index;
            newItem.prefix = plr.SelectedItem.prefix;
            newItem.damage = damage;
            newItem.knockBack = knockBack;
            newItem.useTime = useTime;
            newItem.useAnimation = useAnimation;
            newItem.shoot = shoot;
            newItem.shootSpeed = shootSpeed;
            newItem.ammo = ammo;
            newItem.useAmmo = useAmmo;

            if (plr.TPlayer.selectedItem >= 0 && plr.TPlayer.selectedItem < plr.TPlayer.inventory.Length)
            {
                plr.TPlayer.inventory[plr.TPlayer.selectedItem].SetDefaults(0);
                NetMessage.SendData(5, -1, -1, null, plr.Index, plr.TPlayer.selectedItem);
            }

            plr.SendData(PacketTypes.PlayerSlot, null, @new);
            plr.SendData(PacketTypes.UpdateItemDrop, null, @new);
            plr.SendData(PacketTypes.ItemOwner, null, @new);
            plr.SendData(PacketTypes.TweakItem, null, @new, 255f, 63f);

            // 保存修改后的物品数据
            SaveItem(plr.Name, Config.Dict, newItem.type, newItem.stack, newItem.prefix, damage, scale, knockBack, useTime, useAnimation, shoot, shootSpeed, ammo, useAmmo);
        }

        data.Join = true;
        Config.Write();
    }
    #endregion

    #region 保存修改后的物品数据
    private static void SaveItem(string name, Dictionary<string, List<ItemData>> dict,
        int id, int stack, int prefix, int damage, float scale, float knockBack, int useTime,
        int useAnimation, int shoot, float shootSpeed, int ammo, int useAmmo)
    {
        if (!dict.ContainsKey(name))
        {
            dict[name] = new List<ItemData>();
        }

        // 检查是否已经存在相同的物品，如果存在则更新
        var item = dict[name].FirstOrDefault(item => item.ID == id);
        if (item != null)
        {
            item.Stack = stack;
            item.Prefix = prefix;
            item.Damage = damage;
            item.Scale = scale;
            item.KnockBack = knockBack;
            item.UseTime = useTime;
            item.UseAnimation = useAnimation;
            item.Shoot = shoot;
            item.ShootSpeed = shootSpeed;
            item.Ammo = ammo;
            item.UseAmmo = useAmmo;
        }
        else // 否则添加新的
        {
            dict[name].Add(new ItemData(id, stack, prefix, damage, scale, knockBack, useTime, useAnimation, shoot, shootSpeed, ammo, useAmmo));
        }

        Config.Write();
    }
    #endregion

    #region 读取并显示玩家的修改武器
    private static void ShowReadItem(TSPlayer plr, List<ItemData> items)
    {
        if (items == null || !items.Any())
        {
            plr.SendErrorMessage("您没有修改过的物品。");
            return;
        }

        // 构建消息
        var mess = new StringBuilder();
        var WNames = new HashSet<string>();

        foreach (var item in items)
        {
            WNames.Add(Lang.GetItemName(item.ID).ToString());
        }

        if (WNames.Count > 0)
        {
            // 为每个物品名称添加较亮的随机颜色
            var color = WNames.Select(name =>
            {
                // 生成较亮的随机颜色
                Random random = new Random();
                int r = random.Next(128, 256); // 生成128到255之间的随机数
                int g = random.Next(128, 256);
                int b = random.Next(128, 256);

                // 将RGB转换为16进制字符串
                string hexColor = $"{r:X2}{g:X2}{b:X2}";

                // 返回带有颜色标签的物品名称
                return $"[c/{hexColor}:{name}]";
            });

            // 使用逗号分隔物品名称
            mess.Append($"您已有[C/91DFBB:{WNames.Count}个]修改物品: {string.Join(" ", color)}");
        }

        // 发送消息给玩家
        plr.SendMessage(mess.ToString(), 255, 244, 150);
    }
    #endregion

    #region 列出修改过的物品信息
    private static void ListItem(CommandArgs args, TSPlayer plr, Dictionary<string, List<ItemData>> dict, int page = 1)
    {
        if (dict.TryGetValue(plr.Name, out var list) && list.Count > 0)
        {
            var PageSize = 1; // 每页显示一个物品
            var totalPages = list.Count; // 总页数等于物品总数

            if (page < 1 || page > totalPages)
            {
                plr.SendErrorMessage("无效的页码,总共有 {0} 页。", totalPages);
                return;
            }

            // 计算当前页的起始索引
            var index = (page - 1) * PageSize;

            // 获取当前页的物品
            var data = list[index];

            var itemName = Lang.GetItemNameValue(data.ID);

            // 定义属性列表及其对应的参数名称
            var properties = new[]
            {(name: "数量", value: $"{data.Stack}", param: "st"),
            (name: "前缀", value: $"{data.Prefix}", param: "pr"),
            (name: "伤害", value: $"{data.Damage}", param: "da"),
            (name: "大小", value: $"{data.Scale}", param: "sc"),
            (name: "击退", value: $"{data.KnockBack}", param: "kb"),
            (name: "用速", value: $"{data.UseTime}", param: "ut"),
            (name: "攻速", value: $"{data.UseAnimation}", param: "ua"),
            (name: "弹幕ID", value: $"{data.Shoot}", param: "sh"),
            (name: "弹速", value: $"{data.ShootSpeed}", param: "ss"),
            (name: "作为弹药", value: $"{data.Ammo}", param: "am"),
            (name: "使用弹药", value: $"{data.UseAmmo}", param: "uaa")};

            // 单独处理 "物品" 属性
            var itemMessage = $"物品([c/94D3E4:id]):[c/FFF4{new Random().Next(0, 256).ToString("X2")}:{itemName}]";

            // 拼接其他属性
            itemMessage += "\n" + string.Join("\n", Enumerable.Range(0, (properties.Length + 2) / 3)
                .Select(i => string.Join("  ", properties.Skip(i * 3).Take(3)
                    .Select(prop => $"{prop.name}([c/94D3E4:{prop.param}]):[c/FFF4{new Random().Next(0, 256).ToString("X2")}:{prop.value}]"))));

            // 翻页提示信息
            var allItems = itemMessage;
            if (page < totalPages)
            {
                var nextPage = page + 1;
                var prompt = $"请输入 [c/68A7E8:/mw list {nextPage}] 查看更多";
                allItems += $"\n{prompt}";
            }
            else if (page > 1)
            {
                var prevPage = page - 1;
                var prompt = $"请输入 [c/68A7E8:/mw list {prevPage}] 查看上一页";
                allItems += $"\n{prompt}";
            }

            // 发送消息
            plr.SendMessage($"[c/FE727D:《物品列表》]第 [c/68A7E8:{page}] 页，共 [c/EC6AC9:{totalPages}] 页:\n{allItems}", 255, 244, 150);
        }
        else
        {
            plr.SendInfoMessage("您没有任何修改物品。");
        }
    }
    #endregion

    #region 比较手上物品的修改数值 在使用菜单指令时 只列出修改过的属性
    private static Dictionary<string, object> CompareItem(ItemData Data, ItemData Sel)
    {
        var diff = new Dictionary<string, object>();
        if (Data.ID != Sel.ID) diff.Add("物品ID", Data.ID);
        if (Data.Stack != Sel.Stack) diff.Add("数量", Data.Stack);
        if (Data.Prefix != Sel.Prefix) diff.Add("前缀", Data.Prefix);
        if (Data.Damage != Sel.Damage) diff.Add("伤害", Data.Damage);
        if (Data.Scale != Sel.Scale) diff.Add("大小", Data.Scale);
        if (Data.KnockBack != Sel.KnockBack) diff.Add("击退", Data.KnockBack);
        if (Data.UseTime != Sel.UseTime) diff.Add("用速", Data.UseTime);
        if (Data.UseAnimation != Sel.UseAnimation) diff.Add("攻速", Data.UseAnimation);
        if (Data.Shoot != Sel.Shoot) diff.Add("弹幕ID", Data.Shoot);
        if (Data.ShootSpeed != Sel.ShootSpeed) diff.Add("弹速", Data.ShootSpeed);
        if (Data.Ammo != Sel.Ammo) diff.Add("作为弹药", Data.Ammo);
        if (Data.UseAmmo != Sel.UseAmmo) diff.Add("使用弹药", Data.UseAmmo);

        return diff;
    }
    #endregion

    #region 更新重读次数方法
    internal static void UpdataRead(TSPlayer plr, Configuration.PlayerData? data)
    {
        if (data == null)
        {
            plr.SendErrorMessage("没有找到该玩家的物品数据。");
            return;
        }

        var last = 0f;
        var now = DateTime.UtcNow;
        if (data.ReadTime != default)
        {
            // 上次重读时间，保留2位小数
            last = (float)Math.Round((now - data.ReadTime).TotalSeconds, 2);
        }

        if (!plr.HasPermission("mw.cd")) // 没有权限
        {
            if (data.ReadCount >= 1) // 有重读次数 直接重读
            {
                Read(plr);
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

            Config.Write();
        }
        else // 有权限直接重读
        {
            Read(plr);
        }
    }
    #endregion

    #region 重读物品数据方法
    internal static void Read(TSPlayer plr)
    {
        if (Config.Dict.TryGetValue(plr.Name, out var DataList))
        {
            foreach (var data in DataList)
            {
                var item = TShock.Utils.GetItemById(data.ID);
                var index = new TSPlayer(plr.Index);
                var @new = Item.NewItem(new EntitySource_DebugCommand(), (int)index.X, (int)index.Y, item.width, item.height, item.type, item.maxStack);

                for (int i = 0; i < plr.TPlayer.inventory.Length; i++)
                {
                    var inv = plr.TPlayer.inventory[i];
                    if (inv.type == item.type)
                    {
                        var newItem = Main.item[@new];
                        newItem.playerIndexTheItemIsReservedFor = plr.Index;

                        if (plr.SelectedItem.type == item.type)
                        {
                            newItem.prefix = plr.SelectedItem.prefix;
                        }
                        else
                        {
                            newItem.prefix = (byte)data.Prefix;
                        }

                        newItem.damage = data.Damage;
                        newItem.scale = data.Scale;
                        newItem.knockBack = data.KnockBack;
                        newItem.useTime = data.UseTime;
                        newItem.useAnimation = data.UseAnimation;
                        newItem.shoot = data.Shoot;
                        newItem.shootSpeed = data.ShootSpeed;
                        newItem.ammo = data.Ammo;
                        newItem.useAmmo = data.UseAmmo;

                        inv.SetDefaults(0);
                        NetMessage.SendData(5, -1, -1, null, plr.Index, i);
                        plr.SendData(PacketTypes.PlayerSlot, null, @new);
                        plr.SendData(PacketTypes.UpdateItemDrop, null, @new);
                        plr.SendData(PacketTypes.ItemOwner, null, @new);
                        plr.SendData(PacketTypes.TweakItem, null, @new, 255f, 63f);
                    }
                }
            }
        }
    }
    #endregion

    #region 子命令参数不对的信息反馈
    private static void SetError(TSPlayer plr)
    {
        plr.SendMessage($"\n请手持一个物品后再使用指令:[c/94D3E4: /mw s]", Color.AntiqueWhite);
        plr.SendSuccessMessage("该指令为修改自己手持物品参数,格式为:");
        plr.SendInfoMessage("/mw s d 200 a 10 … 自定义组合");
        plr.SendMessage("[c/E4EC99:参数:] 伤害[c/FF6975:d] 大小[c/5C9EE1:c] " +
            "击退[c/F79361:k] 用速[c/74E55D:t] 攻速[c/F7B661:a]\n" +
            "弹幕[c/A3E295:h] 弹速[c/F7F261:s] 作弹药[c/91DFBB:m] " +
            "用弹药[c/5264D9:aa]", 141, 209, 214);

        plr.SendInfoMessage($"弹药:先用[c/91DFBB:/mw s m 1]指定1个物品作为弹药\n" +
            $"再用[c/FF6975:/mw s aa 1]把另1个物品设为发射器");
    }

    private static void GiveError(TSPlayer plr)
    {
        plr.SendSuccessMessage("\n该指令为给予别人指定物品参数,格式为:");
        plr.SendInfoMessage("/mw g 玩家名 物品名 d 200 ua 10 …");
        plr.SendMessage("[c/E4EC99:参数:] 伤害[c/FF6975:d] 大小[c/5C9EE1:c] " +
            "击退[c/F79361:k] 用速[c/74E55D:t] 攻速[c/F7B661:a]\n" +
            "弹幕[c/A3E295:h] 弹速[c/F7F261:s] 作弹药[c/91DFBB:m] " +
            "用弹药[c/5264D9:aa]", 141, 209, 214);

        plr.SendInfoMessage($"弹药:先用[c/91DFBB:/mw g 玩家名 物品名 m 1]指定1个物品作为弹药\n" +
            $"再用[c/FF6975:/mw g 玩家名 物品名 aa 1]把另1个物品设为发射器");
    }

    private static void UpdateError(TSPlayer plr)
    {
        plr.SendSuccessMessage("\n该指令为修改玩家物品指定参数,格式为:");
        plr.SendInfoMessage("/mw up 玩家名 物品名 d 200(只改1个参数)");
        plr.SendMessage("[c/E4EC99:参数:] 伤害[c/FF6975:d] 大小[c/5C9EE1:c] " +
            "击退[c/F79361:k] 用速[c/74E55D:t] 攻速[c/F7B661:a]\n" +
            "弹幕[c/A3E295:h] 弹速[c/F7F261:s] 作弹药[c/91DFBB:m] " +
            "用弹药[c/5264D9:aa]", 141, 209, 214);
    }
    #endregion

    #region 菜单方法
    private static void HelpCmd(TSPlayer plr, Item sel, Configuration.PlayerData? data, KeyValuePair<string, List<ItemData>> itemData)
    {
        if (data == null)
        {
            plr.SendInfoMessage("请用角色[c/D95065:重进服务器]后输入：/mw 指令查看菜单\n" +
            "羽学声明：本插件纯属[c/7E93DE:免费]请勿上当受骗", 217, 217, 217);
            return;
        }

        if (!plr.HasPermission("mw.admin")) //没有管理权限
        {
            plr.SendMessage("\n          [i:3455][c/AD89D5:修][c/D68ACA:改][c/DF909A:武][c/E5A894:器][i:3454]\n" +
            "/mw hand —— 获取手持物品信息开关\n" +
            "/mw join —— 切换进服重读开关\n" +
            "/mw list —— 列出所有修改物品\n" +
            "/mw read —— 手动重读所有修改物品", Microsoft.Xna.Framework.Color.AntiqueWhite);

            if (data.Hand)
            {
                HandText(plr, sel, data, itemData);
            }
        }
        else
        {
            plr.SendMessage("\n          [i:3455][c/AD89D5:修][c/D68ACA:改][c/DF909A:武][c/E5A894:器][i:3454]\n" +
            "/mw hand —— 获取手持物品信息开关\n" +
            "/mw join —— 切换进服重读开关\n" +
            "/mw list —— 列出所有修改物品\n" +
            "/mw read —— 手动重读所有修改物品\n" +
            "/mw open 玩家名 —— 切换别人进服重读\n" +
            "/mw add 玩家名 次数 ——添加重读次数\n" +
            "/mw del 玩家名 —— 删除指定玩家数据\n" +
            "/mw up —— 修改玩家已有物品的指定属性\n" +
            "/mw set  —— 修改自己手持物品属性\n" +
            "/mw give —— 给玩家修改物品并建数据\n" +
            "/mw reads —— 切换所有人进服重读\n" +
            "/mw reset —— 重置所有玩家数据", Microsoft.Xna.Framework.Color.AntiqueWhite);

            if (data.Hand)
            {
                HandText(plr, sel, data, itemData);
            }
        }
    }
    #endregion

    #region 获取手上物品信息方法
    private static void HandText(TSPlayer plr, Item Sel, Configuration.PlayerData? data, KeyValuePair<string, List<ItemData>> ItemData)
    {
        if (!data.Join)
        {
            plr.SendSuccessMessage("重读开关:[c/E8585B:未开启]");
        }

        var mess = new StringBuilder();
        mess.Append($"修改:[c/92B9D4:{Lang.GetItemName(Sel.type)}]");
        if (ItemData.Value != null && ItemData.Value.Count > 0)
        {
            foreach (var Store in ItemData.Value)
            {
                if (Store.ID == Sel.type)
                {
                    var SelData = new ItemData(Sel.type, Sel.stack, Sel.prefix, Sel.damage, Sel.scale, Sel.knockBack,
                        Sel.useTime, Sel.useAnimation, Sel.shoot, Sel.shootSpeed, Sel.ammo, Sel.useAmmo);

                    var diffs = CompareItem(Store, SelData);

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

        plr.SendInfoMessage("手持:[c/92C5EC:{0}] 伤害[c/FF6975:{1}] 大小[c/5C9EE1:{2}] 击退[c/5C9EE1:{3}] " +
        "用速[c/74E55D:{4}] 攻速[c/94BAE0:{5}] 弹幕[c/A3E295:{6}] 弹速[c/F0EC9E:{7}] 作弹药[c/91DFBB:{8}] 用弹药[c/5264D9:{9}]",
        Lang.GetItemName(Sel.type), Sel.damage, Sel.scale, Sel.knockBack, Sel.useTime, Sel.useAnimation, Sel.shoot, Sel.shootSpeed, Sel.ammo, Sel.useAmmo);
    }
    #endregion
}
