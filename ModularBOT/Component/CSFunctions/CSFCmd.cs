using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ModularBOT.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModularBOT.Component.CSFunctions
{
    public class CSFCmd : CSFunction
    {
        public CSFCmd()
        {
            Name = "CMD";
        }
        public override async Task<bool> Evaluate(CoreScript engine, GuildObject gobj, string response, GuildCommand cmd, IDiscordClient client, IMessage message, EmbedBuilder errorEmbed, int LineInScript, string line, bool contextToDM, ulong ChannelTarget = 0, EmbedBuilder CSEmbed = null, bool StartCORE=false)
        {
            return await Task.FromResult(CaseExecCmd(engine, engine.ProcessVariableString(gobj, line, cmd, client, message),
                engine.ccmgr,cmd, gobj, ref errorEmbed, ref LineInScript, ref client, ref message));
        }

        private bool CaseExecCmd(CoreScript engine, string line, CustomCommandManager ccmg, GuildCommand cmd, GuildObject guildObject, ref EmbedBuilder errorEmbed, ref int LineInScript,
            ref IDiscordClient client, ref IMessage ArgumentMessage)
        {
            ulong gid = 0;
            if (ArgumentMessage.Channel is SocketGuildChannel channel)
            {
                gid = channel.Guild.Id;
            }
            guildObject = ccmg.GuildObjects.FirstOrDefault(x => x.ID == gid) ?? ccmg.GuildObjects.FirstOrDefault(x => x.ID == 0);

            string ecmd = line.Remove(0, Name.Length).Trim();
            
            string resp = ccmg.ProcessMessage(new PseudoMessage(guildObject.CommandPrefix + ecmd, ArgumentMessage.Author as SocketUser,
                (ArgumentMessage.Channel as IGuildChannel), MessageSource.Bot));
            if (resp != "SCRIPT" && resp != "EXEC" && resp != "" && resp != "CLI_EXEC" && resp != null)
            {

                ArgumentMessage.Channel.SendMessageAsync(resp);
                engine.LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", line));
                engine.LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", "CustomCMD Success..."));
                return true;
            }
            if ((resp == "SCRIPT" || resp == "EXEC" || resp == "" || resp == "CLI_EXEC") && resp != null)
            {

                engine.LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", line));
                engine.LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", "CustomCMD Success..."));
                return true;
            }
            //Damn, I can't be sassy here... If it was a command, but not a ccmg command, then try the context for modules. If THAT didn't work
            //Then it will output the result of the context.
            var context = new CommandContext(client, new PseudoMessage(guildObject.CommandPrefix + ecmd, ArgumentMessage.Author as SocketUser, (ArgumentMessage.Channel as IGuildChannel), MessageSource.Bot));
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = engine.cmdsvr.ExecuteAsync(context, guildObject.CommandPrefix.Length, engine.Services);
            engine.LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", line));
            engine.LogToConsole(new LogMessage(LogSeverity.Info, "CoreScript", result.Result.ToString()));
            if (!result.Result.IsSuccess)
            {
                return ScriptError($"The command context returned the following error:\r\n`{result.Result.ErrorReason}`", cmd, errorEmbed, LineInScript, line);
            }
            return true;
        }
    }
}
