# Advanced Features: `SCRIPT` Action
#### Please note, We will use `!` as a command prefix for these examples.
ModularBOT after version 1.3.689 supports this tag.
The syntax of this scripting language is similar to batch. The code formatting requires the ` ```DOS` header as both an easy syntax to read, and a surprisingly fitting syntax highlight. Red commands will output a message, while gray ones do not. Some words might get highlights, but in this context, it does not matter. (do, if, while, and probably others)

## Example Usage and Syntax Highlights.
![Syntax highlights](https://img.rms0.org/persist/gitimg/modu2.png)

#### Usage

```
!addcmd advancedscream false SCRIPT ```DOS
ECHO SCREAMING HARD CORE! AHHHHHHHHHHHHHHHHHHHH!
SETVAR var1 I can do variables too!
ECHO %var1% -- Isn't that amazing?
```
#### Result:

![Image of output](https://img.rms0.org/persist/gitimg/modu1.png)

## Supported Functions
```
ECHO <message or command>
ECHOTTS <message or command. Read out loud using text-to-speech>
SETVAR <VariableName> <Value>
STATUSORB <Status Type>
BOTSTATUS <text>
BOTGOLIVE <twitch_ChannelName> <text>
ROLE_ADD <ulong roleID> <string successMessage>
ROLE_DEL <ulong roleID> <string successMessage>
CMD <CommandName> [command parameters]
WAIT <time_in_milliseconds+20>
EMBED <title>
EMBED_DESC <description>
EMBED_ADDFIELD "<title>" "<Value>"
EMBED_ADDFIELD_I "<title>" "<value">
EMBED_IMAGE <image url>
EMBED_THIMAGE <image url>
EMBED_FOOTER <text>
EMBED_COLOR <#HEXCOLOR>
EMBED_SEND
SET_TARGET <DIRECT or CHANNEL>
```
  
# Usage Notes
* Execution time of most functions will be 20ms. This will account for typical latency, preventing messages from being sent out of order.
### `ROLE_ADD` & `ROLE_DEL`
* Require the `Manage roles` permission in order to work.
* Will only add or delete roles that are BELOW bot's highest ranked role.
* Have an execution time of 120ms to account for API latency.
* ***ARE NOT SUPPORTED for `startup.core`***
### Emotes & Mentions
* You can output emotes, channel mentions, role mentions, and user mentions by using the proper `<>` code representations.
   * *User Mention:* `<@422031199444271124>`
   * *Role Mention:* `<@&374723463073497088>`
   * *Channel Mention:* `<#438476050650103818>`
   * *Guild emote:* `<:falcoPls:429652140714098698>`
   * To get these code representations, simply start typing the item out, then place a backslash `\` at the beginning of said item, then send the message. `\:falcoPls:`
   
