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
    public class CSFEmbedAuthorI : CSFunction
    {
        public CSFEmbedAuthorI()
        {
            Name = "EMBED_AUTHOR_I";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            if (CSEmbed.Author == null)
            {
                errorEmbed.WithDescription($"You must use `EMBED_AUTHOR` first. ```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return await Task.FromResult(false);
            }
            //Get the line removing echo.
            string output = line.Remove(0, Name.Length).Trim();
            string ProcessedValue = engine.ProcessVariableString(gobj, output, cmd, client, message);
            if (string.IsNullOrWhiteSpace(ProcessedValue))
            {
                errorEmbed.WithDescription($"String cannot be empty. ```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return await Task.FromResult(false);
            }
            try
            {
                CSEmbed.Author.IconUrl = ProcessedValue;
            }
            catch (ArgumentException ex)
            {

                errorEmbed.WithDescription($"Argument Exception: `{ex.Message}` ```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return await Task.FromResult(false);
            }
            return await Task.FromResult(true);
        }
    }
}
