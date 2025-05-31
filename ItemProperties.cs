using Microsoft.Xna.Framework;

namespace ModifyWeapons;

//提供给公用武器数据结构的接口（方便CompareItem方法：对比原版武器与修改武器的参数解析）
public interface ItemProperties
{
    int type { get; }
    int stack { get; }
    byte prefix { get; }
    int damage { get; }
    float scale { get; }
    float knockBack { get; }
    int useTime { get; }
    int useAnimation { get; }
    int shoot { get; }
    float shootSpeed { get; }
    int ammo { get; }
    int useAmmo { get; }
    Color color { get; }
}