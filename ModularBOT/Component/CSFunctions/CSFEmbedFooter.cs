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
    public class CSFEmbedFooter : CSFunction
    {
        public CSFEmbedFooter()
        {
            Name = "EMBED_FOOTER";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            string output = line.Remove(0, 12);
            string ProcessedValue = engine.ProcessVariableString(gobj, output, cmd, client, message);
            if (string.IsNullOrWhiteSpace(ProcessedValue))
            {
                errorEmbed.WithDescription($"String cannot be empty. ```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return await Task.FromResult(false);
            }

            CSEmbed.WithFooter(ProcessedValue);
            return await Task.FromResult(true);
        }
    }
}
