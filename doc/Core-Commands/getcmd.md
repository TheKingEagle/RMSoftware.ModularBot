*DISCLAIMER: This command is not yet implemented in v2.*
# Core Command: `getcmd`
#### Please note: We will always use `!` as a command prefix for usage examples.
* Usage: `!getcmd <commandTag>`

##### Output
![Simple output](https://img.rms0.org/persist/gitimg/modu3.png)

## What these mean
* Has Role Restrictions
  * If true, only a user with a role that is registered to the bot management system can use the command.
* Has Guild Restrictions
  * If true, the command can only be used in one specific guild.
  * The name of the guild will be displyed in the `What guild can use this command` section.
* Has Counter
  * If true, the command uses `%counter%` somewhere in the text and has a `counter=#` command database.
  * It will also display a `usage count` section in the breakdown.
* Response/Action
  * This will display an unprocessed output of the command. Variables will not be processed, but emotes will display.

## Special Notes
* You must be bot owner, or have a role that has been added to the bot management system in order to use this command.
