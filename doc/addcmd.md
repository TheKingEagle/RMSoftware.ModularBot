# {p}addcmd Usage
* Normal usage: `{p}addcmd <cmdName> <bool CommandManagersOnly> <bool LockToGuild> <Action>`
* This command is restricted to users with a DevCommand named role only.
* Please note: {p} defines a custom prefix you will asign for your bot when you run initial setup.
* Prefixes are One character, usually a symbol of some kind.

### Action Parameter
The action can be several things, Most of the time, it can be a combination of text and emotes. More advanced things are covered later.

### Variables, Flags & More.
Within the action parameter, you can specify flags, parameters and more.
* `{params}` will take all text typed after the original command and output it into the response/action.
* `{n}` will take 'n' parameter from the text. Words wrapped in Quotes will count as one parameter.
* `%self%` will output the bot's mention.
* `%invoker%` will output the command sender's mention.
* `%version%` will output the program's version number.
* `%counter%` will output the number of command uses.

At this time, you can set temporary `%flags%` by using the console command `setvar <variableName-NoPercents> <value>`. Please note, these are case sensitive.

Example usage for a bot using `!` as a prefix.: `!addcmd hug false false %invoker% hugs {params} for a long time!`
Command: `!hug @TheBotFather`
Output: `@GordonTrollman hugs @TheBotFather for a long time!`

Example usage of `{n}` flags: `!addcmd grouphug false false %invoker% brings {0}, {1}, {2}, and {3} all in for a big hug!`
Command: `!grouphug @TheBotFather @psuedonamesslul @NotSoBot "A large cat"`
Output: `@GordonTrollman brings @TheBotFather, @psuedonamesslul, @NotSoBot, and A large cat all in for a big hug!`

### cmdName
A small command name, The bot uses a custom prefix (as noted by {p}), but you do not need to specify the prefix when adding command. **Commands are Case Sensitive**

### CommandManagersOnly
True/false. If true, Only users with one or more roles added to the bot's CommandManagement database, may access the command.

### boolLockToGuild
True/false. If true, The command will only be available on the guild it was created from.

# Advanced command actions

## SCRIPT support
ModularBOT after version 1.3.689 supports this tag.
The syntax of this scripting language is similar to batch. The code formatting requires the ` ```DOS` header as both an easy syntax to read, and a surprisingly fitting syntax highlight. Red commands will output a message, while gray ones do not. Some words might get highlights, but in this context, it does not matter. (do, if, while, and probably others)

#### Example usage & syntax highlights.
![Syntax highlights](https://img.rms0.org/persist/gitimg/modu2.png)
USAGE (using prefix `!` for example): 

```
!addcmd advancedscream false false SCRIPT ```DOS
ECHO SCREAMING HARD CORE! AHHHHHHHHHHHHHHHHHHHH!
SETVAR var1 I can do variables too!
ECHO %var1% -- Isn't that amazing?
­```
```
#### Result:

![Image of output](https://img.rms0.org/persist/gitimg/modu1.png)

### Special notes
If you are looking for timing, please be aware, that the script adds a deliberate wait at the end of each line (of 20 ms) to prevent scripts appearing to execute out of order due to latency of discord's message delivery.
### Supported commands
```
ECHO <message or command>
SETVAR <VariableName> <Value>
BOTSTATUS <TEXT>
BOTGOLIVE <TWITCH_URL> <TEXT>
CMD <CommandName>
```
* CMD notes: This will execute a command without writing the command out to the channel (Example: `!about`). However, the executed command will output results as required. (The same way OnStart.BCMD runs.)
* You will not be able to execute user-restricted or guild-restricted commands with this feature.
* All of these commands are subject to discord's API rate limits. Please be respectful of this.
