# Planned changes
* Add Config module to allow for some changes, on the fly (avatar, nickname, username, prefix, etc.)
* Add support for per-guild prefixes
* ~Add support for multi-character prefixes~ DONE
* Change custom commands system:
  * Seperate Per-guild and global commands
  * Improve command management
* Change console IO:
  * ~Move ConsoleWriter to own assembly~ Added class ConsoleIO Helper
  * Remove static references
  * ~Fix output errors~ New ConsoleIO Fixed?
  * ~Add write events~
  * ~Move JSON Logging to Console Writer~
* Improve stability
* Optimize code (lol)
* Use Discord.NET 2.0.1
* ~Write new setup wizard~ DONE
* ~Move to JSON Configuration manager~ DONE
* Per-guild command modules
* BETTER ROLE MANAGEMENT

# Current TODOs

* Write actual discord client integration
* Write command system
* Write OnStart/task manager
* Write OnStart/Module loader
* Write permissions system
