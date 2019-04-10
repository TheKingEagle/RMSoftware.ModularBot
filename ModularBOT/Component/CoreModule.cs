using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace ModularBOT.Component
{
    public class CoreModule:ModuleBase
    {
        DiscordShardedClient Client { get; set; }

        CommandService Cmdsvr { get; set; }

        ConsoleIO consoleIO { get; set; }

        DiscordNET net { get; set; }
        
        public CoreModule(DiscordShardedClient client, CommandService cmdservice, ConsoleIO consoleIO, DiscordNET dnet)
        {
            Client = client;
            Cmdsvr = cmdservice;
            this.consoleIO = consoleIO;
            net = dnet;
            this.consoleIO.WriteEntry(new LogMessage(LogSeverity.Critical, "CoreMOD", "Constructor called! This debug message proved it."));

        }

        [Command("about"), Summary("Display information about the bot")]
        public async Task ShowAbout()
        {
            EmbedBuilder builder = new EmbedBuilder
            {
                Title = "About"
            };
            builder.WithAuthor(Context.Client.CurrentUser);
            builder.Color = Color.Blue;
            builder.Description = "A Multi-purpose, multi-module bot designed for discord. Tailor it for your specific server, create your own modules and plug-ins. Includes a core module for custom text-based commands & EXEC functionality";
            builder.AddField("Copyright", "Copyright © 2017-2019 RMSoftware Development");
            builder.AddField("Version", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
            builder.WithFooter("ModularBOT | created by TheKingEagle");
            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("addcmd"),Summary("Add a command to your bot. If you run this via DM, it will create a global command.")]
        public async Task AddCmd(string cmdname, bool restricted, [Remainder]string action)
        {
            if (net.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithTitle("Access Denied");
                b.WithAuthor(Context.Client.CurrentUser);
                b.WithDescription("You do not have permission to use this command. Requires `AccessLevel 1` or higher.");
                b.WithColor(Color.Red);
                b.WithFooter("ModularBOT • Core");
                await Context.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
            ulong gid = 0;
            if(Context.Guild != null)
            {
                gid = Context.Guild.Id;
            }

            await net.ccmgr.AddCmd(Context.Message, cmdname, action, restricted,gid);
        }

        [Command("addgcmd"), Summary("Add a global command to your bot")]
        public async Task AddgCmd(string cmdname, bool restricted, [Remainder]string action)
        {
            if (net.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithTitle("Access Denied");
                b.WithAuthor(Context.Client.CurrentUser);
                b.WithDescription("You do not have permission to use this command. Requires `AccessLevel 1` or higher.");
                b.WithColor(Color.Red);
                b.WithFooter("ModularBOT • Core");
                await Context.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
            await net.ccmgr.AddCmd(Context.Message, cmdname, action, restricted);
        }

        [Command("delcmd"), Summary("Add a command to your bot. If you run this via DM, it will create a global command.")]
        public async Task DelCmd(string cmdname)
        {
            if (net.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithTitle("Access Denied");
                b.WithAuthor(Context.Client.CurrentUser);
                b.WithDescription("You do not have permission to use this command. Requires `AccessLevel 1` or higher.");
                b.WithColor(Color.Red);
                b.WithFooter("ModularBOT • Core");
                await Context.Channel.SendMessageAsync("", false, b.Build());
                return;
            }
            ulong gid = 0;
            if (Context.Guild != null)
            {
                gid = Context.Guild.Id;
            }

            await net.ccmgr.DelCmd(Context.Message, cmdname, gid);
        }

        [Command("delgcmd"), Summary("Add a command to your bot. If you run this via DM, it will create a global command.")]
        public async Task DelgCmd(string cmdname)
        {
            if (net.pmgr.GetAccessLevel(Context.User) < AccessLevels.CommandManager)
            {
                EmbedBuilder b = new EmbedBuilder();
                b.WithTitle("Access Denied");
                b.WithAuthor(Context.Client.CurrentUser);
                b.WithDescription("You do not have permission to use this command. Requires `AccessLevel 1` or higher.");
                b.WithColor(Color.Red);
                b.WithFooter("ModularBOT • Core");
                await Context.Channel.SendMessageAsync("", false, b.Build());
                return;
            }


            await net.ccmgr.DelCmd(Context.Message, cmdname);
        }



    }
}
