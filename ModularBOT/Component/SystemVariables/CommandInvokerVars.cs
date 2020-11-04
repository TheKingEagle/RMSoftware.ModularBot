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
    public class Invoker: SystemVariable
    {
        public Invoker()
        {
            Name = "invoker";
        }
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            return message.Author.Mention;
        }
    }

    public class Invoker_NoMention : SystemVariable
    {
        public Invoker_NoMention()
        {
            Name = "invoker_nomention";
        }
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            return $"{message.Author.Username}#{message.Author.Discriminator}";
        }
    }

    public class Invoker_Nick : SystemVariable
    {
        public Invoker_Nick()
        {
            Name = "invoker_nick";
        }
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            string nick = message.Author.Username;
            if (message.Author is SocketGuildUser sgu)
            {
                nick = sgu.Nickname ?? sgu.Username;
            }
            return nick;
        }
    }

    public class Invoker_Avatar : SystemVariable
    {
        public Invoker_Avatar()
        {
            Name = "invoker_avatar";
        }
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            return message.Author.GetAvatarUrl(ImageFormat.Auto,512);
        }
    }

}
