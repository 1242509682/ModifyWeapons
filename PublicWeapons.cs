using System.Text;
using Terraria;
using TShockAPI;
using static ModifyWeapons.Plugin;


namespace ModifyWeapons;

internal class PublicWeapons
{
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
        if (Config.ItemDatas == null || !Config.PublicWeapons) return;

        // 获取所有在线玩家
        var onlinePlayers = TShock.Players.Where(p => p != null && p.IsLoggedIn && p.Active).ToList();

        if (!onlinePlayers.Any()) return;

        // 存储要发送的消息：TSPlayer -> List<message>
        var messages = new Dictionary<TSPlayer, List<string>>();

        foreach (var item in Config.ItemDatas)
        {
            foreach (var plr in onlinePlayers)
            {
                var data2 = DB.GetData2(plr.Name, item.type);
                if (data2 == null)
                    continue;

                var diffs = MessageManager.CompareItem(item, data2.type, data2.stack, data2.prefix, data2.damage, data2.scale,
                    data2.knockBack, data2.useTime, data2.useAnimation, data2.shoot, data2.shootSpeed,
                    data2.ammo, data2.useAmmo, data2.color);

                if (diffs.Count > 0)
                {
                    var n = Lang.GetItemNameValue(item.type);
                    var t = $"[i/s{1}:{item.type}]";

                    var mess = new StringBuilder($"{t}[c/92C5EC:{n}] ");
                    foreach (var diff in diffs)
                    {
                        mess.Append($" {diff.Key}{diff.Value}");
                    }

                    if (!messages.ContainsKey(plr))
                        messages[plr] = new List<string>();

                    messages[plr].Add(mess.ToString());
                }
            }
        }

        // 如果有消息才广播
        if (messages.Count > 0)
        {
            string broadcastTitle = $"{Config.Title}[c/AD89D5:公][c/D68ACA:用][c/DF909A:武][c/E5A894:器] 已更新!";
            TShock.Utils.Broadcast(broadcastTitle, 240, 255, 150);

            foreach (var kvp in messages)
            {
                TSPlayer plr = kvp.Key;
                foreach (string msg in kvp.Value)
                {
                    plr.SendMessage(msg, 244, 255, 150);
                }
            }
        }
    }
    #endregion

    #region 添加公用武器方法
    public static void AddPublicWeapons(TSPlayer plr)
    {
        if (!Config.PublicWeapons || plr == TSServerPlayer.Server) return;

        if (Config.ItemDatas != null && Config.ItemDatas.Count > 0)
        {
            foreach (var item in Config.ItemDatas)
            {
                // 检查玩家是否已有该 type 的记录
                var data2 = DB.GetData2(plr.Name, item.type);

                if (data2 != null)
                {
                    // 更新已有记录
                    data2.stack = item.stack;
                    data2.prefix = item.prefix;
                    data2.damage = item.damage;
                    data2.scale = item.scale;
                    data2.knockBack = item.knockBack;
                    data2.useTime = item.useTime;
                    data2.useAnimation = item.useAnimation;
                    data2.shoot = item.shoot;
                    data2.shootSpeed = item.shootSpeed;
                    data2.ammo = item.ammo;
                    data2.useAmmo = item.useAmmo;
                    data2.color = item.color;
                    DB.UpdateData2(data2);
                }
                else
                {
                    // 插入新记录
                    var newData2 = new Database.MyItemData
                    {
                        PlayerName = plr.Name,
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
            }

            // 更新玩家的物品缓存
            UpdateCache();
            // 重读玩家的物品数据
            Commands.ReloadItem(plr);
        }
    }
    #endregion

    #region 移除公用武器配置的物品
    public static void RemovePublicWeapons(int type, TSPlayer plr)
    {
        if (Config.PublicWeapons && Config.ItemDatas != null)
        {
            var Count = Config.ItemDatas.Count;
            Config.ItemDatas.RemoveAll(data => data.type == type);

            if (Count != Config.ItemDatas.Count)
            {
                Config.Write();
                TSPlayer.All.SendMessage($"[c/AD89D5:公][c/D68ACA:用][c/DF909A:武][c/E5A894:器]: [i/s{1}:{type}] 已被 {plr.Name} 移除", 240, 250, 150);
            }
        }
        else
        {
            plr.SendMessage("公用武器功能未启用或配置为空。", 255, 0, 0);
        }
    }
    #endregion

    #region 更新公用武器配置
    public static void UpdatePublicWeapons(TSPlayer plr, Item item)
    {
        if (!Config.PublicWeapons || Config.ItemDatas == null) return;

        var newItemList = new List<Configuration.ItemData>(); // 用于收集要新增的项
        bool updated = false;

        foreach (var up in Config.ItemDatas)
        {
            if (up.type == item.type)
            {
                // 更新已有项
                up.prefix = item.prefix;
                up.stack = item.stack;
                up.damage = item.damage;
                up.scale = item.scale;
                up.knockBack = item.knockBack;
                up.useTime = item.useTime;
                up.useAnimation = item.useAnimation;
                up.shoot = item.shoot;
                up.shootSpeed = item.shootSpeed;
                up.ammo = item.ammo;
                up.useAmmo = item.useAmmo;
                up.color = item.color;
                updated = true;
            }
            else
            {
                // 复制一份到临时列表（不立即添加）
                newItemList.Add(new Configuration.ItemData(up));
            }
        }

        // 如果没有找到匹配项，则添加新的配置
        if (!updated)
        {
            var newItem = new Configuration.ItemData(Lang.GetItemNameValue(item.type), item.type, item.stack, item.prefix,
                item.damage, item.scale, item.knockBack, item.useTime, item.useAnimation,
                item.shoot, item.shootSpeed, item.ammo, item.useAmmo, item.color);
            Config.ItemDatas.Add(newItem);
        }

        // 保存配置
        Config.Write();

        // 发送更新消息
        PublicWeaponsMess();

        // 同步数据库
        AddPublicWeapons(plr);

    }
    #endregion

    #region 把配置里的公用武器写入给所有在线玩家数据库(离线就不用管了，反正加入游戏也会自动写入的）
    public static void WritePublicWeapons()
    {
        if (Config.PublicWeapons && Config.ItemDatas != null && Config.ItemDatas.Count > 0)
        {
            var plrs = TShock.Players.Where(p => p != null && p.Active && p.IsLoggedIn).ToList();

            if (!plrs.Any()) return;

            foreach (var itemData in Config.ItemDatas)
            {
                // 获取匹配名称或ID的物品列表
                var items = TShock.Utils.GetItemByIdOrName(itemData.Name);

                if (items.Count == 0)
                {
                    TShock.Log.Warn($"[修改武器] 加载配置时找不到物品: {itemData.Name}");
                    continue;
                }

                // 找到匹配type的物品
                var baseItem = items.FirstOrDefault(i => i.type == itemData.type) ?? items[0];

                // 创建一个新的物品并复制配置属性
                var NewItem = new Item();
                NewItem.SetDefaults(baseItem.type, false);
                NewItem.stack = itemData.stack;
                NewItem.prefix = itemData.prefix;
                NewItem.damage = itemData.damage;
                NewItem.scale = itemData.scale;
                NewItem.knockBack = itemData.knockBack;
                NewItem.useTime = itemData.useTime;
                NewItem.useAnimation = itemData.useAnimation;
                NewItem.shoot = itemData.shoot;
                NewItem.shootSpeed = itemData.shootSpeed;
                NewItem.ammo = itemData.ammo;
                NewItem.useAmmo = itemData.useAmmo;
                NewItem.color = itemData.color;

                // 更新给所有玩家
                foreach (var plr in plrs)
                {
                    PublicWeapons.UpdatePublicWeapons(plr, NewItem);
                }
            }
        }
    }
    #endregion
}
