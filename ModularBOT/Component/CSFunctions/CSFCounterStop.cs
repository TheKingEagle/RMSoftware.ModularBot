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
    public class CSFCounterStop:CSFunction
    {
        public CSFCounterStop()
        {
            Name = "COUNTER_STOP";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            if (message.Channel as SocketTextChannel == null)
            {
                
                //errorMessage = $"SCRIPT ERROR:```\r\nUnexpected header:``` ```{line}```\r\n```CoreScript engine\r\nLine:{LineInScript}\r\nCommand: {cmd}```\r\nAdditional info: Multi-line formatting required.";
                errorEmbed.WithDescription($"This function is unavailable in DMs. ```\r\nCOUNTER_STOP\r\n```");
                errorEmbed.AddField("Additional Information", "This function can only be used in a GUILD CHANNEL.");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            if (CoreScript.MessageCounter.ContainsKey(message.Channel.Id))
            {
                CoreScript.MessageCounter.Remove(message.Channel.Id);
            }

            return await Task.FromResult(true);
        }
    }
}
