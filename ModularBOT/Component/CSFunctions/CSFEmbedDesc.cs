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
            string desc = engine.ProcessVariableString(gobj, output, cmd, client, message)
                .Replace("&q;", "\"").Replace("&nl;", "\r\n").Replace("&bt;", "`");
            
            if (string.IsNullOrWhiteSpace(desc))
            {
                return ScriptError("Syntax is not correct.",
                    "<string description>", cmd, errorEmbed, LineInScript, line);
            }
            if(desc.Length > EmbedBuilder.MaxDescriptionLength)
            {
                return ScriptError($"Description must be no more than {EmbedBuilder.MaxDescriptionLength} characters.", 
                    cmd, errorEmbed, LineInScript, line);
            }
            CSEmbed.WithDescription(desc);
            
            return await Task.FromResult(true);
        }
    }
}
