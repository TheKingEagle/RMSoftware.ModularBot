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
    public class CSFEmbedThImage : CSFunction
    {
        public CSFEmbedThImage()
        {
            Name = "EMBED_THIMAGE";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            string output = line.Remove(0, Name.Length).Trim();
            string ProcessedValue = engine.ProcessVariableString(gobj, output, cmd, client, message);
            if (string.IsNullOrWhiteSpace(ProcessedValue))
            {
                return ScriptError("Syntax is not correct.",
                     "<string imageURL>", cmd, errorEmbed, LineInScript, line);
            }

            CSEmbed.WithThumbnailUrl(ProcessedValue);
            return await Task.FromResult(true);
        }
    }
}
