using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ModularBOT.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModularBOT.Component.SystemVariables
{
    public class Command : SystemVariable
    {
        public Command() => Name = "command";
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr) => cmd.Name;
    }

    public class Command_Count : SystemVariable
    {
        public Command_Count() => Name = "command_count";
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            int c = gobj.GuildCommands.Count + cmdsvr.Commands.Count();
            return c.ToString();
        }
    }

    public class Latency : SystemVariable
    {
        public Latency() => Name = "latency";
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
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
            return (l ?? cl.Latency).ToString() + " ms";
        }
    }

    public class Prefix : SystemVariable
    {
        public Prefix() => Name = "prefix";
    }
}
