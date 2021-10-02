﻿using Discord;
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
                return ScriptError("Attachment path cannot be empty.", cmd, errorEmbed, LineInScript, line);

            }
            if (!File.Exists(@"attachments\" + engine.ProcessVariableString(gobj, output, cmd, client, message)))
            {
                EmbedFieldBuilder[] fields = { new EmbedFieldBuilder() { IsInline = false, Name = "Path", Value = $"`.. / attachments / {output}`" } };
                return ScriptError("Attachment could not be found.", cmd, errorEmbed, LineInScript, line,fields);
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
                    EmbedFieldBuilder[] fields = { new EmbedFieldBuilder() { Name = "Internal Exception", Value = $"```\r\n{ex.Message}\r\n```", IsInline = false } };
                    return ScriptError("Internal Exception thrown.", cmd, errorEmbed, LineInScript, line, fields);
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
