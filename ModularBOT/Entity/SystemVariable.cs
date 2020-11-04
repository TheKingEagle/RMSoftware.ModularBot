using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModularBOT.Entity
{
    public class SystemVariable
    {
        public string Name { get; set; }

        public string GetReplacedString(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            return input.Replace($"%{Name}%", Process(gobj, input, cmd, client, message, cmdsvr));
        }
        protected virtual string Process(GuildObject gobj, string input, GuildCommand cmd, IDiscordClient client, IMessage message, CommandService cmdsvr)
        {
            //override!!
            return "";
        }
    }
}
