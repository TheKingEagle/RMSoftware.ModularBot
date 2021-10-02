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
    public class CSFOrb : CSFunction
    {
        public CSFOrb()
        {
            Name = "STATUSORB";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            engine.OutputCount++;
            if (engine.OutputCount > 2)
            {
                return ScriptError("Rate limit triggered! Add waits between executions.", cmd, errorEmbed, LineInScript, line);
            }
            if (cmd.CommandAccessLevel < AccessLevels.Administrator)
            {
                EmbedFieldBuilder[] fields = { new EmbedFieldBuilder() { IsInline = false, Name = "Minimum AccessLevel", Value = "`Administrator`" } };
                return ScriptError("Command has insufficient AccessLevel requirement.", cmd, errorEmbed, LineInScript, line, fields);
            }
            string cond = line.Remove(0, Name.Length).Trim().ToUpper();
            switch (cond)
            {
                case ("ONLINE"):
                    await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.Online);
                    break;
                case ("AWAY"):
                    await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.Idle);
                    break;
                case ("AFK"):
                    await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.AFK);
                    break;
                case ("BUSY"):
                    await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.DoNotDisturb);
                    break;
                case ("OFFLINE"):
                    await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.Offline);
                    break;
                case ("INVISIBLE"):
                    await ((DiscordShardedClient)client).SetStatusAsync(UserStatus.Invisible);
                    break;
                default:
                    EmbedFieldBuilder[] fields = { new EmbedFieldBuilder() { IsInline = false, Name = "Supported Parameter Values", Value = "`ONLINE`, `AWAY`, `AFK`, `BUSY`, `OFFLINE`, `INVISIBLE`" } };
                    return ScriptError($"Unexpected Value: {cond}", cmd, errorEmbed, LineInScript, line, fields);
            }
            return true;
        }
    }
}
