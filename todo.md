# Planned changes
* Add Config module to allow for some changes, on the fly (avatar, nickname, username, prefix, etc.)
* Add support for per-guild prefixes
* Add support for multi-character prefixes
* Change custom commands system:
  * Seperate Per-guild and global commands
  * Improve command management
* Change console IO:
  * ~Move ConsoleWriter to own assembly~ Inject into command service provider
  * Remove static references
  * Fix output errors
  * Add write events
  * Move JSON Logging to Console Writer
* Improve stability
* Optimize code (lol)
