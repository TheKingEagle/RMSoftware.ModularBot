using Discord;
using ModularBOT.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModularBOT.Entity
{
    public class CSFunction
    {
        public string Name { get; protected set; }

        public virtual async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool isFile=false)
        {
            return await Task.FromResult(true);//derive... On error, returns false with ErrorEmbed set
        }

        public bool ScriptError(string message, GuildCommand cmd, EmbedBuilder errorEmbed, int LineInScript, string line, EmbedFieldBuilder[] Additionalfields = null)
        {
            errorEmbed.WithDescription($"{message}\r\n```\r\n{line}\r\n```");
            errorEmbed.AddField("Line", LineInScript, true);
            errorEmbed.AddField("Execution Context", cmd?.Name ?? "No Context", true);
            if(Additionalfields?.Length > 0)
            {
                errorEmbed.WithFields(Additionalfields);
            }
            return false;
        }

        public bool ScriptError(string message, string usage, GuildCommand cmd, EmbedBuilder errorEmbed, int LineInScript, string line, EmbedFieldBuilder[] Additionalfields = null)
        {
            errorEmbed.WithDescription($"{message}\r\n```\r\n{line}\r\n```");
            errorEmbed.AddField("Usage", $"`{Name} {usage}`");
            errorEmbed.AddField("Line", LineInScript, true);
            errorEmbed.AddField("Execution Context", cmd?.Name ?? "No Context", true);
            if (Additionalfields?.Length > 0)
            {
                errorEmbed.WithFields(Additionalfields);
            }
            return false;
        }
    }
}
