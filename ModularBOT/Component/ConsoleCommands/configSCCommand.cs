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
using System.Threading;
using System.Globalization;

namespace ModularBOT.Component.ConsoleCommands
{
    public class ConfigSCCommand : ConsoleCommand
    {
        public ConfigSCCommand()
        {
            CommandName = "config.setcolors";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            string PRV_TITLE = console.ConsoleTitle;
            List<LogEntry> v = new List<LogEntry>();

            #region Background Color
            ScreenModal = true;
            console.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Console Colors", 1, 2, ConsoleColor.Green);
            console.WriteEntry("\u2502 Please select a background color.");
            console.WriteEntry("\u2502");
            for (int i = 0; i < 16; i++)
            {
                console.WriteEntry($"\u2502\u2005\u2005\u2005 {i.ToString("X")}. {((ConsoleColor)i).ToString()}", (ConsoleColor)i);
            }
            console.WriteEntry("\u2502");
            ConsoleKeyInfo k;
            ScreenModal = true;
            while (true)
            {
                console.WriteEntry("\u2502 Please enter a choice below...", ConsoleColor.DarkBlue, true);
                Console.Write("\u2502 > ");
                k = Console.ReadKey();
                Thread.Sleep(100);
                char c = k.KeyChar;
                if (int.TryParse(c.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int i))
                {
                    Program.configMGR.CurrentConfig.ConsoleBackgroundColor = (ConsoleColor)i;
                    break;
                }
            }
            #endregion

            #region Foreground Color
            console.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Console Colors", 2, 2, ConsoleColor.Green);
            console.WriteEntry("\u2502 Please select a foreground color.");
            console.WriteEntry("\u2502");
            for (int i = 0; i < 16; i++)
            {
                console.WriteEntry($"\u2502\u2005\u2005\u2005 {i.ToString("X")}. {((ConsoleColor)i).ToString()}", (ConsoleColor)i);
            }
            console.WriteEntry("\u2502");
            ConsoleKeyInfo k1;
            ScreenModal = true;
            while (true)
            {
                console.WriteEntry("\u2502 Please enter a choice below...", ConsoleColor.DarkBlue, true);
                Console.Write("\u2502 > ");
                k1 = Console.ReadKey();
                Thread.Sleep(100);
                char c = k1.KeyChar;
                if (int.TryParse(c.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int ie))
                {
                    Program.configMGR.CurrentConfig.ConsoleForegroundColor = (ConsoleColor)ie;
                    break;
                }
            }
            #endregion

            Program.configMGR.Save();
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
            console.WriteEntry(new LogMessage(LogSeverity.Info, "Config", "Console colors were changed successfully."), null, true, false, true);
            v = null;
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
