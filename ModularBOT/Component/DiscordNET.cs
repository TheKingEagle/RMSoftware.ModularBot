using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.Net.WebSockets;
using Discord.WebSocket;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using ModularBOT.Entity;

namespace ModularBOT.Component
{
    public class DiscordNET
    {
        #region Fields
        public CommandService cmdsvr = new Discord.Commands.CommandService();          //Discord Command Service
        public IServiceCollection services;                                            //Discord Service collection
        public IServiceProvider serviceProvider;                                       //Discord Service provider
        public bool InputCanceled = false;                                             //ModularBOT Console Read Operation
        private static bool init_start = false;                                        //DiscordNET Conditional Initialization
        private bool Initialized = false;                                              //Conditional Completed Initialization
        private bool LogConnected = false;                                             //Conditional Log channel found & connected
        private bool LoginEventsCalled = false;                                        //yet another thread locker for offload ready... this is infuriating.
        private List<SocketMessage> messageQueue = new List<SocketMessage>();          //DiscordNET Message Queue
        private Dictionary<ulong, short> userBL = new Dictionary<ulong, short>();      //Auto Blacklisting dictionary for users who like to spam...
        private string lastrecieved = "";
        #endregion

        #region Properties
        public bool DisableMessages { get; set; } = false;                     //DiscordNET Message Processing Flag
        public DateTime ClientStartTime { get; private set; }                  //DiscordNET client session Start Date
        public DateTime InstanceStartTime { get; private set; }                //DiscordNET client instance Start Date
        public CustomCommandManager CustomCMDMgr { get; private set; }         //ModularBOT Custom Commands Manager
        public PermissionManager PermissionManager { get; private set; }       //ModularBOT Permission Manager
        public ModuleManager ModuleMgr { get; private set; }                   //ModularBOT Module Manager
        public UpdateManager Updater { get; private set; }                     //ModularBOT Update Manager
        public DiscordShardedClient Client { get; private set; }               //Discord Sharded Client

        #endregion

        #region Methods
        public void Start(ref ConsoleIO consoleIO, ref Configuration AppConfig, ref bool ShutdownRequest, ref bool RestartRequested,ref bool FromCrash)
        {
            try
            {
                
                DisableMessages = true;//Do not allow messages until bot is fully logged in.
                Initialized = false;
                string token = AppConfig.AuthToken;

                services = new ServiceCollection();
                services.AddSingleton(AppConfig);
                services.AddSingleton(Program.configMGR);
                services.AddSingleton(consoleIO);
                services.AddSingleton(cmdsvr);
                services.AddSingleton(this);
                serviceProvider = services.BuildServiceProvider();
                
                Client = new DiscordShardedClient(new DiscordSocketConfig
                {
                    MessageCacheSize = 20,
                    AlwaysDownloadUsers = true,
                    LogLevel = AppConfig.DiscordEventLogLevel,

                    // TODO: Figure out a way to automatically set this later.
                    TotalShards = AppConfig.ShardCount
                });

                services.AddSingleton(Client);

                services.AddSingleton(new Discord.Addons.Interactive.InteractiveService(Client));

                serviceProvider = services.BuildServiceProvider();
                Client.Log += Client_Log;

                Client.LoggedIn += Client_LoggedIn;
                Client.ShardReady += Client_ShardReady;
                Client.ShardConnected += Client_ShardConnected;
                Client.MessageReceived += Client_MessageReceived;
                Client.ShardDisconnected += Client_ShardDisconnected;
                Client.GuildAvailable += Client_GuildAvailable;
                Client.GuildUnavailable += Client_GuildUnavailable;
                Client.GuildUpdated += Client_GuildUpdated;
                Client.JoinedGuild += Client_JoinedGuild;


                InstanceStartTime = DateTime.Now;
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "I-Uptime", $"Instance start time set to {InstanceStartTime}"));

                Task.Run(() => StartTimeoutKS(10000 * serviceProvider.GetRequiredService<Configuration>().ShardCount, "Discord INIT attempt"));
                Task.Run(async () => await Client.LoginAsync(TokenType.Bot, token));
                SpinWait.SpinUntil(() => Client.LoginState == LoginState.LoggedIn);//wait for the client to login before starting...
                Task.Run(async () => await Client.StartAsync());
                
                Client.SetStatusAsync(UserStatus.DoNotDisturb);//go into DND mode.
                SpinWait.SpinUntil(() => init_start);//Hold thread until needed shard is ready.
                SpinWait.SpinUntil(() => LoginEventsCalled);//Don't instruct core to init until client finished login event.
                Task.Run(() => ResetUserCom());//Start auto-blacklist timer reset system...
                if (AppConfig.LoadCoreModule)
                {
                    cmdsvr.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);//ADD CORE.
                }
                if(!AppConfig.LoadCoreModule)
                {
                    consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "Modules", "You have disabled CoreModule! " +
                        "You will not be able to manage commands or view system stats, unless you have your own implementations!"));
                }
                OffloadReady(ref FromCrash, ref ShutdownRequest, ref RestartRequested);
               
                
            }
            catch (Discord.Net.HttpException httex)
            {
                if (httex.HttpCode == System.Net.HttpStatusCode.Unauthorized)
                {

                     RestartRequested = consoleIO.ShowKillScreen("Unauthorized", 
                         "The server responded with error 401. Make sure your authorization token is correct.", false,
                         ref ShutdownRequest, ref RestartRequested,5, httex, "DNET_HTTPEX_UNAUTHORIZED").GetAwaiter().GetResult();
                }
                if (httex.DiscordCode == 4007)
                {
                    RestartRequested = consoleIO.ShowKillScreen("Invalid Client ID", "The server responded with error 4007.", true,
                        ref ShutdownRequest, ref RestartRequested, 5, httex, "DNET_HTTPEX_INVALID_ID").GetAwaiter().GetResult();
                }
                if (httex.DiscordCode == 5001)
                {
                    RestartRequested = consoleIO.ShowKillScreen("guild timed out", "The server responded with error 5001.", true, 
                        ref ShutdownRequest, ref RestartRequested, 5, httex, "DNET_HTTPEX_TIMED_OUT").GetAwaiter().GetResult();
                }

                else
                {
                    RestartRequested = consoleIO.ShowKillScreen("HTTP_EXCEPTION", "The server responded with an error. SEE Crash.LOG for more info.",
                        true, ref ShutdownRequest, ref RestartRequested, 5, httex, "DNET_HTTPEX_UNKNOWN_ERROR").GetAwaiter().GetResult();
                }
            }

            catch (Exception ex)
            {
                RestartRequested = consoleIO.ShowKillScreen("Unexpected Error", ex.Message, true, ref ShutdownRequest, ref RestartRequested, 5, ex,"DNET_START_ERROR")
                    .GetAwaiter()
                    .GetResult();
            }
        }

        private Task Client_GuildUpdated(SocketGuild arg1, SocketGuild arg2)
        {
            Task.Run(()=> SyncGuild(arg2));
            return Task.Delay(0);
        }

        private Task Client_LoggedIn()
        {
            SpinWait.SpinUntil(() => Client.LoginState == LoginState.LoggedIn);
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Client", "Client is logged in! We can now start permissions system."));
            PermissionManager = new PermissionManager(serviceProvider);
            services.AddSingleton(PermissionManager);
            serviceProvider = services.BuildServiceProvider();
            ModuleMgr = new ModuleManager(ref cmdsvr, ref services, ref serviceProvider);
            CustomCMDMgr = new CustomCommandManager(serviceProvider);
            services.AddSingleton(CustomCMDMgr);
            serviceProvider = services.BuildServiceProvider();
            Updater = new UpdateManager(serviceProvider);
            LoginEventsCalled = true;
            return Task.Delay(1);
        }

        
        public void SyncGuild(SocketGuild arg)
        {
            foreach (SocketRole item in arg.Roles)
            {
                if (item.Permissions.Has(GuildPermission.Administrator) || item.Permissions.Has(GuildPermission.ManageGuild) || item.Permissions.Has(GuildPermission.ManageChannels))
                {
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Guilds", $"Role {item.Name} has met the criteria."));

                    if (PermissionManager.GetAccessLevel(item) < AccessLevels.CommandManager)
                    {
                        serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Guilds", $"Found a role that can manage guilds: " +
                        $"{item.Name} <@&{item.Id}>. Registering role as CommandManager!"));
                        PermissionManager.RegisterEntityNS(item, AccessLevels.CommandManager);
                    }
                }
            }
        }

        public void Stop(ref bool ShutdownRequest)
        {
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Permissions", $"Saving permissions @ Stop"));

            PermissionManager.SaveJson();
            if(Client != null)
            {
                Client.SetStatusAsync(UserStatus.Invisible);

                Client.LogoutAsync();
                Client.StopAsync();
                ShutdownRequest = true;
            }
        }

        private void OffloadReady(ref bool recovered,ref bool shutdownRequested,ref bool RestartRequested)
        {
            try
            {
                if (!Initialized)
                {
                    //Client.SetStatusAsync(UserStatus.DoNotDisturb);
                    ulong id = serviceProvider.GetRequiredService<Configuration>().LogChannel;
                    if (recovered)
                    {
                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithAuthor(Client.CurrentUser);
                        builder.WithTitle("WARNING");
                        builder.WithDescription("The program was auto-restarted due to a crash. Please see `Crash.LOG` and `Errors.LOG` for details.");
                        builder.WithColor(new Color(255, 255, 0));
                        builder.WithFooter("ModularBOT • Core");
                        ((SocketTextChannel)Client.GetChannel(id)).SendMessageAsync("", false, builder.Build());
                        serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "TaskMgr", "The program auto-restarted due to a crash. Please see Crash.LOG."));
                    }

                    #region Startup.CORE
                    IGuildChannel i = (IGuildChannel)Client.GetChannel(id);
                    if (i == null)
                    {
                        InputCanceled = true;
                        ConsoleIO.PostMessage(ConsoleIO.GetConsoleWindow(), ConsoleIO.WM_KEYDOWN, ConsoleIO.VK_RETURN, 0);
                        serviceProvider.GetRequiredService<ConsoleIO>().ShowKillScreen("TaskManager Exception", "You specified an invalid guild channel ID. Please verify your guild channel's ID and try again.", false, ref shutdownRequested, 
                            ref RestartRequested, 0, new ArgumentException("Guild channel was invalid.", "botChannel"),"DNET_INIT_INVALID");
                        
                        Stop(ref shutdownRequested);
                        return;
                    }
                    GuildObject obj = CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == i.Guild.Id) ?? CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == 0);
                    try
                    {
                        CustomCMDMgr.coreScript.EvaluateScriptFile(obj, "startup.core", Client, new PseudoMessage("", Client.CurrentUser, (IGuildChannel)Client.GetChannel(id), MessageSource.Bot)).GetAwaiter().GetResult();
                        Initialized = true;
                    }
                    catch (FileNotFoundException ex)
                    {
                        InputCanceled = true;
                        ConsoleIO.PostMessage(ConsoleIO.GetConsoleWindow(), ConsoleIO.WM_KEYDOWN, ConsoleIO.VK_RETURN, 0);
                        serviceProvider.GetRequiredService<ConsoleIO>().ShowKillScreen("TaskManager Exception", $"{ex.Message}", false, 
                            ref shutdownRequested, ref RestartRequested, 0, ex,"DNET_CORE_FILE_MISSING");
                        Stop(ref shutdownRequested);

                        return;
                    }

                    #endregion

                    #region Update Check
                    if (serviceProvider.GetRequiredService<Configuration>().CheckForUpdates.Value)
                    {
                        serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "TaskMgr", "Checking for updates."));
                        bool pre = serviceProvider.GetRequiredService<Configuration>().UseInDevChannel.Value;
                        bool availableUpdates = Updater.CheckUpdate(pre).GetAwaiter().GetResult();
                        if (availableUpdates)
                        {
                            string verdata = pre ? Updater.UpdateInfo.PREVERS : Updater.UpdateInfo.VERSION;
                            EmbedBuilder builder = new EmbedBuilder();
                            builder.WithAuthor(Client.CurrentUser);
                            builder.WithTitle("UPDATE AVAILABLE");
                            builder.WithThumbnailUrl(Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto, 256));
                            builder.WithDescription("A new version is available for download! From the console, use the `update` command to download and install.");
                            builder.AddField("⚠ Installed", $"`v{Assembly.GetExecutingAssembly().GetName().Version.ToString(4)}`", true);
                            builder.AddField("✅ Latest", $"`v{verdata}`", true);
                            builder.AddField("📂 Updates Channel", $"`{(pre ? "INDEV":"RELEASE")}`", true);
                            builder.WithColor(new Color(0, 255, 60));
                            builder.WithFooter("ModularBOT • Core");
                            ((SocketTextChannel)Client.GetChannel(id)).SendMessageAsync("", false, builder.Build());
                            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Critical, "UPDATE", $"A new version is available! v{verdata}"),ConsoleColor.Green);
                            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Critical, "UPDATE", $"use the 'update' command to download and install."),ConsoleColor.Green);
                        }
                        else
                        {
                            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Critical, "UPDATE", $"You are running the most recent version."),ConsoleColor.Black);
                        }
                    }


                    #endregion
                    Initialized = true;
                    DisableMessages = false;
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "TaskMgr", "Processing Message Queue."));
                    foreach (var item in messageQueue)
                    {
                        Client_MessageReceived(item).GetAwaiter().GetResult();
                        Task.Delay(500);
                    }
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "TaskMgr", "Task is complete."));
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "TaskMgr", "Client Status update!"));
                    //Finished task manager.
                    Client.SetStatusAsync(serviceProvider.GetRequiredService<Configuration>().ReadyStatus);
                    Client.SetGameAsync(serviceProvider.GetRequiredService<Configuration>().ReadyText,null,serviceProvider.GetRequiredService<Configuration>().ReadyActivity);
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "TaskMgr", "Task is complete."));

                    
                }
            }
            catch (HttpException httx)
            {
                if (httx.DiscordCode == 50001)
                {
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Critical, "CRITICAL", "The bot was unable to perform needed operations. Please make sure it has the following permissions: Read messages, Read message history, Send Messages, Embed Links, Attach Files. (Calculated: 117760)", httx));
                }
            }
            catch (Exception ex)
            {
                Initialized = false;
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Error, "TaskMgr", ex.Message, ex));

            }
        }

        private void StartTimeoutKS( int msec_timeout, string eventDescription="Generic Event")
        {
            SpinWait.SpinUntil(() => LogConnected == true, msec_timeout);
            if(!LogConnected)
            {
                try
                {
                    throw (new TimeoutException($"The specified operation timed out: {eventDescription}"));
                }
                catch (Exception ex)
                {

                    serviceProvider.GetRequiredService<ConsoleIO>().ShowKillScreen("Operation Timed out", $"The specified operation timed out: {eventDescription}",
                    true, ref Program.ShutdownCalled, ref Program.RestartRequested, 5,ex, "DNET_TIME_OUT");
                }
                
                
            }
            else
            {
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "TaskMgr", "Log Connected within time limit."));
            }

        }

        private void SetUserCom(ulong userID, short count)
        {
            if(PermissionManager.GetAccessLevel(Client.GetUser(userID)) > AccessLevels.Normal)
            {
                return;//ignore cmdmgr+ rank...
            }
            if(userBL.ContainsKey(userID))
            {
                
                userBL[userID] = count;
            }
            else
            {
                userBL.Add(userID, count);
            }

        }

        private void ResetUserCom()
        {
            while (true)
            {
                Thread.Sleep(12000);
                userBL.Clear();
            }
        }

        #endregion

        #region Events
        private Task Client_GuildUnavailable(SocketGuild guild)
        {
            string guildName = guild.Name.Length > 20 ? guild.Name.Remove(17) + "..." : guild.Name;
            SocketTextChannel c = guild.GetTextChannel(serviceProvider.GetRequiredService<Configuration>().LogChannel);
            if (c != null)
            {
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Guilds", 
                    $"Requested initialization channel ({c.Name}) became unavailable."));
                
                LogConnected = false;
            }
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "Guilds", $"A guild just vanished. [{guildName}] "));
            return Task.Delay(0);
        }

        private async Task Client_GuildAvailable(SocketGuild guild)
        {
            //Console.Title = "RMSoftware.ModularBOT -> " + guild.CurrentUser + " | Connected to " + Client.Guilds.Count + " guilds.";
            string guildName = guild.Name.Length > 20 ? guild.Name.Remove(17) + "..." : guild.Name;

            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Guilds", 
                $"A guild just appeared. [{guildName}] "),ConsoleColor.Green);
            SocketTextChannel c = guild.GetTextChannel(serviceProvider.GetRequiredService<Configuration>().LogChannel);
            if ( c != null)
            {
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Guilds", 
                    $"Requested initialization channel ({c.Name}) has been found. {guild.Name} currently has it!"));
                ClientStartTime = DateTime.Now;
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "Uptime", $"Client SessionStart time set to {ClientStartTime}"));
                LogConnected = true;
            }
            await Task.Delay(0);
#pragma warning disable 4014
            Task.Run(() => SyncGuild(guild));//don't really care about result in this case. just want a new thread.
#pragma warning restore 4014
        }

        private Task Client_ShardDisconnected(Exception arg1, DiscordSocketClient arg2)
        {
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Error, "Shards", $"A shard was disconnected! {arg2.Guilds.Count} guild(s) lost contact. "));
            if(!Program.ShutdownCalled)
            {
                //Set timeout based on shard count.
                //Ie: 1 shard: 10 second timeout; 10 shards: 100 seconds.
                Task.Run(() => StartTimeoutKS(10000 * serviceProvider.GetRequiredService<Configuration>().ShardCount, "Discord re-connection Attempt"));
            }
            return Task.Delay(0);
        }

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            if(!Initialized)
            {
                messageQueue.Add(arg);
                return;
            }
            if (DisableMessages) return;

            lastrecieved = arg.Content;
            if (!(arg is SocketUserMessage message)) return;
            ulong gid = 0;//global by default
            string prefix = serviceProvider.GetRequiredService<Configuration>().CommandPrefix;
            if ((arg.Channel as SocketGuildChannel) != null)
            {
                SocketGuildChannel sc = arg.Channel as SocketGuildChannel;
                gid = sc.Guild.Id;
            }
            if (!string.IsNullOrWhiteSpace(CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == gid)?.CommandPrefix))
            {
                prefix = CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == gid)?.CommandPrefix;
            }

            if (message.Content.StartsWith(prefix))
            {
                if (PermissionManager.GetAccessLevel(arg.Author) == AccessLevels.Blacklisted)
                {
                    if (PermissionManager.GetWarnOnBlacklist(arg.Author))
                    {
                        EmbedBuilder donttalktome = new EmbedBuilder();
                        donttalktome.WithAuthor(Client.CurrentUser);
                        donttalktome.WithColor(Color.DarkRed);
                        donttalktome.WithTitle("Access Denied");
                        donttalktome.WithDescription("You are currently unable to communicate with this bot. You will only see this warning once.");
                        await arg.Channel.SendMessageAsync("", false, donttalktome.Build());
                    }
                    return;
                }
            }

            #region @Mention command
            if (message.Content.StartsWith($"<@{Client.CurrentUser.Id}>") || message.Content.StartsWith($"<@!{Client.CurrentUser.Id}>")) //This command will ALWAYS be a thing. it cannot be overridden.
            {
                if (arg.Author.IsBot)//do not allow bots to mention command. PERIOD.
                {
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "BOT", "Someone tried to bait the bot with a bot..."));
                    return;
                }
                
                if (PermissionManager.GetAccessLevel(arg.Author) == AccessLevels.Blacklisted)
                {
                    if (PermissionManager.GetWarnOnBlacklist(arg.Author))
                    {
                        EmbedBuilder donttalktome = new EmbedBuilder();
                        donttalktome.WithAuthor(Client.CurrentUser);
                        donttalktome.WithColor(Color.DarkRed);
                        donttalktome.WithTitle("Access Denied");
                        donttalktome.WithDescription("You are currently unable to communicate with this bot. You will only see this warning once.");
                        await arg.Channel.SendMessageAsync("", false, donttalktome.Build());
                    }
                    return;
                }

                #region Log
                string mcgontext = "Direct Message";
                if (message.Channel is SocketGuildChannel)
                {
                    mcgontext = ((SocketGuildChannel)message.Channel).Guild.Name.Length > 20 ?
                        ((SocketGuildChannel)message.Channel).Guild.Name.Remove(17) + "..." : ((SocketGuildChannel)message.Channel).Guild.Name;

                }
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Mention",
                    $"<#{message.Channel.Name} [{mcgontext}]> {message.Author.Username}#{message.Author.Discriminator}: {message.Content}"));
                #endregion

                string cm1 = $"<@{Client.CurrentUser.Id}>";
                string cm2 = $"<@!{Client.CurrentUser.Id}>";
                if (message.Content.StartsWith($"{cm1} ")) //modules via mention
                {
                    string cm1result = "";

                    await Task.Run(() => cm1result = CustomCMDMgr.ProcessMessage(arg, cm1 + " "));
                    if (cm1result != "SCRIPT" && cm1result != "EXEC" && cm1result != "" && cm1result != "CLI_EXEC" && cm1result != null)
                    {
                        await arg.Channel.SendMessageAsync(cm1result);
                        return;
                    }
                    if ((cm1result == "SCRIPT" || cm1result == "EXEC" || cm1result == "" || cm1result == "CLI_EXEC") && cm1result != null)
                    {
                        return;
                    }
                    await ExecuteModuleCMD(message, cm1+" ");
                    return;
                }
                if (message.Content.StartsWith($"{cm2} ")) //modules via nick mention
                {
                    string cm2result = "";

                    await Task.Run(() => cm2result = CustomCMDMgr.ProcessMessage(arg, cm2 + " "));
                    if (cm2result != "SCRIPT" && cm2result != "EXEC" && cm2result != "" && cm2result != "CLI_EXEC" && cm2result != null)
                    {
                        await arg.Channel.SendMessageAsync(cm2result);
                        return;
                    }
                    if ((cm2result == "SCRIPT" || cm2result == "EXEC" || cm2result == "" || cm2result == "CLI_EXEC") && cm2result != null)
                    {
                        return;
                    }
                    await ExecuteModuleCMD(message, cm2 + " ");
                    return;
                }
                EmbedBuilder prefixer = new EmbedBuilder();
                prefixer.WithAuthor(Client.CurrentUser);
                prefixer.WithColor(new Color(244, 255, 12));
                prefixer.WithTitle("You rang?");
                prefixer.WithDescription($"Hi {arg.Author.Mention} My prefix is `{prefix}`");
                await arg.Channel.SendMessageAsync("", false, prefixer.Build());


                return;
            }
            #endregion
            
            if (!message.Content.StartsWith(prefix))
            {
                return;
            }
            if (arg.Author.IsBot && !PermissionManager.IsEntityRegistered(arg.Author)) return;//ignore bots unless bot is registered in the permission system!
            
            #region Log
            string cgontext = "Direct Message";
            if (message.Channel is SocketGuildChannel)
            {
                cgontext = ((SocketGuildChannel)message.Channel).Guild.Name.Length > 20 ?
                    ((SocketGuildChannel)message.Channel).Guild.Name.Remove(17) + "..." : ((SocketGuildChannel)message.Channel).Guild.Name;

            }
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Commands",
                $"<#{message.Channel.Name} [{cgontext}]> {message.Author.Username}#{message.Author.Discriminator}: {message.Content}"));
            #endregion

            string result = "";

            await Task.Run(() => result = CustomCMDMgr.ProcessMessage(arg));
            if (result != "SCRIPT" && result != "EXEC" && result != "" && result != "CLI_EXEC" && result != null)
            {
                var blm = CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == gid)?.BlacklistMode;
                if (blm > AutoBlacklistModes.Disabled && arg.Content.ToLower() == lastrecieved.ToLower())
                {
                    if(userBL.ContainsKey(arg.Author.Id))
                    {
                        SetUserCom(arg.Author.Id, (short)(userBL[arg.Author.Id] + 1));
                        if(userBL[arg.Author.Id] > 8)
                        {
                            serviceProvider.GetRequiredService<ConsoleIO>()
                                .WriteEntry(new LogMessage(LogSeverity.Critical, "AutoBL",
                                $"User {message.Author.Username}  was just blacklisted for command abuse!"));
                            PermissionManager.RegisterEntity(arg.Author, AccessLevels.Blacklisted,blm == AutoBlacklistModes.Standard);
                            return;
                        }
                    }
                    else
                    {
                        SetUserCom(arg.Author.Id, 1);
                        lastrecieved = arg.Content;
                    }
                }
                await arg.Channel.SendMessageAsync(result);
                return;
            }
            if ((result == "SCRIPT" || result == "EXEC" || result == "" || result == "CLI_EXEC") && result != null)
            {
                var blm = CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == gid)?.BlacklistMode;
                if (blm > AutoBlacklistModes.Disabled && arg.Content.ToLower() == lastrecieved.ToLower())
                {
                    if (userBL.ContainsKey(arg.Author.Id))
                    {
                        SetUserCom(arg.Author.Id, (short)(userBL[arg.Author.Id] + 1));
                        if (userBL[arg.Author.Id] > 8)
                        {
                            serviceProvider.GetRequiredService<ConsoleIO>()
                                .WriteEntry(new LogMessage(LogSeverity.Critical, "AutoBL",
                                $"User {message.Author.Username}  was just blacklisted for command abuse!"));
                            PermissionManager.RegisterEntity(arg.Author, AccessLevels.Blacklisted, blm == AutoBlacklistModes.Standard);
                            return;
                        }

                    }

                    else
                    {
                        SetUserCom(arg.Author.Id, 1);
                    }
                }
                return;
            }
            await ExecuteModuleCMD(message, prefix);
            await Task.Delay(1);
        }

        private async Task ExecuteModuleCMD(SocketUserMessage message, string prefix)
        {
            
            var context = new CommandContext(Client, message);

            #region Guild-specific module check
            var search = cmdsvr.Search(context, prefix.Length);
            if(search.IsSuccess)
            {
                var c = search.Commands;
                //get module name
                string module = c.First().Command.Module.Name;
                var m = serviceProvider.GetRequiredService<DiscordNET>().ModuleMgr.Modules.FirstOrDefault(x => x.ModuleGroups.Contains(module));
                if (m != null)
                {
                    if(m.GuildsAvailable.Count > 0)
                    {
                        if(m.GuildsAvailable.FirstOrDefault(gid => gid == context.Guild?.Id) == 0)//if no match for guild, don't execute.
                        {
                            serviceProvider.GetRequiredService<ConsoleIO>()
                                .WriteEntry(new LogMessage(LogSeverity.Verbose, "ModuleCMD", "Module was called but wrong guild."));
                            return;
                        }
                    }
                }
            }
            #endregion

            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var cmdres = await cmdsvr.ExecuteAsync(context, prefix.Length, serviceProvider);

            if (cmdres.IsSuccess)
            {
                ulong gid = 0;
                if(message.Channel is SocketGuildChannel sgc)
                {
                    gid = sgc.Guild.Id;
                }
                var blm = CustomCMDMgr.GuildObjects.FirstOrDefault(x => x.ID == gid)?.BlacklistMode;
                if (blm > AutoBlacklistModes.Disabled && message.Content.ToLower() == lastrecieved.ToLower())
                {
                    if (userBL.ContainsKey(message.Author.Id))
                    {
                        SetUserCom(message.Author.Id, (short)(userBL[message.Author.Id] + 1));
                        if (userBL[message.Author.Id] > 8)
                        {
                            serviceProvider.GetRequiredService<ConsoleIO>()
                                .WriteEntry(new LogMessage(LogSeverity.Critical, "AutoBL", 
                                $"User {message.Author.Username}  was just blacklisted for command abuse!"));
                            PermissionManager.RegisterEntity(message.Author, AccessLevels.Blacklisted, blm == AutoBlacklistModes.Standard);
                            return;
                        }
                    }

                    else
                    {
                        SetUserCom(message.Author.Id, 1);
                        lastrecieved = message.Content;
                    }
                }
            }//on successful execution, check for abuse.

            if (cmdres.Error.HasValue)
            {
                if (!cmdres.Error.Value.HasFlag(CommandError.UnknownCommand) && !cmdres.IsSuccess)
                {
                    EmbedBuilder b = new EmbedBuilder();
                    b.WithColor(Color.Orange);
                    b.WithAuthor(Client.CurrentUser);
                    b.WithTitle("Command Error!");
                    b.WithDescription(cmdres.ErrorReason);
                    b.AddField("Error Code", cmdres.Error.Value);
                    await context.Channel.SendMessageAsync("", false, b.Build());
                }
                if (cmdres.Error.Value.HasFlag(CommandError.BadArgCount))
                {
                    EmbedBuilder b = new EmbedBuilder();
                    b.WithColor(Color.Orange);
                    b.WithAuthor(Client.CurrentUser);
                    b.WithTitle("Command Error.");
                    b.WithDescription(cmdres.ErrorReason);
                    b.AddField("Error Code", cmdres.Error.Value);
                    await context.Channel.SendMessageAsync("", false, b.Build());
                }
            }
        }

        private Task Client_ShardConnected(DiscordSocketClient arg)
        {
            //Console.Title = "RMSoftware.ModularBOT -> " + arg.CurrentUser + " | Connected to " + Client.Guilds.Count + " guilds.";
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Shards", 
                $"A shard was connected! {arg.Guilds.Count} guilds just made contact. "), ConsoleColor.DarkGreen);

            Task.Run(() => StartTimeoutKS(10000 * serviceProvider.GetRequiredService<Configuration>().ShardCount, "Discord connection Attempt"));
            
            return Task.Delay(0);
        }

        private Task Client_ShardReady(DiscordSocketClient arg)
        {
            Console.Title = "RMSoftware.ModularBOT -> " + arg.CurrentUser + " | Connected to " + Client.Guilds.Count + " guilds.";
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Shards", 
                $"Shard ready! {arg.Guilds.Count} guilds are fully loaded. "),ConsoleColor.Green);
            if (arg.GetChannel(serviceProvider.GetRequiredService<Configuration>().LogChannel) is SocketTextChannel ch && !Initialized)
            {
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "TaskMgr", $"Executing OnStart.CORE"));

                init_start = true;//Signal SpinWait to run task.
               
            }
            return Task.Delay(0);
        }

        private Task Client_Log(LogMessage arg)
        {
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(arg);
            return Task.Delay(0);
        }

        private async Task Client_JoinedGuild(SocketGuild arg)
        {
            
            Console.Title = "RMSoftware.ModularBOT -> " + arg.CurrentUser + " | Connected to " + Client.Guilds.Count + " guilds.";
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Guilds", $"{Client.CurrentUser.Username} Joined a new guild!" +
                $"Creating {arg.Name}'s {arg.Id}.guild file!"));
            GuildObject g = new GuildObject
            {
                ID = arg.Id,
                CommandPrefix = serviceProvider.GetRequiredService<Configuration>().CommandPrefix,
                LockPFChanges = false,
                BlacklistMode = AutoBlacklistModes.Standard,
                GuildCommands = new List<GuildCommand>(),
            };
            CustomCMDMgr.AddGuildObject(g);
            if (serviceProvider.GetRequiredService<Configuration>().RegisterManagementOnJoin.Value)
            {
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "Guilds", $"Mass-deployment is enabled. Downloading users... This may take a while!"));
                await Task.Delay(1);
#pragma warning disable 4014
                Task.Run(() => SyncGuild(arg));//don't really care about result in this case. just want a new thread.
#pragma warning restore
            }


        }
        #endregion
    }
}
