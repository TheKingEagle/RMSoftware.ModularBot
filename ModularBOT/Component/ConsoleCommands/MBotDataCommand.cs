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
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace ModularBOT.Component.ConsoleCommands
{
    public class MBotDataCommand : ConsoleCommand
    {
        public MBotDataCommand()
        {
            CommandName = "mbotdata";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            
            console.WriteEntry(new LogMessage(LogSeverity.Info, "Console", "Opening ModularBOT's installation directory."));
            Process.Start(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location));
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
