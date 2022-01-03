using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Discord.Addons.Interactive;
using ModularBOT.Entity;
using ModularBOT.Component.ConsoleScreens;
using System.Diagnostics;
using System.Threading;

namespace ModularBOT.Component
{
    [Summary("Pre-installed commands for basic management | ModularBOT • CORE v2.0")]
    public class CoreModule:InteractiveBase<CommandContext>
    {
        #region Property/Construct
        DiscordShardedClient Client { get; set; }
        CommandService Cmdsvr { get; set; }
        ConsoleIO ConsoleIO { get; set; }
        DiscordNET DiscordNet { get; set; }
        DiscordNET _discordNet = null;//used purely for updater

        public CoreModule(DiscordShardedClient client, CommandService cmdservice, ConsoleIO consoleIO, DiscordNET dnet)
        {
            if(client.LoginState != LoginState.LoggedIn)
            {
                this.ConsoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "CoreMOD", "That there client isn't logged in yet."));
                System.Threading.SpinWait.SpinUntil(() => client.LoginState == LoginState.LoggedIn);

            }
            Client = client;
            Cmdsvr = cmdservice;
            this.ConsoleIO = consoleIO;
            DiscordNet = dnet;
            _discordNet = dnet;
            this.ConsoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "CoreMOD", "Constructor called! This debug message proved it."));

        }

        #endregion

        [Command("about"), Summary("Display information about the bot"), Remarks("AccessLevels.Normal")]
        public async Task CORE_ShowAbout()
        {
            EmbedBuilder builder = new EmbedBuilder
            {
                Title = "About"
            };
            builder.WithAuthor(Context.Client.CurrentUser);
            builder.Color = Color.Blue;
            builder.Description = "A Multi-purpose, multi-module bot designed for discord. Tailor it for your specific server, create your own modules and plug-ins. Includes a core module for custom text-based commands & other advanced functionality";
            builder.AddField("Copyright", $"Copyright © 2017-{DateTime.Now.Year} RMSoftware Development");
            builder.AddField("Version", "v"+ Assembly.GetExecutingAssembly().GetName().Version.ToString(4));
            builder.WithFooter("ModularBOT • Created by TheKingEagle");
            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("sysupdate"), Summary("Attempt to automatically update the instance"), Remarks("AccessLevels.Administrator")]
        public async Task CORE_SysUpdate()
        {
            try
            {
                
                SpinWait.SpinUntil(() => !ConsoleIO.ScreenBusy);
                if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
                {
                    await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                    return;
                }
                if (ConsoleIO.ScreenModal)
                {
                    await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Console Busy",
                        "Unable to perform an automatic update. The console is displaying a dialog or screen. " +
                        "Take care of that first, then manually run the `update` command via console.", Color.DarkRed));

                    return;
                }
                EmbedBuilder builder = new EmbedBuilder
                {
                    Title = "System Update"
                };
                builder.WithAuthor(Context.Client.CurrentUser);
                builder.Color = Color.Blue;
                builder.Description = "Attempting to Check for a system update...\r\n\r\nYou may need to check the console...";
                builder.WithFooter("ModularBOT • Created by TheKingEagle");
                await Context.Channel.SendMessageAsync("", false, builder.Build());
                SpinWait.SpinUntil(() => !ConsoleIO.ScreenBusy);
                var NGScreen = new UpdaterScreen(ref Program.configMGR.CurrentConfig, ref _discordNet, true);
                ConsoleIO.ShowConsoleScreen(NGScreen, true,true);

                if (NGScreen.InstallUpdate)
                {
                    SpinWait.SpinUntil(() => !ConsoleIO.ScreenBusy);
                    builder.WithAuthor(Context.Client.CurrentUser);
                    builder.Color = Color.Green;
                    builder.Description = $"System update available! Preparing to install new version: `{NGScreen.UPDATEDVER}`";
                    builder.WithFooter("ModularBOT • Created by TheKingEagle");
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                    ConsoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Terminating application and running system update..."));
                    Program.ImmediateTerm = true;
                    Process.Start(NGScreen.UPDATERLOC, "/silent");
                    Thread.Sleep(500);
                    _discordNet.Stop(ref Program.ShutdownCalled);
                    Program.RestartRequested = false;

                    return;
                }
                else
                {
                    SpinWait.SpinUntil(() => !ConsoleIO.ScreenBusy);
                    builder.WithAuthor(Context.Client.CurrentUser);
                    builder.Color = Color.DarkGreen;
                    builder.Description = $"`{Context.Client.CurrentUser.Username}` is up-to-date!";
                    builder.WithFooter("ModularBOT • Created by TheKingEagle");
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                }
                return;
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString());
            }
            
        }
        //TODO: Come up with automation for generating this.
        //[Command("changes"), Summary("Shows what changed in this version")]
        //public async Task CORE_ShowChanges()
        //{
        //    EmbedBuilder eb = new EmbedBuilder();

        //    eb.WithAuthor("What's New", Client.CurrentUser.GetAvatarUrl(), "");
        //    eb.WithDescription("This version may not be compatible with pre-existing scripts.");
        //    eb.AddField($"v{Assembly.GetExecutingAssembly().GetName().Version.ToString(4)} ModularBOT System Update [BETA]",
        //        $"• REWRITE: CoreScript now implements a modular function list.\r\n" +
        //        $"• RENAMED: `BOTSTATUS` to `TITLE`\r\n" +
        //        $"• FIX: Proper Error handling for misconfiguration`\r\n" +
        //        $"• ADD: `config.reset` to console commands\r\n" +
        //        $"• FIX: KillScreen stacktrace malformed\r\n" +
        //        $"• CHANGE: Startup.CORE now supports the same commands as a standard CoreScript call.\r\n" +
        //        $"• SOURCE: Refactoring/TODOs");


        //    eb.WithFooter("ModularBOT • Core");
        //    eb.Color = Color.DarkBlue;
        //    await Context.Channel.SendMessageAsync("**Full version history/change log: http://rms0.org?a=mbChanges**", false, eb.Build());
        //}

        #region Command Management
        [Command("addcmd"),Summary("Add a command to your bot. If you run this via DM, it will create a global command. (NOTE: Creating a global commands requires AccessLevels.Administrator)"), Remarks("AccessLevels.CommandManager")]
        public async Task CMD_Add(string cmdname, bool restricted, [Remainder]string action)
        {
            
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            if (string.IsNullOrWhiteSpace(cmdname))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's illegal.", $"Please give a valid command name.", Color.DarkRed));
                return;
            }
            if (action == "{params}" || action == "{0}")
            {
                if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
                {
                    await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Sorry!",$"You may not create this type of command. You must have `AccessLevels.Administrator`.",Color.DarkRed));
                    return;
                }
            }
            ulong gid = 0;
            if(Context.Guild != null)
            {
                gid = Context.Guild.Id;
            }
            if(gid==0)
            {
                if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)//global commands require administrator priv.
                {
                    await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                    return;
                }
            }
            await DiscordNet.CustomCMDMgr.AddCmd(Context.Message, cmdname, action, restricted,gid);
        }

        [Command("addcmd"), Summary("Add a command to your bot. If you run this via DM, it will create a global command. (NOTE: Creating a global commands requires AccessLevels.Administrator)"), Remarks("AccessLevels.CommandManager")]
        public async Task CMD_Add(string cmdname, AccessLevels CommandAccessLevel, [Remainder]string action)
        {
            
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            if (string.IsNullOrWhiteSpace(cmdname))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's illegal.", $"Please give a valid command name.", Color.DarkRed));
                return;
            }
            if (action == "{params}" || action == "{0}")
            {
                if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
                {
                    await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Sorry!", $"You may not create this type of command. You must have `AccessLevels.Administrator`.", Color.DarkRed));
                    return;
                }
            }
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < CommandAccessLevel)
            {
                await ReplyAsync("", false, 
                    GetEmbeddedMessage("Wait... That's Illegal.", "You can't create a command you won't have permission to use.", Color.DarkRed));
                return;
            }
            if (CommandAccessLevel == AccessLevels.Blacklisted)
            {
                await ReplyAsync("", false,
                    GetEmbeddedMessage("Wait... That's Illegal.", "You can't create a blacklisted command...", Color.DarkRed));
                return;
            }
            ulong gid = 0;
            if (Context.Guild != null)
            {
                gid = Context.Guild.Id;
            }
            if (gid == 0)
            {
                if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)//global commands require administrator priv.
                {
                    await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                    return;
                }
            }
            await DiscordNet.CustomCMDMgr.AddCmd(Context.Message, cmdname, action, CommandAccessLevel > AccessLevels.Normal,CommandAccessLevel, gid);
        }

        [Command("addgcmd"), Summary("Add a global command to your bot"),Remarks("AccessLevels.Administrator")]
        public async Task CMD_AddGlobal(string cmdname, bool restricted, [Remainder]string action)
        {
            
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context,AccessLevels.Administrator));
                return;
            }
            if (string.IsNullOrWhiteSpace(cmdname))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's illegal.", $"Please give a valid command name.", Color.DarkRed));
                return;
            }
            await DiscordNet.CustomCMDMgr.AddCmd(Context.Message, cmdname, action, restricted);
        }

        [Command("addgcmd"), Summary("Add a global command to your bot"), Remarks("AccessLevels.Administrator")]
        public async Task CMD_AddGlobal(string cmdname, AccessLevels CommandAccessLevel, [Remainder]string action)
        {
            
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            if (string.IsNullOrWhiteSpace(cmdname))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's illegal.", $"Please give a valid command name.", Color.DarkRed));
                return;
            }
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < CommandAccessLevel)
            {
                await ReplyAsync("", false,
                    GetEmbeddedMessage("Wait... That's Illegal.", "You can't create a command you won't have permission to use.", Color.DarkRed));
                return;
            }
            if (CommandAccessLevel == AccessLevels.Blacklisted)
            {
                await ReplyAsync("", false,
                    GetEmbeddedMessage("Wait... That's Illegal.", "You can't create a blacklisted command...", Color.DarkRed));
                return;
            }
            await DiscordNet.CustomCMDMgr.AddCmd(Context.Message, cmdname, action, CommandAccessLevel > AccessLevels.Normal,CommandAccessLevel);
        }

        [Command("delcmd"), Summary("delete a command from your bot. If you run this via DM, it will delete a global command (AccessLevels.Administrator)."), Remarks("AccessLevels.CommandManager")]
        public async Task CMD_Delete(string cmdname)
        {
            
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            if (string.IsNullOrWhiteSpace(cmdname))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's illegal.", $"Please give a valid command name.", Color.DarkRed));
                return;
            }
            ulong gid = 0;
            if (Context.Guild != null)
            {
                gid = Context.Guild.Id;
            }
            if (gid == 0)
            {
                if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)//global commands require administrator priv.
                {
                    await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                    return;
                }
            }
            await DiscordNet.CustomCMDMgr.DelCmd(Context.Message, cmdname, gid);
        }

        [Command("delgcmd"), Summary("delete a global command from your bot."), Remarks("AccessLevels.Administrator")]
        public async Task CMD_DeleteGlobal(string cmdname)
        {
            if (string.IsNullOrWhiteSpace(cmdname))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's illegal.", $"Please give a valid command name.", Color.DarkRed));
                return;
            }
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            await DiscordNet.CustomCMDMgr.DelCmd(Context.Message, cmdname);
        }

        [Command("getcmd"), Summary("View information about a specific command"),Remarks("AccessLevels.CommandManager")]
        public async Task CMD_Get(string cmdname)
        {
            if(DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            await Context.Channel.SendMessageAsync("", false, DiscordNet.CustomCMDMgr.ViewCmd(Context, cmdname));
        }

        [Command("listcmd-html"), Summary("Lists all available commands for current context. sends as HTML file"), Remarks("AccessLevels.Normal")]
        public async Task CMD_ListCommands()
        {
            ulong gid = 0;
            CommandList commandList = new CommandList(Client.CurrentUser.Username,Context.Guild?.Name ?? "Direct Messages");
            string prefix = DiscordNet.serviceProvider.GetRequiredService<Configuration>().CommandPrefix;
            if (Context.Guild != null)
            {
                GuildObject obj = DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);
                if (obj != null) prefix = obj.CommandPrefix;
                gid = Context.Guild.Id;
            }

            #region CORE & Modules.
            foreach (CommandInfo item in Cmdsvr.Commands)
            {
                #region Per-guild module Check
                string module = item.Module.Name;
                var m = DiscordNet.ModuleMgr.Modules.FirstOrDefault(x => x.ModuleGroups.Contains(module));
                if (m != null)
                {
                    if (m.GuildsAvailable.Count > 0)
                    {
                        if (m.GuildsAvailable.FirstOrDefault(ggid => ggid == gid) == 0)//if no match for guild, don't populate.
                        {
                            ConsoleIO.WriteEntry(new LogMessage(LogSeverity.Verbose, "Listcmd-HTML", $"{item.Name}: {module} exists, but not for guild ID: {gid}."));
                            continue;
                        }
                    }
                }
                #endregion

                string group = item.Module.Aliases[0] + " ";
                string sum = item.Summary;
                if (string.IsNullOrWhiteSpace(group))
                {
                    group = "";//Command's groupAttribute?
                }

                string usage = prefix + group + item.Name + " ";
                foreach (var param in item.Parameters)
                {
                    if (param.IsOptional)
                    {
                        usage += $"[{param.Type.Name} {param.Name}] ";
                    }
                    if (!param.IsOptional)
                    {
                        usage += $"<{param.Type.Name} {param.Name}> ";
                    }
                }
                AccessLevels? lv = null;
                if (item.Remarks != null)
                {
                    lv = (AccessLevels)Enum.Parse(typeof(AccessLevels), item.Remarks.Replace("AccessLevels.", ""));
                }
                commandList.AddCommand(prefix + group + item.Name, lv, item.Module.Name == "CoreModule" ? CommandTypes.Core : CommandTypes.Module, sum, usage);
            }

            #endregion

            #region Global & guild commands
            GuildObject globalGO = DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == 0);
            if (globalGO != null)
            {
                foreach (GuildCommand item in globalGO.GuildCommands)
                {
                    commandList.AddCommand(prefix + item.Name, item.RequirePermission ? item.CommandAccessLevel : AccessLevels.Normal, CommandTypes.GCustom);
                }
            }

            if (Context.Guild != null)
            {
                GuildObject currentGuild = DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);
                if (currentGuild != null)
                {
                    foreach (GuildCommand item in currentGuild.GuildCommands)
                    {
                        commandList.AddCommand(prefix + item.Name, item.RequirePermission ? item.CommandAccessLevel : AccessLevels.Normal, CommandTypes.Custom);
                    }
                }
            }

            #endregion

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (StreamWriter sw = new StreamWriter(ms))
                    {
                        sw.WriteLine(commandList.GetFullHTML());
                        sw.Flush();
                        ms.Position = 0;
                        await Context.Channel.SendMessageAsync("**See the attached web document for a full list of commands.**", false);
                        if (Context.Guild != null)
                        {
                            await Context.Channel.SendFileAsync(ms, $"{Context.Client.CurrentUser.Username}_{Context.Guild.Name}_CommandList_{DateTime.Now.ToFileTimeUtc()}.html");
                        }
                        else
                        {
                            await Context.Channel.SendFileAsync(ms, $"{Context.Client.CurrentUser.Username}_GLOBAL_CommandList_{DateTime.Now.ToFileTimeUtc()}.html");
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithAuthor(Client.CurrentUser);
                b.WithTitle("Something Went Wrong!");
                b.WithDescription(ex.Message);
                b.WithFooter("ModularBOT • CORE");
                b.WithColor(Color.Red);
                await Context.Channel.SendMessageAsync("", false, b.Build());
            }
        }

        [Command("listcmd",RunMode = RunMode.Async), Summary("Lists all available commands for current context."), Remarks("AccessLevels.Normal")]
        public async Task CMD_ListPaginator()
        {

            ulong gid = 0;
            string prefix = DiscordNet.serviceProvider.GetRequiredService<Configuration>().CommandPrefix;
            if (Context.Guild != null)
            {
                GuildObject obj = DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);
                gid = Context.Guild.Id;
                if (obj != null) prefix = obj.CommandPrefix;
            }

            List<PaginatedMessage.Page> pages = new List<PaginatedMessage.Page>();
            await ReplyAsync(Context.User.Mention + $", Here's a list of commands! If you are unable to navigate the list, please do `{prefix}listcmd-html` instead.");
            int cmdcount = 0;//comdcount
            int pageNum = 1;
            PaginatedMessage.Page PageItem = new PaginatedMessage.Page
            {
                Title = $"Core Commands",
                Color = Color.Green,
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{Context.Client.CurrentUser.Username}'s Command List",
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
                }
            };

            #region CORE
            foreach (CommandInfo item in Cmdsvr.Commands)
            {
                if(item.Module.Name != "CoreModule")
                {
                    continue;
                }
                if (cmdcount > 8)
                {
                    cmdcount = 0;
                    pageNum++;
                    pages.Add(PageItem);
                    PageItem = new PaginatedMessage.Page
                    {
                        Title = $"Core Commands",
                        Color = Color.Green,
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = $"{Context.Client.CurrentUser.Username}'s Command List",
                            IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
                        }
                    };//start a new page.
                }
                string group = item.Module.Aliases[0] + " ";
                string sum = item.Summary ?? "No summary provided";
                if (string.IsNullOrWhiteSpace(group))
                {
                    group = "";//Command's groupAttribute?
                }

                string usage = prefix + group + item.Name + " ";
                foreach (var param in item.Parameters)
                {
                    if (param.IsOptional)
                    {
                        usage += $"[{param.Type.Name} {param.Name}] ";
                    }
                    if (!param.IsOptional)
                    {
                        usage += $"<{param.Type.Name} {param.Name}> ";
                    }
                }
                AccessLevels? lv = null;
                if (item.Remarks != null)
                {
                    lv = (AccessLevels)Enum.Parse(typeof(AccessLevels), item.Remarks.Replace("AccessLevels.", ""));
                }
                cmdcount++;
                EmbedFieldBuilder fb = new EmbedFieldBuilder()
                {
                    Name = "💠 " + prefix + group + item.Name,
                    Value = $"**{sum}**" + "\r\n" +
                            $"```\r\n• Access Level: {item.Remarks ?? "Not properly annotated in remark."}" + "\r\n" +
                            $"• Usage: {usage}\r\n```"

                };
                PageItem.Fields.Add(fb);
                //commandList.AddCommand(prefix + group + item.Name, lv, item.Module.Name == "CoreModule" ? CommandTypes.Core : CommandTypes.Module, sum, usage);
            }

            #endregion


            if (PageItem.Fields.Count > 0)
            {
                pages.Add(PageItem);//add the page if it has more than 0 commands still
            }

            #region Modules
            cmdcount = 0;
            PageItem = new PaginatedMessage.Page
            {
                Title = $"External Module Commands",
                Color = Color.Blue,
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{Context.Client.CurrentUser.Username}'s Command List",
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
                }
            };
            foreach (CommandInfo item in Cmdsvr.Commands)
            {
                if (item.Module.Name == "CoreModule")
                {
                    continue;
                }

                #region Per-guild module Check
                string module = item.Module.Name;
                var m = DiscordNet.ModuleMgr.Modules.FirstOrDefault(x => x.ModuleGroups.Contains(module));
                if (m != null)
                {
                    if (m.GuildsAvailable.Count > 0)
                    {
                        if (m.GuildsAvailable.FirstOrDefault(ggid => ggid == gid) == 0)//if no match for guild, don't populate.
                        {
                            ConsoleIO.WriteEntry(new LogMessage(LogSeverity.Verbose, "Listcmd", $"{module} exists, but not for guild ID: {gid}."));
                            continue;
                        }
                    }
                }
                #endregion

                if (cmdcount > 8)
                {
                    cmdcount = 0;
                    pageNum++;
                    pages.Add(PageItem);
                    PageItem = new PaginatedMessage.Page
                    {
                        Title = $"External Module Commands",
                        Color = Color.Blue,
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = $"{Context.Client.CurrentUser.Username}'s Command List",
                            IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
                        }
                    };//start a new page.
                }
                string group = item.Module.Aliases[0] + " ";
                string sum = item.Summary ?? "No summary provided";

                if (string.IsNullOrWhiteSpace(group))
                {
                    group = "";//Command's groupAttribute?
                }

                string usage = prefix + group + item.Name + " ";
                foreach (var param in item.Parameters)
                {
                    if (param.IsOptional)
                    {
                        usage += $"[{param.Type.Name} {param.Name}] ";
                    }
                    if (!param.IsOptional)
                    {
                        usage += $"<{param.Type.Name} {param.Name}> ";
                    }
                }
                AccessLevels? lv = null;
                if (item.Remarks != null)
                {
                    lv = (AccessLevels)Enum.Parse(typeof(AccessLevels), item.Remarks.Replace("AccessLevels.", ""));
                }
                cmdcount++;
                EmbedFieldBuilder fb = new EmbedFieldBuilder()
                {
                    Name = $"📦 " + prefix + group + item.Name,
                    Value = $"**{sum}**" + "\r\n" +
                            $"```\r\n• Access Level: {item.Remarks ?? "Not properly annotated in remark."}" + "\r\n" +
                            $"• Usage: {usage}\r\n" +
                            $"• Module: {item.Module.Name}\r\n```"

                };
                PageItem.Fields.Add(fb);
                //commandList.AddCommand(prefix + group + item.Name, lv, item.Module.Name == "CoreModule" ? CommandTypes.Core : CommandTypes.Module, sum, usage);
            }

            #endregion

            if (PageItem.Fields.Count > 0)
            {

                pages.Add(PageItem);//add the page if it has more than 0 commands still
            }

            #region Custom Commands
            cmdcount = 0;
            PageItem = new PaginatedMessage.Page
            {
                Title = $"Custom Commands",
                Color = Color.Purple,
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{Context.Client.CurrentUser.Username}'s Command List",
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
                }
            };
            //GLOBAL

            GuildObject globalGO = DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == 0);
            if (globalGO != null)
            {
                foreach (GuildCommand item in globalGO.GuildCommands)
                {
                    if (cmdcount > 8)
                    {
                        cmdcount = 0;
                        pageNum++;
                        pages.Add(PageItem);
                        PageItem = new PaginatedMessage.Page
                        {
                            Title = $"Custom Commands",
                            Color = Color.Purple,
                            Author = new EmbedAuthorBuilder()
                            {
                                Name = $"{Context.Client.CurrentUser.Username}'s Command List",
                                IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
                            }
                        };//start a new page.
                    }
                    //commandList.AddCommand(prefix + item.Name, item.RequirePermission ? AccessLevels.CommandManager : AccessLevels.Normal, CommandTypes.GCustom);
                    cmdcount++;
                    string perm = item.RequirePermission ? $"AccessLevels.{item.CommandAccessLevel}" : "AccessLevels.Normal";
                    EmbedFieldBuilder fb = new EmbedFieldBuilder()
                    {
                        Name = $"🌐 " + prefix + item.Name,
                        
                        Value = $"```\r\n• Required Access Level: {perm}" + "\r\n```"

                    };
                    PageItem.Fields.Add(fb);
                }
            }
            //Guild
            if (Context.Guild != null)
            {
                GuildObject currentGuild = DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);
                if (currentGuild != null)
                {
                    foreach (GuildCommand item in currentGuild.GuildCommands)
                    {
                        if (cmdcount > 8)
                        {
                            cmdcount = 0;
                            pageNum++;
                            pages.Add(PageItem);
                            PageItem = new PaginatedMessage.Page
                            {
                                Title = $"Custom Commands",
                                Color = Color.Purple,
                                Author = new EmbedAuthorBuilder()
                                {
                                    Name = $"{Context.Client.CurrentUser.Username}'s Command List",
                                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
                                }
                            };//start a new page.
                        }
                        //commandList.AddCommand(prefix + item.Name, item.RequirePermission ? AccessLevels.CommandManager : AccessLevels.Normal, CommandTypes.Custom);
                        cmdcount++;
                        string perm = item.RequirePermission ? $"AccessLevels.{item.CommandAccessLevel}" : "AccessLevels.Normal";
                        EmbedFieldBuilder fb = new EmbedFieldBuilder()
                        {
                            Name = prefix + item.Name,

                            Value = $"```\r\n• Required Access Level: {perm}" + "\r\n```"

                        };
                        PageItem.Fields.Add(fb);
                    }
                }
            }

            #endregion

            if (PageItem.Fields.Count > 0)
            {

                pages.Add(PageItem);//add the page if it has more than 0 commands still
            }

            var pager = new PaginatedMessage
            {
                Pages = pages,
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                    Name = Context.Client.CurrentUser.ToString(),
                    Url = Context.Client.CurrentUser.GetAvatarUrl()
                },
                Color = Color.DarkGreen,
                Options = PaginatedAppearanceOptions.Default,
                TimeStamp = DateTimeOffset.UtcNow
            };

            
            await PagedReplyAsync(pager, new ReactionList
            {
                Forward = true,
                Backward = true,
                Jump = false,
                Trash = true
            });

        }

        [Command("editcmd"), Summary("Edit a command. Note: Global command edits require AccessLevels.Administrator"), Remarks("AccessLevels.CommandManager")]
        public async Task CMD_EditCommand(string cmdName, bool? requirePermission=null, [Remainder]string newAction = "(unchanged)")
        {
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }

            ulong gid = Context.Guild?.Id ?? 0;
            if(gid == 0)
            {
                if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
                {
                    await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                    return;
                }
            }
            await DiscordNet.CustomCMDMgr.EditCMD(Context, cmdName, requirePermission, newAction, gid);

        }

        [Command("editcmd"), Summary("Edit a command. Note: Global command edits require AccessLevels.Administrator"), Remarks("AccessLevels.CommandManager")]
        public async Task CMD_EditCommand(string cmdName, AccessLevels CommandAccessLevel, [Remainder]string newAction = "(unchanged)")
        {
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < CommandAccessLevel)
            {
                await ReplyAsync("", false,
                    GetEmbeddedMessage("Wait... That's Illegal.", "You can't modify a command you don't have permission to use.", Color.DarkRed));
                return;
            }
            if (CommandAccessLevel == AccessLevels.Blacklisted)
            {
                await ReplyAsync("", false,
                    GetEmbeddedMessage("Wait... That's Illegal.", "You can't create a blacklisted command...", Color.DarkRed));
                return;
            }
            ulong gid = Context.Guild?.Id ?? 0;
            if (gid == 0)
            {
                if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
                {
                    await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                    return;
                }
            }
            await DiscordNet.CustomCMDMgr.EditCMD(Context, cmdName,CommandAccessLevel, newAction, gid);

        }

        #endregion

        #region Permission Management
        [Command("permissions set user"),Alias("psu"), Remarks("AccessLevels.Administrator"),Summary("Set a user's access level. NOTE: This will grant said user said access level in ALL guilds.")]
        public async Task PERM_SetUser(IUser user, AccessLevels accessLevel)
        {
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            if(user.IsBot)
            {

                if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
                {
                    await Context.Channel.SendMessageAsync("This user is a bot...", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                    return;
                }
            }
            if (user == Context.User)
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.","You can't change your own access level.",Color.DarkRed));
                return;
            }
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < accessLevel)
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't give someone a higher access level than your own.", Color.DarkRed));
                return;
            }
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < DiscordNet.PermissionManager.GetAccessLevel(user))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't change a user who has a higher access level than your own.", Color.DarkRed));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            try
            {
                int result = DiscordNet.PermissionManager.RegisterEntity(user, accessLevel);
                switch (result)
                {
                    case (1):
                        b.WithAuthor(Client.CurrentUser);
                        
                        b.WithTitle("Permission Manager");
                        b.WithDescription("The user was successfully added to the permissions file.");
                        b.WithColor(Color.Green);
                        if (accessLevel == AccessLevels.Blacklisted)
                        {
                            b.WithColor(new Color(255, 255, 0));
                            b.AddField("Warning", "You have added this user to the blacklisted access level. They will not be able to interact or run commands.");
                        }
                        b.AddField("User", $"`{user.Username}#{user.Discriminator}`", true);
                        b.AddField("AccessLevel", $"`{accessLevel}`", true);
                        b.WithFooter("ModularBOT • Core");
                        
                        
                        break;
                    case (2):
                        b.WithAuthor(Client.CurrentUser);
                        b.WithTitle("Permission Manager");
                        b.WithDescription("The user was successfully updated.");
                        b.WithColor(Color.Green);
                        if (accessLevel == AccessLevels.Blacklisted)
                        {
                            b.WithColor(new Color(255, 255, 0));
                            b.AddField("Warning", "You have moved this user to the blacklisted access level. They will not be able to interact or run commands.");
                        }
                        b.AddField("User", $"`{user.Username}#{user.Discriminator}`", true);
                        b.AddField("AccessLevel", $"`{accessLevel}`", true);
                        b.WithFooter("ModularBOT • Core");
                        
                        break;
                    default:
                        b.WithAuthor(Client.CurrentUser);
                        b.WithTitle("Permission Manager");
                        b.WithDescription("Permission manager did not make any changes. The user may already have that access level.");
                        b.WithFooter("ModularBOT • Core");
                        b.WithColor(Color.Orange);
                        break;
                }
            }
            catch (InvalidOperationException ex)
            {
                b.WithAuthor(Client.CurrentUser);
                b.WithTitle("Permission Manager");
                b.WithDescription("Permission manager failed to make desired changes, due to an error.");
                b.AddField("More Details", $"{ex.Message}", true);
                b.WithFooter("ModularBOT • Core");
                b.WithColor(Color.Red);
            }
            

            
            
            await Context.Channel.SendMessageAsync("", false, b.Build());
        }

        [Command("permissions set entity"), Alias("pse"), Remarks("AccessLevels.Administrator"), Summary("Set permissions for a generic ID. Note: if entity is user/bot/webhook, it will be set for ALL guilds.")]
        public async Task PERM_SetEntity(ulong GenericID, AccessLevels accessLevel)
        {
            #region RequiredCheck
            IRole role = null;
            IGuildUser user = null;
            if (Context.Guild != null)
            {
                role = Context.Guild?.GetRole(GenericID);
                if (role == null)
                {
                    user = Context.Guild?.GetUserAsync(GenericID,CacheMode.AllowDownload).GetAwaiter().GetResult();
                    if (user == null)
                    {
                        IWebhook hook = Context.Guild?.GetWebhookAsync(GenericID).GetAwaiter().GetResult();
                        if (hook==null)
                        {
                            await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... What?",
                                "The entity id did not match a user or role. Please make sure you got it right!", Color.DarkRed));
                            return;
                        }
                    }
                }
            }
            #endregion

            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            if (GenericID == Context.User.Id)
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't change your own access level.", Color.DarkRed));
                return;
            }
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < accessLevel)
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't give anyone a higher access level than your own.", Color.DarkRed));
                return;
            }
            if(role != null)
            {
                if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < DiscordNet.PermissionManager.GetAccessLevel(role))
                {
                    await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't change a role who has a higher access level than your own.", Color.DarkRed));
                    return;
                }
            }
            if (user != null)
            {
                if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < DiscordNet.PermissionManager.GetAccessLevel(user))
                {
                    await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't change a user who has a higher access level than your own.", Color.DarkRed));
                    return;
                }
            }
            EmbedBuilder b = new EmbedBuilder();
            try
            {
                int result = DiscordNet.PermissionManager.RegisterEntity(Context,GenericID, accessLevel);
                switch (result)
                {
                    case (1):
                        b.WithAuthor(Client.CurrentUser);

                        b.WithTitle("Permission Manager");
                        b.WithDescription("The user was successfully added to the permissions file.");
                        b.WithColor(Color.Green);
                        if (accessLevel == AccessLevels.Blacklisted)
                        {
                            b.WithColor(new Color(255, 255, 0));
                            b.AddField("Warning", "You have added this user to the blacklisted access level. They will not be able to interact or run commands.");
                        }
                        if (user != null) { b.AddField("User", $"`{user.Username}#{user.Discriminator}`", true); }
                        if (role != null) { b.AddField("Role", $"`{role.Name}`", true); }
                        b.AddField("AccessLevel", $"`{accessLevel}`", true);
                        b.WithFooter("ModularBOT • Core");


                        break;
                    case (2):
                        b.WithAuthor(Client.CurrentUser);
                        b.WithTitle("Permission Manager");
                        b.WithDescription("The user was successfully updated.");
                        b.WithColor(Color.Green);
                        if (accessLevel == AccessLevels.Blacklisted)
                        {
                            b.WithColor(new Color(255, 255, 0));
                            b.AddField("Warning", "You have moved this user to the blacklisted access level. They will not be able to interact or run commands.");
                        }
                        if(user != null) { b.AddField("User", $"`{user.Username}#{user.Discriminator}`", true); }
                        if (role != null) { b.AddField("Role", $"`{role.Name}`", true); }
                        b.AddField("AccessLevel", $"`{accessLevel}`", true);
                        b.WithFooter("ModularBOT • Core");

                        break;
                    default:
                        b.WithAuthor(Client.CurrentUser);
                        b.WithTitle("Permission Manager");
                        b.WithDescription("Permission manager did not make any changes. The user may already have that access level.");
                        b.WithFooter("ModularBOT • Core");
                        b.WithColor(Color.Orange);
                        break;
                }
            }
            catch (InvalidCastException ex)
            {
                b.WithAuthor(Client.CurrentUser);
                b.WithTitle("Permission Manager");
                b.WithDescription("Permission manager failed to make desired changes, due to an invalid type.");
                b.AddField("More Details", $"{ex.Message}", true);
                b.WithFooter("ModularBOT • Core");
                b.WithColor(Color.Red);
            }
            catch (InvalidOperationException ex)
            {
                b.WithAuthor(Client.CurrentUser);
                b.WithTitle("Permission Manager");
                b.WithDescription("Permission manager failed to make desired changes, due to an invalid operation.");
                b.AddField("More Details", $"{ex.Message}", true);
                b.WithFooter("ModularBOT • Core");
                b.WithColor(Color.Red);
            }
            catch (NullReferenceException ex)
            {
                b.WithAuthor(Client.CurrentUser);
                b.WithTitle("Permission Manager");
                b.WithDescription("Permission manager failed to make desired changes, due to an invalid operation.");
                b.AddField("More Details", $"{ex}", true);
                b.WithFooter("ModularBOT • Core");
                b.WithColor(Color.Red);
            }


            await Context.Channel.SendMessageAsync("", false, b.Build());
        }

        [Command("permissions set role"),RequireContext(ContextType.Guild), Alias("psr"), Remarks("AccessLevels.CommandManager"), Summary("Set permissions for a role")]
        public async Task PERM_SetRole(IRole role, AccessLevels accessLevel)
        {
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            try
            {
                int result = DiscordNet.PermissionManager.RegisterEntity(role, accessLevel);
                switch (result)
                {
                    case (1):
                        b.WithAuthor(Client.CurrentUser);
                        b.WithTitle("Permission Manager");
                        b.WithDescription("The role was successfully added to the permissions file.");
                        b.AddField("Role", $"`{role.Name} ({role.Guild.Name})`", true);
                        b.AddField("AccessLevel", $"`{accessLevel}`", true);
                        b.WithFooter("ModularBOT • Core");
                        b.WithColor(Color.Green);
                        
                        break;
                    case (2):
                        b.WithAuthor(Client.CurrentUser);
                        b.WithTitle("Permission Manager");
                        b.WithDescription("The role was successfully updated.");
                        b.AddField("Role", $"`{role.Name} ({role.Guild.Name})`", true);
                        b.AddField("AccessLevel", $"`{accessLevel}`", true);
                        b.WithFooter("ModularBOT • Core");
                        b.WithColor(Color.Green);
                        break;
                    default:
                        b.WithAuthor(Client.CurrentUser);
                        b.WithTitle("Permission Manager");
                        b.WithDescription("Permission manager did not make any changes. The user may already have that access level.");
                        b.WithFooter("ModularBOT • Core");
                        b.WithColor(Color.Orange);
                        break;
                }
            }
            catch (InvalidOperationException ex)
            {
                b.WithAuthor(Client.CurrentUser);
                b.WithTitle("Permission Manager");
                b.WithDescription($"{ex.Message}");
                b.WithFooter("ModularBOT • Core");
                b.WithColor(Color.Red);
            }




            await Context.Channel.SendMessageAsync("", false, b.Build());
        }

        [Command("permissions del user"), Alias("pdu","pru"), Remarks("AccessLevels.Administrator"), Summary("Remove permission entry for user. (Assumes default: AccessLevels.Normal)")]
        public async Task PERM_DeleteUser(IUser user)
        {
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            if (user == Context.User)
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't remove yourself from the permission system.", Color.DarkRed));
                return;
            }
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < DiscordNet.PermissionManager.GetAccessLevel(user))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't remove a user from the system if they have a higher access level than you...", Color.DarkRed));
                return;
            }
            EmbedBuilder b = new EmbedBuilder();
            try
            {
                bool result = DiscordNet.PermissionManager.DeleteEntity(user);
                switch (result)
                {
                    case (true):
                        b.WithAuthor(Client.CurrentUser);
                        b.WithTitle("Permission Manager");
                        b.WithDescription("The user was successfully removed from the permissions file. User will inherit permissions from any registered roles. Otherwise, they'll be treated as `AccessLevels.Normal`");
                        b.AddField("Affected User", $"`{user.Username}#{user.Discriminator}`", true);
                        b.WithFooter("ModularBOT • Core");
                        b.WithColor(Color.Green);
                        break;
                    case (false):
                        b.WithAuthor(Client.CurrentUser);
                        b.WithTitle("Permission Manager");
                        b.WithDescription("This user was not found in the permissions file.");
                        b.WithFooter("ModularBOT • Core");
                        b.WithColor(Color.Red);
                        break;
                }
            }
            catch (InvalidOperationException ex)
            {
                b.WithAuthor(Client.CurrentUser);
                b.WithTitle("Permission Manager");
                b.WithDescription($"{ex.Message}");
                b.WithFooter("ModularBOT • Core");
                b.WithColor(Color.Red);
            }




            await Context.Channel.SendMessageAsync("", false, b.Build());
        }

        [Command("permissions del role"), Alias("pdr", "prr"), Remarks("AccessLevels.CommandManager"), Summary("Remove permission entry for role. (Assumes default: AccessLevels.Normal)")]
        public async Task PERM_DeleteRole(IRole role)
        {
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < DiscordNet.PermissionManager.GetAccessLevel(role))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't remove a role from the system if they have a higher access level than you...", Color.DarkRed));
                return;
            }
            EmbedBuilder b = new EmbedBuilder();
            try
            {
                bool result = DiscordNet.PermissionManager.DeleteEntity(role);
                if (result)
                {
                    b.WithAuthor(Client.CurrentUser);
                    b.WithTitle("Permission Manager");
                    b.WithDescription("The role was successfully removed from the permissions file.");
                    b.AddField("Affected Role", $"`{role.Name} ({role.Guild.Name})`", true);
                    b.WithFooter("ModularBOT • Core");
                    b.WithColor(Color.Green);
                }
                else
                {
                    b.WithAuthor(Client.CurrentUser);
                    b.WithTitle("Permission Manager");
                    b.WithDescription("This role was not found in the permissions file.");
                    b.WithFooter("ModularBOT • Core");
                    b.WithColor(Color.Red);
                }
            }
            catch (InvalidOperationException ex)
            {
                b.WithAuthor(Client.CurrentUser);
                b.WithTitle("Permission Manager");
                b.WithDescription($"{ex.Message}");
                b.WithFooter("ModularBOT • Core");
                b.WithColor(Color.Red);
            }

            await Context.Channel.SendMessageAsync("", false, b.Build());
        }

        [Command("permissions get"),Alias("plist"),Remarks("AccessLevels.CommandManager"), Summary("List permissions file."),RequireContext(ContextType.Guild)]
        public async Task PERM_ListPermissions(IUser user)
        {
            if(DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            AccessLevels l = DiscordNet.PermissionManager.GetAccessLevel(user, out IRole inheritedRole, out bool BotOwner, out bool InList);
            EmbedBuilder b = new EmbedBuilder();
            b.WithColor(Color.Blue);
            b.WithAuthor(Client.CurrentUser);
            b.WithTitle($"User permissions for {user.Username}#{user.Discriminator}");
            string inheritedPerms = inheritedRole == null ? "not inherit an access level from a role" : "inherit an access level from a role";
            string sep = "";
            if(InList && inheritedRole == null)
            {
                sep = " and does";
            }
            if (InList && inheritedRole != null)
            {
                sep = ", but also";
            }
            if (!InList && inheritedRole != null)
            {
                sep = ", but they";
            }
            if (!InList && inheritedRole == null)
            {
                sep = ", and they do";
            }
            string inListed = InList ? "is registered globally" : "is not registered globally";
            string ownerstring = BotOwner ? "This user is bot owner." : "This user isn't bot owner.";
            b.WithDescription($"This user {inListed}{sep} {inheritedPerms}. {ownerstring}");

            b.AddField("Access Level", $"`{l}`",true);
            b.WithThumbnailUrl(user.GetAvatarUrl(ImageFormat.Auto));
            b.WithFooter("ModularBOT • Core");
            if(inheritedRole != null)
            {
                b.AddField("Inherited from", $"{inheritedRole.Name} (`{inheritedRole.Guild}`)", true);
            }
            await Context.Channel.SendMessageAsync("", false, b.Build());
        }

        #endregion

        #region Bot management
        [Command("stopbot",RunMode= RunMode.Async), Alias("stop"), Remarks("AccessLevels.Administrator"), Summary("Calls for termination of session, and closes program.")]
        public async Task BOT_StopBot()
        {
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Shutting Down...", "Administrator called for application termination. Ending session...", Color.DarkBlue));
            DiscordNet.Stop(ref Program.ShutdownCalled);
        }

        [Command("restartbot", RunMode = RunMode.Async),Alias("restart"), Remarks("AccessLevels.Administrator"), Summary("Calls for termination of session, and restarts program.")]
        public async Task BOT_RestartBot()
        {
            if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Restarting...", "Administrator called for application restart. Ending session...", Color.DarkBlue));

            Program.RestartRequested = true;
            await Task.Run(() =>DiscordNet.Stop(ref Program.ShutdownCalled));
            
        }

        [Command("status"), Remarks("AccessLevels.Administrator"), Summary("Sets status text for bot user. Start with playing, watching, listening to, or streaming.")]
        public async Task BOT_SetStatus(string text, string StreamURL="")
        {
            
            if(DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            if(text.ToLower().StartsWith("playing "))
            {
                await DiscordNet.Client.SetGameAsync(text.Remove(0,8),null);
                await ReplyAsync(null, false, GetEmbeddedMessage("Success!", $"Bot's status changed to: **{text}**", Color.Green));
            }
            if (text.ToLower().StartsWith("watching "))
            {
                await DiscordNet.Client.SetGameAsync(text.Remove(0, 9), null,ActivityType.Watching);
                await ReplyAsync(null, false, GetEmbeddedMessage("Success!", $"Bot's status changed to: **{text}**", Color.Green));
            }
            if (text.ToLower().StartsWith("streaming "))
            {
                await DiscordNet.Client.SetGameAsync(text.Remove(0, 10), StreamURL, ActivityType.Streaming);
                await ReplyAsync(null, false, GetEmbeddedMessage("Success!", $"Bot's status changed to: **{text}**", Color.Green));
            }
            if (text.ToLower().StartsWith("listening to "))
            {
                await DiscordNet.Client.SetGameAsync(text.Remove(0, 13), null, ActivityType.Listening);
                await ReplyAsync(null, false, GetEmbeddedMessage("Success!", $"Bot's status changed to: **{text}**", Color.Green));
            }
        }

        [Command("prefix"), Remarks("AccessLevels.CommandManager"), Summary("Set the prefix for current guild, or if called from Direct message, set global prefix.")]
        public async Task BOT_SetPrefix(string newPrefix="")
        {
            
            if(newPrefix == "")
            {
                ulong pgid = 0;

                if (Context.Guild != null)
                {
                    pgid = Context.Guild.Id;
                }
                var pg = await Context.Client.GetGuildAsync(pgid);
                GuildObject pobj = DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == pgid);
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Current Prefix", $"The current prefix for `{pg?.Name ?? "Direct Messages"}` is `{pobj?.CommandPrefix ?? DiscordNet.serviceProvider.GetRequiredService<Configuration>().CommandPrefix}`", new Color(0,255,0)));
                return;
            }
            if(string.IsNullOrWhiteSpace(newPrefix) || newPrefix.Contains('`'))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Invalid prefix","Your prefix must not start with whitespace, or contain invalid characters!",Color.Red));
                return;
            }
            ulong gid = 0;
            
            if(Context.Guild != null)
            {
                gid = Context.Guild.Id;
            }
            if(!string.IsNullOrWhiteSpace(newPrefix))
            {
                if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
                {
                    await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                    return;
                }
                GuildObject pobj = DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == gid);
                if(pobj !=null)
                {
                    if (pobj.LockPFChanges)
                    {
                        if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) == AccessLevels.CommandManager)
                        {
                            if (Context.User is SocketGuildUser sgu)
                            {
                                if (!sgu.GuildPermissions.Has(GuildPermission.ManageGuild))
                                {
                                    await ReplyAsync("", false, GetEmbeddedMessage("DENIED!",
                                        "This guild has their prefix locked. You must have `AccessLevels.Administrator`. Otherwise, you must have `AccessLevels.CommandManager` AND `Manage Server` Permissions.", Color.DarkRed));
                                    return;
                                }
                            }
                        }
                    }
                }
                
               
            }
            if(gid == 0)
            {
                if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
                {
                    await Context.Channel.SendMessageAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                    return;
                }
                DiscordNet.serviceProvider.GetRequiredService<Configuration>().CommandPrefix = newPrefix;
                Program.configMGR.Save();
                
            }
            GuildObject obj = DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == gid);
            if(obj!=null)
            {
                
                obj.CommandPrefix = newPrefix;
                obj.SaveData();
                var g = await Context.Client.GetGuildAsync(gid);
                ConsoleIO.WriteEntry(new LogMessage(LogSeverity.Warning, "Prefix", $"The prefix for `{g?.Name ?? "Direct Messages"}` has been set to `{newPrefix}`"), ConsoleColor.Cyan);
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Success!", $"The prefix for `{g?.Name ?? "Direct Messages"}` has been set to `{newPrefix}`", Color.Green));
                return;
            }
            else
            {
                obj = new GuildObject
                {
                    CommandPrefix = newPrefix,
                    GuildCommands = new List<GuildCommand>(),
                    ID = gid

                };
                try
                {
                    DiscordNet.CustomCMDMgr.AddGuildObject(obj);//safely inject the new object.
                    var g = await Context.Client.GetGuildAsync(gid);
                    ConsoleIO.WriteEntry(new LogMessage(LogSeverity.Warning, "Prefix", $"The prefix for `{g?.Name ?? "Direct Messages"}` has been set to `{newPrefix}`"), ConsoleColor.Cyan);
                    await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Success!", $"The prefix for `{g.Name}` has been set to `{newPrefix}`", Color.Green));

                }
                catch (Exception ex)
                {
                    ConsoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Prefix", $"Something went wrong... {ex.Message}",ex), ConsoleColor.Cyan);
                    await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Something Went Wrong.", "A new guild object was needed here, but couldn't be created.", Color.DarkRed, ex));
                }

            }
        }

        [Command("uptime"),Remarks("AccessLevels.Normal"), Summary("show how long bot has been connected.")]
        public async Task BOT_ShowUptime([Remainder] string args=null)
        {
            var delta = DateTime.Now - DiscordNet.ClientStartTime;
            var deltb = DateTime.Now - DiscordNet.InstanceStartTime;

            EmbedBuilder b = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto, 128),
                    Name = $"System Uptime for {Client.CurrentUser.Username}#{Client.CurrentUser.Discriminator}"
                },
                Description = "This is a breakdown of how long I've been alive and well!\r\n",
                Color = Color.Green,
                Footer= new EmbedFooterBuilder
                {
                    Text = "ModularBOT • Core"
                }
            };
            b.AddField("Application Uptime", $" {Math.Floor(deltb.TotalHours):n0} hour(s), {deltb.Minutes} minute(s), {deltb.Seconds} second(s)");
            b.AddField("Session Uptime", $" {Math.Floor(delta.TotalHours):n0} hour(s), {delta.Minutes} minute(s), {delta.Seconds} second(s)");
            await Context.Channel.SendMessageAsync("",false,b.Build());
        }


        [Command("consoleio",RunMode=RunMode.Async), Remarks("AccessLevels.Administrator"), Summary("Print consoleIO stats")]
        public async Task BOT_Consoleio([Remainder] string args = null)
        {
            
            if(DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await ReplyAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            EmbedBuilder b = new EmbedBuilder
            {
                Title = "ConsoleIO Statistics",
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto, 128),
                    Name = Client.CurrentUser.Username + "#" + Client.CurrentUser.Discriminator

                },
                Color = Color.Orange,
                Description = "Current `ConsoleIO` Class statistics Debugging information"
            };
            b.AddField("Backlog Count", ConsoleIO.Backlog.Count);
            b.AddField("Is there an active Modal Dialog?", ConsoleIO.ScreenModal);
            b.AddField("Is Screen Resetting?", ConsoleIO.ScreenBusy);
            b.AddField("Is screen Active/writing?", ConsoleIO.Writing);
            b.AddField("Was input canceled?", DiscordNet.InputCanceled);
            b.AddField("Last Logged entry", $"`{ConsoleIO.LatestEntry.LogMessage.ToString()}`");
            b.AddField("Active Modal", $"`{ConsoleIO.ActiveScreen?.Title??"NONE"}`");
            var msg = await ReplyAsync("", false, b.Build());
            for (int i = 1; i < 5; i++)
            {
                await Task.Delay(1000);
                EmbedBuilder be = new EmbedBuilder
                {
                    Title = "ConsoleIO Statistics",
                    Author = new EmbedAuthorBuilder
                    {
                        IconUrl = Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto, 128),
                        Name = Client.CurrentUser.Username + "#" + Client.CurrentUser.Discriminator

                    },
                    Color = Color.Orange,
                    Description = $"Current `ConsoleIO` Class statistics Debugging information (Update `{i}`)"
                };
                be.AddField("Backlog Count", ConsoleIO.Backlog.Count,true);
                be.AddField("Current Output Count", ConsoleIO.LogEntriesBuffer.Count, true);
                be.AddField("Is there an active Modal Dialog?", ConsoleIO.ScreenModal);
                be.AddField("Is Screen Resetting?", ConsoleIO.ScreenBusy);
                be.AddField("Is screen Active/writing?", ConsoleIO.Writing);
                be.AddField("Was input canceled?", DiscordNet.InputCanceled);
                be.AddField("Last Logged entry", $"`{ConsoleIO.LatestEntry.LogMessage.ToString()}`");
                be.AddField("Active Modal", $"`{ConsoleIO.ActiveScreen?.Title ?? "NONE"}`");
                await msg.ModifyAsync(m => m.Embed = be.Build());
            }
        }

        [Command("Shards")]
        public async Task BOT_ShardInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("  Shard      Ping      Guilds    ");
            sb.AppendLine("═════════  ════════  ══════════  ");
            int id = 0;
            foreach (DiscordSocketClient socketClient in Client.Shards)
            {
                if(id > 22)
                {
                    sb.AppendLine("═════════  ════════  ══════════  ");
                    sb.AppendLine("Only showing first 22 results.");
                    break;
                }
                string strid = socketClient.ShardId.ToString().PadLeft(9) + "  ";
                string ping = (socketClient.Latency.ToString() + " ms").PadLeft(8) + "  ";
                string guilds = socketClient.Guilds.Count.ToString().PadLeft(10) + "  ";

                sb.AppendLine(strid + ping + guilds);
                id++;
            }
            await ReplyAsync("Shard info:");
            await ReplyAsync($"```DOS\r\n{sb}\r\n```");
        }

        [Command("invitebot"), Summary("Generate a basic invite link to add the bot to your guild.")]
        public async Task ShowInvite()
        {
            EmbedBuilder builder = new EmbedBuilder
            {
                Title = "Add this bot to your guild",
                Color = Color.Purple
            };
            builder.WithAuthor(Context.Client.CurrentUser);
            builder.Description = "Click the link above to add the bot to a guild. You may only add the bot to a guild that you manage. You may not be able to use the link unless you are the bot owner, or the bot is public.";
            builder.AddField("Permissions", "These are the permissions your bot will require. You can manage additional permissions later. Please note: the bot will not function without these permissions enabled:\r\n• Send Messages (Required)\r\n• Attach Files (Required)\r\n• Embed Links (Required)\r\n");
            builder.AddField("Copyright", $"Copyright © 2017-{DateTime.Now.Year} RMSoftware Development");
            builder.AddField("Version", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
            builder.WithFooter("ModularBOT • Created by TheKingEagle");
            builder.WithUrl($"https://discordapp.com/api/oauth2/authorize?client_id={(await Client.GetApplicationInfoAsync()).Id}&permissions=51200&scope=bot");
            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("setavatar"),Summary("Set the bot's avatar."),Remarks("AccessLevels.Administrator")]
        public async Task SetAvatar([Remainder]string url=null)
        {
            if(DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await ReplyAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            if(Context.Message.Attachments.Count == 0)
            {
                if(url == null)
                {
                    await ReplyAsync("", false, GetEmbeddedMessage("Error", "You must either specify an image URL, upload an attachment.", Color.Red));
                    return;
                }
                try
                {
                    string fn = url.Split('/').Last();
                    string lf = await DownloadFile(fn, url);
                    await Client.CurrentUser.ModifyAsync(x => x.Avatar = new Image(lf));
                    await ReplyAsync("", false, GetEmbeddedMessage("Success!", "The avatar was successfully changed! Note: It may take a minute for updates to appear.", Color.Green));
                    return;
                }
                catch (Exception ex)
                {

                    await ReplyAsync("", false, GetEmbeddedMessage("Error", "There was a problem downloading the attachment.", Color.Red, ex));
                    return;
                }
                
            }
            IAttachment a = Context.Message.Attachments.First();
            if(!a.Width.HasValue)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Invalid Operation", "Attachment must be an image.", Color.Red));
                return;
            }

            try
            {
                string lf = await DownloadFile(a.Filename, a.Url);
                await Client.CurrentUser.ModifyAsync(x => x.Avatar = new Image(lf));
                await ReplyAsync("", false, GetEmbeddedMessage("Success!", "My avatar was successfully changed! Note: It may take a minute for updates to appear.", Color.Green));
                return;
            }
            catch (Exception ex)
            {

                await ReplyAsync("", false, GetEmbeddedMessage("Error", "There was a problem downloading the attachment.", Color.Red,ex));
                return;
            }
            
        }

        [Command("setnickname"),Summary("Set the bot's nickname."),Remarks("AccessLevels.Administrator"),RequireContext(ContextType.Guild,ErrorMessage ="You can only use this in a guild.")]
        public async Task SetNickname([Remainder]string nick=null)
        {
            try
            {
                var u = await Context.Guild.GetCurrentUserAsync(CacheMode.AllowDownload);
                await u.ModifyAsync(x=>x.Nickname = nick);
                await ReplyAsync("", false, GetEmbeddedMessage("Success!", "My nickname was successfully changed! Note: It may take a minute for updates to appear.", Color.Green));
            }
            catch (Exception ex)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Error", "There was a problem setting my nickname.", Color.Red, ex));
                return;
            }
        }
        [Command("listperms"), Summary("Print ALL permissions bot has"), Remarks("AccessLevels.Administrator"), RequireContext(ContextType.Guild, ErrorMessage = "You can only use this in a guild.")]
        public async Task ListPerms()
        {
            
            List<PaginatedMessage.Page> Pages = new List<PaginatedMessage.Page>();

            PaginatedMessage.Page pageItem = new PaginatedMessage.Page();
            string dsc = "";
            var self = await Context.Guild.GetCurrentUserAsync(CacheMode.AllowDownload);
            foreach (var item in self.GuildPermissions.ToList())
            {
                int flen = dsc.Length + $"• {item,-24}:: {(int)item}\r\n".Length;
                if (flen > 768)
                {

                    pageItem.Description = $"```ASCIIDOC\r\n{dsc}\r\n```";
                    Pages.Add(pageItem);
                    pageItem = new PaginatedMessage.Page();
                    dsc = "";

                }
                dsc += $"• {item,-24}:: {(int)item}\r\n";
            }
            

            pageItem.Description = $"```ASCIIDOC\r\n{dsc}\r\n```";
            Pages.Add(pageItem);
            pageItem = new PaginatedMessage.Page();
            var pager = new PaginatedMessage
            {
                Pages = Pages,
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                    Name = Context.Client.CurrentUser.ToString(),
                    Url = Context.Client.CurrentUser.GetAvatarUrl()
                },
                Color = Color.DarkGreen,
                Options = PaginatedAppearanceOptions.Default,
                TimeStamp = DateTimeOffset.UtcNow
            };


            await PagedReplyAsync(pager, new ReactionList
            {
                Forward = true,
                Backward = true,
                Jump = false,
                Trash = true
            });
        }
        [Command("setusername"), Summary("Set the bot's username."), Remarks("AccessLevels.Administrator")]
        public async Task SetUserName([Remainder]string username)
        {
            try
            {
                await Client.CurrentUser.ModifyAsync(x => {
                    x.Username = username;
                });
                await ReplyAsync("", false, GetEmbeddedMessage("Success!", "My username was successfully changed! Note: It may take a minute for updates to appear.", Color.Green));
            }
            catch (Exception ex)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Error", "There was a problem setting my username.", Color.Red, ex));
                return;
            }
        }

        [Command("printvars"), Summary("Output all variables and their values."),Remarks("AccessLevels.CommandManager")]
        public async Task PrintVars()
        {
            if(DiscordNet.PermissionManager.GetAccessLevel(Context.User)< AccessLevels.CommandManager)
            {
                await ReplyAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            GuildObject gobj = DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild?.Id) ?? DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == 0);
            List<PaginatedMessage.Page> Pages = new List<PaginatedMessage.Page>();

            PaginatedMessage.Page pageItem = new PaginatedMessage.Page();
            string dsc = "";
            dsc += "\r\n== System Variables ==\r\n";

            GuildCommand cmd = new GuildCommand()
            {
                Name = "printvars",
                Action = "N/A",
                CommandAccessLevel = AccessLevels.CommandManager,
                Counter = null,
                RequirePermission = true
            };
            foreach (var item in DiscordNet.CustomCMDMgr.coreScript.SystemVariables)
            {
                string eval = item.GetReplacedString(gobj, $"%{item.Name}%", cmd, Client, Context.Message, Cmdsvr);
                if(eval.Length > 20)
                {
                    eval = eval.Substring(0, 18) + " *";
                }
                int flen = dsc.Length + $"• {item.Name,-20} :: {eval}\r\n".Length;
                if (flen > 798)
                {
                    
                    pageItem.Description = $"```ASCIIDOC\r\n{dsc}\r\n* :: Value was too long for display.\r\n```";
                    Pages.Add(pageItem);
                    pageItem = new PaginatedMessage.Page();
                    dsc = "";

                }
                dsc += $"• {item.Name,-20} :: {eval}\r\n";
            }
            dsc += "\r\n* :: Value was too long for display.";
            dsc += "\r\n== Custom Variables ==\r\n";
            foreach (var item in DiscordNet.CustomCMDMgr.coreScript.Variables)
            {
                if(item.Value.hidden)
                {
                    continue;
                }
                int flen = dsc.Length + $"• {item.Key,-20} :: {item.Value.value}\r\n".Length;
                if(flen > 798)
                {

                    pageItem.Description = $"```ASCIIDOC\r\n{dsc}\r\n```";
                    Pages.Add(pageItem);
                    pageItem = new PaginatedMessage.Page();
                    dsc = "";
                    
                }
                dsc += $"• {item.Key,-20} :: {item.Value.value}\r\n";
            }
            if(DiscordNet.PermissionManager.GetAccessLevel(Context.User) == AccessLevels.Administrator)
            {
                dsc += "\r\n== Hidden Variables (Administrator View) ==\r\n";
                foreach (var item in DiscordNet.CustomCMDMgr.coreScript.Variables.Where(x => x.Value.hidden == true))
                {

                    int flen = dsc.Length + $"• {item.Key,-20} :: {item.Value.value}\r\n".Length;
                    if (flen > 819)
                    {

                        pageItem.Description = $"```ASCIIDOC\r\n{dsc}\r\n```";
                        Pages.Add(pageItem);
                        pageItem = new PaginatedMessage.Page();
                        dsc = "";

                    }
                    dsc += $"• {item.Key,-20} :: {item.Value.value}\r\n";
                }
            }

            if(DiscordNet.CustomCMDMgr.coreScript.UserVariableDictionaries.ContainsKey(Context.Message.Author.Id))
            {
                dsc += "\r\n== USER variables ==\r\n";
                foreach (var item in DiscordNet.CustomCMDMgr.coreScript.UserVariableDictionaries[Context.Message.Author.Id])
                {
                    int flen = dsc.Length + $"• {item.Key,-20} :: {item.Value.value}\r\n".Length;
                    if (item.Value.hidden)
                    {
                        flen = dsc.Length + $"• {"[H] " + item.Key,-20} :: {item.Value.value}\r\n".Length;
                    }
                    if (flen > 796)
                    {

                        pageItem.Description = $"```ASCIIDOC\r\n{dsc}\r\n```";
                        Pages.Add(pageItem);
                        pageItem = new PaginatedMessage.Page();
                        dsc = "";

                    }
                    if (item.Value.hidden)
                    {

                        dsc += $"• {"[H] " + item.Key,-20} :: {item.Value.value}\r\n";
                    }
                    else
                    {
                        dsc += $"• {item.Key,-20} :: {item.Value.value}\r\n";
                    }
                }

            }


            pageItem.Description = $"```ASCIIDOC\r\n{dsc}\r\n```";
            Pages.Add(pageItem);
            pageItem = new PaginatedMessage.Page();
            dsc = "";
            var pager = new PaginatedMessage
            {
                Pages = Pages,
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                    Name = Context.Client.CurrentUser.ToString(),
                    Url = Context.Client.CurrentUser.GetAvatarUrl()
                },
                Color = Color.DarkGreen,
                Options = PaginatedAppearanceOptions.Default,
                TimeStamp = DateTimeOffset.UtcNow
            };


            await PagedReplyAsync(pager, new ReactionList
            {
                Forward = true,
                Backward = true,
                Jump = false,
                Trash = true
            });
        }

        [Command("config"),Summary("Modify various configuration settings. NOTE: Access Level depends on item executed."),Remarks("AccessLevels.NotSpecified"),RequireContext(ContextType.Guild,ErrorMessage ="YoU MuST Be iN a GuILd To uSe ThIs CoMmAnD!1!111!!!1")]
        public async Task Configure(string subcommand="",string setting="",[Remainder]string value="")
        {
            switch (subcommand.ToUpper())
            {
                case ("VIEW"):
                    if (DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
                    {
                        await ReplyAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                        break;
                    }
                    GuildObject g = DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);
                    if (g == null)
                    {
                        await ReplyAsync("", false, GetEmbeddedMessage("No guild object...",
                            "There was no guild object found for this guild! That should be impossible, unless your guild was added while the bot was offline." +
                            " Adding a command will fix this problem", Color.DarkRed));
                        break;
                    }
                    EmbedBuilder configViewEB = new EmbedBuilder();
                    configViewEB.WithAuthor(Context.Client.CurrentUser);
                    configViewEB.WithColor(Color.Green);
                    configViewEB.WithTitle("Configuration View");
                    bool DoOncePerPageGC = false;
                    bool DoOncePerPageSC = false;
                    
                    List<PaginatedMessage.Page> Pages = new List<PaginatedMessage.Page>();

                    PaginatedMessage.Page pageItem = new PaginatedMessage.Page();
                    string dsc = "";
                    foreach (ConfigEntity item in DiscordNet.serviceProvider.GetRequiredService<ConfigurationManager>().GuildConfigEntities)
                    {
                        if(!DoOncePerPageGC)
                        {
                            dsc += "== Guild Configuration ==\r\n";
                            DoOncePerPageGC = true;
                        }
                        string toAddG = item.ExecuteView(DiscordNet, Context);
                        int flen = dsc.Length + $"{toAddG}\r\n".Length;
                        if (flen > 2000)
                        {

                            pageItem.Description = $"```ASCIIDOC\r\n{dsc}\r\n```";
                            Pages.Add(pageItem);
                            pageItem = new PaginatedMessage.Page();
                            dsc = "";
                            DoOncePerPageGC = false;

                        }
                        dsc += $"{toAddG}\r\n";
                    }
                    foreach (ConfigEntity item in DiscordNet.serviceProvider.GetRequiredService<ConfigurationManager>().ModularCnfgEntities)
                    {
                        if (!DoOncePerPageSC)
                        {
                            dsc += "\r\n== System Configuration ==\r\n";
                            DoOncePerPageSC = true;
                        }
                        string toAdd = item.ExecuteView(DiscordNet, Context);
                        int flen = dsc.Length + $"{toAdd}\r\n".Length;
                        if (flen > 2000)
                        {

                            pageItem.Description = $"```ASCIIDOC\r\n{dsc}\r\n```";
                            Pages.Add(pageItem);
                            pageItem = new PaginatedMessage.Page();
                            dsc = "";
                            DoOncePerPageSC = false;

                        }
                        dsc += $"{toAdd}\r\n";
                    }

                    pageItem.Description = $"```ASCIIDOC\r\n{dsc}\r\n```";
                    Pages.Add(pageItem);
                    pageItem = new PaginatedMessage.Page();
                    dsc = "";
                    var pager = new PaginatedMessage
                    {
                        Pages = Pages,
                        Author = new EmbedAuthorBuilder
                        {
                            IconUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                            Name = Context.Client.CurrentUser.ToString(),
                            Url = Context.Client.CurrentUser.GetAvatarUrl()
                        },
                        Color = Color.DarkGreen,
                        Options = PaginatedAppearanceOptions.Default,
                        TimeStamp = DateTimeOffset.UtcNow
                    };

                    if(Pages.Count > 1)
                    {
                        await PagedReplyAsync(pager, new ReactionList
                        {
                            Forward = true,
                            Backward = true,
                            Jump = false,
                            Trash = true
                        });
                    }
                    if (Pages.Count == 1)
                    {
                        await PagedReplyAsync(pager, new ReactionList
                        {
                            Forward = false,
                            Backward = false,
                            Jump = false,
                            Trash = true
                        });
                    }

                    break;
                case ("SET"):
                    g = DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);
                    List<ConfigEntity> All = new List<ConfigEntity>();
                    All.AddRange(DiscordNet.serviceProvider.GetRequiredService<ConfigurationManager>().GuildConfigEntities);
                    All.AddRange(DiscordNet.serviceProvider.GetRequiredService<ConfigurationManager>().ModularCnfgEntities);
                    ConfigEntity c = All.FirstOrDefault(x => x.ConfigIdentifier.ToUpper() == setting.ToUpper());
                    if(c== null)
                    {
                        string settingIDs = "";
                        foreach (var item in All)
                        {
                            if(!item.ReadOnly)
                            {
                                settingIDs += $"\u2005\u2005\u2005 • `{item.ConfigIdentifier}`\r\n";
                            }
                        }
                        await ReplyAsync("", false, GetEmbeddedMessage("Invalid Setting!!", $"You can edit the following settings with this command:\r\n\r\n{settingIDs}", Color.Red));

                        break;
                    }
                    if (c.ReadOnly)
                    {
                        string settingIDs = "";
                        foreach (var item in All)
                        {
                            if (!item.ReadOnly)
                            {
                                settingIDs += $"\u2005\u2005\u2005 • `{item.ConfigIdentifier}`\r\n";
                            }
                        }
                        await ReplyAsync("", false, GetEmbeddedMessage("Invalid Setting!!", $"You can edit the following settings with this command:\r\n\r\n{settingIDs}", Color.Red));

                        break;
                    }
                    await c.ExecuteSet(Client, DiscordNet, Context, value);
                    break;
                default:
                    await ReplyAsync("", false, GetEmbeddedMessage("Invalid Sub-command!!", "Available sub commands are\r\n\r\n• `SET`\r\n• `VIEW`", Color.Red));
                    break;
            }
        }


        [Command("upload"), Remarks("AccessLevels.CommandManager"), Summary("Upload a file attachment to the bot's internal attachment server (LIMIT: 8 MB).")]
        public async Task BOT_UploadAttachment(string fileName, bool IsScript=false)
        {
            if(DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await ReplyAsync("", false, DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            ulong guildid = 0;
            int ESizeTooBig = 0;
            int attc = 0;
            string ATTACH_LINES = "";
            if(Context.Guild != null)
            {
                guildid = Context.Guild.Id;
            }
            if(!Directory.Exists($"attachments/{guildid}"))
            {
                Directory.CreateDirectory($"attachments/{guildid}");
            }
            if (!Directory.Exists($"scripts/{guildid}"))
            {
                Directory.CreateDirectory($"scripts/{guildid}");
            }
            if (Context.Message.Attachments.Count == 0)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("No Attachment!", 
                    "You did not add an attachment to upload. You must add a file to upload.", Color.DarkRed));
                return;
            }
            if (fileName.Any(x => Path.GetInvalidFileNameChars().Contains(x)))
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Invalid Filename!",
               "The filename you specified contains illegal characters. \r\nIllegal Characters:\r\n `/`, `\\`, `?`, `\"`,`|`, `<`, `>`, or `*`  ", Color.DarkRed));
                return;
            }
            foreach (Attachment item in Context.Message.Attachments.ToList())
            {
                if(item.Size > 8388608)
                {
                    ESizeTooBig++;
                    continue;
                }
                if(!IsScript)
                {
                    string FPATH = $"attachments/{guildid}/{fileName}_{attc}{Path.GetExtension(item.Filename)}";

                    string APATH = $"{guildid}/{fileName}_{attc}{Path.GetExtension(item.Filename)}";
                    try
                    {
                        WebRequest wr = WebRequest.CreateHttp(item.Url);
                        wr.GetResponse().GetResponseStream().CopyTo(File.Create(FPATH));
                        ATTACH_LINES += $"ATTACH {APATH}\r\n";
                        attc++;
                    }
                    catch (Exception ex)
                    {

                        await ReplyAsync("", false, GetEmbeddedMessage($"Upload Failed!",
                        $"The file could not be uploaded to the attachment server.\r\n```\r\n{ex.Message}\r\n```", Color.DarkRed));
                        return;
                    }
                }
                if (IsScript)
                {
                    string FPATH = $"scripts/{guildid}/{fileName}_{attc}{Path.GetExtension(item.Filename)}";

                    string APATH = $"{guildid}/{fileName}_{attc}{Path.GetExtension(item.Filename)}";
                    try
                    {
                        WebRequest wr = WebRequest.CreateHttp(item.Url);
                        wr.GetResponse().GetResponseStream().CopyTo(File.Create(FPATH));
                        ATTACH_LINES += $"START {APATH}\r\n";
                        attc++;
                    }
                    catch (Exception ex)
                    {

                        await ReplyAsync("", false, GetEmbeddedMessage($"Upload Failed!",
                        $"The file could not be uploaded to the script server.\r\n```\r\n{ex.Message}\r\n```", Color.DarkRed));
                        return;
                    }
                }


            }
            
            if (!string.IsNullOrEmpty(ATTACH_LINES))
            {
                Color DCEC = (ESizeTooBig > 0) ? Color.LightOrange : Color.Green;
                string Message = (ESizeTooBig > 0) ? $"Unable to upload {ESizeTooBig} file(s). Exceeded the 8 MB file limit.\r\n\r\n": $"Uploaded {attc} file(s) Successfully.\r\n\r\n";
                string warntitle = (ESizeTooBig > 0) ? " (With errors)" : "";
                await ReplyAsync("", false, GetEmbeddedMessage($"Upload Completed!{warntitle}",
                    $"{Message}**The script snippet below can be copied into a command**\r\n```DOS\r\n{ATTACH_LINES}\r\n```", DCEC));
                return;
            }
            if(string.IsNullOrEmpty(ATTACH_LINES))
            {
                string Message = (ESizeTooBig > 0) ? $"Unable to upload {ESizeTooBig} file(s). Exceeded the 8 MB file limit.\r\n\r\n" : $"Unable to upload the files due to an unknown error.";
                await ReplyAsync("", false, GetEmbeddedMessage($"Upload Failed...", Message,Color.DarkRed));
                return;
            }
        }
        #endregion

        #region Messages
        public Embed GetEmbeddedMessage(string title, string message, Color color,Exception e=null)
        {
            EmbedBuilder b = new EmbedBuilder();
            b.WithColor(color);
            b.WithAuthor(Context.Client.CurrentUser);
            b.WithTitle(title);
            b.WithDescription(message);
            b.WithFooter("ModularBOT • Core");
            if (e != null)
            {
                b.AddField("Extended Details", e.Message);
                b.AddField("For developer", "See the Errors.LOG for more info!!!");
                ConsoleIO.WriteErrorsLog(e);
            }
            return b.Build();
        }
        #endregion

        #region internal tasks.

        internal async Task<string> DownloadFile(string filename,string attachmentURL)
        {
            try
            {
                WebRequest wrq = WebRequest.Create(attachmentURL);
                WebResponse wrs = await wrq.GetResponseAsync();
                string localfile = $"downloads/{filename}";
                if (!Directory.Exists("downloads"))
                {
                    Directory.CreateDirectory("downloads");
                }
                using (FileStream fs = File.Create(localfile))
                {
                    await wrs.GetResponseStream().CopyToAsync(fs);
                    await fs.FlushAsync();
                }
                return localfile;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion
    }
}
