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
using Discord.WebSocket;

namespace ModularBOT.Component.ConsoleCommands
{
    public class MyRolesCommand : ConsoleCommand
    {
        public MyRolesCommand()
        {
            CommandName = "myroles";
        }
        public override bool Execute(string consoleInput, ref bool ShutdownCalled, ref bool RestartRequested, ref bool InputCanceled, ref DiscordNET discordNET, ref ConsoleIO console)
        {
            string input = consoleInput;
            string page = "1";

            #region Parse Checking

            if (input.Split(' ').Length > 3)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too many arguments!"));
                return true;
            }
            if (input.Split(' ').Length < 2)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "Console", "Too few arguments!"));
                return true;
            }
            if (input.Split(' ').Length < 3)
            {
                input = input.Remove(0, 8).Trim();
            }
            if (input.Split(' ').Length == 3)
            {
                page = input.Split(' ')[2];
                input = input.Split(' ')[1];
            }
            if (!short.TryParse(page, out short numpage))
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "MyRoles", "Invalid Page number"));
                return true;
            }
            if (!ulong.TryParse(input, out ulong id))
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "MyRoles", "Invalid Guild ID format"));
                return true;
            }

            SocketGuild guild = discordNET.Client.GetGuild(id);

            if (guild == null)
            {
                console.WriteEntry(new LogMessage(LogSeverity.Critical, "List Users", "Invalid Guild."));
                return true;
            }
            #endregion Parse Checking

            string PRV_TITLE = console.ConsoleTitle;
            List<LogEntry> v = new List<LogEntry>();
            //---------------start modal---------------
            string gname = guild.Name;
            if (gname.Length > 32)
            {
                gname = gname.Remove(29) + "...";
            }
            string title = $"Listing bot's roles";
            var NGScreen = new RolesScreen(discordNET, guild,guild.CurrentUser.Roles.ToList(), title)
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
            
            return true;
            //return base.Execute(consoleInput, ref ShutdownCalled, ref RestartRequested, ref InputCanceled, ref discordNET);
        }
    }
}
