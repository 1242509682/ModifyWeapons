using System.Text;
using Microsoft.Xna.Framework;
using TShockAPI;
using Terraria;
using Terraria.DataStructures;
using static ModifyWeapons.ModifyWeapons;
using static MonoMod.InlineRT.MonoModRule;

namespace ModifyWeapons;

public class Commands
{
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

            plr.SendInfoMessage("自定武器:[c/92C5EC:{0}] 伤害:[c/FF6975:{1}] 大小:[c/5C9EE1:{2}] 击退[c/5C9EE1:{3}] " +
                "用速:[c/74E55D:{4}] 攻速:[c/94BAE0:{5}] 弹幕:[c/A3E295:{6}] 弹速:[c/F0EC9E:{7}]",
                Lang.GetItemName(dB.ItemId), dB.Damage, dB.Scale, dB.KnockBack, dB.UseTime, dB.UseAnimation, dB.Shoot, dB.ShootSpeed);

            if (plr.HasPermission("mw.admin"))
            {
                plr.SendSuccessMessage("手持原数:[c/92C5EC:{0}] 伤害:[c/FF6975:{1}] 大小:[c/5C9EE1:{2}] 击退[c/5C9EE1:{3}] " +
                    "用速:[c/74E55D:{4}] 攻速:[c/94BAE0:{5}] 弹幕:[c/A3E295:{6}] 弹速:[c/F0EC9E:{7}]",
                    Lang.GetItemName(Sel.type), Sel.damage, Sel.scale, Sel.knockBack, Sel.useTime, Sel.useAnimation, Sel.shoot, Sel.shootSpeed);
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
                    $"玩家 [{plr.Name}] 已[c/92C5EC:禁用]进服重读武器功能。");

                Config.Write();
                DB.UpdateData(data);
                return;
            }

            if (args.Parameters[0].ToLower() == "read")
            {
                Item item = ReadWeapon(plr, dB);
                plr.SendMessage($"已重读武器:[c/92C5EC:{Lang.GetItemName(dB.ItemId)}] 伤害:[c/FF6975:{dB.Damage}] 大小:[c/5C9EE1:{dB.Scale}] 击退[c/5C9EE1:{dB.KnockBack}] 用速:[c/74E55D:{dB.UseTime}] 攻速:[c/94BAE0:{dB.UseAnimation}] 弹幕:[c/A3E295:{dB.Shoot}] 弹速:[c/F0EC9E:{dB.ShootSpeed}]", 255, 244, 150);
                plr.SendInfoMessage("[i:4080]如数值不正确,请再次输入指令恢复数值:/mw read");
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
                    Config.Write();
                    DB.ClearData();
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

                    Config.Write();

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
                                plr.SendErrorMessage($"没有找到玩家{other}的数据记录");
                                return;
                            }

                            data2.Enabled = !data2.Enabled;
                            plr.SendInfoMessage(data2.Enabled ?
                                $"玩家 [{other}] 已[c/92C5EC:启用]进服重读武器功能。" :
                                $"玩家 [{other}] 已[c/92C5EC:禁用]进服重读武器功能。");

                            Config.Write();
                            DB.UpdateData(data2);

                            // 如果目标玩家在线，则发送消息
                            var plr2 = TShock.Players.FirstOrDefault(p => p?.Name.Equals(other, StringComparison.OrdinalIgnoreCase) ?? false);
                            if (plr2 != null && plr2.IsLoggedIn && plr2.Active)
                            {
                                plr2.SendInfoMessage(data2.Enabled ?
                                    $"您的进服重读武器功能已[c/92C5EC:启用]。" :
                                    $"您的进服重读武器功能已[c/92C5EC:禁用]。");
                            }
                            break;
                        }

                    case "del":
                        {
                            // 获取玩家名称
                            var plrname = args.Parameters[1];
                            if (DB.DelData(plrname) || Config.DelData(plrname))
                            {
                                plr.SendSuccessMessage($"已[c/92C5EC:删除]{plrname}的数据！");
                            }
                            else
                            {
                                plr.SendErrorMessage($"未能删除{plrname}的数据，数据库不存在该玩家");
                            }
                            Config.Write();
                            break;
                        }
                }
            }

            if (args.Parameters.Count == 8)
            {
                switch (args.Parameters[0].ToLower())
                {
                    case "set":
                    case "s":
                        {
                            if (int.TryParse(args.Parameters[1], out var damage) &&
                                float.TryParse(args.Parameters[2], out var scale) &&
                                float.TryParse(args.Parameters[3], out var knockBack) &&
                                int.TryParse(args.Parameters[4], out var useTime) &&
                                int.TryParse(args.Parameters[5], out var useAnimation) &&
                                int.TryParse(args.Parameters[6], out var shoot) &&
                                float.TryParse(args.Parameters[7], out var shootSpeed))
                            {
                                ModifyDamage(plr, data, damage, scale, knockBack, useTime, useAnimation, shoot, shootSpeed);

                                //播报
                                var mess = new StringBuilder();
                                mess.AppendFormat("已成功修改武器:[c/92C5EC:{0}] 伤害:[c/FF6975:{1}] 大小:[c/5C9EE1:{2}] 击退[c/5C9EE1:{3}] " +
                                    "用速:[c/74E55D:{4}] 攻速:[c/94BAE0:{5}] 弹幕:[c/A3E295:{6}] 弹速:[c/F0EC9E:{7}]",
                                    Lang.GetItemName(data.ItemId), data.Damage, data.Scale, data.KnockBack, data.UseTime, data.UseAnimation, data.Shoot, data.ShootSpeed);
                                plr.SendMessage(mess.ToString(), 255, 244, 150);
                                return;
                            }
                            break;
                        }
                }
            }

            if (args.Parameters.Count == 10 && (args.Parameters[0].ToLower() == "give" || args.Parameters[0].ToLower() == "g"))
            {
                var other = args.Parameters[1];
                var data2 = DB.GetData(other);
                if (data2 == null)
                {
                    plr.SendErrorMessage($"没有找到玩家{other}的数据记录");
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

                if (int.TryParse(args.Parameters[3], out int damage) &&
                    float.TryParse(args.Parameters[4], out float scale) &&
                    float.TryParse(args.Parameters[5], out var knockBack) &&
                    int.TryParse(args.Parameters[6], out var useTime) &&
                    int.TryParse(args.Parameters[7], out var useAnimation) &&
                    int.TryParse(args.Parameters[8], out var shoot) &&
                    float.TryParse(args.Parameters[9], out var shootSpeed))
                {
                    // 更新目标玩家的数据
                    data2.Enabled = true;
                    data2.ItemId = item.type;
                    data2.Damage = damage;
                    data2.Scale = scale;
                    data2.UseTime = useTime;
                    data2.ShootSpeed = shootSpeed;
                    data2.UseAnimation = useAnimation;

                    // 保存更新后的数据到数据库
                    DB.UpdateData(data2);
                    Config.Write();

                    // 播报给执行者
                    var mess = new StringBuilder();
                    mess.AppendFormat("已成功修改玩家 [{0}] 的武器:[c/92C5EC:{1}] 伤害:[c/FF6975:{2}] 大小:[c/5C9EE1:{3}] 击退[c/5C9EE1:{4}] " +
                        "用速:[c/74E55D:{5}] 攻速:[c/94BAE0:{6}] 弹幕:[c/A3E295:{7}] 弹速:[c/F0EC9E:{8}]",
                        other, Lang.GetItemName(data2.ItemId), data2.Damage, data2.Scale, data2.KnockBack, data2.UseTime, data2.UseAnimation, data2.Shoot, data2.ShootSpeed);
                    plr.SendMessage(mess.ToString(), 255, 244, 150);

                    // 如果目标玩家在线，则直接重读武器并发送消息
                    var plr2 = TShock.Players.FirstOrDefault(p => p?.Name.Equals(other, StringComparison.OrdinalIgnoreCase) ?? false);
                    if (plr2 != null && plr2.IsLoggedIn && plr2.Active)
                    {
                        ReadWeapon(plr2, data2);
                        plr2.SendMessage($"你的武器已被管理员 [{plr.Name}] 修改为: [c/92C5EC:{Lang.GetItemName(data2.ItemId)}]\n" +
                            $"伤害:[c/FF6975:{data2.Damage}] 大小:[c/5C9EE1:{data2.Scale}] 击退[c/5C9EE1:{data2.KnockBack}] " +
                            $"用速:[c/74E55D:{data2.UseTime}] 攻速:[c/94BAE0:{data2.UseAnimation}] 弹幕:[c/A3E295:{data2.Shoot}] 弹速:[c/F0EC9E:{data2.ShootSpeed}]]", 255, 244, 150);
                    }
                }
            }
        }
    }

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
            Config.Write();
        }
    }
    #endregion

    #region 重读武器方法
    internal static Item ReadWeapon(TSPlayer plr, Configuration.PlayerData? data)
    {
        var item = TShock.Utils.GetItemById(data.ItemId);
        var index = new TSPlayer(plr.Index);
        var weapon = Item.NewItem(new EntitySource_DebugCommand(), (int)index.X, (int)index.Y, item.width, item.height, item.type, item.maxStack);
        for (int i = 0; i < plr.TPlayer.inventory.Length; i++)
        {
            var inv = plr.TPlayer.inventory[i];
            if (inv.type == data.ItemId)
            {
                var newItem = Main.item[weapon];
                newItem.playerIndexTheItemIsReservedFor = plr.Index;
                newItem.prefix = (byte)data.Prefix;
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
        return item;
    }
    #endregion

    #region 菜单方法
    private static void Help(TSPlayer plr)
    {
        var mess = new StringBuilder();
        mess.AppendFormat($"          [i:3455][c/AD89D5:修][c/D68ACA:改][c/DF909A:武][c/E5A894:器][i:3454]\n");

        if (!plr.HasPermission("mw.admin")) //没有管理权限
        {
            mess.AppendFormat("/mw —— 查看指令菜单\n" +
             "/mw on —— 开启|关闭进服重读\n" +
             "/mw read —— 手动重读修改数值", 255, 244, 150);
        }
        else //有管理权限
        {
            mess.AppendFormat("/mw on —— 开启|关闭进服重读\n" +
                "/mw read —— 手动重读修改数值\n" +
                "/mw open 玩家名 —— 修改别人进服重读\n" +
                "/mw s 伤害 大小 击退 用速 攻速 弹幕 弹速\n" +
                "/mw g 玩家 物品名 伤害.. -- 改别人\n", 255, 244, 150);
            mess.AppendFormat("/mw del 玩家名 —— 删除指定玩家数据\n", 255, 105, 120);
            mess.AppendFormat("/mw reads —— 修改所有进服重读\n", 255, 105, 120);
            mess.AppendFormat("/mw reset —— 重置所有玩家数据", 255, 105, 120);
        }

        plr.SendMessage(mess.ToString(), Color.AntiqueWhite);
    }
    #endregion
}
