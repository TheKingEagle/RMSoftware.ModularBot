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
using System.Threading;
namespace ModularBOT.Component.ConsoleCommands
{
    public class ConIOCrashCommand : ConsoleCommand
    {
        public ConIOCrashCommand()
        {
            CommandName = "iocrash";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            
            console.WriteEntry(new LogMessage(LogSeverity.Info, "██▒", "iocrash command"), null, true, false, true);
            
            return false;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
