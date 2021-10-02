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
    public class CSFTitle : CSFunction
    {
        public CSFTitle()
        {
            Name = "TITLE";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            engine.OutputCount++;
            string output = line.Remove(0, Name.Length).Trim();
            string processed = engine.ProcessVariableString(gobj, output, cmd, client, message);
            if (engine.OutputCount > 2)
            {
                return ScriptError("Rate limit triggered! Add waits between executions.", cmd, errorEmbed, LineInScript, line);
            }
            if (cmd.CommandAccessLevel < AccessLevels.Administrator)
            {
                EmbedFieldBuilder[] fields = { new EmbedFieldBuilder() { IsInline = false, Name = "Minimum AccessLevel", Value = "`Administrator`" } };
                return ScriptError("Command has insufficient AccessLevel requirement.", cmd, errorEmbed, LineInScript, line, fields);
            }
            await ((DiscordShardedClient)client).SetGameAsync(processed,null,ActivityType.Playing);
            return true;
        }
    }
}
