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
    public class Bot_Owner : SystemVariable
    {
        public Bot_Owner()
        {
            Name = "bot_owner";
        }
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            return client.GetApplicationInfoAsync().GetAwaiter().GetResult().Owner.Mention;
        }
    }

    public class Bot_Owner_NoMention : SystemVariable
    {
        public Bot_Owner_NoMention()
        {
            Name = "bot_owner_nomention";
        }
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            var o = client.GetApplicationInfoAsync().GetAwaiter().GetResult().Owner;
            return $"{o.Username}#{o.Discriminator}";
        }
    }
    
    public class Bot_Owner_Avatar : SystemVariable
    {
        public Bot_Owner_Avatar()
        {
            Name = "bot_owner_avatar";
        }
        protected override string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            var o = client.GetApplicationInfoAsync().GetAwaiter().GetResult().Owner;

            return o.GetAvatarUrl(ImageFormat.Auto, 512);
        }
    }
}
