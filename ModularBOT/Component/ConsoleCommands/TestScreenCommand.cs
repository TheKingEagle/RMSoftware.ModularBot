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

            string PRV_TITLE = console.ConsoleTitle;
            List<LogEntry> v = new List<LogEntry>();
            ScreenModal = true;
            //---------------start modal---------------
            var NGScreen = new TestConsoleScreen()
            {
                ActiveScreen = true
            };
            ActiveScreen = NGScreen;
            NGScreen.RenderScreen();
            while (true)
            {
                if (NGScreen.ProcessInput(Console.ReadKey(true)))
                {
                    break;
                }
            }
            NGScreen.ActiveScreen = false; ConsoleIO.ActiveScreen = null;
            //----------------End modal----------------
            console.ConsoleGUIReset(Program.configMGR.CurrentConfig.ConsoleForegroundColor,
                Program.configMGR.CurrentConfig.ConsoleBackgroundColor, PRV_TITLE);
            ScreenModal = false;
            v.AddRange(console.LogEntries);
            console.LogEntries.Clear();//clear buffer.
                                       //output previous logEntry.
            foreach (var item in v)
            {
                console.WriteEntry(item.LogMessage, item.EntryColor);
            }
            return true;
        }

    }
}
