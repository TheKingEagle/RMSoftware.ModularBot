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
        [Command("RESTARTBOT", RunMode = RunMode.Async), Summary("Restart the bot"), RequireOwner, Remarks("[CMDMgmt]")]
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
            await Retry.Do(async () => await Context.Channel.SendMessageAsync("HEY! You don't have permission to do that!"), TimeSpan.FromMilliseconds(140));
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
                    await arg.Channel.SendMessageAsync("", false, Program.ccmg.ViewCmd(cmdTag));
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
            await Retry.Do(async () => await Context.Channel.SendMessageAsync("HEY! You don't have permission to do that!"), TimeSpan.FromMilliseconds(140));
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
            await Retry.Do(async () => await Context.Channel.SendMessageAsync("Hey " + arg.Author.Mention + ", You don't have permission to use this command!"), TimeSpan.FromMilliseconds(140));
            
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
                    await arg.Channel.SendMessageAsync("Command is probably removed, but I threw some kind of error... My master will look into it...");
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

            return;
        }

        [Command("changes"), Summary("Shows what changed in this version")]
        public async Task ShowChanges()
        {
            EmbedBuilder eb = new EmbedBuilder();

            eb.WithAuthor("What's New", Client.CurrentUser.GetAvatarUrl(), "");
            eb.AddField($"v{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)} (CoreScript update part 5)",
                "• Enabled script to have comments\r\n• edited console commands to remove `bot.` prefix. Affected commands: `bot.stopbot -> stopbot; bot.enablecmd -> enablecmd; bot.disablecmd -> disablecmd; bot.status -> status;`");
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

    public class CoreScript
    {
        public CoreScript(ref IServiceProvider _services,ref CommandService _cmdsvr, Dictionary<string,object> dict = null)
        {
            cmdsvr = _cmdsvr;
            services = _services;
            if(dict == null)
            {
                Variables = new Dictionary<string, object>();
            }
            else
            {
                Variables = dict;
            }
        }
        private Dictionary<string, object> Variables { get; set; }
        private CommandService cmdsvr;
        private IServiceProvider services;
        /// <summary>
        /// These are variable names that are defined by the custom commands class.
        /// They are not managed by the CoreScript in any way.
        /// </summary>
        private readonly string[] SystemVars = { "counter", "invoker", "self", "version"};

        #region Public Methods
        public void Set(string var, object value)
        {
            object v = null;
            if(SystemVars.Contains(var))
            {
                throw (new ArgumentException("This variable cannot be modified."));
            }
            bool result = Variables.TryGetValue(var, out v);
            if (!result)
            {
                //add the new variable.
                Variables.Add(var, value);
                return;
            }
            else
            {
                Variables.Remove(var);//remove the old value.
                Variables.Add(var, value);//add the new value.
                return;
            }
        }

        public object Get(string var)
        {
            object v = null;
            bool result = Variables.TryGetValue(var, out v);
            if (!result)
            {
                return null;
            }
            else
            {
                return v;
            }

        }

        public string ProcessVariableString(string response,INIFile CmdDB, string cmd, DiscordSocketClient client, IMessage message)
        {
            
            if (response.Contains("%counter%"))
            {
                int counter = CmdDB.GetCategoryByName(cmd).GetEntryByName("counter").GetAsInteger() + 1;
                CmdDB.GetCategoryByName(cmd).GetEntryByName("counter").SetValue(counter);
                CmdDB.SaveConfiguration();
                response = response.Replace("%counter%", counter.ToString());
            }
            if (response.Contains("%self%"))
            {
              
                response = response.Replace("%self%", client.CurrentUser.Mention);
            }
            if (response.Contains("%invoker%"))
            {
                response = response.Replace("%invoker%", message.Author.Mention);
            }
            if (response.Contains("%version%"))
            {
                response = response.Replace("%version%", Assembly.GetCallingAssembly().GetName().Version.ToString(4));
            }
            //Check for use of Custom defined variables.
            
            foreach (Match item in Regex.Matches(response, @"%[^%]*%", RegexOptions.ExplicitCapture))
            {
                string vname = item.Value.Replace("%", "");
                if(Get(vname) != null)
                {
                    response = response.Replace(item.Value.ToString(), Get(vname).ToString());
                }
            }
            //Final variable.
            return response;
        }

        public async Task EvaluateScript(string response, INIFile CmdDB, string cmd, DiscordSocketClient client, IMessage message)
        {
            int LineInScript = 0;
            bool error = false;
            string errorMessage = "";
            bool terminated = false;
            //For the sake of in-chat scripts, they should be smaller.
            //otherwise they will be saved as a file.
            using (StringReader sr = new StringReader(response))
            {
                while (!error)
                {
                    
                    if (sr.Peek() == -1)
                    {
                        if (!terminated)
                        {
                            error = true;
                            errorMessage = $"SCRIPT ERROR:```The codeblock was not closed.\r\nCoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                            break;
                        }
                    }
                    string line = await sr.ReadLineAsync();

                    
                    if (line == ("```"))
                    {
                        terminated = true;
                        Program.LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", "End of script!"), ConsoleColor.Green);
                        break;
                    }
                    if (LineInScript == 0)
                    {
                        if (line == "```DOS")
                        {
                            LineInScript = 1;
                            continue;
                        }
                        else
                        {
                            error = true;
                            errorMessage = $"SCRIPT ERROR:```\r\nUnexpected header:``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```\r\nAdditional info: Multi-line formatting required.";
                            break;
                        }
                    }
                    if (LineInScript >= 1)
                    {
                        
                        if (line == ("```"))
                        {
                            terminated = true;
                            break;
                        }
                        if(line.ToUpper().StartsWith("::") || line.ToUpper().StartsWith("REM") || line.ToUpper().StartsWith("//"))
                        {
                            //comment line.
                            LineInScript++;
                            continue;
                        }
                        
                        if (line == "```DOS")
                        {
                            error = true;
                            errorMessage = $"SCRIPT ERROR:```\r\nDuplicate header:``` ```{line.Split(' ')[0]}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                            break;
                        }
                        //GET COMMAND PART.
                        string output = "";
                        switch (line.Split(' ')[0].ToUpper())
                        {
                            case ("ECHO"):
                                
                                //Get the line removing echo.
                                output = line.Remove(0, 5);
                                if (string.IsNullOrWhiteSpace(ProcessVariableString(output, CmdDB, cmd, client, message)))
                                {
                                    error = true;
                                    errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                    break;
                                }
                                await message.Channel.SendMessageAsync(ProcessVariableString(output, CmdDB, cmd, client, message), false);
                                
                                break;
                            case ("ECHOTTS"):
                                //Get the line removing echo.
                                output = line.Remove(0, 8);
                                if (string.IsNullOrWhiteSpace(ProcessVariableString(output, CmdDB, cmd, client, message)))
                                {
                                    error = true;
                                    errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                    break;
                                }
                                await message.Channel.SendMessageAsync(ProcessVariableString(output, CmdDB, cmd, client, message), true);
                                
                                break;
                            case ("SETVAR"):
                                caseSetVar(line, ref error, ref errorMessage, ref LineInScript, ref cmd);
                                
                                break;
                            case ("CMD"):
                                SocketMessage m = message as SocketMessage;
                                caseExecCmd(line, ref error, ref errorMessage, ref LineInScript, ref cmd, ref m);
                                
                                break;
                            case ("BOTSTATUS"):
                                await Program._client.SetGameAsync(line.Remove(0, 10));
                                break;
                            case ("STATUSORB"):
                                string cond = line.Remove(0, 10).ToUpper();
                                switch (cond)
                                {
                                    case ("ONLINE"):
                                        await Program._client.SetStatusAsync(UserStatus.Online);
                                        break;
                                    case ("AWAY"):
                                        await Program._client.SetStatusAsync(UserStatus.Idle);
                                        break;
                                    case ("AFK"):
                                        await Program._client.SetStatusAsync(UserStatus.AFK);
                                        break;
                                    case ("BUSY"):
                                        await Program._client.SetStatusAsync(UserStatus.DoNotDisturb);
                                        break;
                                    case ("OFFLINE"):
                                        await Program._client.SetStatusAsync(UserStatus.Offline);
                                        break;
                                    case ("INVISIBLE"):
                                        await Program._client.SetStatusAsync(UserStatus.Invisible);
                                        break;
                                    default:
                                        error = true;
                                        errorMessage = $"SCRIPT ERROR:```\r\nUnexpected Argument: {cond}. Try either ONLINE, BUSY, AWAY, AFK, INVISIBLE, OFFLINE.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        break;
                                }

                                break;
                            case ("BOTGOLIVE"):
                                string[] data = line.Remove(0, 10).Split(' ');
                                if(data.Length < 2)
                                {
                                    error = true;
                                    errorMessage = $"SCRIPT ERROR:```\r\nFunction error: Expected format BOTGOLIVE <ChannelName> <status text>.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                    break;
                                }
                                string statusText = line.Remove(0, 10 + data[0].Length + 1).Trim();
                                
                                await Program._client.SetGameAsync(statusText,$"https://twitch.tv/{data[0]}",StreamType.Twitch);
                                break;
                            case ("WAIT"):
                                int v = 1;
                                if(!int.TryParse(line.Remove(0, 5),out v))
                                {
                                    error = true;
                                    errorMessage = $"SCRIPT ERROR:```\r\nA number was expected here. You gave: {line.Remove(0,5)}\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";

                                    break;
                                }
                                await Task.Delay(v);
                                break;
                            default:
                                error = true;
                                errorMessage = $"SCRIPT ERROR:```\r\nUnexpected core function: {line.Split(' ')[0]}\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";

                                break;
                        }

                    }
                    await Task.Delay(20);
                    LineInScript++;
                }
            }
            if (error)
            {
                await message.Channel.SendMessageAsync(errorMessage);
                return;
            }
        }
        #endregion

        private void caseSetVar(string line, ref bool error, ref string errorMessage, ref int LineInScript, ref string cmd)
        {
            string output = line;
            if (output.Split(' ').Length < 3)
            {
                error = true;
                errorMessage = $"SCRIPT ERROR:```\r\nThe syntax of this function is incorrect.```"
                    + $" ```Function {line.Split(' ')[0]}```" +
                    $"```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                return;
            }
            output = line.Remove(0, 6).Trim();
            string varname = output.Split(' ')[0];
            output = output.Remove(0, varname.Length);
            output = output.Trim();
            try
            {
                Set(varname, output);
            }
            catch (ArgumentException ex)
            {
                error = true;
                errorMessage = $"SCRIPT ERROR:```\r\n{ex.Message}```"
                    + $" ```Function {line.Split(' ')[0]}\r\nVariable name {varname}```\r\n" +
                    $"```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                return;
            }

        }

        private void caseExecCmd(string line, ref bool error, ref string errorMessage, ref int LineInScript, ref string cmd, ref SocketMessage ArgumentMessage)
        {
            string ecmd = line.Remove(0, 4);
            if (Program.ccmg.Process(new PsuedoMessage(ecmd, ArgumentMessage.Author, (ArgumentMessage.Channel as IGuildChannel), MessageSource.User)).GetAwaiter().GetResult())
            {
                Program.LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", line));
                Program.LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", "CustomCMD Success..."));
                return;
            }
            //Damn, I can't be sassy here... If it was a command, but not a ccmg command, then try the context for modules. If THAT didn't work
            //Then it will output the result of the context.
            var context = new CommandContext(Program._client, new PsuedoMessage(ecmd, ArgumentMessage.Author, (ArgumentMessage.Channel as IGuildChannel), MessageSource.User));
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result =  cmdsvr.ExecuteAsync(context, 1, services);
            Program.LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", line));
            Program.LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", result.Result.ToString()));
            if (!result.Result.IsSuccess)
            {
                error = true;
                errorMessage = $"SCRIPT ERROR:\r\n```\r\nCMD function error!\r\nCommandContext returned: {result.Result.ErrorReason}\r\n```\r\n";
                errorMessage += $"```\r\n{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                return;
            }
        }
    }
}
