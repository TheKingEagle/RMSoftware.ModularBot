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
    public class ConfigUPRCCommand : ConsoleCommand
    {
        public ConfigUPRCCommand()
        {
            CommandName = "config.useprereleasechannel";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            string input = consoleInput;
            if (input.Split(' ').Length > 1)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too many arguments!"));
                return true;
            }
            if (input.Split(' ').Length < 1)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too few arguments!"));
                return true;
            }
            input = input.Remove(0, CommandName.Length).Trim();
            if (bool.TryParse(input, out bool result))
            {
                Program.configMGR.CurrentConfig.UsePreReleaseChannel = result;
                Program.configMGR.Save();
                console.WriteEntry(new LogMessage(LogSeverity.Info, "Console", "You've switched update channels."), null, true, false, true);
            }
            else
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Unexpected argument."));
                return true;
            }
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
