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
namespace ModularBOT.Component
{
    public class CoreModule:InteractiveBase<CommandContext>
    {
        #region Property/Construct
        DiscordShardedClient Client { get; set; }
        CommandService Cmdsvr { get; set; }
        ConsoleIO ConsoleIO { get; set; }
        DiscordNET _DiscordNet { get; set; }

        public CoreModule(DiscordShardedClient client, CommandService cmdservice, ConsoleIO consoleIO, DiscordNET dnet)
        {
            Client = client;
            Cmdsvr = cmdservice;
            this.ConsoleIO = consoleIO;
            _DiscordNet = dnet;
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

        [Command("changes"), Summary("Shows what changed in this version")]
        public async Task CORE_ShowChanges()
        {
            EmbedBuilder eb = new EmbedBuilder();

            eb.WithAuthor("What's New", Client.CurrentUser.GetAvatarUrl(), "");
            eb.WithDescription("NOTE: This application is not finished. Some features are not currently implemented.");
            eb.AddField($"v{Assembly.GetExecutingAssembly().GetName().Version.ToString(4)} ModularBOT Remastered™ - System Updates",
                $"• MOVED to Discord.NET v2.0.1\r\n" +
                $"• Supports more guilds! - Shard connections\r\n" +
                $"• RE-WROTE ConsoleIO & REDESINED Console UI\r\n" +
                $"• RE-WROTE Command manager to use a cleaner, more-modular format\r\n" +
                $"• RE-WROTE Setup Wizard\r\n" +
                $"• RE-WROTE Configuration system\r\n" +
                $"• RE-WROTE Permission system\r\n" +
                $"• ORGANIZED Source code looks pretty\r\n" +
                $"• ADDED Update system\r\n" +
                $"• REMOVED Json log mode\r\n" +
                $"• Cleaned up install directory\r\n" +
                $"• IMPROVED KillScreens & Stability\r\n" + 
                $"• ADDED Customizable console colors.");
            eb.AddField($"v{Assembly.GetExecutingAssembly().GetName().Version.ToString(4)} ModularBOT Remastered™ - Command Updates",
                $"• ADDED commands: `addgcmd`, `delgcmd`, `permissions set/del user/role`, `permissions get`, `prefix`\r\n" +
                $"• CHANGED `status` command syntax\r\n" +
                $"• CHANGED `addcmd` command syntax\r\n" +
                $"• ADDED per-guild prefix support\r\n" +
                $"• ADDED multi-character prefix support\r\n" +
                $"• IMPROVED command list annotations\r\n");


            eb.WithFooter("ModularBOT • CORE");
            eb.Color = Color.DarkBlue;
            await Context.Channel.SendMessageAsync("**Full version history/change log: http://rms0.org?a=mbChanges**", false, eb.Build());
        }

        #region Command Management
        [Command("addcmd"),Summary("Add a command to your bot. If you run this via DM, it will create a global command. (NOTE: Creating a global commands requires AccessLevels.Administrator)"), Remarks("AccessLevels.CommandManager")]
        public async Task CMD_Add(string cmdname, bool restricted, [Remainder]string action)
        {
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            if(action == "{params}" || action == "{0}")
            {
                if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
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
                if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)//global commands require administrator priv.
                {
                    await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                    return;
                }
            }
            await _DiscordNet.CustomCMDMgr.AddCmd(Context.Message, cmdname, action, restricted,gid);
        }

        [Command("addcmd"), Summary("Add a command to your bot. If you run this via DM, it will create a global command. (NOTE: Creating a global commands requires AccessLevels.Administrator)"), Remarks("AccessLevels.CommandManager")]
        public async Task CMD_Add(string cmdname, AccessLevels CommandAccessLevel, [Remainder]string action)
        {
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            if (action == "{params}" || action == "{0}")
            {
                if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
                {
                    await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Sorry!", $"You may not create this type of command. You must have `AccessLevels.Administrator`.", Color.DarkRed));
                    return;
                }
            }
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < CommandAccessLevel)
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
                if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)//global commands require administrator priv.
                {
                    await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                    return;
                }
            }
            await _DiscordNet.CustomCMDMgr.AddCmd(Context.Message, cmdname, action, CommandAccessLevel > AccessLevels.Normal,CommandAccessLevel, gid);
        }

        [Command("addgcmd"), Summary("Add a global command to your bot"),Remarks("AccessLevels.Administrator")]
        public async Task CMD_AddGlobal(string cmdname, bool restricted, [Remainder]string action)
        {
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context,AccessLevels.Administrator));
                return;
            }

            await _DiscordNet.CustomCMDMgr.AddCmd(Context.Message, cmdname, action, restricted);
        }

        [Command("addgcmd"), Summary("Add a global command to your bot"), Remarks("AccessLevels.Administrator")]
        public async Task CMD_AddGlobal(string cmdname, AccessLevels CommandAccessLevel, [Remainder]string action)
        {
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < CommandAccessLevel)
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
            await _DiscordNet.CustomCMDMgr.AddCmd(Context.Message, cmdname, action, CommandAccessLevel > AccessLevels.Normal,CommandAccessLevel);
        }

        [Command("delcmd"), Summary("delete a command from your bot. If you run this via DM, it will delete a global command (AccessLevels.Administrator)."), Remarks("AccessLevels.CommandManager")]
        public async Task CMD_Delete(string cmdname)
        {
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            ulong gid = 0;
            if (Context.Guild != null)
            {
                gid = Context.Guild.Id;
            }
            if (gid == 0)
            {
                if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)//global commands require administrator priv.
                {
                    await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                    return;
                }
            }
            await _DiscordNet.CustomCMDMgr.DelCmd(Context.Message, cmdname, gid);
        }

        [Command("delgcmd"), Summary("delete a global command from your bot."), Remarks("AccessLevels.Administrator")]
        public async Task CMD_DeleteGlobal(string cmdname)
        {
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            await _DiscordNet.CustomCMDMgr.DelCmd(Context.Message, cmdname);
        }

        [Command("getcmd"), Summary("View information about a specific command"),Remarks("AccessLevels.CommandManager")]
        public async Task CMD_Get(string cmdname)
        {
            if(_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            await Context.Channel.SendMessageAsync("", false, _DiscordNet.CustomCMDMgr.ViewCmd(Context, cmdname));
        }

        [Command("listcmd-html"), Summary("Lists all available commands for current context. sends as HTML file"), Remarks("AccessLevels.Normal")]
        public async Task CMD_ListCommands()
        {
            ulong gid = 0;
            CommandList commandList = new CommandList(Client.CurrentUser.Username,Context.Guild?.Name ?? "Direct Messages");
            string prefix = _DiscordNet.serviceProvider.GetRequiredService<Configuration>().CommandPrefix;
            if (Context.Guild != null)
            {
                GuildObject obj = _DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);
                if (obj != null) prefix = obj.CommandPrefix;
                gid = Context.Guild.Id;
            }

            #region CORE & Modules.
            foreach (CommandInfo item in Cmdsvr.Commands)
            {
                #region Per-guild module Check
                string module = item.Module.Name;
                var m = _DiscordNet.ModuleMgr.Modules.FirstOrDefault(x => x.ModuleGroups.Contains(module));
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
            GuildObject globalGO = _DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == 0);
            if (globalGO != null)
            {
                foreach (GuildCommand item in globalGO.GuildCommands)
                {
                    commandList.AddCommand(prefix + item.Name, item.RequirePermission ? item.CommandAccessLevel : AccessLevels.Normal, CommandTypes.GCustom);
                }
            }

            if (Context.Guild != null)
            {
                GuildObject currentGuild = _DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);
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

        [Command("listcmd"), Summary("Lists all available commands for current context."), Remarks("AccessLevels.Normal")]
        public async Task CMD_ListPaginator()
        {

            ulong gid = 0;
            string prefix = _DiscordNet.serviceProvider.GetRequiredService<Configuration>().CommandPrefix;
            if (Context.Guild != null)
            {
                GuildObject obj = _DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);
                gid = Context.Guild.Id;
                if (obj != null) prefix = obj.CommandPrefix;
            }

            List<PaginatedMessage.Page> pages = new List<PaginatedMessage.Page>();
            await ReplyAsync(Context.User.Mention + $", Here's a list of commands! If you are unable to navigate the list, please do `{prefix}listcmd-html` instead.");
            int cmdcount = 0;//comdcount
            int pageNum = 1;
            PaginatedMessage.Page PageItem = new PaginatedMessage.Page();

            PageItem.Title = $"Core Commands";
            PageItem.Color = Color.Green;
            PageItem.Author = new EmbedAuthorBuilder()
            {
                Name = $"{Context.Client.CurrentUser.Username}'s Command List",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
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
                    PageItem = new PaginatedMessage.Page();//start a new page.
                    PageItem.Title = $"Core Commands";
                    PageItem.Color = Color.Green;
                    PageItem.Author = new EmbedAuthorBuilder()
                    {
                        Name = $"{Context.Client.CurrentUser.Username}'s Command List",
                        IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
                    };
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
            PageItem = new PaginatedMessage.Page();

            PageItem.Title = $"External Module Commands";
            PageItem.Color = Color.Blue;
            PageItem.Author = new EmbedAuthorBuilder()
            {
                Name = $"{Context.Client.CurrentUser.Username}'s Command List",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
            };
            foreach (CommandInfo item in Cmdsvr.Commands)
            {
                if (item.Module.Name == "CoreModule")
                {
                    continue;
                }

                #region Per-guild module Check
                string module = item.Module.Name;
                var m = _DiscordNet.ModuleMgr.Modules.FirstOrDefault(x => x.ModuleGroups.Contains(module));
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
                    PageItem = new PaginatedMessage.Page();//start a new page.
                    PageItem.Title = $"External Module Commands";
                    PageItem.Color = Color.Blue;
                    PageItem.Author = new EmbedAuthorBuilder()
                    {
                        Name = $"{Context.Client.CurrentUser.Username}'s Command List",
                        IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
                    };
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
            PageItem = new PaginatedMessage.Page();

            PageItem.Title = $"Custom Commands";
            PageItem.Color = Color.Purple;
            PageItem.Author = new EmbedAuthorBuilder()
            {
                Name = $"{Context.Client.CurrentUser.Username}'s Command List",
                IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
            };
            //GLOBAL

            GuildObject globalGO = _DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == 0);
            if (globalGO != null)
            {
                foreach (GuildCommand item in globalGO.GuildCommands)
                {
                    if (cmdcount > 8)
                    {
                        cmdcount = 0;
                        pageNum++;
                        pages.Add(PageItem);
                        PageItem = new PaginatedMessage.Page();//start a new page.
                        PageItem.Title = $"Custom Commands";
                        PageItem.Color = Color.Purple;
                        PageItem.Author = new EmbedAuthorBuilder()
                        {
                            Name = $"{Context.Client.CurrentUser.Username}'s Command List",
                            IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
                        };
                    }
                    //commandList.AddCommand(prefix + item.Name, item.RequirePermission ? AccessLevels.CommandManager : AccessLevels.Normal, CommandTypes.GCustom);
                    cmdcount++;
                    string perm = item.RequirePermission ? $"AccessLevels.{item.CommandAccessLevel.ToString()}" : "AccessLevels.Normal";
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
                GuildObject currentGuild = _DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);
                if (currentGuild != null)
                {
                    foreach (GuildCommand item in currentGuild.GuildCommands)
                    {
                        if (cmdcount > 8)
                        {
                            cmdcount = 0;
                            pageNum++;
                            pages.Add(PageItem);
                            PageItem = new PaginatedMessage.Page();//start a new page.
                            PageItem.Title = $"Custom Commands";
                            PageItem.Color = Color.Purple;
                            PageItem.Author = new EmbedAuthorBuilder()
                            {
                                Name = $"{Context.Client.CurrentUser.Username}'s Command List",
                                IconUrl = Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
                            };
                        }
                        //commandList.AddCommand(prefix + item.Name, item.RequirePermission ? AccessLevels.CommandManager : AccessLevels.Normal, CommandTypes.Custom);
                        cmdcount++;
                        string perm = item.RequirePermission ? "AccessLevels.CommandManager" : "AccessLevels.Normal";
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
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }

            ulong gid = Context.Guild?.Id ?? 0;
            if(gid == 0)
            {
                if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
                {
                    await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                    return;
                }
            }
            await _DiscordNet.CustomCMDMgr.EditCMD(Context, cmdName, requirePermission, newAction, gid);

        }

        [Command("editcmd"), Summary("Edit a command. Note: Global command edits require AccessLevels.Administrator"), Remarks("AccessLevels.CommandManager")]
        public async Task CMD_EditCommand(string cmdName, AccessLevels CommandAccessLevel, [Remainder]string newAction = "(unchanged)")
        {
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < CommandAccessLevel)
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
                if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
                {
                    await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                    return;
                }
            }
            await _DiscordNet.CustomCMDMgr.EditCMD(Context, cmdName,CommandAccessLevel, newAction, gid);

        }

        #endregion

        #region Permission Management
        [Command("permissions set user"),Alias("psu"), Remarks("AccessLevels.Administrator"),Summary("Set a user's access level. NOTE: This will grant said user said access level in ALL guilds.")]
        public async Task PERM_SetUser(IUser user, AccessLevels accessLevel)
        {
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            if(user.IsBot)
            {

                if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
                {
                    await Context.Channel.SendMessageAsync("This user is a bot...", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                    return;
                }
            }
            if (user == Context.User)
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.","You can't change your own access level.",Color.DarkRed));
                return;
            }
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < accessLevel)
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't give someone a higher access level than your own.", Color.DarkRed));
                return;
            }
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < _DiscordNet.PermissionManager.GetAccessLevel(user))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't change a user who has a higher access level than your own.", Color.DarkRed));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            try
            {
                int result = _DiscordNet.PermissionManager.RegisterEntity(user, accessLevel);
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
                        b.AddField("AccessLevel", $"`{accessLevel.ToString()}`", true);
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
                        b.AddField("AccessLevel", $"`{accessLevel.ToString()}`", true);
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
            IWebhook hook = null;
            if (Context.Guild != null)
            {
                role = Context.Guild?.GetRole(GenericID);
                if (role == null)
                {
                    user = Context.Guild?.GetUserAsync(GenericID,CacheMode.AllowDownload).GetAwaiter().GetResult();
                    if (user == null)
                    {
                        hook = Context.Guild?.GetWebhookAsync(GenericID).GetAwaiter().GetResult();
                        if(hook==null)
                        {
                            await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... What?",
                                "The entity id did not match a user or role. Please make sure you got it right!", Color.DarkRed));
                            return;
                        }
                    }
                }
            }
            #endregion

            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            if (GenericID == Context.User.Id)
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't change your own access level.", Color.DarkRed));
                return;
            }
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < accessLevel)
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't give anyone a higher access level than your own.", Color.DarkRed));
                return;
            }
            if(role != null)
            {
                if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < _DiscordNet.PermissionManager.GetAccessLevel(role))
                {
                    await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't change a role who has a higher access level than your own.", Color.DarkRed));
                    return;
                }
            }
            if (user != null)
            {
                if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < _DiscordNet.PermissionManager.GetAccessLevel(user))
                {
                    await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't change a user who has a higher access level than your own.", Color.DarkRed));
                    return;
                }
            }
            EmbedBuilder b = new EmbedBuilder();
            try
            {
                int result = _DiscordNet.PermissionManager.RegisterEntity(Context,GenericID, accessLevel);
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
                        b.AddField("AccessLevel", $"`{accessLevel.ToString()}`", true);
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
                        b.AddField("AccessLevel", $"`{accessLevel.ToString()}`", true);
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
                b.AddField("More Details", $"{ex.ToString()}", true);
                b.WithFooter("ModularBOT • Core");
                b.WithColor(Color.Red);
            }


            await Context.Channel.SendMessageAsync("", false, b.Build());
        }

        [Command("permissions set role"),RequireContext(ContextType.Guild), Alias("psr"), Remarks("AccessLevels.CommandManager"), Summary("Set permissions for a role")]
        public async Task PERM_SetRole(IRole role, AccessLevels accessLevel)
        {
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            try
            {
                int result = _DiscordNet.PermissionManager.RegisterEntity(role, accessLevel);
                switch (result)
                {
                    case (1):
                        b.WithAuthor(Client.CurrentUser);
                        b.WithTitle("Permission Manager");
                        b.WithDescription("The role was successfully added to the permissions file.");
                        b.AddField("Role", $"`{role.Name} ({role.Guild.Name})`", true);
                        b.AddField("AccessLevel", $"`{accessLevel.ToString()}`", true);
                        b.WithFooter("ModularBOT • Core");
                        b.WithColor(Color.Green);
                        
                        break;
                    case (2):
                        b.WithAuthor(Client.CurrentUser);
                        b.WithTitle("Permission Manager");
                        b.WithDescription("The role was successfully updated.");
                        b.AddField("Role", $"`{role.Name} ({role.Guild.Name})`", true);
                        b.AddField("AccessLevel", $"`{accessLevel.ToString()}`", true);
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
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            if (user == Context.User)
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't remove yourself from the permission system.", Color.DarkRed));
                return;
            }
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < _DiscordNet.PermissionManager.GetAccessLevel(user))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't remove a user from the system if they have a higher access level than you...", Color.DarkRed));
                return;
            }
            EmbedBuilder b = new EmbedBuilder();
            try
            {
                bool result = _DiscordNet.PermissionManager.DeleteEntity(user);
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
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < _DiscordNet.PermissionManager.GetAccessLevel(role))
            {
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Wait... That's Illegal.", "You can't remove a role from the system if they have a higher access level than you...", Color.DarkRed));
                return;
            }
            EmbedBuilder b = new EmbedBuilder();
            try
            {
                bool result = _DiscordNet.PermissionManager.DeleteEntity(role);
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
            if(_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            AccessLevels l = _DiscordNet.PermissionManager.GetAccessLevel(user, out IRole inheritedRole, out bool BotOwner, out bool InList);
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
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Shutting Down...", "Administrator called for application termination. Ending session...", Color.DarkBlue));
            _DiscordNet.Stop(ref Program.ShutdownCalled);
        }

        [Command("restartbot", RunMode = RunMode.Async),Alias("restart"), Remarks("AccessLevels.Administrator"), Summary("Calls for termination of session, and restarts program.")]
        public async Task BOT_RestartBot()
        {
            if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Restarting...", "Administrator called for application restart. Ending session...", Color.DarkBlue));

            Program.RestartRequested = true;
            await Task.Run(() =>_DiscordNet.Stop(ref Program.ShutdownCalled));
            
        }

        [Command("status"), Remarks("AccessLevels.Administrator"), Summary("Sets status text for bot user. Start with playing, watching, listening to, or streaming.")]
        public async Task BOT_SetStatus(string text, string StreamURL="")
        {
            
            if(_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            if(text.ToLower().StartsWith("playing "))
            {
                await _DiscordNet.Client.SetGameAsync(text.Remove(0,8),null);
                await ReplyAsync(null, false, GetEmbeddedMessage("Success!", $"Bot's status changed to: **{text}**", Color.Green));
            }
            if (text.ToLower().StartsWith("watching "))
            {
                await _DiscordNet.Client.SetGameAsync(text.Remove(0, 9), null,ActivityType.Watching);
                await ReplyAsync(null, false, GetEmbeddedMessage("Success!", $"Bot's status changed to: **{text}**", Color.Green));
            }
            if (text.ToLower().StartsWith("streaming "))
            {
                await _DiscordNet.Client.SetGameAsync(text.Remove(0, 10), StreamURL, ActivityType.Streaming);
                await ReplyAsync(null, false, GetEmbeddedMessage("Success!", $"Bot's status changed to: **{text}**", Color.Green));
            }
            if (text.ToLower().StartsWith("listening to "))
            {
                await _DiscordNet.Client.SetGameAsync(text.Remove(0, 13), null, ActivityType.Listening);
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
                GuildObject pobj = _DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == pgid);
                await Context.Channel.SendMessageAsync("", false, GetEmbeddedMessage("Current Prefix", $"The current prefix for `{pg?.Name ?? "Direct Messages"}` is `{pobj?.CommandPrefix ?? _DiscordNet.serviceProvider.GetRequiredService<Configuration>().CommandPrefix}`", new Color(0,255,0)));
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
                if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
                {
                    await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                    return;
                }
                if(_DiscordNet.PermissionManager.GetAccessLevel(Context.User) == AccessLevels.CommandManager)
                {
                    if(Context.User is SocketGuildUser sgu)
                    {
                        if(!sgu.GuildPermissions.Has(GuildPermission.ManageMessages))
                        {
                            await ReplyAsync("", false, GetEmbeddedMessage("DENIED!", 
                                "You must have `AccessLevels.CommandManager` AND have permission to manage messages.", Color.DarkRed));
                            return;
                        }
                    }
                }
               
            }
            if(gid == 0)
            {
                if (_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
                {
                    await Context.Channel.SendMessageAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                    return;
                }
                _DiscordNet.serviceProvider.GetRequiredService<Configuration>().CommandPrefix = newPrefix;
                Program.configMGR.Save();
                
            }
            GuildObject obj = _DiscordNet.CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == gid);
            if(obj!=null)
            {
                
                obj.CommandPrefix = newPrefix;
                obj.SaveJson();
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
                    _DiscordNet.CustomCMDMgr.AddGuildObject(obj);//safely inject the new object.
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
            var delta = DateTime.Now - _DiscordNet.StartTime;
            string format = string.Format("I've been alive and well for **{0}** hours, **{1}** minutes, and **{2}** seconds!", Math.Floor(delta.TotalHours).ToString("n0"), delta.Minutes, delta.Seconds);
            await Context.Channel.SendMessageAsync(format + " " + args);
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
            await ReplyAsync($"```DOS\r\n{sb.ToString()}\r\n```");
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
            if(_DiscordNet.PermissionManager.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await ReplyAsync("", false, _DiscordNet.PermissionManager.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
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
                var user = Context.Guild.GetUserAsync(Client.CurrentUser.Id).GetAwaiter().GetResult();
                await user.ModifyAsync(x => {
                    x.Nickname = nick;
                });
                await ReplyAsync("", false, GetEmbeddedMessage("Success!", "My nickname was successfully changed! Note: It may take a minute for updates to appear.", Color.Green));
            }
            catch (Exception ex)
            {
                await ReplyAsync("", false, GetEmbeddedMessage("Error", "There was a problem setting my nickname.", Color.Red, ex));
                return;
            }
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
