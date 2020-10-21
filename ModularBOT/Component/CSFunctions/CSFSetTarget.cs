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
            if (string.IsNullOrWhiteSpace(ProcessedValue))
            {
                errorEmbed.WithDescription($"Invalid target context. ```{line}```");
                errorEmbed.AddField("Available targets", "• CHANNEL\r\n• DIRECT");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return await Task.FromResult(false);
            }
            if (output.ToUpper().StartsWith("CHANNEL"))
            {
                engine.SetTarget(false, 0);
                if (output.ToUpper() != "CHANNEL")
                {
                    string ulparse = ProcessedValue.ToUpper().Replace("CHANNEL", "").Trim();
                    if (!ulong.TryParse(ulparse, out ulong tempid))
                    {
                        errorEmbed.WithDescription($"Invalid Channel ID format. ```{line}```");
                        errorEmbed.AddField("Available targets", "• `CHANNEL [Optional Channel ID]`\r\n• `DIRECT`");
                        errorEmbed.AddField("Line", LineInScript, true);
                        errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                        return await Task.FromResult(false);
                    }
                    else
                    {
                        if (await client.GetChannelAsync(tempid) == null)
                        {
                            errorEmbed.WithDescription($"The channel with specified ID did not exist ```{line}```");
                            errorEmbed.AddField("Available targets", "• `CHANNEL [Optional Channel ID]`\r\n• `DIRECT`");
                            errorEmbed.AddField("Line", LineInScript, true);
                            errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                            return await Task.FromResult(false);
                        }
                        else
                        {
                            if ((await client.GetChannelAsync(tempid)) is SocketTextChannel)
                            {
                                engine.SetTarget(false, tempid);
                            }
                            else
                            {
                                errorEmbed.WithDescription($"The provided ID was for a valid text channel. ```{line}```");
                                errorEmbed.AddField("Available targets", "• `CHANNEL [Optional Channel ID]`\r\n• `DIRECT`");
                                errorEmbed.AddField("Line", LineInScript, true);
                                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                                return await Task.FromResult(false);
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
