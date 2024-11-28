using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using TShockAPI;
using static ModifyWeapons.ModifyWeapons;

namespace ModifyWeapons;

public class Commands
{
    public static int MessCount = 0; //避免重复播报 UpdataRead方法内的重读消息用 只有在管理给玩家修改物品时增量
    public static void MwCmd(CommandArgs args)
    {
        var plr = args.Player;
        var Sel = plr.SelectedItem;
        var data = Config.data.FirstOrDefault(item => item.Name == plr.Name);
        var dB = DB.GetData(plr.Name);

        if (plr == null || !Config.Enabled)
        {
            return;
        }

        if (args.Parameters.Count == 0)
        {
            Help(plr);

            if (!dB.Enabled)
            {
                plr.SendMessage($"进服重读:[c/79ADE0:关闭]", 255, 170, 150);
            }
            else
            {
                plr.SendMessage($"进服重读:[c/79ADE0:启用]", 255, 170, 150);
            }

            plr.SendInfoMessage("自定:[c/92C5EC:{0}] 伤害:[c/FF6975:{1}] 大小:[c/5C9EE1:{2}] 击退:[c/5C9EE1:{3}] " +
                "用速:[c/74E55D:{4}] 攻速:[c/94BAE0:{5}] 弹幕:[c/A3E295:{6}] 弹速:[c/F0EC9E:{7}]",
                Lang.GetItemName(dB.ItemId), dB.Damage, dB.Scale, dB.KnockBack, dB.UseTime, dB.UseAnimation, dB.Shoot, dB.ShootSpeed);

            plr.SendSuccessMessage("手持:[c/92C5EC:{0}] 伤害:[c/FF6975:{1}] 大小:[c/5C9EE1:{2}] 击退:[c/5C9EE1:{3}] " +
                "用速:[c/74E55D:{4}] 攻速:[c/94BAE0:{5}] 弹幕:[c/A3E295:{6}] 弹速:[c/F0EC9E:{7}]",
                Lang.GetItemName(Sel.type), Sel.damage, Sel.scale, Sel.knockBack, Sel.useTime, Sel.useAnimation, Sel.shoot, Sel.shootSpeed);

            if (plr.HasPermission("mw.admin"))
            {
                plr.SendMessage("[c/689DD9:参数] 伤害([c/FF6975:d]) 大小([c/5C9EE1:s]) 击退([c/F79361:kb]) " +
                    "用速([c/74E55D:ut]) 攻速([c/F7B661:ua]) 弹幕([c/A3E295:sh]) 弹速([c/F7F261:ss])", 141, 209, 214);
            }

            return;
        }

        if (args.Parameters.Count == 1)
        {
            if (args.Parameters[0].ToLower() == "on")
            {
                data.Enabled = !data.Enabled;
                plr.SendInfoMessage(data.Enabled ?
                    $"玩家 [{plr.Name}] 已[c/92C5EC:启用]进服重读武器功能。" :
                    $"玩家 [{plr.Name}] 已[c/92C5EC:关闭]进服重读武器功能。");

                DB.UpdateData(data);
                LoadAllData();
                return;
            }

            if (args.Parameters[0].ToLower() == "read")
            {
                Item item = UpdataRead(plr, dB);
                return;
            }
        }

        //管理权限指令
        if (plr.HasPermission("mw.admin"))
        {
            if (args.Parameters.Count == 1)
            {
                if (args.Parameters[0].ToLower() == "reset")
                {
                    Config.data.Clear();
                    DB.ClearData();
                    LoadAllData();
                    plr.SendSuccessMessage($"已[c/92C5EC:清空]所有人的修改武器数据！");
                    return;
                }

                if (args.Parameters[0].ToLower() == "reads")
                {
                    var AllData = DB.LoadData();
                    var Enabled = AllData.Any(data => data.Enabled);
                    foreach (var All in AllData)
                    {
                        All.Enabled = !Enabled;
                        DB.UpdateData(All);
                    }
                    foreach (var All2 in Config.data)
                    {
                        All2.Enabled = !Enabled;
                    }

                    LoadAllData();

                    plr.SendSuccessMessage(!Enabled ?
                        $"已[c/92C5EC:开启]所有玩家进服重读功能！" :
                        $"已[c/92C5EC:关闭]所有玩家进服重读功能！");

                    return;
                }
            }

            if (args.Parameters.Count == 2)
            {
                switch (args.Parameters[0].ToLower())
                {
                    case "open":
                        {
                            var other = args.Parameters[1];
                            var data2 = DB.GetData(other);

                            if (data2 == null)
                            {
                                plr.SendInfoMessage($"没有找到玩家 [c/8DD1D6:{other}] 的数据记录");
                                return;
                            }

                            data2.Enabled = !data2.Enabled;
                            DB.UpdateData(data2);

                            var data3 = Config.data.FirstOrDefault(c => c.Name == other);
                            if (data3 != null)
                            {
                                data3.Enabled = data2.Enabled;
                            }

                            LoadAllData();

                            plr.SendInfoMessage(data2.Enabled ?
                                $"玩家 [c/8DD1D6:{other}] 已[c/92C5EC:启用]进服重读武器功能。" :
                                $"玩家 [c/8DD1D6:{other}] 已[c/92C5EC:关闭]进服重读武器功能。");

                            // 如果目标玩家在线，则发送消息
                            var plr2 = TShock.Players.FirstOrDefault(p => p?.Name.Equals(other, StringComparison.OrdinalIgnoreCase) ?? false);
                            if (plr2 != null && plr2.IsLoggedIn && plr2.Active)
                            {
                                plr2.SendInfoMessage(data2.Enabled ?
                                    $"您的进服重读武器功能已[c/92C5EC:启用]。" :
                                    $"您的进服重读武器功能已[c/92C5EC:关闭]。");
                            }
                            break;
                        }

                    case "del":
                        {
                            // 获取玩家名称
                            var plrname = args.Parameters[1];
                            if (DB.DelData(plrname))
                            {
                                plr.SendSuccessMessage($"已[c/92C5EC:删除]{plrname}的数据！");
                            }
                            else
                            {
                                plr.SendErrorMessage($"未能删除{plrname}的数据，数据库不存在该玩家");
                            }

                            Config.DelData(plrname);
                            Config.Write();
                            break;
                        }
                }
            }

            if (args.Parameters.Count >= 3)
            {
                switch (args.Parameters[0].ToLower())
                {
                    case "set":
                    case "s":
                        {
                            Dictionary<string, string> ProValues = new Dictionary<string, string>();

                            // 遍历参数列表，跳过第一个参数，因为它是命令名
                            for (int i = 1; i < args.Parameters.Count; i += 2)
                            {
                                if (i + 1 < args.Parameters.Count) // 确保有下一个参数
                                {
                                    string PropertyName = args.Parameters[i].ToLower();
                                    string value = args.Parameters[i + 1];
                                    ProValues[PropertyName] = value;
                                }
                            }

                            // 获取当前选定物品的最新属性
                            int damage = Sel.damage;
                            float scale = Sel.scale;
                            float knockBack = Sel.knockBack;
                            int useTime = Sel.useTime;
                            int useAnimation = Sel.useAnimation;
                            int shoot = Sel.shoot;
                            float shootSpeed = Sel.shootSpeed;

                            // 更新属性值
                            foreach (var kvp in ProValues)
                            {
                                switch (kvp.Key)
                                {
                                    case "d":
                                        if (int.TryParse(kvp.Value, out int d)) damage = d;
                                        break;
                                    case "s":
                                        if (float.TryParse(kvp.Value, out float s)) scale = s;
                                        break;
                                    case "kb":
                                        if (float.TryParse(kvp.Value, out float kb)) knockBack = kb;
                                        break;
                                    case "ut":
                                        if (int.TryParse(kvp.Value, out int ut)) useTime = ut;
                                        break;
                                    case "ua":
                                        if (int.TryParse(kvp.Value, out int ua)) useAnimation = ua;
                                        break;
                                    case "sh":
                                        if (int.TryParse(kvp.Value, out int sh)) shoot = sh;
                                        break;
                                    case "ss":
                                        if (float.TryParse(kvp.Value, out float ss)) shootSpeed = ss;
                                        break;
                                    default:
                                        NotSatSet(plr);
                                        return;
                                }
                            }

                            // 调用ModifyDamage方法更新武器
                            ModifyDamage(plr, data, damage, scale, knockBack, useTime, useAnimation, shoot, shootSpeed);

                            // 播报更新信息
                            var mess = new StringBuilder();
                            foreach (var kvp in ProValues)
                            {
                                string propName = "";
                                switch (kvp.Key)
                                {
                                    case "d": propName = "伤害"; break;
                                    case "s": propName = "大小"; break;
                                    case "kb": propName = "击退"; break;
                                    case "ut": propName = "用速"; break;
                                    case "ua": propName = "攻速"; break;
                                    case "sh": propName = "弹幕"; break;
                                    case "ss": propName = "弹速"; break;
                                    default: propName = kvp.Key; break;
                                }
                                mess.AppendFormat("{0}:[c/FF6975:{1}] ", propName, kvp.Value);
                            }
                            mess.Insert(0, $"已成功修改武器:[c/92C5EC:{Lang.GetItemName(data.ItemId)}] ");
                            plr.SendMessage(mess.ToString(), 255, 244, 150);
                            break;
                        }

                    case "give":
                    case "g":
                        {
                            if (args.Parameters.Count < 4) // 至少需要玩家名、物品名和1个属性及修改值
                            {
                                NotSatGive(plr);
                                return;
                            }

                            var other = args.Parameters[1];
                            var data2 = DB.GetData(other);
                            if (data2 == null)
                            {
                                plr.SendErrorMessage($"没有找到玩家 {other} 的数据记录");
                                return;
                            }

                            var itemQuery = args.Parameters[2];
                            var Items = TShock.Utils.GetItemByIdOrName(itemQuery);

                            if (Items.Count > 1)
                            {
                                args.Player.SendMultipleMatchError(Items.Select(i => i.Name));
                                return;
                            }

                            if (Items.Count == 0)
                            {
                                args.Player.SendErrorMessage("不存在该物品，\"物品查询\": \"[c/92C5EC:https://terraria.wiki.gg/zh/wiki/Item_IDs]\"");
                                return;
                            }

                            var item = Items[0];

                            //设置一次默认 避免沿用上个武器数值
                            item.SetDefaults(item.type);
                            int damage = item.damage;
                            float scale = item.scale;
                            float knockBack = item.knockBack;
                            int useTime = item.useTime;
                            int useAnimation = item.useAnimation;
                            int shoot = item.shoot;
                            float shootSpeed = item.shootSpeed;

                            // 如果参数为10位：/mw g 羽学 铜短剑 200 1 4 13 13 938 2.1
                            if (args.Parameters.Count == 10)
                            {
                                if (int.TryParse(args.Parameters[3], out int d) &&
                                    float.TryParse(args.Parameters[4], out float s) &&
                                    float.TryParse(args.Parameters[5], out float kb) &&
                                    int.TryParse(args.Parameters[6], out int ut) &&
                                    int.TryParse(args.Parameters[7], out int ua) &&
                                    int.TryParse(args.Parameters[8], out int sh) &&
                                    float.TryParse(args.Parameters[9], out float ss))
                                {
                                    damage = d;
                                    scale = s;
                                    knockBack = kb;
                                    useTime = ut;
                                    useAnimation = ua;
                                    shoot = sh;
                                    shootSpeed = ss;
                                }
                                else
                                {
                                    NotSatGive(plr);
                                    return;
                                }
                            }

                            else //拆散组合传参： /mw g 羽学 铜短剑 ut 20 d 200
                            {
                                // 解析参数列表中的属性-值对
                                Dictionary<string, string> ProValues = new Dictionary<string, string>();

                                for (int i = 3; i < args.Parameters.Count; i += 2)
                                {
                                    if (i + 1 < args.Parameters.Count) // 确保有下一个参数
                                    {
                                        string ProName = args.Parameters[i].ToLower();
                                        string value = args.Parameters[i + 1];
                                        ProValues[ProName] = value;
                                    }
                                }

                                // 更新属性值
                                foreach (var kvp in ProValues)
                                {
                                    switch (kvp.Key)
                                    {
                                        case "d":
                                            if (int.TryParse(kvp.Value, out int d)) damage = d;
                                            break;
                                        case "s":
                                            if (float.TryParse(kvp.Value, out float s)) scale = s;
                                            break;
                                        case "kb":
                                            if (float.TryParse(kvp.Value, out float kb)) knockBack = kb;
                                            break;
                                        case "ut":
                                            if (int.TryParse(kvp.Value, out int ut)) useTime = ut;
                                            break;
                                        case "ua":
                                            if (int.TryParse(kvp.Value, out int ua)) useAnimation = ua;
                                            break;
                                        case "sh":
                                            if (int.TryParse(kvp.Value, out int sh)) shoot = sh;
                                            break;
                                        case "ss":
                                            if (float.TryParse(kvp.Value, out float ss)) shootSpeed = ss;
                                            break;
                                        default:
                                            NotSatGive(plr);
                                            return;
                                    }
                                }
                            }

                            // 更新目标玩家的数据
                            data2.Enabled = true;
                            data2.ItemId = item.type;
                            data2.Damage = damage;
                            data2.Scale = scale;
                            data2.KnockBack = knockBack;
                            data2.UseTime = useTime;
                            data2.UseAnimation = useAnimation;
                            data2.Shoot = shoot;
                            data2.ShootSpeed = shootSpeed;
                            data2.ReadCount++;

                            // 播报给执行者
                            var mess = new StringBuilder();
                            mess.AppendFormat("已成功修改玩家 [{0}] 的武器:[c/92C5EC:{1}] 伤害:[c/FF6975:{2}] 大小:[c/5C9EE1:{3}] 击退:[c/5C9EE1:{4}] " +
                                "用速:[c/74E55D:{5}] 攻速:[c/94BAE0:{6}] 弹幕:[c/A3E295:{7}] 弹速:[c/F0EC9E:{8}]",
                                other, Lang.GetItemName(data2.ItemId), data2.Damage, data2.Scale, data2.KnockBack, data2.UseTime, data2.UseAnimation, data2.Shoot, data2.ShootSpeed);
                            plr.SendMessage(mess.ToString(), 255, 244, 150);

                            // 获取目标玩家是否在线
                            var plr2 = TShock.Players.FirstOrDefault(p => p?.Name.Equals(other, StringComparison.OrdinalIgnoreCase) ?? false);
                            // 如果目标玩家在线，则发送消息并直接重读数值
                            if (plr2 != null && plr2.IsLoggedIn && plr2.Active)
                            {
                                MessCount++;

                                UpdataRead(plr2, data2);

                                if (MessCount >= 1)
                                {
                                    plr2.SendMessage($"你的武器已被管理员 [{plr.Name}] 修改为: [c/92C5EC:{Lang.GetItemName(data2.ItemId)}]\n" +
                                    $"伤害:[c/FF6975:{data2.Damage}] 大小:[c/5C9EE1:{data2.Scale}] 击退:[c/5C9EE1:{data2.KnockBack}] " +
                                    $"用速:[c/74E55D:{data2.UseTime}] 攻速:[c/94BAE0:{data2.UseAnimation}] 弹幕:[c/A3E295:{data2.Shoot}] 弹速:[c/F0EC9E:{data2.ShootSpeed}]", 255, 244, 150);
                                    MessCount = 0;
                                }
                            }

                            // 保存更新后的数据到数据库
                            DB.UpdateData(data2);
                            LoadAllData();
                        }
                        break;
                }
            }
        }
    }

    #region 命令参数不对的参考格式
    private static void NotSatSet(TSPlayer plr)
    {
        plr.SendSuccessMessage("该指令为修改自己手持物品参数,格式为:");
        plr.SendInfoMessage("/mw set d 200 ua 10 … 自定义组合");
        plr.SendMessage("[c/689DD9:参数] 伤害([c/FF6975:d]) 大小([c/5C9EE1:s]) 击退([c/F79361:kb]) " +
            "用速([c/74E55D:ut]) 攻速([c/F7B661:ua]) 弹幕([c/A3E295:sh]) 弹速([c/F7F261:ss])", 141, 209, 214);
    }
    private static void NotSatGive(TSPlayer plr)
    {
        plr.SendSuccessMessage("该指令为给予别人指定物品参数,格式为:");
        plr.SendInfoMessage("格式1:/mw give 羽学 铜短剑 d 200 ua 10");
        plr.SendInfoMessage("格式2:/mw g 羽学 铜短剑 200 1 4 13 13 938 2");
        plr.SendMessage("[c/689DD9:参数] 伤害([c/FF6975:d]) 大小([c/5C9EE1:s]) 击退([c/F79361:kb]) " +
            "用速([c/74E55D:ut]) 攻速([c/F7B661:ua]) 弹幕([c/A3E295:sh]) 弹速([c/F7F261:ss])", 141, 209, 214);
    }
    #endregion

    #region 修改武器伤害方法
    private static void ModifyDamage(TSPlayer plr, Configuration.PlayerData? data, int damage, float scale, float knockBack, int useTime, int useAnimation, int shoot, float shootSpeed)
    {
        var item = TShock.Utils.GetItemById(plr.SelectedItem.netID);
        var index = new TSPlayer(plr.Index);
        var weapon = Item.NewItem(new EntitySource_DebugCommand(), (int)index.X, (int)index.Y, item.width, item.height, item.type, item.maxStack);

        if (weapon >= 0 && weapon < Main.item.Length)
        {
            var newItem = Main.item[weapon];
            newItem.playerIndexTheItemIsReservedFor = plr.Index;
            newItem.prefix = plr.SelectedItem.prefix;
            newItem.damage = damage;
            newItem.scale = scale;
            newItem.knockBack = knockBack;
            newItem.useTime = useTime;
            newItem.shoot = shoot;
            newItem.shootSpeed = shootSpeed;
            newItem.useAnimation = useAnimation;

            // 发送数据包
            if (plr.TPlayer.selectedItem >= 0 && plr.TPlayer.selectedItem < plr.TPlayer.inventory.Length)
            {
                plr.TPlayer.inventory[plr.TPlayer.selectedItem].SetDefaults(0);
                NetMessage.SendData(5, -1, -1, null, plr.Index, plr.TPlayer.selectedItem);
            }
            else
            {
                plr.SendErrorMessage("选定的物品槽索引超出范围。");
            }

            TSPlayer.All.SendData(PacketTypes.PlayerSlot, null, weapon);
            TSPlayer.All.SendData(PacketTypes.UpdateItemDrop, null, weapon);
            TSPlayer.All.SendData(PacketTypes.ItemOwner, null, weapon);
            TSPlayer.All.SendData(PacketTypes.TweakItem, null, weapon, 255f, 63f);

            // 更新配置文件和数据库
            data.ItemId = item.type;
            data.Prefix = newItem.prefix;
            data.Damage = damage;
            data.Scale = scale;
            data.KnockBack = knockBack;
            data.UseTime = useTime;
            data.UseAnimation = useAnimation;
            data.Shoot = shoot;
            data.ShootSpeed = shootSpeed;
            DB.UpdateData(data);
            LoadAllData();
        }
    }
    #endregion

    #region 重读武器方法
    internal static Item UpdataRead(TSPlayer plr, Configuration.PlayerData? data)
    {
        var item = TShock.Utils.GetItemById(data.ItemId);
        var index = new TSPlayer(plr.Index);
        var last = 0f;
        var now = DateTime.UtcNow;

        if (data.ReadTime != default)
        {
            //上次重读时间，保留2位小数
            last = (float)Math.Round((now - data.ReadTime).TotalSeconds, 2);
        }

        if (!plr.HasPermission("mw.cd")) //没有权限
        {
            if (data.ReadCount >= 1) //有重读次数 直接重读
            {
                ReadData(plr, data, item, index);
                data.ReadCount = Math.Max(0, data.ReadCount - 1); //最少1次 最多减到0
            }
            else //没有重读次数
            {
                if (last >= Config.ReadTime) //计算过去时间 自动累积重读次数 重置读取时间
                {
                    data.ReadCount++;
                    data.ReadTime = DateTime.UtcNow;
                }
                else //冷却时间没到 播报现在的冷却时间
                {
                    plr.SendInfoMessage($"您的重读冷却:[c/5C9EE1:{last}] < [c/FF6975:{Config.ReadTime}]秒 重读次数:[c/93E0D8:{data.ReadCount}]\n" +
                        $"请等待[c/93E0D8:{Config.ReadTime}]秒后用手动重读指令:[c/A7D3D6:/mw read]");
                }
            }

            DB.UpdateData(data);
            LoadAllData();
        }
        else //有权限直接重读
        {
            ReadData(plr, data, item, index);
        }

        return item;
    }

    private static void ReadData(TSPlayer plr, Configuration.PlayerData? data, Item item, TSPlayer index)
    {
        var weapon = Item.NewItem(new EntitySource_DebugCommand(), (int)index.X, (int)index.Y, item.width, item.height, item.type, item.maxStack);
        for (int i = 0; i < plr.TPlayer.inventory.Length; i++)
        {
            var inv = plr.TPlayer.inventory[i];
            if (inv.type == data.ItemId)
            {
                var newItem = Main.item[weapon];
                newItem.playerIndexTheItemIsReservedFor = plr.Index;

                if (plr.SelectedItem.type == data.ItemId) //如果手上拿着自定义武器
                {
                    newItem.prefix = plr.SelectedItem.prefix; //武器前缀则等于手上武器的前缀
                    data.Prefix = newItem.prefix;
                }
                else //没拿着
                {
                    newItem.prefix = (byte)data.Prefix; //前缀等于数据库里的武器前缀
                }

                newItem.damage = data.Damage;
                newItem.scale = data.Scale;
                newItem.knockBack = data.KnockBack;
                newItem.useTime = data.UseTime;
                newItem.useAnimation = data.UseAnimation;
                newItem.shoot = data.Shoot;
                newItem.shootSpeed = data.ShootSpeed;

                plr.TPlayer.inventory[i].SetDefaults(0);
                NetMessage.SendData(5, -1, -1, null, plr.Index, i);
                plr.SendData(PacketTypes.PlayerSlot, null, weapon);
                plr.SendData(PacketTypes.UpdateItemDrop, null, weapon);
                plr.SendData(PacketTypes.ItemOwner, null, weapon);
                plr.SendData(PacketTypes.TweakItem, null, weapon, 255f, 63f);
            }
        }

        if (MessCount < 1)
        {
            plr.SendMessage($"已重读武器:[c/92C5EC:{Lang.GetItemName(data.ItemId)}] 伤害:[c/FF6975:{data.Damage}] 大小:[c/5C9EE1:{data.Scale}] 击退:[c/5C9EE1:{data.KnockBack}] 用速:[c/74E55D:{data.UseTime}] 攻速:[c/94BAE0:{data.UseAnimation}] 弹幕:[c/A3E295:{data.Shoot}] 弹速:[c/F0EC9E:{data.ShootSpeed}]", 255, 244, 150);
            plr.SendInfoMessage($"[i:4080]如数值不正确,请再次输入指令恢复数值:/mw read 重读次数:[C/A7D3D6:{data.ReadCount - 1}]");
        }
    }
    #endregion

    #region 菜单方法
    private static void Help(TSPlayer plr)
    {
        var mess = new StringBuilder();
        mess.AppendFormat($"          [i:3455][c/AD89D5:修][c/D68ACA:改][c/DF909A:武][c/E5A894:器][i:3454]\n");

        if (!plr.HasPermission("mw.admin")) //没有管理权限
        {
            mess.AppendFormat("/mw -- 查看指令菜单\n" +
             "/mw on -- 开启|关闭自己进服重读\n" +
             "/mw read -- 重读自己武器数值");
        }
        else //有管理权限
        {
            mess.AppendFormat("/mw on -- 开启|关闭自己进服重读\n");
            mess.AppendFormat("/mw read -- 重读自己武器数值\n");
            mess.AppendFormat("/mw set [c/EAE88F:d 200] [c/E8585B:ss 30] -- 修改自己手持\n");
            mess.AppendFormat("/mw g [c/EBB973:玩家 物品名 伤害 大小 击退 用速 攻速 弹幕 弹速]\n");
            mess.AppendFormat("/mw g [c/86ABD9:玩家 物品名] [c/E8585B:d 200 ss 30] -- 给别人修改武器\n");
            mess.AppendFormat("/mw open [c/E99093:玩家名] -- 修改别人进服重读\n");
            mess.AppendFormat("/mw del [c/86CFD9:玩家名] -- 删除指定玩家数据\n");
            mess.AppendFormat("/mw [c/B986D9:reads] -- 修改所有进服重读\n");
            mess.AppendFormat("/mw [c/EBE991:reset] -- 重置所有玩家数据");
        }

        plr.SendMessage(mess.ToString(), Color.AntiqueWhite);
    }
    #endregion
}
