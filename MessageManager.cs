using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using static ModifyWeapons.Plugin;

namespace ModifyWeapons;

internal class MessageManager
{
    #region 重读时显示修改物品数量
    public static void ShowReadItem(TSPlayer plr, List<int> ReItem)
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
    public static void ListItem(TSPlayer plr, int page)
    {
        var data = DB.GetAll2().Where(d => d.PlayerName == plr.Name).ToList();

        if (data.Count == 0)
        {
            plr.SendErrorMessage("没有找到您的物品数据。");
            return;
        }

        // 这里使用你自己的配置项 Config.Page 来控制每页显示多少个物品
        int itemsPerPage = Config.Page;

        int Total = (int)Math.Ceiling(data.Count / (double)itemsPerPage); // 总页数
        int newPage = Math.Clamp(page, 1, Total);

        // 获取当前页的所有物品
        var PageItems = data.Skip((newPage - 1) * itemsPerPage).Take(itemsPerPage).ToList();

        if (PageItems.Count == 0)
        {
            plr.SendErrorMessage("找不到该页的物品。");
            return;
        }

        string ColorToHex(Color color) => $"{color.R:X2}{color.G:X2}{color.B:X2}";

        var allLines = new List<string>(); // 用于存储每个物品的信息

        foreach (var item in PageItems)
        {
            var name = Lang.GetItemNameValue(item.type);
            var prefix = TShock.Utils.GetPrefixById(item.prefix);
            if (string.IsNullOrEmpty(prefix)) prefix = "无";

            var pties = new[]
            {
                (name: "数量", value: $"{item.stack}", param: "st"),
                (name: "伤害", value: $"{item.damage}", param: "da"),
                (name: "大小", value: $"{item.scale}", param: "sc"),
                (name: "击退", value: $"{item.knockBack}", param: "kb"),
                (name: "用速", value: $"{item.useTime}", param: "ut"),
                (name: "攻速", value: $"{item.useAnimation}", param: "ua"),
                (name: "弹幕", value: $"{item.shoot}", param: "sh"),
                (name: "射速", value: $"{item.shootSpeed}", param: "ss"),
                (name: "弹药", value: $"{item.ammo}", param: "am"),
                (name: "发射器", value: $"{item.useAmmo}", param: "uaa"),
                (name: "颜色", value: $"{ColorToHex(item.color)}", param: "hc"),
            };

            var Name = $"物品([c/94D3E4:{item.type}]):[c/FFF4{new Random().Next(0, 256).ToString("X2")}:{name}] | " +
                       $"前缀([c/94D3E4:{item.prefix}]):[c/FFF4{new Random().Next(0, 256).ToString("X2")}:{prefix}]";

            Name += "\n" + string.Join("\n", Enumerable.Range(0, (pties.Length + 2) / 3)
                .Select(i => string.Join("  ", pties.Skip(i * 3).Take(3)
                    .Select(prop => $"{prop.name}([c/94D3E4:{prop.param}]):[c/FFF4{new Random().Next(0, 256).ToString("X2")}:{prop.value}]"))));

            allLines.Add(Name); // 把这个物品的信息加到列表中
        }

        var all = string.Join("\n\n", allLines); // 所有物品信息拼接起来

        if (newPage < Total)
        {
            var nextPage = newPage + 1;
            var prompt = $"请输入 [c/68A7E8:/mw list {nextPage}] 查看更多";
            all += $"\n{prompt}";
        }
        else if (newPage > 1)
        {
            var prevPage = newPage - 1;
            var prompt = $"请输入 [c/68A7E8:/mw list {prevPage}] 查看上一页";
            all += $"\n{prompt}";
        }

        plr.SendMessage($"[c/FE727D:《物品列表》]第 [c/68A7E8:{newPage}] 页，共 [c/EC6AC9:{Total}] 页:\n{all}", 255, 244, 150);
    }
    #endregion

    #region 比较物品的修改数值 只列出修改过的属性
    public static Dictionary<string, object> CompareItem(ItemProperties Data, int type, int stack, int prefix, int damage, float scale, float knockBack, int useTime, int useAnimation, int shoot, float shootSpeed, int ammo, int useAmmo, Color color)
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
    public static void param(TSPlayer plr)
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

    public static void SetError(TSPlayer plr)
    {
        param(plr);
        plr.SendSuccessMessage("修改自己手上物品参数,格式为:");
        plr.SendMessage("/mw s d 20 ua 10 … 自定义组合", Color.AntiqueWhite);
    }

    public static void GiveError(TSPlayer plr)
    {
        param(plr);
        plr.SendSuccessMessage("给别人指定物品并改参数,格式为:");
        plr.SendMessage("/mw g 玩家名 物品名 d 20 ua 10 …", Color.AntiqueWhite);
        plr.SendInfoMessage("发2次:[c/91DFBB:建数据>发物品]");
    }

    public static void PwError(TSPlayer plr)
    {
        param(plr);
        plr.SendSuccessMessage("《公用武器菜单》");
        plr.SendMessage("格式1:/mw p 物品名 d 20 … 添加或修改", Color.AntiqueWhite);
        plr.SendMessage("格式2:/mw p on与off 公用武器开关", Color.AntiqueWhite);
        plr.SendMessage("格式3:/mw p del 物品名 删除玩家指定武器", Color.AntiqueWhite);
        plr.SendInfoMessage("使用指令修改[c/91DFBB:配置] 自动写入所有玩家数据");
        plr.SendSuccessMessage("公用武器数据优先级 ＞ 给物品指令数据");
    }

    public static void AllError(TSPlayer plr)
    {
        param(plr);
        plr.SendSuccessMessage("给所有人指定物品并改参数,格式为:");
        plr.SendMessage("/mw all 物品名 d 200 ua 10 …", Color.AntiqueWhite);
        plr.SendInfoMessage("\n发2次:[c/91DFBB:建数据>发物品]");
    }

    public static void UpdateError(TSPlayer plr)
    {
        param(plr);
        plr.SendInfoMessage("\n保留原有修改参数进行二次更改,格式为:");
        plr.SendMessage("/mw up 玩家名 物品名 d 20 ua 10", Color.AntiqueWhite);
    }
    #endregion

    #region 发送所有人名单方法给/mw all的执行者看
    public static void SendAllLists(TSPlayer plr, Item item, bool flag, HashSet<string> name, StringBuilder mess)
    {
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
                    var datas = DB.GetData2(other, item.type);
                    if (datas != null)
                    {
                        if (datas.type == item.type)
                        {
                            var item2 = new Item();
                            var diffs = CompareItem(datas, item2.type, item2.stack, item2.prefix, item2.damage, item2.scale, item2.knockBack,
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
    #endregion

    #region 获取手上物品信息方法
    public static void HandText(TSPlayer plr, Item Sel, Database.PlayerData? data, Database.MyItemData data2)
    {
        if (data == null) return;

        if (Sel.IsAir || Sel.type <= 0 || Sel.stack <= 0)
        {
            plr.SendErrorMessage("手上没有物品或物品无效。");
            return;
        }

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
            if (data2 != null)
            {
                if (data2.type == Sel.type)
                {
                    var diffs = CompareItem(data2, Sel.type, Sel.stack, Sel.prefix, Sel.damage, Sel.scale, Sel.knockBack,
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
    #endregion
}
