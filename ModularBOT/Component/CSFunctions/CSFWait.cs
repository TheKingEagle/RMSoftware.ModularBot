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
    public class CSFWait : CSFunction
    {
        public CSFWait()
        {
            Name = "WAIT";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            if (!int.TryParse(engine.ProcessVariableString(gobj, line.Remove(0, 5), cmd, client, message), out int v))
            {
                errorEmbed.WithDescription($"Function error: Expected a valid number greater than zero & below the maximum value supported by the system. You gave: `{line.Remove(0, 5)}`");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            if (v < 1)
            {
                //errorMessage = $"SCRIPT ERROR:```\r\nA number was expected here. You gave: {line.Remove(0, 5)}\r\n\r\n\tCoreScript engine\r\n\tLine:{LineInScript}\r\n\tCommand: {cmd}```";
                errorEmbed.WithDescription($"Function error: Expected a valid number greater than zero & below the maximum value supported by the system. You gave: `{line.Remove(0, 5)}`");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            await Task.Delay(v);
            return await Task.FromResult(true);
        }
    }
}
