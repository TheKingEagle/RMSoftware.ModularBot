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
    public class Self : SystemVariable
    {
        public Self()
        {
            Name = "self";
        }
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            return client.CurrentUser.Mention;
        }
    }

    public class Self_NoMention : SystemVariable
    {
        public Self_NoMention()
        {
            Name = "self_nomention";
        }
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            return $"{client.CurrentUser.Username}#{client.CurrentUser.Discriminator}";
        }
    }

    public class Self_Nick : SystemVariable
    {
        public Self_Nick()
        {
            Name = "self_nick";
        }
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            string nick = client.CurrentUser.Username;
            ulong? v = gobj?.ID;
            if (v.HasValue)
            {
                if (client.GetGuildAsync(v.Value).Result.GetCurrentUserAsync().Result is SocketGuildUser sgu)
                {
                    nick = sgu.Nickname ?? sgu.Username;
                }
            }
            return nick;
        }
    }

    public class Self_Avatar : SystemVariable
    {
        public Self_Avatar()
        {
            Name = "self_avatar";
        }
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            return client.CurrentUser.GetAvatarUrl(ImageFormat.Auto, 512);
        }
    }
}
