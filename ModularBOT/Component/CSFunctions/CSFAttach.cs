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
    public class CSFAttach : CSFunction
    {
        public CSFAttach()
        {
            Name = "ATTACH";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            //Get the line removing ATTACH.
            string output = line.Remove(0, Name.Length).Trim();
            if (string.IsNullOrWhiteSpace(engine.ProcessVariableString(gobj, output, cmd, client, message)))
            {
                errorEmbed.WithDescription($"Attachment path cannot be empty. ```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }
            if (!File.Exists(@"attachments\" + engine.ProcessVariableString(gobj, output, cmd, client, message)))
            {
                errorEmbed.WithDescription($"Attachment could not be found. ```{line}```");
                errorEmbed.AddField("Path", "`../attachments/" + output + "`", false);
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return false;
            }

            string attachmentpath = @"attachments\" + engine.ProcessVariableString(gobj, output, cmd, client, message);
            if (contextToDM)
            {
                try
                {
                    await message.Author.SendFileAsync(attachmentpath);
                }
                catch (Exception ex)
                {

                    errorEmbed.WithDescription($"The script failed due to an exception ```{line}```");
                    errorEmbed.AddField("details", $"```{ex.Message}```");
                    errorEmbed.AddField("Line", LineInScript, true);
                    errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                    return false;
                }

            }
            else
            {
                var z = message.Channel.EnterTypingState();
                await message.Channel.SendFileAsync(attachmentpath);
                z.Dispose();

            }
            return await Task.FromResult(true);
        }
    }
}
