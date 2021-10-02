using Discord;
using Discord.WebSocket;
using ModularBOT.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModularBOT.Component.CSFunctions
{
    public class CSFEmbedImage : CSFunction
    {
        public CSFEmbedImage()
        {
            Name = "EMBED_IMAGE";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            string output = line.Remove(0, Name.Length).Trim();
            if (string.IsNullOrWhiteSpace(engine.ProcessVariableString(gobj, output, cmd, client, message)))
            {
                return ScriptError("Syntax is not correct.",
                     "<string imageURL>", cmd, errorEmbed, LineInScript, line);
            }

            CSEmbed.WithImageUrl(engine.ProcessVariableString(gobj, output, cmd, client, message));
            return await Task.FromResult(true) ;
        }
    }
}
