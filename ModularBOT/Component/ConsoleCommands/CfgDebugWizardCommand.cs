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
    public class CfgDebugWizardCommand : ConsoleCommand
    {
        public CfgDebugWizardCommand()
        {
            CommandName = "config.debugwizard";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            string input = consoleInput;
            if (input.Split(' ').Length > 2)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too many arguments!"));
                return true;
            }
            if (input.Split(' ').Length < 2)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too few arguments!"));
                return true;
            }
            input = input.Remove(0, CommandName.Length).Trim();
            if (bool.TryParse(input, out bool result))
            {
                Program.configMGR.CurrentConfig.DebugWizard = result;
                Program.configMGR.Save();
                ConsoleIO.ScreenModal = true;
                while (true)
                {
                    console.WriteEntry(new LogMessage(LogSeverity.Info, "Console", "Changes will take place next time the program is started. Do you want to restart now? [Y/N]"), null, true, true, true);
                    ConsoleKeyInfo k = Console.ReadKey();
                    if (k.Key == ConsoleKey.Y)
                    {

                        discordNET.Stop(ref ShutdownCalled);
                        RestartRequested = true;
                        Thread.Sleep(1000);
                        return false;
                    }
                    if (k.Key == ConsoleKey.N)
                    {

                        ConsoleIO.ScreenModal = false;
                        return true;

                    }
                }

            }

            else console.WriteEntry(new LogMessage(LogSeverity.Critical, "Console", $"Invalid parameter. Try TRUE or FALSE."));
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
