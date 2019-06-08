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

namespace ModularBOT.Component
{
    public class DiscordNET
    {
        #region Fields
        public CommandService cmdsvr = new Discord.Commands.CommandService();  //Discord Command Service
        public IServiceCollection services;                                    //Discord Service collection
        public IServiceProvider serviceProvider;                               //Discord Service provider
        public bool InputCanceled = false;                                     //ModularBOT Console Read Operation
        private static bool init_start = false;                                //DiscordNET Conditional Initialization
        private bool Initialized = false;                                      //Conditional Completed Initialization
        private bool LogConnected = false;                                     //Conditional Log channel found & connected
        private List<SocketMessage> messageQueue = new List<SocketMessage>();  //DiscordNET Message Queue
        
        #endregion

        #region Properties
        public bool DisableMessages { get; set; } = false;                     //DiscordNET Message Processing Flag
        public DateTime StartTime { get; private set; }                        //DiscordNET Operation Start Date
        public CustomCommandManager CustomCMDMgr { get; private set; }         //ModularBOT Custom Commands Manager
        public PermissionManager PermissionManager { get; private set; }       //ModularBOT Permission Manager
        public ModuleManager ModuleMgr { get; private set; }                   //ModularBOT Module Manager
        public UpdateManager Updater { get; private set; }                     //ModularBOT Update Manager
        public DiscordShardedClient Client { get; private set; }               //Discord Sharded Client

        #endregion

        #region Methods

        private readonly Func<bool> ReadyForInit = delegate ()
        {
            return init_start;
        };

        public void Start(ref ConsoleIO consoleIO, ref Configuration AppConfig, ref bool ShutdownRequest, ref bool RestartRequested,ref bool FromCrash)
        {
            try
            {
                Initialized = false;
                string token = AppConfig.AuthToken;

                services = new ServiceCollection();
                services.AddSingleton(AppConfig);
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

                services.AddSingleton(new Discord.Addons.Interactive.InteractiveService(Client));
                services.AddSingleton(Client);
                serviceProvider = services.BuildServiceProvider();
               
                Client.Log += Client_Log;

                Client.LoggedIn += Client_LoggedIn;
                Client.ShardReady += Client_ShardReady;
                Client.ShardConnected += Client_ShardConnected;
                Client.MessageReceived += Client_MessageReceived;
                Client.ShardDisconnected += Client_ShardDisconnected;
                Client.GuildAvailable += Client_GuildAvailable;
                Client.GuildUnavailable += Client_GuildUnavailable;
                Client.JoinedGuild += Client_JoinedGuild;
                
                
                
                cmdsvr.AddModulesAsync(Assembly.GetEntryAssembly(),serviceProvider);//ADD CORE.
                Task.Run(async () => await Client.LoginAsync(TokenType.Bot, token));
                Task.Run(async () => await Client.StartAsync());
                SpinWait.SpinUntil(ReadyForInit);//Hold thread until needed shard is ready.
                OffloadReady(ref FromCrash, ref ShutdownRequest, ref RestartRequested);
                
            }
            catch (Discord.Net.HttpException httex)
            {
                if (httex.HttpCode == System.Net.HttpStatusCode.Unauthorized)
                {

                     RestartRequested = consoleIO.ShowKillScreen("Unauthorized", "The server responded with error 401. Make sure your authorization token is correct.", false, ref ShutdownRequest, ref RestartRequested,5, httex).GetAwaiter().GetResult();
                }
                if (httex.DiscordCode == 4007)
                {
                    RestartRequested = consoleIO.ShowKillScreen("Invalid Client ID", "The server responded with error 4007.", true,ref ShutdownRequest, ref RestartRequested, 5, httex).GetAwaiter().GetResult();
                }
                if (httex.DiscordCode == 5001)
                {
                    RestartRequested = consoleIO.ShowKillScreen("guild timed out", "The server responded with error 5001.", true, ref ShutdownRequest, ref RestartRequested, 5, httex).GetAwaiter().GetResult();
                }

                else
                {
                    RestartRequested = consoleIO.ShowKillScreen("HTTP_EXCEPTION", "The server responded with an error. SEE Crash.LOG for more info.", true, ref ShutdownRequest, ref RestartRequested, 5, httex).GetAwaiter().GetResult();
                }
            }

            catch (Exception ex)
            {
                RestartRequested = consoleIO.ShowKillScreen("Unexpected Error", ex.Message, true, ref ShutdownRequest, ref RestartRequested, 5, ex).GetAwaiter().GetResult();
            }
        }

        private Task Client_LoggedIn()
        {
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Client", "Client is logged in! We can now start permissions system."));
            PermissionManager = new PermissionManager(serviceProvider);
            services.AddSingleton(PermissionManager);
            serviceProvider = services.BuildServiceProvider();
            ModuleMgr = new ModuleManager(ref cmdsvr, ref services, ref serviceProvider);
            CustomCMDMgr = new CustomCommandManager(serviceProvider);
            services.AddSingleton(CustomCMDMgr);
            serviceProvider = services.BuildServiceProvider();
            Updater = new UpdateManager(serviceProvider);
            return Task.Delay(1);
        }

        public void SyncGuild(SocketGuild arg)
        {
            foreach (SocketRole item in arg.Roles)
            {
                if (item.Permissions.ManageGuild)
                {
                    if (PermissionManager.GetAccessLevel(item) < AccessLevels.CommandManager)
                    {
                        serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Guilds", $"Found a role that can manage guilds: " +
                        $"{item.Name} <@&{item.Id}>. Registering role as CommandManager!"));
                        PermissionManager.RegisterEntity(item, AccessLevels.CommandManager);
                    }
                }
            }
            PermissionManager.SaveJson();
            Thread.Sleep(300);
        }

        public void Stop(ref bool ShutdownRequest)
        {
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
                    Client.SetStatusAsync(UserStatus.DoNotDisturb);
                    ulong id = serviceProvider.GetRequiredService<Configuration>().LogChannel;
                    if (recovered)
                    {
                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithAuthor(Client.CurrentUser);
                        builder.WithTitle("WARNING");
                        builder.WithDescription("The program was auto-restarted due to a crash. Please see `Crash.LOG` and `Errors.LOG` for details.");
                        builder.WithColor(new Color(255, 255, 0));
                        builder.WithFooter("RMSoftware.ModularBOT Core");
                        ((SocketTextChannel)Client.GetChannel(id)).SendMessageAsync("", false, builder.Build());
                        serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "TaskMgr", "The program auto-restarted due to a crash. Please see Crash.LOG."));
                    }

                    #region Startup.CORE
                    IGuildChannel i = (IGuildChannel)Client.GetChannel(id);
                    if (i == null)
                    {
                        InputCanceled = true;
                        ConsoleIO.PostMessage(ConsoleIO.GetConsoleWindow(), ConsoleIO.WM_KEYDOWN, ConsoleIO.VK_RETURN, 0);
                        serviceProvider.GetRequiredService<ConsoleIO>().ShowKillScreen("TaskManager Exception", "You specified an invalid guild channel ID. Please verify your guild channel's ID and try again.", false, ref shutdownRequested, ref RestartRequested, 0, new ArgumentException("Guild channel was invalid.", "botChannel"));
                        
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
                        serviceProvider.GetRequiredService<ConsoleIO>().ShowKillScreen("TaskManager Exception", $"{ex.Message}", false, ref shutdownRequested, ref RestartRequested, 0, ex);
                        Stop(ref shutdownRequested);

                        return;
                    }

                    #endregion

                    #region Update Check
                    if (serviceProvider.GetRequiredService<Configuration>().CheckForUpdates.Value)
                    {
                        serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "TaskMgr", "Checking for updates."));
                        bool pre = serviceProvider.GetRequiredService<Configuration>().UsePreReleaseChannel.Value;
                        bool availableUpdates = Updater.CheckUpdate(pre).GetAwaiter().GetResult();
                        if (availableUpdates)
                        {
                            string verdata = pre ? Updater.UpdateInfo.PREVERS : Updater.UpdateInfo.VERSION;
                            string package = pre ? Updater.UpdateInfo.PREPAKG : Updater.UpdateInfo.PACKAGE;
                            EmbedBuilder builder = new EmbedBuilder();
                            builder.WithAuthor(Client.CurrentUser);
                            builder.WithTitle("UPDATE AVAILABLE");
                            builder.WithUrl(package);
                            builder.WithThumbnailUrl(Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto, 256));
                            builder.WithDescription("A new version of ModularBOT is available for download!");
                            builder.AddField("Version", $"v{verdata}");
                            builder.AddField("Download Link", package);
                            builder.WithColor(new Color(0, 255, 60));
                            builder.WithFooter("RMSoftware.ModularBOT Core");
                            ((SocketTextChannel)Client.GetChannel(id)).SendMessageAsync("", false, builder.Build());
                            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Critical, "UPDATE", $"A new version is available! v{verdata}"),ConsoleColor.Green);
                            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Critical, "UPDATE", $"Download: {package}"),ConsoleColor.Green);
                        }
                        else
                        {
                            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Critical, "UPDATE", $"You are running the most recent version."),ConsoleColor.Black);
                        }
                    }
                    

                    #endregion

                    //Finished task manager.
                    Client.SetStatusAsync(serviceProvider.GetRequiredService<Configuration>().ReadyStatus);
                    Client.SetGameAsync(serviceProvider.GetRequiredService<Configuration>().ReadyText,null,serviceProvider.GetRequiredService<Configuration>().ReadyActivity);
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "TaskMgr", "Task is complete."));

                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "TaskMgr", "Processing Message Queue."));
                    foreach (var item in messageQueue)
                    {

                         Client_MessageReceived(item).GetAwaiter().GetResult();
                         Task.Delay(500);
                    }
                    serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "TaskMgr", "Task is complete."));
                }
            }
            catch (Discord.Net.HttpException httx)
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
                    true, ref Program.ShutdownCalled, ref Program.RestartRequested, 5,ex);
                }
                
                
            }
            else
            {
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "TaskMgr", "Log Connected within time limit."));
            }

        }

        #endregion

        #region Events
        private Task Client_GuildUnavailable(SocketGuild guild)
        {
            SocketTextChannel c = guild.GetTextChannel(serviceProvider.GetRequiredService<Configuration>().LogChannel);
            if (c != null)
            {
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Guilds", $"Requested initialization channel ({c.Name}) became unavailable."));
                
                LogConnected = false;
            }
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "Guilds", $"A guild just vanished. [{guild.Name}] "));
            return Task.Delay(0);
        }

        private async Task Client_GuildAvailable(SocketGuild guild)
        {
            //Console.Title = "RMSoftware.ModularBOT -> " + guild.CurrentUser + " | Connected to " + Client.Guilds.Count + " guilds.";
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Guilds", $"A guild just appeared. [{guild.Name}] "),ConsoleColor.Green);
            SocketTextChannel c = guild.GetTextChannel(serviceProvider.GetRequiredService<Configuration>().LogChannel);
            if ( c != null)
            {
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "Guilds", $"Requested initialization channel ({c.Name}) has been found. {guild.Name} currently has it!"));
                StartTime = DateTime.Now;
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "Uptime", $"System uptime set to {StartTime}"));
                LogConnected = true;
            }
            await Task.Delay(0);
            Task.Run(() => SyncGuild(guild));//don't really care about result in this case. just want a new thread.
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
            if (DisableMessages) return;
            
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
            
            #region Deep-rooted prefix command
            if (message.Content.StartsWith("!prefix") || message.Content.StartsWith(".prefix") || message.Content.StartsWith("/prefix")) //This command will ALWAYS be a thing. it cannot be overridden.
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
                var contextpre = new CommandContext(Client, message);

                var cmdrespre = await cmdsvr.ExecuteAsync(contextpre, 1, serviceProvider);
                if (cmdrespre.Error.HasValue)
                {
                    if (!cmdrespre.Error.Value.HasFlag(CommandError.UnknownCommand) && !cmdrespre.IsSuccess)
                    {
                        EmbedBuilder b = new EmbedBuilder();
                        b.WithColor(Color.Orange);
                        b.WithAuthor(Client.CurrentUser);
                        b.WithTitle("Command Error!");
                        b.WithDescription(cmdrespre.ErrorReason);
                        b.AddField("Error Code", cmdrespre.Error.Value);
                        await contextpre.Channel.SendMessageAsync("", false, b.Build());
                    }
                    if (cmdrespre.Error.Value.HasFlag(CommandError.BadArgCount))
                    {
                        EmbedBuilder b = new EmbedBuilder();
                        b.WithColor(Color.Orange);
                        b.WithAuthor(Client.CurrentUser);
                        b.WithTitle("Command Error.");
                        b.WithDescription(cmdrespre.ErrorReason);
                        b.AddField("Error Code", cmdrespre.Error.Value);
                        await contextpre.Channel.SendMessageAsync("", false, b.Build());
                    }
                }
                return;
            }
            #endregion

            
            if (!message.Content.StartsWith(prefix))
            {
                return;
            }
            if (arg.Author.IsBot && !PermissionManager.IsEntityRegistered(arg.Author)) return;//ignore bots unless bot is registered in the permission system!
            string cgontext = "DM/Group";
            if (message.Channel is SocketGuildChannel)
            {
                cgontext = ((SocketGuildChannel)message.Channel).Guild.Name;
            }
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Commands", $"<#{message.Channel.Name} [{cgontext}]> {message.Author.Username}: {message.Content}"));
            string result = "";
           
            await Task.Run(() =>  result = CustomCMDMgr.ProcessMessage(arg));
            if(result != "SCRIPT" && result != "EXEC" && result != "" && result != "CLI_EXEC" && result != null)
            {
                await arg.Channel.SendMessageAsync(result);
                return;
            }
            var context = new CommandContext(Client, message);
            
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var cmdres = await cmdsvr.ExecuteAsync(context,prefix.Length, serviceProvider);
            //If the result is unsuccessful AND not unknown command, send the error details.
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
            await Task.Delay(1);
        }

        private Task Client_ShardConnected(DiscordSocketClient arg)
        {
            //Console.Title = "RMSoftware.ModularBOT -> " + arg.CurrentUser + " | Connected to " + Client.Guilds.Count + " guilds.";
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Shards", $"A shard was connected! {arg.Guilds.Count} guilds just made contact. "), ConsoleColor.DarkGreen);

            Task.Run(() => StartTimeoutKS(10000 * serviceProvider.GetRequiredService<Configuration>().ShardCount, "Discord connection Attempt"));
            
            return Task.Delay(0);
        }

        private Task Client_ShardReady(DiscordSocketClient arg)
        {
            Console.Title = "RMSoftware.ModularBOT -> " + arg.CurrentUser + " | Connected to " + Client.Guilds.Count + " guilds.";
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Shards", $"Shard ready! {arg.Guilds.Count} guilds are fully loaded. "),ConsoleColor.Green);
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
                GuildCommands = new List<GuildCommand>(),
            };
            CustomCMDMgr.AddGuildObject(g);
            if (serviceProvider.GetRequiredService<Configuration>().RegisterManagementOnJoin.Value)
            {
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "Guilds", $"Mass-deployment is enabled. Downloading users... This may take a while!"));
                await Task.Delay(1);
                Task.Run(() => SyncGuild(arg));//don't really care about result in this case. just want a new thread.
            }


        }
        #endregion
    }
}
