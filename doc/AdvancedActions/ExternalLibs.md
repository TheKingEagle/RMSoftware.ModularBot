# Advanced Features: `EXEC` and `CLI_EXEC` External Libraries
#### Please note, for all these examples, we will use `!` as a prefix.
Basic info: These sub commands allow you to execute a static C# method from a dll file.

* EXEC Usage: `!addcmd excCmd false false <EXECType> <dllName>.dll <Namespace>.<Class> <FunctionName> <{params}>`
	* EXECType: Either `EXEC` or `CLI_EXEC`. Refer to a function list within a DLL readme.
	* DllName: The name of the DLL file located in the bot's "ext" folder (in the working directory of your bot).
	* Namespace, Class, and FunctionName: These parameters should be found in a readme of the dll.
	* {params}: This is how you get your bot to accept parameters for EXEC commands. This bit is usually optional. but some commands require you to add this. If you do not, the command will not work correctly.
* Example: `!addcmd AACore false false EXEC Coremod.dll Coremod.Core AArcadeCORE`
* Example: `!addcmd Scream false true CLI_EXEC Screamer.dll Scream.Io.Screamer ScreamText {params}`
	* Result: `!Scream Hello World`
	* Output: `HELLO WORLD!!!!!`

CLI_EXEC is pretty much the same thing, but the bot adds another internal parameter to provide more control to the bot itself.
* Please refer to the DLL readme to get the EXEC Type for the specified function.

## TODO:
* Documentation will soon be available for creating a library that can be called using these EXEC and CLI_EXEC methods.
## Sample EXEC Command DLL ReadMe

```
ArcadeCORE Extension Pack
ArcadeCORE is an extension pack containing functions for commands availale 
to "The Arcade" and "Rewards 1" discord guilds. (The RMSoftwareDevBot, 
a private bot implementing the RMSoftware.ModularBot framework)
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
