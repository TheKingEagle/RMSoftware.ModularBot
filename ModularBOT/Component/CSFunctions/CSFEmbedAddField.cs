using Discord;
using Discord.WebSocket;
using ModularBOT.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ModularBOT.Component.CSFunctions
{
    public class CSFEmbedAddField : CSFunction
    {
        public CSFEmbedAddField()
        {
            Name = "EMBED_ADDFIELD";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            //Get the line removing echo.
            string output = line.Remove(0, Name.Length).Trim();
            output = engine.ProcessVariableString(gobj, output, cmd, client, message);
            Regex r = new Regex("\"[^\"]*\"");
            #region ERRORS
            if (string.IsNullOrWhiteSpace(output))
            {
                errorEmbed.WithDescription($"The Syntax of the command is incorrect. ```{line}```");
                errorEmbed.AddField("Usage", "```\nEMBED_ADDFIELD \"Title in quotes\" \"Content in quotes\"\n```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return await Task.FromResult(false);
            }
            if (r.Matches(output).Count < 2)
            {
                errorEmbed.WithDescription($"The Syntax of the command is incorrect. ```{line}```");
                errorEmbed.AddField("Usage", "```\nEMBED_ADDFIELD \"Title in quotes\" \"Content in quotes\"\n```");
                errorEmbed.AddField("NOTES:", "• The title & content will always be set by the first two group of quotes.\r\n• If you want to have double-quotes within the content or title use `&q;` before and after the content you want to quote.");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return await Task.FromResult(false);
            }
            #endregion

            string emtitle = r.Matches(output)[0].Value.Replace("\"", "").Replace("&q;", "\"").Replace("&nl;", "\r\n");
            string content = r.Matches(output)[1].Value.Replace("\"", "").Replace("&q;", "\"").Replace("&nl;", "\r\n");

            #region MORE ERROR HANDLES
            if (string.IsNullOrWhiteSpace(emtitle))
            {
                errorEmbed.WithDescription($"Title cannot be empty! ```{line}```");
                errorEmbed.AddField("Usage", "```\nEMBED_ADDFIELD \"Title in quotes\" \"Content in quotes\"\n```");
                errorEmbed.AddField("NOTES:", "• The title & content will always be set by the first two group of quotes.\r\n• If you want to have double-quotes within the content or title use `&q;` before and after the content you want to quote.");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return await Task.FromResult(false);
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                errorEmbed.WithDescription($"Content cannot be empty! ```{line}```");
                errorEmbed.AddField("Usage", "```\nEMBED_ADDFIELD \"Title in quotes\" \"Content in quotes\"\n```");
                errorEmbed.AddField("NOTES:", "• The title & content will always be set by the first two group of quotes.\r\n• If you want to have double-quotes within the content or title use `&q` before and after the content you want to quote.");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return await Task.FromResult(false);
            }
            #endregion

            CSEmbed.AddField(emtitle, content);
            return await Task.FromResult(true);
        }
    }
}
