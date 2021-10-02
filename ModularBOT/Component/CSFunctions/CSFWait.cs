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
                return ScriptError("Expected number greater than 0 and below maximum supported by the system.", cmd, errorEmbed, LineInScript, line);
            }
            if (v < 1)
            {
                return ScriptError("Expected number greater than 0 and below maximum supported by the system.", cmd, errorEmbed, LineInScript, line);
            }
            await Task.Delay(v);
            return await Task.FromResult(true);
        }
    }
}
