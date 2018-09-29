using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RMSoftware.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace RMSoftware.ModularBot
{
    
    public class CoreModule : ModuleBase
    {

         DiscordSocketClient Client { get; set; }

         CommandService cmdsvr { get; set; }
        public CoreModule(DiscordSocketClient client, CommandService cmdservice)
        {
            Client = client;
            cmdsvr = cmdservice;
        }
        [Command("about"), Summary("Display information about the bot")]
        public async Task ShowAbout()
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "About";
            builder.WithAuthor(Context.Client.CurrentUser);
            builder.Color = Color.Blue;
            builder.Description = "A Multi-purpose, multi-module bot designed for discord. Tailor it for your specific server, create your own modules and plug-ins. Includes a core module for custom text-based commands & EXEC functionality";
            builder.AddField("Copyright", "Copyright © 2017-2018 RMSoftware Development");
            builder.AddField("Version", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
            builder.WithFooter("RMSoftware.ModularBot, created by TheKingEagle");
            await Retry.Do(async () => await Context.Channel.SendMessageAsync("", false, builder.Build()), TimeSpan.FromMilliseconds(140));
        }

        [Command("invitebot"), Summary("Generate a basic invite link to add the bot to your guild.")]
        public async Task ShowInvite()
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "Add this bot to your guild";
            builder.WithAuthor(Context.Client.CurrentUser);
            builder.Color = Color.Purple;
            builder.Description = "Click the link above to add the bot to a guild. You may only add the bot to a guild that you manage. You may not be able to use the link unless you are the bot owner, or the bot is public.";
            builder.AddField("Permissions", "These are the permissions your bot will require. You can manage additional permissions later. Please note: the bot will not function without these permissions enabled:\r\n• Send Messages (Required)\r\n• Attach Files (Required)\r\n• Embed Links (Required)\r\n");
            builder.AddField("Copyright", "Copyright © 2017-2018 RMSoftware Development");
            builder.AddField("Version", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
            builder.WithFooter("RMSoftware.ModularBot, created by TheKingEagle");
            builder.WithUrl($"https://discordapp.com/api/oauth2/authorize?client_id={(await Client.GetApplicationInfoAsync()).Id}&permissions=51200&scope=bot");
            await Retry.Do(async () => await Context.Channel.SendMessageAsync("", false, builder.Build()), TimeSpan.FromMilliseconds(140));
        }

        [Command("uptime"),Summary("Displays how long the bot has been connected")]
        public async Task ShowUptime([Remainder]string args=null)
        {
            var delta = DateTime.Now - Program.StartTime;

            string format = string.Format("I've been alive and well for **{0}** hours, **{1}** minutes, and **{2}** seconds!", Math.Floor(delta.TotalHours).ToString("n0"), delta.Minutes, delta.Seconds);
            await Retry.Do(async () => await Context.Channel.SendMessageAsync(format + " " + args), TimeSpan.FromMilliseconds(140));

        }

        [Command("status"), Summary("Set the bot's 'Playing' status"), Remarks("[CMDMgmt]"), RequireContext(ContextType.Guild)]
        public async Task setStatus([Remainder]string StatusText = null)
        {
          
            if (await Program.rolemgt.CheckUserRole((SocketGuildUser)(Context.User), Client))
            {
                await Client.SetGameAsync(StatusText);
            }
            else
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithColor(Color.Red);
                b.WithAuthor(Client.CurrentUser);
                b.WithTitle("Access Denied!");
                b.WithDescription("Hey! You don't have permission to do this. Ask a bot manager.");
                await Context.Channel.SendMessageAsync("", false, b.Build());
            }
        }

        [Command("streamstatus"), Summary("Set the bot's status to streaming on twitch, with twitch url and custom text."), Remarks("[CMDMgmt]"),RequireContext(ContextType.Guild)]
        public async Task setStatus(string ChannelName, [Remainder]string StatusText)
        {
            if(await Program.rolemgt.CheckUserRole((SocketGuildUser)(Context.User),Client))
            {
                await Client.SetGameAsync(StatusText,$"http://twitch.tv/{ChannelName}", StreamType.Twitch);
            }
            else
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithColor(Color.Red);
                b.WithAuthor(Client.CurrentUser);
                b.WithTitle("Access Denied!");
                b.WithDescription("Hey! You don't have permission to do this. Ask a bot manager.");
                await Context.Channel.SendMessageAsync("", false, b.Build());
            }
            
        }

        [Command("stopbot",RunMode=RunMode.Async), Summary("Shutdown the bot"), RequireOwner, Remarks("[CMDMgmt]")]
        public async Task StopBot()
        {
            if (Program.LOG_ONLY_MODE)
            {
                await Context.Channel.SendMessageAsync("Unable to run command. Origin instance started with `-log_only` parameter. Stop it from console or UI Host instead.");
                return;
            }
            await Context.Channel.SendMessageAsync("**[BotMaster]** called ***StopBot***... *Ending Session*");
                DiscordSocketClient c = (DiscordSocketClient)Context.Client;
                Program.BCMDStarted = false;
                await c.SetGameAsync("");
                await Task.Delay(1000);
                await c.SetStatusAsync(UserStatus.Invisible);
                await Task.Delay(1000);
                Program.BCMDStarted = false;
                await c.StopAsync();
                await Task.Delay(3000);//Allow the bot to shut down fully before telling Main() to scream at user to finger the keyboard to close the console.
                Program.discon = true;
        }

#if DEBUG
        [Command("Session", RunMode = RunMode.Async), Summary("Shutdown the bot with a session error."), RequireOwner, Remarks("[CMDMgmt]")]
        public async Task InvalidateSession()
        {
            if (Program.LOG_ONLY_MODE)
            {
                await Context.Channel.SendMessageAsync("Unable to run command. Origin instance started with `-log_only` parameter. Stop it from console or UI Host instead.");
                return;
            }
            await Context.Channel.SendMessageAsync("`CRITICAL: Instance owner triggered a session invalidation. Immediately terminating session and calling for restart.`");
            await Task.Delay(200);
            await Context.Channel.SendMessageAsync("`CRITICAL: If restart fails, please verify the install & ensure you can properly contact discord API.`");
            await Task.Delay(400);
            await Context.Channel.SendMessageAsync("`CRITICAL: This command is for DEBUG builds only!`");
            DiscordSocketClient c = (DiscordSocketClient)Context.Client;
            Program.BCMDStarted = false;
            await c.SetGameAsync("");
            await Task.Delay(1000);
            await c.SetStatusAsync(UserStatus.Invisible);
            await Task.Delay(1000);
            Program.BCMDStarted = false;
            await c.StopAsync();
            await Task.Delay(3000);//Allow the bot to shut down fully before telling Main() to scream at user to finger the keyboard to close the console.
            Program.discon = true;
            Program.CriticalError = true;
            Program.crashException = new Discord.Net.WebSocketClosedException(4007, "Forced invalidate session.");
        }

        [Command("Throw", RunMode = RunMode.Async), Summary("Shutdown the bot with a session error."), RequireOwner, Remarks("[CMDMgmt]")]
        public async Task crash()
        {
            if (Program.LOG_ONLY_MODE)
            {
                await Context.Channel.SendMessageAsync("Unable to run command. Origin instance started with `-log_only` parameter. Stop it from console or UI Host instead.");
                return;
            }
            await Context.Channel.SendMessageAsync("`CRITICAL: Instance owner triggered a manual crash!`");
            await Task.Delay(200);
            await Context.Channel.SendMessageAsync("`CRITICAL: If restart fails, please verify the install & ensure you can properly contact discord API.`");
            await Task.Delay(400);
            await Context.Channel.SendMessageAsync("`CRITICAL: This command is for DEBUG builds only!`");
            DiscordSocketClient c = (DiscordSocketClient)Context.Client;
            Program.BCMDStarted = false;
            await c.SetGameAsync("");
            await Task.Delay(1000);
            await c.SetStatusAsync(UserStatus.Invisible);
            await Task.Delay(1000);
            Program.BCMDStarted = false;
            await c.StopAsync();
            await Task.Delay(3000);//Allow the bot to shut down fully before telling Main() to scream at user to finger the keyboard to close the console.
            Program.discon = true;
            Program.CriticalError = true;
            Program.crashException = new Exception("THIS IS A MANUAL DEBUG CRASH!");
            throw (Program.crashException);
        }
#endif
        [Command("restartbot", RunMode = RunMode.Async), Summary("Restart the bot"), RequireOwner, Remarks("[CMDMgmt]")]
        public async Task RestartBot()
        {
            if(Program.LOG_ONLY_MODE)
            {
                await Context.Channel.SendMessageAsync("Unable to run command. Origin instance started with `-log_only` parameter. Restart it from console or UI Host instead.");
                return;
            }
                await Context.Channel.SendMessageAsync("**[BotMaster]** called ***RestartBot***... *Ending Session, then restarting the program...*");
                DiscordSocketClient c = (DiscordSocketClient)Context.Client;
                Program.BCMDStarted = false;
                await c.SetGameAsync("");
                await Task.Delay(1000);
                await c.SetStatusAsync(UserStatus.Invisible);
                await Task.Delay(1000);
                Program.BCMDStarted = false;
                await c.StopAsync();
                await Task.Delay(3000);//Allow the bot to shut down fully before telling Main() to scream at user to finger the keyboard to close the console.
                Program.RestartRequested = true;
                Program.discon = true;
            

        }

        [Command("addcmd"), Summary("Add a custom command to the bot"),RequireContext(ContextType.Guild), Remarks("[CMDMgmt]")]
        public async Task AddCmd(string cmdTag, bool DevCmdOnly, bool lockToGuild, [Remainder]string action)
        {
            SocketGuildUser user = ((SocketGuildUser)Context.Message.Author);
            SocketMessage arg = Context.Message as SocketMessage;
            bool result = await Program.rolemgt.CheckUserRole(user, Client);
            if (await Program.rolemgt.CheckUserRole(user,Client))//If user has a role that is in the database, it is good.
            {
                try
                {
                    Program.LogToConsole(new LogMessage(LogSeverity.Info,"CmdMgmt","User has a role in cmdMgrDB"));

                    string tosend = "";
                    if (action.StartsWith(Program.CommandPrefix.ToString()))
                    {
                        tosend = "Haha, you're funny. This bot will not run commands with nested commands. *That's dumb*.";

                        await Retry.Do(async () => await Context.Channel.SendMessageAsync(tosend), TimeSpan.FromMilliseconds(140));
                        return;
                    }
                    if (!lockToGuild)
                    {
                        tosend = Program.ccmg.AddCommand(cmdTag, action, DevCmdOnly);
                    }
                    if (lockToGuild)
                    {
                        tosend = Program.ccmg.AddCommand(cmdTag, action, DevCmdOnly, Context.Guild.Id);
                    }

                    IUserMessage a = await Retry.Do(async () => await Context.Channel.SendMessageAsync(tosend), TimeSpan.FromMilliseconds(140));//This is something to try, if it works, then boom!
                    return;
                }
                catch (System.Net.Http.HttpRequestException ex)
                {

                    await arg.Channel.SendMessageAsync("The request failed due to an API related http error that I can't sort out right now... please forgive me... (The command was most likely added anyway~)");

                    Program.LogToConsole(new LogMessage(LogSeverity.Error,"CritERR",ex.Message,ex));
                }
                catch (Exception ex)
                {
                    Program.LogToConsole(new LogMessage(LogSeverity.Error, "CritERR", ex.Message, ex));
                    ConsoleColor Last = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    
                    Console.ForegroundColor = Last;
                }
                return;
            }
            EmbedBuilder b = new EmbedBuilder();
            b.WithColor(Color.Red);
            b.WithAuthor(Client.CurrentUser);
            b.WithTitle("Access Denied!");
            b.WithDescription("Hey! You don't have permission to do this. Ask a bot manager.");
            await Context.Channel.SendMessageAsync("", false, b.Build());
            return;
        }

        [Command("getcmd"), Summary("Add a custom command to the bot"), RequireContext(ContextType.Guild), Remarks("[CMDMgmt]")]
        public async Task GetCmd(string cmdTag)
        {
            SocketGuildUser user = ((SocketGuildUser)Context.Message.Author);
            SocketMessage arg = Context.Message as SocketMessage;

            if (await Program.rolemgt.CheckUserRole(user, Client))//If user has a role that is in the database, it is good.
            {
                try
                {
                    
                    await arg.Channel.SendMessageAsync("", false, Program.ccmg.ViewCmd(Context,cmdTag));
                    return;
                }
                catch (System.Net.Http.HttpRequestException ex)
                {

                    await arg.Channel.SendMessageAsync("The request failed due to an API related http error that I can't sort out right now... please forgive me... (The command was most likely added anyway~)");

                    Program.LogToConsole(new LogMessage(LogSeverity.Error, "CritERR", ex.Message, ex));
                }
                catch (Exception ex)
                {
                    Program.LogToConsole(new LogMessage(LogSeverity.Error, "CritERR", ex.Message, ex));
                    ConsoleColor Last = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Gray;

                    Console.ForegroundColor = Last;
                }
                return;
            }
            EmbedBuilder b = new EmbedBuilder();
            b.WithColor(Color.Red);
            b.WithAuthor(Client.CurrentUser);
            b.WithTitle("Access Denied!");
            b.WithDescription("Hey! You don't have permission to do this. Ask a bot manager.");
            await Context.Channel.SendMessageAsync("", false, b.Build());
            return;
        }

        [Command("editcmd"), Summary("edit an existing custom command"), RequireContext(ContextType.Guild), Remarks("[CMDMgmt]")]
        public async Task editcmd(string cmdTag, bool newDevCmdOnly, [Remainder]string newaction)
        {
            SocketGuildUser user = ((SocketGuildUser)Context.Message.Author);
            SocketMessage arg = Context.Message as SocketMessage;

            if (await Program.rolemgt.CheckUserRole(user, Client))
            {
                try
                {
                    Program.LogToConsole(new LogMessage(LogSeverity.Info,"CmdMgmt","User has required permission."));

                    string tosend = "";

                    if (newaction.StartsWith(Program.CommandPrefix.ToString()))
                    {
                        tosend = "Haha, you're funny. This bot will not run commands with nested commands. *That's dumb*.";

                        await Retry.Do(async () => await Context.Channel.SendMessageAsync(tosend), TimeSpan.FromMilliseconds(140));
                        return;
                    }
                    tosend = Program.ccmg.EditCommand(cmdTag, newaction, newDevCmdOnly);

                    RequestOptions op = new RequestOptions();
                    op.Timeout = 256;
                    op.RetryMode = RetryMode.AlwaysRetry;
                    IUserMessage a = await Retry.Do(async () => await Context.Channel.SendMessageAsync(tosend), TimeSpan.FromMilliseconds(140));
                    return;
                }
                catch (AggregateException ex)
                {

                    await arg.Channel.SendMessageAsync("The request failed (MANY TIMES) due to an API related http error that I can't sort out right now... please forgive me... (The command was most likely changed anyway~)");

                    Program.LogToConsole(new LogMessage(LogSeverity.Error,"CritERR",ex.Message,ex));
                    ConsoleColor Last = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = Last;
                }
                catch (Exception ex)
                {
                    Program.LogToConsole(new LogMessage(LogSeverity.Error, "CritERR", ex.Message, ex));
                    ConsoleColor Last = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = Last;
                }
                return;
            }
            EmbedBuilder b = new EmbedBuilder();
            b.WithColor(Color.Red);
            b.WithAuthor(Client.CurrentUser);
            b.WithTitle("Access Denied!");
            b.WithDescription("Hey! You don't have permission to do this. Ask a bot manager.");
            await Context.Channel.SendMessageAsync("", false, b.Build());
            return;
        }

        [Command("delcmd"), Summary("Remove specified custom command."), RequireContext(ContextType.Guild), Remarks("[CMDMgmt]")]
        public async Task delcmd(string cmdTag)
        {
            SocketGuildUser user = ((SocketGuildUser)Context.Message.Author);
            SocketMessage arg = Context.Message as SocketMessage;

            if (await Program.rolemgt.CheckUserRole(user, Client))
            {
                try
                {

                    Program.ccmg.DeleteCommand(cmdTag);
                    RequestOptions op = new RequestOptions();
                    op.Timeout = 256;
                    op.RetryMode = RetryMode.AlwaysRetry;
                    await Retry.Do(async () => await Context.Channel.SendMessageAsync("Command removed! Please make sure to save."), TimeSpan.FromMilliseconds(140));
                }
                catch (AggregateException ex)
                {

                    await arg.Channel.SendMessageAsync("The request failed (MANY TIMES) due to an API related http error that I can't sort out right now... please forgive me... (The command probably still got removed though)");

                    Program.LogToConsole(new LogMessage(LogSeverity.Error, "CritERR", ex.Message, ex));
                    ConsoleColor Last = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = Last;
                }
                catch (Exception ex)
                {
                    EmbedBuilder b = new EmbedBuilder();
                    b.WithColor(Color.Red);
                    b.WithAuthor(Client.CurrentUser);
                    b.WithTitle("Error?!");
                    b.WithDescription($"Something went horrribly wrong. I can try that again, but @{(await Client.GetApplicationInfoAsync()).Owner.Username}#{(await Client.GetApplicationInfoAsync()).Owner.Discriminator} should probably look into this.");
                    await Context.Channel.SendMessageAsync("", false, b.Build());
                    Program.LogToConsole(new LogMessage(LogSeverity.Error, "CritERR", ex.Message, ex));
                    ConsoleColor Last = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = Last;
                }
            }

            return;
        }

        [Command("listcmd"), Summary("Shows a list of available commands."), RequireContext(ContextType.Guild)]
        public async Task listCmds()
        {
            CommandList commandList = new CommandList(Client.CurrentUser.Username);
            INICategory[] cmdcats = Program.ccmg.GetAllCommand();
            
#region CORE
            foreach (CommandInfo item in cmdsvr.Commands)
            {
                string group = item.Module.Aliases[0] + " ";
                string sum = item.Summary;
                if (string.IsNullOrWhiteSpace(group))
                {
                    group = "";//Command's groupAttribute?
                }

                if (item.Module.Name == "CoreModule")
                {
                    
                    if (string.IsNullOrEmpty(sum))
                    {
                        sum = "No summary was provided.";
                    }
                    
                }
                string usage = Program.CommandPrefix + group + item.Name + " ";
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
                commandList.AddCommand(Program.CommandPrefix + group + item.Name, item.Remarks == "[CMDMgmt]", item.Module.Name == "CoreModule", sum, usage);
            }
#endregion

#region Custom Commands
            foreach (INICategory item in cmdcats)
            {
                if (item.CheckForEntry("guildID"))
                {
                    if (item.GetEntryByName("guildID").GetAsUlong() != Context.Guild.Id)
                    {
                        continue;//if the entry exists, and it doesn't match the guild listcmd was called in, don't add it to the list.
                    }

                }
                string commandSummary = "";
                string usage = "";
                if (item.CheckForEntry("summary"))
                {
                    commandSummary = item.GetEntryByName("summary").GetAsString();

                }
                if (item.CheckForEntry("usage"))
                {
                    usage = item.GetEntryByName("usage").GetAsString();

                }
                commandList.AddCommand(Program.CommandPrefix + item.Name, item.GetEntryByName("restricted").GetAsBool(), false, commandSummary, usage);

            }
#endregion

            try
            {

                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(ms))
                    {

                        sw.WriteLine(commandList.GetFullHTML());
                        sw.Flush();
                        ms.Position = 0;
                        await Context.Channel.SendMessageAsync("**See the attached web document for a full list of commands.**", false);
                        
                        await Context.Channel.SendFileAsync(ms, $"{Context.Guild.Name}_{Context.Client.CurrentUser.Username}_AllCommands.html");

                    }
                }




            }
            catch (AggregateException ex)
            {

                await Context.Channel.SendMessageAsync("Tried to do this THREE different times, and Quite honestly, I just could not do it... I'm sorry...");
                Program.LogToConsole(new LogMessage(LogSeverity.Error, "CritERR", ex.Message, ex));
            }
            return;

        }

        [Command("save"), Summary("Save the command database."), RequireContext(ContextType.Guild), Remarks("[CMDMgmt]")]
        public async Task SaveCmd()
        {


            SocketMessage arg = Context.Message as SocketMessage;
            SocketGuildUser user = ((SocketGuildUser)arg.Author);

            if (await Program.rolemgt.CheckUserRole(user, Client))
            {

                Program.LogToConsole(new LogMessage(LogSeverity.Info, "CmdEXEC","User has required permission"));
                await arg.Channel.SendMessageAsync("Command DB saved.");
                Program.ccmg.Save();
                
            }
            else
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithColor(Color.Red);
                b.WithAuthor(Client.CurrentUser);
                b.WithTitle("Access Denied!");
                b.WithDescription("Hey! You don't have permission to do this. Ask a bot manager.");
                await Context.Channel.SendMessageAsync("", false, b.Build());
            }

            return;
        }

        [Command("changes"), Summary("Shows what changed in this version")]
        public async Task ShowChanges()
        {
            EmbedBuilder eb = new EmbedBuilder();

            eb.WithAuthor("What's New", Client.CurrentUser.GetAvatarUrl(), "");
            eb.AddField($"v{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)} Managment Update",
                $"• CHANGED `GETCMD`. You can nolonger view a command from any guild other than the guildID set in config if that command was set to be locked to guild.\r\n   "+
                $"\u25cb FIXED: `Null Reference exception` when trying to `GETCMD` with commands that were unlocked to ALL guilds.\r\n" +
                $"• ADDED core command: !invitebot.\r\n• Converted core commands to lowercase.\r\n"+
                $"• ADDED config option `initLogo=<LocalImagePath>` to allow custom start logo.\r\n   "+
                $"\u25cb First-time Setup wizard will start before splash screen as a result.\r\n"+
                $"• FIXED `Unhandled exception: parameter cannot be null value` when a CoreScript error took place in `OnStart.CORE`\r\n" +
                $"• RENAMED `Command` Parameter in CoreScript error details to `Execution Context`\r\n"+
                $"• FIXED Console log. Messages should no longer corrupt the layout.");
                
            eb.WithFooter("RMSoftware.ModularBOT");
            eb.Color = Color.DarkBlue;
            RequestOptions op = new RequestOptions();
            op.RetryMode = RetryMode.AlwaysRetry;
            await Context.Channel.SendMessageAsync("**Full version history/change log: http://rms0.org?a=mbChanges**", false, eb.Build(), op);
        }

        [Command("shadowban"), RequireContext(ContextType.Guild), RequireOwner, Remarks("[CMDMgmt]")]
        public async Task IgnoreUser(params IUser[] users)
        {
            int count = 0;
            foreach (var item in users)
            {
                if (Program.rolemgt.AddUserToBlacklist(item as SocketUser))
                {
                    count++;
                }
            }
            await Context.Channel.SendMessageAsync($"Added `{count}` user(s) to the blacklist. they can no longer access my commands.");
        }

        [Command("shadowunban"), RequireContext(ContextType.Guild), RequireOwner, Remarks("[CMDMgmt]")]
        public async Task UnIgnoreUser(params IUser[] users)
        {
            int count = 0;
            foreach (var item in users)
            {
                if (Program.rolemgt.DeleteUserFromBlacklist(item as SocketUser))
                {
                    count++;
                }
            }
            await Context.Channel.SendMessageAsync($"Removed `{count}` user(s) from the blacklist. they can now access my commands.");
        }

        [Command("addmgrole"), Summary("add a role to CommandMGMT database"), RequireContext(ContextType.Guild),RequireOwner, Remarks("[CMDMgmt]")]
        public async Task AddRoletoDB(params IRole[] roles)
        {
            foreach (var item in roles)
            {
                Program.rolemgt.AddCommandManagerRole(item as SocketRole);
            }
            await Context.Channel.SendMessageAsync($"Successfully added `{roles.Length}` role(s) to the CommandMGMT database for guild: `{Context.Guild.Name}`.");
        }

        [Command("delmgrole"), Summary("Delete a role from CommandMGMT database"), RequireContext(ContextType.Guild), RequireOwner, Remarks("[CMDMgmt]")]
        public async Task DelRoleFromDB(params IRole[] roles)
        {
            foreach (var item in roles)
            {
                Program.rolemgt.DeleteCommandManager(item as SocketRole);
            }
            await Context.Channel.SendMessageAsync($"Successfully removed `{roles.Length}` role(s) from the CommandMGMT database for guild: `{Context.Guild.Name}`.");
        }

        [Command("listmgrole",RunMode=RunMode.Async), Summary("lists the authorized management roles for the guild where the command was called."),
            RequireContext(ContextType.Guild), RequireOwner, Remarks("[CMDMgmt]")]
        public async Task listmgroledbguild()
        {
            await Context.Channel.SendMessageAsync($"This could take a while...");
            await Task.Delay(500);
            using (Context.Channel.EnterTypingState())
            {
                SocketRole[] items = Program.rolemgt.GetRolesForGuild(Context.Guild as SocketGuild);
                string roles = "";
                foreach (var item in items)
                {
                    roles += $"{item.Id} - {item.Name}{Environment.NewLine}";
                }
                await Context.Channel.SendMessageAsync($"All authorized management roles for `{Context.Guild.Name}`:\r\n```\r\n{roles}\r\n```");
            }
                
        }

        [Command("listallmgroles", RunMode = RunMode.Async), Summary("lists the authorized management roles for all guilds"), 
            RequireContext(ContextType.Guild), RequireOwner, Remarks("[CMDMgmt]")]
        public async Task listmgroledball()
        {
            int zint = 0;
            await Context.Channel.SendMessageAsync($"This could take a while...");
            await Task.Delay(500);
            using (Context.Channel.EnterTypingState())
            {
                
                SocketRole[] items = Program.rolemgt.GetAllRoles();
                List<string> book = new List<string>();
                string roles = "";
                foreach (var item in items)
                {
                    string toAddTo = $"{item.Id} - {item.Name}{Environment.NewLine}";
                    int len = roles.Length + toAddTo.Length;

                    if(len >600)
                    {
                        len = 0;
                        book.Add(new String(roles.ToCharArray()));//This is probably super dumb, but I want to add the string to the list and change the roles string without affecting the entry in the list. Im doing this to be safe.
                        roles = "";

                    }
                    roles += $"{item.Guild.Name} - {item.Id} - {item.Name}{Environment.NewLine}";
                }
                book.Add(roles);
                foreach (string item in book)
                {
                    if(zint <1)
                    {
                        await Context.Channel.SendMessageAsync($"All authorized management roles in the database:\r\n```\r\n{item}\r\n```");
                    }
                    if (zint >= 1)
                    {
                        await Context.Channel.SendMessageAsync($"```\r\n{item}\r\n```");
                    }
                    zint++;

                    SocketGuildUser r = Context.User as SocketGuildUser;

                    string display = r.Nickname ?? r.Username;
                }
            }

        }

    }

   
}
