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
    public class CLSCommand : ConsoleCommand
    {
        public CLSCommand()
        {
            CommandName = "cls";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            ConsoleIO.LogEntries.Clear();//remove buffer.
            console.ConsoleGUIReset(console.ConsoleForegroundColor, console.ConsoleBackgroundColor, ConsoleIO.ConsoleTitle);
            SpinWait.SpinUntil(() => !ConsoleIO.ScreenBusy);
            console.WriteEntry(new LogMessage(LogSeverity.Info, "Console", "Console cleared!"), null, true, false, true);
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
