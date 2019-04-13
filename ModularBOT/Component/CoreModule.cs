using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace ModularBOT.Component
{
    public class CoreModule:ModuleBase
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

        [Command("about"), Summary("Display information about the bot"), Remarks("AccessLevels.Blacklisted")]//for testing. FOR SCIENCE...
        public async Task CORE_ShowAbout()
        {
            EmbedBuilder builder = new EmbedBuilder
            {
                Title = "About"
            };
            builder.WithAuthor(Context.Client.CurrentUser);
            builder.Color = Color.Blue;
            builder.Description = "A Multi-purpose, multi-module bot designed for discord. Tailor it for your specific server, create your own modules and plug-ins. Includes a core module for custom text-based commands & EXEC functionality";
            builder.AddField("Copyright", "Copyright © 2017-2019 RMSoftware Development");
            builder.AddField("Version", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
            builder.WithFooter("ModularBOT | created by TheKingEagle");
            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        #region Command Management
        [Command("addcmd"),Summary("Add a command to your bot. If you run this via DM, it will create a global command."), Remarks("AccessLevels.CommandManager")]
        public async Task CMD_Add(string cmdname, bool restricted, [Remainder]string action)
        {
            if (_DiscordNet.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.pmgr.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            ulong gid = 0;
            if(Context.Guild != null)
            {
                gid = Context.Guild.Id;
            }

            await _DiscordNet.ccmgr.AddCmd(Context.Message, cmdname, action, restricted,gid);
        }

        [Command("addgcmd"), Summary("Add a global command to your bot"),Remarks("AccessLevels.CommandManager")]
        public async Task CMD_AddGlobal(string cmdname, bool restricted, [Remainder]string action)
        {
            if (_DiscordNet.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.pmgr.GetAccessDeniedMessage(Context,AccessLevels.CommandManager));
                return;
            }
            await _DiscordNet.ccmgr.AddCmd(Context.Message, cmdname, action, restricted);
        }

        [Command("delcmd"), Summary("delete a command from your bot. If you run this via DM, it will delete a global command."), Remarks("AccessLevels.CommandManager")]
        public async Task CMD_Delete(string cmdname)
        {
            if (_DiscordNet.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.pmgr.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            ulong gid = 0;
            if (Context.Guild != null)
            {
                gid = Context.Guild.Id;
            }

            await _DiscordNet.ccmgr.DelCmd(Context.Message, cmdname, gid);
        }

        [Command("delgcmd"), Summary("delete a global command from your bot."), Remarks("AccessLevels.CommandManager")]
        public async Task CMD_DeleteGlobal(string cmdname)
        {
            if (_DiscordNet.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.pmgr.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }

            await _DiscordNet.ccmgr.DelCmd(Context.Message, cmdname);
        }

        [Command("listcmd"), Summary("Lists all available commands for current context.")]
        public async Task CMD_ListCommands()
        {

            CommandList commandList = new CommandList(Client.CurrentUser.Username,Context.Guild?.Name ?? "Direct Messages");
            string prefix = _DiscordNet.serviceProvider.GetRequiredService<Configuration>().CommandPrefix;
            if (Context.Guild != null)
            {
                GuildObject obj = _DiscordNet.ccmgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);
                if (obj != null) prefix = obj.CommandPrefix;
            }

            #region CORE
            foreach (CommandInfo item in Cmdsvr.Commands)
            {
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
            GuildObject globalGO = _DiscordNet.ccmgr.GuildObjects.FirstOrDefault(x => x.ID == 0);
            if (globalGO != null)
            {
                foreach (GuildCommand item in globalGO.GuildCommands)
                {
                    commandList.AddCommand(prefix + item.Name, item.RequirePermission ? AccessLevels.CommandManager : AccessLevels.Normal, CommandTypes.GCustom);
                }
            }

            if (Context.Guild != null)
            {
                GuildObject currentGuild = _DiscordNet.ccmgr.GuildObjects.FirstOrDefault(x => x.ID == Context.Guild.Id);
                if (currentGuild != null)
                {
                    foreach (GuildCommand item in currentGuild.GuildCommands)
                    {
                        commandList.AddCommand(prefix + item.Name, item.RequirePermission ? AccessLevels.CommandManager : AccessLevels.Normal, CommandTypes.Custom);
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
        #endregion

        #region Permission Management
        [Command("permissions set user"),Alias("psu"), Remarks("AccessLevels.Administrator")]
        public async Task PERM_SetUser(IUser user, AccessLevels accessLevel)
        {
            if (_DiscordNet.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.pmgr.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            try
            {
                int result = _DiscordNet.pmgr.RegisterEntity(user, accessLevel);
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

        [Command("permissions set role"),RequireContext(ContextType.Guild), Alias("psr"), Remarks("AccessLevels.Administrator")]
        public async Task PERM_SetRole(IRole role, AccessLevels accessLevel)
        {
            if (_DiscordNet.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.pmgr.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            try
            {
                int result = _DiscordNet.pmgr.RegisterEntity(role, accessLevel);
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

        [Command("permissions del user"), Alias("pdu","pru"), Remarks("AccessLevels.Administrator")]
        public async Task PERM_DeleteUser(IUser user)
        {
            if (_DiscordNet.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.pmgr.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            try
            {
                bool result = _DiscordNet.pmgr.DeleteEntity(user);
                switch (result)
                {
                    case (true):
                        b.WithAuthor(Client.CurrentUser);
                        b.WithTitle("Permission Manager");
                        b.WithDescription("The user was successfully removed from the permissions file. User will inherit permissions from any registered roles. Otherwise, they'll be treated as `AccessLevel: 0`");
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

        [Command("permissions del role"), Alias("pdr", "prr"), Remarks("AccessLevels.Administrator")]
        public async Task PERM_DeleteRole(IRole role)
        {
            if (_DiscordNet.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.pmgr.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            try
            {
                bool result = _DiscordNet.pmgr.DeleteEntity(role);
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

        #endregion

        #region Bot management
        [Command("stopbot",RunMode= RunMode.Async), Alias("stop"), Remarks("AccessLevels.Administrator")]
        public async Task BOT_StopBot()
        {
            if (_DiscordNet.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.pmgr.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            if (_DiscordNet.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                
                b.WithTitle("Access Denied");
                b.WithAuthor(Context.Client.CurrentUser);
                b.WithDescription("You do not have permission to use this command. Requires `AccessLevel 2 (Administrator)` or higher.");
                b.WithColor(Color.Red);
                b.WithFooter("ModularBOT • Core");
                await Context.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
            b.WithTitle("Shutting Down...");
            b.WithAuthor(Context.Client.CurrentUser);
            b.WithDescription("Administrator called for termination! Ending session & disconnecting...");
            b.WithColor(Color.Red);
            b.WithFooter("ModularBOT • Core");
            await Context.Channel.SendMessageAsync("", false, b.Build());
            _DiscordNet.Stop(ref Program.ShutdownCalled);
        }

        [Command("restartbot", RunMode = RunMode.Async),Alias("restart"), Remarks("AccessLevels.Administrator")]
        public async Task BOT_RestartBot()
        {
            if (_DiscordNet.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.pmgr.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            if (_DiscordNet.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                
                b.WithTitle("Access Denied");
                b.WithAuthor(Context.Client.CurrentUser);
                b.WithDescription("You do not have permission to use this command. Requires `AccessLevel 2 (Administrator)` or higher.");
                b.WithColor(Color.Red);
                b.WithFooter("ModularBOT • Core");
                await Context.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
            b.WithTitle("Restarting...");
            b.WithAuthor(Context.Client.CurrentUser);
            b.WithDescription("Administrator called for restart! Ending session & restarting the application");
            b.WithColor(Color.Red);
            b.WithFooter("ModularBOT • Core");
            await Context.Channel.SendMessageAsync("", false, b.Build());
            _DiscordNet.Stop(ref Program.ShutdownCalled);
        }

        [Command("status"), Remarks("AccessLevels.Administrator")]
        public async Task BOT_SetStatus(string text, string StreamURL="")
        {
            
            if(_DiscordNet.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, _DiscordNet.pmgr.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            if(text.ToLower().StartsWith("playing "))
            {
                await _DiscordNet.Client.SetGameAsync(text.Remove(0,8),null);
            }
            if (text.ToLower().StartsWith("watching "))
            {
                await _DiscordNet.Client.SetGameAsync(text.Remove(0, 9), null,ActivityType.Watching);
            }
            if (text.ToLower().StartsWith("streaming "))
            {
                await _DiscordNet.Client.SetGameAsync(text.Remove(0, 10), StreamURL, ActivityType.Streaming);
            }
            if (text.ToLower().StartsWith("listening to "))
            {
                await _DiscordNet.Client.SetGameAsync(text.Remove(0, 13), null, ActivityType.Listening);
            }
        }
        #endregion
    }
}
