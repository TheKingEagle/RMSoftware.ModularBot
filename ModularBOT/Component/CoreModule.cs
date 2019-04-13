using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace ModularBOT.Component
{
    public class CoreModule:ModuleBase
    {
        #region Property/Construct
        DiscordShardedClient Client { get; set; }
        CommandService Cmdsvr { get; set; }
        ConsoleIO ConsoleIO { get; set; }
        DiscordNET Net { get; set; }

        public CoreModule(DiscordShardedClient client, CommandService cmdservice, ConsoleIO consoleIO, DiscordNET dnet)
        {
            Client = client;
            Cmdsvr = cmdservice;
            this.ConsoleIO = consoleIO;
            Net = dnet;
            this.ConsoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "CoreMOD", "Constructor called! This debug message proved it."));

        }

        #endregion

        [Command("about"), Summary("Display information about the bot")]
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
        [Command("addcmd"),Summary("Add a command to your bot. If you run this via DM, it will create a global command.")]
        public async Task CMD_Add(string cmdname, bool restricted, [Remainder]string action)
        {
            if (Net.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, Net.pmgr.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            ulong gid = 0;
            if(Context.Guild != null)
            {
                gid = Context.Guild.Id;
            }

            await Net.ccmgr.AddCmd(Context.Message, cmdname, action, restricted,gid);
        }

        [Command("addgcmd"), Summary("Add a global command to your bot")]
        public async Task CMD_AddGlobal(string cmdname, bool restricted, [Remainder]string action)
        {
            if (Net.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, Net.pmgr.GetAccessDeniedMessage(Context,AccessLevels.CommandManager));
                return;
            }
            await Net.ccmgr.AddCmd(Context.Message, cmdname, action, restricted);
        }

        [Command("delcmd"), Summary("Add a command to your bot. If you run this via DM, it will create a global command.")]
        public async Task CMD_Delete(string cmdname)
        {
            if (Net.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, Net.pmgr.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }
            ulong gid = 0;
            if (Context.Guild != null)
            {
                gid = Context.Guild.Id;
            }

            await Net.ccmgr.DelCmd(Context.Message, cmdname, gid);
        }

        [Command("delgcmd"), Summary("Add a command to your bot. If you run this via DM, it will create a global command.")]
        public async Task CMD_DeleteGlobal(string cmdname)
        {
            if (Net.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                await Context.Channel.SendMessageAsync("", false, Net.pmgr.GetAccessDeniedMessage(Context, AccessLevels.CommandManager));
                return;
            }

            await Net.ccmgr.DelCmd(Context.Message, cmdname);
        }

        #endregion

        #region Permission Management
        [Command("permissions set user"),Alias("psu")]
        public async Task PERM_SetUser(IUser user, AccessLevels accessLevel)
        {
            if (Net.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, Net.pmgr.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            try
            {
                int result = Net.pmgr.RegisterEntity(user, accessLevel);
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

        [Command("permissions set role"),RequireContext(ContextType.Guild), Alias("psr")]
        public async Task PERM_SetRole(IRole role, AccessLevels accessLevel)
        {
            if (Net.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, Net.pmgr.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            try
            {
                int result = Net.pmgr.RegisterEntity(role, accessLevel);
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

        [Command("permissions del user"), Alias("pdu","pru")]
        public async Task PERM_DeleteUser(IUser user)
        {
            if (Net.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, Net.pmgr.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            try
            {
                bool result = Net.pmgr.DeleteEntity(user);
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

        [Command("permissions del role"), Alias("pdr", "prr")]
        public async Task PERM_DeleteRole(IRole role)
        {
            if (Net.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, Net.pmgr.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            try
            {
                bool result = Net.pmgr.DeleteEntity(role);
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
        [Command("stopbot",RunMode= RunMode.Async), Alias("stop")]
        public async Task BOT_StopBot()
        {
            if (Net.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, Net.pmgr.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            if (Net.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
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
            Net.Stop(ref Program.ShutdownCalled);
        }

        [Command("restartbot", RunMode = RunMode.Async),Alias("restart")]
        public async Task BOT_RestartBot()
        {
            if (Net.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, Net.pmgr.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }

            EmbedBuilder b = new EmbedBuilder();
            if (Net.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
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
            Net.Stop(ref Program.ShutdownCalled);
        }

        [Command("status")]
        public async Task BOT_SetStatus(string text, string StreamURL="")
        {
            
            if(Net.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
            {
                await Context.Channel.SendMessageAsync("", false, Net.pmgr.GetAccessDeniedMessage(Context, AccessLevels.Administrator));
                return;
            }
            if(text.ToLower().StartsWith("playing "))
            {
                await Net.Client.SetGameAsync(text.Remove(0,8),null);
            }
            if (text.ToLower().StartsWith("watching "))
            {
                await Net.Client.SetGameAsync(text.Remove(0, 9), null,ActivityType.Watching);
            }
            if (text.ToLower().StartsWith("streaming "))
            {
                await Net.Client.SetGameAsync(text.Remove(0, 10), StreamURL, ActivityType.Streaming);
            }
            if (text.ToLower().StartsWith("listening to "))
            {
                await Net.Client.SetGameAsync(text.Remove(0, 13), null, ActivityType.Listening);
            }
        }
        #endregion
    }
}
