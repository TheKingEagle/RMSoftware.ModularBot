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
    public class RSKillCommand : ConsoleCommand
    {
        public RSKillCommand()
        {
            CommandName = "rskill";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            RestartRequested = console.ShowKillScreen("Test KS", "The program was instructed to run a test killscreen. This will auto restart the program.", true, ref ShutdownCalled, ref RestartRequested, 5, new ApplicationException("Command rskill triggered kill screen. USER INITIATED CRASH SCREEN.")).GetAwaiter().GetResult();

            return false;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
