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

namespace ModularBOT.Component.ConsoleCommands
{
    public class TestScreenCommand : ConsoleCommand
    {
        public TestScreenCommand()
        {
            CommandName = "testscreen";
        }

        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {

            console.ShowConsoleScreen(new TestScreen(), true);
            return true;
        }

    }
}
