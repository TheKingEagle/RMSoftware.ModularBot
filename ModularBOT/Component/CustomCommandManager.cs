using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using System.Text.RegularExpressions;
using Discord.Commands;
using System.Reflection;

namespace ModularBOT.Component
{
    public class CustomCommandManager
    {
        List<GuildObject> guilds = new List<GuildObject>();

        internal IReadOnlyCollection<GuildObject> GuildObjects { get { return guilds.AsReadOnly(); } }
        internal CoreScript coreScript;
        IServiceProvider serviceProvider;
        internal CustomCommandManager(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            //Populate guild items.
            coreScript = new CoreScript(this, ref serviceProvider);
            foreach (string guildFile in Directory.GetFiles("guilds", "*.guild", SearchOption.TopDirectoryOnly))
            {
                using (StreamReader sr = new StreamReader(guildFile))
                {
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(
                            new LogMessage(LogSeverity.Verbose, "CmdMgr", $"Found Guild Object {Path.GetFileName(guildFile)}. Parsing."));
                    string json = sr.ReadToEnd();
                    sr.Close();
                    GuildObject ob = JsonConvert.DeserializeObject<GuildObject>(json);
                    if (ob == null)
                    {
                        serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(
                            new LogMessage(LogSeverity.Warning, "CmdMgr", $"Skipped {Path.GetFileName(guildFile)}. Object creation failed."));
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(ob.CommandPrefix))
                    {
                        ob.CommandPrefix = serviceProvider.GetRequiredService<Configuration>().CommandPrefix;//use global (This will set it)
                        ob.SaveJson();
                    }
                    if(ob.CommandPrefix.Contains('`'))
                    {
                        ob.CommandPrefix = serviceProvider.GetRequiredService<Configuration>().CommandPrefix;//use global (This will set it)
                        ob.SaveJson();
                        serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(
                            new LogMessage(LogSeverity.Warning, "CmdMgr", $"Warning: Command prefix had invalid character! reset to configured default."), ConsoleColor.DarkGreen);
                    }
                    guilds.Add(ob);
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(
                            new LogMessage(LogSeverity.Debug, "CmdMgr", $"SUCCESS: Added new GuildObject to list!"), ConsoleColor.DarkGreen);
                }
            }
            GuildObject globalob = guilds.FirstOrDefault(x => x.ID == 0);
            
            if(globalob == null)
            {
                globalob = new GuildObject
                {
                    ID = 0,
                    CommandPrefix = serviceProvider.GetRequiredService<Configuration>().CommandPrefix,
                    GuildCommands = new List<GuildCommand>(),
                    
                };
                globalob.SaveJson();
                guilds.Add(globalob);
            }
            else
            {
                globalob.CommandPrefix = serviceProvider.GetRequiredService<Configuration>().CommandPrefix;
                globalob.SaveJson();
            }
            
        }

        #region Processing

        public string ProcessMessage(IMessage socketmsg)
        {
            ulong gid = 0;//global by default
            string prefix = serviceProvider.GetRequiredService<Configuration>().CommandPrefix;//use global (This will set it);
            if ((socketmsg.Channel as SocketGuildChannel) != null)
            {
                SocketGuildChannel sc = socketmsg.Channel as SocketGuildChannel;
                gid = sc.Guild.Id;
            }
            if (!string.IsNullOrWhiteSpace(guilds.FirstOrDefault(x => x.ID == gid)?.CommandPrefix))
            {
                prefix = guilds.FirstOrDefault(x => x.ID == gid)?.CommandPrefix;
            }

            if (socketmsg.Content.StartsWith(prefix))
            {
                string cmdLine = socketmsg.Content.Remove(0, prefix.Length);//remove prefix length.
                
                return ProcessCmdLine(cmdLine, ref socketmsg);
            }

            return "";
        }

        /// <summary>
        /// Process command string.
        /// </summary>
        /// <param name="cmdline">cmd string</param>
        /// <param name="guildID">context guild. ZERO for global/DM</param>
        /// <returns>returns action if found, otherwise returns empty string.</returns>
        private string ProcessCmdLine(string cmdline, ref IMessage msg)
        {
            GuildObject gobj = null;
            GuildCommand cmd = null;
            string[] cmdlineArr = cmdline.Split(' ');
            string command = cmdlineArr[0].ToLower() ?? "";
            string args = "";
            var permissionManager = serviceProvider.GetRequiredService<PermissionManager>();
            if (cmdline.ToLower().StartsWith(command))
            {
                args = cmdline.Remove(0, command.Length).Trim();
            }

            //check global first.
            GuildObject global = guilds.FirstOrDefault(x => x.ID == 0);
            if (global != null)
            {

                string res = global.GuildCommands.FirstOrDefault(c => c.Name.ToLower() == command)?.Action;
                gobj = global;
                cmd = gobj.GuildCommands.FirstOrDefault(c => c.Name.ToLower() == command);
                if(cmd != null)
                {
                    if (cmd.RequirePermission)
                    {
                        if (permissionManager.GetAccessLevel(msg.Author) < AccessLevels.CommandManager)
                        {
                            EmbedBuilder b = new EmbedBuilder();
                            b.WithTitle("Access Denied");
                            b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                            b.WithDescription("You do not have permission to use this command. Requires `AccessLevel 1` or higher.");
                            b.WithColor(Color.Red);
                            b.WithFooter("ModularBOT • Core");
                            msg.Channel.SendMessageAsync("", false, b.Build());
                            return null;
                        }
                    }
                }
                
                if (string.IsNullOrWhiteSpace(res))
                {
                    //check guild context since global had nothing.
                    if ((msg.Channel as SocketGuildChannel) != null)
                    {
                        SocketGuildChannel s = msg.Channel as SocketGuildChannel;
                        gobj = guilds.FirstOrDefault(z => z.ID == s.Guild.Id);

                    }
                    if (gobj != null)
                    {
                        cmd = gobj.GuildCommands.FirstOrDefault(c => c.Name.ToLower() == command);
                        res = cmd?.Action;
                        if (cmd != null)
                        {
                            if (cmd.RequirePermission)
                            {
                                if (permissionManager.GetAccessLevel(msg.Author) < AccessLevels.CommandManager)
                                {
                                    EmbedBuilder b = new EmbedBuilder();
                                    b.WithTitle("Access Denied");
                                    b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                                    b.WithDescription("You do not have permission to use this command. Requires `AccessLevel 1` or higher.");
                                    b.WithColor(Color.Red);
                                    b.WithFooter("ModularBOT • Core");
                                    msg.Channel.SendMessageAsync("", false, b.Build());
                                    return null;
                                }
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(res))
                        {

                            return ProcessAction(res, args, ref gobj, ref cmd, ref msg);
                        }
                        else
                        {
                            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "CmdMgr", "Conext guild didn't know what that command was!")); return null;
                        }
                    }
                    else
                    {
                        serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "CmdMgr", "Global command list not found! Do you have any guild commands defined?"));
                        return null;
                    }

                }
                else
                {

                    if (cmd != null)
                    {
                        if (cmd.RequirePermission)
                        {
                            if (permissionManager.GetAccessLevel(msg.Author) < AccessLevels.CommandManager)
                            {
                                EmbedBuilder b = new EmbedBuilder();
                                b.WithTitle("Access Denied");
                                b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                                b.WithDescription("You do not have permission to use this command. Requires `AccessLevel 1` or higher.");
                                b.WithColor(Color.Red);
                                b.WithFooter("ModularBOT • Core");
                                msg.Channel.SendMessageAsync("", false, b.Build());
                                return null;
                            }
                        }
                    }
                    return ProcessAction(res, args, ref gobj, ref cmd, ref msg);
                }
            }
            else
            {
                string res = "";
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "CmdMgr", "Global command list not found! Do you have any global commands defined?"));
                //check guild context since global straight up didn't exist.
                if ((msg.Channel as SocketGuildChannel) != null)
                {
                    SocketGuildChannel s = msg.Channel as SocketGuildChannel;
                    gobj = guilds.FirstOrDefault(z => z.ID == s.Guild.Id);

                }
                if (gobj != null)
                {
                    cmd = gobj.GuildCommands.FirstOrDefault(c => c.Name.ToLower() == command);
                    res = cmd?.Action;
                    if (cmd != null)
                    {
                        if (cmd.RequirePermission)
                        {
                            if (permissionManager.GetAccessLevel(msg.Author) < AccessLevels.CommandManager)
                            {
                                EmbedBuilder b = new EmbedBuilder();
                                b.WithTitle("Access Denied");
                                b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                                b.WithDescription("You do not have permission to use this command. Requires `AccessLevel 1` or higher.");
                                b.WithColor(Color.Red);
                                b.WithFooter("ModularBOT • Core");
                                msg.Channel.SendMessageAsync("", false, b.Build());
                                return null;
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(res))
                    {
                        return ProcessAction(res, args, ref gobj, ref cmd, ref msg);
                    }
                    else { serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "CmdMgr", "Context guild didn't know what that command was!")); return null; }
                }
                else
                {
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "CmdMgr", "Context guild's command list not found! Do you have any guild commands defined?"));
                    return null;
                }
            }

        }

        private string ProcessAction(string action, string parameters, ref GuildObject gobj, ref GuildCommand cmd, ref IMessage msg)
        {
            string response = action;
            response = response.Replace("{params}", parameters);//Uses all parameters.
            List<string> individualParams = Regex.Matches(parameters, @"[\""].+?[\""]|[^ ]+").Cast<Match>().Select(m => m.Value).ToList();//selects individual parameters.

            //Count through all individual parameters and replace {i} with that parameter (Without quotations)
            for (int i = 0; i < individualParams.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(individualParams[i]))
                {
                    response = response.Replace("{" + i + "}", individualParams[i].Replace("\"", ""));
                }
                else
                {
                    response = response.Replace("{" + i + "}", "");//replace the thing with a null space.
                }
            }
            //remove unused Variable placeholders.

            foreach (Match i in Regex.Matches(response, "{\\d+\\}"))
            {
                response = response.Replace(i.Value, "");
            }

            response = coreScript.ProcessVariableString(gobj, response, cmd, serviceProvider.GetRequiredService<DiscordShardedClient>(), msg);

            #region EXEC
            if (response.StartsWith("EXEC"))
            {
                string[] resplit = response.Replace("EXEC ", "").Split(' ');
                if (resplit.Length < 3)
                {
                    EmbedBuilder b = new EmbedBuilder();
                    b.WithColor(Color.DarkRed);
                    b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                    b.WithTitle("EXEC Malformed.");
                    b.WithDescription("This command is not going to work. The EXEC setup was invalid.");
                    IMessage asmsg = msg;
                    Task.Run(async () => await asmsg.Channel.SendMessageAsync("", false, b.Build()));

                    
                    return "EXEC";
                }
                string modpath = System.IO.Path.GetFullPath("ext/" + resplit[0]);
                string nsdotclass = resplit[1];
                string mthd = resplit[2];
                string strargs = "";
                for (int i = 3; i < resplit.Length; i++)
                {
                    strargs += resplit[i] + " ";
                }
                object[] parameter = { msg, strargs.Replace("{params}", parameters).Trim() };
                Assembly asm = Assembly.LoadFile(modpath);
                Type t = asm.GetType(nsdotclass, true);
                MethodInfo info = t.GetMethod(mthd, BindingFlags.Public | BindingFlags.Static);
                Task.Run(() => info.Invoke(null, parameter));
                
                return "EXEC";
            }
            #endregion

            #region SCRIPT
            if (response.StartsWith("SCRIPT"))
            {
                string script = response.Replace("SCRIPT ", "");
                GuildObject cg = gobj;
                GuildCommand ccmd = cmd;
                IMessage msgg = msg;
                Task.Run(()=> coreScript.EvaluateScript(cg, script, ccmd, serviceProvider.GetRequiredService<DiscordShardedClient>(), msgg).GetAwaiter().GetResult());
                return "SCRIPT";

            }
            #endregion

            #region CLI_EXEC
            if (response.StartsWith("CLI_EXEC"))//EXEC with client instead of context
            {
                string[] resplit = response.Replace("CLI_EXEC ", "").Split(' ');
                if (resplit.Length < 3)
                {
                    EmbedBuilder b = new EmbedBuilder();
                    b.WithColor(Color.DarkRed);
                    b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                    b.WithTitle("CLI_EXEC Malformed.");
                    b.WithDescription("This command is not going to work. The CLI_EXEC setup was invalid.");
                    IMessage asmsg = msg;
                    Task.Run(async () => await asmsg.Channel.SendMessageAsync("", false, b.Build()));

                    return "CLI_EXEC";
                }
                string modpath = System.IO.Path.GetFullPath("ext/" + resplit[0]);
                string nsdotclass = resplit[1];
                string mthd = resplit[2];
                string strargs = "";
                for (int i = 3; i < resplit.Length; i++)
                {
                    strargs += resplit[i] + " ";
                }
                object[] parameter = { serviceProvider.GetRequiredService<DiscordShardedClient>(), msg, strargs.Replace("{params}", parameters).Trim() };
                Assembly asm = Assembly.LoadFile(modpath);
                Type t = asm.GetType(nsdotclass, true);
                MethodInfo info = t.GetMethod(mthd, BindingFlags.Public | BindingFlags.Static);
                info.Invoke(null, parameter);

                return "CLI_EXEC";
            }
            #endregion
            return response;
        }
        #endregion

        #region Command Management
        //TODO: Add/delete commands

        /// <summary>
        /// Add command to DB. by default to global.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="name"></param>
        /// <param name="action"></param>
        /// <param name="restricted"></param>
        /// <param name="gid"></param>
        /// <returns></returns>
        public async Task AddCmd(IUserMessage message, string name, string action, bool restricted, ulong gid = 0)
        {
            //SocketGuildChannel c = message.Channel as SocketGuildChannel;
            
            //if(c != null)
            //{
            //    gid = c.Guild.Id;
            //}
            GuildObject go = guilds.FirstOrDefault(x => x.ID == gid);
            if(go == null)
            {
                //create the new guildObject
                go = new GuildObject
                {
                    CommandPrefix = serviceProvider.GetRequiredService<Configuration>().CommandPrefix,
                    ID = gid,
                    GuildCommands = new List<GuildCommand>()
                };
                go.SaveJson();
                guilds.Add(go);
            }
            GuildCommand gc = go.GuildCommands.FirstOrDefault(cm => cm.Name.ToLower() == name.ToLower());
                
            if(gc != null)
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithTitle("This command already exists!");
                b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                b.WithDescription($"If you would like to edit this command, please use `{go.CommandPrefix}editcmd` instead.");
                b.WithColor(Color.Red);
                b.WithFooter("ModularBOT • Core");
                await message.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
            else
            {
                gc = new GuildCommand
                {
                    Name = name.ToLower(),
                    Action = action,
                    RequirePermission = restricted
                };
                go.GuildCommands.Add(gc);
                go.SaveJson();
                EmbedBuilder b = new EmbedBuilder();
                b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                b.WithTitle("Custom Command Added!");
                b.AddField("Command", $"`{go.CommandPrefix}{name}`");
                b.AddField("Requires permission", restricted ? "`Yes`" : "`No`",true);
                string guild = (gid > 0) ? serviceProvider.GetRequiredService<DiscordShardedClient>().Guilds.FirstOrDefault(g => g.Id == gid).Name : "All Guilds";
                b.AddField("Availability", $"`{guild}`",true);
                b.AddField("Action", action);
                

                b.WithColor(Color.Green);
                b.WithFooter("ModularBOT • Core");
                await message.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
            
        }

        public async Task DelCmd(IUserMessage message, string name, ulong gid=0)
        {
            GuildObject go = guilds.FirstOrDefault(x => x.ID == gid);
            if (go == null)
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithTitle("No Commands defined!");
                b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                b.WithDescription("This guild does not have a valid configuration! That means there are no commands to delete. "+
                    $"If you are trying to delete a global command, please use `{go?.CommandPrefix ?? serviceProvider.GetRequiredService<Configuration>().CommandPrefix}delgcmd` instead.");
                b.WithColor(Color.Red);
                b.WithFooter("ModularBOT • Core");
                await message.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
            GuildCommand gc = go.GuildCommands.FirstOrDefault(cm => cm.Name.ToLower() == name.ToLower());

            if (gc == null)
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithTitle("This command does not exists!");
                b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                b.WithDescription($"Check your spelling. If this is a global command please use `{go.CommandPrefix}delgcmd` instead.");
                b.WithColor(Color.Red);
                b.WithFooter("ModularBOT • Core");
                await message.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
            else
            {

                go.GuildCommands.Remove(gc);
                go.SaveJson();
                EmbedBuilder b = new EmbedBuilder();
                b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                b.WithTitle("Custom Command Removed!");
                b.AddField("Command", $"`{go.CommandPrefix}{name}`");
                b.AddField("Availability", $"`Nowhere!`", true);
                b.WithColor(Color.Green);
                b.WithFooter("ModularBOT • Core");
                await message.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
        }
        #endregion
    }

    public class GuildCommand
    {
        public string Name { get; set; }
        public string Action { get; set; }
        public bool RequirePermission { get; set; }
        public int? Counter { get; set; }
    }

    public class GuildObject
    {
        public string CommandPrefix { get; set; }
        public ulong ID { get; set; }
        public List<GuildCommand> GuildCommands { get; set; }

        public void SaveJson()
        {
            using (StreamWriter sw = new StreamWriter($"guilds/{ID}.guild"))
            {
               
                sw.WriteLine(JsonConvert.SerializeObject(this,Formatting.Indented));
                sw.Flush();
                sw.Close();
            }
        }
    }
}
