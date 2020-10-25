using Discord;
using ModularBOT.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModularBOT.Entity
{
    public class CSFunction
    {
        public string Name { get; protected set; }

        public virtual async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool isFile=false)
        {
            return await Task.FromResult(true);//derive... On error, returns false with ErrorEmbed set
        }


    }
}
