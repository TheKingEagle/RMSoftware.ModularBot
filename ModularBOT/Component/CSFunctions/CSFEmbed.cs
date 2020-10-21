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
    public class CSFEmbed:CSFunction
    {
        
        public CSFEmbed()
        {
            Name = "EMBED";
        }


        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            string output = line.Remove(0, Name.Length).Trim();
            string title = engine.ProcessVariableString(gobj, output, cmd, client, message);
            if (string.IsNullOrWhiteSpace(title))
            {
                errorEmbed.WithDescription($"Title string cannot be empty. ```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            if (CSEmbed == null)
            {
                CSEmbed = new EmbedBuilder
                {
                    Title = title
                };
            }
            else
            {
                CSEmbed.WithTitle(title);
            }
            engine.LogToConsole(new LogMessage(LogSeverity.Verbose, "CSEmbed", $"New Embed! Title: {CSEmbed.Title}"));
            return await Task.FromResult(true);
        }
    }
}
