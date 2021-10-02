using Discord;
using Discord.WebSocket;
using ModularBOT.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModularBOT.Component.CSFunctions
{
    public class CSFStart : CSFunction
    {
        public CSFStart()
        {
            Name = "START";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            string output = line.Remove(0, Name.Length).Trim();
            
            if (string.IsNullOrWhiteSpace(engine.ProcessVariableString(gobj, output, cmd, client, message)))
            {
                return ScriptError("Synax Error: Path required", "<string path>", cmd, errorEmbed, LineInScript, line);

            }

            string scriptpath = @"scripts\" + engine.ProcessVariableString(gobj, output, cmd, client, message);
            if (!File.Exists(scriptpath))
            {
                EmbedFieldBuilder[] fields = { new EmbedFieldBuilder() { IsInline = false, Name = "At Path", Value = $"`../{scriptpath.Replace('\\', '/')}`" } };

                return ScriptError("Script could not be found.", cmd, errorEmbed, LineInScript, line,fields);
            }
            string eval = "";
            using (StreamReader SR = File.OpenText(scriptpath))
            {
                eval = "```DOS\r\n" + SR.ReadToEnd() + "\r\n```";
                SR.Close();
            }
            await engine.EvaluateScript(gobj, eval, cmd, client, message, CSEmbed);
            return await Task.FromResult(true);
        }
    }
}
