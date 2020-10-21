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
                errorEmbed.WithDescription($"`STATUSORB` Function Error: Preemptive rate limit reached. Please slow down your script with `WAIT`\r\n```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            if (cmd.CommandAccessLevel < AccessLevels.Administrator)
            {
                errorEmbed.WithDescription($"Function error: This requires `AccessLevels.Administrator`");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
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
                    errorEmbed.WithDescription($"Function error. Unexpected argument: {cond}.\r\nTry either ONLINE, BUSY, AWAY, AFK, INVISIBLE, OFFLINE.");
                    errorEmbed.AddField("Line", LineInScript, true);
                    errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                    return false;
            }
            return true;
        }
    }
}
