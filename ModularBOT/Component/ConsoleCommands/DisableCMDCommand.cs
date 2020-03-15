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
    public class DisableCMDCommand : ConsoleCommand
    {
        public DisableCMDCommand()
        {
            CommandName = "disablecmd";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            console.WriteEntry(new LogMessage(LogSeverity.Warning, "Console", "Command processing disabled!"));

            discordNET.Client.SetStatusAsync(UserStatus.DoNotDisturb);
            discordNET.Client.SetGameAsync("");
            discordNET.DisableMessages = true;
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
