using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModularBOT.Entity;
using Discord;
using Discord.Net;
using ModularBOT.Component;
using Microsoft.Extensions.DependencyInjection;
using Discord.WebSocket;

namespace ModularBOT.Component.ConsoleCommands
{
    public class LeaveCommand : ConsoleCommand
    {
        public LeaveCommand()
        {
            CommandName = "leave";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {

            string[] param = GetParameters(consoleInput);

            #region Parse Checking

            if (param.Length > 1)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "LEAVE", "Too many arguments!"),null,true,false,true);
                return true;
            }
            if (param.Length < 1)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "LEAVE", "Too few arguments!"), null, true, false, true);
                return true;
            }
            

            if (!ulong.TryParse(param[0], out ulong id))
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "LEAVE", "Guild ID was malformed!"), null, true, false, true);
                return true;
            }
            #endregion
            var G = discordNET.Client.GetGuild(id);
            if (G == null)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "LEAVE", "This guild isn't valid."), null, true, false, true);
                return true;
            }
            console.WriteEntry(new LogMessage(LogSeverity.Critical, "LEAVE", $"Attempting to leave guild: {G.Name}"), null, true, false, true);

            G.LeaveAsync();

            console.WriteEntry(new LogMessage(LogSeverity.Verbose, "LEAVE", $"Operation Complete"), null, true, false, true);

            return true;

        }
    }
}
