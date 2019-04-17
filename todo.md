# Planned changes
* Add to allow for some changes, on the fly (avatar, nickname, username, prefix, etc.)
* Add support for per-guild prefixes **DONE**
* Add support for multi-character prefixes **DONE**
* Change custom commands system:
  * Seperate Per-guild and global commands **DONE**
  * Improve command management **DONE**
* Change console IO:
  * ~~Move ConsoleWriter to own assembly~~ New public ConsoleIO class. **DONE**
  * Remove static references **DONE?**
  * ~~Fix output errors~~ New public ConsoleIO class. **DONE**
  * ~~Move JSON Logging to Console Writer~~ Removed json logging from feature list...
* **Improve stability (killscreen/autorestart Uncaught crashes) IN PROGRESS**
* Optimize code (lol) **RE-WRITING from scratch.**
* Use Discord.NET 2.0.1 **DONE**
* Use Shards **DONE**
* Write new setup wizard **DONE**
* Move to JSON Configuration manager **DONE**
* Per-guild command modules
* New Permission system **DONE**

# Current TODOs
* ~~WRITE per-change save system into Configuration~~ Auto save configuration on exit
* EXPAND command system
   * **Add command management (~~Add/remove~~/edit/view/~~list~~ commands) IN PROGRESS**
* **Write OnStart/task manager (OnStart.CORE scripting) IN PROGRESS**
* **Write Modules loader IN PROGRESS**
* **FINISH CoreModule commands IN PROGRESS**
   * about **DONE**
   * addcmd **DONE**
   * addgcmd **DONE**
   * **changes IN PROGRESS**
   * delcmd **DONE**
   * delgcmd **DONE**
   * **getcmd IN PROGRESS**
   * invitebot **DONE**
   * listcmd **DONE**
   * permissions get **DONE**
   * **permissions list IN PROGRESS**
   * permissions set role **DONE**
   * permissions set user **DONE**
   * prefix **DONE**
   * restartbot **DONE**
   * setavatar
   * setnick
   * setusername
   * status **DONE**
   * stopbot **DONE**
   * uptime **DONE**
   * variables get
   * variables list
   * variables set
