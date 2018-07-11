# Advanced Features: `SCRIPT` Action
#### Please note, We will use `!` as a command prefix for these examples.
ModularBOT after version 1.3.689 supports this tag.
The syntax of this scripting language is similar to batch. The code formatting requires the ` ```DOS` header as both an easy syntax to read, and a surprisingly fitting syntax highlight. Red commands will output a message, while gray ones do not. Some words might get highlights, but in this context, it does not matter. (do, if, while, and probably others)

## Example Usage and Syntax Highlights.
![Syntax highlights](https://img.rms0.org/persist/gitimg/modu2.png)

#### Usage

```
!addcmd advancedscream false false SCRIPT ```DOS
ECHO SCREAMING HARD CORE! AHHHHHHHHHHHHHHHHHHHH!
SETVAR var1 I can do variables too!
ECHO %var1% -- Isn't that amazing?
­```
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
CMD <CommandName> [command parameters]
WAIT <time_in_milliseconds(+20ms)>
```
## Special Notes
* Each command has a +20ms delay on it. This is to prevent out of order message processing. The WAIT command also has this delay for now. It is planned for removal soon.
* CMD notes: This will execute a command without writing the command out to the channel (Example: `!about`). However, the executed command will output results as required. (The same way OnStart.Core runs.)
* You will not be able to execute user-restricted or guild-restricted commands with this feature.
* All of these commands are subject to discord's API rate limits. Please be respectful of this.
