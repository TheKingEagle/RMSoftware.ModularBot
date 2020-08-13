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

            string PRV_TITLE = console.ConsoleTitle;
            List<LogEntry> v = new List<LogEntry>();
            ScreenModal = true;
            //---------------start modal---------------
            var NGScreen = new GuildsScreen(discordNET.Client.Guilds.ToList(),discordNET)
            {
                ActiveScreen = true
            };
            NGScreen.RenderScreen();
            while (true)
            {
                if(NGScreen.ProcessInput(Console.ReadKey(true)))
                {
                    break;
                }
            }
            NGScreen.ActiveScreen = false;
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
