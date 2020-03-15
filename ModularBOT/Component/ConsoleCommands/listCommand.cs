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
    public class ListCommand : ConsoleCommand
    {
        public ListCommand()
        {
            CommandName = "list";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            //TODO: Replace with user code.
            console.WriteEntry(new LogMessage(LogSeverity.Critical, "ConsoleIO","Printing current ConsoleCommands List"));

            foreach (var item in console.ConsoleCommands)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "ConsoleIO", item.CommandName));
                Thread.Sleep(10);
            }
            console.WriteEntry(new LogMessage(LogSeverity.Critical, "ConsoleIO", $"Listed {console.ConsoleCommands.Count} commands."));

            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
