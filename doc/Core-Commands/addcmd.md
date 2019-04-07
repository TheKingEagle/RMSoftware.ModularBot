*Disclaimer: This documentation is a WORK IN PROGRESS for the v2 update!*
# Core Command: `addcmd`
#### Please note: You will assign a custom prefix for your bot when you run initial setup. We will be using `!` as a command prefix for all future examples.
* Normal usage: `!addcmd <cmdName> <bool restricted> <action>`
* This command is restricted to users with a role added to the management database. See [`CoreCommand: addmgrole`](https://github.com/rmsoftware-development/RMSoftware.ModularBot/blob/v1.4.14.1038-PATCH/doc/Core-Commands/addmgrole.md)
* Prefixes are One character, usually a symbol of some kind.
* the new command will only be available in the guild where you ran `addcmd`.
   * If you send the `addcmd` to the bot via DM, it will be treated like `addgcmd` and be available to ALL guilds instead.

### Parameter breakdown
* **\<cmdName>**
  * A small command name. You do not specify the prefix to the name when adding commands. 

* **\<bool restricted>**
  * True/false. If true, Only users with one or more roles added to the bot's permission system, may access the command.

* **\<action>**
  * The action can be several things, Most of the time, it can be a combination of text and emotes. More advanced things are covered later.
  
# Advanced Response Actions
Addcmd allows for one ACTION such as:
* [`EXEC` and `CLI_EXEC`](https://github.com/rmsoftware-development/RMSoftware.ModularBot/blob/master/doc/AdvancedActions/ExternalLibs.md)
* [`SCRIPT`](https://github.com/rmsoftware-development/RMSoftware.ModularBot/blob/master/doc/AdvancedActions/scripting.md)

##### Example Usage: `!addcmd MyCommand false <ACTION with parameters>`
These will not output a message directly, but they perform tasks that once executed, the tasks may output messages instead.

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
