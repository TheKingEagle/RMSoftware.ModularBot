# Core Command: `addcmd`
#### Please note: You will assign a custom prefix for your bot when you run initial setup. We will be using `!` as a command prefix for all future examples.
* Normal usage: `!addcmd <cmdName> <bool CommandManagersOnly> <bool LockToGuild> <Action>`
* This command is restricted to users with a DevCommand named role only.
* Prefixes are One character, usually a symbol of some kind.

* **\<cmdName>**
  * A small command name, The bot uses a custom prefix (as noted by {p}), but you do not need to specify the prefix when adding command. 

* **\<bool CommandManagersOnly>**
  * True/false. If true, Only users with one or more roles added to the bot's CommandManagement database, may access the command.

* **\<bool LockToGuild>**
  * True/false. If true, The command will only be available on the guild it was created from.

* **\<Action>**
  * The action can be several things, Most of the time, it can be a combination of text and emotes. More advanced things are covered later.
  
# Advanced Actions
Addcmd allows for one ACTION such as:
* [`EXEC` and `CLI_EXEC`](https://github.com/rmsoftware-development/RMSoftware.ModularBot/blob/master/doc/AdvancedActions/ExternalLibs.md)
* [`SCRIPT`](https://github.com/rmsoftware-development/RMSoftware.ModularBot/blob/master/doc/AdvancedActions/scripting.md)

### Example Usage: `!addcmd MyCommand false false <ACTION> <Action parameters>`
These will not output a message directly, but they perform tasks that once executed, the tasks may output messages instead.

# Variables, Flags & More.
Within the action parameter, you can specify flags, parameters and more.
* `{params}` will take all text typed after the original command and output it into the response/action.
* `{n}` will take 'n' parameter from the text. Words wrapped in Quotes will count as one parameter.
* `%self%` will output the bot's mention.
* `%invoker%` will output the command sender's mention.
* `%version%` will output the program's version number.
* `%counter%` will output the number of command uses.

At this time, you can set temporary `%flags%` by using the console command `setvar <variableName-NoPercents> <value>`. Please note, these are case sensitive.

#### Example usage: `!addcmd hug false false %invoker% hugs {params} for a long time!`
* Command: `!hug @TheBotFather`
* Output: `@GordonTrollman hugs @TheBotFather for a long time!`

#### Example usage of `{n}` flags: `!addcmd grouphug false false %invoker% brings {0}, {1}, {2}, and {3} all in for a big hug!`
* Command: `!grouphug @TheBotFather @psuedonamesslul @NotSoBot "A large cat"`
* Output: `@GordonTrollman brings @TheBotFather, @psuedonamesslul, @NotSoBot, and A large cat all in for a big hug!`
