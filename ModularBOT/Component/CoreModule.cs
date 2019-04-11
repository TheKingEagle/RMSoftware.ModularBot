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
        DiscordShardedClient Client { get; set; }

        CommandService Cmdsvr { get; set; }

        ConsoleIO consoleIO { get; set; }

        DiscordNET net { get; set; }
        
        public CoreModule(DiscordShardedClient client, CommandService cmdservice, ConsoleIO consoleIO, DiscordNET dnet)
        {
            Client = client;
            Cmdsvr = cmdservice;
            this.consoleIO = consoleIO;
            net = dnet;
            this.consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "CoreMOD", "Constructor called! This debug message proved it."));

        }

        [Command("about"), Summary("Display information about the bot")]
        public async Task ShowAbout()
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

        [Command("addcmd"),Summary("Add a command to your bot. If you run this via DM, it will create a global command.")]
        public async Task AddCmd(string cmdname, bool restricted, [Remainder]string action)
        {
            if (net.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithTitle("Access Denied");
                b.WithAuthor(Context.Client.CurrentUser);
                b.WithDescription("You do not have permission to use this command. Requires `AccessLevel 1` or higher.");
                b.WithColor(Color.Red);
                b.WithFooter("ModularBOT • Core");
                await Context.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
            ulong gid = 0;
            if(Context.Guild != null)
            {
                gid = Context.Guild.Id;
            }

            await net.ccmgr.AddCmd(Context.Message, cmdname, action, restricted,gid);
        }

        [Command("addgcmd"), Summary("Add a global command to your bot")]
        public async Task AddgCmd(string cmdname, bool restricted, [Remainder]string action)
        {
            if (net.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithTitle("Access Denied");
                b.WithAuthor(Context.Client.CurrentUser);
                b.WithDescription("You do not have permission to use this command. Requires `AccessLevel 1` or higher.");
                b.WithColor(Color.Red);
                b.WithFooter("ModularBOT • Core");
                await Context.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
            await net.ccmgr.AddCmd(Context.Message, cmdname, action, restricted);
        }

        [Command("delcmd"), Summary("Add a command to your bot. If you run this via DM, it will create a global command.")]
        public async Task DelCmd(string cmdname)
        {
            if (net.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithTitle("Access Denied");
                b.WithAuthor(Context.Client.CurrentUser);
                b.WithDescription("You do not have permission to use this command. Requires `AccessLevel 1` or higher.");
                b.WithColor(Color.Red);
                b.WithFooter("ModularBOT • Core");
                await Context.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
            ulong gid = 0;
            if (Context.Guild != null)
            {
                gid = Context.Guild.Id;
            }

            await net.ccmgr.DelCmd(Context.Message, cmdname, gid);
        }

        [Command("delgcmd"), Summary("Add a command to your bot. If you run this via DM, it will create a global command.")]
        public async Task DelgCmd(string cmdname)
        {
            if (net.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithTitle("Access Denied");
                b.WithAuthor(Context.Client.CurrentUser);
                b.WithDescription("You do not have permission to use this command. Requires `AccessLevel 1` or higher.");
                b.WithColor(Color.Red);
                b.WithFooter("ModularBOT • Core");
                await Context.Channel.SendMessageAsync("", false, b.Build());
                return;
            }


            await net.ccmgr.DelCmd(Context.Message, cmdname);
        }

        [Command("permissions set user"),Alias("psu")]
        public async Task perm_set_user(IUser user, AccessLevels accessLevel)
        {
            EmbedBuilder b = new EmbedBuilder();
            try
            {
                int result = net.pmgr.RegisterEntity(user, accessLevel);
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
        public async Task perm_set_role(IRole role, AccessLevels accessLevel)
        {
            EmbedBuilder b = new EmbedBuilder();
            try
            {
                int result = net.pmgr.RegisterEntity(role, accessLevel);
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
        public async Task perm_del_user(IUser user)
        {
            EmbedBuilder b = new EmbedBuilder();
            try
            {
                bool result = net.pmgr.DeleteEntity(user);
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
        public async Task perm_del_role(IRole role)
        {
            EmbedBuilder b = new EmbedBuilder();
            try
            {
                bool result = net.pmgr.DeleteEntity(role);
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

        [Command("stopbot",RunMode= RunMode.Async), Alias("stop")]
        public async Task stopbot()
        {
            EmbedBuilder b = new EmbedBuilder();
            if (net.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
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
            net.Stop(ref Program.ShutdownCalled);
        }

        [Command("restartbot", RunMode = RunMode.Async),Alias("restart")]
        public async Task restartbot()
        {
            EmbedBuilder b = new EmbedBuilder();
            if (net.pmgr.GetAccessLevel(Context.User) < AccessLevels.Administrator)
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
            net.Stop(ref Program.ShutdownCalled);
        }
    }
}
