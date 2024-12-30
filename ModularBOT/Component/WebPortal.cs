using ModularBOT.Entity;
using ModularBOT.Properties;
using RMSoftware.Http;
using SocketIOSharp.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using SocketIOSharp.Common;
using SocketIOSharp.Server.Client;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using EngineIOSharp.Common;
using System.Threading;

namespace ModularBOT.Component
{
    internal class WebPortal
    {
        private QuickServer quickServer;
        private SocketIOServer eventServer;
        public static bool TermFlag = false;
        private ushort port;
        private ushort EPort;
        private string host;
        private ConsoleIO cio;
        private DiscordNET discord;
        internal WebPortal(ushort _port, string _host, ref DiscordNET dnet)
        {
            discord = dnet;
            bool r = false;
            cio = dnet._serviceProvider.GetService<ConsoleIO>();
            port = _port;
            EPort = (ushort)(_port + 1);
            host = _host;
            quickServer = new QuickServer(host, port);
            var sop = new SocketIOServerOption(EPort,"/socket.io",false,4000,25000,10000,true,true,true,true);
            
            eventServer = new SocketIOServer(sop);
            EngineIOLogger.DoWrite = false;
            eventServer.OnConnection((socket) =>
            {
                cio.WriteEntry(new LogMessage(LogSeverity.Info, "WEBPORTAL", "EventServer connection accepted"));
                
                socket.On(SocketIOEvent.DISCONNECT, () =>
                {
                    cio.WriteEntry(new LogMessage(LogSeverity.Info, "WEBPORTAL", "EventServer connection removed"));
                });
                socket.Emit("stats", UpdateStatus());
            });
            quickServer.DefineRoute("/", HomeRoute);
            quickServer.DefineStaticFileRoute("/asset", "wrd_asset");
            cio.WriteEntry(new LogMessage(LogSeverity.Info, "WEBPORTAL", $"EventServer listening on port: {EPort}"));

            eventServer.Start();
            Task.Run(() =>
            {
                while (!TermFlag)
                {
                    eventServer.Emit("stats", UpdateStatus());
                    Thread.Sleep(1000);
                }
                    
            });
            cio.WriteEntry(new LogMessage(LogSeverity.Info, "WEBPORTAL", $"WebPortal is running hosted on http://{_host}:{_port}"));
            Task.Run(()=>quickServer.Start());

            

        }

        public StatusPacket UpdateStatus()
        {
            if(discord.Client.CurrentUser != null)
            {
                StatusPacket sp = new StatusPacket()
                {
                    guildAvailable = discord.Client.Guilds.Where(x => x.IsConnected).Count(),
                    guildCount = discord.Client.Guilds.Count,
                    runtime = DateTime.Now - discord.ClientStartTime,
                    avatar = discord.Client.CurrentUser.GetAvatarUrl(),
                    username = discord.Client.CurrentUser.Username,
                    Status = "Connected"
                };
                return sp;
            }
            else
            {
                StatusPacket sp = new StatusPacket()
                {
                    guildAvailable = 0,
                    guildCount = discord.Client.Guilds.Count,
                    runtime = DateTime.Now - discord.ClientStartTime,
                    avatar = "asset/img/ico.png",
                    username = "ModularBOT",
                    Status = "Disconnected"
                };
                return sp;
            }
            
        }

        private void HomeRoute(HttpListenerContext context) {
            WebPortalPage home = new WebPortalPage()
            {

                ScriptSources = new List<string>() {
                    "https://cdnjs.cloudflare.com/ajax/libs/socket.io/2.0.1/socket.io.js",
                    "asset/js/main.js",
                },
                RawJS = $"<script>const socket = io('http://localhost:{EPort}',{{transports: ['polling', 'xhr-polling']}});</script>",
                LogoUrl = "asset/img/ico.png",
                Title = "ModularBOT",
                Content = Resources.Dashboard
            };
            quickServer.SendResponse(context.Response, home.ToHTML());
        }
    }

    class StatusPacket
    {
        public string Status { get; set; }
        public TimeSpan runtime { get; set; }
        public int guildCount { get; set; }
        public int guildAvailable { get; set; }
        public string avatar { get; set; }
        public string username { get; set; }
    }
}
