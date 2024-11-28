# ModifyWeapons

- Authors: 羽学
- Source: Tshock QQ Group 816771079
- This is a Tshock server plugin, mainly used for：
- modifying and storing player weapon data and automatically reloading. You can use the /mw command to give players items with specified attributes.

## Update Log

```
v1.1.0
- Added the ability to synchronize data within the configuration using /reload to the database.
- If the data table in the configuration is accidentally deleted, restarting the server will write it back from the database to the configuration file.

- Added logic for combining attribute modifications:
  /mw set d 100 ss 20
  /mw g PlayerName ItemName d 100 ss 20

- Added a cooldown mechanism and reload count (immune permission: mw.cd).
  By default, when a player logs in for the first time, they have 2 reload counts, and the login reload is turned off by default.
  When the cooldown reaches the preset seconds in the Config, 1 reload count is added automatically.
  According to the reload count, you can directly use /mw read to manually reload the weapon values.
  If the cooldown is not reached or there are no reload counts, take a screenshot of your /mw menu page and send it to the admin.
  The admin can also help the player manually reload in the background:
  /mw g PlayerName WeaponName Value...
  (This command will also actively enable the player's login reload function)

- Fixed the bug where /mw del would not delete the configuration file.
- Fixed the bug where /mw open would not synchronize the configuration file.
- Fixed the bug where reforging and then reloading the weapon would swallow the weapon prefix.
  /mw g will check if the player is holding a custom weapon to get the accurate prefix.
  (It is recommended that the player selects the custom weapon before the admin uses /mw g)


v1.0.0
- Yuxue's version of custom weapons with a database.
- Refer to the bottom line of the /mw command menu for the value status to modify.
- Modifying your held item: /mw s 200 1 4 20 12 938 10
- Modifying and giving a specific player an item: /mw g Yuxue Copper Shortsword 200 1 4 20 12 938 10
- About projectile weapons:
  Sometimes, reducing speed actually makes the projectile frequency denser.
```

## Commands

| Syntax                             | Alias  |       Permission       |                   Description                   |
| -------------------------------- | :---: | :--------------: | :--------------------------------------: |
| /mw  | None |   mw.use    |    Command menu    |
| /mw on | None |   mw.use    |    Turn on or off personal login reload    |
| /mw read | None |   mw.use    |    Manually reload personal weapon stats    |
| /mw s d 200 ut 20 | /mw set |   mw.admin    |   Modify the attributes of the held item    |
| /mw g PlayerName ItemName d 200 ut 20 | /mw give |   mw.admin    |   Give a modified item with values to a specified player (Format 1)    |
| /mw g PlayerName ItemName Damage Size Knockback UseTime AttackSpeed Projectiles ProjectileSpeed | /mw give |   mw.admin    |   Give a modified item with values to a specified player (Format 2)    |
| /mw open <PlayerName> | None |   mw.admin    |   Modify someone else's login reload function    |
| /mw del <PlayerName> | None |   mw.admin    |   Delete the specified player's data    |
| /mw reads | None |   mw.admin    |    Uniformly modify everyone's login reload status    |
| /mw reset | None |   mw.admin    |   Reset all player data    |
| /reload  | None |   tshock.cfg.reload    |    Reload the configuration file    |
| None  | None |   mw.cd    |    Ignore cooldown and count for weapon reload permission    |

## Configuration
> Configuration file location：tshock/修改武器.json
```json
{
  "PluginSwitch": true,
  "InitialReloadCount": 2,
  "CooldownForAdditionalReloads": 180.0,
  "DataTable": [
    {
      "PlayerName": "Yuxue",
      "LoginReload": true,
      "ItemID": 2624,
      "Prefix": 82,
      "Damage": 200,
      "Size": 1.0,
      "Knockback": 2.0,
      "UseTime": 1,
      "AttackSpeed": 30,
      "Projectiles": 1,
      "ProjectileSpeed": 10.0,
      "ReadTime": "2024-11-28T06:28:08.2528562",
      "ReloadCount": 1
    },
    {
      "PlayerName": "Anan",
      "LoginReload": true,
      "ItemID": 3507,
      "Prefix": 14,
      "Damage": 500,
      "Size": 1.0,
      "Knockback": 4.0,
      "UseTime": 1,
      "AttackSpeed": 30,
      "Projectiles": 938,
      "ProjectileSpeed": 2.1,
      "ReadTime": "2024-11-28T07:09:32.7053442",
      "ReloadCount": 0
    }
  ]
}
```
## FeedBack
- Github Issue -> TShockPlugin Repo: https://github.com/UnrealMultiple/TShockPlugin
- TShock QQ Group: 816771079
- China Terraria Forum: trhub.cn, bbstr.net, tr.monika.love