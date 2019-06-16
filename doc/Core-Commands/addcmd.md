#### Please note: You will assign a custom prefix for your bot when you run initial setup. We will be using `!` as a command prefix for all future examples.
*Reminder:*
> * Prefixes can be one or more characters, typically symbols.
>   * *Note: The command prefix cannot consist of only whitespace. \` symbol is not supported.*

# Core Command: `addcmd & addgcmd`

Usage: `!addcmd <string cmdName> <boolean restricted> <string action>` OR
Usage: `!addcmd <string cmdName> <AccessLevels CommandAccessLevel> <string action>`

* This command is restricted to users who have been registered as `AccessLevels.CommandManager` or higher. See [Permissions System](https://github.com/rmsoftware-development/RMSoftware.ModularBot/blob/v2/doc/Core-Commands/permissions.md) for more information.
* You can create a command that requires a user to be at or higher than a specific access level in the permission system. However, there are some limitations
   * You cannot create a command with a higher access level than your own.
   * You cannot create a command for `AccessLevels.Blacklisted`.


### `addgcmd` vs. `addcmd`
addgcmd will create a global command (as in, available to all guilds & dm) regardless of context. whilst addcmd will depend on said context.

The syntax for both commands will be the same.

Usage: `!addgcmd <string cmdName> <boolean restricted> <string action>` OR 
Usage: `!addgcmd <string cmdName> <AccessLevels CommandAccessLevel> <string action>`

*Context:*
* **Guild** - If `addcmd` is called here, the command will be added for that specific guild only.
* **DM** - if `addcmd` is called here, the command will be added for ALL guilds and DM, however this will require `AccessLevels.Administrator`

### Parameter breakdown
* **\<string cmdName>**
  * A small command name. You do not specify the prefix to the name when adding commands. 

* **\<boolean restricted>**
  * True/false. If true, Only users with one or more roles added to the bot's permission system, may access the command.

* **\<string action>**
   * Multiple supported types.
      * Plain text/emotes
      * [`EXEC` and `CLI_EXEC`](https://github.com/rmsoftware-development/RMSoftware.ModularBot/blob/v2/doc/AdvancedActions/ExternalLibs.md)
      * [`SCRIPT`](https://github.com/rmsoftware-development/RMSoftware.ModularBot/blob/v2/doc/AdvancedActions/scripting.md)

## Variables, Flags & More.
For the \<ACTION> segment of the command, you can specify variables and flags that will output a specific value.
* `{params}` will take all text typed after the original command and output it into the response/action.
* `{n}` will take 'n' parameter from the text. Words wrapped in Quotes will count as one parameter.
* `%self%` will output the bot's mention.
* `%self_avatar%` will output bot user's avatar url at 512 px; automatic format.
* `%invoker%` will output the command sender's mention.
* `%invoker_nomention%` will output the command sender's username and discriminator. `username#1234`
* `%version%` will output the program's version number.
* `%counter%` will output the number of command uses.
* `%bot_mem%` will output bot's memory usage in the most compact unit of storage (eg. `65.9 MB`)
* `%os_name%` will output the bot's host machine operating system name.
* `%os_ver%` will output the bot's host machine Operating system version.
* `%os_bit%` will output the bot's target platform. (`x86` or `x64`)
* `%prefix%` or `%pf%` will output context command prefix.
* `%context%` will output guild name, or Direct message.
* `%command%` will output current command name.
* `%command_count%` will output the number of available commands available in the execution context.
* `%guild_count%` will output the number of guilds the bot is in.
At this time, you can set temporary `%flags%` by using the console command `setvar <variableName-NoPercents> <value>`. Please note, these are case sensitive.

##### Example usage: `!addcmd hug false %invoker% hugs {params} for a long time!`
* Command: `!hug @TheBotFather`
* Output: `@GordonTrollman hugs @TheBotFather for a long time!`

##### Example usage of `{n}` flags: `!addcmd grouphug false %invoker% brings {0}, {1}, {2}, and {3} all in for a big hug!`
* Command: `!grouphug @TheBotFather @psuedonamesslul @NotSoBot "A large cat"`
* Output: `@GordonTrollman brings @TheBotFather, @psuedonamesslul, @NotSoBot, and A large cat all in for a big hug!`
