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
    public class SetgchCommand : ConsoleCommand
    {
        public SetgchCommand()
        {
            CommandName = "setgch";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            string input = consoleInput.Remove(0, CommandName.Length).Trim();
            if (!ulong.TryParse(input, out console.chID))
            {
                console.WriteEntry(new LogMessage(LogSeverity.Error, "Console", "Invalid ULONG."));
                return true;
            }
            console.WriteEntry(new LogMessage(LogSeverity.Error, "Console", "Set guild channel id."));
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
