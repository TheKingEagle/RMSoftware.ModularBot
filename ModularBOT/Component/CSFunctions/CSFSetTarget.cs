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
    public class CSFSetTarget : CSFunction
    {
        public CSFSetTarget()
        {
            Name = "SET_TARGET";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            string output = line.Remove(0, Name.Length).Trim();
            string ProcessedValue = engine.ProcessVariableString(gobj, output, cmd, client, message);
            EmbedFieldBuilder[] fields = { new EmbedFieldBuilder() { IsInline = false, Name = "Supported Targets", Value = "• `DIRECT`\r\n• `CHANNEL [optional ID]`" } };

            if (string.IsNullOrWhiteSpace(ProcessedValue))
            {
                return ScriptError("Synax Error: Target required","<string Target>", cmd, errorEmbed, LineInScript, line, fields);
            }
            if (output.ToUpper().StartsWith("CHANNEL"))
            {
                engine.SetTarget(false, 0);
                if (output.ToUpper() != "CHANNEL")
                {
                    string ulparse = ProcessedValue.ToUpper().Replace("CHANNEL", "").Trim();
                    if (!ulong.TryParse(ulparse, out ulong tempid))
                    {
                        return ScriptError("Synax Error: Invalid Channel ID", "<string Target>", cmd, errorEmbed, LineInScript, line, fields);
                    }
                    else
                    {
                        if (await client.GetChannelAsync(tempid) == null)
                        {
                            return ScriptError("Channel did not exist.", "<string Target>", cmd, errorEmbed, LineInScript, line, fields);
                        }
                        else
                        {
                            if ((await client.GetChannelAsync(tempid)) is SocketTextChannel)
                            {
                                engine.SetTarget(false, tempid);
                            }
                            else
                            {
                                return ScriptError("Channel was not a text channel", "<string Target>", cmd, errorEmbed, LineInScript, line, fields);
                            }
                        }
                    }
                }
            }
            if (output.ToUpper() == "DIRECT")
            {
                engine.SetTarget(true, ChannelTarget);
            }
            return await Task.FromResult(true);
        }
    }
}
