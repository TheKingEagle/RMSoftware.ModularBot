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
    public class CSFEmbedDesc:CSFunction
    {
        
        public CSFEmbedDesc()
        {
            Name = "EMBED_DESC";
        }


        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            string output = line.Remove(0, Name.Length).Trim();
            string desc = engine.ProcessVariableString(gobj, output, cmd, client, message).Replace("&q;", "\"").Replace("&nl;", "\r\n").Replace("&bt;", "`");
            
            if (string.IsNullOrWhiteSpace(desc))
            {
                errorEmbed.WithDescription($"String cannot be empty. ```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            CSEmbed.WithDescription(desc);
            
            return await Task.FromResult(true);
        }
    }
}
