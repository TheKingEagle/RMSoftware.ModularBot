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

        INIFile CmdDB = new INIFile("commands.ini");
        public CoreScript scriptService = new CoreScript();
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
            CmdDB.CreateEntry(Command.Replace(Program.CommandPrefix.ToString(), ""), "action", Action);
            CmdDB.CreateEntry(Command.Replace(Program.CommandPrefix.ToString(), ""), "restricted", Restricted);
            return "Command added to the DB. Please remember to save.";
        }
        public string EditCommand(string Command, string newAction, bool Restricted)
        {
            if (!CmdDB.CheckForCategory(Command.Replace(Program.CommandPrefix.ToString(), "")))
            {
                return "That command does not exists!";
            }
            CmdDB.GetCategoryByName(Command.Replace(Program.CommandPrefix.ToString(), "")).GetEntryByName("action").SetValue(newAction);
            CmdDB.GetCategoryByName(Command.Replace(Program.CommandPrefix.ToString(), "")).GetEntryByName("restricted").SetValue(Restricted);
            return "Command edited. Please remember to save.";
        }

        public string AddCommand(string Command, string Action, bool Restricted, ulong guildID)
        {
            if (CmdDB.CheckForCategory(Command.Replace(Program.CommandPrefix.ToString(), "")))
            {
                return "That command already exists!";
            }
            CmdDB.CreateCategory(Command.Replace(Program.CommandPrefix.ToString(), ""));
            CmdDB.CreateEntry(Command.Replace(Program.CommandPrefix.ToString(), ""), "action", Action);
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
                    if(Program.rolemgt.CheckUserRole(a))
                    {
                        IsTTS = true;
                        cmd = cmd.Remove(cmd.Length - 4);
                    }
                    else
                    {
                        await arg.Channel.SendMessageAsync("Hey " + arg.Author.Mention + ", You don't have permission to TTS this command.");
                    }
                }
                string parameters = content.Replace(Program.CommandPrefix.ToString()+""+cmd, "").Trim();
                
                //find the command in the file.

                if (CmdDB.CheckForCategory(cmd))
                {
                    if (CmdDB.GetCategoryByName(cmd).CheckForEntry("guildID"))//NEW: Check for guild id. If this entry exists, continune.
                    {
                        ulong id = CmdDB.GetCategoryByName(cmd).GetEntryByName("guildID").GetAsUlong();
                        if ((arg.Author as IGuildUser) == null)
                        {
                            await Retry.Do(async () => await arg.Channel.SendMessageAsync("Hey, I know you really want to see that work, but this is my dm..." +
                                " This command will only work on a specific guild. "), TimeSpan.FromMilliseconds(140));

                            return true;
                        }
                        if ((arg.Author as IGuildUser)?.Guild == null)
                        {
                            await Retry.Do(async () => await arg.Channel.SendMessageAsync("Hey, I know you really want to see that work, but this is my dm..." +
                                " This command will only work on a specific guild. "), TimeSpan.FromMilliseconds(140));

                            return true;
                        }

                        if (id != (arg.Author as IGuildUser).Guild?.Id)
                        {
                            await Retry.Do(async () => await arg.Channel.SendMessageAsync("Hey " + arg.Author.Mention + ", Wrong guild."), TimeSpan.FromMilliseconds(140));

                            return true;
                        }
                    }
                    if (CmdDB.GetCategoryByName(cmd).GetEntryByName("restricted").GetAsBool())
                    {


                        SocketGuildUser user = ((SocketGuildUser)arg.Author);

                        if (Program.rolemgt.CheckUserRole(user))
                        {
                            hasrole = true;
                        }
                        if (!hasrole)
                        {
                            await Retry.Do(async () => await arg.Channel.SendMessageAsync("Hey " + arg.Author.Mention + ", You don't have permission to use this command!"), TimeSpan.FromMilliseconds(140));

                            successful = false;
                            return true;
                        }
                    }

                   
                    string response = CmdDB.GetCategoryByName(cmd).GetEntryByName("action").GetAsString();
                    //Counter support!
                    response = scriptService.ProcessVariableString(response, CmdDB, cmd, Program._client, arg);
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
                            await Retry.Do(async () => await arg.Channel.SendMessageAsync("The command failed to execute... EXEC method malformed"), TimeSpan.FromMilliseconds(140));

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
                    if (response.StartsWith("CLI_EXEC"))//EXEC with client instead of context
                    {
                        string[] resplit = response.Replace("CLI_EXEC ", "").Split(' ');
                        if (resplit.Length < 3)
                        {
                            await Retry.Do(async () => await arg.Channel.SendMessageAsync("The command failed to execute... CLI_EXEC method malformed"), TimeSpan.FromMilliseconds(140));

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
                    string version = String.Format("{0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3));

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

                await arg.Channel.SendMessageAsync("There was a problem performing this command. See bot console for info");
                successful = false;
                Program.LogToConsole(new LogMessage(LogSeverity.Error, "CritERR", ex.Message, ex));
                return false;
            }
        }

        public INICategory[] GetAllCommand()
        {
            return CmdDB.Categories.ToArray();
        }
    }
}
