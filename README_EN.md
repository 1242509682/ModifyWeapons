# ModifyWeapons

- Authors: 羽学
- Source: Tshock QQ Group 816771079
- This is a Tshock server plugin, mainly used for：
- modifying and storing player weapon data and automatically reloading. You can use the /mw command to give players items with specified attributes.

## Update Log

```
v1.0.0
Yuxue's version of custom weapons with a database
Refer to the last line of the numerical status in the /mw command menu for modifications.
To modify your own held item: `/mw s 200 1 4 20 12 938 10`
To modify and give an item to a specified player: `/mw g Yuxue Copper Shortsword 200 1 4 20 12 938 10`
Regarding projectile weapons:
Sometimes, decreasing the speed can make the frequency of projectiles more intense.
```

## Commands

| Syntax                             | Alias  |       Permission       |                   Description                   |
| -------------------------------- | :---: | :--------------: | :--------------------------------------: |
| /mw  | 无 |   mw.use    |    Command menu    |
| /mw on | 无 |   mw.use    |    Turn or off the auto-reload feature upon login    |
| /mw read | 无 |   mw.use    |    Manually reload the modified values of the item    |
| /mw open <PlayerName> | 无 |   mw.admin    |    Modify the auto-reload feature for a specified player    |
| /mw set <Damage> <Size> <Knockback> <UseTime> <AttackSpeed> <Projectile> <ProjectileSpeed> | 无 |   mw.admin    |    Modify the attributes of your own held item    |
| /mw g <PlayerName> <ItemName> <Damage> <Size> <Knockback> <UseTime> <AttackSpeed> <Projectile> <ProjectileSpeed> | 无 |   mw.admin    |    Give a specified player an item with modified attributes    |
| /mw del <PlayerName> | 无 |   mw.admin    |    Delete the data of a specified player    |
| /mw reads | 无 |   mw.admin    |    Uniformly modify the auto-reload status for everyone    |
| /mw reset | 无 |   mw.admin    |   Reset all player data    |
| /reload  | 无 |   tshock.cfg.reload    |    Reload the configuration file    |

## Configuration
> Configuration file location：tshock/修改武器.json
```json
{
  "PluginSwitch": true,
  "DataTable": [
    {
      "PlayerName": "Yuxue",
      "AutoReloadOnLogin": true,
      "ItemID": 2624,
      "Prefix": 82,
      "Damage": 200,
      "Size": 1.0,
      "Knockback": 30.0,
      "UseTime": 1,
      "AttackSpeed": 20,
      "Projectile": 200,
      "ProjectileSpeed": 1.0
    },
    {
      "PlayerName": "Anan",
      "AutoReloadOnLogin": true,
      "ItemID": 3507,
      "Prefix": 0,
      "Damage": 200,
      "Size": 1.0,
      "Knockback": 4.0,
      "UseTime": 20,
      "AttackSpeed": 12,
      "Projectile": 938,
      "ProjectileSpeed": 10.0
    }
  ]
}
```
## FeedBack
- Github Issue -> TShockPlugin Repo: https://github.com/UnrealMultiple/TShockPlugin
- TShock QQ Group: 816771079
- China Terraria Forum: trhub.cn, bbstr.net, tr.monika.love