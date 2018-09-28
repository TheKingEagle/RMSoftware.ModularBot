using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RMSoftware.IO;
using Discord.WebSocket;
using Discord;
using System.Runtime.InteropServices;
using System.Reflection;
using Discord.Commands;
using System.Text.RegularExpressions;

namespace RMSoftware.ModularBot
{
    /// <summary>
    /// This is a twitch style command manager
    /// </summary>
    public class CustomCommandManager
    {

        public INIFile CmdDB = new INIFile("commands.ini");
        public CoreScript scriptService;
        CommandService _cmdsvc;
        public CustomCommandManager(bool logonly,char cmdprefix,ConsoleLogWriter writer, ref CommandService cmdsvr,ref IServiceProvider services)
        {
             scriptService = new CoreScript(logonly,cmdprefix,writer,this,ref services, ref cmdsvr);
            _cmdsvc = cmdsvr;
        }
        
        /// <summary>
        /// Adds a command to the bot.
        /// </summary>
        /// <param name="Command">command tag (without !)</param>
        /// <param name="Action">The command response/action</param>
        /// <param name="Restricted">If restricted, only people with roles that are whitelisted in the rolemgmt's database can use the command.</param>
        public string AddCommand(string Command, string Action, bool Restricted)
        {
            if (CmdDB.CheckForCategory(Command.Replace(Program.CommandPrefix.ToString(), "")))
            {
                return "That command already exists!";
            }
            CmdDB.CreateCategory(Command.Replace(Program.CommandPrefix.ToString(), ""));
            if(Action.Contains("%counter%"))
            {
                CmdDB.CreateEntry(Command.Replace(Program.CommandPrefix.ToString(), ""), "counter", 0);
            }
            CmdDB.CreateEntry(Command.Replace(Program.CommandPrefix.ToString(), ""), "action", Action.Replace("\r","\\r").Replace("\n","\\n"));
            CmdDB.CreateEntry(Command.Replace(Program.CommandPrefix.ToString(), ""), "restricted", Restricted);
            return "Command added to the DB. Please remember to save.";
        }
        public string EditCommand(string Command, string newAction, bool Restricted)
        {
            if (!CmdDB.CheckForCategory(Command.Replace(Program.CommandPrefix.ToString(), "")))
            {
                return "That command does not exists!";
            }

            CmdDB.GetCategoryByName(Command.Replace(Program.CommandPrefix.ToString(), "")).GetEntryByName("action").SetValue(newAction.Replace("\r", "\\r").Replace("\n", "\\n"));
            CmdDB.GetCategoryByName(Command.Replace(Program.CommandPrefix.ToString(), "")).GetEntryByName("restricted").SetValue(Restricted);
            return "Command edited. Please remember to save.";
        }
        public Embed ViewCmd(ICommandContext Context, string Command)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithAuthor(Program._client.CurrentUser);
            builder.WithTitle("Command Info");
            if (!CmdDB.CheckForCategory(Command.Replace(Program.CommandPrefix.ToString(), "")))
            {
                string cmd = Command.Replace(Program.CommandPrefix.ToString(), "");
                var c = _cmdsvc.Commands.FirstOrDefault(x => x.Name == cmd);
                if(c == null)
                {
                    builder.WithColor(Color.Red);
                    builder.WithDescription("This command does not exist.");
                    builder.AddField("More info:", $"The command you requested was not found in the custom database, or in any loaded modules.\r\nIf you just created the command, run `{Program.CommandPrefix}save` and try again.\r\nIf you just added a new module, Restart the bot.\r\n\r\nTo check for a list of commands run `{Program.CommandPrefix.ToString()}listcmd`");
                    return builder.Build();
                }

                else
                {
                    
                    builder.WithColor(Color.Blue);
                    builder.WithDescription($"This is the basic breakdown of the command: `{Program.CommandPrefix.ToString()}{cmd}`.");
                    builder.AddField("Command Summary", c.Summary ?? "`Not specified.`");
                    string param = $"{Program.CommandPrefix}{c.Name} ";
                    foreach (var item in c.Parameters)
                    {
                        if (item.IsOptional)
                        {
                            param += $"[{item.Name}] ";
                        }
                        else
                        {
                            param += $"<{item.Name}> ";
                        }
                    }
                    if (string.IsNullOrWhiteSpace(param)) { param = "`No parameters.`"; }
                    builder.AddField("Command Usage", $"`{param.Trim()}`", false);

                    builder.AddField("From Module", c.Module.Name ?? "`Unknown module.`");
                    builder.AddField("Remarks", c.Remarks ?? "`Not specified.`");
                   
                    string preconds = "";
                    foreach (var item in c.Preconditions)
                    {
                        preconds += "• " + item.ToString().Substring(item.ToString().LastIndexOf('.')+1) + "\r\n";
                    }
                    if (string.IsNullOrWhiteSpace(preconds)) { preconds = "`No Preconditions.`"; }
                    builder.AddField("Preconditions", preconds,true);
                    string aliases = "";
                    foreach (var item in c.Aliases)
                    {
                        aliases += "• " +Program.CommandPrefix.ToString()+ item.ToString() + "\r\n";
                    }
                    if (string.IsNullOrWhiteSpace(aliases)) { aliases = "`No aliases.`"; }
                    builder.AddField("Command Aliases", aliases,true);
                    return builder.Build();
                }
                
            }
            if(CmdDB.GetCategoryByName(Command.Replace(Program.CommandPrefix.ToString(), "")).CheckForEntry("guildID"))
            {

                if (Program._client.GetGuild(CmdDB.GetCategoryByName(Command.Replace(Program.CommandPrefix.ToString(), "")).GetEntryByName("guildID").GetAsUlong()) != Context.Guild)
                {
                    builder.WithColor(Color.Red);
                    builder.WithDescription("The command is not available.");
                    builder.AddField("More info:", $"The command you requested was created with the `LockToGuild` property. You may only view or execute the command from the guild it was created in.");
                    return builder.Build();
                }
            }
            builder.WithColor(Color.Blue);
            builder.WithDescription($"This is the basic breakdown of the command: `{Program.CommandPrefix.ToString()}{Command.Replace(Program.CommandPrefix.ToString(), "")}`.");


            bool hasCounter = CmdDB.GetCategoryByName(Command.Replace(Program.CommandPrefix.ToString(), "")).CheckForEntry("counter");
            bool locked = CmdDB.GetCategoryByName(Command.Replace(Program.CommandPrefix.ToString(), "")).CheckForEntry("guildID");
            builder.AddField("Has Role Restrictions: ",CmdDB.GetCategoryByName(Command.Replace(Program.CommandPrefix.ToString(), "")).GetEntryByName("restricted").GetAsString());
            builder.AddField("Has Guild Restrictions: ",locked);
            if(locked)
            {
                builder.AddField("What guild can use this command: ", Program._client.GetGuild(CmdDB.GetCategoryByName(Command.Replace(Program.CommandPrefix.ToString(), "")).GetEntryByName("guildID").GetAsUlong()).Name);
                
            }

            builder.AddField("Has counter: ",hasCounter );
            if(hasCounter)
            {
                builder.AddField("usage count: ", CmdDB.GetCategoryByName(Command.Replace(Program.CommandPrefix.ToString(), "")).GetEntryByName("counter").GetAsString());
            }

            builder.AddField("Response/Action: ", CmdDB.GetCategoryByName(Command.Replace(Program.CommandPrefix.ToString(), "")).GetEntryByName("action").GetAsString().Replace("\\r", "\r").Replace("\\n", "\n"));
            return builder.Build();
        }
        public string AddCommand(string Command, string Action, bool Restricted, ulong guildID)
        {
            if (CmdDB.CheckForCategory(Command.Replace(Program.CommandPrefix.ToString(), "")))
            {
                return "That command already exists!";
            }
            if (Action.Contains("%counter%"))
            {
                CmdDB.CreateEntry(Command.Replace(Program.CommandPrefix.ToString(), ""), "counter", 0);
            }
            CmdDB.CreateCategory(Command.Replace(Program.CommandPrefix.ToString(), ""));
            CmdDB.CreateEntry(Command.Replace(Program.CommandPrefix.ToString(), ""), "action", Action.Replace("\r", "\\r").Replace("\n", "\\n"));
            CmdDB.CreateEntry(Command.Replace(Program.CommandPrefix.ToString(), ""), "restricted", Restricted);
            CmdDB.CreateEntry(Command.Replace(Program.CommandPrefix.ToString(), ""), "guildID", guildID);
            return "Command added to the DB. Please remember to save.";
        }

        public void Save()
        {
            CmdDB.SaveConfiguration();
        }
        public void DeleteCommand(string Command)
        {
            CmdDB.DeleteCategory(Command.Replace(Program.CommandPrefix.ToString(), ""));
        }
        public bool successful = false;
        /// <summary>
        /// Process user input for custom command module
        /// </summary>
        /// <param name="arg"></param>
        public async Task<bool> Process(IMessage arg)//Arg Change
        {

            string content = arg.Content;
            bool hasrole = false;
            bool IsTTS = false;
            int argPos = 1;
            if (!arg.Content.StartsWith(Program.CommandPrefix.ToString())) return false;
            //substring the text into two parts.
            try
            {
                
                string cmd = content.Substring(argPos).Split(' ')[0].ToLower();//get the command bit. To lowercase because it doesnt matter.
                if(cmd.EndsWith(".tts"))
                {
                    SocketGuildUser a = arg.Author as SocketGuildUser;
                    if(a == null)
                    {
                        return false;
                    }
                    if(await Program.rolemgt.CheckUserRole(a, Program._client))
                    {
                        IsTTS = true;
                        cmd = cmd.Remove(cmd.Length - 4);
                    }
                    else
                    {
                        EmbedBuilder b = new EmbedBuilder();
                        b.WithColor(Color.Red);
                        b.WithAuthor(Program._client.CurrentUser);
                        b.WithTitle("Access Denied!");
                        b.WithDescription("You don't have permission to .tts this command!");
                        await arg.Channel.SendMessageAsync("", false, b.Build());
                    }
                }
                string parameters = content.Replace(Program.CommandPrefix.ToString()+""+cmd, "").Trim();
                
                //find the command in the file.

                if (CmdDB.CheckForCategory(cmd))
                {
                    if (CmdDB.GetCategoryByName(cmd).CheckForEntry("guildID"))//NEW: Check for guild id. If this entry exists, continune.
                    {
                        ulong id = CmdDB.GetCategoryByName(cmd).GetEntryByName("guildID").GetAsUlong();
                        if ((arg.Author as SocketGuildUser) == null)
                        {
                            EmbedBuilder b = new EmbedBuilder();
                            b.WithColor(Color.Orange);
                            b.WithAuthor(Program._client.CurrentUser);
                            b.WithTitle("Nope.");
                            b.WithDescription("You can't do that here. This isn't a guild channel.");
                            await arg.Channel.SendMessageAsync("", false, b.Build());

                            return true;
                        }
                        if ((arg.Author as IGuildUser)?.Guild == null)
                        {
                            EmbedBuilder b = new EmbedBuilder();
                            b.WithColor(Color.Orange);
                            b.WithAuthor(Program._client.CurrentUser);
                            b.WithTitle("Nope.");
                            b.WithDescription("You can't do that here. This isn't a guild channel.");
                            await arg.Channel.SendMessageAsync("", false, b.Build());
                            return true;
                        }

                        if (id != (arg.Author as IGuildUser).Guild?.Id)
                        {
                            return true;//The command isn't in that guild, so let's ignore it.
                        }
                    }
                    if (CmdDB.GetCategoryByName(cmd).GetEntryByName("restricted").GetAsBool())
                    {


                        SocketGuildUser user = ((SocketGuildUser)arg.Author);

                        if (await Program.rolemgt.CheckUserRole(user, Program._client))
                        {
                            hasrole = true;
                        }
                        if (!hasrole)
                        {
                            EmbedBuilder b = new EmbedBuilder();
                            b.WithColor(Color.Red);
                            b.WithAuthor(Program._client.CurrentUser);
                            b.WithTitle("Access Denied!");
                            b.WithDescription($"Hi, {arg.Author.Mention}! You don't have access to this command...");
                            await arg.Channel.SendMessageAsync("", false, b.Build());
                            successful = false;
                            return true;
                        }
                    }

                   
                    string response = CmdDB.GetCategoryByName(cmd).GetEntryByName("action").GetAsString();
                    //replace action newlines with actual new lines.
                    response = response.Replace("\\r", "\r").Replace("\\n", "\n");
                    //VariableSupport.
                   
                    response = response.Replace("{params}", parameters);//Uses all parameters.
                    List<string> individualParams = Regex.Matches(parameters, @"[\""].+?[\""]|[^ ]+").Cast<Match>().Select(m => m.Value).ToList();//selects individual parameters.
                    //Count through all individual parameters and replace {i} with that parameter (Without quotations)
                    for (int i = 0; i < individualParams.Count; i++)
                    {
                        response = response.Replace("{"+i+"}", individualParams[i].Replace("\"",""));
                    }

                    if (response.StartsWith("EXEC"))
                    {
                        string[] resplit = response.Replace("EXEC ", "").Split(' ');
                        if (resplit.Length < 3)
                        {
                            EmbedBuilder b = new EmbedBuilder();
                            b.WithColor(Color.DarkRed);
                            b.WithAuthor(Program._client.CurrentUser);
                            b.WithTitle("EXEC Malformed.");
                            b.WithDescription("This command is not going to work. The EXEC setup was invalid.");
                            await arg.Channel.SendMessageAsync("", false, b.Build());

                            successful = true;
                            return true;
                        }
                        string modpath = System.IO.Path.GetFullPath("ext/" + resplit[0]);
                        string nsdotclass = resplit[1];
                        string mthd = resplit[2];
                        string strargs = "";
                        for (int i = 3; i < resplit.Length; i++)
                        {
                            strargs += resplit[i] + " ";
                        }
                        object[] parameter = { arg, strargs.Replace("{params}", parameters).Trim() };
                        Assembly asm = Assembly.LoadFile(modpath);
                        Type t = asm.GetType(nsdotclass, true);
                        MethodInfo info = t.GetMethod(mthd, BindingFlags.Public | BindingFlags.Static);
                        info.Invoke(null, parameter);
                        successful = true;
                        return true;
                    }
                    if (response.StartsWith("SCRIPT"))
                    {
                        string script = response.Replace("SCRIPT ", "");
                        //thread optimize this.
                        
                        #pragma warning disable
                        scriptService.EvaluateScript(script, CmdDB, cmd, Program._client, arg);
                        successful = true;
                        return true;
                      
                    }
                    if (response.StartsWith("CLI_EXEC"))//EXEC with client instead of context
                    {
                        string[] resplit = response.Replace("CLI_EXEC ", "").Split(' ');
                        if (resplit.Length < 3)
                        {
                            EmbedBuilder b = new EmbedBuilder();
                            b.WithColor(Color.DarkRed);
                            b.WithAuthor(Program._client.CurrentUser);
                            b.WithTitle("EXEC Malformed.");
                            b.WithDescription("This command is not going to work. The CLI_EXEC setup was invalid.");
                            await arg.Channel.SendMessageAsync("", false, b.Build());
                            successful = true;
                            return true;
                        }
                        string modpath = System.IO.Path.GetFullPath("ext/" + resplit[0]);
                        string nsdotclass = resplit[1];
                        string mthd = resplit[2];
                        string strargs = "";
                        for (int i = 3; i < resplit.Length; i++)
                        {
                            strargs += resplit[i] + " ";
                        }
                        object[] parameter = { Program._client, arg, strargs.Replace("{params}", parameters).Trim() };
                        Assembly asm = Assembly.LoadFile(modpath);
                        Type t = asm.GetType(nsdotclass, true);
                        MethodInfo info = t.GetMethod(mthd, BindingFlags.Public | BindingFlags.Static);
                        info.Invoke(null, parameter);
                        successful = true;
                        return true;
                    }
                    response = scriptService.ProcessVariableString(response, CmdDB, cmd, Program._client, arg);
                    RequestOptions op = new RequestOptions();
                    op.RetryMode = RetryMode.AlwaysRetry;
                    op.Timeout = 256;
                    await Retry.Do(async () => await arg.Channel.SendMessageAsync(response.Trim(),IsTTS), TimeSpan.FromMilliseconds(140));//we want to allow for custom variables.

                    
                    successful = true;
                    return true;

                }
                else
                {
                    return false;
                }

            }
            catch (AggregateException ex)
            {

                await arg.Channel.SendMessageAsync("The request failed (MANY TIMES) due to some API related thing I can't sort out right now... please forgive me... (You can try that again if you want...)");
                successful = false;
                Program.LogToConsole(new LogMessage(LogSeverity.Error, "CritERR", ex.Message, ex));
                return false;
            }
            catch (Exception ex)
            {

                EmbedBuilder b = new EmbedBuilder();
                b.WithColor(Color.DarkRed);
                b.WithAuthor(Program._client.CurrentUser);
                b.WithTitle("Command Failed!");
                b.WithDescription("The command spectacularly failed. Error details are below.");
                b.AddField("Error Details", ex.Message);
                b.AddField("For Developers", ex.StackTrace);
                await arg.Channel.SendMessageAsync("", false, b.Build());
                successful = false;
                Program.LogToConsole(new LogMessage(LogSeverity.Error, "CritERR", ex.Message, ex));
                return false;
            }
        }

        private void StartScript()
        {
            throw new NotImplementedException();
        }

        public INICategory[] GetAllCommand()
        {
            return CmdDB.Categories.ToArray();
        }
    }
}
