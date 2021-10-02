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
    public class CSFEcho:CSFunction
    {
        
        public CSFEcho()
        {
            Name = "ECHO";
        }


        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            engine.OutputCount++;
            if (engine.OutputCount > 4)
            {
                return ScriptError("Rate limit triggered! Add waits between executions.", cmd, errorEmbed, LineInScript, line);
            }
            string output = line.Remove(0, Name.Length).Trim();
            if (string.IsNullOrWhiteSpace(engine.ProcessVariableString(gobj, output, cmd, client, message)))
            {
                return ScriptError("Message string cannot be empty", "<string Message>", cmd, errorEmbed, LineInScript, line);
            }
            if (contextToDM)
            {
                await message.Author.SendMessageAsync(engine.ProcessVariableString(gobj, output, cmd, client, message), false);
            }
            else
            {
                if (ChannelTarget == 0)
                {
                    await message.Channel.SendMessageAsync(engine.ProcessVariableString(gobj, output, cmd, client, message), false);
                }
                else
                {
                    SocketTextChannel channelfromid = await client.GetChannelAsync(ChannelTarget) as SocketTextChannel;
                    await channelfromid.SendMessageAsync(engine.ProcessVariableString(gobj, output, cmd, client, message), false);
                }
            }
            return true;
        }
    }
}
