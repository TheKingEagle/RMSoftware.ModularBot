*Disclaimer: This documentation is a WORK IN PROGRESS for the v2 update!*
# Core Command: `addcmd & addgcmd`
#### Please note: You will assign a custom prefix for your bot when you run initial setup. We will be using `!` as a command prefix for all future examples.
*Reminder:*
> * Prefixes can be one or more characters, typically symbols.
>   * *Note: The command prefix cannot consist of only whitespace. \` symbol is not supported.*

Usage: `!addcmd <string cmdName> <boolean restricted> <string action>`

* This command is restricted to users who have been registered as `AccessLevels.CommandManager` or higher. See [`CoreCommands: permissions`](https://github.com/rmsoftware-development/RMSoftware.ModularBot/blob/v2/doc/Core-Commands/addmgrole.md) for more information.


### `addgcmd` vs. `addcmd`
addgcmd will create a global command (as in, available to all guilds & dm) regardless of context. whilst addcmd will depend on said context.

The syntax for both commands will be the same.

Usage: `!addgcmd <string cmdName> <boolean restricted> <string action>`

*Context:*
* **Guild** - If `addcmd` is called here, the command will be added for that specific guild only.
* **DM** - if `addcmd` is called here, the command will be added for ALL guilds and DM.

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
* `%invoker%` will output the command sender's mention.
* `%version%` will output the program's version number.
* `%counter%` will output the number of command uses.

At this time, you can set temporary `%flags%` by using the console command `setvar <variableName-NoPercents> <value>`. Please note, these are case sensitive.

##### Example usage: `!addcmd hug false %invoker% hugs {params} for a long time!`
* Command: `!hug @TheBotFather`
* Output: `@GordonTrollman hugs @TheBotFather for a long time!`

##### Example usage of `{n}` flags: `!addcmd grouphug false %invoker% brings {0}, {1}, {2}, and {3} all in for a big hug!`
* Command: `!grouphug @TheBotFather @psuedonamesslul @NotSoBot "A large cat"`
* Output: `@GordonTrollman brings @TheBotFather, @psuedonamesslul, @NotSoBot, and A large cat all in for a big hug!`
