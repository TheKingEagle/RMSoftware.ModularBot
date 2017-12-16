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
using System.Net.Http;
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
            builder.AddField("Copyright", "Copyright © 2017 RMSoftware Development");
            builder.AddField("Version", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
            builder.WithFooter("RMSoftware.ModularBot, created by rmsoft1");
            await Retry.Do(async () => await Context.Channel.SendMessageAsync("", false, builder.Build()), TimeSpan.FromMilliseconds(140));
        }

        [Command("uptime"),Summary("Displays how long the bot has been connected")]
        public async Task ShowUptime([Remainder]string args=null)
        {
            var delta = DateTime.Now - Program.StartTime;

            string format = string.Format("I've been alive and well for **{0}** hours, **{1}** minutes, and **{2}** seconds!", Math.Floor(delta.TotalHours).ToString("n0"), delta.Minutes, delta.Seconds);
            await Retry.Do(async () => await Context.Channel.SendMessageAsync(format + " " + args), TimeSpan.FromMilliseconds(140));

        }

        [Command("status"), Summary("Set the bot's 'Playing' status"), Remarks("[CMDMgmt]")]
        public async Task setStatus([Remainder]string args = null)
        {
           await Client.SetGameAsync(args);
        }

        [Command("streamstatus"), Summary("Set the bot's status to streaming on twitch, with twitch url and custom text."), Remarks("[CMDMgmt]")]
        public async Task setStatus(string streamurl, [Remainder]string args)
        {
            
            await Client.SetGameAsync(args, streamurl, StreamType.Twitch);
        }

        [Command("STOPBOT",RunMode=RunMode.Async), Summary("Shutdown the bot"), RequireOwner, Remarks("[CMDMgmt]")]
        public async Task StopBot()
        {
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

        [Command("RESTARTBOT", RunMode = RunMode.Async), Summary("Restart the bot"), RequireOwner, Remarks("[CMDMgmt]")]
        public async Task RestartBot()
        {

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

            if (Program.rolemgt.CheckUserRole(user))//If user has a role that is in the database, it is good.
            {
                try
                {
                    Program.LogToConsole("cmdMgmt", "User has a role in cmdMgrDB");

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

                    Program.LogToConsole("CritERR", ex.Message);
                    ConsoleColor Last = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Program.LogToConsole("ExStack\r\n\r\n", ex.StackTrace);
                    Console.ForegroundColor = Last;
                }
                catch (Exception ex)
                {
                    Program.LogToConsole("CritERR", ex.Message);
                    ConsoleColor Last = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Program.LogToConsole("ExStack\r\n\r\n", ex.StackTrace);
                    Console.ForegroundColor = Last;
                }
                return;
            }
            await Retry.Do(async () => await Context.Channel.SendMessageAsync("HEY! You don't have permission to do that!"), TimeSpan.FromMilliseconds(140));
            return;
        }

        [Command("editcmd"), Summary("edit an existing custom command"), RequireContext(ContextType.Guild), Remarks("[CMDMgmt]")]
        public async Task editcmd(string cmdTag, bool newDevCmdOnly, [Remainder]string newaction)
        {
            SocketGuildUser user = ((SocketGuildUser)Context.Message.Author);
            SocketMessage arg = Context.Message as SocketMessage;

            if (Program.rolemgt.CheckUserRole(user))
            {
                try
                {
                    Program.LogToConsole("cmdMgmt", "User has required permissions");

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

                    Program.LogToConsole("CritERR", ex.Message);
                    ConsoleColor Last = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Program.LogToConsole("ExStack\r\n\r\n", ex.StackTrace);
                    Console.ForegroundColor = Last;
                }
                catch (Exception ex)
                {
                    Program.LogToConsole("CritERR", ex.Message);
                    ConsoleColor Last = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Program.LogToConsole("ExStack\r\n\r\n", ex.StackTrace);
                    Console.ForegroundColor = Last;
                }
                return;
            }
            await Retry.Do(async () => await Context.Channel.SendMessageAsync("Hey " + arg.Author.Mention + ", You don't have permission to use this command!"), TimeSpan.FromMilliseconds(140));
            
            return;
        }

        [Command("delcmd"), Summary("Remove specified custom command."), RequireContext(ContextType.Guild), Remarks("[CMDMgmt]")]
        public async Task delcmd(string cmdTag)
        {
            SocketGuildUser user = ((SocketGuildUser)Context.Message.Author);
            SocketMessage arg = Context.Message as SocketMessage;

            if (Program.rolemgt.CheckUserRole(user))
            {
                try
                {
                    Program.ccmg.DeleteCommand(cmdTag);
                    RequestOptions op = new RequestOptions();
                    op.Timeout = 256;
                    op.RetryMode = RetryMode.AlwaysRetry;
                    await Retry.Do(async () => await Context.Channel.SendMessageAsync("Command removed!make sure to save."), TimeSpan.FromMilliseconds(140));
                }
                catch (AggregateException ex)
                {

                    await arg.Channel.SendMessageAsync("The request failed (MANY TIMES) due to an API related http error that I can't sort out right now... please forgive me... (The command probably still got removed though)");

                    Program.LogToConsole("CritERR", ex.Message);
                    ConsoleColor Last = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Program.LogToConsole("ExStack\r\n\r\n", ex.StackTrace);
                    Console.ForegroundColor = Last;
                }
                catch (Exception ex)
                {
                    await arg.Channel.SendMessageAsync("Command is probably removed, but I threw some kind of error... My master will look into it...");

                    Program.LogToConsole("CritERR", ex.Message);
                    ConsoleColor Last = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Program.LogToConsole("ExStack\r\n\r\n", ex.StackTrace);
                    Console.ForegroundColor = Last;
                }
            }

            return;
        }

        [Command("listcmd"), Summary("Shows a list of available commands."), RequireContext(ContextType.Guild)]
        public async Task listCmds()
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.Color = Color.Green;
            builder.Title = "Available commands for: " + Client.CurrentUser.Username;
            CommandList commandList = new CommandList(Client.CurrentUser.Username);
            INICategory[] cmdcats = Program.ccmg.GetAllCommand();


            #region CORE
            foreach (CommandInfo item in cmdsvr.Commands)
            {
                string group = item.Module.Aliases[0] + " ";
                if (string.IsNullOrWhiteSpace(group))
                {
                    group = "";//Command's groupAttribute?
                }

                if (item.Module.Name == "CoreModule")
                {
                    builder.AddField("`" + Program.CommandPrefix + item.Name + "`", item.Summary);
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
                commandList.AddCommand(Program.CommandPrefix + group + item.Name, item.Remarks == "[CMDMgmt]", item.Module.Name == "CoreModule", item.Summary, usage);
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
                        builder.WithFooter("See attachment for all commands.");
                        await Context.Channel.SendMessageAsync("", false, builder.Build());
                        await Context.Channel.SendFileAsync(ms, $"{Context.Guild.Name}_{Context.Client.CurrentUser.Username}_AllCommands.html");

                    }
                }
            }
            catch (AggregateException ex)
            {

                await Context.Channel.SendMessageAsync("Tried to do this THREE different times, and Quite honestly, I just could not do it... I'm sorry...");
                Program.LogToConsole("CritERR", ex.Message);
            }
            return;

        }

        [Command("save"), Summary("Save the command database."), RequireContext(ContextType.Guild), Remarks("[CMDMgmt]")]
        public async Task SaveCmd()
        {
            SocketMessage arg = Context.Message as SocketMessage;
            SocketGuildUser user = ((SocketGuildUser)arg.Author);

            if (Program.rolemgt.CheckUserRole(user))
            {

                Program.LogToConsole("CmdExec", "User has required permissions");
                await arg.Channel.SendMessageAsync("Command DB saved.");
                Program.ccmg.Save();
                
            }

            return;
        }

        [Command("changes"), Summary("Shows what changed in this version")]
        public async Task ShowChanges()
        {
            EmbedBuilder eb = new EmbedBuilder();

            eb.WithAuthor("What's New", Client.CurrentUser.GetAvatarUrl(), "");
            eb.AddField($"v{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)} (Beta release)", "• Fixed a minor open/close glitch in command list html.\r\n• Fixed HTML's title. lel.\r\n• Changed the changes (wow, changing the changes again?) command to always reflect app version\r\n• *Added more sass*");
            eb.WithFooter("RMSoftware.ModularBOT");
            eb.Color = Color.DarkBlue;
            RequestOptions op = new RequestOptions();
            op.RetryMode = RetryMode.AlwaysRetry;
            await Context.Channel.SendMessageAsync("**Full version history/change log: http://rms0.org?a=mbChanges**", false, eb.Build(), op);
        }

        [Command("addmgrole"), Summary("add a role to CommandMGMT database"), RequireContext(ContextType.Guild),RequireOwner, Remarks("[CMDMgmt]")]
        public async Task AddRoletoDB([Remainder] string param = null)
        {
            int added = 0;
            foreach (ulong item in Context.Message.MentionedRoleIds)
            {
                Program.rolemgt.AddCommandManagerRole((SocketRole)Context.Guild.GetRole(item));
                added++;
            }
            await Context.Channel.SendMessageAsync("Successfully added `" + added + "` role(s) to the CommandMGMT database for guild: `"+Context.Guild.Name+"`.");
        }
        [Command("delmgrole"), Summary("Delete a role from CommandMGMT database"), RequireContext(ContextType.Guild), RequireOwner, Remarks("[CMDMgmt]")]
        public async Task DelRoleFromDB([Remainder] string param = null)
        {
            int del = 0;
            foreach (ulong item in Context.Message.MentionedRoleIds)
            {
                Program.rolemgt.DeleteCommandManager((SocketRole)Context.Guild.GetRole(item));
                del++;
            }
            await Context.Channel.SendMessageAsync("Successfully removed `" + del + "` role(s) from the CommandMGMT database for guild: `" + Context.Guild.Name + "`.");
        }
    }
}
