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
                errorEmbed.WithDescription($"SCRIPT path cannot be empty. ```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            
            string scriptpath = @"scripts\" + engine.ProcessVariableString(gobj, output, cmd, client, message);
            if (!File.Exists(scriptpath))
            {
                errorEmbed.WithDescription($"script file could not be found. ```{line}```");
                errorEmbed.AddField("Path", $"`../{scriptpath.Replace('\\','/')}`", false);
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
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
