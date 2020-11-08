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
    public class CSFCounterStart : CSFunction
    {
        public CSFCounterStart()
        {
            Name = "COUNTER_START";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            if (message.Channel as SocketTextChannel == null)
            {
                errorEmbed.WithDescription($"This function is unavailable in DMs. ```\r\nCOUNTER_START\r\n```");
                errorEmbed.AddField("Additional Information", "This function can only be used in a GUILD CHANNEL.");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return await Task.FromResult(false);
            }
            if (CoreScript.MessageCounter.ContainsKey(message.Channel.Id))
            {
                CoreScript.MessageCounter.Remove(message.Channel.Id);
            }
            CoreScript.MessageCounter.Add(message.Channel.Id, 0);

            return await Task.FromResult(true);
        }
    }
}
