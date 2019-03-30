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
    internal class CustomCommandManager
    {
        List<GuildObject> guilds = new List<GuildObject>();
        CoreScript coreScript;
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
                    }
                    guilds.Add(ob);
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(
                            new LogMessage(LogSeverity.Debug, "CmdMgr", $"SUCCESS: Added new GuildObject to list!"), ConsoleColor.DarkGreen);
                }
            }
        }

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
            if (cmdline.ToLower().StartsWith(command))
            {
                args = cmdline.Remove(0, command.Length).Trim();
            }

            //check global first.
            GuildObject global = guilds.FirstOrDefault(x => x.ID == 0);
            if (global != null)
            {

                string res = global.GuildCommands.FirstOrDefault(c => c.name.ToLower() == command)?.action;
                if (string.IsNullOrWhiteSpace(res))
                {
                    //check guild context since global had nothing.
                    if ((msg as SocketGuildChannel) != null)
                    {
                        SocketGuildChannel s = msg.Channel as SocketGuildChannel;
                        gobj = guilds.FirstOrDefault(z => z.ID == s.Guild.Id);

                    }
                    if (gobj != null)
                    {
                        cmd = gobj.GuildCommands.FirstOrDefault(c => c.name.ToLower() == command);
                        res = cmd?.action;
                        if (!string.IsNullOrWhiteSpace(res))
                        {
                            return ProcessAction(res, args, ref gobj, ref cmd, ref msg);
                        }
                        else { serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "CmdMgr", "Conext guild didn't know what that command was!")); return null; }
                    }
                    else
                    {
                        serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "CmdMgr", "Context guild's command list not found! Do you have any guild commands defined?"));
                        return null;
                    }

                }
                else
                {
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
                    cmd = gobj.GuildCommands.FirstOrDefault(c => c.name.ToLower() == command);
                    res = cmd?.action;
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
                //thread optimize this.

                #pragma warning disable
                coreScript.EvaluateScript(gobj, script, cmd, serviceProvider.GetRequiredService<DiscordShardedClient>(), msg);
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
    }

    public class GuildCommand
    {
        public string name { get; set; }
        public string action { get; set; }
        public bool RequirePermission { get; set; }
        public int? Counter { get; set; }
    }

    public class GuildObject
    {
        public string CommandPrefix { get; set; }
        public ulong ID { get; set; }
        public List<GuildCommand> GuildCommands { get; set; }
    }
}
