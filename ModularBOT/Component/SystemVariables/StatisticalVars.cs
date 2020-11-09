using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ModularBOT.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr) => gobj.CommandPrefix;
    }
    
    public class PrefixPF : SystemVariable
    {
        public PrefixPF() => Name = "pf";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr) => gobj.CommandPrefix;
    }
    
    public class Version : SystemVariable
    {
        public Version() => Name = "version";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
            => Assembly.GetExecutingAssembly()
                       .GetName().Version
                       .ToString(4);
    }

    public class OS_Name : SystemVariable
    {
        public OS_Name() => Name = "os_name";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
            => SystemInfo.FriendlyName();
    }

    public class OS_Bit : SystemVariable
    {
        public OS_Bit() => Name = "os_bit";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
            => Environment.Is64BitOperatingSystem ? "x64" : "x86";
    }

    public class OS_Ver : SystemVariable
    {
        public OS_Ver() => Name = "os_ver";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            OperatingSystem os = Environment.OSVersion;
            return os.Version.ToString();
        }
    }

    public class Bot_Mem : SystemVariable
    {
        public Bot_Mem() => Name = "bot_mem";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
            => SystemInfo.SizeSuffix(System.Diagnostics.Process.GetCurrentProcess().WorkingSet64);
    }

    public class Guild : SystemVariable
    {
        public Guild() => Name = "guild";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            string Context = message.Author.Mention;
            if (message.Channel is IGuildChannel IGC)
            {
                Context = IGC.Guild.Name;
            }
            return Context;
        }
    }
    
    public class Guild_ID : SystemVariable
    {
        public Guild_ID() => Name = "guild_id";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            string Context = message.Author.Id.ToString();
            if (message.Channel is IGuildChannel IGC)
            {
                Context = IGC.Guild.Id.ToString();
            }
            return Context;
        }
    }

    public class Guild_Count : SystemVariable
    {
        public Guild_Count() => Name = "guild_count";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            DiscordShardedClient cl = client as DiscordShardedClient;
            return cl.Guilds.Count.ToString();
        }
    }
    
    public class Guild_UserCount : SystemVariable
    {
        public Guild_UserCount() => Name = "guild_usercount";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            //TODO: Due to recent events with DISCORD, this may return 1 or 2 until it is called again.
            if (message.Channel is IGuildChannel IGC)
            {
                Task.Run(() => IGC.Guild.DownloadUsersAsync()); //due to threadlock
                var ul = client.GetGuildAsync(IGC.Guild.Id, CacheMode.AllowDownload)
                    .GetAwaiter().GetResult().GetUsersAsync(CacheMode.AllowDownload).GetAwaiter().GetResult();
                return ul.Count.ToString();
            }
            else
            {
                return "2";//assume this is a DM. in which case it will always be two...
            }
        }
    }

    public class Guild_Icon : SystemVariable
    {
        public Guild_Icon() => Name = "guild_icon";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            string Context = message.Author.GetAvatarUrl(ImageFormat.Auto, 512);
            if (message.Channel is IGuildChannel IGC)
            {
                Context = client.GetGuildAsync(IGC.GuildId).GetAwaiter().GetResult().IconUrl;
            }
            return Context;
        }
    }

    public class Channel_NoMention : SystemVariable
    {
        public Channel_NoMention() => Name = "channel_nomention";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            string Context = message.Author.Username;
            if (message.Channel is IGuildChannel IGC)
            {
                Context = IGC.Name;
            }
            return Context;
        }
    }

    public class Channel : SystemVariable
    {
        public Channel() => Name = "channel";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            string Context = message.Author.Mention;
            if (message.Channel is IGuildChannel IGC)
            {
                Context = $"<#{IGC.Id}>";//channel mention format
            }
            return Context;
        }
    }

    public class Channel_ID : SystemVariable
    {
        public Channel_ID() => Name = "channel_id";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            string Context = $"{message.Author.Id}";
            if (message.Channel is IGuildChannel IGC)
            {
                Context = $"{IGC.Id}";
            }
            return Context;
        }
    }

    public class MsgCount : SystemVariable
    {
        public MsgCount() => Name = "msgcount";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            string count = "0";
            if (CoreScript.MessageCounter.ContainsKey(message.Channel.Id))
            {
                count = CoreScript.MessageCounter[message.Channel.Id].ToString();
            }
            return count;
        }
    }

    public class Counter : SystemVariable
    {
        public Counter() => Name = "counter";

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            if (cmd != null)
            {
                if (cmd.Counter.HasValue)
                {
                    cmd.Counter++;
                    gobj.SaveJson();
                    return cmd.Counter.ToString();
                }
                else
                {
                    cmd.Counter = 1;
                    gobj.SaveJson();
                    return cmd.Counter.ToString();
                }
            }
            else return "-1";
            
        }
    }
}
