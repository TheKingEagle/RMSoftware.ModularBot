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
    public class CSFEmbedAddFieldI : CSFunction
    {
        public CSFEmbedAddFieldI()
        {
            Name = "EMBED_ADDFIELD_I";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            //Get the line removing echo.
            string output = line.Remove(0, Name.Length).Trim();
            output = engine.ProcessVariableString(gobj, output, cmd, client, message);
            Regex r = new Regex("\"[^\"]*\"");
            #region ERRORS
            if (string.IsNullOrWhiteSpace(output) || r.Matches(output).Count < 2)
            {
                return ScriptError("Syntax is not correct.",
                    "\"<string Name>\" \"<string Value>\"", cmd, errorEmbed, LineInScript, line);
            }
            #endregion

            string emtitle = r.Matches(output)[0].Value.Replace("\"", "").Replace("&q;", "\"").Replace("&nl;", "\r\n");
            string content = r.Matches(output)[1].Value.Replace("\"", "").Replace("&q;", "\"").Replace("&nl;", "\r\n");

            #region MORE ERROR HANDLES
            if (string.IsNullOrWhiteSpace(emtitle))
            {
                return ScriptError("Field Name cannot be empty",
                    "\"<string Name>\" \"<string Value>\"", cmd, errorEmbed, LineInScript, line);
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                return ScriptError("Field Value cannot be empty",
                    "\"<string Name>\" \"<string Value>\"", cmd, errorEmbed, LineInScript, line);
            }
            #endregion

            CSEmbed.AddField(emtitle, content,true);
            return await Task.FromResult(true);
        }
    }
}
