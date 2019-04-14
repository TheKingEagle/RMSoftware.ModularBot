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
using Microsoft.Extensions.DependencyInjection;

namespace ModularBOT.Component
{
    public class DiscordNET
    {

        public CommandService cmdsvr = new Discord.Commands.CommandService(); //Application Command Service.

        public IServiceCollection services; //Application Service collection
        public IServiceProvider serviceProvider; //Application Service provider.
        public DateTime StartTime; //Uptime origin

        //private int timeout = 0; //Operation timeout value
        //private bool timeoutStart = false; //Did the Operation timeout started?
        private List<SocketMessage> messageQueue = new List<SocketMessage>();
        public CustomCommandManager ccmgr;
        public PermissionManager pmgr;

        public bool DisableMessages { get; set; } = false;
        public DiscordShardedClient Client { get; private set; }
        
        public void Start(ref ConsoleIO consoleIO, ref Configuration AppConfig, ref bool ShutdownRequest, ref bool RestartRequested)
        {
            try
            {
                
                string token = AppConfig.AuthToken;

                services = new ServiceCollection();
                //+-+-+-+-BEGIN LOAD MODULE SERVICES-+-+-+-+
                //TODO: LOAD MODULE SERVICES
                //+-+-+-+-CEASE LOAD MODULE SERVICES-+-+-+-+
                services.AddSingleton(AppConfig);
                services.AddSingleton(consoleIO);
                services.AddSingleton(cmdsvr);
                services.AddSingleton(this);


                Client = new DiscordShardedClient(new DiscordSocketConfig
                {
                    MessageCacheSize = 20,
                    AlwaysDownloadUsers = true,
                    LogLevel = LogSeverity.Verbose,

                    // TODO: Figure out a way to automatically set this later.
                    TotalShards = AppConfig.ShardCount
                });

                services.AddSingleton(Client);
                
                serviceProvider = services.BuildServiceProvider();
                pmgr = new PermissionManager(serviceProvider);
                services.AddSingleton(pmgr);
                serviceProvider = services.BuildServiceProvider();
                ccmgr = new CustomCommandManager(serviceProvider);
                services.AddSingleton(ccmgr);
                serviceProvider = services.BuildServiceProvider();
                Client.Log += Client_Log;
                

                Client.ShardReady += Client_ShardReady;
                Client.ShardConnected += Client_ShardConnected;
                Client.MessageReceived += Client_MessageReceived;
                Client.ShardDisconnected += Client_ShardDisconnected;
                Client.GuildAvailable += Client_GuildAvailable;
                Client.GuildUnavailable += Client_GuildUnavailable;


                //await LoadModules();//ADD CORE AND EXTERNAL MODULES

                //TODO: Load External modules
                cmdsvr.AddModulesAsync(Assembly.GetEntryAssembly(),serviceProvider);//ADD CORE.
                Task.Run(async () => await Client.LoginAsync(TokenType.Bot, token));
                Task.Run(async () => await Client.StartAsync());
                
            }
            catch (Discord.Net.HttpException httex)
            {
                if (httex.HttpCode == System.Net.HttpStatusCode.Unauthorized)
                {

                     RestartRequested = consoleIO.ShowKillScreen("Unauthorized", "The server responded with error 401. Make sure your authorization token is correct.", false, ref ShutdownRequest, 5, httex).GetAwaiter().GetResult();
                }
                if (httex.DiscordCode == 4007)
                {
                    RestartRequested = consoleIO.ShowKillScreen("Invalid Client ID", "The server responded with error 4007.", true,ref ShutdownRequest, 5, httex).GetAwaiter().GetResult();
                }
                if (httex.DiscordCode == 5001)
                {
                    RestartRequested = consoleIO.ShowKillScreen("guild timed out", "The server responded with error 5001.", true, ref ShutdownRequest, 5, httex).GetAwaiter().GetResult();
                }

                else
                {
                    RestartRequested = consoleIO.ShowKillScreen("HTTP_EXCEPTION", "The server responded with an error. SEE Crash.LOG for more info.", true, ref ShutdownRequest, 5, httex).GetAwaiter().GetResult();
                }
            }

            catch (Exception ex)
            {
                RestartRequested = consoleIO.ShowKillScreen("Unexpected Error", ex.Message, true, ref ShutdownRequest, 5, ex).GetAwaiter().GetResult();
            }
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
        private Task Client_GuildUnavailable(SocketGuild arg)
        {
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "Guilds", $"A guild just vanished. [{arg.Name}] "));
            return Task.Delay(0);
        }

        private Task Client_GuildAvailable(SocketGuild guild)
        {
            Console.Title = "RMSoftware.ModularBOT -> " + guild.CurrentUser + " | Connected to " + Client.Guilds.Count + " guilds.";
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Guilds", $"A guild just appeared. [{guild.Name}] "),ConsoleColor.Green);
            if(guild.GetTextChannel(serviceProvider.GetRequiredService<Configuration>().LogChannel)!= null)
            {
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Verbose, "INIT", $"Requested initialization channel found. {guild.Name} has it!"));
            }
            return Task.Delay(0);
        }

        private Task Client_ShardDisconnected(Exception arg1, DiscordSocketClient arg2)
        {
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Error, "Shards", $"A shard was disconnected! {arg2.Guilds.Count} guild(s) lost contact. "));
            return Task.Delay(0);
        }

        private async Task Client_MessageReceived(SocketMessage arg)
        {
            if (DisableMessages) return;
            if (!(arg is SocketUserMessage message)) return;
            ulong gid = 0;//global by default
            string prefix = serviceProvider.GetRequiredService<Configuration>().CommandPrefix;//use global (This will set it);
            if (!string.IsNullOrWhiteSpace(ccmgr.GuildObjects.FirstOrDefault(x => x.ID == gid)?.CommandPrefix))
            {
                prefix = ccmgr.GuildObjects.FirstOrDefault(x => x.ID == gid)?.CommandPrefix;
            }
            if ((arg.Channel as SocketGuildChannel) != null)
            {
                SocketGuildChannel sc = arg.Channel as SocketGuildChannel;
                gid = sc.Guild.Id;
            }
            if (message.Content.StartsWith(prefix))
            {
                if (pmgr.GetAccessLevel(arg.Author) == AccessLevels.Blacklisted)
                {
                    if (pmgr.GetWarnOnBlacklist(arg.Author))
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
                if (pmgr.GetAccessLevel(arg.Author) == AccessLevels.Blacklisted)
                {
                    if (pmgr.GetWarnOnBlacklist(arg.Author))
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
            
            string result = "";
           
            await Task.Run(() =>  result = ccmgr.ProcessMessage(arg));
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
            Console.Title = "RMSoftware.ModularBOT -> " + arg.CurrentUser + " | Connected to " + Client.Guilds.Count + " guilds.";
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Shards", $"A shard was connected! {arg.Guilds.Count} guilds just made contact. "), ConsoleColor.DarkGreen);
            return Task.Delay(0);
        }

        private Task Client_ShardReady(DiscordSocketClient arg)
        {
            Console.Title = "RMSoftware.ModularBOT -> " + arg.CurrentUser + " | Connected to " + Client.Guilds.Count + " guilds.";
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Info, "Shards", $"Shard ready! {arg.Guilds.Count} guilds are fully loaded. "),ConsoleColor.Green);
            if (arg.GetChannel(serviceProvider.GetRequiredService<Configuration>().LogChannel) is SocketTextChannel ch)
            {
                StartTime = DateTime.Now;
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "Uptime", $"Initialization Shard READY! Uptime set to {StartTime}"));
                serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "TaskMgr", $"Executing OnStart.CORE"));
                //TODO: Run OnStart.CORE.
                
               
            }
            return Task.Delay(0);
        }

        private Task Client_Log(LogMessage arg)
        {
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(arg);
            return Task.Delay(0);
        }
    }
}
