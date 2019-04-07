*DISCLAIMER: This documentation is outdated (v1), and will be re-written, once permission system is finished!*
# Core Command: `addmgrole`
#### Please note: We will always use `!` as a command prefix for usage examples.
* Usage: `!addmgrole <Role 1> [Role 2] [...]`
## Summary
This will add one or more roles to your bot's command management database. `cmdMgr.ini`.

## Granted Permissions
If a user has said role in said database, they will be able to:
* Use commands unavailable to other users.
* Create and manage commands
* Alter bot status

These users DO NOT gain the ability to:
* STOP or RESTART the bot
* Use commands that require bot owner permission.

## Special Notes:
* This command requires the user to be bot owner.
* You can also delete one or more roles by using `!delmgrole <Role1 1> [Role 2] [...]`.
* You can view a list of roles registered to the database by using `!listmgroles` and `!listallmgroles`
  * `!listmgroles` - lists registered roles in the guild command was used. 
   * Known issue: **If there are no roles, the command does not output anything.**
  * `!listallmgroles` - lists all registered roles in all guilds. 
   * Known issue: **If there are no roles, the command will output a blank code block.**
  * *These commands could be time consuming if you have many roles, or many guilds.*
