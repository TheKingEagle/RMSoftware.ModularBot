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
namespace ModularBOT.Component.ConsoleCommands
{
    public class EnableCMDCommand : ConsoleCommand
    {
        public EnableCMDCommand()
        {
            CommandName = "enablecmd";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            console.WriteEntry(new LogMessage(LogSeverity.Info, "Console", "Command processing enabled."));

            discordNET.Client.SetStatusAsync(UserStatus.Online);
            discordNET.Client.SetGameAsync("for commands!", null, ActivityType.Watching);
            discordNET.DisableMessages = false;
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
