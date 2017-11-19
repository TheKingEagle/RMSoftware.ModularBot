# !addcmd Usage
* Normal usage: `!addcmd <cmdName> <boolDevCmdOnly> <boolLockToGuild> <Action>`
### Action Parameter
The action can be several things, Most of the time, it can be a combination of text and emotes. More advanced things are covered later.
### cmdName
A small command name, The bot uses the ! prefix, but you do not need to specify the prefix when adding command. **Commands are Case Sensitive**
### boolDevCmdOnly
True/false. If true, only users with the DevCommand role can use this command.
### boolLockToGuild
True/false. If true, The command will only be available on the guild it was created.

# Advanced command actions

### EXEC &amp; CLI_EXEC

* EXEC Usage: `!addcmd excCmd false false EXEC <dllName>.dll <Namespace>.<Class> <FunctionName> <{params}>`
	* DllName: The name of the DLL file located in the bot's "ext" folder (in the working directory of your bot).
	* Namespace, Class, and FunctionName: These parameters should be found in a readme of the dll.
	* {params}: This is how you get your bot to accept parameters for EXEC commands. This bit is usually optional. but some commands require you to add this. If you do not, the command will not work correctly.
* Example: `!addcmd AACore false false EXEC Coremod.dll Coremod.Core AArcadeCORE`
* Example: `!addcmd Scream false true EXEC Screamer.dll Scream.Io.Screamer ScreamText {params}`
	* Result: `!Scream Hello World`
	* Output: `HELLO WORLD!!!!!`

CLI_EXEC is pretty much the same thing, but the bot adds another internal parameter to provide more control to the bot itself.
* Please refer to the DLL readme to get the EXEC Type for the specified function.


## Sample EXEC Command library ReadMe

```
ArcadeCORE Extension Pack
ArcadeCORE is an extension pack containing EXEC/CLI_EXEC Commands you can manually add to the bot with !addcmd
Version: v1.0.56
Creator: rmsoft1#1442
Website: http://rmsoftware.org/
Usage: !addcmd <command> <DevOnly> <ThisGuildOnly> <EXEC/CLI_EXEC*> Coremod.dll Coremod.Core <FunctionName> ({params})

**Function List**


Function Name		EXECType	Require{params}		Description
------------------------------------------------------------------------------------------------------------------------------
AArcadeCORE		EXEC		NO			Display info about ArcadeCORE.
DoServerCheck		EXEC		NO			Check status of RMCraft.net Minecraft server.
DoR1Check		EXEC		NO			Check if Rewards1.com is down.
DoStatusChange		CLI_EXEC	YES			Set the bot's "Playing" status.
SetSTA_Away		CLI_EXEC	NO			Set the bot orb to yellow (AWAY).
SetSTA_Online		CLI_EXEC	NO			Set the bot orb to green (ONLINE).
SetSTA_DoNotDisturb	CLI_EXEC	NO			Set the bot orb to red (DoNotDisturb).
DoUserChannelMute	CLI_EXEC	NO			Revoke chatSend permissions from user in specific channel.
DoUserChannelUnMute	CLI_EXEC	NO			Re-add chatSend permissions to user in specific channel.
GenerateShortURL	EXEC		YES			Create a new shortURL.
GenerateShortURLAlias	EXEC		YES			Create a new shortURL with custom Alias.


To use the library, Go to your bot's working directory, and place the .dll file into ext.
```