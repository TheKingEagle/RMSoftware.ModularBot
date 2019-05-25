Falco Lombardi is a multi-purpose bot that is designed to be customized for each guild. It includes a core module which trusted users can safely manage commands, and add specific functionality.

# Core Commands

* `!about` - Display info about Falco Lombardi
* `!changes` - Show the bot's change log
* `!addcmd` & `!addgcmd` - Add a custom command to the bot. See [Addcmd](https://github.com/rmsoftware-development/RMSoftware.ModularBot/blob/v2/doc/Core-Commands/addcmd.md) for details.
   * Usage: `!addcmd <string cmdName> <bool restrict> <string action>`
   * Requires `AccessLevels.CommandManager`
* `!delcmd` & `!delgcmd` - Delete a custom command
   * Usage: !delcmd <string cmdName>
   * Requires `AccessLevels.CommandManager`
* `!getcmd` - Displays the inner workings of a specific command
   * Usage: `!getcmd <string cmdName>`
   * Requires `AccessLevels.CommandManager`
* `!listcmd` - Display ALL available commands
* `!editcmd` - Edit an existing command
   * Requires `AccessLevels.CommandManager`
* `!prefix` - Get or set the command prefix per guild
   * Requires `AccessLevels.CommandManager` to edit.
* `!uptime` - Display how long modularBOT has been alive
* `!invitebot` - Get an invite link to add bot to your guild.
* `!shards` - Get a shard list, displaying guild number, and ping.

# Permission system

Some commands will only work for certain users who are registered into the permission system. To get all the details on this system, visit the [Permissions](https://github.com/rmsoftware-development/RMSoftware.ModularBot/blob/v2/doc/Core-Commands/permissions.md) documentation for more information.

This bot will automatically assign any user who is permitted to manage servers (thus, adding bots) the AccessLevels.CommandManager permission set.

**Please note:** This permission system will not assign actual discord roles to users. The AccessLevels in this case, are bot-specific.

## Permission commands
* `!Permissions set user` - Registers a user at specific access level
   * Usage: `!permissions set user <@usermention OR username#124> <AccessLevel>`
   * Requires `AccessLevels.CommandManager`
* `!Permissions set role` - Registers a role at specific access level
   * Usage: `!permissions set role <@RoleMention> <AccessLevel>`
   * Requires `AccessLevels.Administrator`
* `!Permissions del user` - Removes a user from the permissions system. By default any unregistered entity is assumed to have the `AccessLevels.Normal` permission set.
   * Usage `!permissions del user <@usermention OR username#124>`
   * Requires `AccessLevels.CommandManager`
* `!Permissions del role` - Removes a role from permission system.
   * Usage `!permissions del role <@RoleMention>`
   * Requires `AccessLevels.Administrator`
* `!Permissions get` - Returns a users inherrited access level.
   * Usage `!permissions get <@usermention OR UserName#1234>`
   * Requires `AccessLevels.Administrator`
* **Full Example:** `!permissions set user @TheKingEagle#0404 CommandManager`

# Custom Commands
Falco Lombardi is a customizable bot, which means you can add custom commands per guild. Any new guild will have very few commands to start out. You can give users permission to add commands to the bot using the built-in permission system.
