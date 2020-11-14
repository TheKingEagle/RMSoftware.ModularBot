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
using ModularBOT.Component.ConsoleScreens;
using static ModularBOT.Component.ConsoleIO;
using System.Threading;

namespace ModularBOT.Component.ConsoleCommands
{
    public class GuildsCommand : ConsoleCommand
    {
        public GuildsCommand()
        {
            CommandName = "guilds";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            string[] param = GetParameters(consoleInput);
            short startpage = 1;
            if (param.Length > 0)
            {
                if(!short.TryParse(param[0],out startpage))
                {
                    console.WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Value must be a number!"));
                    return true;
                }
                if(startpage < 1)
                {
                    console.WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Value cannot be less than 1"));
                    return true;
                }
            }
            console.ShowConsoleScreen(new GuildsScreen(discordNET,console, discordNET.Client.Guilds.ToList(),startpage),true);
            return true;
        }
    }
}
