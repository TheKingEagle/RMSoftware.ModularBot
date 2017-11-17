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

namespace RMSoftware.ModularBot
{
    public class CoreModule : ModuleBase
    {

        [Command("uptime"),Summary("Displays how long the bot has been connected")]
        public async Task ShowUptime([Remainder]string args=null)
        {
            var delta = DateTime.Now - Program.StartTime;

            string format = string.Format("I've been alive and well for **{0}** hours, **{1}** minutes, and **{2}** seconds!", Math.Floor(delta.TotalHours).ToString("n0"), delta.Minutes, delta.Seconds);
            await Retry.Do(async () => await Context.Channel.SendMessageAsync(format + " " + args), TimeSpan.FromMilliseconds(140));

        }
        [Command("about"), Summary("Display information about the bot")]
        public async Task ShowAbout()
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "About";
            builder.Color = Color.Blue;
            builder.Description = "A Multi-purpose, multi-module bot designed for discord. Tailor it for your specific server, create your own modules and plug-ins. Includes a core module for custom text-based commands & EXEC functionality";
            builder.AddField("Copyright", "Copyright © 2017 RMSoftware Development");
            builder.AddField("Version", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
            builder.WithFooter("RMSoftwareModularBot, created by rmsoft1");
            await Retry.Do(async () => await Context.Channel.SendMessageAsync("", false, builder.Build()), TimeSpan.FromMilliseconds(140));
        }

        [Command("STOPBOT",RunMode=RunMode.Async), Summary("[BotMaster] closes the bot")]
        public async Task StopBot()
        {
            if (Context.Guild == null)
            {
                await Context.Channel.SendMessageAsync("This command is locked to a specific guild... My DM is no exception to the rule.");
            }
            if (Context.Guild?.Id != Program.MainCFG.GetCategoryByName("Application").GetEntryByName("masterGuild").GetAsUlong())
            {
                await Context.Channel.SendMessageAsync("You cannot use that command on this guild server...");
                return;
            }
            SocketGuildUser user = ((SocketGuildUser)Context.User);
            bool hasrole = false;
            foreach (SocketRole role in user.Roles)
            {
                hasrole = false;
                if (role.Name == "BotMaster")
                {

                    Program.LogToConsole("CmdExec","User has specific role");
                    hasrole = true;
                    break;
                }
            }
            if (hasrole)
            {
                await Context.Channel.SendMessageAsync("[BotMaster] called ***StopBot***... g'day m8 ;)");
                DiscordSocketClient c = (DiscordSocketClient)Context.Client;
                await c.SetStatusAsync(UserStatus.Invisible);
                await c.StopAsync();
                await Task.Delay(3000);//Allow the bot to shut down fully before telling Main() to scream at user to finger the keyboard to close the console.
                Program.discon = true;
            }
            else
            {
                await Context.Channel.SendMessageAsync("You don't have the *BotMaster* role...");
            }

        }

        [Command("reloadModules"), Summary("[DevCommand] Reload all modules & commands")]
        public async Task Reload()
        {
            bool hasrole = false;
            SocketGuildUser user = ((SocketGuildUser)Context.User);
            foreach (SocketRole role in user.Roles)
            {
                hasrole = false;
                if (role.Name == "DevCommand")
                {
                    Program.LogToConsole("CmdExec", "User has required permissions");
                    await Program.LoadModules();

                    await Context.Channel.SendMessageAsync("Reloading command modules!");
                    hasrole = true;
                    break;
                }
            }
            if (!hasrole)
            {
                await Context.Channel.SendMessageAsync("Hey " + Context.User.Mention + ", You don't have permission to use this command!");
            }
            return;
        }

        [Command("addcmd"), Summary("[DevCommand] Add a custom command to the bot")]
        public async Task AddCmd(string cmdTag, bool DevCmdOnly, bool lockToGuild, [Remainder]string action)
        {
            SocketGuildUser user = ((SocketGuildUser)Context.Message.Author);
            SocketMessage arg = Context.Message as SocketMessage;
            foreach (SocketRole role in user.Roles)
            {

                if (role.Name == "DevCommand")
                {
                    try
                    {
                        Program.LogToConsole("CmdExec", "User has required permissions");

                        string tosend = "";
                        if (action.StartsWith("!"))
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
                            if (Context.Guild == null)
                            {
                                await Retry.Do(async () => await Context.Channel.SendMessageAsync("If you want to restrict a command to run on a specific guild, you have to create the command from that guild."),TimeSpan.FromMilliseconds(140));
                                return;
                            }

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
            }
            await Retry.Do(async () => await Context.Channel.SendMessageAsync("HEY! You don't have permission to do that!"), TimeSpan.FromMilliseconds(140));
            return;
        }

        [Command("editcmd"), Summary("[DevCommand] Add a custom command to the bot")]
        public async Task editcmd(string cmdTag, bool newDevCmdOnly, [Remainder]string newaction)
        {
            SocketGuildUser user = ((SocketGuildUser)Context.Message.Author);
            SocketMessage arg = Context.Message as SocketMessage;
            foreach (SocketRole role in user.Roles)
            {

                if (role.Name == "DevCommand")
                {
                    try
                    {
                        Program.LogToConsole("CmdExec", "User has required permissions");

                        string tosend = "";

                        if(newaction.StartsWith("!"))
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
            }
            await Retry.Do(async () => await Context.Channel.SendMessageAsync("Hey " + arg.Author.Mention + ", You don't have permission to use this command!"), TimeSpan.FromMilliseconds(140));
            
            return;
        }

        [Command("delcmd"), Summary("[DevCommand] Remove specified custom command.")]
        public async Task delcmd(string cmdTag)
        {
            SocketGuildUser user = ((SocketGuildUser)Context.Message.Author);
            SocketMessage arg = Context.Message as SocketMessage;
            foreach (SocketRole role in user.Roles)
            {
                if (role.Name == "DevCommand")
                {
                    try
                    {

                        Program.ccmg.DeleteCommand(cmdTag);
                        RequestOptions op = new RequestOptions();
                        op.Timeout = 256;
                        op.RetryMode = RetryMode.AlwaysRetry;
                        await Retry.Do(async () => await Context.Channel.SendMessageAsync("Command removed!make sure to save."), TimeSpan.FromMilliseconds(140));
                        
                        break;
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
            }

            return;
        }

        [Command("listcmd"), Summary("[DevCommand] Shows a list of available commands.")]
        public async Task listCmds()
        {
            SocketMessage arg = Context.Message as SocketMessage;
            SocketGuildUser user = ((SocketGuildUser)arg.Author);
            foreach (SocketRole role in user.Roles)
            {
                if (role.Name == "DevCommand")
                {

                    Program.LogToConsole("CmdExec", "User has required permissions");
                    //embed builder...
                    EmbedBuilder builder = new EmbedBuilder();
                    builder.Color = Color.Green;
                    string cmdlist = "";
                    string modulecmds = "";

                    foreach (INICategory item in Program.ccmg.GetAllCommand())
                    {
                        if (item.CheckForEntry("guildID"))
                        {
                            if (item.GetEntryByName("guildID").GetAsUlong() != Context.Guild.Id)
                            {
                                continue;//if the entry exists, and it doesn't match the guild listcmd was called in, don't add it to the list.
                            }

                        }
                        string restricted = item.GetEntryByName("restricted").GetAsBool() ? "[DevCommand]" : "";
                        cmdlist += "`!" + item.Name + "` " + restricted + "\r\n";

                    }
                    foreach (CommandInfo item in Program.cmdsvr.Commands)
                    {
                        string group = item.Module.Aliases[0] + " ";
                        if (string.IsNullOrWhiteSpace(group))
                        {
                            group = "";//Command's groupAttribute?
                        }
                        modulecmds += "`!" + group + "" + item.Name + "`" + " " + item.Summary + "\r\n";
                    }

                    builder.AddField("**Core & Module Commands**", modulecmds);
                    builder.AddField("**Custom Commands**", cmdlist);
                    builder.WithAuthor("Command List");
                    builder.Description = "These are the available commands for the bot.";
                    builder.WithFooter("Powered by RMSoftwareModules DevBOT");
                    try
                    {
                        await Retry.Do(async () => await Context.Channel.SendMessageAsync("", false, builder.Build()), TimeSpan.FromMilliseconds(140));
                    }
                    catch (AggregateException ex)
                    {

                        await arg.Channel.SendMessageAsync("Tried to do this THREE different times, and Quite honestly, I just could not do it... I'm sorry...");
                        Program.LogToConsole("CritERR", ex.Message);
                    }
                    
                    

                    return;
                }

            }
            await Retry.Do(async () => await Context.Channel.SendMessageAsync("Hey " + arg.Author.Mention + ", You don't have permission to use this command!"), TimeSpan.FromMilliseconds(140));
           

            return;
        }

        [Command("save"), Summary("[DevCommand] Save the command database.")]
        public async Task SaveCmd()
        {


            SocketMessage arg = Context.Message as SocketMessage;
            SocketGuildUser user = ((SocketGuildUser)arg.Author);
            foreach (SocketRole role in user.Roles)
            {
                if (role.Name == "DevCommand")
                {

                    Program.LogToConsole("CmdExec", "User has required permissions");
                    await arg.Channel.SendMessageAsync("Command DB saved.");
                    Program.ccmg.Save();
                    break;
                }
            }

            return;
        }

        [Command("changes")]
        public async Task ShowChanges()
        {
            EmbedBuilder eb = new EmbedBuilder();

            eb.WithAuthor("What's New", "https://cdn.discordapp.com/app-icons/350413323180834818/dc9bbd8d4ba0beb5e148de4279db0080.png", "");
            eb.AddField("v1.3.269 (1.4.0-PRERELEASE)", "• Re-wrote StopBot to function without blocking gateway task.\r\n• Removed `!crash`\r\n•Removed logout event and replaced with disconnect event\r\n•Added instruction to prompt for application termination when the bot fails to resume a previous connection to the discord gateway.");
            eb.WithFooter("Powered by: RMSoftware.ModularBot\r\n Copyright © 2017 RMSoftware Development");
            eb.Color = Color.DarkBlue;
            RequestOptions op = new RequestOptions();
            op.RetryMode = RetryMode.AlwaysRetry;
            await Context.Channel.SendMessageAsync("**Full version history/change log: http://rms0.org?a=mbChanges**", false, eb.Build(), op);
        }



    }
}
