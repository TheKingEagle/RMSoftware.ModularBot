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

namespace ModularBOT.Component.ConsoleCommands
{
    public class ConfigSLPCommand : ConsoleCommand
    {
        public ConfigSLPCommand()
        {
            CommandName = "config.setlogo";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            string PRV_TITLE = console.ConsoleTitle;
            List<LogEntry> v = new List<LogEntry>();

            console.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Startup Logo", 5, 6, ConsoleColor.Green);

            console.SetLogo_Choices();
            ConsoleKeyInfo k;
            string path = "";
            ScreenModal = true;
            while (true)
            {
                console.WriteEntry("\u2502 Please enter a choice below...", ConsoleColor.DarkBlue, true);
                Console.Write("\u2502 > ");
                k = Console.ReadKey();
                if (k.KeyChar == '1')
                {
                    path = "NONE";
                    break;
                }
                if (k.KeyChar == '2')
                {
                    path = "INTERNAL";
                    console.WriteEntry("\u2502 Previewing action... One second please...");
                    Thread.Sleep(600);
                    console.ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Welcome", 79, 45);
                    Console.WriteLine("Oh, Hello! Greetings! Salutations! Stuff is about to happen... Please wait...");
                    Thread.Sleep(800);
                    console.ConsoleWriteImage(Properties.Resources.RMSoftwareICO);
                    Thread.Sleep(3000);
                    console.ConsoleGUIReset(ConsoleColor.Cyan, ConsoleColor.Black, "Setup Wizard - Startup Logo", 5, 6, ConsoleColor.Green);
                    break;
                }
                if (k.KeyChar == '3')
                {
                    console.WriteEntry("\u2502 Please enter the path to a valid image file...", ConsoleColor.DarkBlue);
                    Console.Write("\u2502 > ");
                    path = Console.ReadLine();
                    console.WriteEntry("\u2502 Previewing action... One second please...");
                    Thread.Sleep(600);
                    console.ConsoleGUIReset(ConsoleColor.Green, ConsoleColor.Black, "Welcome", 79, 45);
                    Console.WriteLine("Oh, Hello! Greetings! Salutations! Stuff is about to happen... Please wait...");
                    Thread.Sleep(800);
                    try
                    {
                        console.ConsoleWriteImage(new System.Drawing.Bitmap(path.Replace("\"", "")));
                    }
                    catch (Exception ex)
                    {

                        console.WriteEntry("\u2502 Something went wrong. Make sure you specified a valid image.", ConsoleColor.Red);
                        console.WriteEntry("\u2502 " + ex.Message, ConsoleColor.Red);
                        console.WriteEntry("\u2502");
                        console.SetLogo_Choices();
                        continue;
                    }
                    Thread.Sleep(3000);
                    break;
                }
            }

            Program.configMGR.CurrentConfig.LogoPath = path.Replace("\"", "");
            Program.configMGR.Save();
            console.ConsoleGUIReset(ConsoleColor.White, ConsoleColor.DarkBlue, PRV_TITLE);
            ScreenModal = false;
            v.AddRange(console.LogEntries);
            console.LogEntries.Clear();//clear buffer.
                               //output previous logEntry.
            foreach (var item in v)
            {
                console.WriteEntry(item.LogMessage, item.EntryColor);
            }
            console.WriteEntry(new LogMessage(LogSeverity.Info, "Config", "Startup logo saved successfully!"), null, true, false, true);
            v = null;
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
