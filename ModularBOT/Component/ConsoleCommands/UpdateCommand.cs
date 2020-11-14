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
using static ModularBOT.Component.ConsoleIO;
using ModularBOT.Component.ConsoleScreens;
using System.Threading;
using System.Diagnostics;

namespace ModularBOT.Component.ConsoleCommands
{
    public class UpdateCommand : ConsoleCommand
    {
        public UpdateCommand()
        {
            CommandName = "update";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            var NGScreen = new UpdaterScreen(ref Program.configMGR.CurrentConfig, ref discordNET);
            
            console.ShowConsoleScreen(NGScreen, true);
            
            if(NGScreen.InstallUpdate)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Terminating application and running system update..."));
                Program.ImmediateTerm = true;
                Process.Start(NGScreen.UPDATERLOC);
                Thread.Sleep(500);
                discordNET.Stop(ref ShutdownCalled);
                RestartRequested = false;
                
                return false;
            }
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
