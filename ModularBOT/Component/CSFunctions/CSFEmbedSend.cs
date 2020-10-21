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
                errorEmbed.WithDescription($"`EMBED_SEND` Function Error: Preemptive rate limit reached. Please slow down your script with `WAIT`\r\n```{line}```");
                errorEmbed.AddField("Line", LineInScript, true);
                errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                return await Task.FromResult(false);
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

                    errorEmbed.WithDescription($"The script failed due to an exception ```{line}```");
                    errorEmbed.AddField("details", $"```{ex.Message}```");
                    errorEmbed.AddField("Line", LineInScript, true);
                    errorEmbed.AddField("Execution Context", cmd?.Name ?? "No context", true);
                    return await Task.FromResult(false);
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
