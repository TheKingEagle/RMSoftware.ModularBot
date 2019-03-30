# Planned changes
* Add to allow for some changes, on the fly (avatar, nickname, username, prefix, etc.)
* ~~Add support for per-guild prefixes~~ DONE
* ~~Add support for multi-character prefixes~~ DONE
* Change custom commands system:
  * **Seperate Per-guild and global commands** IN PROGRESS
  * **Improve command management** IN PROGRESS
* Change console IO:
  * ~~Move ConsoleWriter to own assembly~~ New public ConsoleIO class. DONE
  * ~~Remove static references~~ DONE
  * ~~Fix output errors~~ New ConsoleIO class. DONE...?
  * ~~Move JSON Logging to Console Writer~~ Removed json logging from feature list...
* **Improve stability (killscreen/autorestart Uncaught crashes)** IN PROGRESS
* ~~Optimize code (lol)~~ RE-WRITING from scratch.
* ~~Use Discord.NET 2.0.1~~ DONE
* ~~Use Shards~~ DONE
* ~~Write new setup wizard~~ DONE
* ~~Move to JSON Configuration manager~~ DONE
* Per-guild command modules
* **New Permission system** IN PROGRESS

# Current TODOs

* FINISH Discord.NET integration
* WRITE per-change save system into Configuration
* WRITE new permissions system
* EXPAND command system
   * Add command management (Add/remove/edit/view/list commands)
   * Auto-generate GLOBAL commands and prefix (0.guild) *Note: Must keep in-sync with config*
* Write OnStart/task manager (OnStart.CORE scripting)
* Write Modules loader
