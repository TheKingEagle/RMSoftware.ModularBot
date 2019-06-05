>### Legend
>* **Indicates a WORK IN PROGRESS** ⏳
>* ### Indicates critical item 🔥
>* *Indicates HELP WANTED* ⚠
>* Indicates fully implemented ✅
>* ~~Indicates SCRAPPED feature~~ ⛔
>* Indicates TODO with no immediate action

# Planned changes
* Add to allow for some changes, on the fly (avatar, nickname, username, prefix, etc.) ✅
* Add support for per-guild prefixes ✅
* Add support for multi-character prefixes ✅
* Change custom commands system ✅ 
  * Seperate Per-guild and global commands ✅
  * Improve command management ✅
* ***NEW ConsoleIO class*** ⏳⚠
   * ~~JSon Log mode (for GUI Wrapper)~~ ⛔
   * Improved Console Layout ✅
   * ***FIX Random output glitches See Issue [#9](https://github.com/rmsoftware-development/RMSoftware.ModularBot/issues/9)*** ⏳⚠
   
* **Improve stability (killscreen/autorestart Uncaught crashes)** ⏳
   * **Undergoing testing process** ⏳
* Use Discord.NET 2.1.0 ✅
* Use Shards ✅
* Write new setup wizard ✅
* Move to JSON Configuration manager ✅
* New Permission system ✅
* **Update Checking** ⏳
* Per-guild command modules

# Current TODOs
* **FINISH CoreModule commands** ⏳
   * variables get
   * variables list
   * variables set
   
* **Automatic Updates** ⏳
   * Real time update push (Api.rms0.org)/SERVER SIDE
   * **Client-side download and run (TASK MANAGER)** ⏳
   * **Download Progress screen (CONSOLE)** ⏳
   
* Per-guild modules
   * Module Management Screen (CONSOLE)
   * Module Installer
   
* **Additional CoreSCRIPT functionality** ⏳
   * IF/ELSE statements
   
* Change default command list
   * Add support for "paginator" type command list.
   * Must have a fallback in the event of catestrophic failure (HTML)
   
* Add implementations for External Libraries to use new `ConsoleIO`.   
* *Auto-configure Shard count based on number of guilds. See [#8](https://github.com/rmsoftware-development/RMSoftware.ModularBot/issues/8)* ⚠
