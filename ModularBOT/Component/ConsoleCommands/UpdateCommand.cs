﻿using System;
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
using System.Diagnostics;

namespace ModularBOT.Component.ConsoleCommands
{
    public class UpdateCommand : ConsoleCommand
    {
        public UpdateCommand()
        {
            CommandName = "update";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            string PRV_TITLE = console.ConsoleTitle;
            List<LogEntry> v = new List<LogEntry>();
            ScreenModal = true;
            //---------------start modal---------------
            var NGScreen = new UpdaterConsoleScreen(ref Program.configMGR.CurrentConfig, ref discordNET)
            {
                ActiveScreen = true
            };
            NGScreen.RenderScreen();
            while (true)
            {
                if (NGScreen.ProcessInput(Console.ReadKey(true)))
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
            if(NGScreen.InstallUpdate)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "MAIN", "Terminating application and running system update..."));
                Program.ImmediateTerm = true;
                Process.Start(NGScreen.UPDATERLOC);
                Thread.Sleep(1000);
                discordNET.Stop(ref ShutdownCalled);
                RestartRequested = false;
                
                return false;
            }
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
