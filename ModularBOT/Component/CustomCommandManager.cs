﻿using System;
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
using ModularBOT.Entity;

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
            if (string.IsNullOrWhiteSpace(serviceProvider.GetRequiredService<Configuration>().CommandPrefix))
            {
                serviceProvider.GetRequiredService<ConsoleIO>().ShowKillScreen("Invalid Prefix", "You don't have a valid global prefix configured.", false, ref Program.ShutdownCalled, ref Program.RestartRequested, 5, new ArgumentException("Invalid prefix"), "MBOT_CONFIGURATION_ERR", true);
            }
            if (serviceProvider.GetRequiredService<Configuration>().CommandPrefix.Contains('`'))
            {
                serviceProvider.GetRequiredService<ConsoleIO>().ShowKillScreen("Invalid Prefix", "Your configured global prefix has invalid characters.", false, ref Program.ShutdownCalled, ref Program.RestartRequested, 5, new ArgumentException("Invalid prefix"), "MBOT_CONFIGURATION_ERR", true);
                serviceProvider.GetRequiredService<Configuration>().CommandPrefix = null;
                serviceProvider.GetRequiredService<ConfigurationManager>().Save();
            }
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
                        ob.SaveData();
                    }
                    if(ob.CommandPrefix.Contains('`'))
                    {
                        
                        ob.CommandPrefix = serviceProvider.GetRequiredService<Configuration>().CommandPrefix;//use global (This will set it)
                        ob.SaveData();
                        serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(
                            new LogMessage(LogSeverity.Warning, "CmdMgr", $"Warning: Guild `{ob.ID}`'s Command prefix had invalid character! reset to global default."), ConsoleColor.Magenta);
                    }
                    if(!ob.BlacklistMode.HasValue)
                    {
                        ob.BlacklistMode = AutoBlacklistModes.Standard;//by default, set new missing value...
                        ob.SaveData();
                        serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(
                            new LogMessage(LogSeverity.Warning, "CmdMgr", $"Warning: Auto-Blacklist option was not present. set it to STANDARD"), ConsoleColor.Magenta);
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
                globalob.SaveData();
                guilds.Add(globalob);
            }
            else
            {
                globalob.CommandPrefix = serviceProvider.GetRequiredService<Configuration>().CommandPrefix;
                globalob.SaveData();
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

        public string ProcessMessage(IMessage socketmsg,string prefix)
        {
            
            if (socketmsg.Content.StartsWith(prefix))
            {
                string cmdLine = socketmsg.Content.Remove(0, prefix.Length);//remove prefix length.

                return ProcessCmdLine(cmdLine, ref socketmsg);
            }

            return "";
        }

        private string ProcessCmdLine(string cmdline, ref IMessage msg)
        {
            GuildObject ContextGO = guilds.FirstOrDefault(x => x.ID == 0);
            if (msg.Channel is SocketGuildChannel sc)
            {
                ContextGO = guilds.FirstOrDefault(x => x.ID == sc.Guild.Id) ?? guilds.FirstOrDefault(x => x.ID == 0);//if guild doesnt exist, use global.
            }
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
                        if (permissionManager.GetAccessLevel(msg.Author) < cmd.CommandAccessLevel)
                        {
                            msg.Channel.SendMessageAsync("", false, permissionManager.GetAccessDeniedMessage(msg.Author, cmd.CommandAccessLevel));
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
                                if (permissionManager.GetAccessLevel(msg.Author) < cmd.CommandAccessLevel)
                                {
                                    msg.Channel.SendMessageAsync("", false, permissionManager.GetAccessDeniedMessage(msg.Author, cmd.CommandAccessLevel));
                                    return null;
                                }
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(res))
                        {
                            return ProcessAction(res, args, ref ContextGO, ref cmd, ref msg);
                        }
                        else
                        {
                            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "CmdMgr", "Context guild didn't know what that custom command was!")); return null;
                        }
                    }
                    else
                    {
                        serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "CmdMgr", "Context guild's command list not found! They must be new here."));
                        return null;
                    }

                }
                else
                {

                    if (cmd != null)
                    {
                        if (cmd.RequirePermission)
                        {
                            if (permissionManager.GetAccessLevel(msg.Author) < cmd.CommandAccessLevel)
                            {
                                msg.Channel.SendMessageAsync("", false, permissionManager.GetAccessDeniedMessage(msg.Author, cmd.CommandAccessLevel));
                                return null;
                            }
                        }
                    }
                    return ProcessAction(res, args, ref ContextGO, ref cmd, ref msg);
                }
            }
            else
            {
                string res = "";
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "CmdMgr", "Global command list not found! Were there previously global commands defined?"));
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
                            if (permissionManager.GetAccessLevel(msg.Author) < cmd.CommandAccessLevel)
                            {
                                msg.Channel.SendMessageAsync("",false,permissionManager.GetAccessDeniedMessage(msg.Author, cmd.CommandAccessLevel));
                                return null;
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(res))
                    {
                        return ProcessAction(res, args, ref ContextGO, ref cmd, ref msg);
                    }
                    else { serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "CmdMgr", "Context guild didn't know what that command was!")); return null; }
                }
                else
                {
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "CmdMgr", "Context guild's command list not found! They must be new here."));
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
            if(!response.StartsWith("SCRIPT"))
            {
                response = coreScript.ProcessVariableString(gobj, response, cmd, serviceProvider.GetRequiredService<DiscordShardedClient>(), msg);
            }

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
        public async Task AddCmd(IUserMessage message, string name, string action, bool restricted,AccessLevels CommandAccessLevel,  ulong gid = 0)
        {
            
            string g_prefix = "";

            if (message.Channel is SocketGuildChannel c)
            {
                GuildObject pgo = guilds.FirstOrDefault(a => a.ID == c.Guild.Id);

                if (pgo != null)
                {
                    g_prefix = pgo.CommandPrefix;
                }
                else
                {
                    g_prefix = serviceProvider.GetRequiredService<Configuration>().CommandPrefix;
                }
            }
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
                go.SaveData();
                guilds.Add(go);
            }
            GuildCommand gc = go.GuildCommands.FirstOrDefault(cm => cm.Name.ToLower() == name.ToLower());
                
            if(gc != null)
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithTitle("This command already exists!");
                b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                b.WithDescription($"If you would like to edit this command, please use `{g_prefix}editcmd` instead.");
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
                    RequirePermission = restricted,
                    CommandAccessLevel = CommandAccessLevel
                };
                go.GuildCommands.Add(gc);
                go.SaveData();
                EmbedBuilder b = new EmbedBuilder();
                b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                b.WithTitle("Custom Command Added!");
                b.AddField("Command", $"`{g_prefix}{name}`");
                b.AddField("Minimum AccessLevel", restricted ? $"`AccessLevels.{CommandAccessLevel}`" : "`AccessLevels.Normal`",true);
                string guild = (gid > 0) ? serviceProvider.GetRequiredService<DiscordShardedClient>().Guilds.FirstOrDefault(g => g.Id == gid).Name : "All Guilds";
                b.AddField("Availability", $"`{guild}`",true);
                b.AddField("Action", action);
                

                b.WithColor(Color.Green);
                b.WithFooter("ModularBOT • Core");
                await message.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
            
        }

        public async Task AddCmd(IUserMessage message, string name, string action, bool restricted, ulong gid = 0)
        {

            string g_prefix = "";

            if (message.Channel is SocketGuildChannel c)
            {
                GuildObject pgo = guilds.FirstOrDefault(a => a.ID == c.Guild.Id);

                if (pgo != null)
                {
                    g_prefix = pgo.CommandPrefix;
                }
                else
                {
                    g_prefix = serviceProvider.GetRequiredService<Configuration>().CommandPrefix;
                }
            }
            GuildObject go = guilds.FirstOrDefault(x => x.ID == gid);
            if (go == null)
            {
                //create the new guildObject
                go = new GuildObject
                {
                    CommandPrefix = serviceProvider.GetRequiredService<Configuration>().CommandPrefix,
                    ID = gid,
                    GuildCommands = new List<GuildCommand>()
                };
                go.SaveData();
                guilds.Add(go);
            }
            GuildCommand gc = go.GuildCommands.FirstOrDefault(cm => cm.Name.ToLower() == name.ToLower());

            if (gc != null)
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithTitle("This command already exists!");
                b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                b.WithDescription($"If you would like to edit this command, please use `{g_prefix}editcmd` instead.");
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
                    RequirePermission = restricted,
                    CommandAccessLevel = AccessLevels.CommandManager
                };
                go.GuildCommands.Add(gc);
                go.SaveData();
                EmbedBuilder b = new EmbedBuilder();
                b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                b.WithTitle("Custom Command Added!");
                b.AddField("Command", $"`{g_prefix}{name}`");
                b.AddField("Minimum AccessLevel", restricted ? $"`AccessLevels.CommandManager`" : "`AccessLevels.Normal`", true);
                string guild = (gid > 0) ? serviceProvider.GetRequiredService<DiscordShardedClient>().Guilds.FirstOrDefault(g => g.Id == gid).Name : "All Guilds";
                b.AddField("Availability", $"`{guild}`", true);
                b.AddField("Action", action);


                b.WithColor(Color.Green);
                b.WithFooter("ModularBOT • Core");
                await message.Channel.SendMessageAsync("", false, b.Build());
                return;
            }

        }
        public async Task DelCmd(IUserMessage message, string name, ulong gid=0)
        {
            string g_prefix = "";

            if (message.Channel is SocketGuildChannel c)
            {
                GuildObject pgo = guilds.FirstOrDefault(a => a.ID == c.Guild.Id);

                if (pgo != null)
                {
                    g_prefix = pgo.CommandPrefix;
                }
                else
                {
                    g_prefix = serviceProvider.GetRequiredService<Configuration>().CommandPrefix;
                }
            }
            GuildObject go = guilds.FirstOrDefault(x => x.ID == gid);
            if (go == null)
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithTitle("No Commands defined!");
                b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                b.WithDescription("This guild does not have a valid configuration! That means there are no commands to delete. "+
                    $"Please make sure you are calling the command from the correct context. If you are trying to delete a global command, please call `{g_prefix}delgcmd` instead.");
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
                if(gid >0)
                {
                    b.WithDescription($"Check your spelling. The guild command was not found.");
                }
                if (gid == 0)
                {
                    b.WithDescription($"Check your spelling. The global command was not found.");
                }

                b.WithColor(Color.Red);
                b.WithFooter("ModularBOT • Core");
                await message.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
            else
            {

                go.GuildCommands.Remove(gc);
                go.SaveData();
                EmbedBuilder b = new EmbedBuilder();
                b.WithAuthor(serviceProvider.GetRequiredService<DiscordShardedClient>().CurrentUser);
                b.WithTitle("Custom Command Removed!");
                b.AddField("Command", $"`{g_prefix}{name}`");
                b.AddField("Availability", $"`Command deleted...`", true);
                b.WithColor(Color.Green);
                b.WithFooter("ModularBOT • Core");
                await message.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
        }

        public Embed ViewCmd(ICommandContext Context, string Command)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithAuthor(Context.Client.CurrentUser);
            builder.WithTitle("Command Info");
            ulong gid = Context.Guild?.Id ?? 0;

            GuildObject pgo = guilds.FirstOrDefault(a => a.ID == gid);
            GuildObject ggo = guilds.FirstOrDefault(a => a.ID == 0);
            string g_prefix = pgo?.CommandPrefix ?? serviceProvider.GetRequiredService<Configuration>().CommandPrefix;
            GuildCommand GCMD = ggo?.GuildCommands?.FirstOrDefault(g => g.Name.ToLower() == Command.ToLower()) ?? pgo?.GuildCommands?.FirstOrDefault(p=>p.Name.ToLower() == Command.ToLower());//for checking if global is null.
            var Glob = ggo?.GuildCommands?.FirstOrDefault(g => g.Name.ToLower() == Command.ToLower());
            bool isglobal = Glob != null;
            if (GCMD == null)
            {
                
                var c = serviceProvider.GetRequiredService<CommandService>().Commands.FirstOrDefault(x => x.Name.ToLower() == Command.ToLower());
                if(c!= null)
                {

                    #region Per-guild module Check
                    string module = c.Module.Name;
                    var m = serviceProvider.GetRequiredService<DiscordNET>().ModuleMgr.Modules.FirstOrDefault(x => x.ModuleGroups.Contains(module));

                    //var m = serviceProvider.GetRequiredService<DiscordNET>().ModuleMgr.Modules.FirstOrDefault(x => x.ModuleName.Remove(0, x.ModuleName.LastIndexOf('.') + 1) == module);
                    if (m != null)
                    {
                        if (m.GuildsAvailable.Count > 0)
                        {
                            if (m.GuildsAvailable.FirstOrDefault(ggid => ggid == gid) == 0)//if no match for guild, don't populate.
                            {
                                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "getcmd", $"{c.Name}: {module} exists, but not for guild ID: {gid}."));
                                builder.WithColor(Color.Red);
                                builder.WithDescription("This command does not exist.");
                                builder.AddField("More info:", $"The command you requested was not found.\r\nPlease note: if a new module was just added, the bot will need to restart.\r\n\r\nTo check for a list of commands run `{g_prefix}listcmd`");
                                builder.WithFooter("ModularBOT • CORE");
                                return builder.Build();
                            }
                        }
                    }
                    #endregion
                }

                if (c == null)
                {
                    builder.WithColor(Color.Red);
                    builder.WithDescription("This command does not exist.");
                    builder.AddField("More info:", $"The command you requested was not found.\r\nPlease note: if a new module was just added, the bot will need to restart.\r\n\r\nTo check for a list of commands run `{g_prefix}listcmd`");
                    builder.WithFooter("ModularBOT • CORE");
                    return builder.Build();
                }

                else
                {

                    builder.WithColor(Color.Blue);
                    builder.WithDescription($"This is the basic breakdown of the command: `{g_prefix}{Command}`.");
                    builder.AddField("Summary", $" `{c.Summary ?? "No command summary provided."}`", true);
                    string param = $"{g_prefix}{c.Name} ";
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
                    builder.AddField("Usage", $"`{param.Trim()}`", true);
                    string aliases = "";
                    foreach (var item in c.Aliases)
                    {
                        aliases += "• " + g_prefix + item.ToString() + "\r\n";
                    }
                    if (string.IsNullOrWhiteSpace(aliases)) { aliases = "`No aliases.`"; }
                    string module = c.Module.Name;
                    if(c.Module.Name !="CoreModule")
                    {
                        var m = serviceProvider.GetRequiredService<DiscordNET>().ModuleMgr.Modules.FirstOrDefault(x => x.ModuleGroups.Contains(module));
                        string adj = $"`{Context.Guild?.Name ?? Context.User.Username}`";
                        if (m.GuildsAvailable.Count - 1 > 0)
                        {
                            adj = m.GuildsAvailable.Count - 1 > 1 ? $"`{Context.Guild?.Name ?? Context.User.Username}` and `{m.GuildsAvailable.Count - 1}` other places" : $"`{Context.Guild?.Name ?? Context.User.Username}` and `{m.GuildsAvailable.Count - 1}` other place";
                        }
                        if (m.GuildsAvailable.Count - 1 < 0)
                        {
                            adj = "`Everywhere`";
                        }
                        builder.AddField("Availability", adj);
                    }
                    
                    builder.AddField("Aliases", aliases, true);
                    builder.AddField("Remarks", $"`{c.Remarks ?? "Not specified"}`", true);
                    builder.AddField("From Module", $"`{c.Module.Name ?? "Unknown module"}`");
                    builder.AddField("Module Summary", c.Module.Summary ?? "`No summary provided.`",true);

                    string preconds = "";
                    foreach (var item in c.Preconditions)
                    {
                        preconds += "• " + item.ToString().Substring(item.ToString().LastIndexOf('.') + 1) + "\r\n";
                    }
                    if (string.IsNullOrWhiteSpace(preconds)) { preconds = "`No Preconditions.`"; }
                    builder.AddField("Preconditions", preconds, true);

                    return builder.Build();
                }

            }
           
            builder.WithColor(Color.Blue);
            builder.WithDescription($"This is the basic breakdown of the command: `{g_prefix}{GCMD.Name}`.");


            bool hasCounter = GCMD.Counter.HasValue;
            builder.AddField("Requires Access level: ", GCMD.RequirePermission ? $"`AccessLevels.{GCMD.CommandAccessLevel.ToString()}`" : "`AccessLevels.Normal`");
            builder.AddField("Is Global Command:", isglobal ? "Yes": "No");
            

            builder.AddField("Has counter: ", hasCounter);
            if (hasCounter)
            {
                builder.AddField("usage count: ", GCMD.Counter);
            }
            string action = "";
            if(GCMD.Action.ToUpper().StartsWith("SCRIPT ") || GCMD.Action.Contains('`')) { action = GCMD.Action; }
            else { action = $"```\r\n{GCMD.Action}\r\n```"; }
            if(!isglobal)
            {
                builder.AddField("Response/Action: ", action);
            }
            if(isglobal && serviceProvider.GetRequiredService<PermissionManager>().GetAccessLevel(Context.User) == AccessLevels.Administrator)
            {
                builder.AddField("Response/Action: ", action);
            }
            return builder.Build();
            
        }

        public async Task EditCMD(ICommandContext context, string cmdName, bool? asRestricted, string newAction="(unchanged)", ulong gid=0)
        {
            GuildObject ggo = guilds.FirstOrDefault(x => x.ID == 0);//global guild object.
            GuildObject cgo = guilds.FirstOrDefault(x => x.ID == gid);//context guild object.
            //first check for global.
            GuildCommand gco = ggo?.GuildCommands.FirstOrDefault(c => c.Name.ToLower() == cmdName.ToLower()) ?? null;//global
            if(gco != null)
            {
                if(serviceProvider.GetRequiredService<PermissionManager>().GetAccessLevel(context.User) < AccessLevels.Administrator)
                {
                    await context.Channel.SendMessageAsync("", false, serviceProvider.GetRequiredService<PermissionManager>().GetAccessDeniedMessage(context, AccessLevels.Administrator));
                    return;
                }
                if(serviceProvider.GetRequiredService<PermissionManager>().GetAccessLevel(context.User) < gco.CommandAccessLevel)
                {
                    await context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(context, "Wait... That's Illegal.", "You can't edit a command you don't have permission to use!", Color.DarkRed));
                    return;
                }
                if(asRestricted.HasValue) { gco.RequirePermission = asRestricted.Value; }
                if(newAction != "(unchanged)") { gco.Action = newAction; }
                ggo.SaveData();
                cgo.SaveData();
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "CmdMgr", "Global command modified!"));
                await context.Channel.SendMessageAsync("", false, GetCMDModified(context, cmdName, asRestricted, AccessLevels.CommandManager, newAction));
                return;
            }
            else
            {
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "CmdMgr", "WARNING: global command not found. Trying for context guild."));
                GuildCommand cco = cgo?.GuildCommands.FirstOrDefault(c => c.Name.ToLower() == cmdName.ToLower()) ?? null;//context
                if(cco != null)
                {
                    if (serviceProvider.GetRequiredService<PermissionManager>().GetAccessLevel(context.User) < cco.CommandAccessLevel)
                    {
                        await context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(context, "Wait... That's Illegal.", "You can't edit a command you don't have permission to use!", Color.DarkRed));
                        return;
                    }
                    if (serviceProvider.GetRequiredService<PermissionManager>().GetAccessLevel(context.User) < cco.CommandAccessLevel)
                    {
                        await context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(context, "Wait... That's Illegal.", "You can't edit a command you don't have permission to use!", Color.DarkRed));
                        return;
                    }
                    if (asRestricted.HasValue) { cco.RequirePermission = asRestricted.Value; }
                    if (newAction != "(unchanged)") { cco.Action = newAction; }
                    ggo.SaveData();
                    cgo.SaveData();
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "CmdMgr", "Context-guild command modified!"));
                    await context.Channel.SendMessageAsync("", false, GetCMDModified(context, cmdName, asRestricted, AccessLevels.CommandManager, newAction));
                    return;
                }
                else
                {
                    EmbedBuilder b = new EmbedBuilder();
                    b.WithAuthor(context.Client.CurrentUser);
                    b.WithColor(Color.DarkRed);
                    b.WithDescription("The command did not exist. You can only edit custom commands.");
                    b.WithTitle("Command Not Found");
                    await context.Channel.SendMessageAsync("", false, b.Build());
                    return;
                }
            }
        }

        public async Task EditCMD(ICommandContext context, string cmdName, AccessLevels CommandAccessLevel, string newAction = "(unchanged)", ulong gid = 0)
        {
            GuildObject ggo = guilds.FirstOrDefault(x => x.ID == 0);//global guild object.
            GuildObject cgo = guilds.FirstOrDefault(x => x.ID == gid);//context guild object.
            //first check for global.
            GuildCommand gco = ggo?.GuildCommands.FirstOrDefault(c => c.Name.ToLower() == cmdName.ToLower()) ?? null;
            if (gco != null)
            {
                if (serviceProvider.GetRequiredService<PermissionManager>().GetAccessLevel(context.User) < gco.CommandAccessLevel)
                {
                    await context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(context, "Wait... That's Illegal.", "You can't edit a command you don't have permission to use!", Color.DarkRed));
                    return;
                }
                gco.RequirePermission = CommandAccessLevel > AccessLevels.Normal;
                gco.CommandAccessLevel = CommandAccessLevel;
                
                if (newAction != "(unchanged)") { gco.Action = newAction; }
                ggo.SaveData();
                cgo.SaveData();
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "CmdMgr", "Global command modified!"));
                await context.Channel.SendMessageAsync("", false, GetCMDModified(context, cmdName, CommandAccessLevel > AccessLevels.Normal,CommandAccessLevel, newAction));
                return;
            }
            else
            {
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "CmdMgr", "WARNING: global command not found. Trying for context guild."));
                GuildCommand cco = cgo?.GuildCommands.FirstOrDefault(c => c.Name.ToLower() == cmdName.ToLower()) ?? null;
                if (cco != null)
                {
                    if (serviceProvider.GetRequiredService<PermissionManager>().GetAccessLevel(context.User) < cco.CommandAccessLevel)
                    {
                        await context.Channel.SendMessageAsync("", false, GetEmbeddedMessage(context, "Wait... That's Illegal.", "You can't edit a command you don't have permission to use!", Color.DarkRed));
                        return;
                    }
                    cco.RequirePermission = CommandAccessLevel > AccessLevels.Normal;
                    cco.CommandAccessLevel = CommandAccessLevel;
                    
                    if (newAction != "(unchanged)") { cco.Action = newAction; }
                    ggo.SaveData();
                    cgo.SaveData();
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "CmdMgr", "Context-guild command modified!"));
                    await context.Channel.SendMessageAsync("", false, GetCMDModified(context, cmdName, CommandAccessLevel > AccessLevels.Normal, CommandAccessLevel, newAction));
                    return;
                }
                else
                {
                    EmbedBuilder b = new EmbedBuilder();
                    b.WithAuthor(context.Client.CurrentUser);
                    b.WithColor(Color.DarkRed);
                    b.WithDescription("The command did not exist. You can only edit custom commands.");
                    b.WithTitle("Command Not Found");
                    await context.Channel.SendMessageAsync("", false, b.Build());
                    return;
                }
            }
        }

        #endregion

        #region GuildObject manipulation
        public bool AddGuildObject(GuildObject obj)
        {
            if(guilds.FirstOrDefault(x=>x.ID == obj.ID) != null)
            {
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "Guilds", $"Guild `{obj.ID}` was already in memory."));
                return false;
            }
            guilds.Add(obj);
            obj.SaveData();
            return true;
        }

        public void DeleteGuildObject(GuildObject obj)
        {
            if (guilds.FirstOrDefault(x => x.ID == obj.ID) == null)
            {
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Critical, "Guilds", "Guild object doesn't exist!"));
                return;
            }

            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Guilds", $"Deleting guild data: {obj.ID}.guild"));

            obj.DeleteData();
            guilds.Remove(obj);
        }
        #endregion

        #region Reusable embeds
        Embed GetCMDModified(ICommandContext Context,string cmdName, bool? asRestricted, AccessLevels CommandAccessLevel= AccessLevels.CommandManager, string newAction="(unchanged)")
        {
            EmbedBuilder b = new EmbedBuilder();
            b.WithAuthor(Context.Client.CurrentUser);
            b.WithTitle("Command Modified");
            b.WithFooter("ModularBOT • CORE");
            if (!asRestricted.HasValue && newAction == "(unchanged)")
            {
                b.WithTitle("Nothing Changed!");
                b.WithDescription("You didn't make any changes to the command.");
                b.WithColor(Color.Red);
                return b.Build();
            }
            b.WithDescription("Congrats! The command has been edited.");
            b.WithColor(Color.Green);
            string displayAction = "";
            if(newAction.Contains('`'))
            {
                displayAction = newAction;
            }
            else { displayAction = $"`{newAction}`"; }
            b.AddField("New Action", displayAction);
            if(asRestricted.HasValue)
            {
                b.AddField("Minimum AccessLevel", asRestricted.Value ? $"`AccessLevels.{CommandAccessLevel}`" : "`AccessLevels.Normal`");
            }
            return b.Build();
        }

        public Embed GetEmbeddedMessage(ICommandContext Context, string title, string message, Color color, Exception e = null)
        {
            EmbedBuilder b = new EmbedBuilder();
            b.WithColor(color);
            b.WithAuthor(Context.Client.CurrentUser);
            b.WithTitle(title);
            b.WithDescription(message);
            b.WithFooter("ModularBOT • Core");
            if (e != null)
            {
                b.AddField("Extended Details", e.Message);
                b.AddField("For developers", e.StackTrace);
            }
            return b.Build();
        }
        #endregion
    }



}
