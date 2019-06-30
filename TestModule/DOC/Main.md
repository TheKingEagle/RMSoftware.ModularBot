# TestModule - A basic moderation toolkit
This is a pre-built command module shipped with ModularBOT. 
It features a small subset of commands that allow you to manage your guild with commands.

## Commands
>Please note I use `!` as a prefix with these examples. Your prefix may vary.

* #### `!ml-bind <@server Muted Role>`
   * **Summary**: This allows you to create a moderation log channel. This is where some of the mod actions are logged.
     >Note: This command requires you to have the `Manage Channels` server permission set.
     
     >Note: This command will use the channel you call the command from. Make sure your bot has permission to send embeds.

* #### `!ml-unbind`
   * **Summary**: This allows unbind a previously created moderation log channel. This is where some of the mod actions are logged.
     >Note: This command requires you to have the `Manage Channels` server permission set.
     
     >Note: This command will use the channel you call the command from. Make sure your bot has permission to send embeds.

* #### `!mute <Guild User> [reason]`
   * **Summary**: Assigns specified user the `<@server Muted Role>` with an optional reason.
     >Note: This command requires both bot and user to have the `Manage Roles` server permission set.
     
     >Note: it is up to you to properly create and setup the muted server role before using these commands.
     
* #### `!unmute <Guild User> [reason]`
   * **Summary**: Unassigns specified user the `<@server Muted Role>` with an optional reason.
     >Note: This command requires both bot and user to have the `Manage Roles` server permission set.
     
     >Note: it is up to you to properly create and setup the muted server role before using these commands.

* #### `!kick <Guild User> [reason]`
   * **Summary**: Kicks the specified user from the guild, with specified reason
     >Note: This command requires both bot and user to have the `Kick Members` server permission set.
  
* #### `!ban <Guild User> [reason]`
   * **Summary**: Bans the specified user from the guild, with specified reason
     >Note: This command requires both bot and user to have the `Ban Members` server permission set.
     
     >Note: This command will automatically purge the last 7 days of messages by banned user.
     
* #### `!cpurge <number of messages>`
   * **Summary**: Bulk-deletes up to 7 days worth of messages in the that channel.
      >Note: This 7 day limit is maximum allowed by a bot.
      
      >Note: This command requires both bot and user to have the `Manage Messages` permission set.
