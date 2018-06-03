# Advanced Features: `SCRIPT` Action
#### Please note, We will use `!` as a command prefix for these examples.
#### Please note: This is for a future version of RMSoftware.ModularBOT. The code has not yet been updated for this feature yet.
ModularBOT after version 1.3.689 supports this tag.
The syntax of this scripting language is similar to batch. The code formatting requires the ` ```DOS` header as both an easy syntax to read, and a surprisingly fitting syntax highlight. Red commands will output a message, while gray ones do not. Some words might get highlights, but in this context, it does not matter. (do, if, while, and probably others)

#### Example usage & syntax highlights.
![Syntax highlights](https://img.rms0.org/persist/gitimg/modu2.png)
USAGE: 

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
