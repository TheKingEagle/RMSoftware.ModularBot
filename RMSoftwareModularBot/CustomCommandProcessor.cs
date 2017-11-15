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
namespace RMSoftware.ModularBot
{
    /// <summary>
    /// This is a twitch style command manager
    /// </summary>
    public class CustomCommandManager
    {

        INIFile CmdDB = new INIFile("commands.ini");
        /// <summary>
        /// Adds a command to the bot.
        /// </summary>
        /// <param name="Command">command tag (without !)</param>
        /// <param name="Action">The command response/action</param>
        /// <param name="Restricted">If restricted, only people with the @DevCommand tag can use it</param>
        public string AddCommand(string Command, string Action, bool Restricted)
        {
            if(CmdDB.CheckForCategory(Command.Replace("!","")))
            {
                return "That command already exists!";
            }
            CmdDB.CreateCategory(Command.Replace("!", ""));
            CmdDB.CreateEntry(Command.Replace("!", ""), "action", Action);
            CmdDB.CreateEntry(Command.Replace("!", ""), "restricted", Restricted);
            return "Command added to the DB. Please remember to save with `!save`";
        }
        public string EditCommand(string Command, string newAction, bool Restricted)
        {
            if (!CmdDB.CheckForCategory(Command.Replace("!", "")))
            {
                return "That command does not exists!";
            }
            CmdDB.GetCategoryByName(Command.Replace("!", "")).GetEntryByName("action").SetValue(newAction);
            CmdDB.GetCategoryByName(Command.Replace("!", "")).GetEntryByName("restricted").SetValue(Restricted);
            return "Command edited. Please remember to save with `!save`";
        }
       
        public string AddCommand(string Command, string Action, bool Restricted, ulong guildID)
        {
            if (CmdDB.CheckForCategory(Command.Replace("!", "")))
            {
                return "That command already exists!";
            }
            CmdDB.CreateCategory(Command.Replace("!", ""));
            CmdDB.CreateEntry(Command.Replace("!", ""), "action", Action);
            CmdDB.CreateEntry(Command.Replace("!", ""), "restricted", Restricted);
            CmdDB.CreateEntry(Command.Replace("!", ""), "guildID", guildID);
            return "Command added to the DB. Please remember to save with `!save`";
        }

        public void Save()
        {
            CmdDB.SaveConfiguration();
        }
        public void DeleteCommand(string Command)
        {
            CmdDB.DeleteCategory(Command.Replace("!", ""));
        }
        public bool successful = false;
        /// <summary>
        /// Process user input for custom command module
        /// </summary>
        /// <param name="arg"></param>
        public async void Process(SocketMessage arg)
        {
            
            string content = arg.Content;
            bool hasrole = false;
            //substring the text into two parts.
            try
            {
                if (content.StartsWith("!"))
                {
                    string cmd = content.Split(' ')[0].Replace("!", "");//get the command bit, no ! mark, because it will murder everything
                    string parameters = content.Trim().Substring(cmd.Length+1).Trim();//get the parameters, without space before/after.

                    //find the command in the file.

                    if (CmdDB.CheckForCategory(cmd))
                    {
                        if(CmdDB.GetCategoryByName(cmd).CheckForEntry("guildID"))//NEW: Check for guild id. If this entry exists, continune.
                        {
                            ulong id = CmdDB.GetCategoryByName(cmd).GetEntryByName("guildID").GetAsUlong();
                            if ((arg.Author as IGuildUser) == null)
                            {
                                await Retry.Do(async () => await arg.Channel.SendMessageAsync("Hey, I know you really want to see that work, but this is my dm..."+
                                    " This command will only work on a specific guild. "), TimeSpan.FromMilliseconds(140));
                                
                                return;
                            }
                            if ((arg.Author as IGuildUser)?.Guild == null)
                            {
                                await Retry.Do(async () => await arg.Channel.SendMessageAsync("Hey, I know you really want to see that work, but this is my dm..."+
                                    " This command will only work on a specific guild. "), TimeSpan.FromMilliseconds(140));
                                
                                return;
                            }
                            
                            if (id != (arg.Author as IGuildUser).Guild?.Id)
                            {
                                await Retry.Do(async () => await arg.Channel.SendMessageAsync("Hey " + arg.Author.Mention + ", Wrong guild."), TimeSpan.FromMilliseconds(140));
                                
                                return;
                            }
                        }
                        if (CmdDB.GetCategoryByName(cmd).GetEntryByName("restricted").GetAsBool())
                        {


                            SocketGuildUser user = ((SocketGuildUser)arg.Author);
                            foreach (SocketRole role in user.Roles)
                            {
                                hasrole = false;
                                if (role.Name == "DevCommand")
                                {
                                    hasrole = true;
                                    break;
                                }
                            }
                            if (!hasrole)
                            {
                                await Retry.Do(async () => await arg.Channel.SendMessageAsync("Hey " + arg.Author.Mention + ", You don't have permission to use this command!"), TimeSpan.FromMilliseconds(140));
                                
                                successful = false;
                                return;
                            }
                        }
                        
                        //todo: Add exec commands to allow the program to execute methods in expansions
                        //perform the action with specified parameters if needed

                        //response sample: splitparam 3 | You hug {0}, {1}, and {2}, at the same time!
                        //response sample You hug {params}, for a long time
                        //resp EXEC: EXEC modname.dll ns.class mthdname <args split with spaces>
                        string response = CmdDB.GetCategoryByName(cmd).GetEntryByName("action").GetAsString();

                        if (response.StartsWith("splitparam"))
                        {
                            string[] responsearray = CmdDB.GetCategoryByName(cmd).GetEntryByName("action").GetAsString().Split('|');
                            int paramcount = int.Parse(responsearray[0].Trim().Split(' ')[1]);
                            string[] paramarray = parameters.Split(' ');
                            response = responsearray[1].Trim();
                            for (int i = 0; i < paramcount; i++)
                            {
                                response = response.Replace("{" + i + "}", paramarray[i]);
                            }
                        }
                        else
                        {
                            response = response.Replace("{params}", parameters);
                        }
                        if(response.StartsWith("EXEC"))
                        {
                            string[] resplit = response.Replace("EXEC ", "").Split(' ');
                            if(resplit.Length < 3)
                            {
                                await Retry.Do(async () => await arg.Channel.SendMessageAsync("The command failed to execute... EXEC method malformed"), TimeSpan.FromMilliseconds(140));
                                
                                successful = true;
                                return;
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
                            MethodInfo info = t.GetMethod(mthd,BindingFlags.Public | BindingFlags.Static);
                            info.Invoke(null, parameter);
                            successful = true;
                            return;
                        }
                        if (response.StartsWith("CLI_EXEC"))//EXEC with client instead of context
                        {
                            string[] resplit = response.Replace("CLI_EXEC ", "").Split(' ');
                            if (resplit.Length < 3)
                            {
                                await Retry.Do(async () => await arg.Channel.SendMessageAsync("The command failed to execute... CLI_EXEC method malformed"), TimeSpan.FromMilliseconds(140));
                                
                                successful = true;
                                return;
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
                            return;
                        }
                        string version = String.Format("{0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3));

                        RequestOptions op = new RequestOptions();
                        op.RetryMode = RetryMode.AlwaysRetry;
                        op.Timeout = 256;
                        await Retry.Do(async () => await arg.Channel.SendMessageAsync(response.Replace("%appv%", "v" + version).Trim()), TimeSpan.FromMilliseconds(140));

                        
                        
                        successful = true;

                    }

                }
            }
            catch (AggregateException ex)
            {

                await arg.Channel.SendMessageAsync("The request failed (MANY TIMES) due to some API related thing I can't sort out right now... please forgive me... (You can try that again if you want...)");
                successful = false;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("DevBOT.Exception: " + ex.ToString());
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception ex)
            {
                
                await arg.Channel.SendMessageAsync("There was a problem performing this command. See bot console for info");
                successful = false;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("DevBOT.Exception: " + ex.ToString());
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

       public INICategory[] GetAllCommand()
        {
            return CmdDB.Categories.ToArray();
        }
    }
}
