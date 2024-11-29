# ModifyWeapons

- Authors: 羽学
- Source: Tshock QQ Group 816771079
- This is a Tshock server plugin, mainly used for：
- modifying and storing player weapon data and automatically reloading. You can use the /mw command to give players items with specified attributes.

## Update Log

```
v1.2.0
- Refactored code to support customization of multiple weapon items.
- Removed database logic, switched to using configuration files for data storage.
- When the "Admin-only data creation on join" configuration option is enabled:
- Player data will only be created after an item is given to that player using the /mw g command.
- Added more commands:
  When inputting /mw s or g or up without values, it will prompt the corresponding tutorial.
  (Successful input will ignore the player's own reload count and immediately update the in-game item status.)
- Using /mw list supports paging through all modified weapon values (1 per page).
- The /mw up command will only update specific parameters of the designated player's item, provided the item has been modified before.

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
| /mw  | None |   mw.use    |    Command    |
| /mw hand | None |   mw.use    |    Toggle switch for getting held item info    |
| /mw join | None |   mw.use    |    Toggle switch for login reload    |
| /mw list | None |   mw.use    |    List all modified items    |
| /mw read | None |   mw.use    |    Manually reload all modified items    |
| /mw open PlayerName | None |   mw.admin    |    Switch another player's login reload state    |
| /mw add PlayerName Count | None |   mw.admin    |    Add reload counts    |
| /mw del PlayerName | None |   mw.admin    |    Delete specified player's data    |
| /mw up | /mw g |   mw.admin    |    Modify specific attributes of a player's existing "modified item"    |
| /mw set | /mw s |   mw.admin    |    Modify held item attributes    |
| /mw give | /mw g |   mw.admin    |    Give a player a modified item and create data    |
| /mw reads | None |   mw.admin    |    Toggle everyone's login reload state    |
| /mw reset | None |   mw.admin    |    Reset all player data    |
| /reload  | None |   tshock.cfg.reload    |    Reload configuration file    |
| None  | None |   mw.cd    |    Ignore cooldown and count for reloading weapons    |

## Configuration
> Configuration file location：tshock/修改武器.json
```json
{
  "PluginEnabled": true,
  "InitialReloadCount": 2,
  "AdminOnlyDataCreationOnJoin": true,
  "CooldownSecondsForReloadIncrement": 1800.0,
  "PlayerData": [
    {
      "PlayerName": "Yuxue",
      "ReloadCount": 2,
      "GetHeldItemInfo": true,
      "LoginReloadSwitch": true,
      "LastReloadCooldownTimestamp": "2024-11-29T21:38:42.1398775Z"
    }
  ],
  "ModifiedItemData": {
    "Yuxue": [
      {
        "ItemId": 4952,
        "Stack": 1,
        "Prefix": 0,
        "Damage": 100,
        "Size": 0.7,
        "Knockback": 2.5,
        "UseTime": 2,
        "AttackSpeed": 36,
        "ProjectileId": 931,
        "ProjectileSpeed": 17.0,
        "AmmoType": 0,
        "UsesAmmo": 0
      }
    ]
  }
}
```
## FeedBack
- Github Issue -> TShockPlugin Repo: https://github.com/UnrealMultiple/TShockPlugin
- TShock QQ Group: 816771079
- China Terraria Forum: trhub.cn, bbstr.net, tr.monika.love