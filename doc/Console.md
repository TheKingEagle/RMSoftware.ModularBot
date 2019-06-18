# Console Commands
ModularBOT has a working console with several commands. These commands will help manage your instance.

**Available Commands**
* `guilds` - Show a list of all guilds program is connected to.
* `users <guild ID> [page]` - Show a list of users for a specific guild
* `channels <guild ID> [page]` - Show a list of available channels to your bot on specific guild
* `setgch <channel ID>` - Set the console to a specific guild channel for `conmsg`
* `conmsg <message>` - Send a message to the set guild channel (`setgch`)
* `config.loglevel <LogSeverity LogLevel>` - Set the current log output level to console. (Requires restart). Available log levels:
   * Debug
   * Verbose
   * Info
   * Warning
   * Error
   * Critical
* `config.setlogo` - Runs the setup wizard for console logos. Follow on-screen instruction.
* `config.setcolors` - Customizes console colors. Follow on-screen instruction.
* `config.setupdates <Boolean flag>` - Enable or disable updates. `true` or `false`
* `config.update.prerelease <Boolean flag>` - `true`: use prerelease channel. `false`: use stable channel.
* `setvar <variable name> <value>` - Set a variable that will be globally accessable in commands, or scripts.
   * **NB**: These variables will disappear on next program restart.
* `status` - Set bot activity status via console. This command will always use `Playing` for activity type.
* `mbotdata` - Launches the ModularBOT installation/running directory
* `enablecmd` - Enables command/message processing globally. NOTE: This will change the status indicator of bot to `Online` and reset bot activity status to `READY`
* `disablecmd` - Disable command/message processing globally. NOTE: This will change the status indicator of bot to `DoNotDisturb`
* `tskill` - Crash the program and show a termination prompt (Killscreen).
* `rskill` - Crash the program and show an auto-restart prompt (Killscreen).
* `cls` or `clear` - Clears current console output
* `stopbot` - Gracefully shutdown the discord connection, and exit the program.
