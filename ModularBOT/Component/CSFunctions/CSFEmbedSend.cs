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
    public class CSFEmbedSend : CSFunction
    {
        public CSFEmbedSend()
        {
            Name = "EMBED_SEND";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            engine.OutputCount++;
            if (engine.OutputCount > 4)
            {
                return ScriptError("Rate limit triggered! Add waits between executions.", cmd, errorEmbed, LineInScript, line);
            }
            //Get the line removing echo.
            if (contextToDM)
            {
                try
                {
                    await message.Author.SendMessageAsync("", false, CSEmbed.Build());
                }
                catch (Exception ex)
                {
                    EmbedFieldBuilder[] fields = { new EmbedFieldBuilder() { Name = "Internal Exception", Value = $"```\r\n{ex.Message}\r\n```", IsInline = false } };
                    return ScriptError("Internal Exception thrown.", cmd, errorEmbed, LineInScript, line, fields);
                }

            }
            else
            {
                if (ChannelTarget == 0)
                {
                    await message.Channel.SendMessageAsync("", false, CSEmbed.Build());
                }
                else
                {
                    SocketTextChannel channelfromid = await client.GetChannelAsync(ChannelTarget) as SocketTextChannel;
                    await channelfromid.SendMessageAsync("", false, CSEmbed.Build());
                }

            }


            return await Task.FromResult(true);
        }
    }
}
