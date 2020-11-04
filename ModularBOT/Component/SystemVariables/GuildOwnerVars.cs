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
    public class Guild_Owner : SystemVariable
    {
        public Guild_Owner()
        {
            Name = "guild_owner";
        }
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            string GuildOwner = "";


            if (gobj.ID != 0)
            {
                IGuild g = client.GetGuildAsync(gobj.ID).GetAwaiter().GetResult();
                IGuildUser gu = g.GetOwnerAsync(CacheMode.AllowDownload).GetAwaiter().GetResult();
                GuildOwner = gu.Username + "#" + gu.Discriminator;
            }
            return GuildOwner;
        }
    }

    public class Go_Avatar : SystemVariable
    {
        public Go_Avatar()
        {
            Name = "go_avatar";
        }
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            string GuildOwnerav = "";


            if (gobj.ID != 0)
            {
                IGuild g = client.GetGuildAsync(gobj.ID).GetAwaiter().GetResult();
                IGuildUser gu = g.GetOwnerAsync(CacheMode.AllowDownload).GetAwaiter().GetResult();
                GuildOwnerav = gu.GetAvatarUrl(ImageFormat.Auto, 512);
            }
            return GuildOwnerav;
        }
    }

    public class Go_Nick : SystemVariable
    {
        public Go_Nick()
        {
            Name = "go_nick";
        }

        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            string nick = "";

            ulong? v = gobj?.ID;
            if (v.HasValue)
            {
                if (client.GetGuildAsync(v.Value,CacheMode.AllowDownload).Result.GetOwnerAsync(CacheMode.AllowDownload).Result is SocketGuildUser sgu)
                {
                    nick = sgu.Nickname ?? sgu.Username;
                }
            }

            return nick;

        }
    }
}
