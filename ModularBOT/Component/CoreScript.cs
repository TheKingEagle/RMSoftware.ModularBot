using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ModularBOT.Entity;

namespace ModularBOT.Component
{
    internal class CoreScript
    {
        private CustomCommandManager ccmgr;
        internal Dictionary<string, object> Variables { get; set; }
        private CommandService cmdsvr;
        private IServiceProvider services;
        private short OutputCount = 0;
        public CoreScript(CustomCommandManager ccmgr,
            ref IServiceProvider _services, Dictionary<string, object> dict = null)
        {
            cmdsvr = _services.GetRequiredService<CommandService>();
            services = _services;
            this.ccmgr = ccmgr;
            if (dict == null)
            {
                Variables = new Dictionary<string, object>();
            }
            else
            {
                Variables = dict;
            }
            Task.Run(() => OutputThrottleRS());//new thread throttle loop check.
        }

        /// <summary>
        /// These are variable names that are defined by the custom commands class.
        /// They are not managed by the CoreScript in any way, therefore must be protected.
        /// </summary>
        internal readonly string[] SystemVars =
        {
            #region Bot Instance
            "self",
            "self_nick",
            "self_avatar",

            #endregion

            #region Command Invoker
            "invoker",
            "invoker_nomention",
            "invoker_avatar",
            "invoker_nick",

            #endregion

            #region Bot Owner
            "bot_owner",
            "bot_owner_nomention",
            "bot_owner_avatar",

            #endregion
     
            #region Statistics/Guild Info
            "command",
            "command_count",
            "latency",
            "prefix","pf",
            "version",
            "os_name",
            "os_bit",
            "os_ver",
            "bot_mem",
            "guild",
            "guild_id",
            "guild_count",
            "guild_usercount",
            "guild_icon",
            "channel",
            "channel_id",
            "counter",
            #endregion
            
            #region Guild Owner
            "guild_owner",
            "go_avatar",
            "go_nick"

            #endregion

        };

        #region Public Methods
        public void Set(string var, object value)
        {
            if(var.Length > 20)
            {
                throw (new ArgumentException("Variable name limit exceeded! Keep variable names 20 characters or less"));
            }
            if (string.IsNullOrEmpty((string)value))
            {
                throw (new ArgumentException($"You cannot set `{var}` to a value of `null`"));
            }
            if (SystemVars.Contains(var))
            {
                throw (new ArgumentException("This variable cannot be modified."));
            }
            bool result = Variables.TryGetValue(var, out object v);
            if (!result)
            {
                //add the new variable.
                Variables.Add(var, value);
                services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Debug, "Variables", $"Result False. Creating variable. Name:{var}; Value: {value}"));
                return;
            }
            else
            {

                Variables[var] = value;
                Variables = Variables;
                services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Debug, "Variables", $"Result true. modifying variable. Name:{var}; Value: {Variables[var]}"));
                return;
            }
        }

        public object Get(string var)
        {
            bool result = Variables.TryGetValue(var, out object v);
            if (!result)
            {
                return null;
            }
            else
            {
                return v;
            }

        }

        public string ProcessVariableString(GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message)
        {
            string Processed = response;
            
            #region Bot Instance
            if (Processed.Contains("%self%"))
            {
                Processed = Processed.Replace("%self%", client.CurrentUser.Mention);
            }
            if (Processed.Contains("%self_avatar%"))
            {
                Processed = Processed.Replace("%self_avatar%", client.CurrentUser.GetAvatarUrl(ImageFormat.Auto,512));
            }
            if (Processed.Contains("%self_nick%"))
            {
                string snick = "Not specified";
                if (client.CurrentUser is SocketGuildUser sgu)
                {
                    snick = sgu.Nickname ?? "Not specified";
                }

                Processed = Processed.Replace("%self_nick%", snick);
            }

            #endregion

            #region Command Invoker
            if (Processed.Contains("%invoker%"))
            {
                Processed = Processed.Replace("%invoker%", message.Author.Mention);
            }
            if (Processed.Contains("%invoker_nick%"))
            {
                SocketGuildUser sgu = message.Author as SocketGuildUser;
                string nick = "Not specified";
                if(sgu != null)
                {
                    nick = sgu.Nickname ?? "Not specified";
                }
                Processed = Processed.Replace("%invoker_nick%", nick);
            }
            if (Processed.Contains("%invoker_nomention%"))
            {
                Processed = Processed.Replace("%invoker_nomention%", message.Author.Username+"#"+message.Author.Discriminator);
            }
            if (Processed.Contains("%invoker_avatar%"))
            {
                Processed = Processed.Replace("%invoker_avatar%", message.Author.GetAvatarUrl(ImageFormat.Auto, 512));
            }

            #endregion

            #region Bot Owner
            if (Processed.Contains("%bot_owner%"))
            {
                Processed = Processed.Replace("%bot_owner%", client.GetApplicationInfoAsync().GetAwaiter().GetResult().Owner.Mention);
            }
            if (Processed.Contains("%bot_owner_nomention%"))
            {
                IUser b_own = client.GetApplicationInfoAsync().GetAwaiter().GetResult().Owner;
                Processed = Processed.Replace("%bot_owner_nomention%", b_own.Username + "#" + b_own.Discriminator);
            }
            if (Processed.Contains("%bot_owner_avatar%"))
            {
                IUser b_own = client.GetApplicationInfoAsync().GetAwaiter().GetResult().Owner;
                Processed = Processed.Replace("%bot_owner_avatar%", b_own.GetAvatarUrl(ImageFormat.Auto,512));
            }

            #endregion

            #region Statistics/Guild info
            if (Processed.Contains("%command%"))
            {
                Processed = Processed.Replace("%command%", cmd.Name);
            }
            if (Processed.Contains("%command_count%"))
            {
                int c = gobj.GuildCommands.Count + cmdsvr.Commands.Count();
                Processed = Processed.Replace("%command_count%", c.ToString());
            }
            if (Processed.Contains("%latency%"))
            {
                DiscordShardedClient cl = client as DiscordShardedClient;
                int? l = null;
                if (message is SocketUserMessage a)
                {
                    if (a.Channel is SocketTextChannel c)
                    {
                        if (c.Guild != null)
                        {
                            l = cl.GetShardFor(c.Guild).Latency;
                        }
                    }
                }
                Processed = Processed.Replace("%latency%", (l ?? cl.Latency).ToString() + " ms");
            }
            if (Processed.Contains("%prefix%") || Processed.Contains("%pf%"))
            {

                Processed = Processed.Replace("%prefix%", gobj.CommandPrefix);

                Processed = Processed.Replace("%pf%", gobj.CommandPrefix.ToString());
            }
            if (Processed.Contains("%version%"))
            {
                Processed = Processed.Replace("%version%", Assembly.GetExecutingAssembly().GetName().Version.ToString(4));
            }
            if (Processed.Contains("%os_name%"))
            {
                Processed = Processed.Replace("%os_name%", SystemInfo.FriendlyName());
            }
            if (Processed.Contains("%os_bit%"))
            {
                Processed = Processed.Replace("%os_bit%", Environment.Is64BitOperatingSystem ? "x64" : "x86");
            }
            if (Processed.Contains("%os_ver%"))
            {
                OperatingSystem os = Environment.OSVersion;
                Processed = Processed.Replace("%os_ver%", os.Version.ToString());
            }
            if (Processed.Contains("%bot_mem%"))
            {
                Processed = Processed.Replace("%bot_mem%", SystemInfo.SizeSuffix(System.Diagnostics.Process.GetCurrentProcess().WorkingSet64));
            }
            if (Processed.Contains("%guild%"))
            {
                string Context = message.Author.Mention;
                if (message.Channel is IGuildChannel IGC)
                {
                    Context = IGC.Guild.Name;
                }
                Processed = Processed.Replace("%guild%", Context);
            }
            if (Processed.Contains("%guild_id%"))
            {
                string Context = message.Author.Id.ToString();
                if(message.Channel is IGuildChannel IGC)
                {
                    Context = IGC.GuildId.ToString();
                }
                Processed = Processed.Replace("%guild_id%", Context);
            }
            if (Processed.Contains("%guild_count%"))
            {
                DiscordShardedClient cl = client as DiscordShardedClient;
                Processed = Processed.Replace("%guild_count%", cl.Guilds.Count.ToString());
            }
            if (Processed.Contains("%guild_usercount%"))
            {
                if (message.Channel is IGuildChannel IGC)
                {
                    Task.Run(() => IGC.Guild.DownloadUsersAsync());
                    var ul = client.GetGuildAsync(IGC.Guild.Id, CacheMode.AllowDownload)
                        .GetAwaiter().GetResult().GetUsersAsync(CacheMode.AllowDownload).GetAwaiter().GetResult();
                    Processed = Processed.Replace("%guild_usercount%", ul.Count.ToString());
                }
                else
                {
                    Processed = Processed.Replace("%guild_usercount%", "2");//assume this is a DM. in which case it will always be two... (Groups aren't supported)
                }
            }
            if (Processed.Contains("%guild_icon%"))
            {
                string Context = message.Author.GetAvatarUrl(ImageFormat.Auto,512);
                if (message.Channel is IGuildChannel IGC)
                {
                    Context = client.GetGuildAsync(IGC.GuildId).GetAwaiter().GetResult().IconUrl;
                }
                Processed = Processed.Replace("%guild_icon%", Context);
            }
            if (Processed.Contains("%channel%"))
            {
                string Context = message.Author.Mention;
                if (message.Channel is IGuildChannel IGC)
                {
                    Context = "#"+IGC.Name;
                }
                Processed = Processed.Replace("%channel%", Context);
            }
            if (Processed.Contains("%channel_id%"))
            {
                string Context = message.Author.Id.ToString();
                if (message.Channel is IGuildChannel IGC)
                {
                    Context = IGC.Id.ToString();
                }
                Processed = Processed.Replace("%channel_id%", Context);
            }
            if (cmd != null)
            {
                if (Processed.Contains("%counter%"))
                {
                    if (cmd.Counter.HasValue)
                    {
                        cmd.Counter++;
                        gobj.SaveJson();
                        Processed = Processed.Replace("%counter%", cmd.Counter.ToString());
                    }
                    else
                    {
                        cmd.Counter = 1;
                        gobj.SaveJson();
                        Processed = Processed.Replace("%counter%", cmd.Counter.ToString());
                    }
                }
            } //("%counter%"))

            #endregion

            #region Guild Owner
            if (Processed.Contains("%guild_owner%"))
            {
                string GuildOwner = "Null";


                if(gobj.ID != 0)
                {
                    IGuild g = client.GetGuildAsync(gobj.ID).GetAwaiter().GetResult();
                    IGuildUser gu = g.GetOwnerAsync(CacheMode.AllowDownload).GetAwaiter().GetResult();
                    GuildOwner = gu.Username + "#" + gu.Discriminator;
                }
                Processed = Processed.Replace("%guild_owner%",GuildOwner);
            }
            if (Processed.Contains("%go_avatar%"))
            {
                string GuildOwnerav = "Null";


                if (gobj.ID != 0)
                {
                    IGuild g = client.GetGuildAsync(gobj.ID).GetAwaiter().GetResult();
                    IGuildUser gu = g.GetOwnerAsync(CacheMode.AllowDownload).GetAwaiter().GetResult();
                    GuildOwnerav = gu.GetAvatarUrl(ImageFormat.Auto,512);
                }
                Processed = Processed.Replace("%go_avatar%", GuildOwnerav);
            }
            if (Processed.Contains("%go_nick%"))
            {
                string GuildOwnernick = "Null";


                if (gobj.ID != 0)
                {
                    IGuild g = client.GetGuildAsync(gobj.ID).GetAwaiter().GetResult();
                    IGuildUser gu = g.GetOwnerAsync(CacheMode.AllowDownload).GetAwaiter().GetResult();
                    GuildOwnernick = gu.Nickname;
                }
                Processed = Processed.Replace("%go_nick%", GuildOwnernick);
            }
            #endregion

            //Check for use of Custom defined variables.

            foreach (Match item in Regex.Matches(Processed, @"%[^%]*%"))
            {
                string vname = item.Value.Replace("%", "");
                if (Get(vname) != null)
                {
                    string replacedvar = Get(vname).ToString();
                    Processed = Processed.Replace(item.Value, replacedvar);
                }
            }
            //Final variable.
            return Processed;
        }

        public async Task EvaluateScript(GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message)
        {
            int LineInScript = 0;
            bool error = false;
            bool contextToDM = false;
            ulong channelTarget = 0;
            EmbedBuilder errorEmbed = new EmbedBuilder();
            EmbedBuilder CSEmbed = new EmbedBuilder();
            CSEmbed.WithAuthor(client.CurrentUser);
            string attachmentpath = "";
            if (!response.EndsWith("```"))
            {
                error = true;
                errorEmbed.WithDescription("The codeblock was not closed.");
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
            }
            errorEmbed.WithAuthor(client.CurrentUser);
            errorEmbed.WithTitle("CoreScript Error");
            errorEmbed.WithColor(Color.Red);
            errorEmbed.WithFooter("CoreScript Engine • ModularBOT");
            bool terminated = false;
            //For the sake of in-chat scripts, they should be smaller.
            using (StringReader sr = new StringReader(response))
            {
                try
                {
                    while (!error)
                    {

                        if (sr.Peek() == -1)
                        {
                            if (!terminated)
                            {
                                error = true;
                                //errorMessage = $"SCRIPT ERROR:```The codeblock was not closed.\r\nCoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                errorEmbed.WithDescription("The codeblock was not closed.");
                                errorEmbed.AddField("Line", LineInScript, true);
                                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);

                            }
                        }
                        string line = await sr.ReadLineAsync();

                        if (line == ("```"))
                        {
                            terminated = true;
                            LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", "End of script!"), ConsoleColor.Green);
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
                                //errorMessage = $"SCRIPT ERROR:```\r\nUnexpected header:``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```\r\nAdditional info: Multi-line formatting required.";
                                errorEmbed.WithDescription($"Unexpected Header: ```{line}```");
                                errorEmbed.AddField("Additional Information", "You must type your script in multiple lines. Header should look like ```\r\n```DOS\r\n```");
                                errorEmbed.AddField("Line", LineInScript, true);
                                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);

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
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                LineInScript++;//ignore blank lines.
                                continue;
                            }
                            if (line.ToUpper().StartsWith("::") || line.ToUpper().StartsWith("REM") || line.ToUpper().StartsWith("//"))
                            {
                                //comment line.
                                services.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "CoreScript", "Comment: " + line));
                                LineInScript++;
                                continue;
                            }

                            if (line == "```DOS")
                            {
                                error = true;
                                //errorMessage = $"SCRIPT ERROR:```\r\nDuplicate header:``` ```{line.Split(' ')[0]}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                errorEmbed.WithDescription($"Duplicate Header: ```{line.Split(' ')[0]}```");
                                errorEmbed.AddField("Line", LineInScript, true);
                                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);

                                break;
                            }
                            //GET COMMAND PART.
                            string output = "";
                            switch (line.Split(' ')[0].ToUpper())
                            {
                                case ("ECHO"):
                                    OutputCount++;
                                    if (OutputCount > 4)
                                    {
                                        error = true;
                                        errorEmbed.WithDescription($"`ECHO` Function Error: Preemptive rate limit reached." +
                                            $" Please slow down your script with `WAIT`\r\n```{line}```");

                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    output = line.Remove(0, 5);
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(gobj, output, cmd, client, message)))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Output string cannot be empty. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);

                                        break;
                                    }
                                    if (contextToDM)
                                    {
                                        await message.Author.SendMessageAsync(ProcessVariableString(gobj, output, cmd, client, message), false);
                                    }
                                    else
                                    {
                                        if(channelTarget == 0)
                                        {
                                            await message.Channel.SendMessageAsync(ProcessVariableString(gobj, output, cmd, client, message), false);
                                        }
                                        else
                                        {
                                            SocketTextChannel channelfromid = await client.GetChannelAsync(channelTarget) as SocketTextChannel;
                                            await channelfromid.SendMessageAsync(ProcessVariableString(gobj, output, cmd, client, message), false);
                                        }
                                    }

                                    break;

                                case ("ROLE_ADD"):
                                    if (!client.GetGuildAsync(gobj.ID).GetAwaiter().GetResult()
                                        .GetCurrentUserAsync(CacheMode.AllowDownload).GetAwaiter().GetResult()
                                        .GuildPermissions.Has(GuildPermission.ManageRoles))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```\r\nFunction error: Expected format BOTGOLIVE <ChannelName> <status text>.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: I Don't have the proper permissions to assign roles.");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    OutputCount++;
                                    if (OutputCount > 4)
                                    {
                                        error = true;
                                        errorEmbed.WithDescription($"`ROLE_ADD` Function Error: Preemptive rate limit reached. Please slow down your script with `WAIT`\r\n```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    output = line.Remove(0, 9);
                                    output = ProcessVariableString(gobj, output, cmd, client, message);
                                    string[] arguments = output.Split(' ');
                                    if (string.IsNullOrWhiteSpace(output) || arguments.Length < 2)
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Syntax is not correct ```{line}```");
                                        errorEmbed.AddField("Usage", "`ROLE_ADD <ulong roleID> <string message>`");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd, true);
                                        break;
                                    }
                                    string arg1 = arguments[0];
                                    string arg2 = output.Remove(0, arg1.Length).Trim();
                                    if (ulong.TryParse(arg1, out ulong ulo))
                                    {
                                        IRole role = (await client.GetGuildAsync(gobj.ID)).GetRole(ulo);
                                        if (message.Author is SocketGuildUser sgu)
                                        {

                                            await sgu.AddRoleAsync(role);
                                            await Task.Delay(100);
                                            if (sgu.Roles.FirstOrDefault(rf => rf.Id == role.Id) != null)
                                            {
                                                EmbedBuilder bz = new EmbedBuilder();
                                                bz.WithTitle("Role Added!");
                                                bz.WithAuthor(client.CurrentUser);
                                                bz.WithColor(Color.Green);
                                                bz.WithDescription($"{arg2}");
                                                await message.Channel.SendMessageAsync("", false, bz.Build());
                                                break;
                                            }
                                            else
                                            {
                                                error = true;
                                                //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                                errorEmbed.WithDescription($"The role was not added. Please make sure bot has proper permission to add the role. ```{line}```");
                                                errorEmbed.AddField("Line", LineInScript, true);
                                                errorEmbed.AddField("Execution Context", cmd, true);
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"A ulong ID was expected. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd, true);
                                        break;
                                    }


                                    break;

                                case ("ROLE_ASSIGN"):
                                    if (cmd.CommandAccessLevel < AccessLevels.CommandManager || !cmd.RequirePermission)
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```\r\nFunction error: Expected format BOTGOLIVE <ChannelName> <status text>.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: This requires `AccessLevels.CommandManager`");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    if (!client.GetGuildAsync(gobj.ID).GetAwaiter().GetResult()
                                        .GetCurrentUserAsync(CacheMode.AllowDownload).GetAwaiter().GetResult()
                                        .GuildPermissions.Has(GuildPermission.ManageRoles))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```\r\nFunction error: Expected format BOTGOLIVE <ChannelName> <status text>.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: I don't have permission to manage roles.");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    OutputCount++;
                                    if (OutputCount > 4)
                                    {
                                        error = true;
                                        errorEmbed.WithDescription($"`ROLE_ASSIGN` Function Error: Preemptive rate limit reached. Please slow down your script with `WAIT`\r\n```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    output = line.Remove(0, 12);
                                    output = ProcessVariableString(gobj, output, cmd, client, message);
                                    string[] aarguments = output.Split(' ');
                                    if (string.IsNullOrWhiteSpace(output) || aarguments.Length < 3)
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Syntax is not correct ```{line}```");
                                        errorEmbed.AddField("Usage", "`ROLE_ASSIGN <ulong roleID> <User Mention> <string message>`");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd, true);
                                        break;
                                    }
                                    string aarg1 = aarguments[0];
                                    string aarg2 = aarguments[1];
                                    string aarg3 = output.Remove(0, $"{aarg1} {aarg2}".Length).Trim();
                                    UserTypeReader<SocketGuildUser> SF = new UserTypeReader<SocketGuildUser>();
                                    CommandContext cde = new CommandContext(client, (IUserMessage)message);
                                    TypeReaderResult s = SF.ReadAsync(cde, aarg2, services).GetAwaiter().GetResult();
                                    if (!ulong.TryParse(aarg1, out ulong aulo))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"A ulong ID was expected for Argument 1. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd.Name, true);
                                        break;
                                    }
                                    if (!s.IsSuccess)
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"A Guild User was expected in Argument 2 ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd, true);
                                        break;
                                    }
                                    if (string.IsNullOrWhiteSpace(aarg3))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Argument 3 cannot be empty. Please specify a message ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd, true);
                                        break;
                                    }
                                    IRole arole = (await client.GetGuildAsync(gobj.ID)).GetRole(aulo);
                                    if (s.BestMatch is SocketGuildUser asgu)
                                    {

                                        await asgu.AddRoleAsync(arole);
                                        await Task.Delay(100);
                                        if (asgu.Roles.FirstOrDefault(rf => rf.Id == arole.Id) != null)
                                        {
                                            EmbedBuilder bz = new EmbedBuilder();
                                            bz.WithTitle("Role Assigned!");
                                            bz.WithAuthor(client.CurrentUser);
                                            bz.WithColor(Color.Green);
                                            bz.WithDescription($"{aarg3}");
                                            await message.Channel.SendMessageAsync("", false, bz.Build());
                                            break;
                                        }
                                        else
                                        {
                                            error = true;
                                            //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                            errorEmbed.WithDescription($"The role was not added. Please make sure bot has proper permission to add the role. ```{line}```");
                                            errorEmbed.AddField("Line", LineInScript, true);
                                            errorEmbed.AddField("Execution Context", cmd, true);
                                            break;
                                        }
                                    }

                                    break;

                                case ("ROLE_DEL"):
                                    if (!client.GetGuildAsync(gobj.ID).GetAwaiter().GetResult()
                                        .GetCurrentUserAsync(CacheMode.AllowDownload).GetAwaiter().GetResult()
                                        .GuildPermissions.Has(GuildPermission.ManageRoles))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```\r\nFunction error: Expected format BOTGOLIVE <ChannelName> <status text>.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: I don't have permission to manage roles.");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    OutputCount++;
                                    if (OutputCount > 4)
                                    {
                                        error = true;
                                        errorEmbed.WithDescription($"`ROLE_DEL` Function Error: Preemptive rate limit reached. Please slow down your script with `WAIT`\r\n```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    output = line.Remove(0, 9);
                                    output = ProcessVariableString(gobj, output, cmd, client, message);
                                    string[] arguments1 = output.Split(' ');
                                    if (string.IsNullOrWhiteSpace(output) || arguments1.Length < 2)
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Syntax is not correct ```{line}```");
                                        errorEmbed.AddField("Usage", "`ROLE_ADD <ulong roleID> <string message>`");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd, true);
                                        break;
                                    }
                                    string arg01 = arguments1[0];
                                    string arg02 = output.Remove(0, arg01.Length).Trim();
                                    if (ulong.TryParse(arg01, out ulong ulo1))
                                    {
                                        IRole role = (await client.GetGuildAsync(gobj.ID)).GetRole(ulo1);
                                        if (message.Author is SocketGuildUser sgu)
                                        {

                                            await sgu.RemoveRoleAsync(role);
                                            await Task.Delay(100);
                                            if (sgu.Roles.FirstOrDefault(rf => rf.Id == role.Id) == null)
                                            {
                                                EmbedBuilder bz = new EmbedBuilder();
                                                bz.WithTitle("Role Removed!");
                                                bz.WithAuthor(client.CurrentUser);
                                                bz.WithColor(Color.LightOrange);
                                                bz.WithDescription($"{arg02}");
                                                await message.Channel.SendMessageAsync("", false, bz.Build());
                                                break;
                                            }
                                            else
                                            {
                                                error = true;
                                                //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                                errorEmbed.WithDescription($"The role could not be removed ```{line}```");
                                                errorEmbed.AddField("Line", LineInScript, true);
                                                errorEmbed.AddField("Execution Context", cmd, true);
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"A ulong ID was expected. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd, true);
                                        break;
                                    }


                                    break;

                                case ("EMBED")://embed <TITLE>

                                    //Get the line removing echo.
                                    output = line.Remove(0, 6);
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(gobj, output, cmd, client, message)))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Title string cannot be empty. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);

                                        break;
                                    }
                                    CSEmbed = new EmbedBuilder();
                                    CSEmbed.WithTitle(ProcessVariableString(gobj, output, cmd, client, message));
                                    CSEmbed.WithAuthor(client.CurrentUser);
                                    break;

                                case ("EMBED_DESC")://embed_desc <text>

                                    //Get the line removing echo.
                                    output = line.Remove(0, 10);
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(gobj, output, cmd, client, message)))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"String cannot be empty. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }

                                    CSEmbed.WithDescription(ProcessVariableString(gobj, output, cmd, client, message).Replace("&q", "\"").Replace("&nl;", "\r\n"));
                                    break;

                                case ("DELMSG")://embed_desc <text>

                                    if(message.Channel is SocketTextChannel)
                                    {
                                        IMessage m = message;
                                        SocketTextChannel msgsoc = message.Channel as SocketTextChannel;
                                        if(msgsoc.Guild != null)
                                        {
                                            if ((await (await client.GetGuildAsync(msgsoc.Guild.Id)).GetCurrentUserAsync(CacheMode.AllowDownload)).GuildPermissions.Has(GuildPermission.ManageMessages))
                                            {
                                                await message.DeleteAsync();
                                            }
                                        }
                                        message = m;//keep it in reference.
                                    }
                                    break;

                                case ("ATTACH")://embed_desc <text>

                                    //Get the line removing ATTACH.
                                    output = line.Remove(0, 7);
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(gobj, output, cmd, client, message)))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Attachment path cannot be empty. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    if(!File.Exists(@"attachments\" + output))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Attachment could not be found. ```{line}```");
                                        errorEmbed.AddField("Path", "`../attachments/" + output+"`", false);
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }

                                    attachmentpath = @"attachments\" + output;
                                    if (contextToDM)
                                    {
                                        try
                                        {
                                            await message.Author.SendFileAsync(attachmentpath);
                                        }
                                        catch (Exception ex)
                                        {

                                            error = true;
                                            //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                            errorEmbed.WithDescription($"The script failed due to an exception ```{line}```");

                                            errorEmbed.AddField("details", $"```{ex.Message}```");
                                            errorEmbed.AddField("Line", LineInScript, true);
                                            errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                            break;
                                        }

                                    }
                                    else
                                    {

                                        await message.Channel.SendFileAsync(attachmentpath);

                                    }
                                    break;

                                case ("EMBED_IMAGE")://embed_image <url>

                                    //Get the line removing echo.
                                    output = line.Remove(0, 11);
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(gobj, output, cmd, client, message)))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"String cannot be empty. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }

                                    CSEmbed.WithImageUrl(output);
                                    break;

                                case ("EMBED_THIMAGE")://embed_thimage <url>

                                    //Get the line removing echo.
                                    output = line.Remove(0, 13);
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(gobj, output, cmd, client, message)))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"String cannot be empty. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }

                                    CSEmbed.WithThumbnailUrl(ProcessVariableString(gobj, output, cmd, client, message));
                                    break;

                                case ("EMBED_FOOTER")://embed footer text

                                    //Get the line removing echo.
                                    output = line.Remove(0, 12);
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(gobj, output, cmd, client, message)))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"String cannot be empty. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }

                                    CSEmbed.WithFooter(output);
                                    break;

                                case ("EMBED_FOOTER_I")://embed footer text
                                    if (CSEmbed.Footer == null)
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"You must use `EMBED_FOOTER` first. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    //Get the line removing echo.
                                    output = line.Remove(0, 14);
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(gobj, output, cmd, client, message)))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"String cannot be empty. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    try
                                    {
                                        CSEmbed.Footer.IconUrl = output;
                                    }
                                    catch (ArgumentException ex)
                                    {

                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Argument Exception: `{ex.Message}` ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    break;

                                case ("EMBED_COLOR")://embed footer text

                                    //Get the line removing echo.
                                    output = line.Remove(0, 11);
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(gobj, output, cmd, client, message)))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"String cannot be empty. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    string o = output.Replace("#", "").ToUpper().Trim();
                                    uint c = 9;
                                    c = (uint)Convert.ToUInt32(o, 16);
                                    CSEmbed.WithColor(c);
                                    break;

                                case ("SET_TARGET"):

                                    //Get the line removing echo.
                                    output = line.Remove(0, 11);
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(gobj, output, cmd, client, message)))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Invalid target context. ```{line}```");

                                        errorEmbed.AddField("Available targets", "• CHANNEL\r\n• DIRECT");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    if (output.ToUpper().StartsWith("CHANNEL"))
                                    {
                                        contextToDM = false;
                                        ulong tempid = 0;
                                        if(output.ToUpper() != "CHANNEL")
                                        {
                                            string ulparse = output.Split(' ')[1];
                                            if(!ulong.TryParse(ulparse, out tempid))
                                            {
                                                error = true;
                                                //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                                errorEmbed.WithDescription($"Invalid Channel ID format. ```{line}```");

                                                errorEmbed.AddField("Available targets", "• `CHANNEL [Optional Channel ID]`\r\n• `DIRECT`");
                                                errorEmbed.AddField("Line", LineInScript, true);
                                                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                                break;
                                            }
                                            else
                                            {
                                                if(await client.GetChannelAsync(tempid) == null)
                                                {
                                                    error = true;
                                                    //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                                    errorEmbed.WithDescription($"The channel with specified ID did not exist ```{line}```");

                                                    errorEmbed.AddField("Available targets", "• `CHANNEL [Optional Channel ID]`\r\n• `DIRECT`");
                                                    errorEmbed.AddField("Line", LineInScript, true);
                                                    errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                                    break;
                                                }
                                                else
                                                {
                                                    if((await client.GetChannelAsync(tempid)) is SocketTextChannel)
                                                    {
                                                        channelTarget = tempid;
                                                    }
                                                    else
                                                    {
                                                        error = true;
                                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                                        errorEmbed.WithDescription($"The provided ID was for a valid text channel. ```{line}```");

                                                        errorEmbed.AddField("Available targets", "• `CHANNEL [Optional Channel ID]`\r\n• `DIRECT`");
                                                        errorEmbed.AddField("Line", LineInScript, true);
                                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if (output.ToUpper() == "DIRECT")
                                    {
                                        contextToDM = true;
                                    }
                                    break;

                                case ("EMBED_SEND")://embed footer text
                                    OutputCount++;
                                    if (OutputCount > 4)
                                    {
                                        error = true;
                                        errorEmbed.WithDescription($"`EMBED_SEND` Function Error: Preemptive rate limit reached. Please slow down your script with `WAIT`\r\n```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        return;
                                    }
                                    //Get the line removing echo.
                                    if (contextToDM)
                                    {
                                        try
                                        {
                                            await message.Author.SendMessageAsync("", false, CSEmbed.Build());
                                        }
                                        catch (Exception ex)
                                        {

                                            error = true;
                                            //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                            errorEmbed.WithDescription($"The script failed due to an exception ```{line}```");

                                            errorEmbed.AddField("details", $"```{ex.Message}```");
                                            errorEmbed.AddField("Line", LineInScript, true);
                                            errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                            break;
                                        }

                                    }
                                    else
                                    {
                                        if(channelTarget == 0)
                                        {
                                            await message.Channel.SendMessageAsync("", false, CSEmbed.Build());
                                        }
                                        else
                                        {
                                            SocketTextChannel channelfromid = await client.GetChannelAsync(channelTarget) as SocketTextChannel;
                                            await channelfromid.SendMessageAsync("", false, CSEmbed.Build());
                                        }

                                    }


                                    break;
                                case ("EMBED_ADDFIELD")://embed_addfield <name> <contents> (Quotes required.)

                                    //Get the line removing echo.
                                    output = line.Remove(0, 15);
                                    output = ProcessVariableString(gobj, output, cmd, client, message);
                                    Regex r = new Regex("\"[^\"]*\"");
                                    #region ERRORS
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(gobj, output, cmd, client, message)))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"The Syntax of the command is incorrect. ```{line}```");
                                        errorEmbed.AddField("Usage", "```\nEMBED_ADDFIELD \"Title in quotes\" \"Content in quotes\"\n```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    if (r.Matches(output).Count < 2)
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"The Syntax of the command is incorrect. ```{line}```");
                                        errorEmbed.AddField("Usage", "```\nEMBED_ADDFIELD \"Title in quotes\" \"Content in quotes\"\n```");
                                        errorEmbed.AddField("NOTES:", "• The title & content will always be set by the first two group of quotes.\r\n• If you want to have double-quotes within the content or title use `&q` before and after the content you want to quote.");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    #endregion

                                    string emtitle = r.Matches(output)[0].Value.Replace("&q", "\"").Replace("&nl;", "\r\n");
                                    string content = r.Matches(output)[1].Value.Replace("&q", "\"").Replace("&nl;", "\r\n");

                                    #region MORE ERROR HANDLES
                                    if (string.IsNullOrWhiteSpace(emtitle))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Title cannot be empty! ```{line}```");
                                        errorEmbed.AddField("Usage", "```\nEMBED_ADDFIELD \"Title in quotes\" \"Content in quotes\"\n```");
                                        errorEmbed.AddField("NOTES:", "• The title & content will always be set by the first two group of quotes.\r\n• If you want to have double-quotes within the content or title use `&q` before and after the content you want to quote.");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    if (string.IsNullOrWhiteSpace(content))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Content cannot be empty! ```{line}```");
                                        errorEmbed.AddField("Usage", "```\nEMBED_ADDFIELD \"Title in quotes\" \"Content in quotes\"\n```");
                                        errorEmbed.AddField("NOTES:", "• The title & content will always be set by the first two group of quotes.\r\n• If you want to have double-quotes within the content or title use `&q` before and after the content you want to quote.");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    #endregion

                                    CSEmbed.AddField(emtitle, content);
                                    break;
                                case ("EMBED_ADDFIELD_I")://embed_addfield <name> <contents> (Quotes required.)

                                    //Get the line removing echo.
                                    output = line.Remove(0, 17);
                                    output = ProcessVariableString(gobj, output, cmd, client, message);
                                    Regex ri = new Regex("\"[^\"]*\"");
                                    #region ERRORS
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(gobj, output, cmd, client, message)))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"The Syntax of the command is incorrect. ```{line}```");
                                        errorEmbed.AddField("Usage", "```\nEMBED_ADDFIELD \"Title in quotes\" \"Content in quotes\"\n```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    if (ri.Matches(output).Count < 2)
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"The Syntax of the command is incorrect. ```{line}```");
                                        errorEmbed.AddField("Usage", "```\nEMBED_ADDFIELD \"Title in quotes\" \"Content in quotes\"\n```");
                                        errorEmbed.AddField("NOTES:", "• The title & content will always be set by the first two group of quotes.\r\n• If you want to have double-quotes within the content or title use `&q` before and after the content you want to quote.");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    #endregion

                                    string emtitlei = ri.Matches(output)[0].Value.Replace("\"", "").Replace("&q", "\"");
                                    string contenti = ri.Matches(output)[1].Value.Replace("\"", "").Replace("&q", "\"");

                                    #region MORE ERROR HANDLES
                                    if (string.IsNullOrWhiteSpace(emtitlei))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Title cannot be empty! ```{line}```");
                                        errorEmbed.AddField("Usage", "```\nEMBED_ADDFIELD \"Title in quotes\" \"Content in quotes\"\n```");
                                        errorEmbed.AddField("NOTES:", "• The title & content will always be set by the first two group of quotes.\r\n• If you want to have double-quotes within the content or title use `&q` before and after the content you want to quote.");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    if (string.IsNullOrWhiteSpace(contenti))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Content cannot be empty! ```{line}```");
                                        errorEmbed.AddField("Usage", "```\nEMBED_ADDFIELD \"Title in quotes\" \"Content in quotes\"\n```");
                                        errorEmbed.AddField("NOTES:", "• The title & content will always be set by the first two group of quotes.\r\n• If you want to have double-quotes within the content or title use `&q` before and after the content you want to quote.");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    #endregion

                                    CSEmbed.AddField(emtitlei, contenti, true);
                                    break;
                                case ("ECHOTTS"):
                                    //Get the line removing echo.
                                    OutputCount++;
                                    if (OutputCount > 4)
                                    {
                                        error = true;
                                        errorEmbed.WithDescription($"`ECHOTTS` Function Error: Preemptive rate limit reached. Please slow down your script with `WAIT`\r\n```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }

                                    if (cmd.CommandAccessLevel < AccessLevels.CommandManager || !cmd.RequirePermission)
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```\r\nFunction error: Expected format BOTGOLIVE <ChannelName> <status text>.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: This requires `AccessLevels.CommandManager`");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    output = line.Remove(0, 8);
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(gobj, output, cmd, client, message)))
                                    {
                                        error = true;
                                        errorEmbed.WithDescription($"Output string cannot be empty. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    if (contextToDM)
                                    {
                                        try
                                        {
                                            await message.Author.SendMessageAsync(ProcessVariableString(gobj, output, cmd, client, message), true);
                                        }
                                        catch (Exception ex)
                                        {

                                            error = true;
                                            //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                            errorEmbed.WithDescription($"The script failed due to an exception ```{line}```");

                                            errorEmbed.AddField("details", $"```{ex.Message}```");
                                            errorEmbed.AddField("Line", LineInScript, true);
                                            errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                            break;
                                        }

                                    }
                                    else
                                    {
                                        await message.Channel.SendMessageAsync(ProcessVariableString(gobj, output, cmd, client, message), true);
                                    }

                                    break;
                                case ("SETVAR"):
                                    CaseSetVar(line, ref error, ref errorEmbed, ref LineInScript, ref cmd);

                                    break;
                                case ("CMD"):
                                    //SocketMessage m = message as SocketMessage;
                                    CaseExecCmd(ProcessVariableString(gobj, line, cmd, client, message), ccmgr, gobj, ref error, ref errorEmbed, ref LineInScript, ref client, ref cmd, ref message);

                                    break;
                                case ("BOTSTATUS"):
                                    OutputCount++;
                                    if (OutputCount > 2)
                                    {
                                        error = true;
                                        errorEmbed.WithDescription($"`BOTSTATUS` Function Error: Preemptive rate limit reached. Please slow down your script with `WAIT`\r\n```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    if (cmd.CommandAccessLevel < AccessLevels.Administrator)
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```\r\nFunction error: Expected format BOTGOLIVE <ChannelName> <status text>.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: This requires `AccessLevels.Administrator`");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    await ((DiscordShardedClient)client).SetGameAsync(ProcessVariableString(gobj, line.Remove(0, 10), cmd, client, message));
                                    break;
                                case ("STATUSORB"):
                                    OutputCount++;
                                    if (OutputCount > 2)
                                    {
                                        error = true;
                                        errorEmbed.WithDescription($"`STATUSORB` Function Error: Preemptive rate limit reached. Please slow down your script with `WAIT`\r\n```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    if (cmd.CommandAccessLevel < AccessLevels.Administrator)
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```\r\nFunction error: Expected format BOTGOLIVE <ChannelName> <status text>.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: This requires `AccessLevels.Administrator`");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    string cond = line.Remove(0, 10).ToUpper();
                                    switch (cond)
                                    {
                                        case ("ONLINE"):
                                            await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.Online);
                                            break;
                                        case ("AWAY"):
                                            await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.Idle);
                                            break;
                                        case ("AFK"):
                                            await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.AFK);
                                            break;
                                        case ("BUSY"):
                                            await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.DoNotDisturb);
                                            break;
                                        case ("OFFLINE"):
                                            await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.Offline);
                                            break;
                                        case ("INVISIBLE"):
                                            await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.Invisible);
                                            break;
                                        default:
                                            error = true;
                                            //errorMessage = $"SCRIPT ERROR:```\r\nUnexpected Argument: {cond}. Try either ONLINE, BUSY, AWAY, AFK, INVISIBLE, OFFLINE.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                            errorEmbed.WithDescription($"Function error. Unexpected argument: {cond}.\r\nTry either ONLINE, BUSY, AWAY, AFK, INVISIBLE, OFFLINE.");
                                            errorEmbed.AddField("Line", LineInScript, true);
                                            errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                            break;
                                    }

                                    break;
                                case ("BOTGOLIVE"):
                                    OutputCount++;
                                    if (OutputCount > 2)
                                    {
                                        error = true;
                                        errorEmbed.WithDescription($"`BOTGOLIVE` Function Error: Preemptive rate limit reached. Please slow down your script with `WAIT`\r\n```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    if (cmd.CommandAccessLevel < AccessLevels.Administrator)
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```\r\nFunction error: Expected format BOTGOLIVE <ChannelName> <status text>.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: This requires `AccessLevels.Administrator`");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    string linevar = ProcessVariableString(gobj, line, cmd, client, message);
                                    string[] data = linevar.Remove(0, 10).Split(' ');
                                    if (data.Length < 2)
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```\r\nFunction error: Expected format BOTGOLIVE <ChannelName> <status text>.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: Expected format ```BOTGOLIVE <ChannelName> <status text>.```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    string statusText = line.Remove(0, 10 + data[0].Length + 1).Trim();

                                    await ((DiscordShardedClient)client).SetGameAsync(statusText, $"https://twitch.tv/{data[0]}", ActivityType.Streaming);
                                    break;
                                case ("WAIT"):
                                    int v = 1;
                                    if (!int.TryParse(ProcessVariableString(gobj, line.Remove(0, 5), cmd, client, message), out v))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```\r\nA number was expected here. You gave: {line.Remove(0, 5)}\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: Expected a valid number greater than zero & below the maximum value supported by the system. You gave: `{line.Remove(0, 5)}`");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        break;
                                    }
                                    if (v < 1)
                                    {
                                        //errorMessage = $"SCRIPT ERROR:```\r\nA number was expected here. You gave: {line.Remove(0, 5)}\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: Expected a valid number greater than zero & below the maximum value supported by the system. You gave: `{line.Remove(0, 5)}`");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                        error = true;
                                        break;
                                    }
                                    await Task.Delay(v);
                                    break;
                                default:
                                    error = true;
                                    //errorMessage = $"SCRIPT ERROR:```\r\nUnexpected core function: {line.Split(' ')[0]}\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                    errorEmbed.WithDescription($"Unexpected function: ```{line.Split(' ')[0]}```");
                                    errorEmbed.AddField("Line", LineInScript, true);
                                    errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                    break;
                            }

                        }
                        await Task.Delay(20);
                        LineInScript++;
                    }
                }
                catch (Exception ex)
                {

                    error = true;
                    //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                    errorEmbed.WithDescription($"The script failed due to an exception.");

                    errorEmbed.AddField("details", $"```{ex.Message}```");
                    errorEmbed.AddField("Line", LineInScript, true);
                    errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                }
                
            }
            if (error)
            {
                await message.Channel.SendMessageAsync("", false, errorEmbed.Build());
                return;
            }
        }

        public async Task EvaluateScriptFile(GuildObject gobj, string filename, IDiscordClient client, IMessage message, GuildCommand cmd = null)
        {
            int LineInScript = 1;
            bool error = false;
            EmbedBuilder errorEmbed = new EmbedBuilder();
            errorEmbed.WithAuthor(client.CurrentUser);
            errorEmbed.WithTitle("CoreScript Error");
            errorEmbed.WithColor(Color.Red);
            errorEmbed.WithFooter("CoreScript Engine • RMSoftware.ModularBOT");
            //This method supports reading a script from a file. This script does not require ```DOS header.
            using (FileStream fs = File.OpenRead(filename))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    while (!error)
                    {

                        if (sr.Peek() == -1)
                        {
                            LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", "End of script!"), ConsoleColor.Green);
                            break;
                        }
                        string line = await sr.ReadLineAsync();
                        if(string.IsNullOrWhiteSpace(line))
                        {
                            LineInScript++;
                            continue;
                        }
                        if (LineInScript >= 1)
                        {
                            if (line.ToUpper().StartsWith("::") || line.ToUpper().StartsWith("REM") || line.ToUpper().StartsWith("//"))
                            {
                                //comment line.
                                LineInScript++;
                                continue;
                            }


                            //GET COMMAND PART.
                            string output = "";
                            switch (line.Split(' ')[0].ToUpper())
                            {
                                case ("ECHO"):

                                    //Get the line removing echo.
                                    output = line.Remove(0, 5);
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(gobj, output, null, client, message)))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```Output string cannot be empty.``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Output string cannot be empty. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", Path.GetFileName(filename), true);
                                        break;
                                    }
                                    await message.Channel.SendMessageAsync(ProcessVariableString(gobj, output, null, client, message), false);

                                    break;
                                
                                case ("ECHOTTS"):
                                    //Get the line removing echo.
                                    output = line.Remove(0, 8);
                                    if (string.IsNullOrWhiteSpace(ProcessVariableString(gobj, output, null, client, message)))
                                    {
                                        error = true;
                                        errorEmbed.WithDescription($"Output string cannot be empty. ```{line}```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", Path.GetFileName(filename), true);
                                        break;
                                    }
                                    await message.Channel.SendMessageAsync(ProcessVariableString(gobj, output, null, client, message), true);

                                    break;
                                case ("SETVAR"):
                                    CaseSetVar(line, ref error, ref errorEmbed, ref LineInScript, ref cmd);

                                    break;
                                case ("CMD"):
                                    //SocketMessage m = message as SocketMessage;
                                    CaseExecCmd(ProcessVariableString(gobj, line, null, client, message), ccmgr, gobj, ref error, ref errorEmbed, ref LineInScript, ref client, ref cmd, ref message);

                                    break;
                                case ("BOTSTATUS"):
                                    await ((DiscordShardedClient)client).SetGameAsync(ProcessVariableString(gobj, line.Remove(0, 10), cmd, client, message));
                                    break;
                                case ("STATUSORB"):
                                    string cond = line.Remove(0, 10).ToUpper();
                                    switch (cond)
                                    {
                                        case ("ONLINE"):
                                            await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.Online);
                                            break;
                                        case ("AWAY"):
                                            await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.Idle);
                                            break;
                                        case ("AFK"):
                                            await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.AFK);
                                            break;
                                        case ("BUSY"):
                                            await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.DoNotDisturb);
                                            break;
                                        case ("OFFLINE"):
                                            await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.Offline);
                                            break;
                                        case ("INVISIBLE"):
                                            await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.Invisible);
                                            break;
                                        default:
                                            error = true;
                                            //errorMessage = $"SCRIPT ERROR:```\r\nUnexpected Argument: {cond}. Try either ONLINE, BUSY, AWAY, AFK, INVISIBLE, OFFLINE.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                            break;
                                    }

                                    break;
                                case ("BOTGOLIVE"):
                                    string linevar = ProcessVariableString(gobj, line, cmd, client, message);
                                    string[] data = linevar.Remove(0, 10).Split(' ');
                                    if (data.Length < 2)
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```\r\nFunction error: Expected format BOTGOLIVE <ChannelName> <status text>.\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: Expected format ```BOTGOLIVE <ChannelName> <status text>.```");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", Path.GetFileName(filename), true);
                                        break;
                                    }
                                    string statusText = line.Remove(0, 10 + data[0].Length + 1).Trim();

                                    await ((DiscordShardedClient)client).SetGameAsync(statusText, $"https://twitch.tv/{data[0]}", ActivityType.Streaming);
                                    break;
                                case ("WAIT"):
                                    int v = 1;
                                    if (!int.TryParse(ProcessVariableString(gobj, line.Remove(0, 5), cmd, client, message), out v))
                                    {
                                        error = true;
                                        //errorMessage = $"SCRIPT ERROR:```\r\nA number was expected here. You gave: {line.Remove(0, 5)}\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: Expected a valid number greater than zero & below the maximum value supported by the system. You gave: `{line.Remove(0, 5)}`");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", Path.GetFileName(filename), true);
                                        break;
                                    }
                                    if (v < 1)
                                    {
                                        //errorMessage = $"SCRIPT ERROR:```\r\nA number was expected here. You gave: {line.Remove(0, 5)}\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                        errorEmbed.WithDescription($"Function error: Expected a valid number greater than zero & below the maximum value supported by the system. You gave: `{line.Remove(0, 5)}`");
                                        errorEmbed.AddField("Line", LineInScript, true);
                                        errorEmbed.AddField("Execution Context", Path.GetFileName(filename), true);
                                        error = true;
                                        break;
                                    }
                                    await Task.Delay(v);
                                    break;
                                default:
                                    error = true;
                                    //errorMessage = $"SCRIPT ERROR:```\r\nUnexpected core function: {line.Split(' ')[0]}\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                                    errorEmbed.WithDescription($"Unexpected function: ```{line.Split(' ')[0]}```");
                                    errorEmbed.AddField("Line", LineInScript, true);
                                    errorEmbed.AddField("Execution Context", Path.GetFileName(filename), true);
                                    break;
                            }

                        }
                        await Task.Delay(20);
                        LineInScript++;
                    }
                }
                if (error)
                {
                    await message.Channel.SendMessageAsync("", false, errorEmbed.Build());
                    return;
                }
            }
        }
        #endregion

        #region Private methods
        private void CaseSetVar(string line, ref bool error, ref EmbedBuilder errorEmbed, ref int LineInScript, ref GuildCommand cmd)
        {
            string output = line;
            if (output.Split(' ').Length < 3)
            {
                error = true;
                //errorMessage = $"SCRIPT ERROR:```\r\nThe syntax of this function is incorrect.```"
                //+ $" ```Function {line.Split(' ')[0]}```" +
                //$"```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```";
                errorEmbed.WithDescription($"The Syntax of this function is incorrect. ```{line}```");
                errorEmbed.AddField("Function", line.Split(' ')[0]);
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
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

                errorEmbed.WithDescription($"{ex.Message}\r\n");
                errorEmbed.AddField("Function", "```" + line.Split(' ')[0] + "```", true);
                errorEmbed.AddField("Variable Name", "```" + varname + "```", true);

                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return;
            }

        }

        private void CaseExecCmd(string line, CustomCommandManager ccmg, GuildObject guildObject, ref bool error, ref EmbedBuilder errorEmbed, ref int LineInScript,
            ref IDiscordClient client, ref GuildCommand cmd, ref IMessage ArgumentMessage)
        {
            
            ulong gid = 0;
            if (ArgumentMessage.Channel is SocketGuildChannel channel)
            {
                gid = channel.Guild.Id;
            }
            guildObject = ccmg.GuildObjects.FirstOrDefault(x => x.ID == gid) ?? ccmg.GuildObjects.FirstOrDefault(x => x.ID == 0);
            
            string ecmd = line.Remove(0, 4);
            if (cmd != null)
            {

                if (ecmd == cmd.Name)
                {
                    error = true;
                    errorEmbed.WithDescription($"CMD Function Error: You cannot call this command here, that's API abuse.\r\n```{line}```");
                    errorEmbed.AddField("Line", LineInScript, true);
                    errorEmbed.AddField("Execution Context", ecmd ?? "No context", true);
                    return;
                }
            }
            string resp = ccmg.ProcessMessage(new PseudoMessage(guildObject.CommandPrefix + ecmd, ArgumentMessage.Author as SocketUser,
                (ArgumentMessage.Channel as IGuildChannel), MessageSource.Bot));
            if (resp != "SCRIPT" && resp != "EXEC" && resp != "" && resp != "CLI_EXEC" && resp != null)
            {

                ArgumentMessage.Channel.SendMessageAsync(resp);
                LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", line));
                LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", "CustomCMD Success..."));
                return;
            }
            if ((resp == "SCRIPT" || resp == "EXEC" || resp == "" || resp == "CLI_EXEC") && resp != null)
            {

                LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", line));
                LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", "CustomCMD Success..."));
                return;
            }
            //Damn, I can't be sassy here... If it was a command, but not a ccmg command, then try the context for modules. If THAT didn't work
            //Then it will output the result of the context.
            var context = new CommandContext(client, new PseudoMessage(guildObject.CommandPrefix + ecmd, ArgumentMessage.Author as SocketUser, (ArgumentMessage.Channel as IGuildChannel), MessageSource.Bot));
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = cmdsvr.ExecuteAsync(context, guildObject.CommandPrefix.Length, services);
            LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", line));
            LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", result.Result.ToString()));
            if (!result.Result.IsSuccess)
            {
                error = true;
                errorEmbed.WithDescription($"CMD Function Error: The command context returned the following error:\r\n`{result.Result.ErrorReason}`\r\n```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", ecmd ?? "No context", true);
                return;
            }
        }

        private void LogToConsole(LogMessage msg)
        {

            services.GetRequiredService<ConsoleIO>().WriteEntry(msg);

            if (msg.Exception != null)
            {
                services.GetRequiredService<ConsoleIO>().WriteErrorsLog(msg.Exception);
            }
        }

        public void LogToConsole(LogMessage msg, ConsoleColor entryColor)
        {
            services.GetRequiredService<ConsoleIO>().WriteEntry(msg, entryColor);

            if (msg.Exception != null)
            {
                services.GetRequiredService<ConsoleIO>().WriteErrorsLog(msg.Exception);
            }
        }

        private void OutputThrottleRS()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(5000);
                OutputCount = 0;//reset every 5 seconds.
            }
        }

        #endregion
    }
}