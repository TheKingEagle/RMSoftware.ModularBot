using System;
using System.Collections.Generic;
using System.Linq;
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

        private int timeout = 0; //Operation timeout value
        private bool timeoutStart = false; //Did the Operation timeout started?
        private List<SocketMessage> messageQueue = new List<SocketMessage>();
        public DiscordShardedClient Client { get; private set; }
        
        public void Start(ref ConsoleIO consoleIO, ref Configuration AppConfig, ref bool ShutdownRequest)
        {
            try
            {
                string token = AppConfig.AuthToken;
                services = new ServiceCollection();
                services.AddSingleton(AppConfig);
                services.AddSingleton(consoleIO);
                //+-+-+-+-BEGIN LOAD MODULES-+-+-+-+
                //TODO: LOAD MODULES
                //+-+-+-+-CEASE LOAD MODULES-+-+-+-+

                
                Client = new DiscordShardedClient(new DiscordSocketConfig
                {
                    MessageCacheSize = 20,
                    AlwaysDownloadUsers = true,
                    LogLevel = LogSeverity.Debug,

                    // Please change increase this as your server count grows beyond 2000 guilds. ie. < 2000 = 1, 2000 = 2, 4000 = 2 ...
                    TotalShards = 1
                });

                services.AddSingleton(Client);
                
                serviceProvider = services.BuildServiceProvider();
                Client.Log += Client_Log;
                

                Client.ShardReady += Client_ShardReady;
                Client.ShardConnected += Client_ShardConnected;
                Client.MessageReceived += Client_MessageReceived;
                Client.ShardDisconnected += Client_ShardDisconnected;
                Client.GuildAvailable += Client_GuildAvailable;
                Client.GuildUnavailable += Client_GuildUnavailable;
                

                //await LoadModules();//ADD CORE AND EXTERNAL MODULES
                Task.Run(async () => await Client.LoginAsync(TokenType.Bot, token));
                Task.Run(async () => await Client.StartAsync());
                
            }
            catch (Discord.Net.HttpException httex)
            {
                if (httex.HttpCode == System.Net.HttpStatusCode.Unauthorized)
                {

                     consoleIO.ShowKillScreen("Unauthorized", "The server responded with error 401. Make sure your authorization token is correct.", false, ref ShutdownRequest, 5, httex);
                }
                if (httex.DiscordCode == 4007)
                {
                    consoleIO.ShowKillScreen("Invalid Client ID", "The server responded with error 4007.", true,ref ShutdownRequest, 5, httex);
                }
                if (httex.DiscordCode == 5001)
                {
                    consoleIO.ShowKillScreen("guild timed out", "The server responded with error 5001.", true, ref ShutdownRequest, 5, httex);
                }

                else
                {
                    consoleIO.ShowKillScreen("HTTP_EXCEPTION", "The server responded with an error. SEE Crash.LOG for more info.", true, ref ShutdownRequest, 5, httex);
                }
            }

            catch (Exception ex)
            {
                consoleIO.ShowKillScreen("Unexpected Error", ex.Message, true, ref ShutdownRequest, 5, ex);
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
            //Console.Title = "RMSoftware.ModularBOT -> " + arg.CurrentUser + " | Connected to " + Client.Guilds.Count + " guilds.";
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "Guilds", $"A guild just vanished. [{arg.Name}] "));
            return Task.Delay(0);
        }

        private Task Client_GuildAvailable(SocketGuild arg)
        {
            Console.Title = "RMSoftware.ModularBOT -> " + arg.CurrentUser + " | Connected to " + Client.Guilds.Count + " guilds.";
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Warning, "Guilds", $"A guild just appeared. [{arg.Name}] "));
            return Task.Delay(0);
        }

        private Task Client_ShardDisconnected(Exception arg1, DiscordSocketClient arg2)
        {
            //Console.Title = "RMSoftware.ModularBOT -> " + arg2.CurrentUser + " | Connected to " + Client.Guilds.Count + " guilds.";
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(new LogMessage(LogSeverity.Error, "Shards", $"A shard was disconnected! {arg2.Guilds.Count} guild(s) lost contact. "));
            return Task.Delay(0);
        }

        private async Task Client_MessageReceived(SocketMessage arg)
        {

            //TODO: Add prefix system to process messages accordingly.
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
            return Task.Delay(0);
        }

        private Task Client_Log(LogMessage arg)
        {
            serviceProvider.GetRequiredService<ConsoleIO>().WriteEntry(arg);
            return Task.Delay(0);
        }
    }
}
