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
    public class CSFStrmStatus : CSFunction
    {
        public CSFStrmStatus()
        {
            Name = "STRMSTATUS";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            engine.OutputCount++;
            if (engine.OutputCount > 2)
            {
                errorEmbed.WithDescription($"`{Name}` Function Error: Preemptive rate limit reached. Please slow down your script with `WAIT`\r\n```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            if (cmd.CommandAccessLevel < AccessLevels.Administrator)
            {
                errorEmbed.WithDescription($"`{Name}` Function error: This requires `AccessLevels.Administrator`");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            string linevar = engine.ProcessVariableString(gobj, line, cmd, client, message);
            string[] data = linevar.Remove(0, Name.Length).Trim().Split(' ');
            if (data.Length < 2)
            {
                errorEmbed.WithDescription($"`{Name}` Function error: Expected format ```{Name} <ChannelName> <status text>.```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            string statusText = line.Remove(0, Name.Length + 1 + data[0].Length + 1).Trim();

            await ((DiscordShardedClient)client).SetGameAsync(statusText, $"https://twitch.tv/{data[0]}", ActivityType.Streaming);
            return true;
        }
    }
}
