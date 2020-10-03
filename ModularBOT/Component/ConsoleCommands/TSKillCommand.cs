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
    public class TSKillCommand : ConsoleCommand
    {
        public TSKillCommand()
        {
            CommandName = "tskill";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            RestartRequested = console.ShowKillScreen("Test KS", "The program was instructed to run a test killscreen. This will prompt you to terminate the program.", false, ref ShutdownCalled, ref RestartRequested, 5, new ApplicationException("Command tskill triggered kill screen. USER INITIATED CRASH SCREEN."),"USER_INITIATED_CRASH").GetAwaiter().GetResult();
            
            return false;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
