>### Legend
>* **Indicates a WORK IN PROGRESS** ⏳
>* ### Indicates critical item 🔥
>* *Indicates HELP WANTED* ⚠
>* Indicates fully implemented ✅
>* ~~Indicates SCRAPPED feature~~ ⛔
>* Indicates TODO with no immediate action

# Planned changes
* Use Discord.NET 2.1.0 ✅
* Use Shards ✅
* Add to allow for some changes, on the fly (avatar, nickname, username, prefix, etc.) ✅
* Add support for per-guild prefixes ✅
* Add support for multi-character prefixes ✅
* Change custom commands system ✅ 
  * Seperate Per-guild and global commands ✅
  * Simplify command management syntax ✅
  
* Move to JSON Configuration manager ✅
* New Permission system ✅
* Interactive Command list (Paginator) ✅

* **Improve stability (killscreen/autorestart Uncaught crashes)** ⏳
   * **Undergoing testing process** ⏳
* **Update Checking** ⏳
* **Per-guild command modules** ⏳

* **Better console-based management** ⏳
   >* ***NEW ConsoleIO class*** ⏳⚠
   >   * ~~JSon Log mode (for GUI Wrapper)~~ ⛔
   >   * Improved Console Layout ✅
   >   * ***FIX Random output glitches & lockout. See Issue [#9](https://github.com/rmsoftware-development/RMSoftware.ModularBot/issues/9)*** ⏳⚠

   >* Write new setup wizard ✅
   >* Add a guilds and users list screen ✅
   >* Add a channels list ✅
   >* Add user search ✅
   >* **Add guild search** ⏳
   >* **Download Progress screen (CONSOLE)** ⏳
   >* Module Management Screen
   >* Add Module Installer
   
   
# Current TODOs
* **Automatic Updates** ⏳
   * **Client-side download and run (TASK MANAGER)** ⏳
   * **Download Progress screen (CONSOLE)** ⏳
   * Real time update push (Api.rms0.org)/SERVER SIDE
   
* **Per-guild modules** ⏳
   * Per-guild modules (Execution) ✅
   * Per-guild modules (Command listing) ✅
   * Module Management Screen (CONSOLE)
   * Module Installer
   
* **Additional CoreSCRIPT functionality** ⏳
   * IF/ELSE statements
   * Add `EMBED_URL` and `EMBED_FOOTER_IMG` Functions
   
* **Update External Library support** ⏳
   * Remove `CLI_EXEC`
   * Change `EXEC` to reference Discord.NET's client, message, and `ConsoleIO` class.
* ### FIX Random output glitches See Issue [#9](https://github.com/rmsoftware-development/RMSoftware.ModularBot/issues/9) 🔥⏳⚠
* *Auto-configure Shard count based on number of guilds. See [#8](https://github.com/rmsoftware-development/RMSoftware.ModularBot/issues/8)* ⚠
