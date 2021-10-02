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
                return ScriptError("Invalid Operation. Call EMBED_AUTHOR first.", cmd, errorEmbed, LineInScript, line);
            }
            //Get the line removing echo.
            string output = line.Remove(0, Name.Length).Trim();
            string ProcessedValue = engine.ProcessVariableString(gobj, output, cmd, client, message);
            if (string.IsNullOrWhiteSpace(ProcessedValue))
            {
                return ScriptError("Syntax is not correct.",
                     "<string imageURL>", cmd, errorEmbed, LineInScript, line);
            }
            try
            {
                CSEmbed.Author.IconUrl = ProcessedValue;
            }
            catch (ArgumentException ex)
            {

                EmbedFieldBuilder[] fields = { new EmbedFieldBuilder() { Name = "Internal Exception", Value = $"```\r\n{ex.Message}\r\n```", IsInline = false } };
                return ScriptError("Internal Exception thrown.", cmd, errorEmbed, LineInScript, line, fields);
            }
            return await Task.FromResult(true);
        }
    }
}
