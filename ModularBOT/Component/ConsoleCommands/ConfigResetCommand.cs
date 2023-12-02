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

namespace ModularBOT.Component.ConsoleCommands
{
    public class ConfigResetCommand : ConsoleCommand
    {
        public ConfigResetCommand()
        {
            CommandName = "config.reset";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {

            string PRV_TITLE = ConsoleIO.ConsoleTitle;
            List<LogEntry> v = new List<LogEntry>();
            ScreenModal = true;
            //---------------start modal---------------
            var NGScreen = new PromptScreen("Configuration Warning",
                "   You are about to reset the configuration file. This will restart the application and run the initial setup wizard." +
                " This will not remove installed modules, module configuration, nor any custom commands.")
            {
                ActiveScreen = true
            };
            ActiveScreen = NGScreen;
            NGScreen.RenderScreen();
            int res = NGScreen.Show("Reset Configuration?", "This action cannot be undone.", ConsoleColor.DarkRed, ConsoleColor.White);
            
            NGScreen.ActiveScreen = false; ConsoleIO.ActiveScreen = null;
            //----------------End modal----------------
            console.ConsoleGUIReset(Program.configMGR.CurrentConfig.ConsoleForegroundColor,
                Program.configMGR.CurrentConfig.ConsoleBackgroundColor, PRV_TITLE);
            ScreenModal = false;
            v.AddRange(ConsoleIO.LogEntries);
            ConsoleIO.LogEntries.Clear();//clear buffer.
                                       //output previous logEntry.
            foreach (var item in v)
            {
                console.WriteEntry(item.LogMessage, item.EntryColor);
            }
            if(res == 2)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "Config", 
                    "User initiated configuration reset. The application will now restart."), null, false, true, true);
                Program.configMGR.Delete();
                Thread.Sleep(2000);
                RestartRequested = true;
                ShutdownCalled = true;
                Program.ImmediateTerm = true;
            }
            return true;
        }
    }
}
